using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Xunit.Performance;

namespace CSharpCompilerPerformanceTest
{
    public class CompilationBenchmarks
    {
        [Benchmark]
        public void EmptyCompilation()
        {
            var compilation = CSharpCompilation.Create("empty");
        }
    }
}
