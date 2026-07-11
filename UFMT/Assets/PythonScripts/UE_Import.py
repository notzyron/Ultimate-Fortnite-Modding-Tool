import unreal; unreal.AssetRegistryHelpers.get_asset_registry().scan_paths_synchronous(['/Game'])
import os
import unreal
import json
import sys

json_path = os.environ.get("UFMT_JSON_PATH")

if not json_path or not os.path.exists(json_path):
    unreal.log_error("Critical Error: JSON data path was not found in environment variables.")
    sys.exit(1)

with open(json_path, 'r') as f:
    data = json.load(f)

fbx_paths             = data.get("FbxPaths")
material_names        = data.get("Materials")
diffuse_textures      = data.get("DiffuseTextures")
mask_textures         = data.get("MaskTextures")
normal_textures       = data.get("NormalTextures")
specular_textures     = data.get("SpecularTextures")
code_name             = data.get("CodeName")
asset_names           = data.get("MeshNames")
physics_mesh_names   = data.get("PhysicsMeshNames")
physics_asset_paths   = data.get("PhysicsAssetsPaths")
icon_textures         = data.get("IconTextures")
cid                   = data.get("CID")
lobby_animation_fbx_path = data.get("LobbyAnimationFbxPath")
lobby_animation_json_path = data.get("LobbyAnimationJsonPath")
retarget_source = data.get("RetargetSource")
head_mesh_name = data.get("HeadMeshName")
fn_version = data.get("CurrentFnVersion")

fbx_destination_path = "/Game/CustomSkins/{}/Meshes".format(code_name)
tex_destination_path = "/Game/CustomSkins/{}/Textures".format(code_name)
mi_destination_path  = "/Game/CustomSkins/{}/Materials".format(code_name)
EXISTING_SKELETON_PATH = None
unreal.EditorAssetLibrary.load_asset("/Game/CID_Template")


def delete_directory_if_exists(path):
    if unreal.EditorAssetLibrary.does_directory_exist(path):
        unreal.EditorAssetLibrary.delete_directory(path)
        unreal.log("Deleted existing directory: {}".format(path))


delete_directory_if_exists(fbx_destination_path)
delete_directory_if_exists(tex_destination_path)
delete_directory_if_exists(mi_destination_path)


def import_fbx(fbx_path, asset_name, use_base_head = False):
    skel_data = unreal.FbxSkeletalMeshImportData()
    skel_data.set_editor_property("import_content_type", unreal.FBXImportContentType.FBXICT_ALL)
    skel_data.set_editor_property("import_translation",  unreal.Vector(0.0, 0.0, 0.0))
    skel_data.set_editor_property("import_rotation",     unreal.Rotator(0.0, 0.0, 0.0))
    skel_data.set_editor_property("import_uniform_scale", 1.0)
    skel_data.set_editor_property("convert_scene",       True)
    skel_data.set_editor_property("force_front_x_axis",  False)
    skel_data.set_editor_property("convert_scene_unit",  False)
    skel_data.set_editor_property("import_morph_targets", use_base_head)

    ui = unreal.FbxImportUI()
    ui.import_as_skeletal   = True
    ui.import_mesh          = True
    ui.import_animations    = False
    ui.import_materials     = False
    ui.import_textures      = True
    ui.create_physics_asset = False
    ui.mesh_type_to_import  = unreal.FBXImportType.FBXIT_SKELETAL_MESH
    ui.skeletal_mesh_import_data = skel_data

    if use_base_head:
        target_skeleton_path = "/Game/Modding/Base_Head/Base_Head_Modding" if fn_version == "9.41" else "/Game/Base/Head/Skeleton/Base_Head_Skeleton"
        sk = unreal.load_asset(target_skeleton_path)
        if sk:
            ui.skeleton = sk
        else:
            unreal.log_error("Failed to load base head skeleton at: {}".format(target_skeleton_path))

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
            unreal.log("SUCCESS => {}".format(path))
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
        unreal.log("SUCCESS => {}/{}".format(mi_destination_path, mi_name))
    else:
        unreal.log_error("FAILED — could not create material instance: {}".format(mi_name))

    return material_instance


def apply_materials_to_mesh(asset_name):
    mesh_path = "{}/{}".format(fbx_destination_path, asset_name)
    mesh = unreal.load_asset(mesh_path)

    if not mesh:
        unreal.log_error("FAILED — could not load mesh: {}".format(mesh_path))
        return

    materials = mesh.materials
    for i, skeletal_material in enumerate(materials):
        slot_name = str(skeletal_material.material_slot_name)
        mi_path   = "{}/{}".format(mi_destination_path, slot_name)
        mi        = unreal.load_asset(mi_path)
        if mi:
            materials[i] = unreal.SkeletalMaterial(
                material_interface=mi,
                material_slot_name=skeletal_material.material_slot_name
            )
            unreal.log("Applied {} => {}".format(slot_name, asset_name))
        else:
            unreal.log_error("Material not found: {}".format(mi_path))

    mesh.set_editor_property("materials", materials)
    unreal.EditorAssetLibrary.save_loaded_asset(mesh)


def create_anim_blueprint(anim_bp_name, skeleton_asset_name):
    skeleton_path = "{}/{}".format(fbx_destination_path, skeleton_asset_name)
    skeleton = unreal.load_asset(skeleton_path)

    if not skeleton:
        unreal.log_error("FAILED — could not load skeleton: {}".format(skeleton_path))
        return None

    anim_bp = unreal.PhysicsImporter.create_anim_blueprint(
        fbx_destination_path,
        anim_bp_name,
        skeleton
    )

    if not anim_bp:
        unreal.log_error("FAILED — could not create anim blueprint: {}".format(anim_bp_name))
        return None

    unreal.log("SUCCESS => {}/{}".format(fbx_destination_path, anim_bp_name))
    return anim_bp


def import_texture(texture_path, texture_type):
    task                  = unreal.AssetImportTask()
    task.filename         = texture_path
    task.destination_path = tex_destination_path
    task.destination_name = os.path.splitext(os.path.basename(texture_path))[0]
    task.replace_existing = True
    task.automated        = True
    task.save             = False

    unreal.AssetToolsHelpers.get_asset_tools().import_asset_tasks([task])

    if task.imported_object_paths:
        asset_path = task.imported_object_paths[0]
        texture    = unreal.load_asset(asset_path)
        if texture_type == "diffuse":
            texture.lod_group = unreal.TextureGroup.TEXTUREGROUP_CHARACTER
            texture.compression_settings = unreal.TextureCompressionSettings.TC_DEFAULT
        elif texture_type == "specular":
            texture.compression_settings = unreal.TextureCompressionSettings.TC_MASKS
            texture.lod_group            = unreal.TextureGroup.TEXTUREGROUP_CHARACTER_SPECULAR
            texture.srgb                 = False
        elif texture_type == "normal":
            texture.lod_group = unreal.TextureGroup.TEXTUREGROUP_CHARACTER_NORMAL_MAP
        elif texture_type == "icon":
            texture.lod_group = unreal.TextureGroup.TEXTUREGROUP_UI
        unreal.EditorAssetLibrary.save_loaded_asset(texture)
        unreal.log("SUCCESS => {}".format(asset_path))
    else:
        unreal.log_error("FAILED => {}".format(texture_path))


def create_fake_cid():
    template_path = "/Game/CID_Template"
    new_path      = "/Game/CustomSkins/{}/{}".format(code_name, cid)

    if unreal.EditorAssetLibrary.does_asset_exist(new_path):
        unreal.EditorAssetLibrary.delete_asset(new_path)

    success = unreal.EditorAssetLibrary.duplicate_asset(template_path, new_path)

    if success:
        da = unreal.load_asset(new_path)
        unreal.EditorAssetLibrary.save_loaded_asset(da)
        unreal.log("SUCCESS => {}".format(new_path))
    else:
        unreal.log_error("FAILED — could not create fake cid: {}".format(cid))


def import_animation(fbx_path):
    skeleton_asset_path = "/Game/Characters/Player/Male/Male_Avg_Base/Fortnite_M_Avg_Player_Skeleton"
    skeleton = unreal.load_asset(skeleton_asset_path)
    if not skeleton:
        unreal.log_error("FAILED — could not load skeleton: {}".format(skeleton_asset_path))
        return

    anim_data = unreal.FbxAnimSequenceImportData()
    anim_data.set_editor_property("import_translation", unreal.Vector(0.0, 0.0, 0.0))
    anim_data.set_editor_property("import_rotation", unreal.Rotator(0.0, 0.0, 0.0))
    anim_data.set_editor_property("import_uniform_scale", 1.0)
    anim_data.set_editor_property("convert_scene", True)
    anim_data.set_editor_property("force_front_x_axis", False)
    anim_data.set_editor_property("convert_scene_unit", False)

    ui = unreal.FbxImportUI()

    ui = unreal.FbxImportUI()
    skel_data = unreal.FbxSkeletalMeshImportData()
    skel_data.set_editor_property("import_morph_targets", False)
    ui.skeletal_mesh_import_data = skel_data

    ui.mesh_type_to_import = unreal.FBXImportType.FBXIT_ANIMATION

    ui.automated_import_should_detect_type = False

    ui.import_mesh = False  # no mesh
    ui.import_animations = True  # animation track only
    ui.import_as_skeletal = False  # not a skeletal-mesh import path
    ui.import_materials = False
    ui.import_textures = False
    ui.create_physics_asset = False
    ui.skeleton = skeleton  # required, UE won't import without it
    ui.anim_sequence_import_data = anim_data  # animation transform/scale settings

    task = unreal.AssetImportTask()
    task.filename = fbx_path
    task.destination_path = "/Game/CustomSkins/{}/Animations".format(code_name)
    task.destination_name = "{}_Lobby_Animation".format(code_name)
    task.replace_existing = True
    task.automated = True
    task.save = True
    task.options = ui

    unreal.AssetToolsHelpers.get_asset_tools().import_asset_tasks([task])

    if task.imported_object_paths:
        for path in task.imported_object_paths:
            unreal.log("SUCCESS => {}".format(path))

            anim_sequence = unreal.load_asset(path)
            if anim_sequence and isinstance(anim_sequence, unreal.AnimSequence):
                anim_sequence.set_editor_property("retarget_source", unreal.Name(retarget_source))

                unreal.EditorAssetLibrary.save_loaded_asset(anim_sequence)
                unreal.log("RETARGET SOURCE SET => '{}' on {}".format(retarget_source, path))
            else:
                unreal.log_error("FAILED to cast or load asset as AnimSequence: {}".format(path))
    else:
        unreal.log_error("FAILED — no animation imported from {}".format(fbx_path))


def run_physics_importer(mesh_asset_name, json_path):
    physics_dest_path = "/Game/CustomSkins/{}/Meshes".format(code_name)
    physics_asset_name = "{}".format(os.path.splitext(os.path.basename(json_path))[0])
    skeletal_mesh_path = "{}/{}".format(physics_dest_path, mesh_asset_name)

    physics_json_path = json_path

    with open(physics_json_path, 'r') as f:
        json_content_string = f.read()

    physics_asset = None
    try:
        physics_asset = unreal.PhysicsImporter.import_physics_asset(
            json_content_string,
            physics_dest_path,
            physics_asset_name,
            skeletal_mesh_path
        )

        if physics_asset:
            unreal.log("SUCCESS => Created Physics Asset: {}/{}".format(physics_dest_path, physics_asset_name))
            unreal.log("SUCCESS => Linked to Skeletal Mesh: {}".format(skeletal_mesh_path))
        else:
            unreal.log_error("FAILED — Importer returned null. Check if JSON data matches internal structural exports.")


    except Exception as e:
        unreal.log_error("FAILED — Execution error trying to call PhysicsImporter: {}".format(str(e)))

    return physics_asset

def run_animation_importer(anim_sequence_path, json_path):
    with open(json_path, 'r') as f:
        json_content_string = f.read()

    success = False
    try:
        success = unreal.AnimationImporter.import_animation_sequence(
            json_content_string,
            anim_sequence_path
        )

        if success:
            unreal.log("SUCCESS => Imported animation data onto: {}".format(anim_sequence_path))
        else:
            unreal.log_error("FAILED — Importer returned false for: {}".format(anim_sequence_path))

    except Exception as e:
        unreal.log_error("FAILED — Execution error trying to call AnimationImporter: {}".format(str(e)))

    return success

for i in range(len(material_names)):
    create_material_instance(material_names[i])

if lobby_animation_fbx_path != "":
    import_animation(lobby_animation_fbx_path)
    if lobby_animation_json_path != "":
        anim_sequence_path = "{}/{}_Lobby_Animation".format("/Game/CustomSkins/{}/Animations".format(code_name), code_name)
        run_animation_importer(anim_sequence_path, lobby_animation_json_path)

for i in range(len(fbx_paths)):
    physics_assets = []
    if asset_names[i] in physics_mesh_names:
        for physics_asset_path in physics_asset_paths[i]:
            physics_asset = run_physics_importer(asset_names[i], physics_asset_path)
            physics_assets.append(physics_asset)
            
    if (asset_names[i] == head_mesh_name):
        import_fbx(fbx_paths[i], asset_names[i], True)
        mesh = unreal.EditorAssetLibrary.load_asset("{}/{}".format(fbx_destination_path, asset_names[i]))
        anim_bp_path = "/Game/Base/Head/Skeleton/Base_Head_AnimBP.Base_Head_AnimBP_C"
        if (fn_version == "9.41"):
            anim_bp_path = "/Game/Modding/Base_Head/Base_Head_Modding_AnimBP.Base_Head_Modding_AnimBP_C"
        mesh.set_editor_property(
            "post_process_anim_blueprint",
            unreal.load_class(None, anim_bp_path)
        )

    else:
        import_fbx(fbx_paths[i], asset_names[i])
        anim_bp = create_anim_blueprint("{}_AnimBP".format(asset_names[i]), "{}_Skeleton".format(asset_names[i]))
        unreal.PhysicsImporter.build_anim_graph(anim_bp, physics_assets)
        unreal.EditorAssetLibrary.save_loaded_asset(anim_bp)
        mesh = unreal.EditorAssetLibrary.load_asset("{}/{}".format(fbx_destination_path, asset_names[i]))
        mesh.set_editor_property(
            "post_process_anim_blueprint",
            unreal.load_class(None, "{0}/{1}_AnimBP.{1}_AnimBP_C".format(fbx_destination_path, asset_names[i]))
        )

    apply_materials_to_mesh(asset_names[i])
    unreal.EditorAssetLibrary.save_loaded_asset(mesh)

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

unreal.EditorLoadingAndSavingUtils.save_dirty_packages(False, True)