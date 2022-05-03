// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.NavigationBar;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    /// <summary>
    /// TODO - This must be moved to the MS.CA.LanguageServer.Protocol project once
    /// we no longer reference VS icon types.
    /// </summary>
    [ExportRoslynLanguagesLspRequestHandlerProvider(typeof(DocumentSymbolsHandler)), Shared]
    [Method(Methods.TextDocumentDocumentSymbolName)]
    internal class DocumentSymbolsHandler : AbstractStatelessRequestHandler<DocumentSymbolParams, object[]>
    {
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
            Contract.ThrowIfNull(document);

            var navBarService = document.Project.LanguageServices.GetRequiredService<INavigationBarItemService>();
            var navBarItems = await navBarService.GetItemsAsync(document, supportsCodeGeneration: false, forceFrozenPartialSemanticsForCrossProcessOperations: false, cancellationToken).ConfigureAwait(false);
            if (navBarItems.IsEmpty)
                return Array.Empty<object>();

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            // TODO - Return more than 2 levels of symbols.
            // https://github.com/dotnet/roslyn/projects/45#card-20033869
            using var _ = ArrayBuilder<object>.GetInstance(out var symbols);
            if (context.ClientCapabilities?.TextDocument?.DocumentSymbol?.HierarchicalDocumentSymbolSupport == true)
            {
                // only top level ones
                foreach (var item in navBarItems)
                    symbols.AddIfNotNull(GetDocumentSymbol(item, text, cancellationToken));
            }
            else
            {
                foreach (var item in navBarItems)
                {
                    symbols.AddIfNotNull(GetSymbolInformation(item, document, text, containerName: null));

                    foreach (var childItem in item.ChildItems)
                        symbols.AddIfNotNull(GetSymbolInformation(childItem, document, text, item.Text));
                }
            }

            var result = symbols.ToArray();
            return result;
        }

        /// <summary>
        /// Get a symbol information from a specified nav bar item.
        /// </summary>
        private static SymbolInformation? GetSymbolInformation(
            RoslynNavigationBarItem item, Document document, SourceText text, string? containerName = null)
        {
            if (item is not RoslynNavigationBarItem.SymbolItem symbolItem || symbolItem.Location.InDocumentInfo == null)
                return null;

            return new VSSymbolInformation
            {
                Name = item.Text,
                Location = new LSP.Location
                {
                    Uri = document.GetURI(),
                    Range = ProtocolConversions.TextSpanToRange(symbolItem.Location.InDocumentInfo.Value.navigationSpan, text),
                },
                Kind = ProtocolConversions.GlyphToSymbolKind(item.Glyph),
                ContainerName = containerName,
                Icon = VSLspExtensionConversions.GetImageIdFromGlyph(item.Glyph),
            };
        }

        /// <summary>
        /// Get a document symbol from a specified nav bar item.
        /// </summary>
        private static DocumentSymbol? GetDocumentSymbol(
            RoslynNavigationBarItem item, SourceText text, CancellationToken cancellationToken)
        {
            if (item is not RoslynNavigationBarItem.SymbolItem symbolItem ||
                symbolItem.Location.InDocumentInfo == null)
            {
                return null;
            }

            var inDocumentInfo = symbolItem.Location.InDocumentInfo.Value;
            if (inDocumentInfo.spans.Length == 0)
                return null;

            return new DocumentSymbol
            {
                Name = symbolItem.Name,
                Detail = item.Text,
                Kind = ProtocolConversions.GlyphToSymbolKind(item.Glyph),
                Deprecated = symbolItem.IsObsolete,
                Range = ProtocolConversions.TextSpanToRange(inDocumentInfo.spans.First(), text),
                SelectionRange = ProtocolConversions.TextSpanToRange(inDocumentInfo.navigationSpan, text),
                Children = GetChildren(item.ChildItems, text, cancellationToken),
            };

            static DocumentSymbol[] GetChildren(
                ImmutableArray<RoslynNavigationBarItem> items, SourceText text, CancellationToken cancellationToken)
            {
                using var _ = ArrayBuilder<DocumentSymbol>.GetInstance(out var list);
                foreach (var item in items)
                    list.AddIfNotNull(GetDocumentSymbol(item, text, cancellationToken));

                return list.ToArray();
            }
        }
    }
}
