// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignTool
{
    internal static partial class SignToolFactory
    {
        /// <summary>
        /// The <see cref="SignToolBase"/> implementation used for test / validation runs.  Does not actually 
        /// change the sign state of the binaries.
        /// </summary>
        private sealed class TestSignTool : SignToolBase
        {
            internal TestSignTool(string appPath, string binariesPath, string settingsFile, string msbuildPath)
                : base(appPath, binariesPath, settingsFile, msbuildPath)
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
