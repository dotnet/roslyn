// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
using Roslyn.Utilities;
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
            var (solution, locations) = CreateTestSolution(markup);
            var expected = new DocumentSymbol[]
            {
                CreateDocumentSymbol(VSSymbolKind.Class, "A", locations["class"].First())
            };
            CreateDocumentSymbol(VSSymbolKind.Method, "M", locations["method"].First(), expected.First());

            var results = await RunGetDocumentSymbolsAsync(solution, true);
            AssertCollection(expected, results, AssertDocumentSymbolEquals);
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
            var (solution, locations) = CreateTestSolution(markup);
            var expected = new SymbolInformation[]
            {
                CreateSymbolInformation(VSSymbolKind.Class, "A", locations["class"].First()),
                CreateSymbolInformation(VSSymbolKind.Method, "M()", locations["method"].First())
            };

            var results = await RunGetDocumentSymbolsAsync(solution, false);
            AssertCollection(expected, results, AssertSymbolInformationEquals);
        }

        [Fact(Skip = "GetDocumentSymbolsAsync does not yet support locals.")]
        // TODO - Remove skip & modify once GetDocumentSymbolsAsync is updated to support more than 2 levels.
        // https://github.com/dotnet/roslyn/projects/45#card-20033869
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
            var results = await RunGetDocumentSymbolsAsync(solution, false).ConfigureAwait(false);
            Assert.Equal(results.Length, 3);
        }

        [Fact]
        public async Task TestGetDocumentSymbolsAsync__NoSymbols()
        {
            var (solution, _) = CreateTestSolution(string.Empty);

            var results = await RunGetDocumentSymbolsAsync(solution, true);
            Assert.Empty(results);
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
            var (solution, locations) = CreateTestSolution(markup);
            var expected = new SymbolInformation[]
            {
                CreateSymbolInformation(VSSymbolKind.Class, "A", locations["class"].First())
            };

            var results = await RunGetWorkspaceSymbolsAsync(solution, "A").ConfigureAwait(false);
            AssertCollection(expected, results, AssertSymbolInformationEquals);
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
            var (solution, locations) = CreateTestSolution(markup);
            var expected = new SymbolInformation[]
            {
                CreateSymbolInformation(VSSymbolKind.Method, "M", locations["method"].First())
            };

            var results = await RunGetWorkspaceSymbolsAsync(solution, "M").ConfigureAwait(false);
            AssertCollection(expected, results, AssertSymbolInformationEquals);
        }

        [Fact(Skip = "GetWorkspaceSymbolsAsync does not yet support locals.")]
        // TODO - Remove skip & modify once GetWorkspaceSymbolsAsync is updated to support all symbols.
        // https://github.com/dotnet/roslyn/projects/45#card-20033822
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
            var (solution, locations) = CreateTestSolution(markup);
            var expected = new SymbolInformation[]
            {
                CreateSymbolInformation(VSSymbolKind.Variable, "i", locations["local"].First())
            };

            var results = await RunGetWorkspaceSymbolsAsync(solution, "i").ConfigureAwait(false);
            AssertCollection(expected, results, AssertSymbolInformationEquals);
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
            var (solution, locations) = CreateTestSolution(markup);
            var expected = new SymbolInformation[]
            {
                CreateSymbolInformation(VSSymbolKind.Field, "F", locations["field"][0]),
                CreateSymbolInformation(VSSymbolKind.Class, "F", locations["class"].First()),
                CreateSymbolInformation(VSSymbolKind.Field, "F", locations["field"][1])
            };

            var results = await RunGetWorkspaceSymbolsAsync(solution, "F").ConfigureAwait(false);
            AssertCollection(expected, results, AssertSymbolInformationEquals);
        }

        [Fact]
        public async Task TestGetWorkspaceSymbolsAsync_MultipleDocuments()
        {
            var markups = new string[]
            {
@"class A
{
    void {|method:M|}()
    {
    }
}",
@"class B
{
    void {|method:M|}()
    {
    }
}"
            };

            var (solution, locations) = CreateTestSolution(markups);
            var expected = new SymbolInformation[]
            {
                CreateSymbolInformation(VSSymbolKind.Method, "M", locations["method"][0]),
                CreateSymbolInformation(VSSymbolKind.Method, "M", locations["method"][1])
            };

            var results = await RunGetWorkspaceSymbolsAsync(solution, "M").ConfigureAwait(false);
            AssertCollection(expected, results, AssertSymbolInformationEquals);
        }

        [Fact]
        public async Task TestGetWorkspaceSymbolsAsync_NoSymbols()
        {
            var markup =
@"class A
{
    void M()
    {
    }
}";
            var (solution, _) = CreateTestSolution(markup);

            var results = await RunGetWorkspaceSymbolsAsync(solution, "NonExistingSymbol").ConfigureAwait(false);
            Assert.Empty(results);
        }

        [Fact]
        public async Task TestGetHoverAsync()
        {
            var markup =
@"class A
{
    /// <summary>
    /// A great method
    /// </summary>
    /// <param name='i'>an int</param>
    /// <returns>a string</returns>
    private string {|caret:|}Method(int i)
    {
    }
}";
            var (solution, locations) = CreateTestSolution(markup);
            var expected = "string A.Method(int i)\r\n> A great method";

            var results = await RunGetHoverAsync(solution, locations["caret"].First()).ConfigureAwait(false);
            var markupContent = results.Contents as MarkupContent;
            Assert.NotNull(markupContent);
            Assert.Equal(MarkupKind.Markdown, markupContent.Kind);
            Assert.Equal(expected, markupContent.Value);
        }

        [Fact]
        public async Task TestGetHoverAsync_InvalidLocation()
        {
            var markup =
@"class A
{
    /// <summary>
    /// A great method
    /// </summary>
    /// <param name='i'>an int</param>
    /// <returns>a string</returns>
    private string Method(int i)
    {
        {|caret:|}
    }
}";
            var (solution, locations) = CreateTestSolution(markup);

            var results = await RunGetHoverAsync(solution, locations["caret"].First()).ConfigureAwait(false);
            Assert.Null(results);
        }

        [Fact]
        public async Task TestGotoDefinitionAsync()
        {
            var markup =
@"class A
{
    string {|definition:aString|} = 'hello';
    void M()
    {
        var len = {|caret:|}aString.Length;
    }
}";
            var (solution, locations) = CreateTestSolution(markup);

            var results = await RunGotoDefinitionAsync(solution, locations["caret"].First());
            AssertLocations(locations["definition"], results);
        }

        [Fact]
        public async Task TestGotoDefinitionAsync_DifferentDocument()
        {
            var markups = new string[]
            {
@"namespace One
{
    class A
    {
        public static int {|definition:aInt|} = 1;
    }
}",
@"namespace One
{
    class B
    {
        int bInt = One.A.{|caret:|}aInt;
    }
}"
            };
            var (solution, locations) = CreateTestSolution(markups);

            var results = await RunGotoDefinitionAsync(solution, locations["caret"].First());
            AssertLocations(locations["definition"], results);
        }

        [Fact]
        public async Task TestGotoDefinitionAsync_InvalidLocation()
        {
            var markup =
@"class A
{
    void M()
    {{|caret:|}
        var len = aString.Length;
    }
}";
            var (solution, locations) = CreateTestSolution(markup);

            var results = await RunGotoDefinitionAsync(solution, locations["caret"].First());
            Assert.Empty(results);
        }

        [Fact]
        public async Task TestGotoTypeDefinitionAsync()
        {
            var markup =
@"class {|definition:A|}
{
}
class B
{
    {|caret:|}A classA;
}";
            var (solution, locations) = CreateTestSolution(markup);

            var results = await RunGotoTypeDefinitionAsync(solution, locations["caret"].First());
            AssertLocations(locations["definition"], results);
        }

        [Fact]
        public async Task TestGotoTypeDefinitionAsync_DifferentDocument()
        {
            var markups = new string[]
            {
@"namespace One
{
    class {|definition:A|}
    {
    }
}",
@"namespace One
{
    class B
    {
        {|caret:|}A classA;
    }
}"
            };
            var (solution, locations) = CreateTestSolution(markups);

            var results = await RunGotoTypeDefinitionAsync(solution, locations["caret"].First());
            AssertLocations(locations["definition"], results);
        }

        [Fact]
        public async Task TestGotoTypeDefinitionAsync_InvalidLocation()
        {
            var markup =
@"class {|definition:A|}
{
}
class B
{
    A classA;
    {|caret:|}
}";
            var (solution, locations) = CreateTestSolution(markup);

            var results = await RunGotoTypeDefinitionAsync(solution, locations["caret"].First());
            Assert.Empty(results);
        }

        [Fact]
        public async Task TestFindAllReferencesAsync()
        {
            var markup =
@"class A
{
    public int {|reference:someInt|} = 1;
    void M()
    {
        var i = {|reference:someInt|} + 1;
    }
}
class B
{
    int someInt = A.{|reference:someInt|} + 1;
    void M2()
    {
        var j = someInt + A.{|caret:|}{|reference:someInt|};
    }
}";
            var (solution, locations) = CreateTestSolution(markup);

            var results = await RunFindAllReferencesAsync(solution, locations["caret"].First(), true);
            AssertLocations(locations["reference"], results);
        }

        [Fact]
        public async Task TestFindAllReferencesAsync_DoNotIncludeDeclarations()
        {
            var markup =
@"class A
{
    public int someInt = 1;
    void M()
    {
        var i = {|reference:someInt|} + 1;
    }
}
class B
{
    int someInt = A.{|reference:someInt|} + 1;
    void M2()
    {
        var j = someInt + A.{|caret:|}{|reference:someInt|};
    }
}";
            var (solution, locations) = CreateTestSolution(markup);

            var results = await RunFindAllReferencesAsync(solution, locations["caret"].First(), false);
            AssertLocations(locations["reference"], results);
        }

        [Fact]
        public async Task TestFindAllReferencesAsync_MultipleDocuments()
        {
            var markups = new string[] {
@"class A
{
    public int {|reference:someInt|} = 1;
    void M()
    {
        var i = {|reference:someInt|} + 1;
    }
}",
@"class B
{
    int someInt = A.{|reference:someInt|} + 1;
    void M2()
    {
        var j = someInt + A.{|caret:|}{|reference:someInt|};
    }
}"
            };
            var (solution, locations) = CreateTestSolution(markups);

            var results = await RunFindAllReferencesAsync(solution, locations["caret"].First(), true);
            AssertLocations(locations["reference"], results);
        }

        [Fact]
        public async Task TestFindAllReferencesAsync_InvalidLocation()
        {
            var markup =
@"class A
{
    {|caret:|}
}";
            var (solution, ranges) = CreateTestSolution(markup);

            var results = await RunFindAllReferencesAsync(solution, ranges["caret"].First(), true);
            Assert.Empty(results);
        }

        [Fact]
        public async Task TestGotoImplementationAsync()
        {
            var markup =
@"interface IA
{
    void {|caret:|}M();
}
class A : IA
{
    void IA.{|implementation:M|}()
    {
    }
}";
            var (solution, locations) = CreateTestSolution(markup);

            var results = await RunGotoImplementationAsync(solution, locations["caret"].First());
            AssertLocations(locations["implementation"], results);
        }

        [Fact]
        public async Task TestGotoImplementationAsync_DifferentDocument()
        {
            var markups = new string[]
            {
@"namespace One
{
    interface IA
    {
        void {|caret:|}M();
    }
}",
@"namespace One
{
    class A : IA
    {
        void IA.{|implementation:M|}()
        {
        }
    }
}"
            };
            var (solution, locations) = CreateTestSolution(markups);

            var results = await RunGotoImplementationAsync(solution, locations["caret"].First());
            AssertLocations(locations["implementation"], results);
        }

        [Fact]
        public async Task TestGotoImplementationAsync_InvalidLocation()
        {
            var markup =
@"class A
{
    void M()
    {
        {|caret:|}
    }
}";
            var (solution, locations) = CreateTestSolution(markup);

            var results = await RunGotoImplementationAsync(solution, locations["caret"].First());
            Assert.Empty(results);
        }

        [Fact]
        public async Task TestGetDocumentHighlightAsync()
        {
            var markup =
@"class B
{
}
class A
{
    B {|text:classB|};
    void M()
    {
        var someVar = {|read:classB|};
        {|caret:|}{|write:classB|} = new B();
    }
}";
            var (solution, locations) = CreateTestSolution(markup);
            var expected = new DocumentHighlight[]
            {
                CreateDocumentHighlight(DocumentHighlightKind.Text, locations["text"].First()),
                CreateDocumentHighlight(DocumentHighlightKind.Write, locations["write"].First()),
                CreateDocumentHighlight(DocumentHighlightKind.Read, locations["read"].First())
            };

            var results = await RunGetDocumentHighlightAsync(solution, locations["caret"].First());
            AssertDocumentHighlights(expected, results);
        }

        [Fact]
        public async Task TestGetDocumentHighlightAsync_InvalidLocation()
        {
            var markup =
@"class A
{
    void M()
    {
        {|caret:|}
    }
}";
            var (solution, locations) = CreateTestSolution(markup);

            var results = await RunGetDocumentHighlightAsync(solution, locations["caret"].First());
            Assert.Empty(results);
        }

        [Fact]
        public async Task TestGetFoldingRangeAsync_Imports()
        {
            var markup =
@"using {|foldingRange:System;
using System.Linq;|}";
            var (solution, locations) = CreateTestSolution(markup);
            var expected = locations["foldingRange"]
                .Select(location => CreateFoldingRange(FoldingRangeKind.Imports, location.Range))
                .ToImmutableArray();

            var results = await RunGetFoldingRangeAsync(solution);
            AssertCollection(expected, results, AssertFoldingRangeEquals);
        }

        [Fact(Skip = "GetFoldingRangeAsync does not yet support comments.")]
        public async Task TestGetFoldingRangeAsync_Comments()
        {
            var markup =
@"{|foldingRange:// A comment|}
{|foldingRange:/* A multiline
comment */|}";
            var (solution, locations) = CreateTestSolution(markup);
            var importLocation = locations["foldingRange"].First();
            var expected = locations["foldingRange"]
                .Select(location => CreateFoldingRange(FoldingRangeKind.Comment, location.Range))
                .ToImmutableArray();

            var results = await RunGetFoldingRangeAsync(solution);
            AssertCollection(expected, results, AssertFoldingRangeEquals);
        }

        [Fact(Skip = "GetFoldingRangeAsync does not yet support regions.")]
        public async Task TestGetFoldingRangeAsync_Regions()
        {
            var markup =
@"{|foldingRange:#region ARegion
#endregion|}
}";
            var (solution, locations) = CreateTestSolution(markup);
            var importLocation = locations["foldingRange"].First();
            var expected = locations["foldingRange"]
                .Select(location => CreateFoldingRange(FoldingRangeKind.Region, location.Range))
                .ToImmutableArray();

            var results = await RunGetFoldingRangeAsync(solution);
            AssertCollection(expected, results, AssertFoldingRangeEquals);
        }

        /// <summary>
        /// Assert that two highligh lists are equivalent.
        /// Highlights are not returned in a consistent order, so they must be sorted.
        /// </summary>
        private static void AssertDocumentHighlights(IEnumerable<DocumentHighlight> expectedHighlights, IEnumerable<DocumentHighlight> actualHighlights)
        {
            AssertCollection(expectedHighlights, actualHighlights.Select(highlight => (object)highlight), AssertDocumentHighlightEquals, CompareHighlights);

            // local functions
            static int CompareHighlights(DocumentHighlight h1, DocumentHighlight h2)
            {
                var compareKind = h1.Kind.CompareTo(h2.Kind);
                var compareRange = CompareRange(h1.Range, h2.Range);
                return compareKind != 0 ? compareKind : compareRange;
            }
        }

        /// <summary>
        /// Assert that two location lists are equivalent.
        /// Locations are not returned in a consistent order, so they must be sorted.
        /// </summary>
        private static void AssertLocations(IEnumerable<VSLocation> expectedLocations, IEnumerable<VSLocation> actualLocations)
        {
            AssertCollection(expectedLocations, actualLocations.Select(loc => (object)loc), Assert.Equal, CompareLocations);

            // local functions
            static int CompareLocations(VSLocation l1, VSLocation l2)
            {
                var compareDocument = l1.Uri.OriginalString.CompareTo(l2.Uri.OriginalString);
                var compareRange = CompareRange(l1.Range, l2.Range);
                return compareDocument != 0 ? compareDocument : compareRange;
            }
        }

        private static void AssertCollection<T>(IEnumerable<T> expected, IEnumerable<object> actual, Action<T, T> assertionFunction, Func<T, T, int> compareFunc = null)
        {
            Assert.Equal(expected.Count(), actual.Count());
            var actualWithType = actual.Select(actualObject => (T)actualObject);

            var expectedResult = compareFunc != null ? expected.OrderBy((T t1, T t2) => compareFunc(t1, t2)).ToList() : expected.ToList();
            var actualResult = compareFunc != null ? actualWithType.OrderBy((T t1, T t2) => compareFunc(t1, t2)).ToList() : actualWithType.ToList();
            for (var i = 0; i < actualResult.Count; i++)
            {
                assertionFunction(expectedResult[i], actualResult[i]);
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
            Assert.Equal(expected.Location, actual.Location);
        }

        private static void AssertDocumentHighlightEquals(DocumentHighlight expected, DocumentHighlight actual)
        {
            Assert.Equal(expected.Kind, actual.Kind);
            Assert.Equal(expected.Range, actual.Range);
        }

        private static void AssertFoldingRangeEquals(FoldingRange expected, FoldingRange actual)
        {
            Assert.Equal(expected.Kind, actual.Kind);
            Assert.Equal(expected.StartCharacter, actual.StartCharacter);
            Assert.Equal(expected.EndCharacter, actual.EndCharacter);
            Assert.Equal(expected.StartLine, actual.StartLine);
            Assert.Equal(expected.EndLine, actual.EndLine);
        }

        private static int CompareRange(Range r1, Range r2)
        {
            var compareLine = r1.Start.Line.CompareTo(r2.Start.Line);
            var compareChar = r1.Start.Character.CompareTo(r2.Start.Character);
            return compareLine != 0 ? compareLine : compareChar;
        }


        private static SymbolInformation CreateSymbolInformation(VSSymbolKind kind, string name, VSLocation location)
            => new SymbolInformation()
            {
                Kind = kind,
                Name = name,
                Location = location
            };

        private static DocumentSymbol CreateDocumentSymbol(VSSymbolKind kind, string name, VSLocation location, DocumentSymbol parent = null)
        {
            var documentSymbol = new DocumentSymbol()
            {
                Kind = kind,
                Name = name,
                Range = location.Range,
                Children = new List<DocumentSymbol>()
            };

            if (parent != null)
            {
                parent.Children.Add(documentSymbol);
            }

            return documentSymbol;
        }

        private DocumentHighlight CreateDocumentHighlight(DocumentHighlightKind kind, VSLocation location)
            => new DocumentHighlight()
            {
                Kind = kind,
                Range = location.Range
            };

        private static FoldingRange CreateFoldingRange(string kind, Range range)
            => new FoldingRange()
            {
                Kind = kind,
                StartCharacter = range.Start.Character,
                EndCharacter = range.End.Character,
                StartLine = range.Start.Line,
                EndLine = range.End.Line
            };

        /// <summary>
        /// Creates a solution with a document.
        /// </summary>
        /// <param name="markup">the document text.</param>
        /// <returns>the solution and the annotated ranges in the document.</returns>
        private static (Solution solution, Dictionary<string, IList<VSLocation>> locations) CreateTestSolution(string markup)
            => CreateTestSolution(new string[] { markup });

        /// <summary>
        /// Create a solution with multiple documents.
        /// </summary>
        /// <param name="markups">the documents' text</param>
        /// <returns>
        /// the solution with the documents plus a list for each document of all annotated ranges in the document.
        /// </returns>
        private static (Solution solution, Dictionary<string, IList<VSLocation>> locations) CreateTestSolution(string[] markups)
        {
            using var workspace = TestWorkspace.CreateCSharp(markups);
            var solution = workspace.CurrentSolution;
            var locations = new Dictionary<string, IList<VSLocation>>();

            foreach (var document in workspace.Documents)
            {
                var text = document.TextBuffer.AsTextContainer().CurrentText;
                foreach (var kvp in document.AnnotatedSpans)
                {
                    locations.GetOrAdd(kvp.Key, CreateLocation)
                        .AddRange(kvp.Value.Select(s => s.ToRange(text).ToLocation(GetDocumentFilePathFromName(document.Name))));
                }

                // Pass in the text without markup.
                workspace.ChangeSolution(ChangeDocumentFilePathToValidURI(workspace.CurrentSolution, document, text));

            }

            return (workspace.CurrentSolution, locations);

            // local functions
            static List<VSLocation> CreateLocation(string s) => new List<VSLocation>();
        }

        private static async Task<object[]> RunGetDocumentSymbolsAsync(Solution solution, bool hierarchalSupport)
        {
            var document = solution.Projects.First().Documents.First();
            var request = new DocumentSymbolParams
            {
                TextDocument = CreateTextDocumentIdentifier(new Uri(document.FilePath))
            };

            return await CreateRoslynLanguageService(hierarchalSupport).GetDocumentSymbolsAsync(solution, request, CancellationToken.None);
        }

        private static async Task<SymbolInformation[]> RunGetWorkspaceSymbolsAsync(Solution solution, string query)
        {
            var request = new WorkspaceSymbolParams
            {
                Query = query
            };

            return await CreateRoslynLanguageService().GetWorkspaceSymbolsAsync(solution, request, CancellationToken.None);
        }

        private static async Task<Hover> RunGetHoverAsync(Solution solution, VSLocation caret)
            => await CreateRoslynLanguageService().GetHoverAsync(solution, CreateTextDocumentPositionParams(caret), CancellationToken.None);

        private static async Task<VSLocation[]> RunGotoDefinitionAsync(Solution solution, VSLocation caret)
            => await CreateRoslynLanguageService().GoToDefinitionAsync(solution, CreateTextDocumentPositionParams(caret), CancellationToken.None);

        private static async Task<VSLocation[]> RunGotoTypeDefinitionAsync(Solution solution, VSLocation caret)
            => await CreateRoslynLanguageService().GoToTypeDefinitionAsync(solution, CreateTextDocumentPositionParams(caret), CancellationToken.None);

        private static async Task<VSLocation[]> RunFindAllReferencesAsync(Solution solution, VSLocation caret, bool includeDeclaration)
        {
            var request = new ReferenceParams()
            {
                TextDocument = CreateTextDocumentIdentifier(caret.Uri),
                Position = caret.Range.Start,
                Context = new ReferenceContext()
                {
                    IncludeDeclaration = includeDeclaration
                }
            };

            return await CreateRoslynLanguageService().FindAllReferencesAsync(solution, request, CancellationToken.None);
        }

        private static async Task<VSLocation[]> RunGotoImplementationAsync(Solution solution, VSLocation caret)
            => await CreateRoslynLanguageService().GotoImplementationAsync(solution, CreateTextDocumentPositionParams(caret), CancellationToken.None);

        private static async Task<DocumentHighlight[]> RunGetDocumentHighlightAsync(Solution solution, VSLocation caret)
            => await CreateRoslynLanguageService().GetDocumentHighlightAsync(solution, CreateTextDocumentPositionParams(caret), CancellationToken.None);

        private static async Task<FoldingRange[]> RunGetFoldingRangeAsync(Solution solution)
        {
            var document = solution.Projects.First().Documents.First();
            var request = new FoldingRangeParams()
            {
                TextDocument = CreateTextDocumentIdentifier(new Uri(document.FilePath))
            };

            return await CreateRoslynLanguageService().GetFoldingRangeAsync(solution, request, CancellationToken.None);
        }

        private static TextDocumentPositionParams CreateTextDocumentPositionParams(VSLocation caret)
        {
            var request = new TextDocumentPositionParams
            {
                TextDocument = CreateTextDocumentIdentifier(caret.Uri),
                Position = caret.Range.Start
            };

            return request;
        }

        private static TextDocumentIdentifier CreateTextDocumentIdentifier(Uri uri)
            => new TextDocumentIdentifier()
            {
                Uri = uri
            };

        private static RoslynLanguageService CreateRoslynLanguageService(bool hierarchalSupport = true)
            => new RoslynLanguageService(new ClientCapabilities(), hierarchalSupport);

        private static String GetDocumentFilePathFromName(string documentName)
            => "C:\\" + documentName;

        /// <summary>
        /// Changes the document file path.
        /// Adds/Removes the document instead of updating file path due to
        /// https://github.com/dotnet/roslyn/issues/34837
        /// </summary>
        private static Solution ChangeDocumentFilePathToValidURI(Solution originalSolution, TestHostDocument originalDocument, SourceText text)
        {
            var documentName = originalDocument.Name;
            var documentPath = GetDocumentFilePathFromName(documentName);

            var solution = originalSolution.RemoveDocument(originalDocument.Id);

            var newDocumentId = DocumentId.CreateNewId(originalDocument.Project.Id);
            return solution.AddDocument(newDocumentId, documentName, text, filePath: documentPath);
        }
    }
}
