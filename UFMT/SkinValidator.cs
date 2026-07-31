using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UFMT
{
    internal static class SkinValidator
    {
        internal static bool ValidateBeforeExport(string ueVersion, string gender, string name, string description, string cid)
        {
            if (string.IsNullOrEmpty(ueVersion))
            {
                Log.Error($"No unreal engine selected! Make sure you selected the correct ue version in the settings!");
                return false;
            }
            if (string.IsNullOrEmpty(gender))
            {
                Log.Error($"Skin's gender is unspecified!");
                return false;
            }
            if (string.IsNullOrEmpty(name))
            {
                Log.Error($"Skin's name cannot be empty!");
                return false;
            }
            if (string.IsNullOrEmpty(description))
            {
                Log.Error($"Skin's description cannot be empty!");
                return false;
            }
            if (string.IsNullOrEmpty(cid))
            {
                Log.Error($"Skin's CID cannot be empty!");
                return false;
            }

            return true;
        }
    }
}
