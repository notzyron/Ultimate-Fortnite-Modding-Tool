using System;
using System.Collections.Generic;
using System.IO;
using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.PropertyTypes.Structs;
using UAssetAPI.UnrealTypes;
using System.Linq;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
using Microsoft.UI.Xaml;

namespace UFMT
{
    internal static class SkinAssetCreator
    {
        private static Dictionary<string, string> SeriesCodenames = new(){ {"Dark Series", "CUBESeries"}, { "Star Wars Series", "ColumbusSeries" },
        {"Icon Series", "CreatorCollabSeries"}, {"DC Series", "DCUSeries"}, {"Frozen Series", "FrozenSeries" }, {"Lava Series", "LavaSeries"},
        {"Marvel Series", "MarvelSeries"}, {"Shadow Series", "ShadowSeries"},  {"Slurp Series", "SlurpSeries"},
        {"Test Series", "FakeToken_FDS_Series"}, {"Anual Pass Series", "2020AnnualPassSeries"}};

        internal static void CopyFilesFromUe(string contentFolderPath, DirectoryInfo cookedCharacterDirectory, string cookedAssetsPath, string outputFnGamePath, 
        string baseHeadPath, bool replaceCookedBaseHead, Dictionary<string, string> cookedBaseHeadBase64Strings, string fortniteVersion)
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
            if (fortniteVersion == "9.41") cookedBaseHeadPath = Path.Combine(cookedAssetsPath, "Modding", "Base_Head"); // 9.41 uses a different location for base head

            string outputBaseHeadPath = Path.Combine(outputFnGamePath, baseHeadPath);

            if (!Directory.Exists(outputBaseHeadPath)) Directory.CreateDirectory(outputBaseHeadPath);

            if (replaceCookedBaseHead)
            {
                foreach (var (fileName, base64String) in cookedBaseHeadBase64Strings)
                {
                    File.WriteAllBytes(Path.Combine(cookedBaseHeadPath, fileName), Convert.FromBase64String(base64String));
                }
            } // Replace the files inside cooked ue folders

            foreach (string file in Directory.GetFiles(cookedBaseHeadPath))
            {
                File.Copy(file, Path.Combine(outputBaseHeadPath, Path.GetFileName(file)), true);
            }

            Log.Success($"Copied files from {cookedCharacterDirectory} to {contentFolderPath}");

        }

        internal static void CreateMaterials(string contentFolderPath, string codename, ObservableCollection<Material> materials, FnVersion fnVer, EngineVersion uassetApiEngineVersion)
        {
            string materialsPath = Path.Combine(contentFolderPath, "Materials");
            foreach (Material material in materials)
            {
                string uassetMaterialPath = Path.Combine(materialsPath, $"{material.Name}.uasset");
                string uexpMaterialPath = Path.Combine(materialsPath, $"{material.Name}.uexp");
                string materialUassetBase64;
                string materialUexpBase64;

                materialUassetBase64 = fnVer.MiNoSwizzleUassetBase64;
                materialUexpBase64 = fnVer.MiNoSwizzleUexpBase64;
                if (!fnVer.ManuallySwizzleMaterials && material.Swizzle)
                {
                    materialUassetBase64 = fnVer.MiSwizzleUassetBase64;
                    materialUexpBase64 = fnVer.MiSwizzleUexpBase64;
                }

                File.WriteAllBytes(Path.Combine(uassetMaterialPath),
                Convert.FromBase64String(materialUassetBase64));
                File.WriteAllBytes(Path.Combine(uexpMaterialPath),
                Convert.FromBase64String(materialUexpBase64));
                Log.Success($"Created material instance {material.Name}");
                Console.WriteLine($"Editing {material.Name}");

                var currentMi = new UAsset(uassetMaterialPath, uassetApiEngineVersion);
                var miImportData = currentMi.Imports;
                var miExportData = currentMi.Exports;
                var miExport0 = (NormalExport)currentMi.Exports[0];
                string fnTexturesPath = $"/Game/CustomSkins/{codename}/Textures/";
                miImportData[fnVer.DiffusePathIndex].ObjectName.Value.Value = Path.Combine(fnTexturesPath, material.SelectedDiffuse);
                Console.WriteLine($"Changed the diffuse texture path in {material.Name} to {Path.Combine(fnTexturesPath, material.SelectedDiffuse)}");
                miImportData[fnVer.DiffusePathIndex + 1].ObjectName.Value.Value = Path.Combine(fnTexturesPath, material.SelectedMask);
                Console.WriteLine($"Changed the mask texture path in {material.Name} to {Path.Combine(fnTexturesPath, material.SelectedMask)}");
                miImportData[fnVer.DiffusePathIndex + 2].ObjectName.Value.Value = Path.Combine(fnTexturesPath, material.SelectedNormal);
                Console.WriteLine($"Changed the normal texture path in {material.Name} to {Path.Combine(fnTexturesPath, material.SelectedNormal)}");
                miImportData[fnVer.DiffusePathIndex + 3].ObjectName.Value.Value = Path.Combine(fnTexturesPath, material.SelectedSpecular);
                Console.WriteLine($"Changed the specular texture path in {material.Name} to {Path.Combine(fnTexturesPath, material.SelectedSpecular)}");
                miImportData[fnVer.DiffuseNameIndex].ObjectName.Value.Value = material.SelectedDiffuse;
                Console.WriteLine($"Changed the diffuse texture in {material.Name} to {material.SelectedDiffuse}");
                miImportData[fnVer.DiffuseNameIndex + 1].ObjectName.Value.Value = material.SelectedMask;
                Console.WriteLine($"Changed the mask texture in {material.Name} to {material.SelectedMask}");
                miImportData[fnVer.DiffuseNameIndex + 2].ObjectName.Value.Value = material.SelectedNormal;
                Console.WriteLine($"Changed the normal texture in {material.Name} to {material.SelectedNormal}");
                miImportData[fnVer.DiffuseNameIndex + 3].ObjectName.Value.Value = material.SelectedSpecular;
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

        internal static void CreateCharacterParts(string contentFolderPath, string gender, string codename, List<CharacterPart> characterParts, string fortniteVersion, 
        FnVersion fnVer, EngineVersion uassetApiEngineVersion)
        {
            string characterPartsPath = Path.Combine(contentFolderPath, "CharacterParts");
            CharacterPart body = characterParts.FirstOrDefault(cp => cp.Type == "body");
            CharacterPart head = characterParts.FirstOrDefault(cp => cp.Type == "head");
            CharacterPart faceacc = characterParts.FirstOrDefault(cp => cp.Type == "faceacc");
            CharacterPart hat = characterParts.FirstOrDefault(cp => cp.Type == "hat");
            if (!Path.Exists(characterPartsPath)) Directory.CreateDirectory(characterPartsPath);

            if (gender == "Female")
            {
                body.UassetFileBase64 = fnVer.BodyCpFemaleUassetBase64;
                body.UexpFileBase64 = fnVer.BodyCpFemaleUexpBase64;
                head.UassetFileBase64 = fnVer.HeadCpFemaleUassetBase64;
                head.UexpFileBase64 = fnVer.HeadCpFemaleUexpBase64;
                if (faceacc != null)
                {
                    faceacc.UassetFileBase64 = fnVer.FaceAccCpFemaleUassetBase64;
                    faceacc.UexpFileBase64 = fnVer.FaceAccCpFemaleUexpBase64;
                }
            }
            else if (gender == "Male")
            {
                body.UassetFileBase64 = fnVer.BodyCpMaleUassetBase64;
                body.UexpFileBase64 = fnVer.BodyCpMaleUexpBase64;
                head.UassetFileBase64 = fnVer.HeadCpMaleUassetBase64;
                head.UexpFileBase64 = fnVer.HeadCpMaleUexpBase64;
                if (faceacc != null)
                {
                    faceacc.UassetFileBase64 = fnVer.FaceAccCpMaleUassetBase64;
                    faceacc.UexpFileBase64 = fnVer.FaceAccCpMaleUexpBase64;
                }
            }

            foreach (CharacterPart cp in characterParts)
            {
                Console.WriteLine($"Currently editing the {cp.Type} of the skin");
                string uassetPath = Path.Combine(characterPartsPath,
                $"CP_{cp.Type}_{codename}.uasset");
                string uexpPath = Path.Combine(characterPartsPath,
                $"CP_{cp.Type}_{codename}.uexp");

                File.WriteAllBytes(uassetPath, Convert.FromBase64String(cp.UassetFileBase64));
                File.WriteAllBytes(uexpPath, Convert.FromBase64String(cp.UexpFileBase64));

                var currentCp = new UAsset(uassetPath, uassetApiEngineVersion);
                var cpExport0 = (NormalExport)currentCp.Exports[0];
                var cpExport1 = (NormalExport)currentCp.Exports[1];
                cpExport1.ObjectName.Value.Value = $"CP_{cp.Type}_{codename}";
                if (cp.Type != "hat")
                {
                    string animBpPath;
                    if (cp.Type == "head")
                    {
                        if (fortniteVersion == "9.41") animBpPath = "/Game/Modding/Base_Head/Base_Head_Modding_AnimBP.Base_Head_Modding_AnimBP_C";
                        else animBpPath = "/Game/Base/Head/Skeleton/Base_Head_AnimBP.Base_Head_AnimBP_C";
                    }
                    else animBpPath = $"/Game/CustomSkins/{codename}/Meshes/{codename}_{cp.Type}_AnimBP.{codename}_{cp.Type}_AnimBP_C";

                    var animBpData = (SoftObjectPropertyData)cpExport0["AnimClass"];
                    animBpData.Value.AssetPath.AssetName.Value.Value = animBpPath;

                    Console.WriteLine($"Changed the Animation Blueprint in CP_{cp.Type}_{codename} to {animBpPath}");
                }
                var mesh = (SoftObjectPropertyData)cpExport1["SkeletalMesh"];
                mesh.Value.AssetPath.AssetName.Value.Value = $"/Game/CustomSkins/{codename}/Meshes/" +
                $"{codename}_{cp.Type}.{codename}_{cp.Type}";
                Console.WriteLine($"Changed the Mesh in CP_{cp.Type}_{codename} to /Game/CustomSkins/{codename}/Meshes/" +
                $"{codename}_{cp.Type}.{codename}_{cp.Type}");

                Console.WriteLine(uassetPath);
                currentCp.Write(uassetPath);
                Log.Success($"Successfully edited CP_{cp.Type}_{codename}.uasset and " +
                $"CP_{cp.Type}_{codename}.uexp");
            }
        }

        internal static void CreateHeroSpecialization(string contentFolderPath, string codename, List<CharacterPart> characterParts, FnVersion fnVer, EngineVersion uassetApiEngineVersion)
        {
            string hsUassetBase64;
            string hsUexpBase64;

            IEnumerable<string> characterPartTypes = characterParts.Select(cp => cp.Type);
            if (characterPartTypes.Contains("body") && characterPartTypes.Contains("head") && characterPartTypes.Contains("faceacc"))
            {
                hsUassetBase64 = fnVer.HsBodyHeadFaceAccUassetBase64;
                hsUexpBase64 = fnVer.HsBodyHeadFaceAccUexpBase64;
            }
            else if (characterPartTypes.Contains("body") && characterPartTypes.Contains("head") && characterPartTypes.Contains("hat"))
            {
                hsUassetBase64 = fnVer.HsBodyHeadHatUassetBase64;
                hsUexpBase64 = fnVer.HsBodyHeadHatUexpBase64;
            }
            else
            {
                hsUassetBase64 = fnVer.HsBodyHeadUassetBase64;
                hsUexpBase64 = fnVer.HsBodyHeadUexpBase64;
            }

            File.WriteAllBytes(Path.Combine(contentFolderPath, $"HS_{codename}.uasset"), Convert.FromBase64String(hsUassetBase64));
            File.WriteAllBytes(Path.Combine(contentFolderPath, $"HS_{codename}.uexp"), Convert.FromBase64String(hsUexpBase64));

            Console.WriteLine("Editing the HS");

            var currentHs = new UAsset(Path.Combine(contentFolderPath, $"HS_{codename}.uasset"), uassetApiEngineVersion);
            var hsExport0 = (NormalExport)currentHs.Exports[0];
            var characterPartsArray = (ArrayPropertyData)hsExport0["CharacterParts"];
            var headCp = (SoftObjectPropertyData)characterPartsArray.Value[0];
            var bodyCp = (SoftObjectPropertyData)characterPartsArray.Value[1];
            headCp.Value.AssetPath.AssetName.Value.Value =
            $"/Game/CustomSkins/{codename}/CharacterParts/CP_head_{codename}.CP_head_{codename}";
            Console.WriteLine($"Changed the Head Character Part path in HS_{codename} to " +
            $"/Game/CustomSkins/{codename}/CharacterParts/CP_head_{codename}.CP_head_{codename}");

            bodyCp.Value.AssetPath.AssetName.Value.Value =
            $"/Game/CustomSkins/{codename}/CharacterParts/CP_body_{codename}.CP_body_{codename}";
            Console.WriteLine($"Changed the Body Character Part path in HS_{codename} to " +
            $"/Game/CustomSkins/{codename}/CharacterParts/CP_body_{codename}.CP_body_{codename}");


            if (characterPartTypes.Contains("faceacc"))
            {
                var faceAccCp = (SoftObjectPropertyData)characterPartsArray.Value[2];
                faceAccCp.Value.AssetPath.AssetName.Value.Value =
                $"/Game/CustomSkins/{codename}/CharacterParts/CP_faceacc_{codename}.CP_faceacc_{codename}";
                Console.WriteLine($"Changed the FaceAcc Character Part path in HS_{codename} to " +
                $"/Game/CustomSkins/{codename}/CharacterParts/CP_faceacc_{codename}.CP_faceacc_{codename}");

            }
            else if (characterPartTypes.Contains("hat"))
            {
                var hatCp = (SoftObjectPropertyData)characterPartsArray.Value[2];
                hatCp.Value.AssetPath.AssetName.Value.Value =
                $"/Game/CustomSkins/{codename}/CharacterParts/CP_hat_{codename}.CP_hat_{codename}";
                Console.WriteLine($"Changed the Hat Character Part path in HS_{codename} to " +
                $"/Game/CustomSkins/{codename}/CharacterParts/CP_hat_{codename}.CP_hat_{codename}");
            }

            hsExport0.ObjectName.Value.Value = $"HS_{codename}";

            currentHs.Write(Path.Combine(contentFolderPath, $"HS_{codename}.uasset"));
            Log.Success($"Successfuly edited HS_{codename}.uasset and HS_{codename}.uexp");
        }

        internal static void CreateLobbyAnimationMontage(string contentFolderPath, string codename, string lobbyAnimationPsa, string lobbyAnimationJson, float lobbyAnimationLength, 
        FnVersion fnVer, EngineVersion uassetApiEngineVersion)
        {
            if (string.IsNullOrEmpty(lobbyAnimationPsa)) return;
            string idleAnimationUassetPath = Path.Combine(contentFolderPath, "Animations", $"{codename}_Idle_Montage.uasset");
            string idleAnimationUexpPath = Path.Combine(contentFolderPath, "Animations", $"{codename}_Idle_Montage.uexp");

            File.WriteAllBytes(idleAnimationUassetPath, Convert.FromBase64String(fnVer.IdleMontageUassetBase64));
            File.WriteAllBytes(idleAnimationUexpPath, Convert.FromBase64String(fnVer.IdleMontageUexpBase64));

            var currentIdleAnimation = new UAsset(idleAnimationUassetPath, uassetApiEngineVersion);
            Console.WriteLine($"Editing {codename}_Idle_Montage.uasset");

            var idleAnimationImport = currentIdleAnimation.Imports;
            var idleAnimationExport0 = (NormalExport)currentIdleAnimation.Exports[0];

            idleAnimationExport0.ObjectName.Value.Value = $"{codename}_Idle_Montage";
            idleAnimationImport[1].ObjectName.Value.Value = $"{codename}_Lobby_Animation";
            idleAnimationImport[1].ObjectName.Number = 0;
            Console.WriteLine($"Changed the animation name in {codename}_Idle_Montage to {codename}_Lobby_Animation");
            idleAnimationImport[3].ObjectName.Value.Value = $"/Game/CustomSkins/{codename}/Animations/{codename}_Lobby_Animation";
            idleAnimationImport[3].ObjectName.Number = 0;
            Console.WriteLine($"Changed the animation path in {codename}_Idle_Montage to /Game/CustomSkins/{codename}/Animations/{codename}_Lobby_Animation");

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

        internal static void CreateHero(string contentFolderPath, string codename, string gender, string smallIcon, string largeIcon, FnVersion fnVer, EngineVersion uassetApiEngineVersion)
        {
            Console.WriteLine("Editing HID...");
            string hidUassetPath = Path.Combine(contentFolderPath, $"HID_{codename}.uasset");
            string hidUexpPath = Path.Combine(contentFolderPath, $"HID_{codename}.uexp");
            File.WriteAllBytes(hidUassetPath, Convert.FromBase64String
            (gender == "Male" ? fnVer.HidMaleUassetBase64 : fnVer.HidFemaleUassetBase64));
            File.WriteAllBytes(hidUexpPath, Convert.FromBase64String
            (gender == "Male" ? fnVer.HidMaleUexpBase64 : fnVer.HidFemaleUexpBase64));

            var currentHid = new UAsset(hidUassetPath, uassetApiEngineVersion);
            var hidExport0 = (NormalExport)currentHid.Exports[0];
            hidExport0.ObjectName.Value.Value = $"HID_{codename}";
            var hidSmallIcon = (SoftObjectPropertyData)hidExport0["SmallPreviewImage"];
            var hidLargeIcon = (SoftObjectPropertyData)hidExport0["LargePreviewImage"];
            hidSmallIcon.Value.AssetPath.AssetName.Value.Value =
            $"/Game/CustomSkins/{codename}/Textures/{smallIcon}.{smallIcon}";
            Console.WriteLine($"Changed the Small Icon path in HID_{codename} to " +
            $"/Game/CustomSkins/{codename}/Textures/{smallIcon}.{smallIcon}");
            hidLargeIcon.Value.AssetPath.AssetName.Value.Value =
            $"/Game/CustomSkins/{codename}/Textures/{largeIcon}.{largeIcon}";
            Console.WriteLine($"Changed the Large Icon path in HID_{codename} to " +
            $"/Game/CustomSkins/{codename}/Textures/{largeIcon}.{largeIcon}");
            var hidSpecializationsArray = (ArrayPropertyData)hidExport0["Specializations"];
            var hidSpecialization = (SoftObjectPropertyData)hidSpecializationsArray.Value[0];
            hidSpecialization.Value.AssetPath.AssetName.Value.Value =
            $"/Game/CustomSkins/{codename}/HS_{codename}.HS_{codename}";
            Console.WriteLine($"Changed the Hero Specialization path in HID_{codename} to " +
            $"/Game/CustomSkins/{codename}/HS_{codename}.HS_{codename}");
            var idleMontage = (SoftObjectPropertyData)hidExport0["FrontendAnimMontageIdleOverride"];
            idleMontage.Value.AssetPath.AssetName.Value.Value =
            $"/Game/CustomSkins/{codename}/Animations/{codename}_Idle_Montage.{codename}_Idle_Montage";

            currentHid.Write(hidUassetPath);
            Log.Success($"Successfuly edited HID_{codename}.uasset and HID_{codename}.uexp");

        }

        internal static void CreateCharacter(string outputFnGamePath, string cid, string codename, string name, string description, 
        string skinRarity, string series, string fortniteVersion, FnVersion fnVer, EngineVersion uassetApiEngineVersion)
        {
            Console.WriteLine($"Editing {cid}.uasset");
            string cidPath = Path.Combine(outputFnGamePath, "Content", "Athena", "Items",
            "Cosmetics", "Characters");
            if (!Path.Exists(cidPath)) Directory.CreateDirectory(cidPath);
            string cidUassetPath = Path.Combine(cidPath, $"{cid}.uasset");
            string cidUexpPath = Path.Combine(cidPath, $"{cid}.uexp");
            File.WriteAllBytes(cidUassetPath, Convert.FromBase64String(fnVer.CidUassetBase64));
            File.WriteAllBytes(cidUexpPath, Convert.FromBase64String(fnVer.CidUexpBase64));

            var currentCid = new UAsset(cidUassetPath, uassetApiEngineVersion);
            var cidExport0 = (NormalExport)currentCid.Exports[0];
            var cidImport = currentCid.Imports;
            cidImport[fnVer.HidNameIndex].ObjectName.Value.Value = $"HID_{codename}";
            Console.WriteLine($"Changed the Hero Id in {cid} to HID_{codename}");
            cidImport[fnVer.HidPathIndex].ObjectName.Value.Value = $"/Game/CustomSkins/{codename}/HID_{codename}";
            Console.WriteLine($"Changed the Hero Id path in {cid} to " +
            $"/Game/CustomSkins/{codename}/HID_{codename}");

            cidExport0.ObjectName.Value.Value = cid;
            var rarity = (EnumPropertyData)cidExport0["Rarity"];
            rarity.Value.Value.Value = $"EFortRarity::{skinRarity}";

            if (skinRarity == "Uncommon") cidExport0.Data.RemoveAt(1); //Removes the rarity property since no rarity is equal to uncommon in fn
            else if (skinRarity == "Unattainable (Impossible T7)") rarity.Value.Value.Value = $"EFortRarity::Unattainable";
            if ((fortniteVersion == "8.51-9.10" || fortniteVersion == "9.41") && skinRarity != "Uncommon")
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
                cidImport[3].ObjectName.Value.Value = SeriesCodenames.GetValueOrDefault(series);
                cidImport[5].ObjectName.Value.Value = $"/Game/Athena/Items/Cosmetics/Series/{SeriesCodenames.GetValueOrDefault(series)}";
                Console.WriteLine($"Changed the Series in {cid} to {series}");
            }

            currentCid.Write(cidUassetPath);
            Log.Success($"Successfuly edited {cid}.uasset");
        }
    }
}
