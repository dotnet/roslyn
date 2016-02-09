// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Xunit.Performance;

namespace Microsoft.CodeAnalysis.CSharp.PerformanceTests
{
    public class SyntaxBenchmarks
    {
        [Benchmark]
        public void EmptyParse()
        {
            Benchmark.Iterate(() =>
            {
                var tree = CSharpSyntaxTree.ParseText("");
            });
        }

        [Benchmark]
        public void HelloWorldParse()
        {
            const string helloCs = @"using static System.Console;

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
                var tree = CSharpSyntaxTree.ParseText(helloCs);
            });
        }
    }
}
