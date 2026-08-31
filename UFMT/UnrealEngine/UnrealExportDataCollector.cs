using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using UFMT.FnAssets;

namespace UFMT.UnrealEngine
{
    internal static class UnrealExportDataCollector
    {
        internal static UnrealExportData CollectData(string smallIcon, string largeIcon, ObservableCollection<Material> materials, string texturesPath, bool manuallySwizzleMaterials, string sourcePath, 
        string lobbyAnimationFbx, string lobbyAnimationJson, List<CharacterPart> characterParts, string skinGender, string codename, string CID, string ueSkinsPackagePath)
        {
            List<string> meshNames = new();
            List<string> diffuseTexturePaths = new();
            List<string> maskTexturePaths = new();
            List<string> normalTexturePaths = new();
            List<string> specularTexturePaths = new();
            List<string> iconTexturePaths = new();

            if (smallIcon != string.Empty && largeIcon != string.Empty)
            {
                iconTexturePaths.Add(Path.Combine(sourcePath, "Textures", $"{smallIcon}.png"));
                iconTexturePaths.Add(Path.Combine(sourcePath, "Textures", $"{largeIcon}.png"));
            }
            else iconTexturePaths = new(){string.Empty, string.Empty };

            foreach (Material mat in materials)
            {
                diffuseTexturePaths.Add(Path.Combine(texturesPath, $"{mat.SelectedDiffuse}.png"));
                maskTexturePaths.Add(Path.Combine(texturesPath, $"{mat.SelectedMask}.png"));
                normalTexturePaths.Add(Path.Combine(texturesPath, $"{mat.SelectedNormal}.png"));

                if (manuallySwizzleMaterials && mat.Swizzle) specularTexturePaths.Add(Path.Combine(texturesPath, "Swizzled", $"{mat.SelectedSpecular}.png"));
                else specularTexturePaths.Add(Path.Combine(texturesPath, $"{mat.SelectedSpecular}.png"));
            }
            Path.Combine(sourcePath, "Fbx", $"{lobbyAnimationFbx}.fbx");

            string lobbyAnimationFbxFilePath = Path.Combine(sourcePath, "Fbx", "Lobby_Animation", $"{lobbyAnimationFbx}.fbx");
            var unrealData = new UnrealExportData()
            {
                FbxPaths = characterParts.Select(cp => $"{cp.FbxPath}.fbx").ToList(),
                PhysicsMeshNames = characterParts.Where(cp => cp.PhysicsAssetJsonPaths.Count > 0).ToList().Select(cp => Path.GetFileNameWithoutExtension(cp.FbxPath)).ToList(),
                PhysicsAssetsPaths = characterParts.Select(cp => cp.PhysicsAssetJsonPaths).ToList(),
                DiffuseTextures = diffuseTexturePaths,
                MaskTextures = maskTexturePaths,
                NormalTextures = normalTexturePaths,
                SpecularTextures = specularTexturePaths,
                IconTextures = iconTexturePaths,
                Materials = materials.Select(mat => mat.Name).ToList(),
                Codename = codename,
                MeshNames = characterParts.Select(cp => Path.GetFileNameWithoutExtension(cp.FbxPath)).ToList(),
                CID = CID,
                LobbyAnimationFbxPath = Path.Exists(lobbyAnimationFbxFilePath) ? lobbyAnimationFbxFilePath : string.Empty,
                RetargetSource = skinGender == "Male" ? "MPR_SK_M_MALE_Base_Skeleton" : "SK_M_Female_Base_Skeleton",
                LobbyAnimationJsonPath = string.IsNullOrEmpty(lobbyAnimationJson) ? string.Empty :
                Path.Combine(sourcePath, "Lobby_Animation", $"{lobbyAnimationJson}.json"),
                HeadMeshName = Path.GetFileNameWithoutExtension(characterParts.FirstOrDefault(cp => cp.Type == "Head").FbxPath),
                CurrentFnVersion = App.Settings.FnVersion,
                UeSkinsPackagePath = ueSkinsPackagePath
            };

            return unrealData;
        }
    }
}
