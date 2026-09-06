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

        internal static bool ValidateAfterPathChange(string currentSkinFolderPath, SkinData currentSkin)
        {
            if (currentSkinFolderPath == string.Empty)
            {
                Log.Error("The Current skin path is empty!");
                return false;
            }
            if (!Directory.Exists(currentSkinFolderPath))
            {
                Log.Error($"\"{currentSkinFolderPath}\" doesn't exist!");
                return false;
            }
            string sourcePath = Path.Combine(currentSkinFolderPath, "Source");
            if (!Directory.Exists(sourcePath))
            {
                Log.Error($"Cannot find Source folder inside \"{currentSkinFolderPath}\"");
                return false;
            }
            string meshesPath = Path.Combine(sourcePath, "Meshes");
            if (!Directory.Exists(meshesPath))
            {
                Log.Error($"Cannot find Meshes folder inside \"{sourcePath}\"");
                return false;
            }
            string texturesPath = Path.Combine(sourcePath, "Textures");
            if (!Directory.Exists(texturesPath))
            {
                Log.Error($"Cannot find Textures folder inside \"{sourcePath}\"");
                return false;
            }
            string lobbyAnimationFolderPath = Path.Combine(sourcePath, "Lobby_Animation");
            if (!Directory.Exists(lobbyAnimationFolderPath))
            {
                Log.Error($"Cannot find Lobby_Animation folder inside \"{sourcePath}\"");
                return false;
            }
            string physicsPath = Path.Combine(sourcePath, "Physics");
            if (!Directory.Exists(physicsPath))
            {
                Log.Error($"Cannot find Physics folder inside \"{sourcePath}\"");
                return false;
            }

            currentSkin.Path = currentSkinFolderPath;
            currentSkin.SourcePath = sourcePath;
            currentSkin.MeshesPath = meshesPath;
            currentSkin.TexturesPath = texturesPath;
            currentSkin.LobbyAnimationFolderPath = lobbyAnimationFolderPath;
            currentSkin.PhysicsPath = physicsPath;
            return true;
        }
    }
}
