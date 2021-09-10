// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Workspaces.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal static partial class Extensions
    {
        public static readonly CultureInfo USCultureInfo = new("en-US");

        public static string GetBingHelpMessage(this Diagnostic diagnostic, OptionSet options)
        {
            // We use the ENU version of the message for bing search.
            return options.GetOption(InternalDiagnosticsOptions.PutCustomTypeInBingSearch) ?
                diagnostic.GetMessage(USCultureInfo) : diagnostic.Descriptor.GetBingHelpMessage();
        }

        public static string GetBingHelpMessage(this DiagnosticDescriptor descriptor)
        {
            // We use the ENU version of the message for bing search.
            return descriptor.MessageFormat.ToString(USCultureInfo);
        }

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
            this DiagnosticDataLocation? dataLocation, Project project, CancellationToken cancellationToken)
        {
            if (dataLocation?.DocumentId == null)
            {
                return Location.None;
            }

            var textDocument = project.GetTextDocument(dataLocation.DocumentId);
            if (textDocument == null)
            {
                return Location.None;
            }

            if (textDocument is Document document && document.SupportsSyntaxTree)
            {
                var syntacticDocument = await SyntacticDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);
                return dataLocation.ConvertLocation(syntacticDocument);
            }

            return dataLocation.ConvertLocation();
        }

        public static Location ConvertLocation(
            this DiagnosticDataLocation dataLocation, SyntacticDocument? document = null)
        {
            if (dataLocation?.DocumentId == null)
            {
                return Location.None;
            }

            if (document == null)
            {
                if (dataLocation.OriginalFilePath == null || dataLocation.SourceSpan == null)
                {
                    return Location.None;
                }

                var span = dataLocation.SourceSpan.Value;
                return Location.Create(dataLocation.OriginalFilePath, span, new LinePositionSpan(
                    new LinePosition(dataLocation.OriginalStartLine, dataLocation.OriginalStartColumn),
                    new LinePosition(dataLocation.OriginalEndLine, dataLocation.OriginalEndColumn)));
            }

            Contract.ThrowIfFalse(dataLocation.DocumentId == document.Document.Id);

            var syntaxTree = document.SyntaxTree;
            return syntaxTree.GetLocation(dataLocation.SourceSpan ?? DiagnosticData.GetTextSpan(dataLocation, document.Text));
        }

        public static string GetAnalyzerId(this DiagnosticAnalyzer analyzer)
        {
            // Get the unique ID for given diagnostic analyzer.
            var type = analyzer.GetType();
            return GetAssemblyQualifiedName(type);
        }

        private static string GetAssemblyQualifiedName(Type type)
        {
            // AnalyzerFileReference now includes things like versions, public key as part of its identity. 
            // so we need to consider them.
            return type.AssemblyQualifiedName ?? throw ExceptionUtilities.UnexpectedValue(type);
        }

        public static async Task<ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResultBuilder>> ToResultBuilderMapAsync(
            this AnalysisResult analysisResult,
            ImmutableArray<Diagnostic> additionalPragmaSuppressionDiagnostics,
            DocumentAnalysisScope? documentAnalysisScope,
            Project project,
            VersionStamp version,
            Compilation compilation,
            IEnumerable<DiagnosticAnalyzer> analyzers,
            SkippedHostAnalyzersInfo skippedAnalyzersInfo,
            bool includeSuppressedDiagnostics,
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
            foreach (var analyzer in analyzers)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (skippedAnalyzersInfo.SkippedAnalyzers.Contains(analyzer))
                {
                    continue;
                }

                var result = new DiagnosticAnalysisResultBuilder(project, version);
                var diagnosticIdsToFilter = skippedAnalyzersInfo.FilteredDiagnosticIdsForAnalyzers.GetValueOrDefault(
                    analyzer,
                    ImmutableArray<string>.Empty);

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
                                if (analysisResult.SyntaxDiagnostics.TryGetValue(treeToAnalyze, out diagnosticsByAnalyzerMap))
                                {
                                    AddAnalyzerDiagnosticsToResult(analyzer, diagnosticsByAnalyzerMap, ref result, compilation,
                                        treeToAnalyze, additionalDocumentId: null, spanToAnalyze, AnalysisKind.Syntax, diagnosticIdsToFilter, includeSuppressedDiagnostics);
                                }
                            }
                            else if (analysisResult.AdditionalFileDiagnostics.TryGetValue(additionalFileToAnalyze!, out diagnosticsByAnalyzerMap))
                            {
                                AddAnalyzerDiagnosticsToResult(analyzer, diagnosticsByAnalyzerMap, ref result, compilation,
                                    tree: null, documentAnalysisScope.TextDocument.Id, spanToAnalyze, AnalysisKind.Syntax, diagnosticIdsToFilter, includeSuppressedDiagnostics);
                            }

                            break;

                        case AnalysisKind.Semantic:
                            if (analysisResult.SemanticDiagnostics.TryGetValue(treeToAnalyze!, out diagnosticsByAnalyzerMap))
                            {
                                AddAnalyzerDiagnosticsToResult(analyzer, diagnosticsByAnalyzerMap, ref result, compilation,
                                    treeToAnalyze, additionalDocumentId: null, spanToAnalyze, AnalysisKind.Semantic, diagnosticIdsToFilter, includeSuppressedDiagnostics);
                            }

                            break;

                        default:
                            throw ExceptionUtilities.UnexpectedValue(kind);
                    }
                }
                else
                {
                    foreach (var (tree, diagnosticsByAnalyzerMap) in analysisResult.SyntaxDiagnostics)
                    {
                        AddAnalyzerDiagnosticsToResult(analyzer, diagnosticsByAnalyzerMap, ref result, compilation,
                            tree, additionalDocumentId: null, span: null, AnalysisKind.Syntax, diagnosticIdsToFilter, includeSuppressedDiagnostics);
                    }

                    foreach (var (tree, diagnosticsByAnalyzerMap) in analysisResult.SemanticDiagnostics)
                    {
                        AddAnalyzerDiagnosticsToResult(analyzer, diagnosticsByAnalyzerMap, ref result, compilation,
                            tree, additionalDocumentId: null, span: null, AnalysisKind.Semantic, diagnosticIdsToFilter, includeSuppressedDiagnostics);
                    }

                    foreach (var (file, diagnosticsByAnalyzerMap) in analysisResult.AdditionalFileDiagnostics)
                    {
                        var additionalDocumentId = project.GetDocumentForFile(file);
                        var kind = additionalDocumentId != null ? AnalysisKind.Syntax : AnalysisKind.NonLocal;
                        AddAnalyzerDiagnosticsToResult(analyzer, diagnosticsByAnalyzerMap, ref result, compilation,
                            tree: null, additionalDocumentId, span: null, kind, diagnosticIdsToFilter, includeSuppressedDiagnostics);
                    }

                    AddAnalyzerDiagnosticsToResult(analyzer, analysisResult.CompilationDiagnostics, ref result, compilation,
                        tree: null, additionalDocumentId: null, span: null, AnalysisKind.NonLocal, diagnosticIdsToFilter, includeSuppressedDiagnostics);
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
                            AddDiagnosticsToResult(diagnostics, ref result, compilation, treeToAnalyze, additionalDocumentId: null,
                                documentAnalysisScope!.Span, AnalysisKind.Semantic, diagnosticIdsToFilter, includeSuppressedDiagnostics);
                        }
                    }
                    else
                    {
                        foreach (var group in additionalPragmaSuppressionDiagnostics.GroupBy(d => d.Location.SourceTree!))
                        {
                            AddDiagnosticsToResult(group.AsImmutable(), ref result, compilation, group.Key, additionalDocumentId: null,
                                span: null, AnalysisKind.Semantic, diagnosticIdsToFilter, includeSuppressedDiagnostics);
                        }
                    }

                    additionalPragmaSuppressionDiagnostics = ImmutableArray<Diagnostic>.Empty;
                }

                builder.Add(analyzer, result);
            }

            return builder.ToImmutable();

            static void AddAnalyzerDiagnosticsToResult(
                DiagnosticAnalyzer analyzer,
                ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>> diagnosticsByAnalyzer,
                ref DiagnosticAnalysisResultBuilder result,
                Compilation compilation,
                SyntaxTree? tree,
                DocumentId? additionalDocumentId,
                TextSpan? span,
                AnalysisKind kind,
                ImmutableArray<string> diagnosticIdsToFilter,
                bool includeSuppressedDiagnostics)
            {
                if (diagnosticsByAnalyzer.TryGetValue(analyzer, out var diagnostics))
                {
                    AddDiagnosticsToResult(diagnostics, ref result, compilation,
                        tree, additionalDocumentId, span, kind, diagnosticIdsToFilter, includeSuppressedDiagnostics);
                }
            }

            static void AddDiagnosticsToResult(
                ImmutableArray<Diagnostic> diagnostics,
                ref DiagnosticAnalysisResultBuilder result,
                Compilation compilation,
                SyntaxTree? tree,
                DocumentId? additionalDocumentId,
                TextSpan? span,
                AnalysisKind kind,
                ImmutableArray<string> diagnosticIdsToFilter,
                bool includeSuppressedDiagnostics)
            {
                if (diagnostics.IsEmpty)
                {
                    return;
                }

                diagnostics = diagnostics.Filter(diagnosticIdsToFilter, includeSuppressedDiagnostics, span);
                Debug.Assert(diagnostics.Length == CompilationWithAnalyzers.GetEffectiveDiagnostics(diagnostics, compilation).Count());

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
        /// If <paramref name="includeSuppressedDiagnostics"/> is false, filters out suppressed diagnostics.
        /// If <paramref name="filterSpan"/> is non-null, filters out diagnostics with location outside this span.
        /// </summary>
        public static ImmutableArray<Diagnostic> Filter(
            this ImmutableArray<Diagnostic> diagnostics,
            ImmutableArray<string> diagnosticIdsToFilter,
            bool includeSuppressedDiagnostics,
            TextSpan? filterSpan = null)
        {
            if (diagnosticIdsToFilter.IsEmpty && includeSuppressedDiagnostics && !filterSpan.HasValue)
            {
                return diagnostics;
            }

            return diagnostics.RemoveAll(diagnostic =>
                diagnosticIdsToFilter.Contains(diagnostic.Id) ||
                !includeSuppressedDiagnostics && diagnostic.IsSuppressed ||
                filterSpan.HasValue && !filterSpan.Value.IntersectsWith(diagnostic.Location.SourceSpan));
        }

        public static async Task<(AnalysisResult result, ImmutableArray<Diagnostic> additionalDiagnostics)> GetAnalysisResultAsync(
            this CompilationWithAnalyzers compilationWithAnalyzers,
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

        private static async Task<AnalysisResult> GetAnalysisResultAsync(
            CompilationWithAnalyzers compilationWithAnalyzers,
            DocumentAnalysisScope? documentAnalysisScope,
            CancellationToken cancellationToken)
        {
            if (documentAnalysisScope == null)
            {
                return await compilationWithAnalyzers.GetAnalysisResultAsync(cancellationToken).ConfigureAwait(false);
            }

            Debug.Assert(documentAnalysisScope.Analyzers.ToSet().IsSubsetOf(compilationWithAnalyzers.Analyzers));

            switch (documentAnalysisScope.Kind)
            {
                case AnalysisKind.Syntax:
                    if (documentAnalysisScope.TextDocument is Document document)
                    {
                        var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                        return await compilationWithAnalyzers.GetAnalysisResultAsync(tree, documentAnalysisScope.Analyzers, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        return await compilationWithAnalyzers.GetAnalysisResultAsync(documentAnalysisScope.AdditionalFile, documentAnalysisScope.Analyzers, cancellationToken).ConfigureAwait(false);
                    }

                case AnalysisKind.Semantic:
                    var model = await ((Document)documentAnalysisScope.TextDocument).GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                    return await compilationWithAnalyzers.GetAnalysisResultAsync(model, documentAnalysisScope.Span, documentAnalysisScope.Analyzers, cancellationToken).ConfigureAwait(false);

                default:
                    throw ExceptionUtilities.UnexpectedValue(documentAnalysisScope.Kind);
            }
        }

        private static async Task<ImmutableArray<Diagnostic>> GetPragmaSuppressionAnalyzerDiagnosticsAsync(
            this CompilationWithAnalyzers compilationWithAnalyzers,
            DocumentAnalysisScope? documentAnalysisScope,
            Project project,
            DiagnosticAnalyzerInfoCache analyzerInfoCache,
            CancellationToken cancellationToken)
        {
            var analyzers = documentAnalysisScope?.Analyzers ?? compilationWithAnalyzers.Analyzers;
            var suppressionAnalyzer = analyzers.OfType<IPragmaSuppressionsAnalyzer>().FirstOrDefault();
            if (suppressionAnalyzer == null)
            {
                return ImmutableArray<Diagnostic>.Empty;
            }

            if (documentAnalysisScope != null)
            {
                if (!(documentAnalysisScope.TextDocument is Document document))
                {
                    return ImmutableArray<Diagnostic>.Empty;
                }

                using var _ = ArrayBuilder<Diagnostic>.GetInstance(out var diagnosticsBuilder);
                await AnalyzeDocumentAsync(suppressionAnalyzer, document, documentAnalysisScope.Span, diagnosticsBuilder.Add).ConfigureAwait(false);
                return diagnosticsBuilder.ToImmutable();
            }
            else
            {
                if (compilationWithAnalyzers.AnalysisOptions.ConcurrentAnalysis)
                {
                    var bag = new ConcurrentBag<Diagnostic>();
                    using var _ = ArrayBuilder<Task>.GetInstance(project.DocumentIds.Count, out var tasks);
                    foreach (var document in project.Documents)
                    {
                        tasks.Add(AnalyzeDocumentAsync(suppressionAnalyzer, document, span: null, bag.Add));
                    }

                    await Task.WhenAll(tasks).ConfigureAwait(false);
                    return bag.ToImmutableArray();
                }
                else
                {
                    using var _ = ArrayBuilder<Diagnostic>.GetInstance(out var diagnosticsBuilder);
                    foreach (var document in project.Documents)
                    {
                        await AnalyzeDocumentAsync(suppressionAnalyzer, document, span: null, diagnosticsBuilder.Add).ConfigureAwait(false);
                    }

                    return diagnosticsBuilder.ToImmutable();
                }
            }

            async Task AnalyzeDocumentAsync(IPragmaSuppressionsAnalyzer suppressionAnalyzer, Document document, TextSpan? span, Action<Diagnostic> reportDiagnostic)
            {
                var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                await suppressionAnalyzer.AnalyzeAsync(semanticModel, span, compilationWithAnalyzers,
                    analyzerInfoCache.GetDiagnosticDescriptors, reportDiagnostic, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
