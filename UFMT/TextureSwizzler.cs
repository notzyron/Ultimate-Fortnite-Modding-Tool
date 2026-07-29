#pragma warning disable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UFMT
{
    internal static class TextureSwizzler
    {
        internal static void SwizzleSpecularTextures(string texturesPath)
        {
            List<string> texturePaths = TextureCategorizer.GetTexturesBySuffix(texturesPath, "_S").Select(tex => Path.Combine(texturesPath, $"{tex}.png")).ToList();
            texturePaths.Add(Path.Combine(texturesPath, "Default_Specular.png"));
            var swizzledFolder = Directory.CreateDirectory(Path.Combine(texturesPath, "Swizzled"));
            swizzledFolder.Attributes |= System.IO.FileAttributes.Hidden;
            Parallel.ForEach(texturePaths, texPath => 
            {
                using Bitmap bmp = new Bitmap(texPath);
                var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
                var bmpData = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, bmp.PixelFormat);

                unsafe
                {
                    byte* ptr = (byte*)bmpData.Scan0;
                    int bytesPerPixel = System.Drawing.Image.GetPixelFormatSize(bmp.PixelFormat) / 8;
                    int totalBytes = bmpData.Stride * bmp.Height;

                    for (int i = 0; i < totalBytes; i += bytesPerPixel)
                    {
                        byte blue = ptr[i];
                        ptr[i] = ptr[i + 1];
                        ptr[i + 1] = blue;
                    }
                }

                bmp.UnlockBits(bmpData);
                bmp.Save(Path.Combine(texturesPath, "Swizzled", Path.GetFileName(texPath)));
                Console.WriteLine($"Swizzled {texPath}");
            });
        }
    }
}
