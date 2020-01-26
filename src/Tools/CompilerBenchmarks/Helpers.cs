// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;

namespace CompilerBenchmarks
{
    internal static class Helpers
    {
        public const string TestProjectEnvVarName = "ROSLYN_TEST_PROJECT_DIR";

        public static Compilation CreateReproCompilation()
        {
            var projectDir = Environment.GetEnvironmentVariable(TestProjectEnvVarName);
            var cmdLineParser = new CSharpCommandLineParser();
            var responseFile = Path.Combine(projectDir, "repro.rsp");
            var compiler = new MockCSharpCompiler(responseFile, projectDir, Array.Empty<string>());
            var output = new StringWriter();
            return compiler.CreateCompilation(output, null, null);
        }
    }
}
