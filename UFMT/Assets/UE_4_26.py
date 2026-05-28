import os
import unreal
import json
import sys
from pathlib import Path

json_path = os.environ.get("UFMT_JSON_PATH")

if not json_path or not os.path.exists(json_path):
    unreal.log_error("Critical Error: JSON data path was not found in environment variables.")
    sys.exit(1)

# Because we used memory, Python doesn't care about slashes or spaces!
with open(json_path, 'r', encoding='utf-8') as f:
    data = json.load(f)

fbx_paths     = data.get("FbxPaths")
material_names = data.get("Materials")
diffuse_textures  = data.get("DiffuseTextures")
mask_textures  = data.get("MaskTextures")
normal_textures  = data.get("NormalTextures")
specular_textures  = data.get("SpecularTextures")
code_name = data.get("CodeName")
asset_names    = data.get("MeshNames")
icon_textures = data.get("IconTextures")
cid = data.get("CID")

fbx_destination_path = f"/Game/CustomSkins/{code_name}/Meshes"
tex_destination_path = f"/Game/CustomSkins/{code_name}/Textures"
mi_destination_path = f"/Game/CustomSkins/{code_name}/Materials"
EXISTING_SKELETON_PATH = None

def delete_directory_if_exists(path):
    if unreal.EditorAssetLibrary.does_directory_exist(path):
        unreal.EditorAssetLibrary.delete_directory(path)
        unreal.log(f"Deleted existing directory: {path}")

delete_directory_if_exists(fbx_destination_path)
delete_directory_if_exists(tex_destination_path)
delete_directory_if_exists(mi_destination_path)

def import_psk(fbx_path, asset_name):
    skel_data = unreal.FbxSkeletalMeshImportData()
    skel_data.set_editor_property("import_content_type", unreal.FBXImportContentType.FBXICT_ALL)
    skel_data.set_editor_property("import_translation",  unreal.Vector(0.0, 0.0, 0.0))
    skel_data.set_editor_property("import_rotation",     unreal.Rotator(0.0, 0.0, 0.0))
    skel_data.set_editor_property("import_uniform_scale", 1.0)
    skel_data.set_editor_property("convert_scene",       True)
    skel_data.set_editor_property("force_front_x_axis",  False)
    skel_data.set_editor_property("convert_scene_unit",  False)

    ui = unreal.FbxImportUI()
    ui.import_as_skeletal  = True
    ui.import_mesh         = True
    ui.import_animations   = False
    ui.import_materials    = False
    ui.import_textures     = True
    ui.create_physics_asset = False
    ui.mesh_type_to_import = unreal.FBXImportType.FBXIT_SKELETAL_MESH
    ui.skeletal_mesh_import_data = skel_data

    if None:
        sk = unreal.load_asset(None)
        if sk:
            ui.skeleton = sk

    task                  = unreal.AssetImportTask()
    task.filename         = fbx_path
    task.destination_path = fbx_destination_path
    task.destination_name = asset_name
    task.replace_existing = True
    task.automated        = True
    task.save             = True
    task.options          = ui

    unreal.AssetToolsHelpers.get_asset_tools().import_asset_tasks([task])

    if task.imported_object_paths:
        for path in task.imported_object_paths:
            unreal.log(f"SUCCESS => {path}")
    else:
        unreal.log_error("FAILED — no assets imported.")

def create_material_instance(mi_name):
    factory = unreal.MaterialInstanceConstantFactoryNew()

    asset_tools = unreal.AssetToolsHelpers.get_asset_tools()
    material_instance = asset_tools.create_asset(
        mi_name,
        mi_destination_path,
        unreal.MaterialInstanceConstant,
        factory
    )

    if material_instance:
        unreal.EditorAssetLibrary.save_loaded_asset(material_instance)
        unreal.log(f"SUCCESS => {mi_destination_path}/{mi_name}")
    else:
        unreal.log_error(f"FAILED — could not create material instance: {mi_name}")

    return material_instance

def apply_materials_to_mesh(asset_name):
    mesh_path = f"{fbx_destination_path}/{asset_name}"
    mesh = unreal.load_asset(mesh_path)

    if not mesh:
        unreal.log_error(f"FAILED — could not load mesh: {mesh_path}")
        return

    materials = mesh.materials
    for i, skeletal_material in enumerate(materials):
        slot_name = str(skeletal_material.material_slot_name)
        mi_path = f"{mi_destination_path}/{slot_name}"
        mi = unreal.load_asset(mi_path)
        if mi:
            skeletal_material.material_interface = mi
            materials[i] = skeletal_material
            unreal.log(f"Applied {slot_name} => {asset_name}")
        else:
            unreal.log_error(f"Material not found: {mi_path}")

    mesh.materials = materials
    unreal.EditorAssetLibrary.save_loaded_asset(mesh)

def create_anim_blueprint(anim_bp_name, skeleton_asset_name):
    template_path = "/Game/AnimBP_Template"
    new_path = f"{fbx_destination_path}/{anim_bp_name}"

    if unreal.EditorAssetLibrary.does_asset_exist(new_path):
        unreal.EditorAssetLibrary.delete_asset(new_path)

    success = unreal.EditorAssetLibrary.duplicate_asset(template_path, new_path)

    if not success:
        unreal.log_error(f"FAILED — could not duplicate anim blueprint: {anim_bp_name}")
        return

    anim_bp = unreal.load_asset(new_path)

    if not anim_bp:
        unreal.log_error(f"FAILED — could not load duplicated anim blueprint: {new_path}")
        return

    skeleton_path = f"{fbx_destination_path}/{skeleton_asset_name}"
    skeleton = unreal.load_asset(skeleton_path)

    if not skeleton:
        unreal.log_error(f"FAILED — could not load skeleton: {skeleton_path}")
        return

    anim_bp.set_editor_property("target_skeleton", skeleton)
    unreal.EditorAssetLibrary.save_loaded_asset(anim_bp)
    unreal.log(f"SUCCESS => {new_path}")

def import_texture(texture_path, texture_type):
    task                  = unreal.AssetImportTask()
    task.filename         = texture_path
    task.destination_path = tex_destination_path
    task.destination_name = Path(texture_path).stem
    task.replace_existing = True
    task.automated        = True
    task.save             = False  # don't save yet
    task.save             = False  # don't save yet

    unreal.AssetToolsHelpers.get_asset_tools().import_asset_tasks([task])

    if task.imported_object_paths:
        asset_path = task.imported_object_paths[0]
        texture = unreal.load_asset(asset_path)
        if texture_type == "diffuse":
            texture.lod_group = unreal.TextureGroup.TEXTUREGROUP_CHARACTER
        elif texture_type == "specular":
            texture.compression_settings = unreal.TextureCompressionSettings.TC_MASKS
            texture.lod_group = unreal.TextureGroup.TEXTUREGROUP_CHARACTER_SPECULAR
            texture.srgb = False
        elif texture_type == "normal":
            texture.lod_group = unreal.TextureGroup.TEXTUREGROUP_CHARACTER_NORMAL_MAP
        elif texture_type == "icon":
            texture.lod_group = unreal.TextureGroup.TEXTUREGROUP_UI
        unreal.EditorAssetLibrary.save_loaded_asset(texture)
        unreal.log(f"SUCCESS => {asset_path}")
    else:
        unreal.log_error(f"FAILED => {texture_path}")

def create_fake_cid():
    template_path = "/Game/CID_Template"
    new_path = f"/Game/CustomSkins/{code_name}/{cid}"

    existing_assets = unreal.EditorAssetLibrary.list_assets(f"/Game/CustomSkins/{code_name}", recursive=False)
    for asset_path in existing_assets:
        asset = unreal.load_asset(asset_path)
        if isinstance(asset, unreal.Blueprint):
            unreal.EditorAssetLibrary.delete_asset(asset_path)
            unreal.log(f"Deleted existing blueprint: {asset_path}")

    success = unreal.EditorAssetLibrary.duplicate_asset(template_path, new_path)

    if success:
        da = unreal.load_asset(new_path)
        unreal.EditorAssetLibrary.save_loaded_asset(da)
        unreal.log(f"SUCCESS => {new_path}")
    else:
        unreal.log_error(f"FAILED — could not create fake cid: {cid}")

for i in range(len(material_names)):
    create_material_instance(material_names[i])

for i in range(len(fbx_paths)):
    import_psk(fbx_paths[i], asset_names[i])
    create_anim_blueprint(f"{asset_names[i]}_AnimBP", f"{asset_names[i]}_Skeleton")
    apply_materials_to_mesh(asset_names[i])

for i in range(len(diffuse_textures)):
    import_texture(diffuse_textures[i], "diffuse")

for i in range(len(mask_textures)):
    import_texture(mask_textures[i], "specular")

for i in range(len(normal_textures)):
    import_texture(normal_textures[i], "normal")

for i in range(len(specular_textures)):
    import_texture(specular_textures[i], "specular")

for i in range(len(icon_textures)):
    if icon_textures[i] != "":
        import_texture(icon_textures[i], "icon")

create_fake_cid()

unreal.EditorLoadingAndSavingUtils.save_dirty_packages(
    save_map_packages=False,
    save_content_packages=True
)