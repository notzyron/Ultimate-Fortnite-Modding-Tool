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

male_animation_fbx_path = data.get("MaleAnimationFbxPath")
male_animation_json_path = data.get("MaleAnimationJsonPath")
male_animation_length = data.get("MaleAnimationLength")
female_animation_fbx_path = data.get("FemaleAnimationFbxPath")
female_animation_json_path = data.get("FemaleAnimationJsonPath")
female_animation_length = data.get("FemaleAnimationLength")
icon_textures = data.get("IconTexturePaths")
sound_wav_path = data.get("SoundWavPath")
sound_wav_compression_quality = data.get("SoundWavCompressionQuality")
code_name = data.get("Codename")
eid = data.get("EID")
package_path = data.get("UeEmotesPackagePath")

animations_destination_path = "{}/{}/Animation".format(package_path, code_name)
sounds_destination_path = "{}/{}/Sound".format(package_path, code_name)
icons_destination_path  = "{}/{}/UI".format(package_path, code_name)

unreal.EditorAssetLibrary.load_asset("/Game/CID_Template")

def delete_directory_if_exists(path):
    if unreal.EditorAssetLibrary.does_directory_exist(path):
        unreal.EditorAssetLibrary.delete_directory(path)
        unreal.log("Deleted existing directory: {}".format(path))

delete_directory_if_exists(animations_destination_path)
delete_directory_if_exists(sounds_destination_path)
delete_directory_if_exists(icons_destination_path)

def create_fake_eid():
    template_path = "/Game/CID_Template"
    new_path      = "{}/{}/{}".format(package_path, code_name, eid)

    if unreal.EditorAssetLibrary.does_asset_exist(new_path):
        unreal.EditorAssetLibrary.delete_asset(new_path)

    success = unreal.EditorAssetLibrary.duplicate_asset(template_path, new_path)

    if success:
        da = unreal.load_asset(new_path)
        unreal.EditorAssetLibrary.save_loaded_asset(da)
        unreal.log("SUCCESS => {}".format(new_path))
    else:
        unreal.log_error("FAILED — could not create fake cid: {}".format(cid))


def import_animation(fbx_path, animation_name):
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
    anim_data.set_editor_property("preserve_local_transform", True)

    ui = unreal.FbxImportUI()

    ui = unreal.FbxImportUI()
    skel_data = unreal.FbxSkeletalMeshImportData()
    skel_data.set_editor_property("import_morph_targets", False)
    ui.skeletal_mesh_import_data = skel_data

    ui.mesh_type_to_import = unreal.FBXImportType.FBXIT_ANIMATION

    ui.automated_import_should_detect_type = False

    ui.import_mesh = False  # no mesh
    ui.import_animations = True  # animation track only
    ui.import_as_skeletal = False  # not a skeletal mesh import path
    ui.import_materials = False
    ui.import_textures = False
    ui.create_physics_asset = False
    ui.skeleton = skeleton  # required, UE won't import without it
    ui.anim_sequence_import_data = anim_data  # animation transform/scale settings

    task = unreal.AssetImportTask()
    task.filename = fbx_path
    task.destination_path = "{}/{}/Animations".format(package_path, code_name)
    task.destination_name = animation_name
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
                anim_sequence.set_editor_property("retarget_source", "MPR_SK_M_MALE_Base_Skeleton") # Always apply male retarget source for emote anims

                unreal.EditorAssetLibrary.save_loaded_asset(anim_sequence)
                unreal.log("RETARGET SOURCE SET => '{}' on {}".format(retarget_source, path))
            else:
                unreal.log_error("FAILED to cast or load asset as AnimSequence: {}".format(path))
    else:
        unreal.log_error("FAILED — no animation imported from {}".format(fbx_path))

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


def import_sound(wav_path):
    if not wav_path or not os.path.exists(wav_path):
        return

    task = unreal.AssetImportTask()
    task.filename = wav_path
    task.destination_path = sounds_destination_path
    task.destination_name = "{}_Sound".format(code_name)
    task.replace_existing = True
    task.automated = True
    task.save = False

    unreal.AssetToolsHelpers.get_asset_tools().import_asset_tasks([task])

    if task.imported_object_paths:
        sound_wave = unreal.load_asset(task.imported_object_paths[0])
        if sound_wave:
            sound_wave.set_editor_property("streaming", True)
            sound_wave.set_editor_property("b_virtualize_when_silent", True)
            sound_wave.set_editor_property("looping", True)

            if sound_wav_compression_quality is not None:
                sound_wave.set_editor_property("compression_quality", int(sound_wav_compression_quality))

            unreal.EditorAssetLibrary.save_loaded_asset(sound_wave)
            unreal.log("SUCCESS => Imported sound: {}".format(task.imported_object_paths[0]))
    else:
        unreal.log_error("FAILED => Could not import sound: {}".format(wav_path))

if male_animation_fbx_path != "":
    import_animation(male_animation_fbx_path, "Emote_{}_CMM".format(code_name))
    if male_animation_json_path != "":
        anim_sequence_path = "{}/{}_Lobby_Animation".format("{}/{}/Animations".format(package_path, code_name), code_name)
        run_animation_importer(anim_sequence_path, male_animation_json_path)

if female_animation_fbx_path != "":
    import_animation(female_animation_fbx_path, "Emote_{}_CMF".format(code_name))
    if female_animation_json_path != "":
        anim_sequence_path = "{}/{}_Lobby_Animation".format("{}/{}/Animations".format(package_path, code_name), code_name)
        run_animation_importer(anim_sequence_path, female_animation_json_path)

if sound_wav_path:
    import_sound(sound_wav_path)

for i in range(len(icon_textures)):
    if icon_textures[i] != "":
        import_texture(icon_textures[i], "icon")

create_fake_eid()

unreal.EditorLoadingAndSavingUtils.save_dirty_packages(False, True)