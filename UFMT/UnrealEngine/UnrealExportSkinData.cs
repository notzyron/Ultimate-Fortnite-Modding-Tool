using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UFMT.UnrealEngine
{
    internal class UnrealExportSkinData
    {
        public List<string> FbxPaths { get; set; }
        public List<string> PhysicsMeshNames { get; set; }
        public List<List<string>> PhysicsAssetsPaths { get; set; }
        public List<string> DiffuseTextures { get; set; }
        public List<string> MaskTextures { get; set; }
        public List<string> NormalTextures { get; set; }
        public List<string> SpecularTextures { get; set; }
        public List<string> IconTextures { get; set; }
        public List<string> Materials { get; set; }
        public string Codename { get; set; }
        public List<string> MeshNames { get; set; }
        public string CID { get; set; } = string.Empty;
        public string LobbyAnimationFbxPath { get; set; } = string.Empty;
        public string LobbyAnimationJsonPath { get; set; } = string.Empty;
        public string RetargetSource { get; set; }
        public string HeadMeshName { get; set; }
        public string CurrentFnVersion { get; set; }
        public string UeSkinsPackagePath { get; set; }
    }

}
