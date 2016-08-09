// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeLens
{
    [ExportWorkspaceService(typeof(ICodeLensReferencesService)), Shared]
    internal sealed class CodeLensReferenceService : ICodeLensReferencesService
    {
        public async Task<ReferenceCount?> GetReferenceCountAsync(Solution solution, DocumentId documentId, SyntaxNode syntaxNode, int maxSearchResults, CancellationToken cancellationToken)
        {
            if (solution == null || documentId == null || syntaxNode == null)
            {
                return null;
            }

            var document = solution.GetDocument(documentId);
            if (document == null)
            {
                return null;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (semanticModel == null)
            {
                return null;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var symbol = semanticModel.GetDeclaredSymbol(syntaxNode, cancellationToken);
            if (symbol == null)
            {
                return null;
            }

            using (var progress = new CodeLensFindReferencesProgress(symbol, syntaxNode, maxSearchResults, cancellationToken))
            {
                try
                {
                    await SymbolFinder.FindReferencesAsync(symbol, solution, progress, null,
                        progress.CancellationToken).ConfigureAwait(false);

                    return new ReferenceCount(
                        progress.SearchCap > 0
                            ? Math.Min(progress.ReferencesCount, progress.SearchCap)
                            : progress.ReferencesCount, progress.SearchCapReached);
                }
                catch (OperationCanceledException)
                {
                    if (progress.SearchCapReached)
                    {
                        // search was cancelled, and it was cancelled by us because a cap was reached.
                        return new ReferenceCount(progress.SearchCap, true);
                    }

                    // search was cancelled, but not because of cap.
                    // this always throws.
                    cancellationToken.ThrowIfCancellationRequested();
                }

                throw ExceptionUtilities.Unreachable;
            }
        }

        private static async Task<ReferenceLocationDescriptor> GetDescriptorOfEnclosingSymbolAsync(Solution solution, Location location, CancellationToken cancellationToken)
        {
            var document = solution.GetDocument(location.SourceTree);

            if (document == null)
            {
                return null;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (semanticModel == null)
            {
                return null;
            }

            var langServices = document.GetLanguageService<ICodeLensDisplayInfoService>();
            if (langServices == null)
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, "Unsupported language '{0}'", semanticModel.Language), nameof(semanticModel));
            }

            var position = location.SourceSpan.Start;
            var token = (await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false)).FindToken(position, true);
            var node = GetEnclosingCodeElementNode(document, token, langServices);
            var longName = langServices.GetDisplayName(semanticModel, node, false);

            // get the full line of source text on the line that contains this position
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            // get the actual span of text for the line containing reference
            var textLine = text.Lines.GetLineFromPosition(position);
            
            // turn the span from document relative to line relative
            var spanStart = token.Span.Start - textLine.Span.Start;
            var line = textLine.ToString();

            var beforeLine1 = textLine.LineNumber > 0 ? text.Lines[textLine.LineNumber - 1].ToString() : string.Empty;
            var beforeLine2 = textLine.LineNumber - 1 > 0
                ? text.Lines[textLine.LineNumber - 2].ToString()
                : string.Empty;
            var afterLine1 = textLine.LineNumber < text.Lines.Count - 1
                ? text.Lines[textLine.LineNumber + 1].ToString()
                : string.Empty;
            var afterLine2 = textLine.LineNumber + 1 < text.Lines.Count - 1
                ? text.Lines[textLine.LineNumber + 2].ToString()
                : string.Empty;
            var referenceSpan = new TextSpan(spanStart, token.Span.Length);

            var symbol = semanticModel.GetDeclaredSymbol(node);
            var glyph = symbol?.GetGlyph();

            return new ReferenceLocationDescriptor(longName,
                                   semanticModel.Language,
                                   glyph,
                                   location,
                                   solution.GetDocument(location.SourceTree)?.Id,
                                   line.TrimEnd(),
                                   referenceSpan.Start,
                                   referenceSpan.Length,
                                   beforeLine1.TrimEnd(),
                                   beforeLine2.TrimEnd(),
                                   afterLine1.TrimEnd(),
                                   afterLine2.TrimEnd());
        }

        private static SyntaxNode GetEnclosingCodeElementNode(Document document, SyntaxToken token, ICodeLensDisplayInfoService langServices)
        {
            var syntaxFactsService = document.GetLanguageService<ISyntaxFactsService>();

            var node = token.Parent;
            while (node != null)
            {
                if (syntaxFactsService.IsDocumentationComment(node))
                {
                    var structuredTriviaSyntax = (IStructuredTriviaSyntax)node;
                    var parentTrivia = structuredTriviaSyntax.ParentTrivia;
                    node = parentTrivia.Token.Parent;
                }
                else if (syntaxFactsService.IsDeclaration(node) ||
                         syntaxFactsService.IsDirectiveOrImport(node) ||
                         syntaxFactsService.IsGlobalAttribute(node))
                {
                    break;
                }
                else
                {
                    node = node.Parent;
                }
            }

            if (node == null)
            {
                node = token.Parent;
            }

            return langServices.GetDisplayNode(node);
        }

        public async Task<IEnumerable<ReferenceLocationDescriptor>> FindReferenceLocationsAsync(Solution solution, DocumentId documentId, SyntaxNode syntaxNode, CancellationToken cancellationToken)
        {
            if (solution == null || documentId == null || syntaxNode == null)
            {
                return null;
            }

            var document = solution.GetDocument(documentId);
            if (document == null)
            {
                return null;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (semanticModel == null)
            {
                return null;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var symbol = semanticModel.GetDeclaredSymbol(syntaxNode, cancellationToken);
            if (symbol == null)
            {
                return null;
            }

            using (var progress = new CodeLensFindReferencesProgress(symbol, syntaxNode, 0, cancellationToken))
            {
                await SymbolFinder.FindReferencesAsync(symbol, solution, progress, null,
                    progress.CancellationToken).ConfigureAwait(false);

                var referenceTasks = progress.Locations
                    .Where(location => location.Kind != LocationKind.MetadataFile && location.Kind != LocationKind.None)
                    .Distinct(LocationComparer.Instance)
                    .Select(
                        location =>
                            GetDescriptorOfEnclosingSymbolAsync(solution, location, cancellationToken))
                    .ToArray();

                Task.WaitAll(referenceTasks, cancellationToken);

                return referenceTasks.Select(task => task.Result);
            }
        }
    }
}
