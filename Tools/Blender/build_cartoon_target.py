"""Build Konseki's free, reference-inspired cartoon target character.

Run with:
  /Applications/Blender.app/Contents/MacOS/Blender --background \
    --python Tools/Blender/build_cartoon_target.py

The script intentionally uses only Blender primitives and Python.  It creates a
smooth stylised adult character, a real armature, rigid skin weights and an FBX
that Unity can import without any paid asset or generative-model credit.
"""

from __future__ import annotations

import math
from pathlib import Path

import bpy
from mathutils import Vector


PROJECT_ROOT = Path(__file__).resolve().parents[2]
OUTPUT_DIR = PROJECT_ROOT / "Assets" / "Konseki" / "Models" / "CartoonTarget"
SOURCE_DIR = PROJECT_ROOT / "Tools" / "Blender" / "Generated"
BLEND_PATH = SOURCE_DIR / "CartoonTargetSource.blend"
FBX_PATH = OUTPUT_DIR / "CartoonTarget.fbx"


def reset_scene() -> None:
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for datablocks in (bpy.data.meshes, bpy.data.curves, bpy.data.armatures, bpy.data.materials):
        for block in list(datablocks):
            if block.users == 0:
                datablocks.remove(block)


def material(name: str, rgba: tuple[float, float, float, float], roughness: float = 0.7):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = rgba
    mat.use_nodes = True
    principled = mat.node_tree.nodes.get("Principled BSDF")
    if principled:
        principled.inputs["Base Color"].default_value = rgba
        principled.inputs["Roughness"].default_value = roughness
        principled.inputs["Specular IOR Level"].default_value = 0.28
    return mat


def apply_transform(obj) -> None:
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.select_set(False)


def finish_mesh(obj, mat, bevel: float = 0.0) -> None:
    if mat is not None:
        obj.data.materials.append(mat)
    for polygon in obj.data.polygons:
        polygon.use_smooth = True
    if bevel > 0.0:
        modifier = obj.modifiers.new("Soft cartoon edges", "BEVEL")
        modifier.width = bevel
        modifier.segments = 2
        modifier.limit_method = "ANGLE"
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)
        bpy.ops.object.modifier_apply(modifier=modifier.name)
        obj.select_set(False)


def add_uv_sphere(name, location, scale, mat, segments=32, rings=20):
    bpy.ops.mesh.primitive_uv_sphere_add(
        segments=segments,
        ring_count=rings,
        location=location,
    )
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    apply_transform(obj)
    finish_mesh(obj, mat)
    return obj


def add_cube(name, location, dimensions, mat, rotation=(0.0, 0.0, 0.0), bevel=0.015):
    bpy.ops.mesh.primitive_cube_add(location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = dimensions
    apply_transform(obj)
    finish_mesh(obj, mat, bevel)
    return obj


def add_between(name, start, end, radius_x, radius_y, mat, end_roundness=1.0):
    """Create a rounded, smooth limb/lock along local Z."""
    start_v = Vector(start)
    end_v = Vector(end)
    direction = end_v - start_v
    obj = add_uv_sphere(
        name,
        (start_v + end_v) * 0.5,
        (radius_x, radius_y, direction.length * 0.5 * end_roundness),
        mat,
        segments=28,
        rings=18,
    )
    obj.rotation_mode = "QUATERNION"
    obj.rotation_quaternion = direction.to_track_quat("Z", "Y")
    obj.rotation_mode = "XYZ"
    return obj


def ring_mesh(name, rings, segments, mat, pleat=0.0, phase=0.0, cap=True):
    """Create a smooth closed surface from (z, radius_x, radius_y, center_y) rings."""
    vertices = []
    faces = []
    for ring_index, (z, radius_x, radius_y, center_y) in enumerate(rings):
        for index in range(segments):
            angle = (index / segments) * math.tau + phase
            wave = 1.0
            if pleat:
                wave += pleat * math.cos(angle * (segments // 2))
            vertices.append((
                math.sin(angle) * radius_x * wave,
                center_y + math.cos(angle) * radius_y * wave,
                z,
            ))
    for ring_index in range(len(rings) - 1):
        base = ring_index * segments
        next_base = (ring_index + 1) * segments
        for index in range(segments):
            nxt = (index + 1) % segments
            faces.append((base + index, next_base + index, next_base + nxt, base + nxt))
    if cap:
        bottom_center = len(vertices)
        bottom = rings[0]
        vertices.append((0.0, bottom[3], bottom[0]))
        top_center = len(vertices)
        top = rings[-1]
        vertices.append((0.0, top[3], top[0]))
        top_base = (len(rings) - 1) * segments
        for index in range(segments):
            nxt = (index + 1) % segments
            faces.append((bottom_center, base_index(0, index, segments), base_index(0, nxt, segments)))
            faces.append((top_center, top_base + nxt, top_base + index))
    mesh = bpy.data.meshes.new(name + "Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    finish_mesh(obj, mat, bevel=0.004)
    return obj


def base_index(ring, index, segments):
    return ring * segments + index


def create_armature():
    armature_data = bpy.data.armatures.new("CartoonTargetRig")
    armature = bpy.data.objects.new("CartoonTargetRig", armature_data)
    bpy.context.collection.objects.link(armature)
    bpy.context.view_layer.objects.active = armature
    armature.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")

    def bone(name, head, tail, parent=None):
        edit_bone = armature_data.edit_bones.new(name)
        edit_bone.head = head
        edit_bone.tail = tail
        edit_bone.use_deform = True
        if parent:
            edit_bone.parent = armature_data.edit_bones[parent]
        return edit_bone

    bone("Root", (0, 0, 0.02), (0, 0, 0.18))
    bone("Hips", (0, 0, 0.94), (0, 0, 1.10), "Root")
    bone("Skirt", (0, 0, 1.09), (0, 0, 1.19), "Hips")
    bone("Spine", (0, 0, 1.05), (0, 0, 1.30), "Hips")
    bone("Chest", (0, 0, 1.25), (0, 0, 1.50), "Spine")
    bone("Neck", (0, 0, 1.48), (0, 0, 1.58), "Chest")
    bone("Head", (0, 0, 1.57), (0, 0, 1.85), "Neck")
    bone("Hair", (0, 0.09, 1.76), (0, 0.09, 1.86), "Head")

    for side, x in (("Left", -0.29), ("Right", 0.29)):
        bone(side + "UpperArm", (x, 0, 1.44), (x, 0, 1.54), "Chest")
        bone(side + "LowerArm", (x * 1.06, 0, 1.17), (x * 1.06, 0, 1.27), side + "UpperArm")
        bone(side + "Hand", (x * 1.08, -0.01, 0.92), (x * 1.08, -0.01, 1.02), side + "LowerArm")
        leg_x = -0.135 if side == "Left" else 0.135
        bone(side + "UpperLeg", (leg_x, 0, 0.98), (leg_x, 0, 1.08), "Hips")
        bone(side + "LowerLeg", (leg_x, 0, 0.54), (leg_x, 0, 0.64), side + "UpperLeg")
        bone(side + "Foot", (leg_x, 0, 0.14), (leg_x, 0, 0.24), side + "LowerLeg")

    bpy.ops.object.mode_set(mode="OBJECT")
    armature.show_in_front = True
    armature.select_set(False)
    return armature


def skin_to_bone(obj, armature, bone_name):
    group = obj.vertex_groups.new(name=bone_name)
    group.add(list(range(len(obj.data.vertices))), 1.0, "REPLACE")
    modifier = obj.modifiers.new("Cartoon armature", "ARMATURE")
    modifier.object = armature
    obj.parent = armature
    obj.matrix_parent_inverse = armature.matrix_world.inverted()


def combine_compatible_meshes(objects):
    """Merge details that share one material and one rigid skin bone.

    This preserves the silhouette and animation while substantially reducing
    Unity renderer/draw-call overhead for the single moving target.
    """
    groups = {}
    for obj in objects:
        bone_name = obj.vertex_groups[0].name if obj.vertex_groups else "Unweighted"
        material_name = obj.data.materials[0].name if obj.data.materials else "NoMaterial"
        groups.setdefault((bone_name, material_name), []).append(obj)

    combined = []
    for (bone_name, material_name), group in groups.items():
        if len(group) == 1:
            combined.append(group[0])
            continue
        bpy.ops.object.select_all(action="DESELECT")
        active = group[0]
        for obj in group:
            obj.select_set(True)
        bpy.context.view_layer.objects.active = active
        bpy.ops.object.join()
        active.name = f"{bone_name}_{material_name}"
        active.data.name = active.name + "Mesh"
        combined.append(active)
    return combined


def build_character():
    skin = material("Skin", (0.92, 0.67, 0.60, 1.0), 0.76)
    skin_blush = material("Skin Blush", (0.93, 0.43, 0.48, 1.0), 0.72)
    hair = material("Hair", (0.105, 0.055, 0.075, 1.0), 0.48)
    blouse = material("Blouse", (0.93, 0.92, 0.94, 1.0), 0.82)
    navy = material("Navy", (0.055, 0.085, 0.22, 1.0), 0.64)
    shoe = material("Shoe", (0.025, 0.018, 0.03, 1.0), 0.32)
    eye_white = material("Eye White", (0.98, 0.98, 0.96, 1.0), 0.55)
    eye_dark = material("Eye Dark", (0.025, 0.012, 0.025, 1.0), 0.38)
    iris = material("Iris Brown", (0.31, 0.12, 0.09, 1.0), 0.40)
    lip = material("Lip", (0.58, 0.15, 0.21, 1.0), 0.62)

    armature = create_armature()
    objects = []

    def add(obj, bone):
        skin_to_bone(obj, armature, bone)
        objects.append(obj)
        return obj

    # Smooth, moderately stylised adult body under the outfit.
    add(add_uv_sphere("Hips_Skin", (0, 0.015, 1.00), (0.265, 0.185, 0.185), skin), "Hips")
    add(ring_mesh("Blouse_Tailored", [
        (1.06, 0.205, 0.135, 0.015),
        (1.17, 0.215, 0.145, 0.005),
        (1.31, 0.265, 0.185, -0.018),
        (1.39, 0.285, 0.195, -0.025),
        (1.48, 0.255, 0.155, -0.005),
    ], 40, blouse), "Chest")

    skirt = ring_mesh("Skirt_Pleated", [
        (0.765, 0.355, 0.255, 0.025),
        (1.055, 0.275, 0.195, 0.012),
        (1.105, 0.238, 0.168, 0.008),
    ], 32, navy, pleat=0.09, phase=math.pi / 32)
    for polygon in skirt.data.polygons:
        polygon.use_smooth = False
    add(skirt, "Skirt")
    add(add_cube("Skirt_Waistband", (0, 0.01, 1.095), (0.49, 0.34, 0.055), navy, bevel=0.02), "Skirt")

    # Sailor-inspired collar and restrained ribbon details.
    add(add_cube("Collar_Back", (0, 0.125, 1.405), (0.43, 0.055, 0.18), navy, bevel=0.018), "Chest")
    add(add_cube("Collar_Left", (-0.09, -0.16, 1.385), (0.22, 0.032, 0.035), navy,
                 rotation=(0, 0, math.radians(-38)), bevel=0.012), "Chest")
    add(add_cube("Collar_Right", (0.09, -0.16, 1.385), (0.22, 0.032, 0.035), navy,
                 rotation=(0, 0, math.radians(38)), bevel=0.012), "Chest")
    ribbon_left = add_uv_sphere("Ribbon_Left", (-0.065, -0.202, 1.315), (0.090, 0.025, 0.038), navy, 24, 14)
    ribbon_left.rotation_euler.z = math.radians(-16)
    add(ribbon_left, "Chest")
    ribbon_right = add_uv_sphere("Ribbon_Right", (0.065, -0.202, 1.315), (0.090, 0.025, 0.038), navy, 24, 14)
    ribbon_right.rotation_euler.z = math.radians(16)
    add(ribbon_right, "Chest")
    add(add_uv_sphere("Ribbon_Knot", (0, -0.226, 1.33), (0.035, 0.026, 0.035), navy, 24, 14), "Chest")

    # Arms, short sleeves and hands; overlap at the joints to avoid puppet gaps.
    for side, x in (("Left", -0.29), ("Right", 0.29)):
        upper_bone = side + "UpperArm"
        lower_bone = side + "LowerArm"
        hand_bone = side + "Hand"
        add(add_between(side + "_Sleeve", (x, 0, 1.44), (x * 1.035, 0, 1.31), 0.075, 0.082, blouse), upper_bone)
        add(add_between(side + "_SleeveTrim", (x * 1.035, 0, 1.325), (x * 1.05, 0, 1.295), 0.080, 0.087, navy), upper_bone)
        add(add_between(side + "_UpperArmSkin", (x * 1.045, 0, 1.31), (x * 1.06, 0, 1.16), 0.064, 0.068, skin), upper_bone)
        add(add_uv_sphere(side + "_Elbow", (x * 1.06, 0, 1.165), (0.068, 0.071, 0.078), skin, 28, 18), upper_bone)
        add(add_between(side + "_ForearmSkin", (x * 1.06, 0, 1.18), (x * 1.08, -0.01, 0.91), 0.060, 0.063, skin), lower_bone)
        add(add_uv_sphere(side + "_Hand", (x * 1.08, -0.018, 0.86), (0.060, 0.047, 0.105), skin, 28, 18), hand_bone)

    # Legs, knee socks and loafers.
    for side, x in (("Left", -0.135), ("Right", 0.135)):
        upper = side + "UpperLeg"
        lower = side + "LowerLeg"
        foot = side + "Foot"
        add(add_between(side + "_Thigh", (x, 0.01, 0.91), (x, 0.005, 0.535), 0.118, 0.125, skin), upper)
        add(add_uv_sphere(side + "_Knee", (x, -0.005, 0.535), (0.105, 0.112, 0.108), skin, 30, 18), lower)
        add(add_between(side + "_LowerLegSkin", (x, 0, 0.54), (x, 0, 0.40), 0.093, 0.10, skin), lower)
        add(add_between(side + "_NavySock", (x, 0.008, 0.43), (x, 0.005, 0.12), 0.088, 0.094, navy), lower)
        add(add_uv_sphere(side + "_SockTop", (x, 0.004, 0.425), (0.098, 0.104, 0.023), navy, 28, 14), lower)
        add(add_uv_sphere(side + "_Loafer", (x, -0.065, 0.085), (0.102, 0.185, 0.072), shoe, 32, 18), foot)
        add(add_cube(side + "_Sole", (x, -0.06, 0.025), (0.21, 0.36, 0.04), shoe, bevel=0.018), foot)
        add(add_cube(side + "_LoaferStrap", (x, -0.10, 0.123), (0.19, 0.06, 0.022), shoe, bevel=0.012), foot)

    # Neck, anime-style face and small features.
    add(add_uv_sphere("Neck_Skin", (0, 0, 1.535), (0.09, 0.085, 0.12), skin, 30, 18), "Neck")
    add(add_uv_sphere("Hair_Cap", (0, 0.015, 1.69), (0.225, 0.202, 0.235), hair, 40, 24), "Head")
    add(add_uv_sphere("Face_Skin", (0, -0.045, 1.67), (0.195, 0.178, 0.205), skin, 40, 24), "Head")
    add(add_uv_sphere("Left_Ear", (-0.195, -0.015, 1.67), (0.032, 0.024, 0.055), skin, 24, 14), "Head")
    add(add_uv_sphere("Right_Ear", (0.195, -0.015, 1.67), (0.032, 0.024, 0.055), skin, 24, 14), "Head")

    for side, x, tilt in (("Left", -0.077, -5.0), ("Right", 0.077, 5.0)):
        add(add_uv_sphere(side + "_EyeWhite", (x, -0.224, 1.695), (0.058, 0.012, 0.027), eye_white, 28, 14), "Head")
        add(add_uv_sphere(side + "_Iris", (x * 0.98, -0.237, 1.692), (0.023, 0.009, 0.023), iris, 24, 14), "Head")
        add(add_uv_sphere(side + "_Pupil", (x * 0.98, -0.244, 1.692), (0.009, 0.006, 0.013), eye_dark, 20, 12), "Head")
        add(add_uv_sphere(side + "_EyeHighlight", (x * 0.98 - 0.007, -0.249, 1.703), (0.006, 0.004, 0.007), eye_white, 16, 10), "Head")
        add(add_cube(side + "_Eyebrow", (x, -0.225, 1.765), (0.09, 0.018, 0.014), hair,
                     rotation=(0, math.radians(tilt), math.radians(-tilt)), bevel=0.006), "Head")
        add(add_uv_sphere(side + "_Blush", (x * 1.35, -0.222, 1.62), (0.038, 0.007, 0.014), skin_blush, 20, 10), "Head")

    add(add_uv_sphere("Nose", (0, -0.234, 1.64), (0.022, 0.019, 0.034), skin, 22, 12), "Head")
    add(add_uv_sphere("Mouth", (0.01, -0.236, 1.575), (0.052, 0.009, 0.011), lip, 24, 10), "Head")

    # Layered bangs frame the face rather than covering it with a solid helmet.
    bang_specs = [
        ("Bang_L3", (-0.19, -0.165, 1.84), (-0.15, -0.205, 1.67), 0.046),
        ("Bang_L2", (-0.12, -0.185, 1.88), (-0.10, -0.218, 1.70), 0.043),
        ("Bang_L1", (-0.045, -0.195, 1.89), (-0.03, -0.222, 1.73), 0.040),
        ("Bang_R1", (0.04, -0.195, 1.89), (0.025, -0.222, 1.73), 0.040),
        ("Bang_R2", (0.12, -0.18, 1.87), (0.105, -0.21, 1.69), 0.043),
        ("Bang_R3", (0.19, -0.15, 1.82), (0.17, -0.195, 1.64), 0.046),
    ]
    for name, start, end, width in bang_specs:
        add(add_between(name, start, end, width, width * 0.62, hair), "Head")

    # Broad back layer plus side locks create the long dark-brown silhouette.
    add(ring_mesh("Long_Hair_Back", [
        (0.86, 0.07, 0.05, 0.15),
        (0.92, 0.20, 0.085, 0.15),
        (1.04, 0.255, 0.095, 0.16),
        (1.35, 0.285, 0.105, 0.15),
        (1.65, 0.245, 0.115, 0.11),
        (1.84, 0.20, 0.12, 0.065),
    ], 36, hair), "Hair")
    add(add_between("Long_Hair_Left", (-0.205, 0.035, 1.78), (-0.255, 0.08, 0.98), 0.085, 0.065, hair), "Hair")
    add(add_between("Long_Hair_Right", (0.205, 0.035, 1.78), (0.255, 0.08, 0.98), 0.085, 0.065, hair), "Hair")
    add(add_between("Face_Lock_Left", (-0.205, -0.11, 1.76), (-0.225, -0.07, 1.17), 0.052, 0.042, hair), "Hair")
    add(add_between("Face_Lock_Right", (0.205, -0.11, 1.76), (0.225, -0.07, 1.17), 0.052, 0.042, hair), "Hair")

    # Lightweight render settings and metadata help Unity and future edits.
    armature["konseki_character"] = "Reference-inspired adult cartoon target"
    armature["license"] = "Project-created with Blender primitives; no third-party model"
    return armature, objects


def save_and_export(armature, objects) -> None:
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    SOURCE_DIR.mkdir(parents=True, exist_ok=True)
    bpy.context.preferences.filepaths.save_version = 0
    scene = bpy.context.scene
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0
    scene.render.engine = "BLENDER_EEVEE"

    bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_PATH))

    bpy.ops.object.select_all(action="DESELECT")
    armature.select_set(True)
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = armature
    bpy.ops.export_scene.fbx(
        filepath=str(FBX_PATH),
        use_selection=True,
        object_types={"ARMATURE", "MESH"},
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_UNITS",
        use_space_transform=True,
        axis_forward="-Z",
        axis_up="Y",
        add_leaf_bones=False,
        use_armature_deform_only=True,
        bake_anim=False,
        mesh_smooth_type="FACE",
        path_mode="AUTO",
    )
    total_vertices = sum(len(obj.data.vertices) for obj in objects)
    print(
        "[Konseki] BLENDER_CARTOON_TARGET_READY "
        f"meshes={len(objects)} vertices={total_vertices} fbx={FBX_PATH}"
    )


def main() -> None:
    reset_scene()
    armature, objects = build_character()
    objects = combine_compatible_meshes(objects)
    save_and_export(armature, objects)


if __name__ == "__main__":
    main()
