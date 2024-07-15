// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    /// <summary>
    /// Hide all of the runtime specific implementations of types that we need to use when multi-targeting.
    /// </summary>
    public static partial class RuntimeUtilities
    {
        internal static bool IsDesktopRuntime =>
#if NET472
            true;
#elif NETCOREAPP
            false;
#else
#error Unsupported configuration
#endif
        internal static bool IsCoreClrRuntime => !IsDesktopRuntime;

        internal static bool IsCoreClr6Runtime
            => IsCoreClrRuntime && RuntimeInformation.FrameworkDescription.StartsWith(".NET 6.", StringComparison.Ordinal);

        internal static bool IsCoreClr8OrHigherRuntime
            => IsCoreClrRuntime && RuntimeInformation.FrameworkDescription.StartsWith(".NET 8.", StringComparison.Ordinal);

        internal static bool IsCoreClr9OrHigherRuntime
            => IsCoreClrRuntime && RuntimeInformation.FrameworkDescription.StartsWith(".NET 9.", StringComparison.Ordinal);
#if NET9_0_OR_GREATER
#error Make the above check be an #if NET9_OR_GREATER when we add net8 support to build
#endif

        internal static BuildPaths CreateBuildPaths(string workingDirectory, string sdkDirectory = null, string tempDirectory = null)
        {
            tempDirectory ??= Path.GetTempPath();
#if NET472
            return new BuildPaths(
                clientDir: Path.GetDirectoryName(typeof(BuildPathsUtil).Assembly.Location),
                workingDir: workingDirectory,
                sdkDir: sdkDirectory ?? RuntimeEnvironment.GetRuntimeDirectory(),
                tempDir: tempDirectory);
#else
            return new BuildPaths(
                clientDir: AppContext.BaseDirectory,
                workingDir: workingDirectory,
                sdkDir: sdkDirectory,
                tempDir: tempDirectory);
#endif
        }

        internal static IRuntimeEnvironmentFactory GetRuntimeEnvironmentFactory()
        {
#if NET472
            return new Roslyn.Test.Utilities.Desktop.DesktopRuntimeEnvironmentFactory();
#elif NETCOREAPP
            return new Roslyn.Test.Utilities.CoreClr.CoreCLRRuntimeEnvironmentFactory();
#else
#error Unsupported configuration
#endif
        }

        /// <summary>
        /// Get the location of the assembly that contains this type
        /// </summary>
        internal static string GetAssemblyLocation(Type type)
        {
            return type.GetTypeInfo().Assembly.Location;
        }
    }
}
