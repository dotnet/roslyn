// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Design.Serialization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.Decompiler.IL;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp.Rename;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Rename.ConflictEngine;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Xunit;
using RenameAnnotation = Microsoft.CodeAnalysis.Rename.ConflictEngine.RenameAnnotation;

namespace Microsoft.CodeAnalysis.Test.Utilities.Rename
{
    [UseExportProvider]
    [Trait(Traits.Feature, Traits.Features.Rename)]
    public class RenameRewriterTests
    {
        private const string ConflictTag = "Conflict";
        private const string RenameTag = "Rename";

        protected sealed class Verifier : IDisposable
        {
            private readonly TestWorkspace _testWorkspace;
            private readonly RenamedSpansTracker _renamedSpansTracker = new();
            private readonly Dictionary<DocumentId, HashSet<RenameSymbolContext>> _documentToRenameSymbolContextsMap = new();
            private readonly AnnotationTable<RenameAnnotation> _annotationTable = new(RenameAnnotation.Kind);
            private Solution _currentSolution;

            public Verifier(string workspaceXml)
            {
                _testWorkspace = TestWorkspace.Create(workspaceXml);
                _currentSolution = _testWorkspace.CurrentSolution;
            }

            public async Task RenameAndAnnotatedDocumentAsync(
                string documentFilePath,
                Dictionary<string, (string replacementText, SymbolRenameOptions renameOptions)> renameTagsToReplacementInfo,
                CancellationToken cancellationToken)
            {
                var testHostDocument = _testWorkspace.Documents.Single(doc => doc.FilePath == documentFilePath);
                var newRoot = await RenameDocumentAsync(_currentSolution, testHostDocument, renameTagsToReplacementInfo, cancellationToken).ConfigureAwait(false);
                if (newRoot == null)
                {
                    return;
                }

                var documentId = testHostDocument.Id;
                _currentSolution = _currentSolution.WithDocumentSyntaxRoot(documentId, newRoot);
            }

            public async Task VerifyAsync(
                string documentFilePath,
                string tagName,
                string replacementText,
                CancellationToken cancellationToken)
            {
                var testHostDocument = _testWorkspace.Documents.Single(doc => doc.FilePath == documentFilePath);
                var newDocument = _currentSolution.GetRequiredDocument(testHostDocument.Id);
                var sourceText = await newDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);

                foreach (var (tag, spans) in testHostDocument.AnnotatedSpans)
                {
                    if (tag == tagName)
                    {
                        foreach (var oldSpan in spans)
                        {
                            var newStartPosition = _renamedSpansTracker.GetAdjustedPosition(oldSpan.Start, testHostDocument.Id);
                            var newSpan = new TextSpan(newStartPosition, replacementText.Length);
                            var contentAtNewSpan = sourceText.ToString(newSpan);
                            Assert.Equal(replacementText, contentAtNewSpan);
                        }
                    }
                }
            }

            public async Task SimplifyAsync(
                string documentFilePath,
                CancellationToken cancellationToken)
            {
                var documentId = _testWorkspace.Documents.Single(doc => doc.FilePath == documentFilePath).Id;
                var replacementTextValid = _documentToRenameSymbolContextsMap[documentId].All(context => context.ReplacementTextValid);
                _currentSolution = await _renamedSpansTracker.SimplifyAsync(
                    _currentSolution,
                    SpecializedCollections.SingletonEnumerable(documentId),
                    replacementTextValid,
                    _annotationTable,
                    CodeActionOptions.DefaultProvider,
                    cancellationToken).ConfigureAwait(false);
            }

            public void VerifyDocument(
                string documentFilePath,
                string expectedDocumentContent)
            {
                var actualDocumentContext = _testWorkspace.Documents.Single(doc => doc.FilePath == documentFilePath).GetTextView().TextBuffer.ToString();
                Assert.Equal(expectedDocumentContent, actualDocumentContext);
            }

            private async Task<SyntaxNode?> RenameDocumentAsync(
                Solution solution,
                TestHostDocument testHostDocument,
                Dictionary<string, (string replacementText, SymbolRenameOptions renameOptions)> renameTagsToReplacementInfo,
                CancellationToken cancellationToken)
            {
                var document = solution.GetRequiredDocument(testHostDocument.Id);
                var renameRewriterService = document.GetRequiredLanguageService<IRenameRewriterLanguageService>();
                var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
                var annotatedSpans = testHostDocument.AnnotatedSpans;

                if (!_documentToRenameSymbolContextsMap.ContainsKey(document.Id))
                {
                    _documentToRenameSymbolContextsMap[document.Id] = new HashSet<RenameSymbolContext>();
                }

                using var _1 = PooledHashSet<TextSpan>.GetInstance(out var conflictLocationSetBuilder);
                using var _2 = ArrayBuilder<RenameSymbolContext>.GetInstance(out var symbolContextsBuilder);
                using var _3 = ArrayBuilder<TextSpanRenameContext>.GetInstance(out var tokenTextSpanRenameContextsBuilder);
                using var _4 = ArrayBuilder<TextSpanRenameContext>.GetInstance(out var stringAndCommentsContextsBuilder);

                foreach (var (tag, spans) in annotatedSpans)
                {
                    if (tag == ConflictTag)
                    {
                        conflictLocationSetBuilder.AddRange(spans);
                    }
                    else if (tag.StartsWith(RenameTag))
                    {
                        foreach (var span in spans)
                        {
                            var renameSymbol = await RenameLocations.ReferenceProcessing.TryGetRenamableSymbolAsync(document, span.Start, cancellationToken).ConfigureAwait(false);
                            if (renameSymbol == null)
                            {
                                Assert.False(true, $"Can't find symbol at tagged place, tag: {tag}.");
                                return null;
                            }

                            if (!renameTagsToReplacementInfo.TryGetValue(tag, out var replacementInfo))
                            {
                                Assert.False(true, $"Can't find the replacementInfo for tag: {tag}.");
                                return null;
                            }

                            var (replacementText, options) = replacementInfo;
                            var possibleNameConflicts = new List<string>();

                            renameRewriterService.TryAddPossibleNameConflicts(renameSymbol, replacementText, possibleNameConflicts);
                            var symbolContext = new RenameSymbolContext(
                                    new RenameAnnotation(),
                                    renameSymbol.Locations.First(l => l.IsInSource),
                                    replacementText,
                                    renameSymbol.Name,
                                    possibleNameConflicts,
                                    renameSymbol,
                                    renameSymbol as IAliasSymbol,
                                    renameRewriterService.IsIdentifierValid(replacementText, syntaxFacts),
                                    options.RenameInStrings,
                                    options.RenameInComments);

                            _documentToRenameSymbolContextsMap[document.Id].Add(symbolContext);
                            symbolContextsBuilder.Add(symbolContext);

                            var renameLocationsSet = await Renamer.FindRenameLocationsAsync(
                                solution,
                                renameSymbol,
                                options,
                                CodeActionOptions.DefaultProvider,
                                cancellationToken).ConfigureAwait(false);

                            var locationsInDocument = renameLocationsSet.Locations.Where(location => location.DocumentId == document.Id).ToImmutableArray();
                            var textSpanRenameContexts = locationsInDocument
                                .WhereAsArray(location => RenameUtilities.ShouldIncludeLocation(renameLocationsSet.Locations, location))
                                .SelectAsArray(location => new TextSpanRenameContext(location, symbolContext));
                            tokenTextSpanRenameContextsBuilder.AddRange(textSpanRenameContexts);

                            var stringAndCommentsRenameContexts = locationsInDocument
                                .WhereAsArray(location => RenameUtilities.ShouldIncludeLocation(renameLocationsSet.Locations, location))
                                .SelectAsArray(location => new TextSpanRenameContext(location, symbolContext));
                            stringAndCommentsContextsBuilder.AddRange(stringAndCommentsRenameContexts);
                        }
                    }
                }

                var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var parameters = new RenameRewriterParameters(
                    conflictLocationSetBuilder.ToImmutableHashSet(),
                    solution,
                    syntaxTree,
                    _renamedSpansTracker,
                    syntaxTree.GetRoot(cancellationToken),
                    document,
                    semanticModel,
                    new AnnotationTable<RenameAnnotation>(RenameAnnotation.Kind),
                    tokenTextSpanRenameContextsBuilder.ToImmutable(),
                    stringAndCommentsContextsBuilder.ToImmutable(),
                    symbolContextsBuilder.ToImmutable(),
                    cancellationToken);

                return renameRewriterService.AnnotateAndRename(parameters);
            }

            public void Dispose()
                => _testWorkspace.Dispose();
        }
    }
}
