using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UAssetAPI;

namespace UFMT
{
    internal static class BlenderPreviewRenderer
    {
        private static string MaleLobbyAnimPath = Path.Combine
        (AppDomain.CurrentDomain.BaseDirectory, "Assets", "LobbyAnimations", "Male_Commando_Idle_01.psa");
        
        private static string FemaleLobbyAnimPath = Path.Combine
        (AppDomain.CurrentDomain.BaseDirectory, "Assets", "LobbyAnimations", "Female_Commando_Idle_01.psa");

        private static string RenderScript = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "PythonScripts", "Blender_RenderPreviewCh1.py");

        internal static async Task RenderSkinPreviewImage(List<CharacterPart> characterParts, string gender, string lobbyAnimationFolderPath, string lobbyAnimationPsa, 
        ObservableCollection<Material> materials, string skinPath, string codename, string texturesPath, string fnVersion)
        {
            string[] pskPaths = characterParts.Select(cp => cp.PskPath).ToArray();

            List<string> texturePaths = new();
            List<bool> swizzleMaterials = new();
            List<string> materialNames = new();
            string lobbyAnimPath = gender == "Male" ? MaleLobbyAnimPath : FemaleLobbyAnimPath;
            lobbyAnimPath = lobbyAnimationPsa == string.Empty ? lobbyAnimPath :
            Path.Combine(lobbyAnimationFolderPath, $"{lobbyAnimationPsa}.psa");
            foreach (Material mat in materials)
            {
                materialNames.Add(mat.Name);
                texturePaths.Add(Path.Combine(texturesPath, $"{mat.SelectedDiffuse}.png"));
                texturePaths.Add(Path.Combine(texturesPath, $"{mat.SelectedMask}.png"));
                texturePaths.Add(Path.Combine(texturesPath, $"{mat.SelectedNormal}.png"));
                texturePaths.Add(Path.Combine(texturesPath, $"{mat.SelectedSpecular}.png"));
                swizzleMaterials.Add(mat.Swizzle);
            }

            var exportData = new BlenderExportData
            {
                Psks = pskPaths,
                Materials = materialNames,
                Textures = texturePaths,
                Swizzle = swizzleMaterials,
                RenderPath = Path.Combine(skinPath, "Source", $"{codename}.png"),
                LobbyAnimPath = lobbyAnimPath,
                HeadPsk = characterParts.FirstOrDefault(cp => cp.Type == "Head").PskPath
            };

            string jsonString = System.Text.Json.JsonSerializer.Serialize(exportData, AppJsonContext.Default.BlenderExportData);

            // Base64 to prevent spaces or quotes 
            var plainTextBytes = Encoding.UTF8.GetBytes(jsonString);
            string base64Json = Convert.ToBase64String(plainTextBytes);

            if (fnVersion == "8.51-9.10" || fnVersion == "9.41") RenderScript = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "PythonScripts", "Blender_RenderPreviewCh1.py");
            else RenderScript = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "PythonScripts", "Blender_RenderPreviewCh2.py");
            string arguments = $"-b --python \"{RenderScript}\" -- {base64Json}";

            Console.WriteLine("Rendering the preview...");
            await Task.Run(() =>
            {
                Process blender = Process.Start(App.Settings.BlenderPath, arguments);
                blender.WaitForExit();
            });
            Log.Success("Successfully Rendered the preview image!");

            await Task.Delay(10);
        }
    }
}
