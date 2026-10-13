// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.IO;
#if !NET
// On .NET Framework, File.ResolveLinkTarget is provided as an extension member in this namespace
// (see NativeMethods.cs). On .NET it is a native BCL method, so this using is unnecessary there.
using Microsoft.CodeAnalysis.CommandLine;
#endif
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// This type provides information about the runtime which is hosting application. It must be included in a concrete 
    /// target framework to be used.
    /// </summary>
    internal static class RuntimeHostInfo
    {
        internal static bool IsDesktopRuntime => !IsCoreClrRuntime;

        internal static bool IsCoreClrRuntime =>
#if NET
            true;
#else
            false;
#endif

        /// <summary>
        /// Disable JIT tiered compilation on .NET Framework (i.e., keep it enabled on 'dotnet build' but not 'msbuild' which would slow down VS startup perf).
        /// The caller should also check that the environment variable is not already set to avoid overriding user preferences.
        /// </summary>
        internal static bool ShouldDisableTieredCompilation => !IsCoreClrRuntime;

        internal static string DotNetHostExecutableName => $"dotnet{PlatformInformation.ExeExtension}";
        internal const string DotNetRootEnvironmentName = "DOTNET_ROOT";
        internal const string DotNetHostPathEnvironmentName = "DOTNET_HOST_PATH";
        internal const string DotNetExperimentalHostPathEnvironmentName = "DOTNET_EXPERIMENTAL_HOST_PATH";
        internal const string DotNetTieredCompilationEnvironmentName = "DOTNET_TieredCompilation";

        /// <summary>
        /// The <c>DOTNET_ROOT</c> that should be used when launching executable tools. If the return
        /// is non-null then it will be a fully qualified path.
        /// </summary>
        internal static string? GetToolDotNetRoot(IBuildEnvironment buildEnvironment, Action<string, object[]>? logger)
            => GetToolDotNetRoot(GetDotNetHostPath(buildEnvironment), logger);

        internal static string? GetToolDotNetRoot(string dotNetHostPath, Action<string, object[]>? logger)
        {
            if (!Path.IsPathFullyQualified(dotNetHostPath))
            {
                logger?.Invoke("Cannot resolve root as the dotnet path is not fully qualified: {0}", [dotNetHostPath]);
                return null;
            }

            // Resolve symlinks to dotnet
            try
            {
#pragma warning disable RS0030 // Validated as fully qualified above.
                var resolvedPath = File.ResolveLinkTarget(dotNetHostPath, returnFinalTarget: true);
#pragma warning restore RS0030
                if (resolvedPath != null)
                {
                    dotNetHostPath = resolvedPath.FullName;
                }
            }
            catch (Exception ex)
            {
                logger?.Invoke("Failed to resolve symbolic link for dotnet path '{0}': {1}", [dotNetHostPath, ex]);
                return null;
            }

            var directoryName = Path.GetDirectoryName(dotNetHostPath);
            if (string.IsNullOrEmpty(directoryName))
            {
                return null;
            }

            return directoryName;
        }

        /// <summary>
        /// Get the path to the dotnet host executable. The path returned is not guaranteed to be fully qualified.
        /// </summary>
        internal static string GetDotNetHostPath(IBuildEnvironment buildEnvironment)
        {
            if (buildEnvironment.GetEnvironmentVariable(DotNetHostPathEnvironmentName) is { Length: > 0 } pathToDotNet)
            {
                return pathToDotNet;
            }

            if (buildEnvironment.GetEnvironmentVariable(DotNetExperimentalHostPathEnvironmentName) is { Length: > 0 } pathToDotNetExperimental)
            {
                return pathToDotNetExperimental;
            }

            return DotNetHostExecutableName;
        }

        internal static string GetDotNetExecCommandLine(string toolFilePath, string commandLineArguments) =>
            $@"exec ""{toolFilePath}"" {commandLineArguments}";
    }
}
