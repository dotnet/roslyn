using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Protocol.LanguageServices.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Xunit;
using VSSymbolKind = Microsoft.VisualStudio.LanguageServer.Protocol.SymbolKind;
using VSLocation = Microsoft.VisualStudio.LanguageServer.Protocol.Location;

namespace Microsoft.CodeAnalysis.Protocol.LanguageServices.UnitTests
{
    [UseExportProvider]
    public class RoslynLanguageServiceTests
    {
        [Fact]
        public async Task TestGetDocumentSymbolsAsync__WithHierarchicalSupport()
        {
            var markup =
@"{|class:class A
{
    {|method:void M()
    {
    }|}
}|}";
            var (solution, ranges) = CreateTestSolution(markup);
            var expectedDocumentSymbols = new DocumentSymbol[]
            {
                CreateDocumentSymbol(VSSymbolKind.Class, "A", ranges["class"].First())
            };
            CreateDocumentSymbol(VSSymbolKind.Method, "M", ranges["method"].First(), expectedDocumentSymbols.First());

            var results = await TestGetDocumentSymbolsAsync(solution, true);
            AssertCollection(results, expectedDocumentSymbols, AssertDocumentSymbolEquals);
        }

        [Fact]
        public async Task TestGetDocumentSymbolsAsync__WithoutHierarchicalSupport()
        {
            var markup =
@"class {|class:A|}
{
    void {|method:M|}()
    {
    }
}";
            var (solution, ranges) = CreateTestSolution(markup);
            var expectedDocumentSymbols = new SymbolInformation[]
            {
                CreateSymbolInformation(VSSymbolKind.Class, "A", ranges["class"].First()),
                CreateSymbolInformation(VSSymbolKind.Method, "M()", ranges["method"].First())
            };

            var results = await TestGetDocumentSymbolsAsync(solution, false);
            AssertCollection(results, expectedDocumentSymbols, AssertSymbolInformationEquals);
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
            var (solution, _) = CreateTestSolution(markup);
            var results = await TestGetDocumentSymbolsAsync(solution, false).ConfigureAwait(false);
            Assert.Equal(results.Length, 3);
        }

        [Fact]
        public async Task TestGetWorkspaceSymbolsAsync_Class()
        {
            var markup =
@"class {|class:A|}
{
    void M()
    {
    }
}";
            var (solution, ranges) = CreateTestSolution(markup);
            var expectedDocumentSymbols = new SymbolInformation[]
            {
                CreateSymbolInformation(VSSymbolKind.Class, "A", ranges["class"].First())
            };

            var results = await TestGetWorkspaceSymbolsAsync(solution, "A").ConfigureAwait(false);
            AssertCollection(results, expectedDocumentSymbols, AssertSymbolInformationEquals);
        }

        [Fact]
        public async Task TestGetWorkspaceSymbolsAsync_Method()
        {
            var markup =
@"class A
{
    void {|method:M|}()
    {
    }
}";
            var (solution, ranges) = CreateTestSolution(markup);
            var expectedDocumentSymbols = new SymbolInformation[]
            {
                CreateSymbolInformation(VSSymbolKind.Method, "M", ranges["method"].First())
            };

            var results = await TestGetWorkspaceSymbolsAsync(solution, "M").ConfigureAwait(false);
            AssertCollection(results, expectedDocumentSymbols, AssertSymbolInformationEquals);
        }

        [Fact(Skip = "GetWorkspaceSymbolsAsync does not yet support locals.")]
        // TODO - Remove skip & modify once GetWorkspaceSymbolsAsync is updated to support all symbols.
        public async Task TestGetWorkspaceSymbolsAsync_Local()
        {
            var markup =
@"class A
{
    void M()
    {
        int {|local:i|} = 1;
    }
}";
            var (solution, ranges) = CreateTestSolution(markup);
            var expectedDocumentSymbols = new SymbolInformation[]
            {
                CreateSymbolInformation(VSSymbolKind.Variable, "i", ranges["local"].First())
            };

            var results = await TestGetWorkspaceSymbolsAsync(solution, "i").ConfigureAwait(false);
            AssertCollection(results, expectedDocumentSymbols, AssertSymbolInformationEquals);
        }

        [Fact]
        public async Task TestGetWorkspaceSymbolsAsync_MultipleKinds()
        {
            var markup =
@"class A
{
    int {|field:F|};
    void M()
    {
    }
    class {|class:F|}
    {
        int {|field:F|};
    }
}";
            var (solution, ranges) = CreateTestSolution(markup);
            var expectedDocumentSymbols = new SymbolInformation[]
            {
                CreateSymbolInformation(VSSymbolKind.Field, "F", ranges["field"][0]),
                CreateSymbolInformation(VSSymbolKind.Class, "F", ranges["class"].First()),
                CreateSymbolInformation(VSSymbolKind.Field, "F", ranges["field"][1])
            };

            var results = await TestGetWorkspaceSymbolsAsync(solution, "F").ConfigureAwait(false);
            AssertCollection(results, expectedDocumentSymbols, AssertSymbolInformationEquals);
        }

        private static void AssertCollection<T>(object[] actual, T[] expected, Action<T, T> assertionFunction)
        {
            Assert.Equal(expected.Length, actual.Length);
            var actualDocumentSymbols = actual.Select(a => (T)a).ToArray();

            for (var i = 0; i < actualDocumentSymbols.Length; i++)
            {
                assertionFunction(expected[i], actualDocumentSymbols[i]);
            }
        }

        private static void AssertDocumentSymbolEquals(DocumentSymbol expected, DocumentSymbol actual)
        {
            Assert.Equal(expected.Kind, actual.Kind);
            Assert.Equal(expected.Name, actual.Name);
            Assert.Equal(expected.Range, actual.Range);
            Assert.Equal(expected.Children.Count, actual.Children.Count);

            for (var i = 0; i < actual.Children.Count; i++)
            {
                AssertDocumentSymbolEquals(expected.Children[i], actual.Children[i]);
            }
        }

        private static void AssertSymbolInformationEquals(SymbolInformation expected, SymbolInformation actual)
        {
            Assert.Equal(expected.Kind, actual.Kind);
            Assert.Equal(expected.Name, actual.Name);
            Assert.Equal(expected.Location.Range, actual.Location.Range);
        }

        private static SymbolInformation CreateSymbolInformation(VSSymbolKind kind, string name, Range range)
        {
            return new SymbolInformation()
            {
                Kind = kind,
                Name = name,
                Location = new VSLocation()
                {
                    Range = range
                }
            };
        }

        private static DocumentSymbol CreateDocumentSymbol(VSSymbolKind kind, string name, Range range, DocumentSymbol parent = null)
        {
            var documentSymbol = new DocumentSymbol()
            {
                Kind = kind,
                Name = name,
                Range = range,
                Children = new List<DocumentSymbol>()
            };

            if (parent != null)
            {
                parent.Children.Add(documentSymbol);
            }

            return documentSymbol;
        }

        private static (Solution solution, Dictionary<string, ImmutableArray<Range>> ranges) CreateTestSolution(string markup)
        {
            using (var workspace = TestWorkspace.CreateCSharp(markup))
            {
                var originalDocument = workspace.Documents.First();
                var text = originalDocument.TextBuffer.AsTextContainer().CurrentText;
                var ranges = originalDocument.AnnotatedSpans.ToDictionary(
                    annotatedSpan => annotatedSpan.Key,
                    annotatedSpan => annotatedSpan.Value.Select(s => s.ToRange(text)).ToImmutableArray());

                // Pass in the text without markup.
                workspace.ChangeSolution(ChangeDocumentFilePathToValidURI(workspace.CurrentSolution, originalDocument, text));
                return (workspace.CurrentSolution, ranges);
            }
        }

        private static async Task<object[]> TestGetDocumentSymbolsAsync(Solution solution, bool hierarchalSupport)
        {
            var document = solution.Projects.First().Documents.First();

            var clientCapabilities = new ClientCapabilities();
            var roslynLanguageService = new RoslynLanguageService(clientCapabilities, hierarchalSupport);

            var request = new DocumentSymbolParams
            {
                TextDocument = new TextDocumentIdentifier()
                {
                    Uri = new Uri(document.FilePath)
                }
            };

            return await roslynLanguageService.GetDocumentSymbolsAsync(solution, request, CancellationToken.None);
        }

        private static async Task<SymbolInformation[]> TestGetWorkspaceSymbolsAsync(Solution solution, string query)
        {
            var clientCapabilities = new ClientCapabilities();
            var roslynLanguageService = new RoslynLanguageService(clientCapabilities, true);

            var request = new WorkspaceSymbolParams
            {
                Query = query
            };

            return await roslynLanguageService.GetWorkspaceSymbolsAsync(solution, request, CancellationToken.None);
        }

        /// <summary>
        /// Changes the document file path.
        /// Adds/Removes the document instead of updating file path due to
        /// https://github.com/dotnet/roslyn/issues/34837
        /// </summary>
        private static Solution ChangeDocumentFilePathToValidURI(Solution originalSolution, TestHostDocument originalDocument, SourceText text)
        {
            var documentName = originalDocument.Name;
            var documentPath = "C:\\" + documentName;

            var solution = originalSolution.RemoveDocument(originalDocument.Id);

            var newDocumentId = DocumentId.CreateNewId(originalDocument.Project.Id);
            return solution.AddDocument(newDocumentId, documentName, text, filePath: documentPath);
        }
    }
}
