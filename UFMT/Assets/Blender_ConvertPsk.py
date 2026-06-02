import bpy
import sys
bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete()
psk_path = sys.argv[-2]
export_path = sys.argv[-1]

bpy.ops.psk.import_file(
    filepath=psk_path,
)

bpy.ops.export_scene.fbx(
    filepath=export_path,
    global_scale=0.01
)