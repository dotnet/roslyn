using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SignRoslyn
{
    internal static class PathUtil
    {
        internal static bool IsVsix(string fileName)
        {
            return Path.GetExtension(fileName) == ".vsix";
        }

        internal static bool IsAssembly(string fileName)
        {
            var ext = Path.GetExtension(fileName);
            return ext == ".exe" || ext == ".dll";
        }
    }
}
