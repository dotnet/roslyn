// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;

namespace Benchmarks;

/// <seealso href="https://github.com/dotnet/roslyn/issues/84529"/>
[MemoryDiagnoser]
[EventPipeProfiler(EventPipeProfile.GcVerbose)]
public class RedundantPatternsBenchmarks
{
    [Params(500)]
    public int TupleConstantArmCount { get; set; }

    private string? _tupleConstantSwitchSource;

    [GlobalSetup]
    public void Setup()
    {
        _tupleConstantSwitchSource = GenerateTupleConstantSwitchSource();
    }

    [Benchmark]
    public object EmitWithTupleConstantSwitch() => DecisionDagBenchmarks.EmitCore(_tupleConstantSwitchSource!);

    private string GenerateTupleConstantSwitchSource()
    {
        const int fieldsPerEntity = 25;
        var sb = new StringBuilder();
        sb.AppendLine("public static class P");
        sb.AppendLine("{");
        sb.AppendLine("    public static int Get(string entity, string field) => (entity, field) switch");
        sb.AppendLine("    {");
        for (var i = 0; i < TupleConstantArmCount; i++)
        {
            var entityIndex = i / fieldsPerEntity;
            sb.AppendLine($"        (\"Entity{entityIndex}\", \"Field{i}\") => {i},");
        }

        sb.AppendLine("        _ => -1");
        sb.AppendLine("    };");
        sb.AppendLine("}");
        return sb.ToString();
    }
}
