using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignRoslyn
{
    internal interface ISignTool
    {
        void RemovePublicSign(string assemblyPath);

        bool VerifySignedAssembly(Stream assemblyStream);

        void Sign(IEnumerable<FileSignInfo> filesToSign);
    }

    internal static partial class SignToolFactory
    {
        internal static ISignTool Create(string appPath, string binariesPath, string sourcePath, bool test)
        {
            if (test)
            {
                return new TestSignTool(appPath, binariesPath, sourcePath);
            }

            return new RealSignTool(appPath, binariesPath, sourcePath);
        }
    }
}
