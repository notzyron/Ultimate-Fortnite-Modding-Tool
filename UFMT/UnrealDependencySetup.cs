using System;
using System.Collections.Generic;
using System.IO;

namespace UFMT
{
    internal static class UnrealDependencySetup
    {
        internal static void CreateMissingFiles(string cookedAssetsPath, string codename, string ueBaseHeadPath, string cookedCodenamePath, string UeVersionNumber, string[] baseHeadFileNames)
        {
            string fakeCIDTemplatePath = Path.Combine(Path.GetDirectoryName(App.Settings.UeProjectPath), "Content",
            "CID_Template.uasset");
            string BaseMeshSkeletonPath = Path.Combine(Path.GetDirectoryName(App.Settings.UeProjectPath), "Content",
            "Characters", "Player", "Male", "Male_Avg_Base", "Fortnite_M_Avg_Player_Skeleton.uasset");
            string BaseMeshPath = Path.Combine(Path.GetDirectoryName(App.Settings.UeProjectPath), "Content",
            "Characters", "Player", "Male", "Male_Avg_Base", "Fortnite_M_Avg_Player.uasset");
            string baseHeadPath = Path.Combine(Path.GetDirectoryName(App.Settings.UeProjectPath), ueBaseHeadPath);

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

            foreach (string fileName in baseHeadFileNames)
            {
                string filePath = Path.Combine(baseHeadPath, $"{fileName}");
                File.WriteAllBytes(filePath, TemplateLoader.GetEmbeddedFile(UeVersionNumber, "RawUeAssets", fileName));
            }
        }
    }
}
