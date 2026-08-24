using Microsoft.UI.Xaml;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.PropertyTypes.Structs;
using UAssetAPI.UnrealTypes;

namespace UFMT
{
    internal static class SkinAssetCreator
    {
        public static Dictionary<string, string> SeriesCodenames = new(){ {"Dark Series", "CUBESeries"}, { "Star Wars Series", "ColumbusSeries" },
        {"Icon Series", "CreatorCollabSeries"}, {"DC Series", "DCUSeries"}, {"Frozen Series", "FrozenSeries" }, {"Lava Series", "LavaSeries"},
        {"Marvel Series", "MarvelSeries"}, {"Shadow Series", "ShadowSeries"},  {"Slurp Series", "SlurpSeries"},
        {"Test Series", "FakeToken_FDS_Series"}, {"Anual Pass Series", "2020AnnualPassSeries"}};

        internal static void CopyFilesFromUe(string contentFolderPath, DirectoryInfo cookedCharacterDirectory, string cookedAssetsPath, string outputFnGamePath, 
        string baseHeadPath, bool replaceCookedBaseHead, string fnVerNumber, string ueVersionName, string[] baseHeadFileNames)
        {
            if (!Path.Exists(contentFolderPath)) Directory.CreateDirectory(contentFolderPath);
            foreach (DirectoryInfo subFolder in cookedCharacterDirectory.GetDirectories("*", SearchOption.AllDirectories))
            {
                string targetSubDir = subFolder.FullName.Replace(cookedCharacterDirectory.FullName, contentFolderPath);
                Directory.CreateDirectory(targetSubDir);

                foreach (FileInfo file in subFolder.GetFiles())
                {
                    file.CopyTo(Path.Combine(targetSubDir, file.Name), true);
                }
            }

            string cookedBaseHeadPath = Path.Combine(cookedAssetsPath, "Base", "Head", "Skeleton");
            if (fnVerNumber == "9.41") cookedBaseHeadPath = Path.Combine(cookedAssetsPath, "Modding", "Base_Head"); // 9.41 uses a different location for base head

            string outputBaseHeadPath = Path.Combine(outputFnGamePath, baseHeadPath);

            if (!Directory.Exists(outputBaseHeadPath)) Directory.CreateDirectory(outputBaseHeadPath);

            if (replaceCookedBaseHead)
            {
                foreach (string fileName in baseHeadFileNames)
                {
                    File.WriteAllBytes(Path.Combine(cookedBaseHeadPath, fileName), TemplateLoader.GetEmbeddedFile(ueVersionName, "CookedUeAssets.BaseHead", fileName));
                    File.WriteAllBytes(Path.Combine(cookedBaseHeadPath, Path.ChangeExtension(fileName, ".uexp")), 
                    TemplateLoader.GetEmbeddedFile(ueVersionName, "CookedUeAssets.BaseHead", Path.ChangeExtension(fileName, ".uexp")));
                }
            } // Replace the files inside cooked ue folders

            foreach (string file in Directory.GetFiles(cookedBaseHeadPath))
            {
                File.Copy(file, Path.Combine(outputBaseHeadPath, Path.GetFileName(file)), true);
            }

            Log.Success($"Copied files from {cookedCharacterDirectory} to {contentFolderPath}");

        }

        internal static void CreateMaterials(string contentFolderPath, string codename, ObservableCollection<Material> materials, 
        FnVersion fnVersion, EngineVersion uassetApiEngineVersion, string ueSkinsPackagePath)
        {
            string materialsPath = Path.Combine(contentFolderPath, "Materials");
            foreach (Material material in materials)
            {
                string uassetMaterialPath = Path.Combine(materialsPath, $"{material.Name}.uasset");
                string uexpMaterialPath = Path.Combine(materialsPath, $"{material.Name}.uexp");

                byte[] materialUassetBase64 = TemplateLoader.GetEmbeddedFile(fnVersion.Name, "CookedUeAssets", "MiNoSwizzle.uasset");
                byte[] materialUexpBase64 = TemplateLoader.GetEmbeddedFile(fnVersion.Name, "CookedUeAssets", "MiNoSwizzle.uexp");
                if (!fnVersion.ManuallySwizzleMaterials && material.Swizzle)
                {
                    materialUassetBase64 = TemplateLoader.GetEmbeddedFile(fnVersion.Name, "CookedUeAssets", "MiSwizzle.uasset");
                    materialUexpBase64 = TemplateLoader.GetEmbeddedFile(fnVersion.Name, "CookedUeAssets", "MiSwizzle.uexp");
                }

                File.WriteAllBytes(Path.Combine(uassetMaterialPath), materialUassetBase64);
                File.WriteAllBytes(Path.Combine(uexpMaterialPath), materialUexpBase64);
                Log.Success($"Created material instance {material.Name}");
                Console.WriteLine($"Editing {material.Name}");

                var currentMi = new UAsset(uassetMaterialPath, uassetApiEngineVersion);
                var miImportData = currentMi.Imports;
                var miExportData = currentMi.Exports;
                var miExport0 = (NormalExport)currentMi.Exports[0];
                string fnTexturesPath = $"{ueSkinsPackagePath}/{codename}/Textures/";
                miImportData[fnVersion.DiffusePathIndex].ObjectName.Value.Value = Path.Combine(fnTexturesPath, material.SelectedDiffuse);
                Console.WriteLine($"Changed the diffuse texture path in {material.Name} to {Path.Combine(fnTexturesPath, material.SelectedDiffuse)}");
                miImportData[fnVersion.DiffusePathIndex + 1].ObjectName.Value.Value = Path.Combine(fnTexturesPath, material.SelectedMask);
                Console.WriteLine($"Changed the mask texture path in {material.Name} to {Path.Combine(fnTexturesPath, material.SelectedMask)}");
                miImportData[fnVersion.DiffusePathIndex + 2].ObjectName.Value.Value = Path.Combine(fnTexturesPath, material.SelectedNormal);
                Console.WriteLine($"Changed the normal texture path in {material.Name} to {Path.Combine(fnTexturesPath, material.SelectedNormal)}");
                miImportData[fnVersion.DiffusePathIndex + 3].ObjectName.Value.Value = Path.Combine(fnTexturesPath, material.SelectedSpecular);
                Console.WriteLine($"Changed the specular texture path in {material.Name} to {Path.Combine(fnTexturesPath, material.SelectedSpecular)}");
                miImportData[fnVersion.DiffuseNameIndex].ObjectName.Value.Value = material.SelectedDiffuse;
                Console.WriteLine($"Changed the diffuse texture in {material.Name} to {material.SelectedDiffuse}");
                miImportData[fnVersion.DiffuseNameIndex + 1].ObjectName.Value.Value = material.SelectedMask;
                Console.WriteLine($"Changed the mask texture in {material.Name} to {material.SelectedMask}");
                miImportData[fnVersion.DiffuseNameIndex + 2].ObjectName.Value.Value = material.SelectedNormal;
                Console.WriteLine($"Changed the normal texture in {material.Name} to {material.SelectedNormal}");
                miImportData[fnVersion.DiffuseNameIndex + 3].ObjectName.Value.Value = material.SelectedSpecular;
                Console.WriteLine($"Changed the specular texture in {material.Name} to {material.SelectedSpecular}");
                miExportData[0].ObjectName.Value.Value = material.Name;

                if (material.UseSkinBoostColor)
                {
                    var vectorParamaterValues = (ArrayPropertyData)miExport0["VectorParameterValues"];
                    var vectorParamaterValues2 = (StructPropertyData)vectorParamaterValues.Value[0];
                    var parameterValue = (StructPropertyData)vectorParamaterValues2.Value[1];
                    var colors = (LinearColorPropertyData)parameterValue.Value[0];

                    colors.Value = new FLinearColor(material.SbcRed, material.SbcGreen, material.SbcBlue, material.SbcAlpha);

                    Console.WriteLine($"Changed the skin boost color and exponent to {colors.Value.ToString()} in {material}");
                }

                currentMi.Write(uassetMaterialPath);
                Log.Success($"Successfully edited {material.Name}.uasset and {material.Name}.uexp");
            }
        }

        internal static void CreateCharacterParts(string contentFolderPath, string gender, string codename, List<CharacterPart> characterParts, 
        FnVersion fnVersion, EngineVersion uassetApiEngineVersion, string ueSkinsPackagePath)
        {
            string characterPartsPath = Path.Combine(contentFolderPath, "CharacterParts");
            CharacterPart body = characterParts.FirstOrDefault(cp => cp.Type == "Body");
            CharacterPart head = characterParts.FirstOrDefault(cp => cp.Type == "Head");
            CharacterPart faceacc = characterParts.FirstOrDefault(cp => cp.Type == "Faceacc");
            CharacterPart hat = characterParts.FirstOrDefault(cp => cp.Type == "Hat");
            if (!Path.Exists(characterPartsPath)) Directory.CreateDirectory(characterPartsPath);

            if (gender == "Female")
            {
                body.UassetFile = TemplateLoader.GetEmbeddedFile(fnVersion.Name, "CookedUeAssets", "CpBodyFemale.uasset");
                body.UexpFile = TemplateLoader.GetEmbeddedFile(fnVersion.Name, "CookedUeAssets", "CpBodyFemale.uexp");
                head.UassetFile = TemplateLoader.GetEmbeddedFile(fnVersion.Name, "CookedUeAssets", "CpHeadFemale.uasset");
                head.UexpFile = TemplateLoader.GetEmbeddedFile(fnVersion.Name, "CookedUeAssets", "CpHeadFemale.uexp");
                if (faceacc != null)
                {
                    faceacc.UassetFile = TemplateLoader.GetEmbeddedFile(fnVersion.Name, "CookedUeAssets", "CpFaceAccFemale.uasset"); ;
                    faceacc.UexpFile = TemplateLoader.GetEmbeddedFile(fnVersion.Name, "CookedUeAssets", "CpFaceAccFemale.uexp"); ;
                }
            }
            else if (gender == "Male")
            {
                body.UassetFile = TemplateLoader.GetEmbeddedFile(fnVersion.Name, "CookedUeAssets", "CpBodyMale.uasset");
                body.UexpFile = TemplateLoader.GetEmbeddedFile(fnVersion.Name, "CookedUeAssets", "CpBodyMale.uexp");
                head.UassetFile = TemplateLoader.GetEmbeddedFile(fnVersion.Name, "CookedUeAssets", "CpHeadMale.uasset");
                head.UexpFile = TemplateLoader.GetEmbeddedFile(fnVersion.Name, "CookedUeAssets", "CpHeadMale.uexp");
                if (faceacc != null)
                {
                    faceacc.UassetFile = TemplateLoader.GetEmbeddedFile(fnVersion.Name, "CookedUeAssets", "CpFaceAccMale.uasset"); ;
                    faceacc.UexpFile = TemplateLoader.GetEmbeddedFile(fnVersion.Name, "CookedUeAssets", "CpFaceAccMale.uexp"); ;
                }
            }

            foreach (CharacterPart cp in characterParts)
            {
                Console.WriteLine($"Currently editing the {cp.Type} of the skin");
                string uassetPath = Path.Combine(characterPartsPath,
                $"CP_{cp.Type}_{codename}.uasset");
                string uexpPath = Path.Combine(characterPartsPath,
                $"CP_{cp.Type}_{codename}.uexp");

                File.WriteAllBytes(uassetPath, cp.UassetFile);
                File.WriteAllBytes(uexpPath, cp.UexpFile);

                var currentCp = new UAsset(uassetPath, uassetApiEngineVersion);
                var cpExport0 = (NormalExport)currentCp.Exports[0];
                var cpExport1 = (NormalExport)currentCp.Exports[1];
                cpExport1.ObjectName.Value.Value = $"CP_{cp.Type}_{codename}";
                if (cp.Type != "Hat")
                {
                    string animBpPath;
                    if (cp.Type == "Head")
                    {
                        if (fnVersion.Name == "9.41") animBpPath = $"/Game/Modding/Base_Head/Base_Head_Modding_AnimBP.Base_Head_Modding_AnimBP_C";
                        else animBpPath = $"/Game/Base/Head/Skeleton/Base_Head_AnimBP.Base_Head_AnimBP_C";
                    }
                    else animBpPath = $"{ueSkinsPackagePath}/{codename}/Meshes/{codename}_{cp.Type}_AnimBP.{codename}_{cp.Type}_AnimBP_C";

                    var animBpData = (SoftObjectPropertyData)cpExport0["AnimClass"];
                    animBpData.Value.AssetPath.AssetName.Value.Value = animBpPath;

                    Console.WriteLine($"Changed the Animation Blueprint in CP_{cp.Type}_{codename} to {animBpPath}");
                }
                var mesh = (SoftObjectPropertyData)cpExport1["SkeletalMesh"];
                mesh.Value.AssetPath.AssetName.Value.Value = $"{ueSkinsPackagePath}/{codename}/Meshes/" +
                $"{codename}_{cp.Type}.{codename}_{cp.Type}";
                Console.WriteLine($"Changed the Mesh in CP_{cp.Type}_{codename} to {ueSkinsPackagePath}/{codename}/Meshes/" +
                $"{codename}_{cp.Type}.{codename}_{cp.Type}");

                Console.WriteLine(uassetPath);
                currentCp.Write(uassetPath);
                Log.Success($"Successfully edited CP_{cp.Type}_{codename}.uasset and " +
                $"CP_{cp.Type}_{codename}.uexp");
            }
        }

        internal static void CreateHeroSpecialization(string contentFolderPath, string codename, List<CharacterPart> characterParts, FnVersion fnVersion, 
        EngineVersion uassetApiEngineVersion, string ueSkinsPackagePath)
        {
            byte[] hsUasset;
            byte[] hsUexp;

            IEnumerable<string> characterPartTypes = characterParts.Select(cp => cp.Type);
            if (characterPartTypes.Contains("Body") && characterPartTypes.Contains("Head") && characterPartTypes.Contains("Faceacc"))
            {
                hsUasset = TemplateLoader.GetEmbeddedFile(fnVersion.Name, "CookedUeAssets", "HsBodyHeadFaceAcc.uasset");
                hsUexp = TemplateLoader.GetEmbeddedFile(fnVersion.Name, "CookedUeAssets", "HsBodyHeadFaceAcc.uexp");
            }
            else if (characterPartTypes.Contains("Body") && characterPartTypes.Contains("Head") && characterPartTypes.Contains("Hat"))
            {
                hsUasset = TemplateLoader.GetEmbeddedFile(fnVersion.Name, "CookedUeAssets", "HsBodyHeadHat.uasset");
                hsUexp = TemplateLoader.GetEmbeddedFile(fnVersion.Name, "CookedUeAssets", "HsBodyHeadHat.uexp");
            }
            else
            {
                hsUasset = TemplateLoader.GetEmbeddedFile(fnVersion.Name, "CookedUeAssets", "HsBodyHead.uasset");
                hsUexp = TemplateLoader.GetEmbeddedFile(fnVersion.Name, "CookedUeAssets", "HsBodyHead.uexp");
            }

            File.WriteAllBytes(Path.Combine(contentFolderPath, $"HS_{codename}.uasset"), hsUasset);
            File.WriteAllBytes(Path.Combine(contentFolderPath, $"HS_{codename}.uexp"), hsUexp);

            Console.WriteLine("Editing the HS");

            var currentHs = new UAsset(Path.Combine(contentFolderPath, $"HS_{codename}.uasset"), uassetApiEngineVersion);
            var hsExport0 = (NormalExport)currentHs.Exports[0];
            var characterPartsArray = (ArrayPropertyData)hsExport0["CharacterParts"];
            var headCp = (SoftObjectPropertyData)characterPartsArray.Value[0];
            var bodyCp = (SoftObjectPropertyData)characterPartsArray.Value[1];
            headCp.Value.AssetPath.AssetName.Value.Value =
            $"{ueSkinsPackagePath}/{codename}/CharacterParts/CP_Head_{codename}.CP_Head_{codename}";
            Console.WriteLine($"Changed the Head Character Part path in HS_{codename} to " +
            $"{ueSkinsPackagePath}/{codename}/CharacterParts/CP_Head_{codename}.CP_Head_{codename}");

            bodyCp.Value.AssetPath.AssetName.Value.Value =
            $"{ueSkinsPackagePath}/{codename}/CharacterParts/CP_Body_{codename}.CP_Body_{codename}";
            Console.WriteLine($"Changed the Body Character Part path in HS_{codename} to " +
            $"{ueSkinsPackagePath}/{codename}/CharacterParts/CP_Body_{codename}.CP_Body_{codename}");


            if (characterPartTypes.Contains("Faceacc"))
            {
                var faceAccCp = (SoftObjectPropertyData)characterPartsArray.Value[2];
                faceAccCp.Value.AssetPath.AssetName.Value.Value =
                $"{ueSkinsPackagePath}/{codename}/CharacterParts/CP_Faceacc_{codename}.CP_Faceacc_{codename}";
                Console.WriteLine($"Changed the FaceAcc Character Part path in HS_{codename} to " +
                $"{ueSkinsPackagePath}/{codename}/CharacterParts/CP_Faceacc_{codename}.CP_Faceacc_{codename}");

            }
            else if (characterPartTypes.Contains("Hat"))
            {
                var hatCp = (SoftObjectPropertyData)characterPartsArray.Value[2];
                hatCp.Value.AssetPath.AssetName.Value.Value =
                $"{ueSkinsPackagePath}/{codename}/CharacterParts/CP_Hat_{codename}.CP_Hat_{codename}";
                Console.WriteLine($"Changed the Hat Character Part path in HS_{codename} to " +
                $"{ueSkinsPackagePath}/{codename}/CharacterParts/CP_Hat_{codename}.CP_Hat_{codename}");
            }

            hsExport0.ObjectName.Value.Value = $"HS_{codename}";

            currentHs.Write(Path.Combine(contentFolderPath, $"HS_{codename}.uasset"));
            Log.Success($"Successfuly edited HS_{codename}.uasset and HS_{codename}.uexp");
        }

        internal static void CreateLobbyAnimationMontage(string contentFolderPath, string codename, string lobbyAnimationPsa, string lobbyAnimationJson, float lobbyAnimationLength, 
        FnVersion fnVersion, EngineVersion uassetApiEngineVersion, string ueSkinsPackagePath)
        {
            if (string.IsNullOrEmpty(lobbyAnimationPsa)) return;
            string idleAnimationUassetPath = Path.Combine(contentFolderPath, "Animations", $"{codename}_Idle_Montage.uasset");
            string idleAnimationUexpPath = Path.Combine(contentFolderPath, "Animations", $"{codename}_Idle_Montage.uexp");

            File.WriteAllBytes(idleAnimationUassetPath, TemplateLoader.GetEmbeddedFile(fnVersion.Name, "CookedUeAssets", "IdleMontage.uasset"));
            File.WriteAllBytes(idleAnimationUexpPath, TemplateLoader.GetEmbeddedFile(fnVersion.Name, "CookedUeAssets", "IdleMontage.uexp"));

            var currentIdleAnimation = new UAsset(idleAnimationUassetPath, uassetApiEngineVersion);
            Console.WriteLine($"Editing {codename}_Idle_Montage.uasset");

            var idleAnimationImport = currentIdleAnimation.Imports;
            var idleAnimationExport0 = (NormalExport)currentIdleAnimation.Exports[0];

            idleAnimationExport0.ObjectName.Value.Value = $"{codename}_Idle_Montage";
            idleAnimationImport[1].ObjectName.Value.Value = $"{codename}_Lobby_Animation";
            idleAnimationImport[1].ObjectName.Number = 0;
            Console.WriteLine($"Changed the animation name in {codename}_Idle_Montage to {codename}_Lobby_Animation");
            idleAnimationImport[3].ObjectName.Value.Value = $"{ueSkinsPackagePath}/{codename}/Animations/{codename}_Lobby_Animation";
            idleAnimationImport[3].ObjectName.Number = 0;
            Console.WriteLine($"Changed the animation path in {codename}_Idle_Montage to {ueSkinsPackagePath}/{codename}/Animations/{codename}_Lobby_Animation");

            var slotAnimTracks = (ArrayPropertyData)idleAnimationExport0["SlotAnimTracks"];
            var slotAnimTracks2 = (StructPropertyData)slotAnimTracks.Value[0];
            var AnimTrack = (StructPropertyData)slotAnimTracks2.Value[1];
            var AnimSegments = (ArrayPropertyData)AnimTrack.Value[0];
            var AnimSegments2 = (StructPropertyData)AnimSegments.Value[0];
            var AnimEndTime = (FloatPropertyData)AnimSegments2.Value[3];
            AnimEndTime.Value = (float)Math.Round(lobbyAnimationLength, 5);
            Console.WriteLine($"Changed the animation length in {codename}_Idle_Montage to {Math.Round(lobbyAnimationLength, 5)}");
            if (string.IsNullOrEmpty(lobbyAnimationJson)) idleAnimationExport0.Data.RemoveAt(5); // Remove DisableFaceOverride if no .json is provided
                                                                                                             // since there is no way to get the idle pose's facial animations
            currentIdleAnimation.Write(idleAnimationUassetPath);
            Log.Success($"Successfuly edited {codename}_Idle_Montage.uasset");
        }

        internal static void CreateHero(string contentFolderPath, string codename, string gender, string smallIcon, string largeIcon, FnVersion fnVersion, 
        EngineVersion uassetApiEngineVersion, string ueSkinsPackagePath)
        {
            Console.WriteLine("Editing HID...");
            string hidUassetPath = Path.Combine(contentFolderPath, $"HID_{codename}.uasset");
            string hidUexpPath = Path.Combine(contentFolderPath, $"HID_{codename}.uexp");
            File.WriteAllBytes(hidUassetPath, TemplateLoader.GetEmbeddedFile(fnVersion.Name, "CookedUeAssets", gender == "Male" ? "HidMale.uasset" : "HidFemale.uasset"));
            File.WriteAllBytes(hidUexpPath, TemplateLoader.GetEmbeddedFile(fnVersion.Name, "CookedUeAssets", gender == "Male" ? "HidMale.uexp" : "HidFemale.uexp"));

            var currentHid = new UAsset(hidUassetPath, uassetApiEngineVersion);
            var hidExport0 = (NormalExport)currentHid.Exports[0];
            hidExport0.ObjectName.Value.Value = $"HID_{codename}";
            var hidSmallIcon = (SoftObjectPropertyData)hidExport0["SmallPreviewImage"];
            var hidLargeIcon = (SoftObjectPropertyData)hidExport0["LargePreviewImage"];
            hidSmallIcon.Value.AssetPath.AssetName.Value.Value =
            $"{ueSkinsPackagePath}/{codename}/Textures/{smallIcon}.{smallIcon}";
            Console.WriteLine($"Changed the Small Icon path in HID_{codename} to " +
            $"{ueSkinsPackagePath}/{codename}/Textures/{smallIcon}.{smallIcon}");
            hidLargeIcon.Value.AssetPath.AssetName.Value.Value =
            $"{ueSkinsPackagePath}/{codename}/Textures/{largeIcon}.{largeIcon}";
            Console.WriteLine($"Changed the Large Icon path in HID_{codename} to " +
            $"{ueSkinsPackagePath}/{codename}/Textures/{largeIcon}.{largeIcon}");
            var hidSpecializationsArray = (ArrayPropertyData)hidExport0["Specializations"];
            var hidSpecialization = (SoftObjectPropertyData)hidSpecializationsArray.Value[0];
            hidSpecialization.Value.AssetPath.AssetName.Value.Value =
            $"{ueSkinsPackagePath}/{codename}/HS_{codename}.HS_{codename}";
            Console.WriteLine($"Changed the Hero Specialization path in HID_{codename} to " +
            $"{ueSkinsPackagePath}/{codename}/HS_{codename}.HS_{codename}");
            var idleMontage = (SoftObjectPropertyData)hidExport0["FrontendAnimMontageIdleOverride"];
            idleMontage.Value.AssetPath.AssetName.Value.Value =
            $"{ueSkinsPackagePath}/{codename}/Animations/{codename}_Idle_Montage.{codename}_Idle_Montage";

            currentHid.Write(hidUassetPath);
            Log.Success($"Successfuly edited HID_{codename}.uasset and HID_{codename}.uexp");

        }

        internal static void CreateCharacter(string outputFnGamePath, string cid, string codename, string name, string description, 
        string skinRarity, string series, FnVersion fnVersion, EngineVersion uassetApiEngineVersion, string ueSkinsPackagePath)
        {
            Console.WriteLine($"Editing {cid}.uasset");
            string cidPath = Path.Combine(outputFnGamePath, "Content", "Athena", "Items",
            "Cosmetics", "Characters");
            if (!Path.Exists(cidPath)) Directory.CreateDirectory(cidPath);
            string cidUassetPath = Path.Combine(cidPath, $"{cid}.uasset");
            string cidUexpPath = Path.Combine(cidPath, $"{cid}.uexp");
            File.WriteAllBytes(cidUassetPath, TemplateLoader.GetEmbeddedFile(fnVersion.Name, "CookedUeAssets", "Cid.uasset"));
            File.WriteAllBytes(cidUexpPath, TemplateLoader.GetEmbeddedFile(fnVersion.Name, "CookedUeAssets", "Cid.uexp"));

            var currentCid = new UAsset(cidUassetPath, uassetApiEngineVersion);
            var cidExport0 = (NormalExport)currentCid.Exports[0];
            var cidImport = currentCid.Imports;
            cidImport[fnVersion.HidNameIndex].ObjectName.Value.Value = $"HID_{codename}";
            Console.WriteLine($"Changed the Hero Id in {cid} to HID_{codename}");
            cidImport[fnVersion.HidPathIndex].ObjectName.Value.Value = $"{ueSkinsPackagePath}/{codename}/HID_{codename}";
            Console.WriteLine($"Changed the Hero Id path in {cid} to " +
            $"{ueSkinsPackagePath}/{codename}/HID_{codename}");

            cidExport0.ObjectName.Value.Value = cid;
            var rarity = (EnumPropertyData)cidExport0["Rarity"];
            rarity.Value.Value.Value = $"EFortRarity::{skinRarity}";

            if (skinRarity == "Uncommon") cidExport0.Data.RemoveAt(1); //Removes the rarity property since no rarity is equal to uncommon in fn
            else if (skinRarity == "Unattainable (Impossible T7)") rarity.Value.Value.Value = $"EFortRarity::Unattainable";
            if ((fnVersion.Name == "8.51-9.10" || fnVersion.Name == "9.41") && skinRarity != "Uncommon")
            {
                string rarityCodename = "";
                if (skinRarity == "Common") rarityCodename = "Handmade";
                else if (skinRarity == "Rare") rarityCodename = "Sturdy";
                else if (skinRarity == "Epic") rarityCodename = "Quality";
                else if (skinRarity == "Legendary") rarityCodename = "Fine";
                else if (skinRarity == "Mythic") rarityCodename = "Elegant";
                else if (skinRarity == "Transcendent") rarityCodename = "Masterwork";
                else if (skinRarity == "Unattainable (Impossible T7)") rarityCodename = "Epic";
                rarity.Value.Value.Value = $"EFortRarity::{rarityCodename}";
            }

            Console.WriteLine($"Changed the Rarity in {cid} to {skinRarity}");
            ((TextPropertyData)cidExport0["DisplayName"]).CultureInvariantString.Value = name;
            Console.WriteLine($"Changed the DisplayName in {cid} to {name}");
            ((TextPropertyData)cidExport0["Description"]).CultureInvariantString.Value = description;
            Console.WriteLine($"Changed the Description in {cid} to {description}");
            string displayNameKey = Guid.NewGuid().ToString("N").ToUpper(); //Generates a new key for the display name since multiple display names can't use the same key
            string descriptionKey = Guid.NewGuid().ToString("N").ToUpper();
            ((TextPropertyData)cidExport0["DisplayName"]).Value.Value = displayNameKey;
            ((TextPropertyData)cidExport0["Description"]).Value.Value = descriptionKey;
            cidExport0.Data.RemoveAt(skinRarity == "Uncommon" ? 4 : 5); //Removes gameplay tags

            if (series == "None") cidExport0.Data.RemoveAt(skinRarity == "Uncommon" ? 5 : 6);
            else
            {
                string seriesCodename = SeriesCodenames.GetValueOrDefault(series) ?? series;
                cidImport[3].ObjectName.Value.Value = seriesCodename;
                cidImport[5].ObjectName.Value.Value = $"/Game/Athena/Items/Cosmetics/Series/{seriesCodename}";
                Console.WriteLine($"Changed the Series in {cid} to {series}");
            }

            currentCid.Write(cidUassetPath);
            Log.Success($"Successfuly edited {cid}.uasset");
        }
    }
}
