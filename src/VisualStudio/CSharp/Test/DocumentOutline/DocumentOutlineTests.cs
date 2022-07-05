// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.LanguageServices.DocumentOutline;
using Roslyn.Utilities;
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

        [Fact]
        public void TestSort()
        {
            var documentSymbolModels = DocumentOutlineHelper.GetDocumentSymbolModels(GetDocumentSymbols());

            var sortedByName = DocumentOutlineHelper.Sort(documentSymbolModels, SortOption.Name, null, null);
            CheckSortedByName(sortedByName);
            for (var i = 0; i < sortedByName.Length; i++)
            {
                CheckSortedByName(sortedByName[i].Children);
            }

            static void CheckSortedByName(ImmutableArray<DocumentSymbolItem> sortedByName)
            {
                for (var i = 0; i < sortedByName.Length - 1; i++)
                {
                    var name1 = sortedByName[i].Name;
                    var name2 = sortedByName[i + 1].Name;
                    Assert.True(StringComparer.Ordinal.Compare(name1, name2) <= 0);
                }
            }

            var sortedByType = DocumentOutlineHelper.Sort(documentSymbolModels, SortOption.Type, null, null);
            CheckSortedByType(sortedByType);
            for (var i = 0; i < sortedByType.Length; i++)
            {
                CheckSortedByType(sortedByType[i].Children);
            }

            static void CheckSortedByType(ImmutableArray<DocumentSymbolItem> sortedByType)
            {
                for (var i = 0; i < sortedByType.Length - 1; i++)
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
        }

        [Fact]
        public void TestSearch()
        {
            var documentSymbolModels = DocumentOutlineHelper.GetDocumentSymbolModels(GetDocumentSymbols());

            var searchResults = DocumentOutlineHelper.Search(documentSymbolModels, "Class");
            Assert.True(searchResults.Length == 2);

            searchResults = DocumentOutlineHelper.Search(documentSymbolModels, "Field");
            Assert.True(searchResults.Length == 1);

            searchResults = DocumentOutlineHelper.Search(documentSymbolModels, "Banana");
            Assert.True(searchResults.IsEmpty);
        }
    }
}
