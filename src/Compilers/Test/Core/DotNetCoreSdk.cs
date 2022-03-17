// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Linq;

namespace Roslyn.Test.Utilities
{
    public static class DotNetCoreSdk
    {
        public sealed class IsAvailable : ExecutionCondition
        {
            public override bool ShouldSkip
                => ExePath == null;
            public override string SkipReason
                => "The location of dotnet SDK can't be determined (DOTNET_INSTALL_DIR environment variable is unset)";
        }

        public static string? ExePath { get; }

        static DotNetCoreSdk()
        {
            var dotNetExeName = "dotnet" + (Path.DirectorySeparatorChar == '/' ? "" : ".exe");

            bool DotNetExeExists(string? directory)
                => directory != null
                && File.Exists(Path.Combine(directory, dotNetExeName));

            var dotNetInstallDir = Environment.GetEnvironmentVariable("DOTNET_INSTALL_DIR");
            if (!DotNetExeExists(dotNetInstallDir))
            {
                dotNetInstallDir = Environment.GetEnvironmentVariable("PATH")!
                    .Split(Path.PathSeparator)
                    .FirstOrDefault(DotNetExeExists);
            }

            if (dotNetInstallDir != null)
            {
                ExePath = Path.Combine(dotNetInstallDir, dotNetExeName);
            }
        }
    }
}
