using System.Collections.Generic;

namespace UFMT
{
    public class CharacterPart
    {
        public string Type { get; set; }
        public string PskPath { get; set; } = string.Empty;
        [System.Text.Json.Serialization.JsonIgnore]
        public string FbxPath { get; set; } = string.Empty;
        public List<string> PhysicsAssetJsonPaths { get; set; } = new();

        [System.Text.Json.Serialization.JsonIgnore]
        public string UassetFileBase64 { get; set; } = string.Empty;
        [System.Text.Json.Serialization.JsonIgnore]
        public string UexpFileBase64 { get; set; } = string.Empty;
    }
}
