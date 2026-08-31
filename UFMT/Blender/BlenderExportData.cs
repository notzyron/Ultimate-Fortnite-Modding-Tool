using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UFMT.Blender
{
    public class BlenderExportData
    {
        public string[] Psks { get; set; }
        public List<string> Textures { get; set; }
        public List<bool> Swizzle { get; set; }
        public List<string> Materials { get; set; }
        public string RenderPath { get; set; }
        public string LobbyAnimPath { get; set; }
        public string HeadPsk { get; set; }
    }
}
