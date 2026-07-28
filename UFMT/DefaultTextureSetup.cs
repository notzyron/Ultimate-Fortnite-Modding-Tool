#pragma warning disable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UFMT
{
    internal static class DefaultTextureSetup
    {
        private static Dictionary<string, int[]> DefaultTexturesColors = new() //The colors are in ARGB
        {
            {"Default_Diffuse", [255, 228, 228, 228]},
            {"Default_Mask", [255, 252, 172, 0]},
            {"Default_Normal", [255, 124, 130, 254]},
            {"Default_Specular", [255, 0, 0, 0]},
        };

        internal static List<string> FindMissingDefaultTextures(string texturesPath)
        {
            List<string> missingDefaultTextures = DefaultTexturesColors.Keys.Where(texName => !File.Exists(Path.Combine(texturesPath, $"{texName}.png"))).ToList();
            return missingDefaultTextures;
        }

        internal static void CreateDefaultTextures(List<string> missingDefaultTextures, string texturesPath)
        {
            foreach (string texture in missingDefaultTextures)
            {
                using (Bitmap bmp = new Bitmap(1, 1))
                {
                    int[] missingTextureColor = DefaultTexturesColors.GetValueOrDefault(Path.GetFileNameWithoutExtension(texture));
                    bmp.SetPixel(0, 0, Color.FromArgb(missingTextureColor[0], missingTextureColor[1], missingTextureColor[2], missingTextureColor[3]));
                    bmp.Save(Path.Combine(texturesPath, $"{texture}.png"), ImageFormat.Png);
                }
                Console.WriteLine($"Created {texture}");
            }
        }
    }
}
