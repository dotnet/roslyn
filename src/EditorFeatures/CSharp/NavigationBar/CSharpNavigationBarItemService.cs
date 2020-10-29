// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.DocumentSymbols;
using Microsoft.CodeAnalysis.DocumentSymbols;
using Microsoft.CodeAnalysis.Editor.Extensibility.NavigationBar;
using Microsoft.CodeAnalysis.Editor.Implementation.NavigationBar;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.NavigationBar
{
    [ExportLanguageService(typeof(INavigationBarItemService), LanguageNames.CSharp), Shared]
    internal class CSharpNavigationBarItemService : AbstractNavigationBarItemService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpNavigationBarItemService()
        {
        }

        public override async Task<IList<NavigationBarItem>> GetItemsAsync(Document document, CancellationToken cancellationToken)
        {
            var documentSymbolsSerivce = document.GetRequiredLanguageService<IDocumentSymbolsService>();
            var membersInDocument = await documentSymbolsSerivce.GetSymbolsInDocumentAsync(document, DocumentSymbolsOptions.TypesAndMembersOnly, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (membersInDocument.IsEmpty)
            {
                return SpecializedCollections.EmptyList<NavigationBarItem>();
            }

            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            // If we got members from the symbol service, there had to have been a tree
            Debug.Assert(tree is not null);
            return MapMembersToNavigationBarItems(membersInDocument, cancellationToken);
        }

        private static IList<NavigationBarItem> MapMembersToNavigationBarItems(ImmutableArray<DocumentSymbolInfo> types, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.NavigationBar_ItemService_GetMembersInTypes_CSharp, cancellationToken))
            {
                var typeSymbolIndexProvider = new NavigationBarSymbolIdIndexProvider(caseSensitive: true);
                var items = new List<NavigationBarItem>();

                foreach (var typeInfo in types)
                {
                    var memberSymbolIndexProvider = new NavigationBarSymbolIdIndexProvider(caseSensitive: true);

                    var memberItems = new List<NavigationBarItem>();
                    foreach (var memberInfo in typeInfo.ChildrenSymbols)
                    {
                        var symbolKey = memberInfo.GetSymbolKey();
                        memberItems.Add(CreateItemForMember(
                            memberInfo,
                            symbolKey,
                            memberSymbolIndexProvider.GetIndexForSymbolId(symbolKey)));
                    }

                    memberItems.Sort((x, y) =>
                    {
                        var textComparison = x.Text.CompareTo(y.Text);
                        return textComparison != 0 ? textComparison : x.Grayed.CompareTo(y.Grayed);
                    });

                    var symbolId = typeInfo.GetSymbolKey();
                    items.Add(new NavigationBarSymbolItem(
                        text: typeInfo.Text,
                        glyph: typeInfo.Glyph,
                        indent: 0,
                        spans: typeInfo.EnclosingSpans,
                        navigationSymbolId: symbolId,
                        navigationSymbolIndex: typeSymbolIndexProvider.GetIndexForSymbolId(symbolId),
                        childItems: memberItems));
                }

                items.Sort((x1, x2) => x1.Text.CompareTo(x2.Text));
                return items;
            }
        }

        private static NavigationBarItem CreateItemForMember(DocumentSymbolInfo member, SymbolKey symbolKey, int symbolIndex)
        {
            return new NavigationBarSymbolItem(
                member.Text,
                member.Glyph,
                member.EnclosingSpans,
                symbolKey,
                symbolIndex,
                grayed: member.EnclosingSpans.Length == 0);
        }

        protected internal override VirtualTreePoint? GetSymbolItemNavigationPoint(Document document, NavigationBarSymbolItem item, CancellationToken cancellationToken)
        {
            var compilation = document.Project.GetCompilationAsync(cancellationToken).WaitAndGetResult(cancellationToken);
            // If we're being called back, there must have been a compilation to resolve the item in the first place
            Debug.Assert(compilation is not null);
            var symbols = item.NavigationSymbolId.Resolve(compilation, cancellationToken: cancellationToken);

            var symbol = symbols.Symbol;

            if (symbol == null)
            {
                if (item.NavigationSymbolIndex < symbols.CandidateSymbols.Length)
                {
                    symbol = symbols.CandidateSymbols[item.NavigationSymbolIndex.Value];
                }
                else
                {
                    return null;
                }
            }

            var syntaxTree = document.GetSyntaxTreeSynchronously(cancellationToken);
            var location = symbol.Locations.FirstOrDefault(l => l.SourceTree?.Equals(syntaxTree) ?? false);

            if (location == null)
            {
                location = symbol.Locations.FirstOrDefault();
            }

            if (location == null || location.SourceTree == null)
            {
                return null;
            }

            return new VirtualTreePoint(location.SourceTree, location.SourceTree.GetText(cancellationToken), location.SourceSpan.Start);
        }

        public override void NavigateToItem(Document document, NavigationBarItem item, ITextView textView, CancellationToken cancellationToken)
            => NavigateToSymbolItem(document, (NavigationBarSymbolItem)item, cancellationToken);
    }
}
