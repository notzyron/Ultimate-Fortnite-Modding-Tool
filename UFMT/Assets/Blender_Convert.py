import bpy
import sys
bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete()
psk_path = sys.argv[-2]
export_path = sys.argv[-1]

bpy.ops.psk.import_file(
    filepath=psk_path,
)

bpy.ops.better_export.fbx(filepath=export_path,
my_scale= 0.01, use_optimize_for_game_engine=False, use_edge_crease=False)