// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Emit
{
    public class EmitBenchmark
    {
        private Compilation _comp;

        [GlobalSetup]
        public void Setup()
        {
            var projectDir = Environment.GetEnvironmentVariable("TEST_PROJECT_DIR");
            var cmdLineParser = new CSharpCommandLineParser();
            var responseFile = Path.Combine(projectDir, "repro.rsp");
            var compiler = new MockCSharpCompiler(responseFile, projectDir, Array.Empty<string>());
            var output = new StringWriter();
            _comp = compiler.CreateCompilation(output, null, null);

            // Call GetDiagnostics to force binding to finish and most semantic analysis to be completed
            _ = _comp.GetDiagnostics();
        }

        [Benchmark]
        public EmitResult RunEmit()
        {
            var stream = new MemoryStream();
            return _comp.Emit(stream);
        }
    }
}
