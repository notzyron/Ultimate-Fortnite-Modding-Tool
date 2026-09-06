#pragma warning disable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UFMT;
using UFMT.Blender;
using UFMT.FnAssets;
using UFMT.UI;

namespace UFMT.Blender
{
    internal static class PskReader
    {
        internal static List<Material> GetMaterialData
        (List<string> pskPaths, List<CharacterPart> characterParts, bool swizzleMats, SkinsPage parentPage)
        {
            List<string> alreadyUsedMatNames = new List<string>();
            List<Material> materials = new();

            for (int m = 0; m < pskPaths.Count; m++)
            {
                string pskPath = pskPaths[m];
                CharacterPart currentPskCp = characterParts[m];
                using var r = new BinaryReader(File.OpenRead(pskPath));

                while (r.BaseStream.Position < r.BaseStream.Length)
                {
                    string id = Encoding.ASCII.GetString(r.ReadBytes(24)).Trim();
                    int sz = r.ReadInt32();
                    int ct = r.ReadInt32();
                    if (id.Contains("MATT0000"))
                    {
                        for (int i = 0; i < ct; i++)
                        {
                            string matName = Encoding.ASCII.GetString(r.ReadBytes(64)).Trim('\0').Trim();
                            if (!alreadyUsedMatNames.Contains(matName))
                            {
                                materials.Add(new Material()
                                {
                                    Name = matName,
                                    ParentPage = parentPage,
                                    Cp = currentPskCp,
                                    Swizzle = swizzleMats
                                });
                            }
                            alreadyUsedMatNames.Add(matName);
                            r.BaseStream.Seek(sz - 64, SeekOrigin.Current);
                        }
                    }
                    else
                    {
                        r.BaseStream.Seek((long)sz * ct, SeekOrigin.Current);
                    }
                }
            }
            return materials;
        }
    }
}
