// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.IO;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace IdeBenchmarks
{
    internal class Program
    {

        public const string RoslynRootPathEnvVariableName = "ROSLYN_SOURCE_ROOT_PATH";

        public static string GetRoslynRootLocation([CallerFilePath] string sourceFilePath = "")
        {
            var current = Path.GetDirectoryName(sourceFilePath);
            while (current != null)
            {
                if (File.Exists(Path.Combine(current, "Roslyn.slnx")))
                    return current;

                current = Path.GetDirectoryName(current);
            }

            throw new Exception("Cannot find Roslyn.slnx in any parent directory of " + sourceFilePath);
        }

        private static void Main(string[] args)
        {
#if DEBUG
            var config = new DebugInProcessConfig();
#else
            IConfig config = null;
#endif

            Environment.SetEnvironmentVariable(RoslynRootPathEnvVariableName, GetRoslynRootLocation());
            new BenchmarkSwitcher(typeof(Program).Assembly).Run(args, config);
        }
    }
}
