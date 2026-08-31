using System;
using System.IO;
using System.Reflection;

namespace UFMT.Core
{
    internal static class TemplateLoader
    {
        private static Assembly asm = Assembly.GetExecutingAssembly();
        internal static byte[] GetEmbeddedFile(string Version, string filePath, string fullFileName)
        {
            Version = Version.Replace(".", "_").Replace("-", "_");
            string fullResourceName = $"UFMT.{filePath}._{Version}.{fullFileName}";
            Stream stream = asm.GetManifestResourceStream(fullResourceName);
            if (stream == null)
            {
                fullResourceName = $"UFMT.{filePath}.{Version}.{fullFileName}";
                stream = asm.GetManifestResourceStream(fullResourceName);
            }
            if (stream == null)
            {
                Log.Error($"{fullResourceName} not found inside the assembly, are you sure it exists?");
                return null;
            }
            MemoryStream ms = new MemoryStream();
            stream.CopyTo(ms);
            byte[] data = ms.ToArray();
            return data;
        }
    }
}
