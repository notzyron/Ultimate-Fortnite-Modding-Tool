using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using UFMT.Core;

namespace UFMT.Blender
{
    internal static class ShapeKeyCombiner
    {
        private static string CombineShapeKeysScript = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "PythonScripts", "Blender_CombineShapeKeys.py");
        internal static async Task CombineShapeKeys(string pskPath, string fbxFilePath)
        {
            await Task.Run(() =>
            {
                Process blender = Process.Start(App.Settings.BlenderPath, $"-b --python \"{CombineShapeKeysScript}\" -- \"{pskPath}\" \"{fbxFilePath}\"");
                blender.WaitForExit();
            });
            Log.Success($"Succesfully combined shape keys for {fbxFilePath}");
        }
    }
}
