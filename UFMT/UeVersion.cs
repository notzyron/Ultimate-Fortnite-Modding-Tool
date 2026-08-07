#pragma warning disable
using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.UnrealTypes;

namespace UFMT
{
    public record class UeVersion
    {
        public Action<string, string[]> FixRequiredFiles;
        public string BaseHeadPath = @"Content\Base\Head\Skeleton";
        public EngineVersion UassetApiEngineVer;
        public bool ReplaceCookedBaseHead = false;
        public string Name;
        public string[] BaseHeadFileNames = { "Base_Head_AnimBP.uasset", "Base_Head_Skeleton.uasset", "Frontend_Default_Face_Idle.uasset" };
        public bool ReplaceDefaultEngineIni = true;
    }

    public class UeVersionsData
    {
        public static UeVersion Ue4_26 = new UeVersion()
        {
            FixRequiredFiles = (string animationPath, string[] meshesPaths) =>
            {
                foreach (string meshPath in meshesPaths)
                {
                    string uassetPath = meshPath;
                    string uexpPath = Path.ChangeExtension(meshPath, ".uexp");
                    byte[] uasset = File.ReadAllBytes(uassetPath);
                    byte[] uexp = File.ReadAllBytes(uexpPath);

                    List<int> insertions = FindInsertions(uexp);

                    if (insertions.Count == 0)
                    {
                        Log.Warning("The cooked mesh may already be correct or was not cooked by UE 4.26.2.");
                        continue;
                    }

                    byte[] fixedUexp = RemoveBytes(uexp, insertions);

                    byte[] fixedUasset = (byte[])uasset.Clone();
                    bool ok = PatchUassetHeader(fixedUasset, insertions);

                    if (!ok)
                    {
                        Log.Warning("\nWARNING: A header field was not found/patched. Output may be invalid.");
                    }

                    File.WriteAllBytes(uassetPath, fixedUasset);
                    File.WriteAllBytes(uexpPath, fixedUexp);
                }

                List<int> FindInsertions(byte[] uexp)
                {
                    var results = new List<int>();
                    int[] ones = { -17, -1, 0 };
                    int[] zeros = { -20, -19, -18, -16, -14, -10, -7, -6, -5, -4, -3, -2, 1, 2, 3, 6, 7, 8, 9, 10, 11, 13, 14, 15, 17 };
                    for (int i = 20; i < uexp.Length - 17; i++)
                    {
                        if (ones.Any(o => uexp[i + o] != 0x01)) continue;
                        if (zeros.Any(o => uexp[i + o] != 0x00)) continue;
                        results.Add(i);
                    }
                    return results;
                }

                byte[] RemoveBytes(byte[] data, List<int> offsets)
                {
                    var sorted = offsets.OrderBy(x => x).ToList();
                    var result = new List<byte>(data.Length - sorted.Count);
                    int prev = 0;
                    foreach (int off in sorted)
                    {
                        for (int i = prev; i < off; i++) result.Add(data[i]);
                        prev = off + 1;
                    }
                    for (int i = prev; i < data.Length; i++) result.Add(data[i]);
                    return result.ToArray();
                }

                bool PatchUassetHeader(byte[] data, List<int> uexpInsertions)
                {
                    try
                    {
                        int off = 0;
                        off += 4;
                        off += 4; 
                        off += 4;
                        off += 4;
                        off += 4;

                        int cvCount = ReadInt32(data, off); off += 4;
                        off += cvCount * 20;

                        int totalHeaderSize = ReadInt32(data, off); off += 4;
                        off = SkipFString(data, off);

                        uint packageFlags = ReadUInt32(data, off); off += 4;
                        off += 4;
                        off += 4;
                        off += 4;
                        off += 4;

                        const uint PKG_FilterEditorOnly = 0x80000000;
                        if ((packageFlags & PKG_FilterEditorOnly) == 0)
                        {
                            off += 4;
                            off += 4;
                        }

                        int exportCount = ReadInt32(data, off); off += 4;
                        int exportOffset = ReadInt32(data, off); off += 4;
                        off += 4;
                        off += 4;
                        int dependsOffset = ReadInt32(data, off); off += 4;

                        if (exportCount <= 0) return false;
                        int entrySize = (dependsOffset - exportOffset) / exportCount;

                        off += 4;
                        off += 4;
                        off += 4;
                        off += 4;
                        off += 16;

                        int genCount = ReadInt32(data, off); off += 4;
                        off += genCount * 8;

                        off = SkipEngineVersion(data, off);
                        off = SkipEngineVersion(data, off);

                        off += 4;
                        int chunkCount = ReadInt32(data, off); off += 4;
                        off += chunkCount * 16;

                        off += 4;
                        int addPkgCount = ReadInt32(data, off); off += 4;
                        for (int i = 0; i < addPkgCount; i++) off = SkipFString(data, off);

                        off += 4;
                        int bulkDataStartOffsetPos = off;
                        int bulkDataStartOffset = ReadInt32(data, off);

                        var absInsertions = uexpInsertions.Select(p => totalHeaderSize + p).OrderBy(x => x).ToList();

                        int CountBefore(long pos) => absInsertions.Count(p => p < pos);
                        int CountWithin(long start, long size) => absInsertions.Count(p => p >= start && p < start + size);

                        for (int i = 0; i < exportCount; i++)
                        {
                            int entryBase = exportOffset + i * entrySize;
                            long serialSize = BitConverter.ToInt64(data, entryBase + 28);
                            long serialOffset = BitConverter.ToInt64(data, entryBase + 36);

                            long newSerialOffset = serialOffset - CountBefore(serialOffset);
                            long newSerialSize = serialSize - CountWithin(serialOffset, serialSize);

                            Array.Copy(BitConverter.GetBytes(newSerialSize), 0, data, entryBase + 28, 8);
                            Array.Copy(BitConverter.GetBytes(newSerialOffset), 0, data, entryBase + 36, 8);
                        }

                        int newBulkDataStartOffset = bulkDataStartOffset - CountBefore(bulkDataStartOffset);
                        Array.Copy(BitConverter.GetBytes(newBulkDataStartOffset), 0, data, bulkDataStartOffsetPos, 4);

                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                }

                int SkipFString(byte[] data, int off)
                {
                    int n = ReadInt32(data, off); off += 4;
                    if (n > 0) off += n;
                    else if (n < 0) off += (-n) * 2;
                    return off;
                }

                int SkipEngineVersion(byte[] data, int off)
                {
                    off += 2 + 2 + 2 + 4;
                    return SkipFString(data, off);
                }

                int ReadInt32(byte[] data, int offset) => BitConverter.ToInt32(data, offset);
                uint ReadUInt32(byte[] data, int offset) => BitConverter.ToUInt32(data, offset);
            },
            UassetApiEngineVer = EngineVersion.VER_UE4_26,
            Name = "UE_4.26"
        };
        public static UeVersion Ue4_25 = new UeVersion()
        {
            FixRequiredFiles = (string animationPath, string[] meshesPaths) =>
            {
                if (!File.Exists(animationPath)) return;

                UAsset asset = new UAsset(animationPath, EngineVersion.VER_UE4_25);
                asset.SetEngineVersion(EngineVersion.VER_UE4_26);

                Import packImport = new Import(
                    "/Script/CoreUObject",
                    "Package",
                    new FPackageIndex(0),
                    "/Engine/Animation/DefaultAnimCurveCompressionSettings",
                    false,
                    asset
                );
                asset.Imports.Add(packImport);
                int packImportIndex = asset.Imports.Count;

                Import objImport = new Import(
                    "/Script/Engine",
                    "AnimCurveCompressionSettings",
                    new FPackageIndex(-packImportIndex),
                    "DefaultAnimCurveCompressionSettings",
                    false,
                    asset
                );
                asset.Imports.Add(objImport);
                int objImportIndex = asset.Imports.Count;

                var normalExport = (NormalExport)asset.Exports[0];
                ObjectPropertyData curveProp = new ObjectPropertyData(new FName(asset, "CurveCompressionSettings"));
                curveProp.Value = new FPackageIndex(-objImportIndex);
                normalExport.Data.Add(curveProp);

                asset.Write(animationPath);
                Console.WriteLine("Fixed!");
            },
            UassetApiEngineVer = EngineVersion.VER_UE4_25,
            ReplaceCookedBaseHead = true,
            Name = "UE_4.25"
        };
        public static UeVersion Ue4_26_Modded_14_30 = Ue4_25 with
        {
            FixRequiredFiles = (string animationPath, string[] meshesPaths) => { }, //Nothing to fix 
            UassetApiEngineVer = EngineVersion.VER_UE4_26,
            Name = "UE_4.26_FnGameProj14.30",
            ReplaceCookedBaseHead = false,
            ReplaceDefaultEngineIni = false
    };
        public static UeVersion Ue4_22 = new()
        {
            FixRequiredFiles = (string animationPath, string[] meshesPaths) =>
            {
                List<(int PrefixOffset, int Length, string FollowingString)> FindByteStreamCandidates(byte[] data)
                {
                    var results = new List<(int PrefixOffset, int Length, string FollowingString)>();
                    int n = data.Length;

                    for (int p = 0; p + 8 <= n; p++)
                    {
                        int v = BitConverter.ToInt32(data, p);
                        if (v <= 0) continue;

                        long end = (long)p + 4 + v;
                        if (end + 4 > n) continue;

                        int endI = (int)end;
                        string? s = TryReadFString(data, endI);
                        if (s != null)
                        {
                            results.Add((p, v, s));
                        }
                    }

                    return results;
                }

                string? TryReadFString(byte[] data, int pos)
                {
                    if (pos + 4 > data.Length) return null;

                    int length = BitConverter.ToInt32(data, pos);
                    if (length < 1 || length > 500) return null;

                    int start = pos + 4;
                    long endLong = (long)start + length;
                    if (endLong > data.Length) return null;
                    int end = (int)endLong;

                    if (data[end - 1] != 0) return null;

                    for (int i = start; i < end - 1; i++)
                    {
                        byte b = data[i];
                        if (b < 32 || b >= 127) return null;
                    }

                    if (end - 1 == start) return null; // empty string, not useful as a signal

                    return System.Text.Encoding.ASCII.GetString(data, start, length - 1);
                }

                List<int> FindInt64Matches(byte[] data, long target)
                {
                    var results = new List<int>();
                    for (int p = 0; p + 8 <= data.Length; p++)
                    {
                        long v = BitConverter.ToInt64(data, p);
                        if (v == target) results.Add(p);
                    }
                    return results;
                }

                void AddToInt64(byte[] data, int offset, long delta)
                {
                    long v = BitConverter.ToInt64(data, offset);
                    byte[] newBytes = BitConverter.GetBytes(v + delta);
                    Array.Copy(newBytes, 0, data, offset, 8);
                }

                byte[] InsertZeros(byte[] data, int offset, int count)
                {
                    byte[] result = new byte[data.Length + count];
                    Array.Copy(data, 0, result, 0, offset);
                    Array.Copy(data, offset, result, offset + count, data.Length - offset);
                    return result;
                }

                if (!File.Exists(animationPath)) return;
                try
                {
                    string uexpPath = Path.ChangeExtension(animationPath, ".uexp");
                    byte[] uasset = File.ReadAllBytes(animationPath);
                    byte[] uexp = File.ReadAllBytes(uexpPath);
                    Console.WriteLine($".uasset: {animationPath} ({uasset.Length} bytes)");
                    Console.WriteLine($".uexp:   {uexpPath} ({uexp.Length} bytes)");
                    Console.WriteLine();

                    var candidates = FindByteStreamCandidates(uexp);
                    if (candidates.Count == 0)
                    {
                        Log.Warning("Could not find the correct offset in .uexp, the file might already be patched or invalid");
                        return;
                    }

                    var chosen = candidates.OrderByDescending(c => c.Length).First();
                    Console.WriteLine($"Found {candidates.Count} CompressedByteStream candidate(s) in the .uexp:");
                    foreach (var c in candidates)
                    {
                        string marker = c.Equals(chosen) ? "  <- chosen (largest)" : "";
                        Console.WriteLine($"  offset {c.PrefixOffset}, length {c.Length}, string \"{c.FollowingString}\"{marker}");
                    }

                    int insertOffset = chosen.PrefixOffset + 4;
                    Console.WriteLine($".uexp insertion offset: {insertOffset}");

                    long targetUexpMinus4 = uexp.Length - 4L;
                    long targetCombinedMinus4 = uasset.Length + uexp.Length - 4L;
                    List<int> matchesA = FindInt64Matches(uasset, targetUexpMinus4);
                    List<int> matchesB = FindInt64Matches(uasset, targetCombinedMinus4);

                    if (matchesA.Count != 1 || matchesB.Count != 1)
                    {
                        Log.Warning("Could not find the correct offsets in .uasset, the file might already be patched or invalid");
                        return;
                    }

                    int offsetA = matchesA[0];
                    int offsetB = matchesB[0];

                    byte[] patchedUexp = InsertZeros(uexp, insertOffset, 4);
                    byte[] patchedUasset = (byte[])uasset.Clone();
                    AddToInt64(patchedUasset, offsetA, 4);
                    AddToInt64(patchedUasset, offsetB, 4);

                    File.WriteAllBytes(animationPath, patchedUasset);
                    File.WriteAllBytes(uexpPath, patchedUexp);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    return;
                }
            },
            UassetApiEngineVer = EngineVersion.VER_UE4_22,
            ReplaceCookedBaseHead = true,
            Name = "UE_4.22",
        };
        public static UeVersion Ue4_23_Modded_8_51 = Ue4_22 with
        {
            FixRequiredFiles = (string animationPath, string[] meshesPaths) => { },
            UassetApiEngineVer = EngineVersion.VER_UE4_23,
            ReplaceCookedBaseHead = false,
            Name = "UE_4.23_FnGameProj8.51",
            ReplaceDefaultEngineIni = false
        };
        public static UeVersion Ue4_23_Modded_9_10 = Ue4_22 with
        {
            FixRequiredFiles = (string animationPath, string[] meshesPaths) => { },
            UassetApiEngineVer = EngineVersion.VER_UE4_23,
            ReplaceCookedBaseHead = false,
            Name = "UE_4.23_FnGameProj9.10",
            ReplaceDefaultEngineIni = false
        };
        public static UeVersion Ue4_23_Modded_9_41 = Ue4_23_Modded_9_10 with
        {
            BaseHeadPath = @"Content\Modding\Base_Head",
            ReplaceCookedBaseHead = false,
            Name = "UE_4.23_FnGameProj9.41",
            BaseHeadFileNames = [ "Base_Head_Modding.uasset", "Base_Head_Modding_AnimBP.uasset", "Frontend_Default_Face_Idle.uasset", "Base_Head_Modding_FacialPoses_PoseAsset.uasset"],
            ReplaceDefaultEngineIni = false
        };
        public static UeVersion Ue4_25_Modded_12_41 = new UeVersion
        {
            FixRequiredFiles = (string animationPath, string[] meshes) => {},
            UassetApiEngineVer = EngineVersion.VER_UE4_24,
            Name = "UE_4.25_FnGameProj12.41",
            ReplaceDefaultEngineIni = false
        };
        public static UeVersion Ue4_24 = Ue4_25_Modded_12_41 with
        {
            FixRequiredFiles = (string animationPath, string[] meshes) => { },
            BaseHeadPath = @"Content\Base\Head\Skeleton",
            UassetApiEngineVer = EngineVersion.VER_UE4_24,
            ReplaceCookedBaseHead = false,
            Name = "UE_4.24",
            ReplaceDefaultEngineIni = false
        }; //Currently doesn't work, I might work on it in the future, but there will be no physics assets support due to incompatible Animation blueprints
        public static Dictionary<string, UeVersion> UeVersions = new() { { "UE_4.25", Ue4_25 } , { "UE_4.26", Ue4_26 },
            { "UE_4.26_FnGameProj14.30", Ue4_26_Modded_14_30 }, { "UE_4.22", Ue4_22 },{ "UE_4.23_FnGameProj8.51", Ue4_23_Modded_8_51},
            { "UE_4.23_FnGameProj9.10", Ue4_23_Modded_9_10}, { "UE_4.23_FnGameProj9.41", Ue4_23_Modded_9_41},
            { "UE_4.25_FnGameProj12.41", Ue4_25_Modded_12_41}, { "UE_4.24", Ue4_24}};
    }
}
