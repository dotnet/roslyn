// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CompilerBenchmarks
{
    [SimpleJob(RuntimeMoniker.NetCoreApp50)]
    public class ComplexCondAccessEquals
    {
        private MetadataReference[] coreRefs = null!;

        [GlobalSetup]
        public void Setup()
        {
            coreRefs = new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) };
        }

        [Params(10, 12, 15, 18)]
        public int Depth { get; set; }

        [Params("enable", "disable")]
        public string NullableContext { get; set; } = null!;

        [Benchmark]
        public void CondAccess_ComplexRightSide()
        {
            var source1 = @"
#nullable " + NullableContext + @"
object? x = null;
C? c = null;
if (
";
            var source2 = @"
    )
{
}

class C
{
    public bool M(object? obj) => false;
}
";
            var sourceBuilder = new StringBuilder();
            sourceBuilder.Append(source1);
            for (var i = 0; i < Depth; i++)
            {
                sourceBuilder.AppendLine($"    c?.M(x = {i}) == (");
            }
            sourceBuilder.AppendLine("    c!.M(x)");

            sourceBuilder.Append("    ");
            for (var i = 0; i < Depth; i++)
            {
                sourceBuilder.Append(")");
            }

            sourceBuilder.Append(source2);

            var tree = SyntaxFactory.ParseSyntaxTree(sourceBuilder.ToString(), path: "bench.cs");
            var comp = CSharpCompilation.Create(
                "Benchmark",
                new[] { tree },
                coreRefs);
            var diags = comp.GetDiagnostics();
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<ComplexCondAccessEquals>();
        }
    }
}
