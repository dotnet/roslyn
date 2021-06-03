// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.NavigationBar;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Text.Adornments;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [ExportLspRequestHandlerProvider, Shared]
    [ProvidesMethod(Methods.TextDocumentDocumentSymbolName)]
    internal class DocumentSymbolsHandler : AbstractStatelessRequestHandler<DocumentSymbolParams, object[]>
    {
        public override string Method => Methods.TextDocumentDocumentSymbolName;

        public override bool MutatesSolutionState => false;
        public override bool RequiresLSPSolution => true;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DocumentSymbolsHandler()
        {
        }

        public override TextDocumentIdentifier GetTextDocumentIdentifier(DocumentSymbolParams request) => request.TextDocument;

        public override async Task<object[]> HandleRequestAsync(DocumentSymbolParams request, RequestContext context, CancellationToken cancellationToken)
        {
            var document = context.Document;
            if (document == null)
            {
                return Array.Empty<SymbolInformation>();
            }

            var navBarService = document.Project.LanguageServices.GetRequiredService<INavigationBarItemService>();
            var navBarItems = await navBarService.GetItemsAsync(document, supportsCodeGeneration: false, cancellationToken).ConfigureAwait(false);
            if (navBarItems.IsEmpty)
            {
                return Array.Empty<object>();
            }

            var compilation = await document.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
            var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            // TODO - Return more than 2 levels of symbols.
            // https://github.com/dotnet/roslyn/projects/45#card-20033869
            using var _ = ArrayBuilder<object>.GetInstance(out var symbols);
            if (context.ClientCapabilities?.TextDocument?.DocumentSymbol?.HierarchicalDocumentSymbolSupport == true)
            {
                foreach (var item in navBarItems)
                {
                    // only top level ones
                    symbols.AddIfNotNull(GetDocumentSymbol(item, tree, text, cancellationToken));
                }
            }
            else
            {
                foreach (var item in navBarItems)
                {
                    symbols.AddIfNotNull(GetSymbolInformation(item, compilation, tree, document, text, cancellationToken, containerName: null));

                    foreach (var childItem in item.ChildItems)
                    {
                        symbols.AddIfNotNull(GetSymbolInformation(childItem, compilation, tree, document, text, cancellationToken, item.Text));
                    }
                }
            }

            var result = symbols.ToArray();
            return result;
        }

        /// <summary>
        /// Get a symbol information from a specified nav bar item.
        /// </summary>
        private static SymbolInformation? GetSymbolInformation(RoslynNavigationBarItem item, Compilation compilation, SyntaxTree tree, Document document,
            SourceText text, CancellationToken cancellationToken, string? containerName = null)
        {
            if (item is not RoslynNavigationBarItem.SymbolItem symbolItem)
                return null;

            var location = GetLocation(symbolItem, compilation, tree, cancellationToken);

            return location == null
                ? Create(item, symbolItem.Spans.First(), containerName, document, text)
                : Create(item, location.SourceSpan, containerName, document, text);

            static VSSymbolInformation Create(RoslynNavigationBarItem item, TextSpan span, string? containerName, Document document, SourceText text)
            {
                return new VSSymbolInformation
                {
                    Name = item.Text,
                    Location = new LSP.Location
                    {
                        Uri = document.GetURI(),
                        Range = ProtocolConversions.TextSpanToRange(span, text),
                    },
                    Kind = ProtocolConversions.GlyphToSymbolKind(item.Glyph),
                    ContainerName = containerName,
                    Icon = new ImageElement(item.Glyph.GetImageId()),
                };
            }
        }

        /// <summary>
        /// Get a document symbol from a specified nav bar item.
        /// </summary>
        private static DocumentSymbol? GetDocumentSymbol(
            RoslynNavigationBarItem item, SyntaxTree tree, SourceText text, CancellationToken cancellationToken)
        {
            if (item is not RoslynNavigationBarItem.SymbolItem symbolItem || symbolItem.Spans.Length == 0 || symbolItem.SelectionSpan == null)
                return null;

            return new DocumentSymbol
            {
                Name = symbolItem.Name,
                Detail = item.Text,
                Kind = ProtocolConversions.GlyphToSymbolKind(item.Glyph),
                Deprecated = symbolItem.IsObsolete,
                Range = ProtocolConversions.TextSpanToRange(symbolItem.Spans.First(), text),
                SelectionRange = ProtocolConversions.TextSpanToRange(symbolItem.SelectionSpan.Value, text),
                Children = GetChildren(item.ChildItems, tree, text, cancellationToken),
            };

            static DocumentSymbol[] GetChildren(
                ImmutableArray<RoslynNavigationBarItem> items, SyntaxTree tree, SourceText text, CancellationToken cancellationToken)
            {
                using var _ = ArrayBuilder<DocumentSymbol>.GetInstance(out var list);
                foreach (var item in items)
                    list.AddIfNotNull(GetDocumentSymbol(item, tree, text, cancellationToken));

                return list.ToArray();
            }
        }

        /// <summary>
        /// Get a location for a particular nav bar item.
        /// </summary>
        private static Location? GetLocation(RoslynNavigationBarItem.SymbolItem symbolItem, Compilation compilation, SyntaxTree tree, CancellationToken cancellationToken)
        {
            var symbols = symbolItem.NavigationSymbolId.Resolve(compilation, cancellationToken: cancellationToken);
            var symbol = symbols.Symbol;

            if (symbol == null)
            {
                if (symbolItem.NavigationSymbolIndex < symbols.CandidateSymbols.Length)
                {
                    symbol = symbols.CandidateSymbols[symbolItem.NavigationSymbolIndex];
                }
                else
                {
                    return null;
                }
            }

            var location = symbol.Locations.FirstOrDefault(l => l.SourceTree?.Equals(tree) == true);
            return location ?? symbol.Locations.FirstOrDefault();
        }
    }
}
