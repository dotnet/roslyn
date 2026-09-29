// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using Basic.Reference.Assemblies;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;

namespace Benchmarks;

/// <seealso href="https://github.com/dotnet/roslyn/issues/84529"/>
[MemoryDiagnoser]
[EventPipeProfiler(EventPipeProfile.GcVerbose)]
[WarmupCount(1)]
[IterationCount(1)]
[InvocationCount(1)]
public class RedundantPatternsBenchmarks
{
    [Params(100, 500, 1000)]
    public int TupleConstantArmCount { get; set; }

    private string? _tupleConstantSwitchSource;

    [GlobalSetup]
    public void Setup()
    {
        _tupleConstantSwitchSource = GenerateTupleConstantSwitchSource();
    }

    [Benchmark]
    public object EmitWithTupleConstantSwitch() => EmitCore(_tupleConstantSwitchSource!);

    internal static object EmitCore(string sourceCode)
    {
        var sourceText = SourceText.From(sourceCode);
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceText);
        var compilation = CSharpCompilation.Create(
            "BenchmarkAssembly",
            [syntaxTree],
            Net90.References.All,
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary));

        var assemblyStream = new MemoryStream();
        var emitResult = compilation.Emit(
            assemblyStream,
            pdbStream: null,
            xmlDocumentationStream: null,
            win32Resources: null,
            manifestResources: [],
            options: new EmitOptions().WithDebugInformationFormat(DebugInformationFormat.Embedded),
            cancellationToken: default);
        if (!emitResult.Success)
        {
            throw new Exception("Compilation failed: " + string.Join(Environment.NewLine, emitResult.Diagnostics));
        }

        assemblyStream.Position = 0;
        return assemblyStream;
    }

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

/// <summary>
/// Compares the cost of the redundant-pattern reachability analysis (used to detect
/// <see cref="Microsoft.CodeAnalysis.CSharp.ErrorCode.WRN_RedundantPattern"/>) when the warning is enabled
/// versus when it is suppressed at the location of every candidate pattern via '#pragma warning disable'.
/// Each switch arm below has the syntactic shape 'not A or B', which makes it a candidate for the analysis;
/// disjoint numeric ranges are used per arm so that no arm makes another arm unreachable.
/// </summary>
/// <seealso href="https://github.com/dotnet/roslyn/issues/75506"/>
[MemoryDiagnoser]
[EventPipeProfiler(EventPipeProfile.GcVerbose)]
[WarmupCount(1)]
[IterationCount(1)]
[InvocationCount(1)]
public class RedundantPatternsPragmaSuppressionBenchmarks
{
    [Params(25, 50, 100)]
    public int RedundantPatternArmCount { get; set; }

    [Params(false, true)]
    public bool PragmaDisabled { get; set; }

    private string? _redundantPatternSwitchSource;

    [GlobalSetup]
    public void Setup()
    {
        _redundantPatternSwitchSource = GenerateRedundantPatternSwitchSource();
    }

    [Benchmark]
    public object EmitWithRedundantPatternSwitch() => RedundantPatternsBenchmarks.EmitCore(_redundantPatternSwitchSource!);

    private string GenerateRedundantPatternSwitchSource()
    {
        var sb = new StringBuilder();
        if (PragmaDisabled)
        {
            sb.AppendLine("#pragma warning disable CS9336");
        }

        sb.AppendLine("public static class R");
        sb.AppendLine("{");
        sb.AppendLine("    public static int Get(int x) => x switch");
        sb.AppendLine("    {");
        for (var i = 0; i < RedundantPatternArmCount; i++)
        {
            var lo = i * 10;
            var hi = lo + 9;
            var redundant = lo + 1;
            sb.AppendLine($"        >= {lo} and <= {hi} and not {lo} or {redundant} => {i},");
        }

        sb.AppendLine("        _ => -1");
        sb.AppendLine("    };");
        sb.AppendLine("}");
        if (PragmaDisabled)
        {
            sb.AppendLine("#pragma warning restore CS9336");
        }

        return sb.ToString();
    }
}
