// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Internal.TypeSystem;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LanguageServices;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using Range = Microsoft.VisualStudio.LanguageServer.Protocol.Range;

namespace Roslyn.VisualStudio.CSharp.UnitTests.DocumentOutline
{
    public class DocumentOutlineTests
    {
        private static DocumentSymbol[] GetDocumentSymbols()
        {
            var documentSymbols = Array.Empty<DocumentSymbol>();
            for (var i = 0; i < 2; i++)
            {
                var children = Array.Empty<DocumentSymbol>();
                for (var j = 0; j < 3; j++)
                {
                    var child = new DocumentSymbol
                    {
                        Name = "Method" + j.ToString(),
                        Kind = SymbolKind.Method,
                        Range = new Range
                        {
                            Start = new Position(j, 0),
                            End = new Position(j, 1)
                        },
                    };

                    children = children.Append(child);
                }

                var documentSymbol = new DocumentSymbol
                {
                    Name = "Class" + i.ToString(),
                    Kind = SymbolKind.Class,
                    Range = new Range
                    {
                        Start = new Position(i, 0),
                        End = new Position(i, 1)
                    },
                    Children = children
                };

                documentSymbols = documentSymbols.Append(documentSymbol);
            }
            return documentSymbols;
        }

        private static List<DocSymbol> GetDocSymbols()
        {
            var documentSymbols = new List<DocSymbol>();
            for (var i = 0; i < 2; i++)
            {
                var children = new ObservableCollection<DocSymbol>();
                for (var j = 0; j < 3; j++)
                {
                    var child = new DocSymbol(
                        name: "Method" + j.ToString(),
                        symbolKind: SymbolKind.Method,
                        startLine: j,
                        startChar: 0,
                        endLine: j,
                        endChar: 1);

                    children.Add(child);
                }

                var docSymbol = new DocSymbol(
                        name: "Class" + i.ToString(),
                        symbolKind: SymbolKind.Class,
                        startLine: i,
                        startChar: 0,
                        endLine: i,
                        endChar: 1)
                {
                    Children = children
                };

                documentSymbols.Add(docSymbol);
            }
            return documentSymbols;
        }

        private static void CompareDocSymbols(DocSymbol node, DocSymbol expectedNode)
        {
            Assert.Equal(node.Name, expectedNode.Name);
            Assert.Equal(node.SymbolKind, expectedNode.SymbolKind);
            Assert.Equal(node.StartLine, expectedNode.StartLine);
            Assert.Equal(node.StartChar, expectedNode.StartChar);
            Assert.Equal(node.EndLine, expectedNode.EndLine);
            Assert.Equal(node.EndChar, expectedNode.EndChar);
        }

        [Fact]
        public void TestGetDocumentSymbols()
        {
            var documentSymbols = Array.Empty<DocumentSymbol>();
            var result = DocumentOutlineHelper.GetDocumentSymbols(documentSymbols);
            var expectedResult = new List<DocSymbol>();
            Assert.Equal(result, expectedResult);

            documentSymbols = GetDocumentSymbols();
            result = DocumentOutlineHelper.GetDocumentSymbols(documentSymbols);
            expectedResult = GetDocSymbols();

            for (var i = 0; i < result.Count; i++)
            {
                var node = result[i];
                var expectedNode = expectedResult[i];
                CompareDocSymbols(node, expectedNode);

                for (var j = 0; j < node.Children.Count; j++)
                {
                    CompareDocSymbols(node.Children[j], expectedNode.Children[j]);
                }
            }
        }
    }
}
