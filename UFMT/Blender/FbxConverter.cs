#pragma warning disable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UFMT;
using UFMT.Blender;
using UFMT.Core;
using UFMT.FnAssets;

namespace UFMT.Blender
{
    internal static class FbxConverter
    {
        private static string PskConvertScript = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "PythonScripts", "Blender_ConvertPsk.py");
        private static string PsaConvertScript = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "PythonScripts", "Blender_ConvertPsa.py");
        private static string ProperSkeletonBlendPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "proper_fn_skeleton.blend");
        internal static async Task<bool> ConvertPskToFbx(List<CharacterPart> characterParts, string sourcePath, string codename)
        {
            Console.WriteLine("Converting .psk files to .fbx");

            foreach (CharacterPart cp in characterParts)
            {
                string fbxFolderPath = Path.Combine(sourcePath, "Fbx", cp.Type);
                string exportName = $"{codename}_{cp.Type}";
                string fbxFilePath = Path.Combine(Path.Combine(fbxFolderPath, $"{exportName}.fbx"));
                if (!Directory.Exists(fbxFolderPath))
                {
                    Directory.CreateDirectory(fbxFolderPath);
                    Console.WriteLine($"Created {fbxFolderPath}");
                }

                cp.FbxPath = Path.Combine(fbxFolderPath, exportName);
                if (!File.Exists(fbxFilePath))
                {
                    string[] fbxFiles = Directory.GetFiles(fbxFolderPath, "*.fbx");
                    if (fbxFiles.Length > 1) 
                    {
                        Log.Error($"More than 1 fbx files found in {fbxFolderPath}, make sure there is only 1 mesh per character part!");
                        return false;
                    } 
                    else if (fbxFiles.Length == 1)
                    {
                        File.Move(fbxFiles[0], fbxFilePath);
                    }
                    else
                    {
                        await Task.Run(() =>
                        {
                            Process blender = Process.Start(App.Settings.BlenderPath, $"-b --python \"{PskConvertScript}\" -- \"{cp.PskPath}\" " +
                        $"\"{fbxFilePath}\"");
                            blender.WaitForExit();
                        });

                        Log.Success($"Succesfully converted " +
                        $"{Path.Combine(sourcePath, Path.GetFileName(cp.PskPath))}" +
                        $" to {fbxFilePath}!");
                    }
                }

                if (cp.Type == "Head")
                {
                    await ShapeKeyCombiner.CombineShapeKeys(cp.PskPath, fbxFilePath);
                }
            }
            return true;
        }

        internal static async Task<(bool isValid, string lobbyAnimationFbx, float lobbyAnimationLength)> ConvertPsaToFbx
        (string sourcePath, string codename, string lobbyAnimationFolderPath, string lobbyAnimationPsa)
        {
            string lobbyAnimationFbx = string.Empty;
            float animationLength = 0f;
            Console.WriteLine("Converting .psa Lobby animation to .fbx");
            await Task.Run(() =>
            {
                string fbxFolderPath = Path.Combine(sourcePath, "Fbx", "Lobby_Animation");
                if (!Directory.Exists(fbxFolderPath)) 
                {
                    Directory.CreateDirectory(fbxFolderPath);
                    Console.WriteLine($"Created \n{fbxFolderPath}\n");
                }
                string exportName = $"{codename}_Lobby_Animation";
                lobbyAnimationFbx = exportName;
                string psaFilePath = Path.Combine(lobbyAnimationFolderPath, $"{lobbyAnimationPsa}.psa");
                string fbxFilePath = Path.Combine(fbxFolderPath, $"{exportName}.fbx");
                string arguments = $"-b \"{ProperSkeletonBlendPath}\" --python \"{PsaConvertScript}\" -- \"{psaFilePath}\" \"{fbxFilePath}\"";

                ProcessStartInfo psi = new ProcessStartInfo(App.Settings.BlenderPath, arguments)
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process blender = Process.Start(psi))
                {
                    var stdoutTask = Task.Run(() => blender.StandardOutput.ReadToEnd());
                    var stderrTask = Task.Run(() => blender.StandardError.ReadToEnd());
                    blender.WaitForExit();
                    Task.WhenAll(stdoutTask, stderrTask).Wait();
                }

                string metaPath = fbxFilePath + ".meta";
                if (File.Exists(metaPath))
                {
                    string content = File.ReadAllText(metaPath).Trim();
                    if (int.TryParse(content, out int animLength))
                    {
                        animationLength = (float)animLength / 30; //Divide the animation length by 30 since it's in 30fps
                    }
                    File.Delete(metaPath);
                }
            });
            Log.Success($"Successfully converted the Lobby .psa animation to .fbx!");
            return (true, lobbyAnimationFbx, animationLength);
        }

    }
}
