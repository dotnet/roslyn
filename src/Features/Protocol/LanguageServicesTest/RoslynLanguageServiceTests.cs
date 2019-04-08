using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using VSSymbolKind = Microsoft.VisualStudio.LanguageServer.Protocol.SymbolKind;
using Xunit;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Protocol.LanguageServices.UnitTests
{
    [UseExportProvider]
    public class RoslynLanguageServiceTests
    {
        [Fact]
        public async Task TestGetDocumentSymbolsAsync__WithHierarchicalSupport()
        {
            var markup =
@"[|class A
{
    [|void M()
    {
    }|]
}|]";
            var results = await TestGetDocumentSymbolsAsync(markup, true).ConfigureAwait(false);

            var expectedDocumentSymbols = new DocumentSymbol[]
            {
                CreateDocumentSymbol(VSSymbolKind.Class, "A"),
            };
            CreateDocumentSymbol(VSSymbolKind.Method, "M", expectedDocumentSymbols.First());

            AssertGetDocumentSymbolsAsync(results, expectedDocumentSymbols, AssertDocumentSymbolEquals);
        }

        [Fact]
        public async Task TestGetDocumentSymbolsAsync__WithoutHierarchicalSupport()
        {
            var markup =
@"class A
{
    void M()
    {
    }
}";
            var results = await TestGetDocumentSymbolsAsync(markup, false).ConfigureAwait(false);

            var expectedDocumentSymbols = new SymbolInformation[]
            {
                CreateSymbolInformation(VSSymbolKind.Class, "A"),
                CreateSymbolInformation(VSSymbolKind.Method, "M()")
            };

            AssertGetDocumentSymbolsAsync(results, expectedDocumentSymbols, AssertSymbolInformationEquals);
        }

        [Fact(Skip = "GetDocumentSymbolsAsync does not yet support locals.")]
        // TODO - Remove skip & modify once GetDocumentSymbolsAsync is updated to support more than 2 levels.
        public async Task TestGetDocumentSymbolsAsync__WithLocals()
        {
            var markup =
@"class A
{
    void Method()
    {
        int i = 1;
    }
}";
            var results = await TestGetDocumentSymbolsAsync(markup, false).ConfigureAwait(false);

            Assert.Equal(results.Length, 3);
        }

        private void AssertGetDocumentSymbolsAsync<T>(object[] actual, T[] expected, Action<T, T> assertionFunction)
        {
            Assert.Equal(expected.Length, actual.Length);
            var actualDocumentSymbols = actual.Select(a => (T)a).ToArray();

            for (var i = 0; i < actualDocumentSymbols.Length; i++)
            {
                assertionFunction(expected[i], actualDocumentSymbols[i]);
            }
        }

        private void AssertDocumentSymbolEquals(DocumentSymbol expected, DocumentSymbol actual)
        {
            Assert.Equal(expected.Kind, actual.Kind);
            Assert.Equal(expected.Name, actual.Name);
            Assert.Equal(expected.Children.Count, actual.Children.Count);

            for (var i = 0; i < actual.Children.Count; i++)
            {
                AssertDocumentSymbolEquals(expected.Children[i], actual.Children[i]);
            }
        }

        private void AssertSymbolInformationEquals(SymbolInformation expected, SymbolInformation actual)
        {
            Assert.Equal(expected.Kind, actual.Kind);
            Assert.Equal(expected.Name, actual.Name);
        }

        private SymbolInformation CreateSymbolInformation(VSSymbolKind kind, string name)
        {
            return new SymbolInformation()
            {
                Kind = kind,
                Name = name,
            };
        }

        private DocumentSymbol CreateDocumentSymbol(VSSymbolKind kind, string name, DocumentSymbol parent = null)
        {
            var documentSymbol = new DocumentSymbol()
            {
                Kind = kind,
                Name = name,
                Children = new List<DocumentSymbol>()
            };

            if (parent != null)
            {
                parent.Children.Add(documentSymbol);
            }

            return documentSymbol;
        }

        private async Task<object[]> TestGetDocumentSymbolsAsync(string markup, bool hierarchalSupport)
        {
            using (var workspace = TestWorkspace.CreateCSharp(markup))
            {
                var originalDocument = workspace.Documents.First();
                workspace.ChangeSolution(ChangeDocumentFilePathToValidURI(workspace.CurrentSolution, originalDocument, markup));
                var document = workspace.CurrentSolution.Projects.First().Documents.First();

                var clientCapabilities = new ClientCapabilities();
                var roslynLanguageService = new RoslynLanguageService(clientCapabilities, hierarchalSupport);

                var request = new DocumentSymbolParams
                {
                    TextDocument = new TextDocumentIdentifier()
                    {
                        Uri = new Uri(document.FilePath)
                    }
                };

                return await roslynLanguageService.GetDocumentSymbolsAsync(workspace.CurrentSolution, request, CancellationToken.None);
            }
        }

        /// <summary>
        /// Changes the document file path.
        /// Adds/Removes the document instead of updating file path due to
        /// https://github.com/dotnet/roslyn/issues/34837
        /// </summary>
        private Solution ChangeDocumentFilePathToValidURI(Solution originalSolution, TestHostDocument originalDocument, string markup)
        {
            var documentName = originalDocument.Name;
            var documentPath = "C:\\" + documentName;

            var solution = originalSolution.RemoveDocument(originalDocument.Id);

            var newDocumentId = DocumentId.CreateNewId(originalDocument.Project.Id);
            return solution.AddDocument(newDocumentId, documentName, markup, filePath: documentPath);
        }

        /*
        private static void SetupSelection(IWpfTextView textView, IEnumerable<Span> spans)
        {
            var snapshot = textView.TextSnapshot;
            if (spans.Count() == 1)
            {
                textView.Selection.Select(new SnapshotSpan(snapshot, spans.Single()), isReversed: false);
                textView.Caret.MoveTo(new SnapshotPoint(snapshot, spans.Single().End));
            }
            else
            {
                textView.Selection.Mode = TextSelectionMode.Box;
                textView.Selection.Select(new VirtualSnapshotPoint(snapshot, spans.First().Start),
                                          new VirtualSnapshotPoint(snapshot, spans.Last().End));
                textView.Caret.MoveTo(new SnapshotPoint(snapshot, spans.Last().End));
            }
        }*/

    }
}
