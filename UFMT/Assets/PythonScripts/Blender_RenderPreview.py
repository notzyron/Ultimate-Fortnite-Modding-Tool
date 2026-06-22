import bpy
import sys
import math
import traceback
import json
import base64

bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete()

encoded_data = sys.argv[-1]

decoded_bytes = base64.b64decode(encoded_data)
data = json.loads(decoded_bytes)

psk_files = data.get("Psks")
material_names = data.get("Materials")
texture_files = data.get("Textures")
swizzle_data = data.get("Swizzle")
gender = data.get("Gender")
render_path = data.get("RenderPath")
psa_path = data.get("LobbyAnimPath")
head_psk = data.get("HeadPsk")


bpy.context.scene.frame_set(25)
character_collection = bpy.data.collections.new("CharacterCollection")
bpy.context.scene.collection.children.link(character_collection)
default_collection = bpy.data.collections.get("Collection")
for psk_path in psk_files:
    try:
        # Note: scale=0.01 replaces bScaleDown=True in newer operators
        bpy.ops.psk.import_file(filepath=psk_path, scale=0.01)

        obj = bpy.context.active_object

        if not obj or obj.type != 'ARMATURE':
            continue

        pre_import_actions = set(bpy.data.actions)

        bpy.ops.psa.import_file(filepath=psa_path)

        #Manually link the new action (Since 4.5 doesn't do it automatically)
        new_actions = set(bpy.data.actions) - pre_import_actions
        if new_actions:
            action = list(new_actions)[0]
            if not obj.animation_data:
                obj.animation_data_create()
            obj.animation_data.action = action

        if psk_path == head_psk:
            head_bone_name = "head"

            if obj.animation_data and obj.animation_data.action:
                action = obj.animation_data.action
                head_bone = obj.pose.bones.get(head_bone_name)

                if head_bone:
                    head_family = [b.name for b in head_bone.children_recursive]

                    for fc in reversed(action.fcurves):
                        if 'pose.bones["' in fc.data_path:
                            bone_name = fc.data_path.split('"')[1]
                            if bone_name in head_family:
                                action.fcurves.remove(fc)

                    for name in head_family:
                        if name in obj.pose.bones:
                            obj.pose.bones[name].matrix_basis.identity()

                    print(f"Cleaned head animations for: {obj.name}")
    except Exception as e:
        print(f"FAILED TO PROCESS: {psk_path}\nError: {e}")
for obj in bpy.context.scene.collection.objects:
    bpy.context.scene.collection.objects.unlink(obj)
    character_collection.objects.link(obj)

for material_name in material_names:
    mat = bpy.data.materials.get(material_name)
    try:
        if mat.name in material_names:
            matIndex = material_names.index(mat.name)
            mat.use_nodes = True
            nodes = mat.node_tree.nodes
            links = mat.node_tree.links
            swizzle_material = swizzle_data[matIndex]

            bsdf = next((n for n in nodes if n.type == 'BSDF_PRINCIPLED'), None)

            if bsdf:
                if not any(n.type == 'ShaderNodeMix' for n in nodes):
                    multiplyNode = nodes.new('ShaderNodeMix')
                    multiplyNode.data_type = 'RGBA'
                    multiplyNode.blend_type = 'MULTIPLY'
                    multiplyNode.inputs[0].default_value = 1
                    links.new(multiplyNode.outputs['Result'], bsdf.inputs['Base Color'])

                    diffuseTexture = nodes.new("ShaderNodeTexImage")
                    links.new(diffuseTexture.outputs['Color'], multiplyNode.inputs['A'])

                    normalTexture = nodes.new("ShaderNodeTexImage")
                    normalMap = nodes.new("ShaderNodeNormalMap")
                    links.new(normalTexture.outputs['Color'], normalMap.inputs['Color'])
                    links.new(normalMap.outputs['Normal'], bsdf.inputs['Normal'])

                    specularTexture = nodes.new("ShaderNodeTexImage")
                    separateColor = nodes.new('ShaderNodeSeparateColor')

                    maskTexture = nodes.new("ShaderNodeTexImage")
                    colorRamp = nodes.new("ShaderNodeValToRGB")
                    links.new(maskTexture.outputs['Color'], colorRamp.inputs['Fac'])
                    links.new(colorRamp.outputs['Color'], multiplyNode.inputs['B'])

                    links.new(specularTexture.outputs['Color'], separateColor.inputs['Color'])
                    if swizzle_material:
                        links.new(separateColor.outputs['Green'], bsdf.inputs['Roughness'])
                        links.new(separateColor.outputs['Blue'], bsdf.inputs['Metallic'])
                    else:
                        links.new(separateColor.outputs['Blue'], bsdf.inputs['Roughness'])
                        links.new(separateColor.outputs['Green'], bsdf.inputs['Metallic'])
                    links.new(separateColor.outputs['Red'], bsdf.inputs[13])  # IOR Level

                    # Map textures using calculated index
                    base_idx = matIndex * 4
                    diffuseTexture.image = bpy.data.images.load(texture_files[base_idx])
                    maskTexture.image = bpy.data.images.load(texture_files[base_idx + 1])
                    normalTexture.image = bpy.data.images.load(texture_files[base_idx + 2])
                    specularTexture.image = bpy.data.images.load(texture_files[base_idx + 3])

                    normalTexture.image.colorspace_settings.name = 'Non-Color'
                    specularTexture.image.colorspace_settings.name = 'Non-Color'
                    maskTexture.image.colorspace_settings.name = 'Non-Color'

    except Exception as e:
        error_info = traceback.format_exc()

cam_data = bpy.data.cameras.new("cameraData")
cam_obj = bpy.data.objects.new("camera", cam_data)
bpy.context.collection.objects.link(cam_obj)
cam_obj.location = (-0.163458, -5.94001, 1.62492)
cam_obj.rotation_euler[0] = math.radians(82.7301)
cam_obj.rotation_euler[1] = math.radians(0)
cam_obj.rotation_euler[2] = math.radians(3.2)
bpy.context.scene.camera = cam_obj

sun_data = bpy.data.lights.new(name="sun_data", type="SUN")
sun_obj = bpy.data.objects.new("SunLight", sun_data)
bpy.context.collection.objects.link(sun_obj)
sun_data.energy = 4
sun_obj.rotation_euler[0] = math.radians(9.90522)
sun_obj.rotation_euler[1] = math.radians(56.6127)
sun_obj.rotation_euler[2] = math.radians(70.3923)

area1_data = bpy.data.lights.new(name="area1_data", type="AREA")
area1_obj = bpy.data.objects.new("area1", area1_data)
character_collection.objects.link(area1_obj)
area1_obj.location = (-0.790322, 0.388373, 1.90285)
area1_obj.rotation_euler[0] = math.radians(-26.7347)
area1_obj.rotation_euler[1] = math.radians(-69.034)
area1_obj.rotation_euler[2] = math.radians(-3.5952)
area1_data.energy = 30
area1_data.size = 0.67

area2_data = bpy.data.lights.new(name="area2_data", type="AREA")
area2_obj = bpy.data.objects.new("area2", area2_data)
bpy.context.collection.objects.link(area2_obj)
area2_obj.location = (0.763212, 0.402607, 0.626726)
area2_obj.rotation_euler[0] = math.radians(72.6652)
area2_obj.rotation_euler[1] = math.radians(38.7354)
area2_obj.rotation_euler[2] = math.radians(137.142)
area2_data.energy = 10
area2_data.size = 0.68

area3_data = bpy.data.lights.new(name="area3_data", type="AREA")
area3_obj = bpy.data.objects.new("area3", area3_data)
bpy.context.collection.objects.link(area3_obj)
area3_obj.location = (-0.041041, -1.16674, 1.59209)
area3_obj.rotation_euler[0] = math.radians(85.85)
area3_obj.rotation_euler[1] = math.radians(-0.115121)
area3_obj.rotation_euler[2] = math.radians(0.002299)
area3_data.energy = 17.2
area3_data.size = 1

area4_data = bpy.data.lights.new(name="area4_data", type="AREA")
area4_obj = bpy.data.objects.new("area4", area4_data)
bpy.context.collection.objects.link(area4_obj)
area4_obj.location = (0.833295, 0.594419, 1.24294)
area4_obj.rotation_euler[0] = math.radians(96.4921)
area4_obj.rotation_euler[1] = math.radians(100.761)
area4_obj.rotation_euler[2] = math.radians(138.723)
area4_data.energy = 5.7
area4_data.size = 1
area4_data.color = (0, 0.238, 0.815)

area1_obj.light_linking.receiver_collection = bpy.data.collections.new("area1_receivers")
area1_obj.light_linking.receiver_collection = character_collection
area2_obj.light_linking.receiver_collection = bpy.data.collections.new("area1_receivers")
area2_obj.light_linking.receiver_collection = character_collection
area3_obj.light_linking.receiver_collection = bpy.data.collections.new("area1_receivers")
area3_obj.light_linking.receiver_collection = character_collection
area4_obj.light_linking.receiver_collection = bpy.data.collections.new("area1_receivers")
area4_obj.light_linking.receiver_collection = character_collection


bpy.ops.mesh.primitive_plane_add(size=2.0, enter_editmode=False, align='WORLD', location=(0, 0, 0))
shadow_catcher = bpy.context.active_object
shadow_catcher.scale = (5,5,5)
sc_mat = bpy.data.materials.new("ScMaterial")
sc_mat.use_nodes = True
sc_mat_nodes = sc_mat.node_tree.nodes
sc_mat_links = sc_mat.node_tree.links

color_ramp = sc_mat_nodes.new('ShaderNodeValToRGB')
cr_elements = color_ramp.color_ramp.elements
cr_elements[0].color = (1.0, 1.0, 1.0, 1.0)
cr_elements[1].color = (0.0, 0.0, 0.0, 1.0)
cr_elements[1].position = 0.53091

shader_to_rgb = sc_mat_nodes.new('ShaderNodeShaderToRGB')
diffuse_bsdf = sc_mat_nodes.new('ShaderNodeBsdfDiffuse')

principled_bsdf = sc_mat_nodes.get('Principled BSDF')
principled_bsdf.inputs['Roughness'].default_value = 1.0
principled_bsdf.inputs[12].default_value = 0.0 #Specular IOR Level
principled_bsdf.inputs['Base Color'].default_value = (0,0,0,0)

sc_mat_links.new(diffuse_bsdf.outputs['BSDF'], shader_to_rgb.inputs['Shader'])
sc_mat_links.new(shader_to_rgb.outputs['Color'], color_ramp.inputs['Fac'])
sc_mat_links.new(shader_to_rgb.outputs['Color'], color_ramp.inputs['Fac'])
sc_mat_links.new(color_ramp.outputs['Color'], principled_bsdf.inputs['Alpha'])


sc_mat.blend_method = 'BLEND'
shadow_catcher.data.materials.append(sc_mat)

world_data = bpy.context.scene.world
world_data.use_nodes = True
bg_node = world_data.node_tree.nodes.get("Background")
bg_node.inputs['Strength'].default_value = 0.5
bg_node.inputs['Color'].default_value = (1, 1, 1, 1)

bpy.context.scene.render.film_transparent = True

bpy.context.scene.view_settings.look = "AgX - High Contrast"

bpy.context.scene.use_nodes = True
tree = bpy.context.scene.node_tree

tree.nodes.clear()
render_layers = tree.nodes.new(type="CompositorNodeRLayers")
hue_sat = tree.nodes.new(type="CompositorNodeHueSat")
composite = tree.nodes.new(type="CompositorNodeComposite")


hue_sat.inputs["Saturation"].default_value = 1.1

tree.links.new(render_layers.outputs["Image"], hue_sat.inputs["Image"])
tree.links.new(hue_sat.outputs["Image"], composite.inputs["Image"])

render = bpy.context.scene.render
render.filepath = render_path
render.image_settings.file_format = 'PNG'
render.resolution_x = 2560
render.resolution_y = 1440
render.resolution_percentage = 100
bpy.ops.render.render(write_still=True)