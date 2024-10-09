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
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    /// <summary>
    /// TODO - This must be moved to the MS.CA.LanguageServer.Protocol project once
    /// we no longer reference VS icon types.
    /// </summary>
    [ExportCSharpVisualBasicStatelessLspService(typeof(DocumentSymbolsHandler)), Shared]
    [Method(Methods.TextDocumentDocumentSymbolName)]
    internal sealed class DocumentSymbolsHandler : ILspServiceDocumentRequestHandler<RoslynDocumentSymbolParams, SumType<DocumentSymbol[], SymbolInformation[]>>
    {
        public bool MutatesSolutionState => false;
        public bool RequiresLSPSolution => true;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DocumentSymbolsHandler()
        {
        }

        public TextDocumentIdentifier GetTextDocumentIdentifier(RoslynDocumentSymbolParams request) => request.TextDocument;

        public Task<SumType<DocumentSymbol[], SymbolInformation[]>> HandleRequestAsync(RoslynDocumentSymbolParams request, RequestContext context, CancellationToken cancellationToken)
        {
            var document = context.GetRequiredDocument();
            var clientCapabilities = context.GetRequiredClientCapabilities();
            var useHierarchicalSymbols = clientCapabilities.TextDocument?.DocumentSymbol?.HierarchicalDocumentSymbolSupport == true || request.UseHierarchicalSymbols;
            var service = document.Project.Solution.Services.GetRequiredService<ILspSymbolInformationCreationService>();

            return GetDocumentSymbolsAsync(document, useHierarchicalSymbols, service, cancellationToken);
        }

        internal static async Task<SumType<DocumentSymbol[], SymbolInformation[]>> GetDocumentSymbolsAsync(Document document, bool useHierarchicalSymbols, ILspSymbolInformationCreationService symbolInformationCreationService, CancellationToken cancellationToken)
        {
            var navBarService = document.Project.Services.GetRequiredService<INavigationBarItemService>();
            var navBarItems = await navBarService.GetItemsAsync(document, supportsCodeGeneration: false, frozenPartialSemantics: false, cancellationToken).ConfigureAwait(false);
            if (navBarItems.IsEmpty)
                return Array.Empty<DocumentSymbol>();

            var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
            if (useHierarchicalSymbols)
            {
                using var _ = ArrayBuilder<DocumentSymbol>.GetInstance(out var symbols);
                // only top level ones
                foreach (var item in navBarItems)
                    symbols.AddIfNotNull(GetDocumentSymbol(item, text, cancellationToken));

                return symbols.ToArray();
            }
            else
            {
                using var _ = ArrayBuilder<SymbolInformation>.GetInstance(out var symbols);
                foreach (var item in navBarItems)
                {
                    symbols.AddIfNotNull(GetSymbolInformation(item, document, text, containerName: null, symbolInformationCreationService));

                    foreach (var childItem in item.ChildItems)
                        symbols.AddIfNotNull(GetSymbolInformation(childItem, document, text, item.Text, symbolInformationCreationService));
                }

                return symbols.ToArray();
            }
        }

        /// <summary>
        /// Get a symbol information from a specified nav bar item.
        /// </summary>
        private static SymbolInformation? GetSymbolInformation(
            RoslynNavigationBarItem item, Document document, SourceText text, string? containerName, ILspSymbolInformationCreationService symbolInformationCreationService)
        {
            if (item is not RoslynNavigationBarItem.SymbolItem symbolItem || symbolItem.Location.InDocumentInfo == null)
                return null;

            return symbolInformationCreationService.Create(
                GetDocumentSymbolName(item.Text),
                containerName,
                ProtocolConversions.GlyphToSymbolKind(item.Glyph),
                new LSP.Location
                {
                    Uri = document.GetURI(),
                    Range = ProtocolConversions.TextSpanToRange(symbolItem.Location.InDocumentInfo.Value.navigationSpan, text),
                },
                item.Glyph);
        }

        /// <summary>
        /// Get a document symbol from a specified nav bar item.
        /// </summary>
        private static RoslynDocumentSymbol? GetDocumentSymbol(
            RoslynNavigationBarItem item, SourceText text, CancellationToken cancellationToken)
        {
            if (item is not RoslynNavigationBarItem.SymbolItem symbolItem ||
                symbolItem.Location.InDocumentInfo == null)
            {
                return null;
            }

            var (spans, navigationSpan) = symbolItem.Location.InDocumentInfo.Value;
            if (spans.Length == 0)
                return null;

            return new RoslynDocumentSymbol
            {
                Name = GetDocumentSymbolName(symbolItem.Name),
                Detail = item.Text,
                Kind = ProtocolConversions.GlyphToSymbolKind(item.Glyph),
                Glyph = (int)item.Glyph,
#pragma warning disable CS0618 // SymbolInformation.Deprecated is obsolete, use Tags
                Deprecated = symbolItem.IsObsolete,
#pragma warning restore CS0618
                Range = ProtocolConversions.TextSpanToRange(spans.First(), text),
                SelectionRange = ProtocolConversions.TextSpanToRange(navigationSpan, text),
                Children = GetChildren(item.ChildItems, text, cancellationToken),
            };

            static RoslynDocumentSymbol[] GetChildren(
                ImmutableArray<RoslynNavigationBarItem> items, SourceText text, CancellationToken cancellationToken)
            {
                using var _ = ArrayBuilder<RoslynDocumentSymbol>.GetInstance(items.Length, out var list);
                foreach (var item in items)
                    list.AddIfNotNull(GetDocumentSymbol(item, text, cancellationToken));

                return list.ToArray();
            }
        }

        /// <summary>
        /// DocumentSymbol name cannot be null or empty. Check if the name is invalid,
        /// and if so return a substitute string.
        /// </summary>
        /// <param name="proposedName">Name proposed for DocumentSymbol</param>
        /// <returns>Valid name for DocumentSymbol</returns>
        private static string GetDocumentSymbolName(string proposedName)
        {
            return String.IsNullOrEmpty(proposedName) ? "." : proposedName;
        }
    }
}
