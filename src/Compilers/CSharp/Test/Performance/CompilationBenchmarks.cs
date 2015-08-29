using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.Xunit.Performance;

namespace Microsoft.CodeAnalysis.CSharp.PerformanceTests
{
    public class CompilationBenchmarks : CSharpTestBase
    {
        [Benchmark]
        public void EmptyCompilation()
        {
            var compilation = CreateCSharpCompilation(code: string.Empty);
        }

        [Benchmark]
        public void CompileHelloWorld()
        {
            const string helloWorldCSharpSource = @"using static System.Console;

namespace HelloApplication
{
    class Program
    {
        static void Main()
        {
             WriteLine(""Hello, World"");
        }
    }
}
";

            var compilation = CreateCompilationWithMscorlib(helloWorldCSharpSource);
            var errors = compilation.GetDiagnostics();
        }
    }
}
