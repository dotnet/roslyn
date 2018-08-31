// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// LICENSING NOTE: The license for this file is from the originating 
// source and not the general https://github.com/dotnet/roslyn license.
// See https://github.com/dotnet/docfx/blob/d4b1e3015d7c527057193974042ba657da96656c/src/Microsoft.DocAsCode.Metadata.ManagedReference/MsBuildEnvironmentScope.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Microsoft.CodeAnalysis.Tools.MSBuild
{
    internal class MSBuildEnvironment
    {
        private static readonly Regex DotnetBasePathRegex = new Regex("Base Path:(.*)$", RegexOptions.Compiled | RegexOptions.Multiline);
        private static string DotnetBasePath;

        public static void ApplyEnvironmentVariables()
        {
            var environment = GetEnvironmentVariables();
            foreach (var kvp in environment)
            {
                Environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
            }
        }

        public static Dictionary<string, string> GetEnvironmentVariables()
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
                };
            }

            return new Dictionary<string, string>();
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
            using (var outputStream = new MemoryStream())
            using (var outputStreamWriter = new StreamWriter(outputStream))
            {
                try
                {
                    CommandUtility.RunCommand(new CommandInfo
                    {
                        Name = "dotnet",
                        Arguments = "--info"
                    }, outputStreamWriter, timeoutInMilliseconds: 60000);
                }
                catch
                {
                    // when error running dotnet command, consilder dotnet as not available
                    return null;
                }

                // writer streams have to be flushed before reading from memory streams
                // make sure that streamwriter is not closed before reading from memory stream
                outputStreamWriter.Flush();

                var outputString = System.Text.Encoding.UTF8.GetString(outputStream.ToArray());

                var matched = DotnetBasePathRegex.Match(outputString);
                if (matched.Success)
                {
                    DotnetBasePath = matched.Groups[1].Value.Trim();
                    return DotnetBasePath;
                }

                return null;
            }
        }
    }
}
