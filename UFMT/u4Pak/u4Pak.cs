#pragma warning disable
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Based on panzi's u4pak: https://github.com/panzi/u4pak

namespace UFMT.u4Pak
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using UFMT;
    using UFMT.u4Pak;

    internal sealed record IndexEntry(string RelativePath, byte[] RecordBytes);

    internal static class U4Pak
    {
        public const int DefaultCompressionBlockSize = 65536;

        public static void Pack(string folderPath, string outputPath)
        {
            var files = CollectFiles(new List<string> { folderPath });
            Console.WriteLine($"Packing {files.Count} file(s) into \"{outputPath}\"...");

            using var archive = new FileStream(outputPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
            using var writer = new BinaryWriter(archive, Encoding.UTF8, leaveOpen: true);

            var indexEntries = new List<IndexEntry>(files.Count);
            uint compressionMethod = 0x01u;
            string mountPoint = Path.Combine("..", "..", "..") + Path.DirectorySeparatorChar;

            for (int i = 0; i < files.Count; i++)
            {
                string relativePath = files[i];

                double pct = Math.Round((i + 1) / (double)files.Count * 100.0, 2);
                Console.Write($"Compressing {pct:0.00}%\r");

                byte[] recordBytes = WriteRecordV3(writer, archive, relativePath, compressionMethod);
                indexEntries.Add(new IndexEntry(relativePath, recordBytes));
            }

            WriteIndex(writer, archive, mountPoint, indexEntries);
        }

        private static readonly Dictionary<string, string> _diskPathByRelativePath = new();

        private static List<string> CollectFiles(List<string> filesOrDirs)
        {
            var files = new List<string>();
            _diskPathByRelativePath.Clear();

            foreach (string name in filesOrDirs)
            {
                string fullRoot = Path.GetFullPath(name);

                if (Directory.Exists(fullRoot))
                {
                    string parent = Path.GetDirectoryName(fullRoot) ?? "";

                    foreach (string filePath in Directory.EnumerateFiles(fullRoot, "*", SearchOption.AllDirectories))
                    {
                        string relative = Path.GetRelativePath(parent, filePath);
                        files.Add(relative);
                        _diskPathByRelativePath[relative] = filePath;
                    }
                }
                else if (File.Exists(fullRoot))
                {
                    string parent = Path.GetDirectoryName(fullRoot) ?? "";
                    string relative = Path.GetRelativePath(parent, fullRoot);
                    files.Add(relative);
                    _diskPathByRelativePath[relative] = fullRoot;
                }
                else
                {
                    throw new FileNotFoundException($"No such file or directory: {name}");
                }
            }

            files.Sort(StringComparer.Ordinal);
            return files;
        }
        private static string ResolveDiskPath(string relativePath) =>
            _diskPathByRelativePath.TryGetValue(relativePath, out var full) ? full : relativePath;

        private static byte[] PackPath(string path)
        {
            string normalized = path.Replace(Path.DirectorySeparatorChar, '/').Replace('\\', '/');
            byte[] encoded = Encoding.UTF8.GetBytes(normalized);
            byte[] withNull = new byte[encoded.Length + 1];
            Buffer.BlockCopy(encoded, 0, withNull, 0, encoded.Length);

            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            bw.Write((uint)withNull.Length);
            bw.Write(withNull);
            return ms.ToArray();
        }

        private static byte[] WriteRecordV3(BinaryWriter writer, FileStream archive, string relativePath, uint compressionMethod)
        {
            long recordOffset = archive.Position;

            string diskPath = ResolveDiskPath(relativePath);
            var fileInfo = new FileInfo(diskPath);
            long size = fileInfo.Length;

            int compressionBlockSize = compressionMethod == 0x01u ? DefaultCompressionBlockSize : 0;

            writer.Write(new byte[16]);
            writer.Write(size);
            writer.Write(compressionMethod);
            writer.Write(new byte[20]);

            long compressedSize;
            byte[] sha1;
            int blockCount = 0;
            long[] blocks = null;

            using (var fh = new FileStream(diskPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (compressionMethod == 0x01u)
                {
                    (compressedSize, sha1, blockCount, blocks) =
                        WriteDataZlib(writer, archive, fh, size, compressionBlockSize);
                }
                else
                {
                    writer.Write((byte)0);
                    writer.Write(0u);
                    (compressedSize, sha1) = WriteDataRaw(writer, fh, size);
                }
            }

            long dataEnd = archive.Position;

            archive.Position = recordOffset + 8;
            writer.Write(compressedSize);
            writer.Flush();

            archive.Position = recordOffset + 28;
            writer.Write(sha1);
            writer.Flush();

            archive.Position = dataEnd;

            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            bw.Write(recordOffset);
            bw.Write(compressedSize);
            bw.Write(size);
            bw.Write(compressionMethod);
            bw.Write(sha1);

            if (compressionMethod == 0x01u)
            {
                bw.Write((uint)blockCount);
                foreach (long v in blocks!)
                {
                    bw.Write(v);
                }
                bw.Write((byte)0);
                bw.Write((uint)compressionBlockSize);
            }
            else
            {
                bw.Write((byte)0);
                bw.Write(0u);
            }

            return ms.ToArray();
        }

        private static (long compressedSize, byte[] sha1) WriteDataRaw(BinaryWriter writer, FileStream fh, long size)
        {
            using var sha1Alg = SHA1.Create();
            const int bufSize = 81920;
            var buf = new byte[bufSize];
            long bytesLeft = size;

            while (bytesLeft > 0)
            {
                int toRead = (int)Math.Min(bufSize, bytesLeft);
                int n = fh.Read(buf, 0, toRead);
                if (n < toRead)
                {
                    throw new IOException("unexpected end of file");
                }
                sha1Alg.TransformBlock(buf, 0, n, buf, 0);
                writer.Write(buf, 0, n);
                bytesLeft -= n;
            }

            sha1Alg.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            return (size, sha1Alg.Hash!);
        }

        private static (long compressedSize, byte[] sha1, int blockCount, long[] blocks) WriteDataZlib(
            BinaryWriter writer, FileStream archive, FileStream fh, long size, int compressionBlockSize)
        {
            int blockCount = (int)Math.Ceiling(size / (double)compressionBlockSize);
            long baseOffset = archive.Position;

            writer.Write((uint)blockCount);

            archive.Position = baseOffset + 4 + (long)blockCount * 16;

            writer.Write((byte)0);
            writer.Write((uint)compressionBlockSize);

            long curOffset = baseOffset + 4 + (long)blockCount * 16 + 5;

            var blocks = new long[blockCount * 2];
            long compressedSize = 0;
            int blockNo = 0;

            using var sha1Alg = SHA1.Create();
            var buf = new byte[compressionBlockSize];
            long bytesLeft = size;

            while (bytesLeft > 0)
            {
                int toRead = (int)Math.Min(compressionBlockSize, bytesLeft);
                int n = fh.Read(buf, 0, toRead);
                if (n < toRead)
                {
                    throw new IOException("unexpected end of file");
                }

                byte[] compressed = ZlibCompress(buf, n);

                sha1Alg.TransformBlock(compressed, 0, compressed.Length, compressed, 0);

                compressedSize += compressed.Length;
                blocks[blockNo * 2] = curOffset;
                curOffset += compressed.Length;
                blocks[blockNo * 2 + 1] = curOffset;
                blockNo++;

                writer.Write(compressed);

                bytesLeft -= n;
            }

            sha1Alg.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

            long finalPos = archive.Position;
            archive.Position = baseOffset + 4;
            foreach (long v in blocks)
            {
                writer.Write(v);
            }
            archive.Position = finalPos;

            return (compressedSize, sha1Alg.Hash!, blockCount, blocks);
        }

        private static byte[] ZlibCompress(byte[] data, int length)
        {
            using var ms = new MemoryStream();
            using (var zs = new ZLibStream(ms, CompressionLevel.Optimal, leaveOpen: true))
            {
                zs.Write(data, 0, length);
            }
            return ms.ToArray();
        }

        private static void WriteIndex(BinaryWriter writer, FileStream archive, string mountPoint, List<IndexEntry> entries)
        {
            using var sha1Alg = SHA1.Create();
            long indexOffset = archive.Position;

            byte[] header = PackPath(mountPoint);
            byte[] countBytes = BitConverter.GetBytes((uint)entries.Count);

            sha1Alg.TransformBlock(header, 0, header.Length, header, 0);
            writer.Write(header);

            sha1Alg.TransformBlock(countBytes, 0, countBytes.Length, countBytes, 0);
            writer.Write(countBytes);

            foreach (var entry in entries)
            {
                byte[] nameBytes = PackPath(entry.RelativePath);
                sha1Alg.TransformBlock(nameBytes, 0, nameBytes.Length, nameBytes, 0);
                writer.Write(nameBytes);

                sha1Alg.TransformBlock(entry.RecordBytes, 0, entry.RecordBytes.Length, entry.RecordBytes, 0);
                writer.Write(entry.RecordBytes);
            }

            sha1Alg.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            byte[] indexSha1 = sha1Alg.Hash!;

            long indexEnd = archive.Position;
            long indexSize = indexEnd - indexOffset;

            writer.Write(0x5A6F12E1u);
            writer.Write(3u);
            writer.Write(indexOffset);
            writer.Write(indexSize);
            writer.Write(indexSha1);
        }
    }
}