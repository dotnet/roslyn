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
            //This file is located at [Roslyn]\src\Tools\IdeBenchmarks\Program.cs
            return Path.Combine(Path.GetDirectoryName(sourceFilePath), @"..\..\..");
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
