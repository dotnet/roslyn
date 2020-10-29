// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.DocumentSymbols;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [Shared]
    [ExportLspMethod(Methods.TextDocumentDocumentSymbolName, mutatesSolutionState: false)]
    internal class DocumentSymbolsHandler : IRequestHandler<DocumentSymbolParams, SumType<DocumentSymbol, SymbolInformation>[]>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DocumentSymbolsHandler()
        {
        }

        public TextDocumentIdentifier GetTextDocumentIdentifier(DocumentSymbolParams request) => request.TextDocument;

        public async Task<SumType<DocumentSymbol, SymbolInformation>[]> HandleRequestAsync(DocumentSymbolParams request, RequestContext context, CancellationToken cancellationToken)
        {
            var document = context.Document;
            if (document == null)
            {
                return Array.Empty<SumType<DocumentSymbol, SymbolInformation>>();
            }

            var symbols = ArrayBuilder<SumType<DocumentSymbol?, SymbolInformation?>>.GetInstance();

            var hierarchicalDocumentSymbolSupport = context.ClientCapabilities?.TextDocument?.DocumentSymbol?.HierarchicalDocumentSymbolSupport == true;
            var documentSymbolsService = document.Project.LanguageServices.GetRequiredService<IDocumentSymbolsService>();
            var documentSymbols = await documentSymbolsService.GetSymbolsInDocumentAsync(
                document,
                hierarchicalDocumentSymbolSupport ? DocumentSymbolsOptions.FullHierarchy : DocumentSymbolsOptions.TypesAndMembersOnly,
                cancellationToken).ConfigureAwait(false);
            if (documentSymbols.IsEmpty)
            {
                return symbols.ToArrayAndFree()!;
            }

            var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            if (compilation is null || tree is null || text is null)
            {
                return symbols.ToArrayAndFree()!;
            }

            if (hierarchicalDocumentSymbolSupport)
            {
                foreach (var item in documentSymbols)
                {
                    // only top level ones
                    symbols.Add(GetDocumentSymbol(item, tree, text));
                }
            }
            else
            {
                foreach (var item in documentSymbols)
                {
                    symbols.Add(GetSymbolInformation(item, document, text, containerName: null));

                    foreach (var childItem in item.ChildrenSymbols)
                    {
                        symbols.Add(GetSymbolInformation(childItem, document, text, item.Text));
                    }
                }
            }

            var result = symbols.WhereSumElementsNotNull().ToArray();
            symbols.Free();
            return result;
        }

        /// <summary>
        /// Get a symbol information from a specified nav bar item.
        /// </summary>
        private static SymbolInformation? GetSymbolInformation(DocumentSymbolInfo item, Document document, SourceText text,
            string? containerName = null)
        {
            if (item.DeclaringSpans.IsEmpty)
            {
                return null;
            }

            return new SymbolInformation
            {
                Name = item.Text,
                Location = new LSP.Location
                {
                    Uri = document.GetURI(),
                    Range = ProtocolConversions.TextSpanToRange(item.DeclaringSpans[0], text),
                },
                Kind = ProtocolConversions.GlyphToSymbolKind(item.Glyph, item.Tags),
                ContainerName = containerName,
            };
        }

        /// <summary>
        /// Get a document symbol from a specified nav bar item.
        /// </summary>
        private static DocumentSymbol? GetDocumentSymbol(DocumentSymbolInfo item, SyntaxTree tree, SourceText text)
        {
            if (item.DeclaringSpans.IsEmpty || item.EnclosingSpans.IsEmpty)
            {
                return null;
            }

            return new DocumentSymbol
            {
                Name = item.Name,
                Detail = item.Text,
                Kind = ProtocolConversions.GlyphToSymbolKind(item.Glyph, item.Tags),
                Deprecated = item.Obsolete,
                Range = ProtocolConversions.TextSpanToRange(item.EnclosingSpans[0], text),
                SelectionRange = ProtocolConversions.TextSpanToRange(item.DeclaringSpans[0], text),
                Children = GetChildren(item.ChildrenSymbols, tree, text)
            };

            static DocumentSymbol[] GetChildren(ImmutableArray<DocumentSymbolInfo> items, SyntaxTree tree, SourceText text)
            {
                var list = new ArrayBuilder<DocumentSymbol?>();
                foreach (var item in items)
                {
                    list.Add(GetDocumentSymbol(item, tree, text));
                }

                var ret = list.WhereNotNull().ToArray();
                list.Free();
                return ret;
            }
        }
    }
}
