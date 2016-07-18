using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignRoslyn
{
    /// <summary>
    /// The <see cref="SignToolBase"/> implementation used for test / validation runs.  Does not actually 
    /// change the sign state of the binaries.
    /// </summary>
    internal sealed class TestSignTool : SignToolBase
    {
        internal TestSignTool(string appPath, string binariesPath, string sourcePath) 
            :base(appPath, binariesPath, sourcePath)
        {

        }

        internal override void RemovePublicSign(string assemblyPath)
        {

        }

        internal override bool VerifySignedAssembly(Stream assemblyStream)
        {
            return true;
        }

        protected override int RunMSBuild(ProcessStartInfo startInfo)
        {
            return 0;
        }
    }
}
