// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LanguageServices;
using Roslyn.Utilities;
using Xunit;
using Range = Microsoft.VisualStudio.LanguageServer.Protocol.Range;

namespace Roslyn.VisualStudio.CSharp.UnitTests.DocumentOutline
{
    public class DocumentOutlineTests
    {
        private const int NumParentNodes = 3;
        private const int NumChildNodes = 3;

        // Mocks a DocumentSymbol[] returned from an LSP document symbol request
        private static DocumentSymbol[] GetDocumentSymbols()
        {
            var documentSymbols = Array.Empty<DocumentSymbol>();
            for (var i = 0; i < NumParentNodes; i++)
            {
                var children = Array.Empty<DocumentSymbol>();
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

                    children = children.Append(child);
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

                documentSymbols = documentSymbols.Append(documentSymbol);
            }

            return documentSymbols;
        }

        // The ObservableCollection<DocumentSymbolViewModel> equivalent of GetDocumentSymbols()
        private static ObservableCollection<DocumentSymbolViewModel> GetDocumentSymbolViewModels()
        {
            var documentSymbols = GetDocumentSymbols();
            var documentSymbolModels = new ObservableCollection<DocumentSymbolViewModel>();

            foreach (var documentSymbol in documentSymbols)
            {
                var documentSymbolModel = new DocumentSymbolViewModel(documentSymbol);
                var children = new ObservableCollection<DocumentSymbolViewModel>();
                if (documentSymbol.Children is not null)
                {
                    foreach (var child in documentSymbol.Children)
                    {
                        var childModel = new DocumentSymbolViewModel(child);
                        children.Add(childModel);
                    }
                }

                documentSymbolModel.Children = children;
                documentSymbolModels.Add(documentSymbolModel);
            }

            return documentSymbolModels;
        }

        private static void CompareDocumentSymbolViewModels(DocumentSymbolViewModel node, DocumentSymbolViewModel expectedNode)
        {
            Assert.Equal(node.Name, expectedNode.Name);
            Assert.Equal(node.SymbolKind, expectedNode.SymbolKind);
            Assert.Equal(node.StartLine, expectedNode.StartLine);
            Assert.Equal(node.StartChar, expectedNode.StartChar);
            Assert.Equal(node.EndLine, expectedNode.EndLine);
            Assert.Equal(node.EndChar, expectedNode.EndChar);
            Assert.Equal(node.Children.Count, expectedNode.Children.Count);
        }

        [Fact]
        public void TestGetDocumentSymbols()
        {
            // Test the empty case
            var documentSymbols = Array.Empty<DocumentSymbol>();
            var result = DocumentOutlineHelper.GetDocumentSymbols(documentSymbols);
            var expectedResult = new ObservableCollection<DocumentSymbolViewModel>();
            Assert.Equal(result, expectedResult);

            // Test using mock data
            documentSymbols = GetDocumentSymbols();
            result = DocumentOutlineHelper.GetDocumentSymbols(documentSymbols);
            expectedResult = GetDocumentSymbolViewModels();

            Assert.Equal(result.Count, expectedResult.Count);
            for (var i = 0; i < result.Count; i++)
            {
                var node = result[i];
                var expectedNode = expectedResult[i];
                CompareDocumentSymbolViewModels(node, expectedNode);
                for (var j = 0; j < node.Children.Count; j++)
                {
                    CompareDocumentSymbolViewModels(node.Children[j], expectedNode.Children[j]);
                }
            }
        }

        [Fact]
        public void TestSort()
        {
            var documentSymbolModels = GetDocumentSymbolViewModels();

            static void CheckSortedByName(ObservableCollection<DocumentSymbolViewModel> sortedByName)
            {
                for (var i = 0; i < sortedByName.Count - 1; i++)
                {
                    var name1 = sortedByName[i].Name;
                    var name2 = sortedByName[i + 1].Name;
                    Assert.True(StringComparer.Ordinal.Compare(name1, name2) <= 0);
                }
            }

            var sortedByName = DocumentOutlineHelper.Sort(documentSymbolModels, SortOption.Name);
            CheckSortedByName(sortedByName);
            for (var i = 0; i < sortedByName.Count; i++)
            {
                CheckSortedByName(sortedByName[i].Children);
            }

            static void CheckSortedByOrder(ObservableCollection<DocumentSymbolViewModel> sortedByOrder)
            {
                for (var i = 0; i < sortedByOrder.Count - 1; i++)
                {
                    var position1 = new Tuple<int, int>(sortedByOrder[i].StartLine, sortedByOrder[i].StartChar);
                    var position2 = new Tuple<int, int>(sortedByOrder[i + 1].StartLine, sortedByOrder[i + 1].StartChar);
                    Assert.True(position1.Item1 <= position2.Item2);
                    if (position1.Item1 == position2.Item1)
                    {
                        Assert.True(position1.Item2 < position1.Item2);
                    }
                }
            }

            var sortedByOrder = DocumentOutlineHelper.Sort(documentSymbolModels, SortOption.Order);
            CheckSortedByOrder(sortedByOrder);
            for (var i = 0; i < sortedByOrder.Count; i++)
            {
                CheckSortedByOrder(sortedByOrder[i].Children);
            }

            static void CheckSortedByType(ObservableCollection<DocumentSymbolViewModel> sortedByType)
            {
                for (var i = 0; i < sortedByType.Count - 1; i++)
                {
                    var type1 = sortedByType[i].SymbolKind;
                    var type2 = sortedByType[i + 1].SymbolKind;
                    Assert.True(type1 <= type2);
                    if (type1 == type2)
                    {
                        var name1 = sortedByType[i].Name;
                        var name2 = sortedByType[i + 1].Name;
                        Assert.True(StringComparer.Ordinal.Compare(name1, name2) <= 0);
                    }
                }
            }

            var sortedByType = DocumentOutlineHelper.Sort(documentSymbolModels, SortOption.Type);
            CheckSortedByType(sortedByType);
            for (var i = 0; i < sortedByType.Count; i++)
            {
                CheckSortedByType(sortedByType[i].Children);
            }
        }

        [Fact]
        public void TestSearchNodeTree()
        {
            var documentSymbolModels = GetDocumentSymbolViewModels();

            // Case where all nodes match
            foreach (var documentSymbolModel in documentSymbolModels)
            {
                Assert.True(DocumentOutlineHelper.SearchNodeTree(documentSymbolModel, "e"));
            }

            // Case where exactly 1 parent matches search
            var results = new List<bool>();
            foreach (var documentSymbolModel in documentSymbolModels)
            {
                results.Add(DocumentOutlineHelper.SearchNodeTree(documentSymbolModel, "Class0"));
            }

            Assert.True(results.Count(item => item) == 1);

            // Case where exactly 2 children match search
            results = new List<bool>();
            foreach (var documentSymbolModel in documentSymbolModels)
            {
                results.Add(DocumentOutlineHelper.SearchNodeTree(documentSymbolModel, "Method"));
            }

            Assert.True(results.Count(item => item) == 2);

            // Case where nothing matches search
            foreach (var documentSymbolModel in documentSymbolModels)
            {
                Assert.False(DocumentOutlineHelper.SearchNodeTree(documentSymbolModel, "xxx"));
            }
        }
    }
}
