using System;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Emit
{
    public class PerfBenchmarks
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
            _ = _comp.GetDiagnostics();
        }

        [Benchmark]
        public EmitResult EmitBenchmark()
        {
            var stream = new MemoryStream();
            return _comp.Emit(stream);
        }
    }
}
