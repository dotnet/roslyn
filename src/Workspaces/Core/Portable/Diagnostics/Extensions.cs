// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Workspaces.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal static partial class Extensions
{
    private static readonly ConditionalWeakTable<Project, AsyncLazy<Checksum>> s_projectToDiagnosticChecksum = new();

    public static async Task<ImmutableArray<Diagnostic>> ToDiagnosticsAsync(this IEnumerable<DiagnosticData> diagnostics, Project project, CancellationToken cancellationToken)
    {
        var result = ArrayBuilder<Diagnostic>.GetInstance();
        foreach (var diagnostic in diagnostics)
        {
            result.Add(await diagnostic.ToDiagnosticAsync(project, cancellationToken).ConfigureAwait(false));
        }

        return result.ToImmutableAndFree();
    }

    public static ValueTask<ImmutableArray<Location>> ConvertLocationsAsync(this IReadOnlyCollection<DiagnosticDataLocation> locations, Project project, CancellationToken cancellationToken)
        => locations.SelectAsArrayAsync((location, project, cancellationToken) => location.ConvertLocationAsync(project, cancellationToken), project, cancellationToken);

    public static async ValueTask<Location> ConvertLocationAsync(
        this DiagnosticDataLocation dataLocation, Project project, CancellationToken cancellationToken)
    {
        if (dataLocation.DocumentId == null)
            return Location.None;

        var textDocument = project.GetTextDocument(dataLocation.DocumentId)
            ?? await project.GetSourceGeneratedDocumentAsync(dataLocation.DocumentId, cancellationToken).ConfigureAwait(false);
        if (textDocument == null)
            return Location.None;

        var text = await textDocument.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
        var tree = textDocument is Document { SupportsSyntaxTree: true } document
            ? await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false)
            : null;

        // Intentionally get the unmapped text span (the span in the original document).  If there is any mapping it
        // will be reapplied with tree.GetLocation below.
        var span = dataLocation.UnmappedFileSpan.GetClampedTextSpan(text);

        // Defer to the tree if we have one.  This will make sure that remapping is properly supported.
        if (tree != null)
            return tree.GetLocation(span);

        if (textDocument.FilePath is null)
            return Location.None;

        // Otherwise, just produce a basic location using the path/span information we determined.
        return Location.Create(textDocument.FilePath, span, text.Lines.GetLinePositionSpan(span));
    }

    public static string GetAnalyzerId(this DiagnosticAnalyzer analyzer)
    {
        // Get the unique ID for given diagnostic analyzer.
        var type = analyzer.GetType();
        return GetAssemblyQualifiedName(type);
    }

    /// <summary>
    /// Cache of a <see cref="Type"/> to its <see cref="Type.AssemblyQualifiedName"/>.  We cache this as the latter
    /// computes and allocates expensively every time it is called.
    /// </summary>
    private static ImmutableSegmentedDictionary<Type, string> s_typeToAssemblyQualifiedName = ImmutableSegmentedDictionary<Type, string>.Empty;

    private static string GetAssemblyQualifiedName(Type type)
    {
        // AnalyzerFileReference now includes things like versions, public key as part of its identity.
        // so we need to consider them.
        return RoslynImmutableInterlocked.GetOrAdd(
            ref s_typeToAssemblyQualifiedName,
            type,
            static type => type.AssemblyQualifiedName ?? throw ExceptionUtilities.UnexpectedValue(type));
    }

    public static bool IsFeaturesAnalyzer(this AnalyzerReference reference)
    {
        var fileNameSpan = reference.FullPath.AsSpan(FileNameUtilities.IndexOfFileName(reference.FullPath));
        return
          fileNameSpan.Equals("Microsoft.CodeAnalysis.Features.dll".AsSpan(), StringComparison.OrdinalIgnoreCase) ||
          fileNameSpan.Equals("Microsoft.CodeAnalysis.CSharp.Features.dll".AsSpan(), StringComparison.OrdinalIgnoreCase) ||
          fileNameSpan.Equals("Microsoft.CodeAnalysis.VisualBasic.Features.dll".AsSpan(), StringComparison.OrdinalIgnoreCase);
    }

    public static async Task<ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResultBuilder>> ToResultBuilderMapAsync(
        this AnalysisResultPair analysisResult,
        ImmutableArray<Diagnostic> additionalPragmaSuppressionDiagnostics,
        DocumentAnalysisScope? documentAnalysisScope,
        Project project,
        ImmutableArray<DiagnosticAnalyzer> projectAnalyzers,
        ImmutableArray<DiagnosticAnalyzer> hostAnalyzers,
        SkippedHostAnalyzersInfo skippedAnalyzersInfo,
        CancellationToken cancellationToken)
    {
        SyntaxTree? treeToAnalyze = null;
        AdditionalText? additionalFileToAnalyze = null;
        if (documentAnalysisScope != null)
        {
            if (documentAnalysisScope.TextDocument is Document document)
            {
                treeToAnalyze = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                additionalFileToAnalyze = documentAnalysisScope.AdditionalFile;
            }
        }

        var builder = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, DiagnosticAnalysisResultBuilder>();
        foreach (var analyzer in projectAnalyzers.ConcatFast(hostAnalyzers))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (skippedAnalyzersInfo.SkippedAnalyzers.Contains(analyzer))
            {
                continue;
            }

            var result = new DiagnosticAnalysisResultBuilder(project);
            var diagnosticIdsToFilter = skippedAnalyzersInfo.FilteredDiagnosticIdsForAnalyzers.GetValueOrDefault(
                analyzer,
                []);

            if (documentAnalysisScope != null)
            {
                RoslynDebug.Assert(treeToAnalyze != null || additionalFileToAnalyze != null);
                var spanToAnalyze = documentAnalysisScope.Span;
                var kind = documentAnalysisScope.Kind;

                ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>? diagnosticsByAnalyzerMap;
                switch (kind)
                {
                    case AnalysisKind.Syntax:
                        if (treeToAnalyze != null)
                        {
                            if (analysisResult.MergedSyntaxDiagnostics.TryGetValue(treeToAnalyze, out diagnosticsByAnalyzerMap))
                            {
                                AddAnalyzerDiagnosticsToResult(analyzer, diagnosticsByAnalyzerMap, ref result,
                                    treeToAnalyze, additionalDocumentId: null, spanToAnalyze, AnalysisKind.Syntax, diagnosticIdsToFilter);
                            }
                        }
                        else if (analysisResult.MergedAdditionalFileDiagnostics.TryGetValue(additionalFileToAnalyze!, out diagnosticsByAnalyzerMap))
                        {
                            AddAnalyzerDiagnosticsToResult(analyzer, diagnosticsByAnalyzerMap, ref result,
                                tree: null, documentAnalysisScope.TextDocument.Id, spanToAnalyze, AnalysisKind.Syntax, diagnosticIdsToFilter);
                        }

                        break;

                    case AnalysisKind.Semantic:
                        if (analysisResult.MergedSemanticDiagnostics.TryGetValue(treeToAnalyze!, out diagnosticsByAnalyzerMap))
                        {
                            AddAnalyzerDiagnosticsToResult(analyzer, diagnosticsByAnalyzerMap, ref result,
                                treeToAnalyze, additionalDocumentId: null, spanToAnalyze, AnalysisKind.Semantic, diagnosticIdsToFilter);
                        }

                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(kind);
                }
            }
            else
            {
                foreach (var (tree, diagnosticsByAnalyzerMap) in analysisResult.MergedSyntaxDiagnostics)
                {
                    AddAnalyzerDiagnosticsToResult(analyzer, diagnosticsByAnalyzerMap, ref result,
                        tree, additionalDocumentId: null, span: null, AnalysisKind.Syntax, diagnosticIdsToFilter);
                }

                foreach (var (tree, diagnosticsByAnalyzerMap) in analysisResult.MergedSemanticDiagnostics)
                {
                    AddAnalyzerDiagnosticsToResult(analyzer, diagnosticsByAnalyzerMap, ref result,
                        tree, additionalDocumentId: null, span: null, AnalysisKind.Semantic, diagnosticIdsToFilter);
                }

                foreach (var (file, diagnosticsByAnalyzerMap) in analysisResult.MergedAdditionalFileDiagnostics)
                {
                    var additionalDocumentId = project.GetDocumentForFile(file);
                    var kind = additionalDocumentId != null ? AnalysisKind.Syntax : AnalysisKind.NonLocal;
                    AddAnalyzerDiagnosticsToResult(analyzer, diagnosticsByAnalyzerMap, ref result,
                        tree: null, additionalDocumentId, span: null, kind, diagnosticIdsToFilter);
                }

                AddAnalyzerDiagnosticsToResult(analyzer, analysisResult.MergedCompilationDiagnostics, ref result,
                    tree: null, additionalDocumentId: null, span: null, AnalysisKind.NonLocal, diagnosticIdsToFilter);
            }

            // Special handling for pragma suppression diagnostics.
            if (!additionalPragmaSuppressionDiagnostics.IsEmpty &&
                analyzer is IPragmaSuppressionsAnalyzer)
            {
                if (documentAnalysisScope != null)
                {
                    if (treeToAnalyze != null)
                    {
                        var diagnostics = additionalPragmaSuppressionDiagnostics.WhereAsArray(d => d.Location.SourceTree == treeToAnalyze);
                        AddDiagnosticsToResult(diagnostics, ref result, treeToAnalyze, additionalDocumentId: null,
                            documentAnalysisScope.Span, AnalysisKind.Semantic, diagnosticIdsToFilter);
                    }
                }
                else
                {
                    foreach (var group in additionalPragmaSuppressionDiagnostics.GroupBy(d => d.Location.SourceTree!))
                    {
                        AddDiagnosticsToResult(group.AsImmutable(), ref result, group.Key, additionalDocumentId: null,
                            span: null, AnalysisKind.Semantic, diagnosticIdsToFilter);
                    }
                }

                additionalPragmaSuppressionDiagnostics = [];
            }

            builder.Add(analyzer, result);
        }

        return builder.ToImmutable();

        static void AddAnalyzerDiagnosticsToResult(
            DiagnosticAnalyzer analyzer,
            ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>> diagnosticsByAnalyzer,
            ref DiagnosticAnalysisResultBuilder result,
            SyntaxTree? tree,
            DocumentId? additionalDocumentId,
            TextSpan? span,
            AnalysisKind kind,
            ImmutableArray<string> diagnosticIdsToFilter)
        {
            if (diagnosticsByAnalyzer.TryGetValue(analyzer, out var diagnostics))
            {
                AddDiagnosticsToResult(diagnostics, ref result,
                    tree, additionalDocumentId, span, kind, diagnosticIdsToFilter);
            }
        }

        static void AddDiagnosticsToResult(
            ImmutableArray<Diagnostic> diagnostics,
            ref DiagnosticAnalysisResultBuilder result,
            SyntaxTree? tree,
            DocumentId? additionalDocumentId,
            TextSpan? span,
            AnalysisKind kind,
            ImmutableArray<string> diagnosticIdsToFilter)
        {
            if (diagnostics.IsEmpty)
            {
                return;
            }

            diagnostics = diagnostics.Filter(diagnosticIdsToFilter, span);

            switch (kind)
            {
                case AnalysisKind.Syntax:
                    if (tree != null)
                    {
                        Debug.Assert(diagnostics.All(d => d.Location.SourceTree == tree));
                        result.AddSyntaxDiagnostics(tree!, diagnostics);
                    }
                    else
                    {
                        RoslynDebug.Assert(additionalDocumentId != null);
                        result.AddExternalSyntaxDiagnostics(additionalDocumentId, diagnostics);
                    }

                    break;

                case AnalysisKind.Semantic:
                    Debug.Assert(diagnostics.All(d => d.Location.SourceTree == tree));
                    result.AddSemanticDiagnostics(tree!, diagnostics);
                    break;

                default:
                    result.AddCompilationDiagnostics(diagnostics);
                    break;
            }
        }
    }

    /// <summary>
    /// Filters out the diagnostics with the specified <paramref name="diagnosticIdsToFilter"/>.
    /// If <paramref name="filterSpan"/> is non-null, filters out diagnostics with location outside this span.
    /// </summary>
    public static ImmutableArray<Diagnostic> Filter(
        this ImmutableArray<Diagnostic> diagnostics,
        ImmutableArray<string> diagnosticIdsToFilter,
        TextSpan? filterSpan = null)
    {
        if (diagnosticIdsToFilter.IsEmpty && !filterSpan.HasValue)
        {
            return diagnostics;
        }

        return diagnostics.RemoveAll(diagnostic =>
            diagnosticIdsToFilter.Contains(diagnostic.Id) ||
            filterSpan.HasValue && !filterSpan.Value.IntersectsWith(diagnostic.Location.SourceSpan));
    }

    public static async Task<(AnalysisResultPair? analysisResult, ImmutableArray<Diagnostic> additionalDiagnostics)> GetAnalysisResultAsync(
        this CompilationWithAnalyzersPair compilationWithAnalyzers,
        DocumentAnalysisScope? documentAnalysisScope,
        Project project,
        DiagnosticAnalyzerInfoCache analyzerInfoCache,
        CancellationToken cancellationToken)
    {
        var result = await GetAnalysisResultAsync(compilationWithAnalyzers, documentAnalysisScope, cancellationToken).ConfigureAwait(false);
        var additionalDiagnostics = await compilationWithAnalyzers.GetPragmaSuppressionAnalyzerDiagnosticsAsync(
            documentAnalysisScope, project, analyzerInfoCache, cancellationToken).ConfigureAwait(false);
        return (result, additionalDiagnostics);
    }

    private static async Task<AnalysisResultPair?> GetAnalysisResultAsync(
        CompilationWithAnalyzersPair compilationWithAnalyzers,
        DocumentAnalysisScope? documentAnalysisScope,
        CancellationToken cancellationToken)
    {
        if (documentAnalysisScope == null)
        {
            return await compilationWithAnalyzers.GetAnalysisResultAsync(cancellationToken).ConfigureAwait(false);
        }

        Debug.Assert(documentAnalysisScope.ProjectAnalyzers.ToSet().IsSubsetOf(compilationWithAnalyzers.ProjectAnalyzers));
        Debug.Assert(documentAnalysisScope.HostAnalyzers.ToSet().IsSubsetOf(compilationWithAnalyzers.HostAnalyzers));

        switch (documentAnalysisScope.Kind)
        {
            case AnalysisKind.Syntax:
                if (documentAnalysisScope.TextDocument is Document document)
                {
                    var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                    return await compilationWithAnalyzers.GetAnalysisResultAsync(tree, documentAnalysisScope.Span, documentAnalysisScope.ProjectAnalyzers, documentAnalysisScope.HostAnalyzers, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    return await compilationWithAnalyzers.GetAnalysisResultAsync(documentAnalysisScope.AdditionalFile, documentAnalysisScope.Span, documentAnalysisScope.ProjectAnalyzers, documentAnalysisScope.HostAnalyzers, cancellationToken).ConfigureAwait(false);
                }

            case AnalysisKind.Semantic:
                var model = await ((Document)documentAnalysisScope.TextDocument).GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                return await compilationWithAnalyzers.GetAnalysisResultAsync(model, documentAnalysisScope.Span, documentAnalysisScope.ProjectAnalyzers, documentAnalysisScope.HostAnalyzers, cancellationToken).ConfigureAwait(false);

            default:
                throw ExceptionUtilities.UnexpectedValue(documentAnalysisScope.Kind);
        }
    }

    private static async Task<ImmutableArray<Diagnostic>> GetPragmaSuppressionAnalyzerDiagnosticsAsync(
        this CompilationWithAnalyzersPair compilationWithAnalyzers,
        DocumentAnalysisScope? documentAnalysisScope,
        Project project,
        DiagnosticAnalyzerInfoCache analyzerInfoCache,
        CancellationToken cancellationToken)
    {
        var hostAnalyzers = documentAnalysisScope?.HostAnalyzers ?? compilationWithAnalyzers.HostAnalyzers;
        var suppressionAnalyzer = hostAnalyzers.OfType<IPragmaSuppressionsAnalyzer>().FirstOrDefault();
        if (suppressionAnalyzer == null)
            return [];

        RoslynDebug.AssertNotNull(compilationWithAnalyzers.HostCompilationWithAnalyzers);

        if (documentAnalysisScope != null)
        {
            if (documentAnalysisScope.TextDocument is not Document document)
                return [];

            using var _ = ArrayBuilder<Diagnostic>.GetInstance(out var diagnosticsBuilder);
            await AnalyzeDocumentAsync(
                compilationWithAnalyzers.HostCompilationWithAnalyzers, analyzerInfoCache, suppressionAnalyzer,
                document, documentAnalysisScope.Span, diagnosticsBuilder.Add, cancellationToken).ConfigureAwait(false);
            return diagnosticsBuilder.ToImmutableAndClear();
        }
        else
        {
            if (compilationWithAnalyzers.ConcurrentAnalysis)
            {
                return await ProducerConsumer<Diagnostic>.RunParallelAsync(
                    source: project.GetAllRegularAndSourceGeneratedDocumentsAsync(cancellationToken),
                    produceItems: static async (document, callback, args, cancellationToken) =>
                    {
                        var (hostCompilationWithAnalyzers, analyzerInfoCache, suppressionAnalyzer) = args;
                        await AnalyzeDocumentAsync(
                            hostCompilationWithAnalyzers, analyzerInfoCache, suppressionAnalyzer,
                            document, span: null, callback, cancellationToken).ConfigureAwait(false);
                    },
                    args: (compilationWithAnalyzers.HostCompilationWithAnalyzers, analyzerInfoCache, suppressionAnalyzer),
                    cancellationToken).ConfigureAwait(false);
            }
            else
            {
                using var _ = ArrayBuilder<Diagnostic>.GetInstance(out var diagnosticsBuilder);
                await foreach (var document in project.GetAllRegularAndSourceGeneratedDocumentsAsync(cancellationToken))
                {
                    await AnalyzeDocumentAsync(
                        compilationWithAnalyzers.HostCompilationWithAnalyzers, analyzerInfoCache, suppressionAnalyzer,
                        document, span: null, diagnosticsBuilder.Add, cancellationToken).ConfigureAwait(false);
                }

                return diagnosticsBuilder.ToImmutableAndClear();
            }
        }

        static async Task AnalyzeDocumentAsync(
            CompilationWithAnalyzers hostCompilationWithAnalyzers,
            DiagnosticAnalyzerInfoCache analyzerInfoCache,
            IPragmaSuppressionsAnalyzer suppressionAnalyzer,
            Document document,
            TextSpan? span,
            Action<Diagnostic> reportDiagnostic,
            CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            await suppressionAnalyzer.AnalyzeAsync(
                semanticModel, span, hostCompilationWithAnalyzers, analyzerInfoCache.GetDiagnosticDescriptors, reportDiagnostic, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Calculates a checksum that contains a project's checksum along with a checksum for each of the project's 
    /// transitive dependencies.
    /// </summary>
    /// <remarks>
    /// This checksum calculation can be used for cases where a feature needs to know if the semantics in this project
    /// changed.  For example, for diagnostics or caching computed semantic data. The goal is to ensure that changes to
    /// <list type="bullet">
    ///    <item>Files inside the current project</item>
    ///    <item>Project properties of the current project</item>
    ///    <item>Visible files in referenced projects</item>
    ///    <item>Project properties in referenced projects</item>
    /// </list>
    /// are reflected in the metadata we keep so that comparing solutions accurately tells us when we need to recompute
    /// semantic work.   
    /// 
    /// <para>This method of checking for changes has a few important properties that differentiate it from other methods of determining project version.
    /// <list type="bullet">
    ///    <item>Changes to methods inside the current project will be reflected to compute updated diagnostics.
    ///        <see cref="Project.GetDependentSemanticVersionAsync(CancellationToken)"/> does not change as it only returns top level changes.</item>
    ///    <item>Reloading a project without making any changes will re-use cached diagnostics.
    ///        <see cref="Project.GetDependentSemanticVersionAsync(CancellationToken)"/> changes as the project is removed, then added resulting in a version change.</item>
    /// </list>   
    /// </para>
    /// This checksum is also affected by the <see cref="SourceGeneratorExecutionVersion"/> for this project.
    /// As such, it is not usable across different sessions of a particular host.
    /// </remarks>
    public static Task<Checksum> GetDiagnosticChecksumAsync(this Project? project, CancellationToken cancellationToken)
    {
        if (project is null)
            return SpecializedTasks.Default<Checksum>();

        var lazyChecksum = s_projectToDiagnosticChecksum.GetValue(
            project,
            static project => AsyncLazy.Create(
                static (project, cancellationToken) => ComputeDiagnosticChecksumAsync(project, cancellationToken),
                project));

        return lazyChecksum.GetValueAsync(cancellationToken);

        static async Task<Checksum> ComputeDiagnosticChecksumAsync(Project project, CancellationToken cancellationToken)
        {
            var solution = project.Solution;

            using var _ = ArrayBuilder<Checksum>.GetInstance(out var tempChecksumArray);

            // Mix in the SG information for this project.  That way if it changes, we will have a different
            // checksum (since semantics could have changed because of this).
            if (solution.CompilationState.SourceGeneratorExecutionVersionMap.Map.TryGetValue(project.Id, out var executionVersion))
                tempChecksumArray.Add(executionVersion.Checksum);

            // Get the checksum for the project itself.  Note: this will normally be cached.  As such, even if we
            // have a different Project instance (due to a change in an unrelated project), this will be fast to
            // compute and return.
            var projectChecksum = await project.State.GetChecksumAsync(cancellationToken).ConfigureAwait(false);
            tempChecksumArray.Add(projectChecksum);

            // Calculate a checksum this project and for each dependent project that could affect semantics for this
            // project. We order the projects guid so that we are resilient to the underlying in-memory graph structure
            // changing this arbitrarily.
            foreach (var projectRef in project.ProjectReferences.OrderBy(r => r.ProjectId.Id))
            {
                // Note that these checksums should only actually be calculated once, if the project is unchanged
                // the same checksum will be returned.
                tempChecksumArray.Add(await GetDiagnosticChecksumAsync(
                    solution.GetProject(projectRef.ProjectId), cancellationToken).ConfigureAwait(false));
            }

            return Checksum.Create(tempChecksumArray);
        }
    }
}
