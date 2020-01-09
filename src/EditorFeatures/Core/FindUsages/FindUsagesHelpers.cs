// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SymbolMapping;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.FindUsages
{
    internal static class FindUsagesHelpers
    {
        public static string GetDisplayName(ISymbol symbol)
            => symbol.IsConstructor() ? symbol.ContainingType.Name : symbol.Name;

        /// <summary>
        /// Common helper for both the synchronous and streaming versions of FAR. 
        /// It returns the symbol we want to search for and the solution we should
        /// be searching.
        /// 
        /// Note that the <see cref="Solution"/> returned may absolutely *not* be
        /// the same as <c>document.Project.Solution</c>.  This is because 
        /// there may be symbol mapping involved (for example in Metadata-As-Source
        /// scenarios).
        /// </summary>
        public static async Task<(ISymbol symbol, Project project)?> GetRelevantSymbolAndProjectAtPositionAsync(
            Document document, int position, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var symbol = await SymbolFinder.FindSymbolAtPositionAsync(document, position, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (symbol == null)
            {
                return null;
            }

            // If this document is not in the primary workspace, we may want to search for results
            // in a solution different from the one we started in. Use the starting workspace's
            // ISymbolMappingService to get a context for searching in the proper solution.
            var mappingService = document.Project.Solution.Workspace.Services.GetService<ISymbolMappingService>();

            var mapping = await mappingService.MapSymbolAsync(document, symbol, cancellationToken).ConfigureAwait(false);
            if (mapping == null)
            {
                return null;
            }

            return (mapping.Symbol, mapping.Project);
        }

        public static async Task<(ISymbol symbol, Project project, ImmutableArray<ISymbol> implementations, string message)?> FindImplementationsAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var symbolAndProject = await GetRelevantSymbolAndProjectAtPositionAsync(
                document, position, cancellationToken).ConfigureAwait(false);
            if (symbolAndProject == null)
            {
                return null;
            }

            return await FindImplementationsAsync(
                symbolAndProject?.symbol, symbolAndProject?.project, cancellationToken).ConfigureAwait(false);
        }

        private static async Task<(ISymbol symbol, Project project, ImmutableArray<ISymbol> implementations, string message)?> FindImplementationsAsync(
            ISymbol symbol, Project project, CancellationToken cancellationToken)
        {
            var implementations = await FindImplementationsWorkerAsync(
                symbol, project, cancellationToken).ConfigureAwait(false);

            var filteredSymbols = implementations.WhereAsArray(
                s => s.Locations.Any(l => l.IsInSource));

            return filteredSymbols.Length == 0
                ? (symbol, project, filteredSymbols, EditorFeaturesResources.The_symbol_has_no_implementations)
                : (symbol, project, filteredSymbols, null);
        }

        private static async Task<ImmutableArray<ISymbol>> FindImplementationsWorkerAsync(
            ISymbol symbol, Project project, CancellationToken cancellationToken)
        {
            var solution = project.Solution;
            if (symbol.IsInterfaceType() || symbol.IsImplementableMember())
            {
                var implementations = await SymbolFinder.FindImplementationsAsync(
                    symbol, solution, cancellationToken: cancellationToken).ConfigureAwait(false);

                // It's important we use a HashSet here -- we may have cases in an inheritance hierarchy where more than one method
                // in an overrides chain implements the same interface method, and we want to duplicate those. The easiest way to do it
                // is to just use a HashSet.
                var implementationsAndOverrides = new HashSet<ISymbol>();

                foreach (var implementation in implementations)
                {
                    implementationsAndOverrides.Add(implementation);

                    // FindImplementationsAsync will only return the base virtual/abstract method, not that method and the overrides
                    // of the method. We should also include those.
                    if (implementation.IsOverridable())
                    {
                        var overrides = await SymbolFinder.FindOverridesAsync(
                            implementation, solution, cancellationToken: cancellationToken).ConfigureAwait(false);
                        implementationsAndOverrides.AddRange(overrides);
                    }
                }

                if (!symbol.IsInterfaceType() && !symbol.IsAbstract)
                {
                    implementationsAndOverrides.Add(symbol);
                }

                return implementationsAndOverrides.ToImmutableArray();
            }
            else if ((symbol as INamedTypeSymbol)?.TypeKind == TypeKind.Class)
            {
                var derivedClasses = await SymbolFinder.FindDerivedClassesAsync(
                    (INamedTypeSymbol)symbol, solution, cancellationToken: cancellationToken).ConfigureAwait(false);
                var implementations = derivedClasses.Concat(symbol);

                return implementations.ToImmutableArray();
            }
            else if (symbol.IsOverridable())
            {
                var overrides = await SymbolFinder.FindOverridesAsync(
                    symbol, solution, cancellationToken: cancellationToken).ConfigureAwait(false);
                var implementations = overrides.Concat(symbol);

                return implementations.ToImmutableArray();
            }
            else
            {
                // This is something boring like a regular method or type, so we'll just go there directly
                return ImmutableArray.Create(symbol);
            }
        }

        public static async Task<(Guid projectId, string projectName, SourceText sourceText)> GetGuidAndProjectNameAndSourceTextAsync(Document document, CancellationToken token)
        {
            // The FAR system needs to know the guid for the project that a def/reference is 
            // from (to support features like filtering).  Normally that would mean we could
            // only support this from a VisualStudioWorkspace.  However, we want till work 
            // in cases like Any-Code (which does not use a VSWorkspace).  So we are tolerant
            // when we have another type of workspace.  This means we will show results, but
            // certain features (like filtering) may not work in that context.
            var projectGuidService = document.Project.Solution.Workspace.Services.GetRequiredService<IWorkspaceProjectGuidService>();
            var projectGuid = projectGuidService.GetProjectGuid(document.Project);
            var projectName = document.Project.Name;
            var sourceText = await document.GetTextAsync(token).ConfigureAwait(false);
            return (projectGuid, projectName, sourceText);
        }

        public static async Task<(ExcerptResult, SourceText)> ExcerptAsync(SourceText sourceText, DocumentSpan documentSpan, CancellationToken token)
        {
            var excerptService = documentSpan.Document.Services.GetService<IDocumentExcerptService>();
            if (excerptService != null)
            {
                var result = await excerptService.TryExcerptAsync(documentSpan.Document, documentSpan.SourceSpan, ExcerptMode.SingleLine, token).ConfigureAwait(false);
                if (result != null)
                {
                    return (result.Value, GetLineContainingPosition(result.Value.Content, result.Value.MappedSpan.Start));
                }
            }

            var classificationResult = await ClassifiedSpansAndHighlightSpanFactory.ClassifyAsync(documentSpan, token).ConfigureAwait(false);

            // need to fix the span issue tracking here - https://github.com/dotnet/roslyn/issues/31001
            var excerptResult = new ExcerptResult(
                sourceText,
                classificationResult.HighlightSpan,
                classificationResult.ClassifiedSpans,
                documentSpan.Document,
                documentSpan.SourceSpan);

            return (excerptResult, GetLineContainingPosition(sourceText, documentSpan.SourceSpan.Start));
        }

        public static async Task<MappedSpanResult?> TryMapAndGetFirstAsync(DocumentSpan documentSpan, SourceText sourceText, CancellationToken cancellationToken)
        {
            var service = documentSpan.Document.Services.GetService<ISpanMappingService>();
            if (service == null)
            {
                return new MappedSpanResult(documentSpan.Document.FilePath, sourceText.Lines.GetLinePositionSpan(documentSpan.SourceSpan), documentSpan.SourceSpan);
            }

            var results = await service.MapSpansAsync(
                documentSpan.Document, SpecializedCollections.SingletonEnumerable(documentSpan.SourceSpan), cancellationToken).ConfigureAwait(false);

            if (results.IsDefaultOrEmpty)
            {
                return new MappedSpanResult(documentSpan.Document.FilePath, sourceText.Lines.GetLinePositionSpan(documentSpan.SourceSpan), documentSpan.SourceSpan);
            }

            // if span mapping service filtered out the span, make sure
            // to return null so that we remove the span from the result
            return results.FirstOrNull(r => !r.IsDefault);
        }

        public static SourceText GetLineContainingPosition(SourceText text, int position)
        {
            var line = text.Lines.GetLineFromPosition(position);

            return text.GetSubText(line.Span);
        }
    }
}
