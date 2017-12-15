using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    internal static class BuildPathsUtil
    {
        internal static BuildPaths Create(string workingDirectory)
        {
#if NET461 || NET46
            return new BuildPaths(
                clientDir: Path.GetDirectoryName(typeof(BuildPathsUtil).Assembly.Location),
                workingDir: workingDirectory,
                sdkDir: RuntimeEnvironment.GetRuntimeDirectory(),
                tempDir: Path.GetTempPath());
#else
            return new BuildPaths(
                clientDir: AppContext.BaseDirectory,
                workingDir: workingDirectory,
                sdkDir: null,
                tempDir: Path.GetTempPath());
#endif
        }
    }
}
