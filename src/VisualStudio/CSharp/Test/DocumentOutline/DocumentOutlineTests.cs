// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.LanguageServices.DocumentOutline;
using Xunit;
using Range = Microsoft.VisualStudio.LanguageServer.Protocol.Range;

namespace Roslyn.VisualStudio.CSharp.UnitTests.DocumentOutline
{
    public class DocumentOutlineTests
    {
        private const int NumParentNodes = 3;
        private const int NumChildNodes = 3;

        // Mocks a nested DocumentSymbol[] returned from an LSP document symbol request
        private static DocumentSymbol[] GetDocumentSymbols()
        {
            var documentSymbols = new DocumentSymbol[NumParentNodes];
            for (var i = 0; i < NumParentNodes; i++)
            {
                var children = new DocumentSymbol[NumChildNodes];
                for (var j = 0; j < NumChildNodes; j++)
                {
                    var child = new DocumentSymbol
                    {
                        Name = (i % 2 == 0 ? "Method" : "Field") + (j + i).ToString(),
                        Kind = i % 2 == 0 ? SymbolKind.Method : SymbolKind.Field,
                        Range = new Range
                        {
                            Start = new Position(j, j),
                            End = new Position(j, j + 1)
                        },
                    };

                    children[j] = child;
                }

                var documentSymbol = new DocumentSymbol
                {
                    Name = (i % 2 == 0 ? "Class" : "Interface") + i.ToString(),
                    Kind = i % 2 == 0 ? SymbolKind.Class : SymbolKind.Interface,
                    Range = new Range
                    {
                        Start = new Position(i, i),
                        End = new Position(i, i + 1)
                    },
                    Children = children
                };

                documentSymbols[i] = documentSymbol;
            }

            return documentSymbols;
        }

        [Fact]
        public void TestSortByName()
        {
            var sortedByName = DocumentOutlineHelper.Sort(
                DocumentOutlineHelper.GetDocumentSymbolItems(GetDocumentSymbols()), SortOption.Name, CancellationToken.None);

            CheckSortedByName(sortedByName);
            foreach (var documentSymbolItem in sortedByName)
                CheckSortedByName(documentSymbolItem.Children);

            static void CheckSortedByName(ImmutableArray<DocumentSymbolItem> sortedByName)
            {
                for (var i = 0; i < sortedByName.Length - 1; i++)
                    Assert.True(StringComparer.Ordinal.Compare(sortedByName[i].Name, sortedByName[i + 1].Name) <= 0);
            }
        }

        [Fact]
        public void TestSortByOrder()
        {
            var sortedByOrder = DocumentOutlineHelper.Sort(
                DocumentOutlineHelper.GetDocumentSymbolItems(GetDocumentSymbols()), SortOption.Location, CancellationToken.None);

            CheckSortedByOrder(sortedByOrder);
            foreach (var documentSymbolItem in sortedByOrder)
                CheckSortedByOrder(documentSymbolItem.Children);

            static void CheckSortedByOrder(ImmutableArray<DocumentSymbolItem> sortedByOrder)
            {
                for (var i = 0; i < sortedByOrder.Length - 1; i++)
                {
                    Assert.True(sortedByOrder[i].StartPosition.Line <= sortedByOrder[i + 1].StartPosition.Line);
                    if (sortedByOrder[i].StartPosition.Line == sortedByOrder[i + 1].StartPosition.Line)
                        Assert.True(sortedByOrder[i].StartPosition.Character < sortedByOrder[i + 1].StartPosition.Character);
                }
            }
        }

        [Fact]
        public void TestSortByType()
        {
            var sortedByType = DocumentOutlineHelper.Sort(
                DocumentOutlineHelper.GetDocumentSymbolItems(GetDocumentSymbols()), SortOption.Type, CancellationToken.None);

            CheckSortedByType(sortedByType);
            foreach (var documentSymbolItem in sortedByType)
                CheckSortedByType(documentSymbolItem.Children);

            static void CheckSortedByType(ImmutableArray<DocumentSymbolItem> sortedByType)
            {
                for (var i = 0; i < sortedByType.Length - 1; i++)
                {
                    Assert.True(sortedByType[i].SymbolKind <= sortedByType[i + 1].SymbolKind);
                    if (sortedByType[i].SymbolKind == sortedByType[i + 1].SymbolKind)
                    {
                        Assert.True(StringComparer.Ordinal.Compare(sortedByType[i].Name, sortedByType[i + 1].Name) <= 0);
                    }
                }
            }
        }

        [Fact]
        public void TestSearch()
        {
            var documentSymbolModels = DocumentOutlineHelper.GetDocumentSymbolItems(GetDocumentSymbols());

            Assert.Equal(2, DocumentOutlineHelper.Search(documentSymbolModels, "Class", CancellationToken.None).Length);
            Assert.Equal(1, DocumentOutlineHelper.Search(documentSymbolModels, "Field", CancellationToken.None).Length);
            Assert.Empty(DocumentOutlineHelper.Search(documentSymbolModels, "Banana", CancellationToken.None));
        }
    }
}
