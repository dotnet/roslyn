using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignRoslyn
{
    internal static partial class SignToolFactory
    {
        /// <summary>
        /// The <see cref="SignToolBase"/> implementation used for test / validation runs.  Does not actually 
        /// change the sign state of the binaries.
        /// </summary>
        private sealed class TestSignTool : SignToolBase
        {
            internal TestSignTool(string appPath, string binariesPath, string sourcePath)
                : base(appPath, binariesPath, sourcePath)
            {

            }

            public override void RemovePublicSign(string assemblyPath)
            {

            }

            public override bool VerifySignedAssembly(Stream assemblyStream)
            {
                return true;
            }

            protected override int RunMSBuild(ProcessStartInfo startInfo)
            {
                return 0;
            }
        }
    }
}
