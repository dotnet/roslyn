using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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
        internal static BuildPaths CreateBuildPaths(string workingDirectory, string tempDirectory = null)
        {
            tempDirectory = tempDirectory ?? Path.GetTempPath();
#if NET46
            return new BuildPaths(
                clientDir: Path.GetDirectoryName(typeof(BuildPathsUtil).Assembly.Location),
                workingDir: workingDirectory,
                sdkDir: RuntimeEnvironment.GetRuntimeDirectory(),
                tempDir: tempDirectory);
#else
            return new BuildPaths(
                clientDir: AppContext.BaseDirectory,
                workingDir: workingDirectory,
                sdkDir: null,
                tempDir: tempDirectory);
#endif
        }

        internal static IRuntimeEnvironmentFactory GetRuntimeEnvironmentFactory()
        {
#if NET46
            return new Roslyn.Test.Utilities.Desktop.DesktopRuntimeEnvironmentFactory();
#elif NETCOREAPP2_0
            return new Roslyn.Test.Utilities.CoreClr.CoreCLRRuntimeEnvironmentFactory();
#elif NETSTANDARD1_3
            throw new PlatformNotSupportedException();
#else
#error Unsupported configuration
#endif
        }

        internal static AnalyzerAssemblyLoader CreateAnalyzerAssemblyLoader()
        {
#if NET46
            return new DesktopAnalyzerAssemblyLoader();
#else 
            return new ThrowingAnalyzerAssemblyLoader();
#endif
        }

        /// <summary>
        /// Get the location of the assembly that contains this type
        /// </summary>
        internal static string GetAssemblyLocation(Type type)
        {
#if NET46 || NETCOREAPP2_0
            return type.GetTypeInfo().Assembly.Location;
#elif NETSTANDARD1_3
            throw new PlatformNotSupportedException();
#else
#error Unsupported configuration
#endif
        }
    }
}
