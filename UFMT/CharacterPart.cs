using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace UFMT
{
    public class CharacterPart
    {
        public string Type { get; set; }
        [JsonIgnore]
        public string PskPath { get; set; } = string.Empty;
        public string Psk { get; set; } = string.Empty;
        [JsonIgnore]
        public string FbxPath { get; set; } = string.Empty;
        public List<string> PhysicsAssets { get; set; } = new();
        [JsonIgnore]
        public List<string> PhysicsAssetJsonPaths { get; set; } = new();

        [JsonIgnore]
        public byte[] UassetFile { get; set; } = { };
        [JsonIgnore]
        public byte[] UexpFile { get; set; } = { };
    }
}
