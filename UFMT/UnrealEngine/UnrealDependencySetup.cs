using System;
using System.Collections.Generic;
using System.IO;
using UFMT.Core;

namespace UFMT.UnrealEngine
{
    internal static class UnrealDependencySetup
    {
        internal static void CreateMissingFiles(string ueProjectPath, string codename, string ueBaseHeadPath, string cookedCodenamePath, string UeVersionNumber, 
        string[] baseHeadFileNames)
        {
            string fakeCIDTemplatePath = Path.Combine(Path.GetDirectoryName(ueProjectPath), "Content",
            "CID_Template.uasset");
            string BaseMeshSkeletonPath = Path.Combine(Path.GetDirectoryName(ueProjectPath), "Content",
            "Characters", "Player", "Male", "Male_Avg_Base", "Fortnite_M_Avg_Player_Skeleton.uasset");
            string BaseMeshPath = Path.Combine(Path.GetDirectoryName(ueProjectPath), "Content",
            "Characters", "Player", "Male", "Male_Avg_Base", "Fortnite_M_Avg_Player.uasset");
            string baseHeadPath = Path.Combine(Path.GetDirectoryName(ueProjectPath), ueBaseHeadPath);
            string mediumLodSettingsFolderPath = Path.Combine(Path.GetDirectoryName(ueProjectPath), "Content", "Characters", "Player", "Common", "LODSettings");
            string mediumLodSettingsFilePath = Path.Combine(Path.GetDirectoryName(ueProjectPath), "Content", "Characters", "Player", "Common", "LODSettings", "Medium_Player_LODSettings.uasset");

            if (!File.Exists(fakeCIDTemplatePath)) File.WriteAllBytes(fakeCIDTemplatePath, TemplateLoader.GetEmbeddedFile(UeVersionNumber, "RawUeAssets", "FakeCID.uasset"));
            if (!File.Exists(BaseMeshSkeletonPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(BaseMeshSkeletonPath));
                File.WriteAllBytes(BaseMeshSkeletonPath, TemplateLoader.GetEmbeddedFile(UeVersionNumber, "RawUeAssets", "BaseMeshSkeleton.uasset"));
            }
            if (!File.Exists(BaseMeshPath))
            {
                File.WriteAllBytes(BaseMeshPath, TemplateLoader.GetEmbeddedFile(UeVersionNumber, "RawUeAssets", "BaseMesh.uasset"));
            }
            if (Directory.Exists(cookedCodenamePath))
            {
                Directory.Delete(cookedCodenamePath, true);
            }
            if (!Directory.Exists(baseHeadPath))
            {
                Directory.CreateDirectory(baseHeadPath);
            }
            if (!Directory.Exists(mediumLodSettingsFolderPath))
            {
                Directory.CreateDirectory(mediumLodSettingsFolderPath);
            }
            if (!File.Exists(mediumLodSettingsFilePath))
            {
                File.WriteAllBytes(mediumLodSettingsFilePath, TemplateLoader.GetEmbeddedFile(UeVersionNumber, "RawUeAssets", "Medium_Player_LODSettings.uasset"));
            }

            foreach (string fileName in baseHeadFileNames)
            {
                string filePath = Path.Combine(baseHeadPath, $"{fileName}");
                File.WriteAllBytes(filePath, TemplateLoader.GetEmbeddedFile(UeVersionNumber, "RawUeAssets", fileName));
            }
        }
    }
}
