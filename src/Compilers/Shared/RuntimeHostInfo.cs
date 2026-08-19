// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
#if !NET || MICROSOFT_CODEANALYSIS_MSBUILD_TASK
// On .NET Framework, File.ResolveLinkTarget is provided as an extension member in this namespace
// (see NativeMethods.cs). On .NET it is a native BCL method, so this using is unnecessary there.
using Microsoft.CodeAnalysis.CommandLine;
#endif
#if MICROSOFT_CODEANALYSIS_MSBUILD_TASK
using Microsoft.Build.Framework;
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

#if MICROSOFT_CODEANALYSIS_MSBUILD_TASK
        /// <summary>
        /// The <c>DOTNET_ROOT</c> that should be used when launching executable tools.
        /// </summary>
        internal static string? GetToolDotNetRoot(
            TaskEnvironment taskEnvironment,
            Action<string, object[]>? logger)
        {
            var dotnetpath = GetDotNetHostPath(taskEnvironment);
            return GetToolDotNetRootCore(
                taskEnvironment.GetFullPath(dotnetpath),
                logger);
        }
#else
        /// <summary>
        /// The <c>DOTNET_ROOT</c> that should be used when launching executable tools.
        /// </summary>
        internal static string? GetToolDotNetRoot(Action<string, object[]>? logger) =>
            GetToolDotNetRootCore(GetDotNetHostPath(), logger);
#endif

        internal static string? GetToolDotNetRoot(Func<string, string?> getEnvFunc, Action<string, object[]>? logger) =>
            GetToolDotNetRootCore(GetDotNetHostPath(getEnvFunc), logger);

        private static string? GetToolDotNetRootCore(string dotNetPath, Action<string, object[]>? logger)
        {
            if (!Path.IsPathFullyQualified(dotNetPath))
            {
                logger?.Invoke("Cannot resolve root as the dotnet path is not fully qualified: {0}", [dotNetPath]);
                return null;
            }

            // Resolve symlinks to dotnet
            try
            {
#pragma warning disable RS0030 // Validated as fully qualified above.
                var resolvedPath = File.ResolveLinkTarget(dotNetPath, returnFinalTarget: true);
#pragma warning restore RS0030
                if (resolvedPath != null)
                {
                    dotNetPath = resolvedPath.FullName;
                }
            }
            catch (Exception ex)
            {
                logger?.Invoke("Failed to resolve symbolic link for dotnet path '{0}': {1}", [dotNetPath, ex]);
                return null;
            }

            var directoryName = Path.GetDirectoryName(dotNetPath);
            if (string.IsNullOrEmpty(directoryName))
            {
                return null;
            }

            return directoryName;
        }

#if MICROSOFT_CODEANALYSIS_MSBUILD_TASK
        /// <inheritdoc cref="GetDotNetHostPath(System.Func{string, string?})"/>
        internal static string GetDotNetHostPath(TaskEnvironment taskEnvironment)
            => GetDotNetHostPath(taskEnvironment.GetEnvironmentVariable);
#else
        /// <inheritdoc cref="GetDotNetHostPath(System.Func{string, string?})"/>
        internal static string GetDotNetHostPath()
            => GetDotNetHostPath(Environment.GetEnvironmentVariable);
#endif

        /// <summary>
        /// Get the path to the dotnet host executable. The path returned is not guaranteed to be fully qualified.
        /// </summary>
        private static string GetDotNetHostPath(Func<string, string?> getEnvironmentVariable)
        {
            if (getEnvironmentVariable(DotNetHostPathEnvironmentName) is { Length: > 0 } pathToDotNet)
            {
                return pathToDotNet;
            }

            if (getEnvironmentVariable(DotNetExperimentalHostPathEnvironmentName) is { Length: > 0 } pathToDotNetExperimental)
            {
                return pathToDotNetExperimental;
            }

            return DotNetHostExecutableName;
        }

        internal static string GetDotNetExecCommandLine(string toolFilePath, string commandLineArguments) =>
            $@"exec ""{toolFilePath}"" {commandLineArguments}";
    }
}
