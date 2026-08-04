using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UFMT.Helper;

namespace UFMT
{
    internal static class AssetRegistryBuilder
    {
        internal static void CreateAssetRegistry(string cookedAssetsPath, string cidJsonBase64, string assetRegistryBinBase64, string skinPath, string outputFnGamePath)
        {
            Console.WriteLine("Creating AssetRegistry.bin!");
            //Just in case the user has the project folder named differently than the .uproject

            string[] customSkinFolders = Directory.GetDirectories(Path.Combine(cookedAssetsPath, "CustomSkins"));
            List<string> jsonCids = new();

            Console.WriteLine($"Searching for CIDs inside {cookedAssetsPath}...");
            foreach (string customSkinFolder in customSkinFolders)
            {
                string[] cid = Directory.GetFiles(customSkinFolder, "*.uasset");
                if (cid.Length > 0)
                {
                    string currentFoundCid = Path.GetFileNameWithoutExtension(cid[0]);

                    string json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(cidJsonBase64));
                    var root = JObject.Parse(json);
                    root["ObjectPath"] = root["ObjectPath"]!.Value<string>()!.Replace(
                        "/Game/Athena/Items/Cosmetics/Characters/CID_Template.CID_Template",
                        $"/Game/Athena/Items/Cosmetics/Characters/{currentFoundCid}.{currentFoundCid}");
                    root["PackageName"] = root["PackageName"]!.Value<string>()!.Replace(
                        "/Game/Athena/Items/Cosmetics/Characters/CID_Template",
                        $"/Game/Athena/Items/Cosmetics/Characters/{currentFoundCid}");
                    root["AssetName"] = root["PackageName"]!.Value<string>()!.Replace(
                        "CID_Template", currentFoundCid);
                    var tagAndValue = root["TagAndValue"]!.ToArray();
                    foreach (var tag in tagAndValue)
                    {
                        if (tag["Item1"]!.Value<string>() == "PrimaryAssetName")
                        {
                            tag["Item2"] = currentFoundCid;
                            break;
                        }
                    }
                    jsonCids.Add(root.ToString(Formatting.Indented));
                    Console.WriteLine($"Added {currentFoundCid} to the AssetRegistry.bin!");
                }
                else
                {
                    Log.Warning($"no uasset files found inside {customSkinFolder}");
                }
            }

            DeleteOldFnGamePath(skinPath);

            if (!Directory.Exists(outputFnGamePath))
            {
                Directory.CreateDirectory(outputFnGamePath);
                Console.WriteLine($"Created {outputFnGamePath}!");
            }

            AssetRegistryHelper.Inject(assetRegistryBinBase64, jsonCids.ToArray(), Path.Combine(outputFnGamePath, "AssetRegistry312398E80AB6209B22CAA2EBAB2DB35B.bin"));
        }

        private static void DeleteOldFnGamePath(string skinPath)
        {
            string oldOutputPath = Path.Combine(skinPath, "Output", "FortniteGame");
            if (Directory.Exists(oldOutputPath)) Directory.Delete(oldOutputPath, true); //For OG users ;)
        }
    }
}
