// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// Original License:
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// See https://github.com/dotnet/docfx/blob/d4b1e3015d7c527057193974042ba657da96656c/src/Microsoft.DocAsCode.Metadata.ManagedReference/MsBuildEnvironmentScope.cs

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text.RegularExpressions;
using RunTests;

namespace Microsoft.CodeAnalysis.Tools.MSBuild
{
    internal class MSBuildEnvironment
    {
        private static readonly Regex DotnetBasePathRegex = new Regex("Base Path:(.*)$", RegexOptions.Compiled | RegexOptions.Multiline);
        private static string DotnetBasePath;

        /// <summary>
        /// Ensures the proper MSBuild environment variables are populated.
        /// </summary>
        public static void ApplyEnvironmentVariables()
        {
            var environment = GetEnvironmentVariables();
            foreach (var kvp in environment)
            {
                Environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
            }
        }

        private static ImmutableDictionary<string, string> GetEnvironmentVariables()
        {
            // workaround for https://github.com/dotnet/docfx/issues/1752
            var dotnetBasePath = GetDotnetBasePath();
            if (dotnetBasePath != null)
            {
                return new Dictionary<string, string>
                {
                    ["MSBUILD_EXE_PATH"] = dotnetBasePath + "MSBuild.dll",
                    ["MSBuildExtensionsPath"] = dotnetBasePath,
                    ["MSBuildSDKsPath"] = dotnetBasePath + "Sdks"
                }.ToImmutableDictionary();
            }

            return ImmutableDictionary<string, string>.Empty;
        }

        public static string GetDotnetBasePath()
        {
            if (string.IsNullOrEmpty(DotnetBasePath))
            {
                DotnetBasePath = DetermineDotnetBasePath();
            }

            return DotnetBasePath;
        }

        private static string DetermineDotnetBasePath()
        {
            var processResult = ProcessRunner.CreateProcess("dotnet", "--info", captureOutput: true, displayWindow: false).Result.Result;
            if (processResult.ExitCode != 0)
            {
                // when error running dotnet command, consider dotnet as not available
                return null;
            }

            var outputString = string.Join(Environment.NewLine, processResult.OutputLines);
            var matched = DotnetBasePathRegex.Match(outputString);

            if (!matched.Success)
            {
                return null;
            }

            DotnetBasePath = matched.Groups[1].Value.Trim();
            return DotnetBasePath;
        }
    }
}
