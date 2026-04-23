// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace IdeCoreBenchmarks;

/// <summary>
/// Shared source generation and warm-path setup for diagnostic incremental analysis benchmarks.
/// Both the targeted-analyzer and real-analyzer benchmarks share this infrastructure.
/// </summary>
public abstract class DiagnosticIncrementalAnalysisBenchmarkBase
{
    protected const int MemberCount = 50;
    protected const int MethodCount = 100;

    private AdhocWorkspace _workspace = null!;
    private IDiagnosticAnalyzerService _diagnosticService = null!;
    private DocumentId _documentId;

    private static readonly string s_initialSource = GenerateSource();
    private const string OriginalMethodBody = "var x = 1;\r\n        Console.WriteLine(x);";
    private const string EditedMethodBody = "var x = 42;\r\n        Console.WriteLine(x + 1);";

    private static string GenerateSource()
    {
        var sb = new StringBuilder();
        sb.AppendLine("using System;");
        sb.AppendLine();
        sb.AppendLine("namespace BenchmarkTarget;");
        sb.AppendLine();
        sb.AppendLine("public class LargeComponent");
        sb.AppendLine("{");

        var fieldTypes = new[] { "string", "int", "double", "bool", "object" };
        for (var i = 1; i <= MemberCount; i++)
        {
            sb.AppendLine($"    public {fieldTypes[(i - 1) % fieldTypes.Length]} Field{i};");
        }

        sb.AppendLine();

        for (var i = 1; i <= MethodCount; i++)
        {
            sb.AppendLine($"    public void Method{i}()");
            sb.AppendLine("    {");
            sb.AppendLine("        var x = 1;");
            sb.AppendLine("        Console.WriteLine(x);");
            sb.AppendLine("    }");
            if (i < MethodCount)
                sb.AppendLine();
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    /// <summary>
    /// Derived classes provide the analyzer references to use for the benchmark.
    /// </summary>
    protected abstract ImmutableArray<AnalyzerReference> CreateAnalyzerReferences();

    [IterationSetup(Target = nameof(ColdFullDocument_NoCache))]
    public void SetupCold()
    {
        SetupWorkspaceAndDocument();
    }

    [IterationSetup(Target = nameof(WarmIncrementalMemberEdit))]
    public void SetupWarm()
    {
        SetupWorkspaceAndDocument();

        // Prime the cache: run full diagnostics once so the IncrementalMemberEditAnalyzer caches results
        var document = _workspace.CurrentSolution.GetDocument(_documentId)!;
        _ = _diagnosticService.GetDiagnosticsForSpanAsync(
            document, range: null, DiagnosticKind.All, CancellationToken.None).Result;

        // Make a member-body edit using text.WithChanges() to preserve the change chain
        // (critical — the analyzer uses text change tracking to detect the edited member)
        var text = document.GetTextSynchronously(CancellationToken.None);

        var sourceText = text.ToString();
        var bodyStart = sourceText.IndexOf(OriginalMethodBody, StringComparison.Ordinal);
        var newText = text.WithChanges(new TextChange(
            new TextSpan(bodyStart, OriginalMethodBody.Length),
            EditedMethodBody));

        var newSolution = _workspace.CurrentSolution.WithDocumentText(_documentId, newText);
        _workspace.TryApplyChanges(newSolution);
    }

    [IterationCleanup]
    public void Cleanup()
    {
        _workspace?.Dispose();
        _workspace = null!;
    }

    [Benchmark(Description = "Full document diagnostics (cold, no cache)")]
    public async Task ColdFullDocument_NoCache()
    {
        var document = _workspace.CurrentSolution.GetDocument(_documentId)!;
        var diagnostics = await _diagnosticService.GetDiagnosticsForSpanAsync(
            document, range: null, DiagnosticKind.All, CancellationToken.None);
    }

    [Benchmark(Description = "Incremental diagnostics (warm cache, member edit)")]
    public async Task WarmIncrementalMemberEdit()
    {
        var document = _workspace.CurrentSolution.GetDocument(_documentId)!;
        var diagnostics = await _diagnosticService.GetDiagnosticsForSpanAsync(
            document, range: null, DiagnosticKind.All, CancellationToken.None);
    }

    private void SetupWorkspaceAndDocument()
    {
        var hostServices = MefHostServices.DefaultHost;
        _workspace = new AdhocWorkspace(hostServices);

        var projectId = ProjectId.CreateNewId();
        _documentId = DocumentId.CreateNewId(projectId);

        var solution = _workspace.CurrentSolution
            .AddProject(projectId, "BenchmarkProject", "BenchmarkProject", LanguageNames.CSharp)
            .WithProjectCompilationOptions(projectId, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .WithProjectMetadataReferences(projectId, new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
            })
            .AddDocument(_documentId, "LargeComponent.cs", SourceText.From(s_initialSource));

        foreach (var analyzerRef in CreateAnalyzerReferences())
            solution = solution.AddAnalyzerReference(projectId, analyzerRef);

        _workspace.TryApplyChanges(solution);
        _diagnosticService = _workspace.Services.GetRequiredService<IDiagnosticAnalyzerService>();
    }
}

/// <summary>
/// Uses targeted analyzers (one SemanticSpan built-in + one external) to isolate the
/// incremental member-edit optimization. Shows the best-case improvement when analyzers
/// are properly categorized for incremental analysis.
/// </summary>
[MemoryDiagnoser]
public class DiagnosticIncrementalAnalysis_TargetedAnalyzers : DiagnosticIncrementalAnalysisBenchmarkBase
{
    protected override ImmutableArray<AnalyzerReference> CreateAnalyzerReferences()
    {
        var builtInAnalyzer = new BuiltInFieldAnalyzer("BENCH001", DiagnosticAnalyzerCategory.SemanticSpanAnalysis);
        var externalAnalyzer = new ExternalFieldAnalyzer("BENCH002");
        return ImmutableArray.Create<AnalyzerReference>(
            new AnalyzerImageReference(ImmutableArray.Create<DiagnosticAnalyzer>(builtInAnalyzer, externalAnalyzer)));
    }

    private sealed class BuiltInFieldAnalyzer : DiagnosticAnalyzer, IBuiltInAnalyzer
    {
        private readonly DiagnosticAnalyzerCategory _category;
        public DiagnosticDescriptor Descriptor { get; }

        public BuiltInFieldAnalyzer(string diagnosticId, DiagnosticAnalyzerCategory category)
        {
            _category = category;
            Descriptor = new DiagnosticDescriptor(
                diagnosticId, "Title", "Message for {0}", "Benchmark",
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true);
        }

        public bool IsHighPriority => false;
        public DiagnosticAnalyzerCategory GetAnalyzerCategory() => _category;

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Descriptor);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(compilationContext =>
            {
                compilationContext.RegisterSymbolAction(symbolContext =>
                {
                    symbolContext.ReportDiagnostic(
                        Diagnostic.Create(Descriptor, symbolContext.Symbol.Locations[0], symbolContext.Symbol.Name));
                }, SymbolKind.Field);
            });
        }
    }

    private sealed class ExternalFieldAnalyzer : DiagnosticAnalyzer
    {
        public DiagnosticDescriptor Descriptor { get; }

        public ExternalFieldAnalyzer(string diagnosticId)
        {
            Descriptor = new DiagnosticDescriptor(
                diagnosticId, "Title", "External message for {0}", "Benchmark",
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true);
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Descriptor);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(compilationContext =>
            {
                compilationContext.RegisterSymbolAction(symbolContext =>
                {
                    symbolContext.ReportDiagnostic(
                        Diagnostic.Create(Descriptor, symbolContext.Symbol.Locations[0], symbolContext.Symbol.Name));
                }, SymbolKind.Field);
            });
        }
    }
}

/// <summary>
/// Uses the real Roslyn built-in analyzers (from Features + CSharp.Features assemblies)
/// to measure incremental analysis performance with the full analyzer mix that runs in VS.
/// </summary>
[MemoryDiagnoser]
public class DiagnosticIncrementalAnalysis_RealAnalyzers : DiagnosticIncrementalAnalysisBenchmarkBase
{
    protected override ImmutableArray<AnalyzerReference> CreateAnalyzerReferences()
    {
        var binDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        return ImmutableArray.Create<AnalyzerReference>(
            new AnalyzerFileReference(Path.Combine(binDir, "Microsoft.CodeAnalysis.Features.dll"), AssemblyLoader.Instance),
            new AnalyzerFileReference(Path.Combine(binDir, "Microsoft.CodeAnalysis.CSharp.Features.dll"), AssemblyLoader.Instance));
    }

    private sealed class AssemblyLoader : IAnalyzerAssemblyLoader
    {
        public static readonly AssemblyLoader Instance = new();
        public void AddDependencyLocation(string fullPath) { }
        public Assembly LoadFromPath(string fullPath) => Assembly.LoadFrom(fullPath);
    }
}
