// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        internal static bool IsDesktopRuntime =>
#if NET472
            true;
#elif NETCOREAPP2_1 || NETCOREAPP3_0
            false;
#elif NETSTANDARD2_0
            throw new PlatformNotSupportedException();
#else
#error Unsupported configuration
#endif
        internal static bool IsCoreClrRuntime => !IsDesktopRuntime;

        internal static BuildPaths CreateBuildPaths(string workingDirectory, string sdkDirectory = null, string tempDirectory = null)
        {
            tempDirectory = tempDirectory ?? Path.GetTempPath();
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
#elif NETCOREAPP2_1 || NETCOREAPP3_0
            return new Roslyn.Test.Utilities.CoreClr.CoreCLRRuntimeEnvironmentFactory();
#elif NETSTANDARD2_0
            throw new PlatformNotSupportedException();
#else
#error Unsupported configuration
#endif
        }

        internal static AnalyzerAssemblyLoader CreateAnalyzerAssemblyLoader()
        {
#if NET472
            return new DesktopAnalyzerAssemblyLoader();
#elif NETCOREAPP2_1 || NETCOREAPP3_0
            return new CoreClrAnalyzerAssemblyLoader();
#elif NETSTANDARD2_0
            return new ThrowingAnalyzerAssemblyLoader();
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
