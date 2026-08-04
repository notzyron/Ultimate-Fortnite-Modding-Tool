using System;
using System.Collections.Generic;
using System.IO;

namespace UFMT
{
    internal static class UnrealDependencySetup
    {
        internal static void CreateMissingFiles(string cookedAssetsPath, string codename, string ueBaseHeadPath, string fakeCidBase64, string baseMeshSkeletonBase64, 
        string baseMeshBase64, Dictionary<string, string> baseHeadBase64strings, string cookedCodeNamePath)
        {
            string fakeCIDTemplatePath = Path.Combine(Path.GetDirectoryName(App.Settings.UeProjectPath), "Content",
            "CID_Template.uasset");
            string BaseMeshSkeletonPath = Path.Combine(Path.GetDirectoryName(App.Settings.UeProjectPath), "Content",
            "Characters", "Player", "Male", "Male_Avg_Base", "Fortnite_M_Avg_Player_Skeleton.uasset");
            string BaseMeshPath = Path.Combine(Path.GetDirectoryName(App.Settings.UeProjectPath), "Content",
            "Characters", "Player", "Male", "Male_Avg_Base", "Fortnite_M_Avg_Player.uasset");
            string baseHeadPath = Path.Combine(Path.GetDirectoryName(App.Settings.UeProjectPath), ueBaseHeadPath);

            if (!File.Exists(fakeCIDTemplatePath))
            {
                File.WriteAllBytes(fakeCIDTemplatePath, Convert.FromBase64String(fakeCidBase64));
            }
            if (!File.Exists(BaseMeshSkeletonPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(BaseMeshSkeletonPath));
                File.WriteAllBytes(BaseMeshSkeletonPath, Convert.FromBase64String(baseMeshSkeletonBase64));
            }
            if (!File.Exists(BaseMeshPath))
            {
                File.WriteAllBytes(BaseMeshPath, Convert.FromBase64String(baseMeshBase64));
            }
            if (Directory.Exists(cookedCodeNamePath))
            {
                Directory.Delete(cookedCodeNamePath, true);
            }
            if (!Directory.Exists(baseHeadPath))
            {
                Directory.CreateDirectory(baseHeadPath);
            }

            foreach (var (fileName, base64String) in baseHeadBase64strings)
            {
                string filePath = Path.Combine(baseHeadPath, $"{fileName}.uasset");
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"{fileName}.uasset is missing, creating the file...");
                    File.WriteAllBytes(filePath, Convert.FromBase64String(base64String));
                }
            }
        }
    }
}
