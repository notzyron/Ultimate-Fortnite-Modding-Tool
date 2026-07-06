obj = next((o for o in bpy.context.scene.objects if o.type == 'MESH'), None)

shape_key_combinations = {
    "C_glabella_down_pose": {"browDownL": 0.75, "browDownR": 0.75, "browLateralL": 1.0, "browLateralR": 1.0},
    "tongue_up_pose": {"tongueUp": 1.0},
    "phoneme_mbp_pose": {"mouthLowerLipTowardsTeethL": 0.5, "mouthLowerLipTowardsTeethR": 0.5, "mouthUpperLipTowardsTeethL": 0.5, "mouthUpperLipTowardsTeethR": 0.5},
    "L_upper_lip_lower_pose": {"mouthUpperLipTowardsTeethL": 1.0},
    "R_blink_pose": {"eyeBlinkR": 1.0},
    "R_wide_pose": {"eyeWidenR": 1.0},
    "look_right_pose": {"eyeLookRightL": 1.0, "eyeLookRightR": 1.0},
    "jaw_up_pose": {"jawChinRaiseDL": 0.5, "jawChinRaiseDR": 0.5},
    "L_squeeze_pose": {"eyeCheekRaiseL": 1.0, "noseWrinkleL": 0.5},
    "R_brow_up_pose": {"browRaiseInR": 1.0, "browRaiseOuterR": 0.5},
    "look_left_pose": {"eyeLookLeftL": 1.0, "eyeLookLeftR": 1.0},
    "tongue_back_pose": {"tongueIn": 1.0},
    "R_squint_inner_pose": {"eyeSquintInnerR": 1.0},
    "look_up_pose": {"eyeLookUpL": 1.0, "eyeLookUpR": 1.0},
    "phoneme_fv_pose": {"jawChinRaiseUL": 0.3, "jawChinRaiseUR": 0.3, "mouthCornerWideL": 1.0, "mouthCornerWideR": 1.0, "mouthLowerLipBiteL": 1.0, "mouthLowerLipBiteR": 1.0, "mouthLowerLipTowardsTeethL": 0.2, "mouthLowerLipTowardsTeethR": 0.2},
    "jaw_back_pose": {"jawBack": 1.0},
    "R_smile_pose": {"mouthCornerPullR": 0.65, "mouthCornerWideR": 1.0, "mouthUpperLipTowardsTeethR": 0.5},
    "R_lip_corner_narrow_pose": {"mouthCornerNarrowR": 1.0},
    "eye_convergence_pose": {"eyeLookLeftR": 0.6, "eyeLookRightL": 0.6},
    "R_brow_down_pose": {"browDownR": 1.0},
    "R_frown_pose": {"mouthCornerWideR": 1.0, "mouthLowerLipTowardsTeethR": 1.0, "mouthStretchR": 1.0},
    "L_lower_lip_down_pose": {"mouthLowerLipDepressL": 1.0, "mouthLowerLipTowardsTeethR": 0.5},
    "L_blink_pose": {"eyeBlinkL": 1.0},
    "L_lip_corner_narrow_pose": {"mouthCornerNarrowR": 1.0},
    "look_down_pose": {"eyeWidenR": 0.5, "eyeWidenL": 0.5, "eyeSquintInnerL": 1.0, "eyeSquintInnerR": 1.0, "eyeLookDownL": 1.0, "eyeLookDownR": 1.0},
    "R_upper_lip_raiser_pose": {"mouthUpperLipRaiseR": 1.0},
    "phoneme_oo_pose": {"jawOpen": 0.25, "mouthCornerNarrowL": 0.5, "mouthCornerNarrowR": 0.5, "mouthLipsPurseDL": 1.0, "mouthLipsPurseDR": 1.0, "mouthLipsPurseUL": 1.0, "mouthLipsPurseUR": 1.0},
    "R_brow_squeeze_in_pose": {"browDownR": 0.3, "browLateralR": 1.0},
    "jaw_left_pose": {"jawLeft": 1.0},
    "L_lip_corner_wide_pose": {"mouthCornerWideL": 1.0},
    "C_nose_wrinkler_pose": {"noseWrinkleL": 0.5, "noseWrinkleR": 0.5, "browDownL": 0.5, "browDownR": 0.5, "browLateralL": 1.0, "browLateralR": 1.0, "noseNasolabialDeepenL": 1.0, "noseNasolabialDeepenR": 1.0},
    "R_lower_lip_up_pose": {"mouthLowerLipTowardsTeethR": 1.0},
    "jaw_open_pose": {"jawOpen": 1.0},
    "L_brow_up_pose": {"browRaiseInL": 1.0, "browRaiseOuterL": 0.5},
    "R_lip_corner_wide_pose": {"mouthCornerWideR": 1.0},
    "L_lower_lip_up_pose": {"mouthLowerLipTowardsTeethL": 1.0},
    "R_lower_lip_down_pose": {"mouthLowerLipDepressR": 1.0, "mouthLowerLipTowardsTeethL": 0.5},
    "L_upper_lip_raiser_pose": {"mouthUpperLipRaiseL": 1.0},
    "R_cheek_up_pose": {"eyeCheekRaiseR": 0.75},
    "L_brow_down_pose": {"browDownL": 1.0},
    "jaw_forward_pose": {"jawFwd": 1.0},
    "L_squint_inner_pose": {"eyeSquintInnerL": 1.0},
    "tongue_down_pose": {"tongueDown": 1.0},
    "R_upper_lip_lower_pose": {"mouthUpperLipTowardsTeethR": 1.0},
    "L_brow_squeeze_in_pose": {"browDownL": 0.3, "browLateralL": 1.0},
    "jaw_right_pose": {"jawRight": 1.0},
    "mouth_down_pose": {"mouthDown": 1.0},
    "C_glabella_up_pose": {"browRaiseInL": 0.5, "browRaiseInR": 0.5},
    "phoneme_ch_pose": {"mouthLowerLipDepressL": 0.5, "mouthLowerLipDepressR": 0.5, "mouthUpperLipRaiseL": 0.6, "mouthUpperLipRaiseR": 0.6, "mouthCornerNarrowL": 1.0, "mouthCornerNarrowR": 1.0},
    "L_frown_pose": {"mouthCornerWideL": 1.0, "mouthLowerLipTowardsTeethL": 1.0, "mouthStretchL": 1.0},
    "R_squeeze_pose": {"eyeCheekRaiseR": 1.0, "noseWrinkleR": 0.5},
    "tongue_forward_pose": {"tongueOut": 1.0},
    "L_upperLidDown_pose": {"eyeCheekRaiseL": 1.0, "eyeLookDownL": 1.0, "eyeLookUpL": 1.0, "eyeSquintInnerL": 0.5},
    "R_upperLidDown_pose": {"eyeCheekRaiseR": 1.0, "eyeLookDownR": 1.0, "eyeLookUpR": 1.0, "eyeSquintInnerR": 0.5},
    "L_wide_pose": {"eyeWidenL": 1.0},
    "L_smile_pose": {"mouthCornerPullL": 0.65, "mouthCornerWideL": 1.0, "mouthUpperLipTowardsTeethL": 0.5},
    "mouth_up_pose": {"mouthUp": 1.0},
    "L_cheek_up_pose": {"eyeCheekRaiseL": 0.75}
}


def combine_shape_keys(obj, new_shape_key_name, shape_key_data):
    if new_shape_key_name in obj.data.shape_keys.key_blocks:
        return

    basis = obj.data.shape_keys.key_blocks['Basis']
    new_shape = obj.shape_key_add(name=new_shape_key_name, from_mix=False)
    new_shape.interpolation = 'KEY_LINEAR'

    for key_name, value in shape_key_data.items():
        if key_name in obj.data.shape_keys.key_blocks:
            key_block = obj.data.shape_keys.key_blocks[key_name]
            for i in range(len(obj.data.vertices)):
                new_shape.data[i].co += (key_block.data[i].co - basis.data[i].co) * value

    num_keys = len(obj.data.shape_keys.key_blocks)
    basis_index = obj.data.shape_keys.key_blocks.keys().index('Basis')
    new_shape_index = obj.active_shape_key_index
    while new_shape_index > basis_index + 1:
        obj.active_shape_key_index = new_shape_index
        bpy.ops.object.shape_key_move(type='UP')
        new_shape_index -= 1

if obj and obj.data.shape_keys:
    bpy.context.view_layer.objects.active = obj
    for new_shape_key, data in shape_key_combinations.items():
        if new_shape_key not in obj.data.shape_keys.key_blocks:
            combine_shape_keys(obj, new_shape_key, data)