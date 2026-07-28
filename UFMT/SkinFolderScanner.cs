using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Store;

namespace UFMT
{
    internal class SkinFolderScanner
    {
        internal static List<CharacterPart> FindCharacterParts(string meshesPath, string physicsPath, List<CharacterPart> allCharacterParts)
        {
            List<string> pskPaths = new();
            List<CharacterPart> characterParts = new();

            foreach (string meshFolder in Directory.GetDirectories(meshesPath))
            {
                string[] pskFiles = Directory.GetFiles(meshFolder, "*.psk");
                if (pskFiles.Length == 1)
                {
                    Console.WriteLine($"{Path.GetFileName(pskFiles[0])} is a {Path.GetFileName(meshFolder)} CharactePart Type!");
                    pskPaths.Add(pskFiles[0]);
                }
                else if (pskFiles.Length > 1)
                {
                    Log.Error($"{meshFolder} contains more than 1 .psk files, cannot get the correct {Path.GetFileName(meshFolder)} " +
                    $"Character Part Type!");
                    return null;
                }


            }

            foreach (string pskPath in pskPaths)
            {
                string pskCpType = Path.GetFileName(Path.GetDirectoryName(pskPath));
                CharacterPart currentPskCp = allCharacterParts.FirstOrDefault(cp => cp.Type == pskCpType.ToLower());
                currentPskCp.PskPath = pskPath;


                currentPskCp.PhysicsAssetJsonPaths = Directory.GetFiles(Path.Combine(physicsPath, pskCpType), "*.json").ToList();
                currentPskCp.PhysicsAssetJsonPaths.ForEach(json => Console.WriteLine($"Added {Path.GetFileNameWithoutExtension(json)} to {Path.GetFileNameWithoutExtension(pskPath)}"));

                characterParts.Add(currentPskCp);
                Log.Success($"{Path.GetFileName(pskPath)} is a {currentPskCp.Type} character part type!");
            }

            if (characterParts.FirstOrDefault(cp => cp.Type == "body") == null)
            {
                Log.Error($"Cannot find a body .psk file in {meshesPath}\nThe character must have at least a body and a head!");
                return null;
            }
            if (characterParts.FirstOrDefault(cp => cp.Type == "head") == null)
            {
                Log.Error($"Cannot find a head .psk file in {meshesPath}\nThe character must have at least a body and a head!");
                return null;
            }

            return characterParts;
        }

        internal static (bool isValid, string psaName, string jsonPath) FindLobbyAnimationFiles(string lobbyAnimationPath)
        {
            string psaName = string.Empty;
            string jsonName = string.Empty;
            string[] lobbyAnimationFiles = Directory.GetFiles(lobbyAnimationPath, "*.psa");
            if (lobbyAnimationFiles.Length > 1)
            {
                Log.Error($"Multiple .psa files in \"{lobbyAnimationPath}\"!\nMake sure there is only 1 .psa lobby animation!");
                return (false, psaName, jsonName);
            }
            if (lobbyAnimationFiles.Length != 0)
            {
                psaName = Path.GetFileNameWithoutExtension(lobbyAnimationFiles[0]);
                Log.Success($"The lobby animation is {psaName}.psa");
            }
            else
            {
                return (true, psaName, jsonName);
            }

            string[] lobbyAnimationJsonFiles = Directory.GetFiles(lobbyAnimationPath, "*.json");
            if (lobbyAnimationJsonFiles.Length > 1)
            {
                Log.Error($"Multiple .json files in \"{lobbyAnimationPath}\"!\nMake sure there is only 1 .json lobby animation!");
                return (false, psaName, jsonName);
            }
            if (lobbyAnimationJsonFiles.Length != 0)
            {
                jsonName = Path.GetFileNameWithoutExtension(lobbyAnimationJsonFiles[0]);
                Log.Success($"The lobby animation is {jsonName}.json");
            }

            return (true, psaName, jsonName);
        }
    }
}
