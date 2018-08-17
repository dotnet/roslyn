// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
