// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.Xunit.Performance;

namespace Microsoft.CodeAnalysis.CSharp.PerformanceTests
{
    public class CompilationBenchmarks : CSharpTestBase
    {
        [Benchmark]
        public void EmptyCompilation()
        {
            Benchmark.Iterate(() =>
            {
                var compilation = CreateCSharpCompilation(code: string.Empty);
            });
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

            Benchmark.Iterate(() =>
            {
                var compilation = CreateCompilationWithMscorlib(helloWorldCSharpSource);
                var errors = compilation.GetDiagnostics();
            });
        }
    }
}
