using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UFMT.UnrealEngine
{
    internal class UnrealExportEmoteData
    {
        public string MaleAnimationFbxPath { get; set; }
        public string MaleAnimationJsonPath { get; set; }
        public float MaleAnimationLength { get; set; }
        public string FemaleAnimationFbxPath { get; set; }
        public string FemaleAnimationJsonPath { get; set; }
        public float FemaleAnimationLength { get; set; }
        public string SoundWavPath { get; set; }
        public int SoundWavCompressionQuality { get; set; }
        public string IconTexturePaths { get; set; }
        public string Codename { get; set; }
        public string EID { get; set; } = string.Empty;
        public string UeEmotesPackagePath { get; set; }
    }
}
