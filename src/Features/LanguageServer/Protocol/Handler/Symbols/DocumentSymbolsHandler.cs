﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
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

            var symbols = ArrayBuilder<object>.GetInstance();

            var navBarService = document.Project.LanguageServices.GetRequiredService<Editor.INavigationBarItemService>();
            var navBarItems = await navBarService.GetItemsAsync(document, cancellationToken).ConfigureAwait(false);
            if (navBarItems.Count == 0)
            {
                return symbols.ToArrayAndFree();
            }

            var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            // TODO - Return more than 2 levels of symbols.
            // https://github.com/dotnet/roslyn/projects/45#card-20033869
            if (context.ClientCapabilities?.TextDocument?.DocumentSymbol?.HierarchicalDocumentSymbolSupport == true)
            {
                foreach (var item in navBarItems)
                {
                    // only top level ones
                    symbols.Add(await GetDocumentSymbolAsync(item, compilation, tree, text, cancellationToken).ConfigureAwait(false));
                }
            }
            else
            {
                foreach (var item in navBarItems)
                {
                    symbols.Add(GetSymbolInformation(item, compilation, tree, document, text, cancellationToken, containerName: null));

                    foreach (var childItem in item.ChildItems)
                    {
                        symbols.Add(GetSymbolInformation(childItem, compilation, tree, document, text, cancellationToken, item.Text));
                    }
                }
            }

            var result = symbols.WhereNotNull().ToArray();
            symbols.Free();
            return result;
        }

        /// <summary>
        /// Get a symbol information from a specified nav bar item.
        /// </summary>
        private static SymbolInformation GetSymbolInformation(NavigationBarItem item, Compilation compilation, SyntaxTree tree, Document document,
            SourceText text, CancellationToken cancellationToken, string containerName = null)
        {
            if (item.Spans.Count == 0)
            {
                return null;
            }

            var location = GetLocation(item, compilation, tree, cancellationToken);

            if (location == null)
            {
                return Create(item, item.Spans.First(), containerName, document, text);
            }

            return Create(item, location.SourceSpan, containerName, document, text);

            static VSSymbolInformation Create(NavigationBarItem item, TextSpan span, string containerName, Document document, SourceText text)
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
        private static async Task<DocumentSymbol> GetDocumentSymbolAsync(NavigationBarItem item, Compilation compilation, SyntaxTree tree,
            SourceText text, CancellationToken cancellationToken)
        {
            // it is actually symbol location getter. but anyway.
            var location = GetLocation(item, compilation, tree, cancellationToken);
            if (location == null)
            {
                return null;
            }

            var symbol = await GetSymbolAsync(location, compilation, cancellationToken).ConfigureAwait(false);
            if (symbol == null)
            {
                return null;
            }

            return new DocumentSymbol
            {
                Name = symbol.Name,
                Detail = item.Text,
                Kind = ProtocolConversions.GlyphToSymbolKind(item.Glyph),
                Deprecated = symbol.IsObsolete(),
                Range = ProtocolConversions.TextSpanToRange(item.Spans.First(), text),
                SelectionRange = ProtocolConversions.TextSpanToRange(location.SourceSpan, text),
                Children = await GetChildrenAsync(item.ChildItems, compilation, tree, text, cancellationToken).ConfigureAwait(false),
            };

            static async Task<DocumentSymbol[]> GetChildrenAsync(IEnumerable<NavigationBarItem> items, Compilation compilation, SyntaxTree tree,
                SourceText text, CancellationToken cancellationToken)
            {
                var list = new ArrayBuilder<DocumentSymbol>();
                foreach (var item in items)
                {
                    list.Add(await GetDocumentSymbolAsync(item, compilation, tree, text, cancellationToken).ConfigureAwait(false));
                }

                return list.ToArrayAndFree();
            }

            static async Task<ISymbol> GetSymbolAsync(Location location, Compilation compilation, CancellationToken cancellationToken)
            {
                var model = compilation.GetSemanticModel(location.SourceTree);
                var root = await model.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
                var node = root.FindNode(location.SourceSpan);

                while (node != null)
                {
                    var symbol = model.GetDeclaredSymbol(node, cancellationToken);
                    if (symbol != null)
                    {
                        return symbol;
                    }

                    node = node.Parent;
                }

                return null;
            }
        }

        /// <summary>
        /// Get a location for a particular nav bar item.
        /// </summary>
        private static Location GetLocation(NavigationBarItem item, Compilation compilation, SyntaxTree tree, CancellationToken cancellationToken)
        {
            if (item is not WrappedNavigationBarItem { UnderlyingItem: RoslynNavigationBarItem.SymbolItem symbolItem })
                return null;

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

            var location = symbol.Locations.FirstOrDefault(l => l.SourceTree.Equals(tree));
            return location ?? symbol.Locations.FirstOrDefault();
        }
    }
}
