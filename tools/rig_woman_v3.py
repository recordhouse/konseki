"""Bake a planted, in-place walk for the front-facing figure of the supplied v3 scan.

Run with blender -b -t 4 --factory-startup --python-exit-code 1 -P tools/rig_woman_v3.py
"""
from pathlib import Path
import math

import bmesh
import bpy
from mathutils import Matrix, Quaternion, Vector


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'artifacts/new-woman-v3/anime girl 3d model.glb'
OUTPUT = ROOT / 'public/models/woman-v3-walk.glb'
HEIGHT = 1.68
FIGURE_HEIGHT = .773
SCALE = HEIGHT / FIGURE_HEIGHT
CYCLE = 26 / 25
WALK_SPEED = 1.12


def smooth(low, high, value):
    t = min(1, max(0, (value - low) / (high - low)))
    return t * t * (3 - 2 * t)


def isolate_figure():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(SOURCE), merge_vertices=True)
    figure = next(obj for obj in bpy.context.scene.objects if obj.type == 'MESH')
    bpy.context.view_layer.objects.active = figure
    decimate = figure.modifiers.new('Gameplay silhouette', 'DECIMATE')
    decimate.ratio = .083
    bpy.ops.object.modifier_apply(modifier=decimate.name)
    mesh = bmesh.new()
    mesh.from_mesh(figure.data)
    discard = []
    # Classify entire surface islands, including fingers that overlap in projection.
    for first in mesh.verts:
        if first.tag:
            continue
        first.tag = True
        island = [first]
        for vert in island:
            for edge in vert.link_edges:
                other = edge.other_vert(vert)
                if not other.tag:
                    other.tag = True
                    island.append(other)
        if sum(v.co.x for v in island) / len(island) > -.117:
            discard.extend(island)
    bmesh.ops.delete(mesh, geom=discard, context='VERTS')
    xmin, xmax = min(v.co.x for v in mesh.verts), max(v.co.x for v in mesh.verts)
    assert -.51 < xmin < -.47 and -.15 < xmax < -.09, (xmin, xmax)
    for v in mesh.verts:
        v.co.x += .307
    mesh.to_mesh(figure.data)
    mesh.free()
    figure.data.update()
    figure.name = 'Woman V3'
    return figure


def skeleton():
    data = bpy.data.armatures.new('Walking skeleton')
    rig = bpy.data.objects.new('Woman V3 Rig', data)
    bpy.context.collection.objects.link(rig)
    bpy.context.view_layer.objects.active = rig
    rig.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT')

    def add(name, head, tail, parent=None):
        bone = data.edit_bones.new(name)
        bone.head, bone.tail = head, tail
        if parent:
            bone.parent = data.edit_bones[parent]

    add('Pelvis', (0, 0, .405), (0, 0, .455))
    add('Spine', (0, 0, .455), (0, 0, .53), 'Pelvis')
    add('Chest', (0, 0, .53), (0, 0, .626), 'Spine')
    add('Head', (0, 0, .626), (0, 0, .767), 'Chest')
    for sign, side in [(1, '.L'), (-1, '.R')]:
        hip, knee, ankle = [(sign*.041, y, z) for y, z in [(0, .405), (-.006, .227), (.014, .053)]]
        add('Thigh'+side, hip, knee, 'Pelvis')
        add('Shin'+side, knee, ankle, 'Thigh'+side)
        add('Foot'+side, ankle, (sign*.041, -.065, .018), 'Shin'+side)
        shoulder, elbow, wrist, hand = [(sign*x, y, z) for x, y, z in
                                         [(.070, 0, .592), (.117, .002, .482),
                                          (.163, -.002, .393), (.175, -.004, .352)]]
        add('Arm'+side, shoulder, elbow, 'Chest')
        add('Forearm'+side, elbow, wrist, 'Arm'+side)
        add('Hand'+side, wrist, hand, 'Forearm'+side)
    bpy.ops.object.mode_set(mode='OBJECT')
    return rig


def skin(figure, rig):
    groups = {b.name: figure.vertex_groups.new(name=b.name) for b in rig.data.bones}
    for v in figure.data.vertices:
        x, y, z = v.co
        ax = abs(x)
        side = '.L' if x >= 0 else '.R'
        # Interpolate continuously on both sides of UV seams.
        foot = 1 - smooth(.042, .068, z)
        shin = 1 - smooth(.205, .250, z)
        leg = 1 - smooth(.345, .443, z)
        spine = smooth(.427, .50, z)
        chest = smooth(.495, .555, z)
        head = smooth(.618, .654, z)
        weights = {'Pelvis': (1-leg)*(1-spine), 'Spine': (1-leg)*spine*(1-chest),
                   'Chest': (1-leg)*spine*chest*(1-head), 'Head': head}
        # Across the continuous skirt hem, blend between legs; shoes remain rigid.
        width = .001 + .045*smooth(.30, .39, z)
        left = smooth(-width, width, x)
        for suffix, fraction in [('.L', left), ('.R', 1-left)]:
            influence = leg*fraction
            weights['Foot'+suffix] = influence*shin*foot
            weights['Shin'+suffix] = influence*shin*(1-foot)
            weights['Thigh'+suffix] = influence*(1-shin)
        arm_center = .07 + max(0, .592-z)*.46
        arm = (smooth(arm_center-.045, arm_center-.022, ax) *
               (1-smooth(.022, .05, abs(y))) * smooth(.32, .34, z) *
               (1-smooth(.578, .617, z)))
        elbow = 1-smooth(.463, .503, z)
        wrist = 1-smooth(.377, .405, z)
        weights = {key: value*(1-arm) for key, value in weights.items()}
        weights.update({'Arm'+side: arm*(1-elbow), 'Forearm'+side: arm*elbow*(1-wrist),
                        'Hand'+side: arm*elbow*wrist})
        strongest = sorted(((w, n) for n, w in weights.items() if w > .00001), reverse=True)[:4]
        total = sum(w for w, n in strongest)
        for w, name in strongest:
            groups[name].add([v.index], w/total, 'REPLACE')
    figure.parent = rig
    modifier = figure.modifiers.new('Walk deformation', 'ARMATURE')
    modifier.object = rig
    rig.scale = (SCALE,)*3


def foot_path(phase):
    stance = .56
    travel = WALK_SPEED*CYCLE/SCALE
    if phase < stance:
        return travel*(stance/2-phase), 0
    t = (phase-stance)/(1-stance)
    # Match the planted foot's backwards speed at toe-off and heel strike.
    advance = travel*(smooth(0, 1, t) - (1-stance)*t - stance/2)
    return advance, .037*math.sin(math.pi*t)**2


def bake(rig, idle=False):
    # Specify rotations in model space, then express each relative to its parent.
    bones = rig.pose.bones
    rest = {b.name: b.bone.matrix_local.to_quaternion() for b in bones}
    action = bpy.data.actions.new('Idle' if idle else 'Walk')
    rig.animation_data_create().action = action
    for i in range(27):
        frame, phase = i, i/26
        theta = math.tau*phase
        drop = .004 if idle else .028+.008*math.cos(2*theta)
        hips = bones['Pelvis']
        hips.location = rest['Pelvis'].inverted() @ Vector((0 if idle else .004*math.sin(theta), 0, -drop))
        hips.keyframe_insert('location', frame=frame)
        global_q = {'Pelvis': Quaternion()}

        def pose(name, q):
            b = bones[name]
            b.rotation_quaternion = rest[name].inverted() @ global_q[b.parent.name].inverted() @ q @ rest[name]
            b.keyframe_insert('rotation_quaternion', frame=frame)
            global_q[name] = q

        twist = 0 if idle else math.cos(theta)*.045
        pose('Spine', Quaternion((0, 0, 1), twist*.45))
        pose('Chest', Quaternion((0, 0, 1), twist))
        pose('Head', Quaternion((0, 0, 1), twist*.12))
        for sign, side in [(1, '.L'), (-1, '.R')]:
            offset, lift = (0, 0) if idle else foot_path((phase + (0 if sign==1 else .5))%1)
            forward, down = offset-.014, .405-drop-(.053+lift)
            a, b = bones['Thigh'+side].bone.length, bones['Shin'+side].bone.length
            d = min(a+b-.00001, math.hypot(forward, down))
            alpha = math.atan2(forward, down) + math.acos(max(-1,min(1,(a*a+d*d-b*b)/(2*a*d))))
            beta = alpha - math.acos(max(-1,min(1,(d*d-a*a-b*b)/(2*a*b))))
            pose('Thigh'+side, Quaternion((1,0,0), math.atan2(.006,.178)-alpha))
            pose('Shin'+side, Quaternion((1,0,0), math.atan2(-.020,.174)-beta))
            pose('Foot'+side, Quaternion((1,0,0), -lift*.9))
            swing = 0 if idle else sign*math.cos(theta)*.22
            lowered = Quaternion((0,1,0), sign*.255)
            pose('Arm'+side, Quaternion((1,0,0), swing) @ lowered)
            forearm = Quaternion((1,0,0), swing-.13) @ lowered
            pose('Forearm'+side, forearm)
            pose('Hand'+side, forearm)
    track = rig.animation_data.nla_tracks.new()
    track.name = action.name
    track.strips.new(action.name, 0, action)
    rig.animation_data.action = None
    track.mute = True


def main():
    figure = isolate_figure()
    rig = skeleton()
    skin(figure, rig)
    scene = bpy.context.scene
    scene.render.fps = 25
    scene.frame_start, scene.frame_end = 0, 26
    bake(rig)
    bake(rig, idle=True)
    for bone in rig.pose.bones:
        bone.matrix_basis = Matrix.Identity(4)
    bpy.context.view_layer.update()
    bpy.ops.export_scene.gltf(filepath=str(OUTPUT), export_format='GLB',
                              export_animation_mode='ACTIONS', export_rest_position_armature=True,
                              export_current_frame=False)
    print(f'{OUTPUT.name} / {len(figure.data.polygons)} triangles')


if __name__ == '__main__':
    main()
