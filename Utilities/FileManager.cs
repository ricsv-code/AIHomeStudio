using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIHomeStudio.Utilities
{
    public static class FileManager
    {
        public static List<string> GetAllFolderNamesFrom(string path)
        {
            if (!Directory.Exists(path))
                return new List<string>();

            return Directory.GetDirectories(path)
                            .Select(Path.GetFileName)
                            .ToList();
        }
    }
}
