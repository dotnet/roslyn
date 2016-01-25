// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue.UnitTests
{
    public class CSharpEditAndContinueAnalyzerTests
    {
        #region Helpers

        private static void TestSpans(string source, Func<SyntaxKind, bool> hasLabel)
        {
            var tree = SyntaxFactory.ParseSyntaxTree(source);

            foreach (var expected in GetExpectedSpans(source))
            {
                string expectedText = source.Substring(expected.Start, expected.Length);
                SyntaxToken token = tree.GetRoot().FindToken(expected.Start);
                SyntaxNode node = token.Parent;
                while (!hasLabel(node.Kind()))
                {
                    node = node.Parent;
                }

                var actual = CSharpEditAndContinueAnalyzer.GetDiagnosticSpanImpl(node.Kind(), node, EditKind.Update);
                var actualText = source.Substring(actual.Start, actual.Length);

                Assert.True(expected == actual, "\r\n" +
                    "Expected span: '" + expectedText + "' " + expected + "\r\n" +
                    "Actual span: '" + actualText + "' " + actual);
            }
        }

        private static IEnumerable<TextSpan> GetExpectedSpans(string source)
        {
            const string StartTag = "/*<span>*/";
            const string EndTag = "/*</span>*/";
            int i = 0;

            while (true)
            {
                int start = source.IndexOf(StartTag, i, StringComparison.Ordinal);
                if (start == -1)
                {
                    break;
                }

                start += StartTag.Length;
                int end = source.IndexOf(EndTag, start + 1, StringComparison.Ordinal);
                yield return new TextSpan(start, end - start);
                i = end + 1;
            }
        }

        private static void TestErrorSpansAllKinds(Func<SyntaxKind, bool> hasLabel)
        {
            List<SyntaxKind> unhandledKinds = new List<SyntaxKind>();
            foreach (var kind in Enum.GetValues(typeof(SyntaxKind)).Cast<SyntaxKind>().Where(hasLabel))
            {
                try
                {
                    CSharpEditAndContinueAnalyzer.GetDiagnosticSpanImpl(kind, null, EditKind.Update);
                }
                catch (NullReferenceException)
                {
                    // expected, we passed null node
                }
                catch (Exception)
                {
                    // unexpected:
                    unhandledKinds.Add(kind);
                }
            }

            AssertEx.Equal(Array.Empty<SyntaxKind>(), unhandledKinds);
        }

        #endregion

        [Fact]
        public void ErrorSpans_TopLevel()
        {
            string source = @"
/*<span>*/extern alias A;/*</span>*/
/*<span>*/using Z = Foo.Bar;/*</span>*/

[assembly: /*<span>*/A(1,2,3,4)/*</span>*/, /*<span>*/B/*</span>*/]

/*<span>*/namespace N.M/*</span>*/ { }

[A, B]
/*<span>*/struct S<[A]T>/*</span>*/ : B 
    /*<span>*/where T : new, struct/*</span>*/ { }

[A, B]
/*<span>*/public abstract partial class C/*</span>*/ { }

/*<span>*/interface I/*</span>*/ : J, K, L { }

[A]
/*<span>*/enum E1/*</span>*/ { }

/*<span>*/enum E2/*</span>*/ : uint { }

/*<span>*/public enum E3/*</span>*/
{ 
    Q,
    [A]R = 3
}

[A]
/*<span>*/public delegate void D1<T>()/*</span>*/ where T : struct;

/*<span>*/delegate C<T> D2()/*</span>*/;

[/*<span>*/Attrib/*</span>*/]
/*<span>*/[Attrib]/*</span>*/
/*<span>*/public class Z/*</span>*/
{
    /*<span>*/int f/*</span>*/;
    [A]/*<span>*/int f = 1/*</span>*/;
    /*<span>*/public static readonly int f/*</span>*/;

    /*<span>*/int M1()/*</span>*/ { }
    [A]/*<span>*/int M2()/*</span>*/ { }
    [A]/*<span>*/int M3<T1, T2>()/*</span>*/ where T1 : bar where T2 : baz { }

    [A]/*<span>*/abstract C<T> M4()/*</span>*/;
    int M5([A]/*<span>*/Z d = 2345/*</span>*/, /*<span>*/ref int x/*</span>*/, /*<span>*/params int[] x/*</span>*/) { return 1; }

    [A]/*<span>*/event A E1/*</span>*/;
    [A]/*<span>*/public event A E2/*</span>*/;

    [A]/*<span>*/public abstract event A E3/*</span>*/ { /*<span>*/add/*</span>*/; /*<span>*/remove/*</span>*/; }
    [A]/*<span>*/public abstract event A E4/*</span>*/ { [A, B]/*<span>*/add/*</span>*/ { } [A]/*<span>*/internal remove/*</span>*/ { } }

    [A]/*<span>*/int P/*</span>*/ { get; set; }
    [A]/*<span>*/internal string P/*</span>*/ { /*<span>*/internal get/*</span>*/ { } [A]/*<span>*/set/*</span>*/ { }}
    
    [A]/*<span>*/internal string this[int a, int b]/*</span>*/ { /*<span>*/get/*</span>*/ { } /*<span>*/set/*</span>*/ { } }
    [A]/*<span>*/string this[[A]int a = 123]/*</span>*/ { get { } set { } }

    [A]/*<span>*/public static explicit operator int(Z d)/*</span>*/ { return 1; }
    [A]/*<span>*/operator double(Z d)/*</span>*/ { return 1; }

    [A]/*<span>*/public static operator int +(Z d, int x)/*</span>*/ { return 1; }
    [A]/*<span>*/operator int +(Z d, int x)/*</span>*/ { return 1; }
    
}
";
            TestSpans(source, kind => TopSyntaxComparer.HasLabel(kind, ignoreVariableDeclarations: false));
        }

        [Fact]
        public void ErrorSpans_StatementLevel_Update()
        {
            string source = @"
class C
{
    void M()
    {
        /*<span>*/{/*</span>*/}
        /*<span>*/using (expr)/*</span>*/ {}
        /*<span>*/fixed (int* a = expr)/*</span>*/ {}
        /*<span>*/lock (expr)/*</span>*/ {}
        /*<span>*/yield break;/*</span>*/
        /*<span>*/yield return 1;/*</span>*/
        /*<span>*/try/*</span>*/ {} catch { };
        try {} /*<span>*/catch/*</span>*/ { };
        try {} /*<span>*/finally/*</span>*/ { };
        /*<span>*/if (expr)/*</span>*/ { };
        if (expr) { } /*<span>*/else/*</span>*/ { };
        /*<span>*/while (expr)/*</span>*/ { };
        /*<span>*/do/*</span>*/ {} while (expr);
        /*<span>*/for (;;)/*</span>*/ { };
        /*<span>*/foreach (var a in b)/*</span>*/ { };
        /*<span>*/switch (expr)/*</span>*/ { case 1: break; };
        switch (expr) { case 1: /*<span>*/goto case 1;/*</span>*/ };
        switch (expr) { case 1: /*<span>*/goto case default;/*</span>*/ };
        /*<span>*/label/*</span>*/: Foo();
        /*<span>*/checked/*</span>*/ { };
        /*<span>*/unchecked/*</span>*/ { };
        /*<span>*/unsafe/*</span>*/ { };
        /*<span>*/return expr;/*</span>*/
        /*<span>*/throw expr;/*</span>*/
        /*<span>*/break;/*</span>*/
        /*<span>*/continue;/*</span>*/
        /*<span>*/goto label;/*</span>*/
        /*<span>*/expr;/*</span>*/
        /*<span>*/int a;/*</span>*/
        F(/*<span>*/(x)/*</span>*/ => x);
        F(/*<span>*/x/*</span>*/ => x);
        F(/*<span>*/delegate/*</span>*/(x) { });
        F(from a in b /*<span>*/select/*</span>*/ a.x);
        F(from a in b /*<span>*/let/*</span>*/ x = expr select expr);
        F(from a in b /*<span>*/where/*</span>*/ expr select expr);
        F(from a in b /*<span>*/join/*</span>*/ c in d on e equals f select g);
        F(from a in b orderby /*<span>*/a/*</span>*/ select b);
        F(from a in b orderby a, /*<span>*/b descending/*</span>*/ select b);
        F(from a in b /*<span>*/group/*</span>*/ a by b select d);
    }
}
";
            // TODO: test
            // /*<span>*/F($$from a in b from c in d select a.x);/*</span>*/
            // /*<span>*/F(from a in b $$from c in d select a.x);/*</span>*/
            TestSpans(source, StatementSyntaxComparer.IgnoreLabeledChild);
        }

        /// <summary>
        /// Verifies that <see cref="CSharpEditAndContinueAnalyzer.GetDiagnosticSpanImpl"/> handles all <see cref="SyntaxKind"/>s.
        /// </summary>
        [Fact]
        public void ErrorSpansAllKinds()
        {
            TestErrorSpansAllKinds(StatementSyntaxComparer.IgnoreLabeledChild);
            TestErrorSpansAllKinds(kind => TopSyntaxComparer.HasLabel(kind, ignoreVariableDeclarations: false));
        }

        [Fact]
        public async Task AnalyzeDocumentAsync_InsignificantChangesInMethodBody()
        {
            string source1 = @"
class C
{
    public static void Main()
    {
        // comment
        System.Console.WriteLine(1);
    }
}
";
            string source2 = @"
class C
{
    public static void Main()
    {
        System.Console.WriteLine(1);
    }
}
";
            var analyzer = new CSharpEditAndContinueAnalyzer();

            using (var workspace = await TestWorkspace.CreateCSharpAsync(source1))
            {
                var documentId = workspace.CurrentSolution.Projects.First().Documents.First().Id;
                var oldSolution = workspace.CurrentSolution;
                var newSolution = workspace.CurrentSolution.WithDocumentText(documentId, SourceText.From(source2));
                var oldDocument = oldSolution.GetDocument(documentId);
                var oldText = await oldDocument.GetTextAsync();
                var oldSyntaxRoot = await oldDocument.GetSyntaxRootAsync();
                var newDocument = newSolution.GetDocument(documentId);
                var newText = await newDocument.GetTextAsync();
                var newSyntaxRoot = await newDocument.GetSyntaxRootAsync();

                const string oldStatementSource = "System.Console.WriteLine(1);";
                var oldStatementPosition = source1.IndexOf(oldStatementSource, StringComparison.Ordinal);
                var oldStatementTextSpan = new TextSpan(oldStatementPosition, oldStatementSource.Length);
                var oldStatementSpan = oldText.Lines.GetLinePositionSpan(oldStatementTextSpan);
                var oldStatementSyntax = oldSyntaxRoot.FindNode(oldStatementTextSpan);

                var baseActiveStatements = ImmutableArray.Create(new ActiveStatementSpan(ActiveStatementFlags.LeafFrame, oldStatementSpan));
                var result = await analyzer.AnalyzeDocumentAsync(oldSolution, baseActiveStatements, newDocument, default(CancellationToken));

                Assert.True(result.HasChanges);
                Assert.True(result.SemanticEdits[0].PreserveLocalVariables);
                var syntaxMap = result.SemanticEdits[0].SyntaxMap;

                var newStatementSpan = result.ActiveStatements[0];
                var newStatementTextSpan = newText.Lines.GetTextSpan(newStatementSpan);
                var newStatementSyntax = newSyntaxRoot.FindNode(newStatementTextSpan);

                var oldStatementSyntaxMapped = syntaxMap(newStatementSyntax);
                Assert.Same(oldStatementSyntax, oldStatementSyntaxMapped);
            }
        }

        [Fact]
        public async Task AnalyzeDocumentAsync_SyntaxError_Change()
        {
            string source1 = @"
class C
{
    public static void Main()
    {
        System.Console.WriteLine(1) // syntax error
    }
}
";
            string source2 = @"
class C
{
    public static void Main()
    {
        System.Console.WriteLine(2) // syntax error
    }
}
";
            var analyzer = new CSharpEditAndContinueAnalyzer();

            using (var workspace = await TestWorkspace.CreateCSharpAsync(source1))
            {
                var documentId = workspace.CurrentSolution.Projects.First().Documents.First().Id;
                var oldSolution = workspace.CurrentSolution;
                var newSolution = workspace.CurrentSolution.WithDocumentText(documentId, SourceText.From(source2));

                var baseActiveStatements = ImmutableArray.Create<ActiveStatementSpan>();
                var result = await analyzer.AnalyzeDocumentAsync(oldSolution, baseActiveStatements, newSolution.GetDocument(documentId), default(CancellationToken));

                Assert.True(result.HasChanges);
                Assert.True(result.HasChangesAndErrors);
                Assert.True(result.HasChangesAndCompilationErrors);
            }
        }

        [Fact]
        public async Task AnalyzeDocumentAsync_SyntaxError_NoChange()
        {
            string source = @"
class C
{
    public static void Main()
    {
        System.Console.WriteLine(1) // syntax error
    }
}
";
            var analyzer = new CSharpEditAndContinueAnalyzer();

            using (var workspace = await TestWorkspace.CreateCSharpAsync(source))
            {
                var document = workspace.CurrentSolution.Projects.First().Documents.First();
                var baseActiveStatements = ImmutableArray.Create<ActiveStatementSpan>();
                var result = await analyzer.AnalyzeDocumentAsync(workspace.CurrentSolution, baseActiveStatements, document, default(CancellationToken));

                Assert.False(result.HasChanges);
                Assert.False(result.HasChangesAndErrors);
                Assert.False(result.HasChangesAndCompilationErrors);
            }
        }

        [Fact]
        public async Task AnalyzeDocumentAsync_SyntaxError_NoChange2()
        {
            string source1 = @"
class C
{
    public static void Main()
    {
        System.Console.WriteLine(1) // syntax error
    }
}
";
            string source2 = @"
class C
{
    public static void Main()
    {
        System.Console.WriteLine(1) // syntax error
    }
}
";
            var analyzer = new CSharpEditAndContinueAnalyzer();

            using (var workspace = await TestWorkspace.CreateCSharpAsync(source1))
            {
                var documentId = workspace.CurrentSolution.Projects.First().Documents.First().Id;
                var oldSolution = workspace.CurrentSolution;
                var newSolution = workspace.CurrentSolution.WithDocumentText(documentId, SourceText.From(source2));

                var baseActiveStatements = ImmutableArray.Create<ActiveStatementSpan>();
                var result = await analyzer.AnalyzeDocumentAsync(oldSolution, baseActiveStatements, newSolution.GetDocument(documentId), default(CancellationToken));

                Assert.False(result.HasChanges);
                Assert.False(result.HasChangesAndErrors);
                Assert.False(result.HasChangesAndCompilationErrors);
            }
        }

        [Fact]
        public async Task AnalyzeDocumentAsync_Features_NoChange()
        {
            string source = @"
class C
{
    public static void Main()
    {
        System.Console.WriteLine(1);
    }
}
";
            var analyzer = new CSharpEditAndContinueAnalyzer();
            var experimentalFeatures = new Dictionary<string, string>(); // no experimental features to enable
            var experimental = TestOptions.Regular.WithFeatures(experimentalFeatures);

            using (var workspace = await TestWorkspace.CreateCSharpAsync(
                source, parseOptions: experimental, compilationOptions: null, exportProvider: null))
            {
                var document = workspace.CurrentSolution.Projects.First().Documents.First();
                var baseActiveStatements = ImmutableArray.Create<ActiveStatementSpan>();
                var result = await analyzer.AnalyzeDocumentAsync(workspace.CurrentSolution, baseActiveStatements, document, default(CancellationToken));

                Assert.False(result.HasChanges);
                Assert.False(result.HasChangesAndErrors);
                Assert.False(result.HasChangesAndCompilationErrors);
                Assert.True(result.RudeEditErrors.IsDefaultOrEmpty);
            }
        }

        [Fact]
        public async Task AnalyzeDocumentAsync_Features_Change()
        {
            // these are all the experimental features currently implemented
            string[] experimentalFeatures = Array.Empty<string>();

            foreach (var feature in experimentalFeatures)
            {
                string source1 = @"
class C
{
    public static void Main()
    {
        System.Console.WriteLine(1);
    }
}
";
                string source2 = @"
class C
{
    public static void Main()
    {
        System.Console.WriteLine(2);
    }
}
";
                var analyzer = new CSharpEditAndContinueAnalyzer();

                var featuresToEnable = new Dictionary<string, string>() { { feature, "enabled" } };
                var experimental = TestOptions.Regular.WithFeatures(featuresToEnable);

                using (var workspace = await TestWorkspace.CreateCSharpAsync(
                    source1, parseOptions: experimental, compilationOptions: null, exportProvider: null))
                {
                    var documentId = workspace.CurrentSolution.Projects.First().Documents.First().Id;
                    var oldSolution = workspace.CurrentSolution;
                    var newSolution = workspace.CurrentSolution.WithDocumentText(documentId, SourceText.From(source2));

                    var baseActiveStatements = ImmutableArray.Create<ActiveStatementSpan>();
                    var result = await analyzer.AnalyzeDocumentAsync(oldSolution, baseActiveStatements, newSolution.GetDocument(documentId), default(CancellationToken));

                    Assert.True(result.HasChanges);
                    Assert.True(result.HasChangesAndErrors);
                    Assert.False(result.HasChangesAndCompilationErrors);
                    Assert.Equal(RudeEditKind.ExperimentalFeaturesEnabled, result.RudeEditErrors.Single().Kind);
                }
            }
        }

        [Fact]
        public async Task AnalyzeDocumentAsync_SemanticError_NoChange()
        {
            string source = @"
class C
{
    public static void Main()
    {
        System.Console.WriteLine(1);
        Bar(); // semantic error
    }
}
";
            var analyzer = new CSharpEditAndContinueAnalyzer();

            using (var workspace = await TestWorkspace.CreateCSharpAsync(source))
            {
                var document = workspace.CurrentSolution.Projects.First().Documents.First();
                var baseActiveStatements = ImmutableArray.Create<ActiveStatementSpan>();
                var result = await analyzer.AnalyzeDocumentAsync(workspace.CurrentSolution, baseActiveStatements, document, default(CancellationToken));

                Assert.False(result.HasChanges);
                Assert.False(result.HasChangesAndErrors);
                Assert.False(result.HasChangesAndCompilationErrors);
            }
        }

        [Fact]
        public async Task AnalyzeDocumentAsync_SemanticError_Change()
        {
            string source1 = @"
class C
{
    public static void Main()
    {
        System.Console.WriteLine(1);
        Bar(); // semantic error
    }
}
";
            string source2 = @"
class C
{
    public static void Main()
    {
        System.Console.WriteLine(2);
        Bar(); // semantic error
    }
}
";
            var analyzer = new CSharpEditAndContinueAnalyzer();

            using (var workspace = await TestWorkspace.CreateCSharpAsync(source1))
            {
                var documentId = workspace.CurrentSolution.Projects.First().Documents.First().Id;
                var oldSolution = workspace.CurrentSolution;
                var newSolution = workspace.CurrentSolution.WithDocumentText(documentId, SourceText.From(source2));

                var baseActiveStatements = ImmutableArray.Create<ActiveStatementSpan>();
                var result = await analyzer.AnalyzeDocumentAsync(oldSolution, baseActiveStatements, newSolution.GetDocument(documentId), default(CancellationToken));

                Assert.True(result.HasChanges);
                Assert.True(result.HasChangesAndErrors);
                Assert.True(result.HasChangesAndCompilationErrors);
            }
        }

        [Fact]
        public async Task AnalyzeDocumentAsync_AddingNewFileHavingRudeEdits()
        {
            string source1 = @"
namespace N
{
    class C
    {
        public static void Main()
        {
        }
    }
}
";
            string source2 = @"
namespace N
{
    public class D
    {
    }
}
";
            var analyzer = new CSharpEditAndContinueAnalyzer();

            using (var workspace = await TestWorkspace.CreateCSharpAsync(source1))
            {
                // fork the solution to introduce a change
                var project = workspace.CurrentSolution.Projects.Single();
                var newDocId = DocumentId.CreateNewId(project.Id);
                var oldSolution = workspace.CurrentSolution;
                var newSolution = oldSolution.AddDocument(newDocId, "foo.cs", SourceText.From(source2));

                workspace.TryApplyChanges(newSolution);

                var newProject = newSolution.Projects.Single();
                var changes = newProject.GetChanges(project);

                Assert.Equal(2, newProject.Documents.Count());
                Assert.Equal(0, changes.GetChangedDocuments().Count());
                Assert.Equal(1, changes.GetAddedDocuments().Count());

                var changedDocuments = changes.GetChangedDocuments().Concat(changes.GetAddedDocuments());

                var result = new List<DocumentAnalysisResults>();
                var baseActiveStatements = ImmutableArray.Create<ActiveStatementSpan>();
                foreach (var changedDocumentId in changedDocuments)
                {
                    result.Add(await analyzer.AnalyzeDocumentAsync(oldSolution, baseActiveStatements, newProject.GetDocument(changedDocumentId), default(CancellationToken)));
                }

                Assert.True(result.IsSingle());
                Assert.Equal(1, result.Single().RudeEditErrors.Count());
                Assert.Equal(RudeEditKind.Insert, result.Single().RudeEditErrors.Single().Kind);
            }
        }

        [Fact]
        public async Task AnalyzeDocumentAsync_AddingNewFile()
        {
            string source1 = @"
namespace N
{
    class C
    {
        public static void Main()
        {
        }
    }
}
";
            string source2 = @"
";
            var analyzer = new CSharpEditAndContinueAnalyzer();

            using (var workspace = await TestWorkspace.CreateCSharpAsync(source1))
            {
                // fork the solution to introduce a change
                var project = workspace.CurrentSolution.Projects.Single();
                var newDocId = DocumentId.CreateNewId(project.Id);
                var oldSolution = workspace.CurrentSolution;
                var newSolution = oldSolution.AddDocument(newDocId, "foo.cs", SourceText.From(source2));

                workspace.TryApplyChanges(newSolution);

                var newProject = newSolution.Projects.Single();
                var changes = newProject.GetChanges(project);

                Assert.Equal(2, newProject.Documents.Count());
                Assert.Equal(0, changes.GetChangedDocuments().Count());
                Assert.Equal(1, changes.GetAddedDocuments().Count());

                var changedDocuments = changes.GetChangedDocuments().Concat(changes.GetAddedDocuments());

                var result = new List<DocumentAnalysisResults>();
                var baseActiveStatements = ImmutableArray.Create<ActiveStatementSpan>();
                foreach (var changedDocumentId in changedDocuments)
                {
                    result.Add(await analyzer.AnalyzeDocumentAsync(oldSolution, baseActiveStatements, newProject.GetDocument(changedDocumentId), default(CancellationToken)));
                }

                Assert.True(result.IsSingle());
                Assert.Equal(1, result.Single().RudeEditErrors.Count());
                Assert.Equal(RudeEditKind.InsertFile, result.Single().RudeEditErrors.Single().Kind);
            }
        }
    }
}
