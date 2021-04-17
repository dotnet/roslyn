// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.EditAndContinue.UnitTests;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue.UnitTests
{
    [UseExportProvider]
    public class CSharpEditAndContinueAnalyzerTests
    {
        private static readonly TestComposition s_composition = FeaturesTestCompositions.Features;

        #region Helpers

        private static void TestSpans(string source, Func<SyntaxKind, bool> hasLabel)
        {
            var tree = SyntaxFactory.ParseSyntaxTree(source);

            foreach (var expected in GetExpectedSpans(source))
            {
                var expectedText = source.Substring(expected.Start, expected.Length);
                var token = tree.GetRoot().FindToken(expected.Start);
                var node = token.Parent;
                while (!hasLabel(node.Kind()))
                {
                    node = node.Parent;
                }

                var actual = CSharpEditAndContinueAnalyzer.GetDiagnosticSpan(node, EditKind.Update);
                var actualText = source.Substring(actual.Start, actual.Length);

                Assert.True(expected == actual,
                    $"{Environment.NewLine}Expected span: '{expectedText}' {expected}" +
                    $"{Environment.NewLine}Actual span: '{actualText}' {actual}");
            }
        }

        private static IEnumerable<TextSpan> GetExpectedSpans(string source)
        {
            const string StartTag = "/*<span>*/";
            const string EndTag = "/*</span>*/";
            var i = 0;

            while (true)
            {
                var start = source.IndexOf(StartTag, i, StringComparison.Ordinal);
                if (start == -1)
                {
                    break;
                }

                start += StartTag.Length;
                var end = source.IndexOf(EndTag, start + 1, StringComparison.Ordinal);
                yield return new TextSpan(start, end - start);
                i = end + 1;
            }
        }

        private static void TestErrorSpansAllKinds(Func<SyntaxKind, bool> hasLabel)
        {
            var unhandledKinds = new List<SyntaxKind>();
            foreach (var kind in Enum.GetValues(typeof(SyntaxKind)).Cast<SyntaxKind>().Where(hasLabel))
            {
                TextSpan? span;
                try
                {
                    span = CSharpEditAndContinueAnalyzer.TryGetDiagnosticSpanImpl(kind, null, EditKind.Update);
                }
                catch (NullReferenceException)
                {
                    // expected, we passed null node
                    continue;
                }

                // unexpected:
                if (span == null)
                {
                    unhandledKinds.Add(kind);
                }
            }

            AssertEx.Equal(Array.Empty<SyntaxKind>(), unhandledKinds);
        }

        #endregion

        [Fact]
        public void ErrorSpans_TopLevel()
        {
            var source = @"
/*<span>*/extern alias A;/*</span>*/
/*<span>*/using Z = Goo.Bar;/*</span>*/

[assembly: /*<span>*/A(1,2,3,4)/*</span>*/, /*<span>*/B/*</span>*/]

/*<span>*/namespace N.M/*</span>*/ { }

[A, B]
/*<span>*/struct S<[A]T>/*</span>*/ : B 
    /*<span>*/where T : new, struct/*</span>*/ { }

[A, B]
/*<span>*/public abstract partial class C/*</span>*/ { }

[A, B]
/*<span>*/public abstract partial record R/*</span>*/ { }

[A, B]
/*<span>*/public abstract partial record struct R/*</span>*/ { }

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

    [A]/*<span>*/public static operator +(Z d, int x)/*</span>*/ { return 1; }
    [A]/*<span>*/operator +(Z d, int x)/*</span>*/ { return 1; }
    
}
";
            TestSpans(source, kind => SyntaxComparer.TopLevel.HasLabel(kind));
        }

        [Fact]
        public void ErrorSpans_StatementLevel_Update()
        {
            var source = @"
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
        /*<span>*/label/*</span>*/: Goo();
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
            TestSpans(source, kind => SyntaxComparer.Statement.HasLabel(kind));
        }

        /// <summary>
        /// Verifies that <see cref="CSharpEditAndContinueAnalyzer.TryGetDiagnosticSpanImpl"/> handles all <see cref="SyntaxKind"/>s.
        /// </summary>
        [Fact]
        public void ErrorSpansAllKinds()
        {
            TestErrorSpansAllKinds(kind => SyntaxComparer.Statement.HasLabel(kind));
            TestErrorSpansAllKinds(kind => SyntaxComparer.TopLevel.HasLabel(kind));
        }

        [Fact]
        public async Task AnalyzeDocumentAsync_InsignificantChangesInMethodBody()
        {
            var source1 = @"
class C
{
    public static void Main()
    {
        // comment
        System.Console.WriteLine(1);
    }
}
";
            var source2 = @"
class C
{
    public static void Main()
    {
        System.Console.WriteLine(1);
    }
}
";

            using var workspace = TestWorkspace.CreateCSharp(source1, composition: s_composition);
            var oldSolution = workspace.CurrentSolution;
            var oldProject = oldSolution.Projects.Single();
            var oldDocument = oldProject.Documents.Single();
            var oldText = await oldDocument.GetTextAsync();
            var oldSyntaxRoot = await oldDocument.GetSyntaxRootAsync();
            var documentId = oldDocument.Id;
            var newSolution = workspace.CurrentSolution.WithDocumentText(documentId, SourceText.From(source2));
            var newDocument = newSolution.GetDocument(documentId);
            var newText = await newDocument.GetTextAsync();
            var newSyntaxRoot = await newDocument.GetSyntaxRootAsync();

            var oldStatementSource = "System.Console.WriteLine(1);";
            var oldStatementPosition = source1.IndexOf(oldStatementSource, StringComparison.Ordinal);
            var oldStatementTextSpan = new TextSpan(oldStatementPosition, oldStatementSource.Length);
            var oldStatementSpan = oldText.Lines.GetLinePositionSpan(oldStatementTextSpan);
            var oldStatementSyntax = oldSyntaxRoot.FindNode(oldStatementTextSpan);

            var baseActiveStatements = new ActiveStatementsMap(
                ImmutableDictionary.CreateRange(new[]
                {
                    KeyValuePairUtil.Create(newDocument.FilePath, ImmutableArray.Create(
                        new ActiveStatement(
                            ordinal: 0,
                            documentOrdinal: 0,
                            ActiveStatementFlags.IsLeafFrame,
                            new SourceFileSpan(newDocument.FilePath, oldStatementSpan),
                            instructionId: default)))
                }),
                ActiveStatementsMap.Empty.InstructionMap);

            var analyzer = new CSharpEditAndContinueAnalyzer();

            var result = await analyzer.AnalyzeDocumentAsync(oldProject, baseActiveStatements, newDocument, ImmutableArray<LinePositionSpan>.Empty, EditAndContinueTestHelpers.Net5RuntimeCapabilities, CancellationToken.None);

            Assert.True(result.HasChanges);
            var syntaxMap = result.SemanticEdits[0].SyntaxMap;
            Assert.NotNull(syntaxMap);

            var newStatementSpan = result.ActiveStatements[0].Span;
            var newStatementTextSpan = newText.Lines.GetTextSpan(newStatementSpan);
            var newStatementSyntax = newSyntaxRoot.FindNode(newStatementTextSpan);

            var oldStatementSyntaxMapped = syntaxMap(newStatementSyntax);
            Assert.Same(oldStatementSyntax, oldStatementSyntaxMapped);
        }

        [Fact]
        public async Task AnalyzeDocumentAsync_SyntaxError_Change()
        {
            var source1 = @"
class C
{
    public static void Main()
    {
        System.Console.WriteLine(1) // syntax error
    }
}
";
            var source2 = @"
class C
{
    public static void Main()
    {
        System.Console.WriteLine(2) // syntax error
    }
}
";

            using var workspace = TestWorkspace.CreateCSharp(source1, composition: s_composition);
            var oldSolution = workspace.CurrentSolution;
            var oldProject = oldSolution.Projects.Single();
            var oldDocument = oldProject.Documents.Single();
            var documentId = oldDocument.Id;
            var newSolution = workspace.CurrentSolution.WithDocumentText(documentId, SourceText.From(source2));

            var baseActiveStatements = ActiveStatementsMap.Empty;
            var analyzer = new CSharpEditAndContinueAnalyzer();

            var result = await analyzer.AnalyzeDocumentAsync(oldProject, baseActiveStatements, newSolution.GetDocument(documentId), ImmutableArray<LinePositionSpan>.Empty, EditAndContinueTestHelpers.Net5RuntimeCapabilities, CancellationToken.None);

            Assert.True(result.HasChanges);
            Assert.True(result.HasChangesAndErrors);
            Assert.True(result.HasChangesAndSyntaxErrors);
        }

        [Fact]
        public async Task AnalyzeDocumentAsync_SyntaxError_NoChange()
        {
            var source = @"
class C
{
    public static void Main()
    {
        System.Console.WriteLine(1) // syntax error
    }
}
";

            using var workspace = TestWorkspace.CreateCSharp(source, composition: s_composition);
            var oldProject = workspace.CurrentSolution.Projects.Single();
            var oldDocument = oldProject.Documents.Single();
            var baseActiveStatements = ActiveStatementsMap.Empty;
            var analyzer = new CSharpEditAndContinueAnalyzer();

            var result = await analyzer.AnalyzeDocumentAsync(oldProject, baseActiveStatements, oldDocument, ImmutableArray<LinePositionSpan>.Empty, EditAndContinueTestHelpers.Net5RuntimeCapabilities, CancellationToken.None);

            Assert.False(result.HasChanges);
            Assert.False(result.HasChangesAndErrors);
            Assert.False(result.HasChangesAndSyntaxErrors);
        }

        [Fact]
        public async Task AnalyzeDocumentAsync_SyntaxError_NoChange2()
        {
            var source1 = @"
class C
{
    public static void Main()
    {
        System.Console.WriteLine(1) // syntax error
    }
}
";
            var source2 = @"
class C
{
    public static void Main()
    {
        System.Console.WriteLine(1) // syntax error
    }
}
";

            using var workspace = TestWorkspace.CreateCSharp(source1, composition: s_composition);

            var oldSolution = workspace.CurrentSolution;
            var oldProject = oldSolution.Projects.Single();
            var oldDocument = oldProject.Documents.Single();
            var documentId = oldDocument.Id;

            var newSolution = workspace.CurrentSolution.WithDocumentText(documentId, SourceText.From(source2));

            var baseActiveStatements = ActiveStatementsMap.Empty;
            var analyzer = new CSharpEditAndContinueAnalyzer();

            var result = await analyzer.AnalyzeDocumentAsync(oldProject, baseActiveStatements, newSolution.GetDocument(documentId), ImmutableArray<LinePositionSpan>.Empty, EditAndContinueTestHelpers.Net5RuntimeCapabilities, CancellationToken.None);

            Assert.False(result.HasChanges);
            Assert.False(result.HasChangesAndErrors);
            Assert.False(result.HasChangesAndSyntaxErrors);
        }

        [Fact]
        public async Task AnalyzeDocumentAsync_Features_NoChange()
        {
            var source = @"
class C
{
    public static void Main()
    {
        System.Console.WriteLine(1);
    }
}
";
            var experimentalFeatures = new Dictionary<string, string>(); // no experimental features to enable
            var experimental = TestOptions.Regular.WithFeatures(experimentalFeatures);

            using var workspace = TestWorkspace.CreateCSharp(
                source, parseOptions: experimental, compilationOptions: null, composition: s_composition);

            var oldSolution = workspace.CurrentSolution;
            var oldProject = oldSolution.Projects.Single();
            var oldDocument = oldProject.Documents.Single();
            var documentId = oldDocument.Id;

            var baseActiveStatements = ActiveStatementsMap.Empty;
            var analyzer = new CSharpEditAndContinueAnalyzer();

            var result = await analyzer.AnalyzeDocumentAsync(oldProject, baseActiveStatements, oldDocument, ImmutableArray<LinePositionSpan>.Empty, EditAndContinueTestHelpers.Net5RuntimeCapabilities, CancellationToken.None);

            Assert.False(result.HasChanges);
            Assert.False(result.HasChangesAndErrors);
            Assert.False(result.HasChangesAndSyntaxErrors);
            Assert.True(result.RudeEditErrors.IsEmpty);
        }

        [Fact]
        public async Task AnalyzeDocumentAsync_Features_Change()
        {
            // these are all the experimental features currently implemented
            var experimentalFeatures = Array.Empty<string>();

            foreach (var feature in experimentalFeatures)
            {
                var source1 = @"
class C
{
    public static void Main()
    {
        System.Console.WriteLine(1);
    }
}
";
                var source2 = @"
class C
{
    public static void Main()
    {
        System.Console.WriteLine(2);
    }
}
";

                var featuresToEnable = new Dictionary<string, string>() { { feature, "enabled" } };
                var experimental = TestOptions.Regular.WithFeatures(featuresToEnable);

                using var workspace = TestWorkspace.CreateCSharp(
                    source1, parseOptions: experimental, compilationOptions: null, exportProvider: null);

                var oldSolution = workspace.CurrentSolution;
                var oldProject = oldSolution.Projects.Single();
                var oldDocument = oldProject.Documents.Single();
                var documentId = oldDocument.Id;

                var newSolution = workspace.CurrentSolution.WithDocumentText(documentId, SourceText.From(source2));

                var baseActiveStatements = ActiveStatementsMap.Empty;
                var analyzer = new CSharpEditAndContinueAnalyzer();

                var result = await analyzer.AnalyzeDocumentAsync(oldProject, baseActiveStatements, newSolution.GetDocument(documentId), ImmutableArray<LinePositionSpan>.Empty, EditAndContinueTestHelpers.Net5RuntimeCapabilities, CancellationToken.None);

                Assert.True(result.HasChanges);
                Assert.True(result.HasChangesAndErrors);
                Assert.False(result.HasChangesAndSyntaxErrors);
                Assert.Equal(RudeEditKind.ExperimentalFeaturesEnabled, result.RudeEditErrors.Single().Kind);
            }
        }

        [Fact]
        public async Task AnalyzeDocumentAsync_SemanticError_NoChange()
        {
            var source = @"
class C
{
    public static void Main()
    {
        System.Console.WriteLine(1);
        Bar(); // semantic error
    }
}
";

            using var workspace = TestWorkspace.CreateCSharp(source, composition: s_composition);

            var oldSolution = workspace.CurrentSolution;
            var oldProject = oldSolution.Projects.Single();
            var oldDocument = oldProject.Documents.Single();
            var documentId = oldDocument.Id;

            var baseActiveStatements = ActiveStatementsMap.Empty;
            var analyzer = new CSharpEditAndContinueAnalyzer();

            var result = await analyzer.AnalyzeDocumentAsync(oldProject, baseActiveStatements, oldDocument, ImmutableArray<LinePositionSpan>.Empty, EditAndContinueTestHelpers.Net5RuntimeCapabilities, CancellationToken.None);

            Assert.False(result.HasChanges);
            Assert.False(result.HasChangesAndErrors);
            Assert.False(result.HasChangesAndSyntaxErrors);
        }

        [Fact, WorkItem(10683, "https://github.com/dotnet/roslyn/issues/10683")]
        public async Task AnalyzeDocumentAsync_SemanticErrorInMethodBody_Change()
        {
            var source1 = @"
class C
{
    public static void Main()
    {
        System.Console.WriteLine(1);
        Bar(); // semantic error
    }
}
";
            var source2 = @"
class C
{
    public static void Main()
    {
        System.Console.WriteLine(2);
        Bar(); // semantic error
    }
}
";

            using var workspace = TestWorkspace.CreateCSharp(source1, composition: s_composition);

            var oldSolution = workspace.CurrentSolution;
            var oldProject = oldSolution.Projects.Single();
            var oldDocument = oldProject.Documents.Single();
            var documentId = oldDocument.Id;

            var newSolution = workspace.CurrentSolution.WithDocumentText(documentId, SourceText.From(source2));

            var baseActiveStatements = ActiveStatementsMap.Empty;
            var analyzer = new CSharpEditAndContinueAnalyzer();

            var result = await analyzer.AnalyzeDocumentAsync(oldProject, baseActiveStatements, newSolution.GetDocument(documentId), ImmutableArray<LinePositionSpan>.Empty, EditAndContinueTestHelpers.Net5RuntimeCapabilities, CancellationToken.None);

            Assert.True(result.HasChanges);

            // no declaration errors (error in method body is only reported when emitting):
            Assert.False(result.HasChangesAndErrors);
            Assert.False(result.HasChangesAndSyntaxErrors);
        }

        [Fact, WorkItem(10683, "https://github.com/dotnet/roslyn/issues/10683")]
        public async Task AnalyzeDocumentAsync_SemanticErrorInDeclaration_Change()
        {
            var source1 = @"
class C
{
    public static void Main(Bar x)
    {
        System.Console.WriteLine(1);
    }
}
";
            var source2 = @"
class C
{
    public static void Main(Bar x)
    {
        System.Console.WriteLine(2);
    }
}
";

            using var workspace = TestWorkspace.CreateCSharp(source1, composition: s_composition);

            var oldSolution = workspace.CurrentSolution;
            var oldProject = oldSolution.Projects.Single();
            var oldDocument = oldProject.Documents.Single();
            var documentId = oldDocument.Id;

            var newSolution = workspace.CurrentSolution.WithDocumentText(documentId, SourceText.From(source2));

            var baseActiveStatements = ActiveStatementsMap.Empty;
            var analyzer = new CSharpEditAndContinueAnalyzer();

            var result = await analyzer.AnalyzeDocumentAsync(oldProject, baseActiveStatements, newSolution.GetDocument(documentId), ImmutableArray<LinePositionSpan>.Empty, EditAndContinueTestHelpers.Net5RuntimeCapabilities, CancellationToken.None);

            Assert.True(result.HasChanges);

            // No errors reported: EnC analyzer is resilient against semantic errors.
            // They will be reported by 1) compiler diagnostic analyzer 2) when emitting delta - if still present.
            Assert.False(result.HasChangesAndErrors);
            Assert.False(result.HasChangesAndSyntaxErrors);
        }

        [Fact]
        public async Task AnalyzeDocumentAsync_AddingNewFileHavingRudeEdits()
        {
            var source1 = @"
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
            var source2 = @"
namespace N
{
    public class D
    {
    }
}
";

            using var workspace = TestWorkspace.CreateCSharp(source1, composition: s_composition);
            // fork the solution to introduce a change
            var oldProject = workspace.CurrentSolution.Projects.Single();
            var newDocId = DocumentId.CreateNewId(oldProject.Id);
            var oldSolution = workspace.CurrentSolution;
            var newSolution = oldSolution.AddDocument(newDocId, "goo.cs", SourceText.From(source2));

            workspace.TryApplyChanges(newSolution);

            var newProject = newSolution.Projects.Single();
            var changes = newProject.GetChanges(oldProject);

            Assert.Equal(2, newProject.Documents.Count());
            Assert.Equal(0, changes.GetChangedDocuments().Count());
            Assert.Equal(1, changes.GetAddedDocuments().Count());

            var changedDocuments = changes.GetChangedDocuments().Concat(changes.GetAddedDocuments());

            var result = new List<DocumentAnalysisResults>();
            var baseActiveStatements = ActiveStatementsMap.Empty;
            var analyzer = new CSharpEditAndContinueAnalyzer();

            foreach (var changedDocumentId in changedDocuments)
            {
                result.Add(await analyzer.AnalyzeDocumentAsync(oldProject, baseActiveStatements, newProject.GetDocument(changedDocumentId), ImmutableArray<LinePositionSpan>.Empty, EditAndContinueTestHelpers.Net5RuntimeCapabilities, CancellationToken.None));
            }

            Assert.True(result.IsSingle());
            Assert.Equal(1, result.Single().RudeEditErrors.Count());
            Assert.Equal(RudeEditKind.Insert, result.Single().RudeEditErrors.Single().Kind);
        }

        [Fact]
        public async Task AnalyzeDocumentAsync_AddingNewFile()
        {
            var source1 = @"
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
            var source2 = @"
class D
{
}
";

            using var workspace = TestWorkspace.CreateCSharp(source1, composition: s_composition);

            var oldSolution = workspace.CurrentSolution;
            var oldProject = oldSolution.Projects.Single();
            var newDocId = DocumentId.CreateNewId(oldProject.Id);
            var newSolution = oldSolution.AddDocument(newDocId, "goo.cs", SourceText.From(source2));

            workspace.TryApplyChanges(newSolution);

            var newProject = newSolution.Projects.Single();
            var changes = newProject.GetChanges(oldProject);

            Assert.Equal(2, newProject.Documents.Count());
            Assert.Equal(0, changes.GetChangedDocuments().Count());
            Assert.Equal(1, changes.GetAddedDocuments().Count());

            var changedDocuments = changes.GetChangedDocuments().Concat(changes.GetAddedDocuments());

            var result = new List<DocumentAnalysisResults>();
            var baseActiveStatements = ActiveStatementsMap.Empty;
            var analyzer = new CSharpEditAndContinueAnalyzer();

            foreach (var changedDocumentId in changedDocuments)
            {
                result.Add(await analyzer.AnalyzeDocumentAsync(oldProject, baseActiveStatements, newProject.GetDocument(changedDocumentId), ImmutableArray<LinePositionSpan>.Empty, EditAndContinueTestHelpers.Net5RuntimeCapabilities, CancellationToken.None));
            }

            Assert.True(result.IsSingle());
            Assert.Empty(result.Single().RudeEditErrors);
        }

        [Theory, CombinatorialData]
        public async Task AnalyzeDocumentAsync_InternalError(bool outOfMemory)
        {
            var source1 = @"class C {}";
            var source2 = @"class C { int x; }";

            using var workspace = TestWorkspace.CreateCSharp(source1, composition: s_composition);
            var oldProject = workspace.CurrentSolution.Projects.Single();
            var documentId = DocumentId.CreateNewId(oldProject.Id);
            var oldSolution = workspace.CurrentSolution;
            var newSolution = oldSolution.AddDocument(documentId, "goo.cs", SourceText.From(source2), filePath: "src.cs");
            var newProject = newSolution.Projects.Single();
            var newDocument = newProject.GetDocument(documentId);
            var newSyntaxTree = await newDocument.GetSyntaxTreeAsync().ConfigureAwait(false);

            workspace.TryApplyChanges(newSolution);

            var baseActiveStatements = ActiveStatementsMap.Empty;

            var analyzer = new CSharpEditAndContinueAnalyzer(node =>
            {
                if (node is CompilationUnitSyntax)
                {
                    throw outOfMemory ? new OutOfMemoryException() : new NullReferenceException("NullRef!");
                }
            });

            var result = await analyzer.AnalyzeDocumentAsync(oldProject, baseActiveStatements, newDocument, ImmutableArray<LinePositionSpan>.Empty, EditAndContinueTestHelpers.Net5RuntimeCapabilities, CancellationToken.None);

            var expectedDiagnostic = outOfMemory ?
                $"ENC0089: {string.Format(FeaturesResources.Modifying_source_file_will_prevent_the_debug_session_from_continuing_because_the_file_is_too_big, "src.cs")}" :
                // Because the error message that is formatted into this template string includes a stacktrace with newlines, we need to replicate that behavior
                // here so that any trailing punctuation is removed from the translated template string.
                $"ENC0080: {string.Format(FeaturesResources.Modifying_source_file_will_prevent_the_debug_session_from_continuing_due_to_internal_error, "src.cs", "System.NullReferenceException: NullRef!\n")}".Split('\n').First();

            AssertEx.Equal(new[] { expectedDiagnostic }, result.RudeEditErrors.Select(d => d.ToDiagnostic(newSyntaxTree))
                .Select(d => $"{d.Id}: {d.GetMessage().Split(new[] { Environment.NewLine }, StringSplitOptions.None).First()}"));
        }

        [Fact]
        public async Task AnalyzeDocumentAsync_NotSupportedByRuntime()
        {
            var source1 = @"
class C
{
    public static void Main()
    {
        System.Console.WriteLine(1);
    }
}
";
            var source2 = @"
class C
{
    public static void Main()
    {
        System.Console.WriteLine(2);
    }
}
";

            using var workspace = TestWorkspace.CreateCSharp(source1, composition: s_composition);

            var oldSolution = workspace.CurrentSolution;
            var oldProject = oldSolution.Projects.Single();
            var documentId = oldProject.Documents.Single().Id;
            var newSolution = workspace.CurrentSolution.WithDocumentText(documentId, SourceText.From(source2));
            var newDocument = newSolution.GetDocument(documentId);

            var analyzer = new CSharpEditAndContinueAnalyzer();

            var capabilities = EditAndContinueCapabilities.None;

            var result = await analyzer.AnalyzeDocumentAsync(oldProject, ActiveStatementsMap.Empty, newDocument, ImmutableArray<LinePositionSpan>.Empty, capabilities, CancellationToken.None);

            Assert.Equal(RudeEditKind.NotSupportedByRuntime, result.RudeEditErrors.Single().Kind);
        }
    }
}
