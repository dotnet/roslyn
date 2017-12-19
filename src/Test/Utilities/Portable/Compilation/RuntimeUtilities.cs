using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    /// <summary>
    /// Hide all of the runtime specific implementations of types that we need to use when multi-targeting.
    /// </summary>
    public static partial class RuntimeUtilities
    {
        internal static BuildPaths CreateBuildPaths(string workingDirectory)
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

        internal static IRuntimeEnvironmentFactory GetRuntimeEnvironmentFactory()
        {
#if NET461 || NET46
            return new Roslyn.Test.Utilities.Desktop.DesktopRuntimeEnvironmentFactory();
#elif NETCOREAPP2_0
            return new Roslyn.Test.Utilities.CoreClr.CoreCLRRuntimeEnvironmentFactory();
#elif NETSTANDARD1_3
            throw new NotSupportedException();
#else
#error Unsupported configuration
#endif
        }

        internal static AnalyzerAssemblyLoader CreateAnalyzerAssemblyLoader()
        {
#if NET461 || NET46
            return new DesktopAnalyzerAssemblyLoader();
#else 
            return new ThrowingAnalyzerAssemblyLoader();
#endif
        }
    }
}
