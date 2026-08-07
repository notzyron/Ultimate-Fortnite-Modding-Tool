#pragma warning disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UFMT
{
    public record class FnVersion
    {
        public bool ManuallySwizzleMaterials = false;
        public int DiffusePathIndex = 4;
        public int DiffuseNameIndex = 9;
        public int HidNameIndex = 2;
        public int HidPathIndex = 4;
        public string Name;
    };

    public class FnVersionsData
    {
        public static FnVersion v14_30 = new FnVersion()
        {
            DiffusePathIndex = 5,
            DiffuseNameIndex = 18,
            Name = "14.30"
        };
        public static FnVersion v13_40 = new FnVersion()
        {
            ManuallySwizzleMaterials = true,
            DiffusePathIndex = 37,
            DiffuseNameIndex = 160,
            Name = "13.40"
        };
        public static FnVersion v8_51 = new()
        {
            ManuallySwizzleMaterials = true,
            Name = "8.51-9.10"
        };
        public static FnVersion v9_41 = new()
        {
            ManuallySwizzleMaterials = false,
            Name = "9.41"
        };
        public static FnVersion v12_41 = new()
        {
            ManuallySwizzleMaterials = true,
            Name = "12.41"
        };

        public static Dictionary<string, FnVersion> FnVersions = new() { {"14.30", v14_30 }, { "13.40", v13_40 }, { "8.51-9.10", v8_51 }, { "9.41", v9_41 }, 
        { "12.41", v12_41 } };
    }
}
