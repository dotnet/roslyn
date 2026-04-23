// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AnalyzerRunner;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace IdeCoreBenchmarks;

/// <summary>
/// Benchmarks the IncrementalMemberEditAnalyzer on a real Roslyn project (the C# compiler).
/// Opens the CSharp compiler project via MSBuildWorkspace and measures diagnostic computation
/// on LanguageParser.cs (~663 KB, ~415 methods) — a realistically large file.
///
/// IMPORTANT: We avoid calling TryApplyChanges on the MSBuildWorkspace because it writes
/// changes to disk. Instead, we work with solution snapshots and store the target document
/// in a field for the benchmark method to use.
/// </summary>
[MemoryDiagnoser]
public class DiagnosticIncrementalAnalysisRealProjectBenchmarks
{
    // The text we search for and replace to simulate a member-body edit.
    // IsPossibleGlobalAttributeDeclaration is a small method in LanguageParser.cs (line ~1030).
    // We reorder the last two && conditions — semantically equivalent, but a textual change.
    private const string OriginalBody =
        "return this.CurrentToken.Kind == SyntaxKind.OpenBracketToken\r\n" +
        "                && IsGlobalAttributeTarget(this.PeekToken(1))\r\n" +
        "                && this.PeekToken(2).Kind == SyntaxKind.ColonToken;";

    private const string EditedBody =
        "return this.CurrentToken.Kind == SyntaxKind.OpenBracketToken\r\n" +
        "                && this.PeekToken(2).Kind == SyntaxKind.ColonToken\r\n" +
        "                && IsGlobalAttributeTarget(this.PeekToken(1));";

    private MSBuildWorkspace _workspace = null!;
    private IDiagnosticAnalyzerService _diagnosticService = null!;
    private DocumentId _languageParserDocId;
    private SourceText _originalText = null!;

    // The document to use in the benchmark method, set by IterationSetup
    private Document _benchmarkDocument = null!;

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        var roslynRoot = Environment.GetEnvironmentVariable(Program.RoslynRootPathEnvVariableName);
        var csharpProjectPath = Path.Combine(roslynRoot,
            @"src\Compilers\CSharp\Portable\Microsoft.CodeAnalysis.CSharp.csproj");

        if (!File.Exists(csharpProjectPath))
            throw new FileNotFoundException($"CSharp compiler project not found at {csharpProjectPath}");

        _workspace = AnalyzerRunnerHelper.CreateWorkspace();
        var project = await _workspace.OpenProjectAsync(csharpProjectPath, progress: null, CancellationToken.None);

        var languageParserDoc = project.Documents
            .FirstOrDefault(d => d.Name == "LanguageParser.cs"
                && d.Folders.Any(f => f.Equals("Parser", StringComparison.OrdinalIgnoreCase)));

        if (languageParserDoc is null)
            throw new InvalidOperationException("Could not find LanguageParser.cs in the CSharp compiler project");

        _languageParserDocId = languageParserDoc.Id;
        _originalText = await languageParserDoc.GetTextAsync(CancellationToken.None);

        if (!_originalText.ToString().Contains(OriginalBody))
            throw new InvalidOperationException($"Could not find edit target in LanguageParser.cs: '{OriginalBody}'");

        _diagnosticService = _workspace.Services.GetRequiredService<IDiagnosticAnalyzerService>();
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _workspace?.Dispose();
        _workspace = null!;
    }

    /// <summary>
    /// Creates a fresh document version (new SourceText, no cache) and pre-warms the compilation.
    /// The benchmark measures diagnostic analysis only, not compilation creation (~115 MB).
    /// </summary>
    [IterationSetup(Target = nameof(ColdFullDocument_RealProject))]
    public void SetupCold()
    {
        var freshText = SourceText.From(_originalText.ToString(), _originalText.Encoding, _originalText.ChecksumAlgorithm);
        var solution = _workspace.CurrentSolution.WithDocumentText(_languageParserDocId, freshText);

        // Pre-warm the compilation (not measured by BDN)
        _benchmarkDocument = solution.GetDocument(_languageParserDocId)!;
        _ = _benchmarkDocument.Project.GetCompilationAsync(CancellationToken.None).Result;
    }

    /// <summary>
    /// Prime the diagnostic cache, make a member-body edit, and pre-warm the new compilation.
    /// The benchmark measures incremental diagnostic analysis with warm cache.
    /// </summary>
    [IterationSetup(Target = nameof(WarmIncrementalMemberEdit_RealProject))]
    public void SetupWarm()
    {
        // Step 1: Create a fresh document version
        var freshText = SourceText.From(_originalText.ToString(), _originalText.Encoding, _originalText.ChecksumAlgorithm);
        var solution = _workspace.CurrentSolution.WithDocumentText(_languageParserDocId, freshText);

        // Step 2: Prime the diagnostic cache by running full analysis
        var primeDoc = solution.GetDocument(_languageParserDocId)!;
        _ = _diagnosticService.GetDiagnosticsForSpanAsync(
            primeDoc, range: null, DiagnosticKind.All, CancellationToken.None).Result;

        // Step 3: Make a member-body edit preserving the SourceText change chain
        // (critical for text-tracking-based member detection in IncrementalMemberEditAnalyzer)
        var text = primeDoc.GetTextSynchronously(CancellationToken.None);
        var sourceString = text.ToString();
        var editStart = sourceString.IndexOf(OriginalBody, StringComparison.Ordinal);
        var editedText = text.WithChanges(new TextChange(
            new TextSpan(editStart, OriginalBody.Length),
            EditedBody));

        var editedSolution = solution.WithDocumentText(_languageParserDocId, editedText);

        // Step 4: Pre-warm the compilation for the edited document
        _benchmarkDocument = editedSolution.GetDocument(_languageParserDocId)!;
        _ = _benchmarkDocument.Project.GetCompilationAsync(CancellationToken.None).Result;
    }

    /// <summary>
    /// Full diagnostics on LanguageParser.cs with no prior cache (compilation pre-warmed).
    /// Exercises the semantic pass merge optimization.
    /// </summary>
    [Benchmark(Description = "LanguageParser.cs — Full diagnostics (cold, no cache)")]
    public async Task<int> ColdFullDocument_RealProject()
    {
        var diagnostics = await _diagnosticService.GetDiagnosticsForSpanAsync(
            _benchmarkDocument, range: null, DiagnosticKind.All, CancellationToken.None);
        return diagnostics.Length;
    }

    /// <summary>
    /// Incremental diagnostics on LanguageParser.cs after a member-body edit with warm cache
    /// (compilation pre-warmed). Exercises the diagnostic splicing optimization.
    /// </summary>
    [Benchmark(Description = "LanguageParser.cs — Incremental diagnostics (warm cache, member edit)")]
    public async Task<int> WarmIncrementalMemberEdit_RealProject()
    {
        var diagnostics = await _diagnosticService.GetDiagnosticsForSpanAsync(
            _benchmarkDocument, range: null, DiagnosticKind.All, CancellationToken.None);
        return diagnostics.Length;
    }
}
