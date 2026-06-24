// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CompilerBenchmarks;

[MemoryDiagnoser]
public class AnalyzerAndGeneratorTimingBenchmarks
{
    private const int SourceFileCount = 12;
    private const int TypesPerFile = 8;
    private const int MethodsPerType = 6;

    private static readonly CSharpParseOptions s_parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
    private static readonly CSharpCompilationOptions s_compilationOptions = new(
        OutputKind.DynamicallyLinkedLibrary,
        optimizationLevel: OptimizationLevel.Release,
        concurrentBuild: true);

    private ImmutableArray<SyntaxTree> _syntaxTrees;
    private ImmutableArray<PortableExecutableReference> _references;
    private ImmutableArray<DiagnosticAnalyzer> _analyzers;
    private ImmutableArray<ISourceGenerator> _generators;
    private AnalyzerOptions _analyzerOptions = null!;

    [Params(1, 32)]
    public int ComponentCount { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        var builder = ImmutableArray.CreateBuilder<SyntaxTree>(SourceFileCount);
        for (var i = 0; i < SourceFileCount; i++)
            builder.Add(CSharpSyntaxTree.ParseText(CreateSource(i), s_parseOptions, path: $"BenchmarkFile{i}.cs", encoding: Encoding.UTF8));

        _syntaxTrees = builder.ToImmutable();
        _references = Basic.Reference.Assemblies.Net100.References.All;
        _analyzers = Enumerable.Range(0, ComponentCount).Select(static i => (DiagnosticAnalyzer)new SymbolTouchAnalyzer(i)).ToImmutableArray();
        _generators = Enumerable.Range(0, ComponentCount).Select(static i => new CompilationScanningGenerator(i).AsSourceGenerator()).ToImmutableArray();
        _analyzerOptions = new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty);
    }

    [Benchmark(Baseline = true, Description = "AnalyzerTiming=false, GeneratorTiming=false")]
    public int CompileWithoutTiming()
        => Compile(logAnalyzerExecutionTime: false, collectGeneratorTiming: false);

    [Benchmark(Description = "AnalyzerTiming=true, GeneratorTiming=false")]
    public int CompileWithAnalyzerTiming()
        => Compile(logAnalyzerExecutionTime: true, collectGeneratorTiming: false);

    [Benchmark(Description = "AnalyzerTiming=false, GeneratorTiming=true")]
    public int CompileWithGeneratorTiming()
        => Compile(logAnalyzerExecutionTime: false, collectGeneratorTiming: true);

    [Benchmark(Description = "AnalyzerTiming=true, GeneratorTiming=true")]
    public int CompileWithAnalyzerAndGeneratorTiming()
        => Compile(logAnalyzerExecutionTime: true, collectGeneratorTiming: true);

    private int Compile(bool logAnalyzerExecutionTime, bool collectGeneratorTiming)
    {
        Compilation compilation = CSharpCompilation.Create(
            assemblyName: "AnalyzerAndGeneratorTimingBenchmark",
            syntaxTrees: _syntaxTrees,
            references: _references,
            options: s_compilationOptions);

        GeneratorDriver driver = CSharpGeneratorDriver.Create(_generators, parseOptions: s_parseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out compilation, out var generatorDiagnostics);
        var generatorTimingCount = collectGeneratorTiming ? driver.GetTimingInfo().GeneratorTimes.Length : 0;

        var compilationWithAnalyzers = new CompilationWithAnalyzers(
            compilation,
            _analyzers,
            new CompilationWithAnalyzersOptions(
                _analyzerOptions,
                onAnalyzerException: null,
                concurrentAnalysis: true,
                logAnalyzerExecutionTime,
                reportSuppressedDiagnostics: false));

        using var stream = new MemoryStream();
        var emitResult = compilationWithAnalyzers.Compilation.Emit(stream);
        ThrowIfEmitFailed(emitResult);

        var analyzerDiagnostics = compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync(CancellationToken.None).GetAwaiter().GetResult();
        return generatorDiagnostics.Length + generatorTimingCount + analyzerDiagnostics.Length + (int)stream.Length;
    }

    private static string CreateSource(int fileIndex)
    {
        var builder = new StringBuilder();
        builder.AppendLine("namespace BenchmarkProject;");
        builder.AppendLine();

        for (var typeIndex = 0; typeIndex < TypesPerFile; typeIndex++)
        {
            builder.AppendLine($"internal sealed class File{fileIndex}_Type{typeIndex}");
            builder.AppendLine("{");
            builder.AppendLine($"    private readonly int _offset = {fileIndex + typeIndex};");

            for (var methodIndex = 0; methodIndex < MethodsPerType; methodIndex++)
            {
                builder.AppendLine($"    public int Method{methodIndex}(int value)");
                builder.AppendLine("    {");
                builder.AppendLine("        var result = value + _offset;");
                builder.AppendLine("        result = (result * 31) ^ (result >> 3);");
                builder.AppendLine("        return result;");
                builder.AppendLine("    }");
            }

            builder.AppendLine("}");
            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static void ThrowIfEmitFailed(EmitResult emitResult)
    {
        if (!emitResult.Success)
            throw new InvalidOperationException(string.Join(Environment.NewLine, emitResult.Diagnostics));
    }

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    private sealed class SymbolTouchAnalyzer : DiagnosticAnalyzer
    {
        private readonly DiagnosticDescriptor _rule;

        public SymbolTouchAnalyzer(int id)
        {
            _rule = new DiagnosticDescriptor(
                id: $"TIMING{id:000}",
                title: "Timing benchmark analyzer",
                messageFormat: "Timing benchmark analyzer",
                category: "Performance",
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true);
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
        }

        private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
        {
            var method = (MethodDeclarationSyntax)context.Node;
            var symbol = (IMethodSymbol?)context.SemanticModel.GetDeclaredSymbol(method, context.CancellationToken);
            _ = symbol?.Parameters.Length;
        }
    }

    private sealed class CompilationScanningGenerator : IIncrementalGenerator
    {
        private readonly int _id;

        public CompilationScanningGenerator(int id)
        {
            _id = id;
        }

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var methodParameterCounts = context.SyntaxProvider
                .CreateSyntaxProvider(
                    static (node, _) => node is MethodDeclarationSyntax,
                    static (context, cancellationToken) =>
                    {
                        var method = (MethodDeclarationSyntax)context.Node;
                        var symbol = (IMethodSymbol?)context.SemanticModel.GetDeclaredSymbol(method, cancellationToken);
                        return symbol?.Parameters.Length ?? method.ParameterList.Parameters.Count;
                    });

            var methodSummary = methodParameterCounts
                .Collect()
                .Select(static (parameterCounts, _) => new MethodSummary(parameterCounts.Length, parameterCounts.Sum()));

            var syntaxTreeCount = context.CompilationProvider
                .Select(static (compilation, _) => compilation.SyntaxTrees.Count());

            context.RegisterSourceOutput(methodSummary.Combine(syntaxTreeCount), GenerateSource);
        }

        private void GenerateSource(SourceProductionContext context, (MethodSummary, int) input)
        {
            var (methodSummary, syntaxTreeCount) = input;

            context.AddSource(
                $"GeneratedSummary{_id}.g.cs",
                SourceText.From(
                    $$"""
                    // <auto-generated/>
                    namespace BenchmarkProject;

                    internal static class GeneratedSummary{{_id}}
                    {
                        public const int MethodCount = {{methodSummary.MethodCount}};
                        public const int ParameterCount = {{methodSummary.ParameterCount}};
                        public const int SyntaxTreeCount = {{syntaxTreeCount}};
                    }
                    """,
                    Encoding.UTF8));
        }

        private readonly struct MethodSummary
        {
            public MethodSummary(int methodCount, int parameterCount)
            {
                MethodCount = methodCount;
                ParameterCount = parameterCount;
            }

            public int MethodCount { get; }
            public int ParameterCount { get; }
        }
    }
}
