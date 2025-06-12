// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Globalization;
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
        private static readonly object s_outputGuard = new();

        internal static bool IsDesktopRuntime =>
#if NET472
            true;
#elif NETCOREAPP
            false;
#else
#error Unsupported configuration
#endif
        internal static bool IsCoreClrRuntime => !IsDesktopRuntime;

        private static int? CoreClrRuntimeVersion { get; } = IsDesktopRuntime
            ? null
            : typeof(object).Assembly.GetName()!.Version!.Major;

        internal static bool IsCoreClr6Runtime
            => IsCoreClrRuntime && RuntimeInformation.FrameworkDescription.StartsWith(".NET 6.", StringComparison.Ordinal);

        internal static bool IsCoreClr8OrHigherRuntime
            => CoreClrRuntimeVersion is { } v && v >= 8;

        internal static bool IsCoreClr9OrHigherRuntime
            => CoreClrRuntimeVersion is { } v && v >= 9;

        internal static BuildPaths CreateBuildPaths(string workingDirectory, string? sdkDirectory = null, string? tempDirectory = null)
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

        internal static IRuntimeEnvironment CreateRuntimeEnvironment(ModuleData mainModule, ImmutableArray<ModuleData> modules = default)
        {
#if NET472
            return new Roslyn.Test.Utilities.Desktop.DesktopRuntimeEnvironment(mainModule, modules);
#elif NETCOREAPP
            return new Roslyn.Test.Utilities.CoreClr.CoreCLRRuntimeEnvironment(mainModule, modules);
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

        public static (string Output, string ErrorOutput) CaptureOutput(Action action, IFormatProvider? formatProvider = null)
        {
            lock (s_outputGuard)
            {
                var savedConsoleOut = Console.Out;
                var savedConsoleError = Console.Error;

                using var outputWriter = new StringWriter(formatProvider);
                using var errorWriter = new StringWriter(formatProvider);
                try
                {
                    Console.SetOut(outputWriter);
                    Console.SetError(errorWriter);
                    action();
                }
                finally
                {
                    Console.SetOut(savedConsoleOut);
                    Console.SetError(savedConsoleError);
                }

                var output = outputWriter.ToString();
                var errorOutput = errorWriter.ToString();
                return (output, errorOutput);
            }
        }
    }
}
