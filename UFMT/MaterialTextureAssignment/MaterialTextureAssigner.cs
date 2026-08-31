#pragma warning disable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UFMT;
using UFMT.MaterialTextureAssignment;

namespace UFMT.MaterialTextureAssignment
{
    internal static class MaterialTextureAssigner
    {
        private static List<string> fallbackKeywords = new() { "body", "head", "faceacc", "eyes", "hair" };
        private static Dictionary<string, string> fallBackKeywordPairs = new() { { "head", "eyes" }, { "faceacc", "hair" } };

        internal static void AssignTexturesToAllMaterials(string texturesPath, string skinCodename, ObservableCollection<Material> materials)
        {
            List<string> validMaterialTextures = TextureCategorizer.GetTexturesByMultipleSuffix(texturesPath, ["_D", "_M", "_N", "_S"]);
            foreach (string texture in validMaterialTextures)
            {
                string textureKeyword = GetTextureKeyword(texture, skinCodename);
                ApplyTextureToMatchingMaterials(texture, textureKeyword, materials, skinCodename);
            }

            List<Material> materialsWithMissingTextures = GetMaterialsWithMissingTextures(materials);
            if (materialsWithMissingTextures.Count == 0) return;
            foreach (string texture in validMaterialTextures)
            {
                string textureFallbackKeyword = GetFallbackKeyword(texture);
                ApplyTextureToMaterialByFallback(texture, textureFallbackKeyword, materialsWithMissingTextures);
            }

            materialsWithMissingTextures = GetMaterialsWithMissingTextures(materials);
            if (materialsWithMissingTextures.Count == 0) return;
            GuessMaterialsTextures(materialsWithMissingTextures, materials);
        }

        private static string GetTextureKeyword(string textureName, string skinCodename)
        {
            string textureKeyword;
            textureKeyword = RemoveTextureSuffix(textureName);
            textureKeyword = textureKeyword.Replace(skinCodename, "");
            textureKeyword = RemoveCommonFortnitePrefixes(textureKeyword);

            return textureKeyword;
        }

        private static string GetFallbackKeyword(string name)
        {
            foreach (string keyword in fallbackKeywords)
            {
                name = RemoveTextureSuffix(name).ToLower();
                if (name.StartsWith($"{keyword}_") || name.EndsWith($"_{keyword}") || name.Contains($"_{keyword}_")) return keyword;
            }
            return null;
        }

        private static void ApplyTextureToMaterialByFallback(string texture, string textureFallbackKeyword, List<Material> materials)
        {
            if (textureFallbackKeyword == null) return;
            foreach (Material mat in materials)
            {
                string matFallbackKeyword = GetFallbackKeyword(mat.Name);
                if (matFallbackKeyword == textureFallbackKeyword ||
                fallBackKeywordPairs.Keys.Contains(textureFallbackKeyword) && matFallbackKeyword == fallBackKeywordPairs.GetValueOrDefault(textureFallbackKeyword))
                {
                    if (texture.EndsWith("_D") && mat.SelectedDiffuse == "Default_Diffuse") mat.SelectedDiffuse = texture;
                    else if (texture.EndsWith("_M") && mat.SelectedMask == "Default_Mask") mat.SelectedMask = texture;
                    else if (texture.EndsWith("_N") && mat.SelectedNormal == "Default_Normal") mat.SelectedNormal = texture;
                    else if (texture.EndsWith("_S") && mat.SelectedSpecular == "Default_Specular") mat.SelectedSpecular = texture;
                }
            }
        }

        private static string RemoveTextureSuffix(string textureName)
        {
            if (textureName.EndsWith("_D") || textureName.EndsWith("_M") || textureName.EndsWith("_N") || textureName.EndsWith("_S")) 
            {
                textureName = textureName.Substring(0, textureName.Length - 2);
            } 
            return textureName;
        }

        private static string RemoveCommonFortnitePrefixes(string textureName)
        {
            string oldTextureName = string.Empty;
            while (oldTextureName != textureName)
            {
                oldTextureName = textureName;
                if (textureName.ToLower().StartsWith("t_")) textureName = textureName.Substring(2); // t is shorter for texture
                if (textureName.ToLower().StartsWith("f_med_")) textureName = textureName.Substring(6); // f_med means the skin is a female, medium size (almost all fn br skins are medium size)
                if (textureName.ToLower().StartsWith("m_med_")) textureName = textureName.Substring(6); // m_med means the skin is a male and medium size
            }
            return textureName;
        }

        private static void ApplyTextureToMatchingMaterials(string texture, string textureKeyword, ObservableCollection<Material> materials, string skinCodename)
        {
            foreach (Material mat in materials)
            {
                string filteredMatName = mat.Name.Replace(skinCodename, "");
                if (filteredMatName.EndsWith(textureKeyword))
                {
                    if (texture.EndsWith("_D") && mat.SelectedDiffuse == "Default_Diffuse") mat.SelectedDiffuse = texture;
                    else if (texture.EndsWith("_M") && mat.SelectedMask == "Default_Mask") mat.SelectedMask = texture;
                    else if (texture.EndsWith("_N") && mat.SelectedNormal == "Default_Normal") mat.SelectedNormal = texture;
                    else if (texture.EndsWith("_S") && mat.SelectedSpecular == "Default_Specular") mat.SelectedSpecular = texture;
                }
            }
        }

        private static List<Material> GetMaterialsWithMissingTextures(ObservableCollection<Material> materials)
        {
            return materials.Where(mat => mat.SelectedDiffuse == "Default_Diffuse" && mat.SelectedMask == "Default_Mask"
            && mat.SelectedNormal == "Default_Normal" && mat.SelectedSpecular == "Default_Specular").ToList();
        }

        private static void GuessMaterialsTextures(List<Material> materialsWithMissingTextures, ObservableCollection<Material> allMaterials)
        {
            foreach (Material missingTextureMat in materialsWithMissingTextures)
            {
                Material workingMat = allMaterials.FirstOrDefault
                (mat => mat.SelectedDiffuse != "Default_Diffuse" && mat.SelectedMask != "Default_Mask" && mat.SelectedNormal != "Default_Normal" &&
                mat.SelectedSpecular != "Default_Specular" && mat.Cp == missingTextureMat.Cp);
                //Get the first material that is the same character part type and has all the textures correctly assigned
                if (workingMat != null)
                {
                    missingTextureMat.SelectedDiffuse = workingMat.SelectedDiffuse;
                    missingTextureMat.SelectedMask = workingMat.SelectedMask;
                    missingTextureMat.SelectedNormal = workingMat.SelectedNormal;
                    missingTextureMat.SelectedSpecular = workingMat.SelectedSpecular;
                }
            }
        }
    }
}
