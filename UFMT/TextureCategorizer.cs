#pragma warning disable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UFMT
{
    internal static class TextureCategorizer
    {
        internal static List<string> GetTexturesBySuffix(string texturesPath, string suffix)
        {
            List<string> texturePaths = Directory.GetFiles(texturesPath, "*.png").Where(tex => Path.GetFileNameWithoutExtension(tex).EndsWith(suffix)).ToList();
            if (texturePaths.Count == 0) Log.Warning($"No textures found with the suffix \"{suffix}\"!");

            return texturePaths;
        }
    }
}
