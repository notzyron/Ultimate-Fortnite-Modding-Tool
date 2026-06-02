import bpy
import sys
import os

psa_path = sys.argv[-2]
export_path = sys.argv[-1]

armature = bpy.data.objects.get("Armature")

if armature:
    bpy.context.view_layer.objects.active = armature
    armature.select_set(True)

    bpy.ops.psa.import_file(filepath=psa_path)
    bpy.context.view_layer.update()

    if bpy.data.actions:
        if not armature.animation_data:
            armature.animation_data_create()

        action = bpy.data.actions[-1]
        armature.animation_data.action = action

        end_frame = int(action.frame_range[1])
        bpy.context.scene.frame_end = end_frame

        # Write to a temp file next to the export
        meta_path = export_path + ".meta"
        with open(meta_path, "w") as f:
            f.write(str(end_frame))

    bpy.ops.export_scene.fbx(
        filepath=export_path,
        global_scale=0.01,
        use_selection=False
    )
else:
    desktop = os.path.join(os.path.expanduser("~"), "Desktop")
    with open(os.path.join(desktop, "error.txt"), "w") as f:
        f.write("Error: Armature not found in the template .blend file!")