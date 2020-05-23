using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DeafComposer.Api.Helpers
{
    public static class FileSystemUtils
    {
        public static string GetBase64encodedFile(string filePath)
        {
            Byte[] bytes = File.ReadAllBytes(filePath);
            return Convert.ToBase64String(bytes);
        }
        public static string GetLastDirectoryName(string path)
        {
            int start = path.LastIndexOf(Path.DirectorySeparatorChar) + 1;
            return path.Substring(start, path.Length - start);
        }
    }
}
