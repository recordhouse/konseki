"""Build the game-ready V4 character from its static source GLB.

blender -b -t 4 --factory-startup --python-exit-code 1 -P tools/rig_woman_v4.py
"""

import math
from pathlib import Path
from tempfile import TemporaryDirectory

import bmesh
import bpy
from mathutils import Matrix, Quaternion, Vector
from mathutils.kdtree import KDTree

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'artifacts/new-woman-v4/model.glb'
OUTPUT = ROOT / 'public/models/woman-v4-walk.glb'

HEIGHT = 1.68
MIDLINE = .002
WALK_SPEED = 1.12
FPS = 30
CYCLE_FRAMES = 32
STRIDE_METERS = WALK_SPEED * CYCLE_FRAMES / FPS
SIDES = ((1, '.L'), (-1, '.R'))
FORWARD = Vector((0, -1, 0))
UP = Vector((0, 0, 1))


def smooth(low, high, x):
    t = min(1., max(0., (x - low) / (high - low)))
    return t * t * (3 - 2 * t)


def split_seam(mesh, name, point, normal, bottom, top, palette):
    """Separate the surfaces of a weld and close the resulting interior faces."""
    side_layer = mesh.verts.layers.int.new(name)
    normal = normal.normalized()

    def distance(vert):
        return (vert.co - point).dot(normal)

    faces = []
    for face in mesh.faces:
        heights = [v.co.z for v in face.verts]
        if min(heights) >= top + .008 or max(heights) <= bottom - .008:
            continue
        distances = [distance(v) for v in face.verts]
        if min(distances) <= 0 <= max(distances):
            faces.append(face)

    geometry = {element for face in faces for element in (*face.verts, *face.edges, face)}
    result = bmesh.ops.bisect_plane(mesh, geom=list(geometry), dist=.0000001,
                                   plane_co=point, plane_no=normal)
    cut = {v for v in result['geom_cut'] if isinstance(v, bmesh.types.BMVert) and v.is_valid}
    negative_faces = {face for vert in cut if bottom < vert.co.z < top for face in vert.link_faces
                      if sum(distance(v) for v in face.verts) < 0}
    copies = {}
    for vert in cut:
        if not bottom < vert.co.z < top:
            continue
        # Copy exactly the vertices shared by faces on both sides of the plane.
        count = sum(face in negative_faces for face in vert.link_faces)
        if count == 0:
            vert[side_layer] = 1
        elif count == len(vert.link_faces):
            vert[side_layer] = -1
        else:
            duplicate = mesh.verts.new(vert.co.copy())
            duplicate[side_layer] = -1
            vert[side_layer] = 1
            copies[vert] = duplicate

    old_faces = []
    old_edges = set()
    uv_layer = mesh.loops.layers.uv.active
    for face in negative_faces:
        if not any(v in copies for v in face.verts):
            continue
        replacement = mesh.faces.new([copies.get(v, v) for v in face.verts])
        replacement.material_index = face.material_index
        replacement.smooth = face.smooth
        if uv_layer:
            for new_loop, old_loop in zip(replacement.loops, face.loops):
                new_loop[uv_layer].uv = old_loop[uv_layer].uv.copy()
        old_edges.update(face.edges)
        old_faces.append(face)
    for face in old_faces:
        mesh.faces.remove(face)
    for edge in old_edges:
        if edge.is_wire:
            mesh.edges.remove(edge)

    cut.update(copies.values())
    boundaries = {1: [], -1: []}
    for edge in mesh.edges:
        if edge.is_boundary and all(v in cut for v in edge.verts):
            side = next((v[side_layer] for v in edge.verts if v[side_layer]), 0)
            if side:
                boundaries[side].append(edge)
    filled = []
    for edges in boundaries.values():
        degrees = {}
        for edge in edges:
            for vert in edge.verts:
                degrees[vert] = degrees.get(vert, 0) + 1
        endpoints = [vert for vert, degree in degrees.items() if degree == 1]
        # The cut can stop where the surfaces still join. Close that last pair
        # of endpoints so the two sides can be capped separately.
        if len(endpoints) == 2:
            closing_edge = mesh.edges.get(endpoints) or mesh.edges.new(endpoints)
            edges.append(closing_edge)
        filled.extend(bmesh.ops.holes_fill(mesh, edges=edges, sides=0)['faces'])
    caps = bmesh.ops.triangulate(mesh, faces=filled)['faces']
    for face in caps:
        face.smooth = True
        if uv_layer:
            center = face.calc_center_median()
            region = 'skin'
            if name == 'leg_side' and center.z < .068:
                region = 'shoe'
            elif (name.startswith('arm_') and all(v[side_layer] <= 0 for v in face.verts)
                  and center.z > .472 + .023 * smooth(-.044, .025, center.y)):
                region = 'skirt'
            for loop in face.loops:
                loop[uv_layer].uv = palette[region]
    print(f'{name}: separated {len(copies)} shared vertices, closed {len(filled)} interior surfaces.')


def separate_limbs(figure):
    mesh = bmesh.new()
    mesh.from_mesh(figure.data)
    uv_layer = mesh.loops.layers.uv.active
    palette = {}
    if uv_layer:
        verts = list(mesh.verts)
        tree = KDTree(len(verts))
        for i, vert in enumerate(verts):
            tree.insert(vert.co, i)
        tree.balance()
        # Flat swatches from the source texture avoid smearing unrelated UV
        # islands across the inside surfaces exposed by the cuts.
        for region, sample in {
            'skin': (.049, -.058, .364),
            'shoe': (.039, -.079, .025),
            'skirt': (.058, -.063, .509),
        }.items():
            _, index, _ = tree.find(sample)
            palette[region] = next(iter(verts[index].link_loops))[uv_layer].uv.copy()

    split_seam(mesh, 'leg_side', Vector((MIDLINE, 0, 0)), Vector((1, 0, 0)), -.01, .447, palette)
    for sign, name in ((1, 'arm_l'), (-1, 'arm_r')):
        split_seam(mesh, name, Vector((MIDLINE + sign * .092, 0, .415)),
                   Vector((sign, 0, -.18)), .383, .507, palette)
    bmesh.ops.recalc_face_normals(mesh, faces=list(mesh.faces))
    for vert in mesh.verts:
        vert.co.x -= MIDLINE
    mesh.to_mesh(figure.data)
    mesh.free()
    figure.data.update()


def import_figure(texture_dir):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(SOURCE), merge_vertices=True)
    figure = next(obj for obj in bpy.context.scene.objects if obj.type == 'MESH')
    bpy.context.view_layer.objects.active = figure

    decimate = figure.modifiers.new('Game mesh', 'DECIMATE')
    decimate.ratio = .025
    bpy.ops.object.modifier_apply(modifier=decimate.name)
    separate_limbs(figure)
    for material in figure.data.materials:
        shader = next(node for node in material.node_tree.nodes if node.type == 'BSDF_PRINCIPLED')
        shader.inputs['Metallic'].default_value = 0
        shader.inputs['Roughness'].default_value = .85
    for image in [img for img in bpy.data.images if img.source == 'FILE' and img.has_data]:
        image.scale(2048, 2048)
        image.file_format = 'JPEG'
        image.filepath_raw = str(Path(texture_dir) / 'woman-v4-color.jpg')
        image.save()
        image.pack()

    height = max(v.co.z for v in figure.data.vertices) - min(v.co.z for v in figure.data.vertices)
    figure.name = 'Woman V4'
    return figure, HEIGHT / height


def make_rig():
    skeleton = bpy.data.armatures.new('V4 skeleton')
    rig = bpy.data.objects.new('Woman V4 Rig', skeleton)
    bpy.context.collection.objects.link(rig)
    bpy.context.view_layer.objects.active = rig
    rig.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT')

    def bone(name, head, tail, parent=None):
        edit = skeleton.edit_bones.new(name)
        edit.head, edit.tail = head, tail
        if parent:
            edit.parent = skeleton.edit_bones[parent]

    bone('Pelvis', (0, -.01, .527), (0, -.008, .558))
    bone('Spine', (0, -.008, .558), (0, -.017, .636), 'Pelvis')
    bone('Chest', (0, -.017, .636), (0, -.025, .777), 'Spine')
    bone('Head', (0, -.025, .777), (0, -.023, .966), 'Chest')
    bone('Hair', (0, .041, .861), (0, .087, .608), 'Head')

    for sign, side in SIDES:
        hip = (sign * .054, -.009, .527)
        knee = (sign * .031, -.030, .304)
        ankle = (sign * .020, -.010, .075)
        bone('Thigh' + side, hip, knee, 'Pelvis')
        bone('Shin' + side, knee, ankle, 'Thigh' + side)
        bone('Foot' + side, ankle, (sign * .027, -.083, .016), 'Shin' + side)

        shoulder = (sign * .085, -.021, .750)
        elbow = (sign * .105, -.011, .588)
        wrist = (sign * .123, -.022, .479)
        fingers = (sign * .124, -.018, .413)
        bone('Arm' + side, shoulder, elbow, 'Chest')
        bone('Forearm' + side, elbow, wrist, 'Arm' + side)
        bone('Hand' + side, wrist, fingers, 'Forearm' + side)

    bpy.ops.object.mode_set(mode='OBJECT')
    return rig


def skin(figure, rig, scale):
    groups = {bone.name: figure.vertex_groups.new(name=bone.name) for bone in rig.data.bones}
    seam_sides = figure.data.attributes['leg_side'].data
    arm_seams = {
        '.L': figure.data.attributes['arm_l'].data,
        '.R': figure.data.attributes['arm_r'].data,
    }
    for vert in figure.data.vertices:
        x, y, z = vert.co
        ax = abs(x)
        sign = seam_sides[vert.index].value or (1 if x >= 0 else -1)
        side = '.L' if sign == 1 else '.R'

        # Let the upper skirt follow the pelvis, softening twists at its hem.
        skirt_hem = .466 + .027 * smooth(-.045, .027, y)
        leg = 1 - smooth(skirt_hem - .057, skirt_hem + .070, z)
        shin = 1 - smooth(.279, .329, z)
        foot = 1 - smooth(.064, .102, z)
        spine = smooth(.528, .612, z)
        chest = smooth(.595, .68, z)
        head = smooth(.774, .829, z)

        weights = {
            'Pelvis': (1 - leg) * (1 - spine),
            'Spine': (1 - leg) * spine * (1 - chest),
            'Chest': (1 - leg) * spine * chest * (1 - head),
            'Head': head,
        }

        # Blend both thighs only where the mesh actually joins at the top.
        center = smooth(-.036, .036, x)
        join = smooth(.390, .480, z)
        left = (1 - join) * (sign == 1) + join * center
        for suffix, fraction in (('.L', left), ('.R', 1 - left)):
            influence = leg * fraction
            weights['Thigh' + suffix] = influence * (1 - shin)
            weights['Shin' + suffix] = influence * shin * (1 - foot)
            weights['Foot' + suffix] = influence * shin * foot

        arm_inner = (.082 + min(.028, max(0., .68 - z) * .14)
                     - .018 * (1 - smooth(.429, .472, z)))
        arm = (smooth(arm_inner - .008, arm_inner + .007, ax)
               * smooth(-.060, -.042, y) * (1 - smooth(.027, .052, y))
               * smooth(.386, .398, z) * (1 - smooth(.744, .787, z)))
        if z < .525:
            arm_side = arm_seams[side][vert.index].value
            if not arm_side:
                arm_side = 1 if ax > .092 + (z - .415) * .18 else -1
            hand_region = smooth(.385, .399, z) * (1 - smooth(.492, .525, z))
            arm = arm * (1 - hand_region) + (arm_side == 1) * hand_region
        elbow = 1 - smooth(.564, .614, z)
        wrist = 1 - smooth(.455, .493, z)
        hair = (smooth(.021, .068, y) * smooth(.583, .622, z)
                * (1 - smooth(.797, .867, z)))
        for key in weights:
            weights[key] *= (1 - arm) * (1 - hair)
        weights.update({
            'Hair': hair * (1 - arm),
            'Arm' + side: arm * (1 - elbow),
            'Forearm' + side: arm * elbow * (1 - wrist),
            'Hand' + side: arm * elbow * wrist,
        })

        strongest = sorted(((weight, name) for name, weight in weights.items()
                            if weight > .00001), reverse=True)[:4]
        total = sum(weight for weight, _ in strongest)
        for weight, name in strongest:
            groups[name].add([vert.index], weight / total, 'REPLACE')

    figure.parent = rig
    modifier = figure.modifiers.new('V4 skin', 'ARMATURE')
    modifier.object = rig
    rig.scale = (scale,) * 3


def foot_path(phase, scale):
    """Move back at the actor's exact forward speed during ground contact."""
    travel = STRIDE_METERS / scale
    stance = .60
    if phase < stance:
        return travel * (stance / 2 - phase), 0., 0.

    t = (phase - stance) / (1 - stance)
    lift = math.sin(math.pi * t) ** 2
    distance = travel * (smooth(0, 1, t) - (1 - stance) * t - stance / 2)
    pitch = .16 * math.sin(math.tau * t) * math.sin(math.pi * t)
    return distance, .047 * lift, pitch


def bake(rig, scale, idle):
    bones = rig.pose.bones
    rest = {bone.name: bone.bone.matrix_local.to_quaternion() for bone in bones}
    action = bpy.data.actions.new('Idle' if idle else 'Walk')
    rig.animation_data_create().action = action

    for frame in range(CYCLE_FRAMES + 1):
        phase = frame / CYCLE_FRAMES
        theta = math.tau * phase
        drop = .005 if idle else .030 + .006 * math.cos(2 * theta)
        pelvis_offset = Vector((0 if idle else .007 * math.sin(theta), 0, -drop))
        bones['Pelvis'].location = rest['Pelvis'].inverted() @ pelvis_offset
        bones['Pelvis'].keyframe_insert('location', frame=frame)
        world_pose = {'Pelvis': Quaternion()}

        def pose(name, rotation):
            bone = bones[name]
            bone.rotation_quaternion = (rest[name].inverted()
                                        @ world_pose[bone.parent.name].inverted()
                                        @ rotation @ rest[name])
            bone.keyframe_insert('rotation_quaternion', frame=frame)
            world_pose[name] = rotation

        twist = 0. if idle else .043 * math.cos(theta)
        breathe = .012 * math.cos(theta) if idle else -.018
        pose('Spine', Quaternion(UP, twist * .48) @ Quaternion((1, 0, 0), breathe * .5))
        pose('Chest', Quaternion(UP, twist) @ Quaternion((1, 0, 0), breathe))
        pose('Head', Quaternion(UP, twist * .22))
        pose('Hair', Quaternion(UP, twist * .72)
             @ Quaternion((0, 1, 0), 0. if idle else .018 * math.sin(theta - .6)))

        for sign, side in SIDES:
            offset, lift, pitch = (0., 0., 0.) if idle else foot_path(
                (phase + (0 if sign == 1 else .5)) % 1, scale)
            thigh = bones['Thigh' + side].bone
            shin = bones['Shin' + side].bone
            hip = thigh.head_local + pelvis_offset
            ankle = shin.tail_local + Vector((sign * .014, -offset, lift))
            axis = ankle - hip
            length = min(axis.length, thigh.length + shin.length - .0001)
            axis.normalize()
            bend = (FORWARD - axis * FORWARD.dot(axis)).normalized()
            along = (thigh.length**2 - shin.length**2 + length**2) / (2 * length)
            knee = hip + axis * along + bend * math.sqrt(max(0., thigh.length**2 - along**2))
            pose('Thigh' + side, (thigh.tail_local - thigh.head_local).rotation_difference(knee - hip))
            pose('Shin' + side, (shin.tail_local - shin.head_local).rotation_difference(hip + axis * length - knee))
            pose('Foot' + side, Quaternion((1, 0, 0), pitch))

            swing = 0. if idle else sign * .14 * math.cos(theta - .25)
            relaxed = Quaternion((0, 1, 0), -sign * .070)
            pose('Arm' + side, Quaternion((1, 0, 0), swing) @ relaxed)
            hand = Quaternion((1, 0, 0), swing - .07) @ relaxed
            pose('Forearm' + side, hand)
            pose('Hand' + side, hand)

    track = rig.animation_data.nla_tracks.new()
    track.name = action.name
    track.strips.new(action.name, 0, action)
    rig.animation_data.action = None
    track.mute = True


def main():
    with TemporaryDirectory() as texture_dir:
        figure, scale = import_figure(texture_dir)
        rig = make_rig()
        skin(figure, rig, scale)
        scene = bpy.context.scene
        scene.render.fps = FPS
        scene.frame_start, scene.frame_end = 0, CYCLE_FRAMES
        bake(rig, scale, idle=False)
        bake(rig, scale, idle=True)
        for bone in rig.pose.bones:
            bone.matrix_basis = Matrix.Identity(4)
        bpy.context.view_layer.update()
        bpy.ops.export_scene.gltf(filepath=str(OUTPUT), export_format='GLB',
                                  export_animation_mode='ACTIONS', export_rest_position_armature=True,
                                  export_image_format='JPEG', export_jpeg_quality=92, export_current_frame=False)
        print(f'{OUTPUT.name}: {len(figure.data.polygons)} triangles, stride {STRIDE_METERS:.4f} m')


if __name__ == '__main__':
    main()
