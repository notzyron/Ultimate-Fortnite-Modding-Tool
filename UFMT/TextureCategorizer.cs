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
            List<string> textures = Directory.GetFiles(texturesPath, "*.png").Select(tex => Path.GetFileNameWithoutExtension(tex)).Where(tex => tex.EndsWith(suffix)).ToList();
            if (textures.Count == 0) Log.Warning($"No textures found with the suffix \"{suffix}\"!");
            return textures;
        }

        internal static List<string> GetTexturesByMultipleSuffix(string texturesPath, List<string> suffixes)
        {
            List<string> textures = 
            Directory.GetFiles(texturesPath, "*.png").Select(tex => Path.GetFileNameWithoutExtension(tex)).Where(tex => suffixes.Any(suffix => tex.EndsWith(suffix))).ToList();
            if (textures.Count == 0) suffixes.ForEach(suffix => Log.Warning($"No textures found with the suffix \"{suffix}\"!"));

            return textures;
        }

        internal static (string largeIcon, string smallIcon) GetIconTextures(string texturesPath)
        {
            string largeIcon;
            string smallIcon;
            List<string> textures = Directory.GetFiles(texturesPath, "*.png").ToList().Select(tex => Path.GetFileNameWithoutExtension(tex)).ToList();
            largeIcon = textures.FirstOrDefault(tex => (tex.ToLower().StartsWith("t-soldier") || tex.ToLower().StartsWith("t_soldier")) && 
            (tex.ToLower().EndsWith("-l") || tex.ToLower().EndsWith("_l")));
            smallIcon = textures.FirstOrDefault(tex => (tex.ToLower().StartsWith("t-soldier") || tex.ToLower().StartsWith("t_soldier")) && 
            (!tex.ToLower().EndsWith("-l") && !tex.ToLower().EndsWith("_l")));

            if (largeIcon == null && smallIcon != null)
            {
                largeIcon = smallIcon;
                Log.Warning($"Cannot find the large icon, small icon will be used for the large icon as well.");
            }
            else if (smallIcon == null && largeIcon != null)
            {
                smallIcon = largeIcon;
                Log.Warning($"Cannot find the small icon, large icon will be used for the small icon as well.");
            }
            else if (smallIcon == null && largeIcon == null)
            {
                Log.Warning("Cannot find the icons for the skin, the skin won't have any icons!");
                return (string.Empty, string.Empty);
            }

            return (largeIcon, smallIcon);
        }

        internal static List<string> GetAllTextures (string texturesPath)
        {
            Console.WriteLine($"Searching for valid textures inside {texturesPath}");
            List<string> allTextures = Directory.GetFiles(texturesPath, "*.png").ToList().Select(tex => Path.GetFileNameWithoutExtension(tex)).ToList();
            if (allTextures.Count == 0) Log.Warning($"No .png files found inside \"{texturesPath}\", the skin won't have any textures.");
            return allTextures;
        }
    }
}
