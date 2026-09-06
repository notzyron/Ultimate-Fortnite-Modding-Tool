using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UFMT.Core;
using UFMT.UI;

namespace UFMT.FnAssetsLogic
{
    internal static class EmoteValidator
    {
        internal static bool ValidateAfterPathChange(string currentEmoteFolderPath, EmoteData currentEmote)
        {
            if (currentEmote == null)
            {
                Log.Error("Current Emote was null when trying to validate it!");
                return false;
            }

            if (currentEmoteFolderPath == string.Empty)
            {
                Log.Error("The Current skin path is empty!");
                return false;
            }
            if (!Directory.Exists(currentEmoteFolderPath))
            {
                Log.Error($"\"{currentEmoteFolderPath}\" doesn't exist!");
                return false;
            }
            string sourcePath = Path.Combine(currentEmoteFolderPath, "Source");
            if (!Directory.Exists(sourcePath))
            {
                Log.Error($"Cannot find Source folder inside \"{currentEmoteFolderPath}\"");
                return false;
            }
            string animationsPath = Path.Combine(sourcePath, "Animations");
            if (!Directory.Exists(animationsPath))
            {
                Log.Error($"Cannot find Animations folder inside \"{sourcePath}\"");
                return false;
            }
            string iconsPath = Path.Combine(sourcePath, "Icons");
            if (!Directory.Exists(iconsPath))
            {
                Log.Error($"Cannot find UI folder inside \"{sourcePath}\"");
                return false;
            }
            string soundPath = Path.Combine(sourcePath, "Animations");
            if (!Directory.Exists(animationsPath))
            {
                Log.Error($"Cannot find Animations folder inside \"{sourcePath}\"");
                return false;
            }

            currentEmote.Path = currentEmoteFolderPath;
            currentEmote.SourcePath = sourcePath;
            currentEmote.AnimationsPath = animationsPath;
            currentEmote.IconsPath = iconsPath;
            currentEmote.SoundPath = soundPath;
            return true;
        }
    }
}
