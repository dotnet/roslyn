// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.UnitTests
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

        public static string ExePath { get; }

        static DotNetCoreSdk()
        {
            var dotNetExeName = "dotnet" + (Path.DirectorySeparatorChar == '/' ? "" : ".exe");

            bool DotNetExeExists(string directory)
                => directory != null
                && File.Exists(Path.Combine(directory, dotNetExeName));

            var dotNetInstallDir = Environment.GetEnvironmentVariable("DOTNET_INSTALL_DIR");
            if (!DotNetExeExists(dotNetInstallDir))
            {
                dotNetInstallDir = Environment.GetEnvironmentVariable("PATH")
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
