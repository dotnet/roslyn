// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class LocationsTests : TestBase
    {
        private static readonly TestSourceResolver s_resolver = new TestSourceResolver();

        private class TestSourceResolver : SourceFileResolver
        {
            public TestSourceResolver()
                : base(ImmutableArray<string>.Empty, null)
            {
            }

            public override string NormalizePath(string path, string baseFilePath)
            {
                return string.Format("[{0};{1}]", path, baseFilePath);
            }
        }

        private void AssertMappedSpanEqual(
            SyntaxTree syntaxTree,
            string sourceText,
            string expectedPath,
            int expectedStartLine,
            int expectedStartOffset,
            int expectedEndLine,
            int expectedEndOffset,
            bool hasMappedPath)
        {
            var span = GetSpanIn(syntaxTree, sourceText);
            var mappedSpan = syntaxTree.GetMappedLineSpan(span);
            var actualDisplayPath = syntaxTree.GetDisplayPath(span, s_resolver);

            Assert.Equal(hasMappedPath, mappedSpan.HasMappedPath);
            Assert.Equal(expectedPath, mappedSpan.Path);
            if (expectedPath == "")
            {
                Assert.Equal("", actualDisplayPath);
            }
            else
            {
                Assert.Equal(string.Format("[{0};{1}]", expectedPath, hasMappedPath ? syntaxTree.FilePath : null), actualDisplayPath);
            }

            Assert.Equal(expectedStartLine, mappedSpan.StartLinePosition.Line);
            Assert.Equal(expectedStartOffset, mappedSpan.StartLinePosition.Character);
            Assert.Equal(expectedEndLine, mappedSpan.EndLinePosition.Line);
            Assert.Equal(expectedEndOffset, mappedSpan.EndLinePosition.Character);
        }

        private TextSpan GetSpanIn(SyntaxTree syntaxTree, string textToFind)
        {
            string s = syntaxTree.GetText().ToString();
            int index = s.IndexOf(textToFind, StringComparison.Ordinal);
            Assert.True(index >= 0, "textToFind not found in the tree");
            return new TextSpan(index, textToFind.Length);
        }

        [ClrOnlyFact]
        public void TestGetSourceLocationInFile()
        {
            string sampleProgram = @"class X {
#line 20 ""d:\banana.cs""
int x; 
}";
            SyntaxTree syntaxTree = SyntaxFactory.ParseSyntaxTree(sampleProgram, path: "c:\\goo.cs");

            TextSpan xSpan = new TextSpan(sampleProgram.IndexOf("x;", StringComparison.Ordinal), 2);
            TextSpan xToCloseBraceSpan = new TextSpan(xSpan.Start, sampleProgram.IndexOf('}') - xSpan.Start + 1);
            Location locX = new SourceLocation(syntaxTree, xSpan);
            Location locXToCloseBrace = new SourceLocation(syntaxTree, xToCloseBraceSpan);

            FileLinePositionSpan flpsX = locX.GetLineSpan();
            Assert.Equal("c:\\goo.cs", flpsX.Path);
            Assert.Equal(2, flpsX.StartLinePosition.Line);
            Assert.Equal(4, flpsX.StartLinePosition.Character);
            Assert.Equal(2, flpsX.EndLinePosition.Line);
            Assert.Equal(6, flpsX.EndLinePosition.Character);

            flpsX = locX.GetMappedLineSpan();
            Assert.Equal("d:\\banana.cs", flpsX.Path);
            Assert.Equal(19, flpsX.StartLinePosition.Line);
            Assert.Equal(4, flpsX.StartLinePosition.Character);
            Assert.Equal(19, flpsX.EndLinePosition.Line);
            Assert.Equal(6, flpsX.EndLinePosition.Character);

            FileLinePositionSpan flpsXToCloseBrace = locXToCloseBrace.GetLineSpan();
            Assert.Equal("c:\\goo.cs", flpsXToCloseBrace.Path);
            Assert.Equal(2, flpsXToCloseBrace.StartLinePosition.Line);
            Assert.Equal(4, flpsXToCloseBrace.StartLinePosition.Character);
            Assert.Equal(3, flpsXToCloseBrace.EndLinePosition.Line);
            Assert.Equal(1, flpsXToCloseBrace.EndLinePosition.Character);

            flpsXToCloseBrace = locXToCloseBrace.GetMappedLineSpan();
            Assert.Equal("d:\\banana.cs", flpsXToCloseBrace.Path);
            Assert.Equal(19, flpsXToCloseBrace.StartLinePosition.Line);
            Assert.Equal(4, flpsXToCloseBrace.StartLinePosition.Character);
            Assert.Equal(20, flpsXToCloseBrace.EndLinePosition.Line);
            Assert.Equal(1, flpsXToCloseBrace.EndLinePosition.Character);
        }

        [ClrOnlyFact]
        public void TestLineMapping1()
        {
            string sampleProgram = @"using System;
class X {
#line 20 ""banana.cs""
int x;
int y;
#line 44
int z;
#line default
int w;
#line hidden
int q;
int f;
#if false
#line 17 ""d:\twing.cs""
#endif
int a;
}".NormalizeLineEndings();
            var resolver = new TestSourceResolver();

            SyntaxTree syntaxTree = SyntaxFactory.ParseSyntaxTree(sampleProgram, path: "goo.cs");

            AssertMappedSpanEqual(syntaxTree, "ing Sy", "goo.cs", 0, 2, 0, 8, hasMappedPath: false);
            AssertMappedSpanEqual(syntaxTree, "class X", "goo.cs", 1, 0, 1, 7, hasMappedPath: false);
            AssertMappedSpanEqual(syntaxTree, $"System;\r\nclass X", "goo.cs", 0, 6, 1, 7, hasMappedPath: false);
            AssertMappedSpanEqual(syntaxTree, "x;", "banana.cs", 19, 4, 19, 6, hasMappedPath: true);
            AssertMappedSpanEqual(syntaxTree, "y;", "banana.cs", 20, 4, 20, 6, hasMappedPath: true);
            AssertMappedSpanEqual(syntaxTree, "z;", "banana.cs", 43, 4, 43, 6, hasMappedPath: true);
            AssertMappedSpanEqual(syntaxTree, "w;", "goo.cs", 8, 4, 8, 6, hasMappedPath: false);
            AssertMappedSpanEqual(syntaxTree, "q;\r\nin", "goo.cs", 10, 4, 11, 2, hasMappedPath: false);
            AssertMappedSpanEqual(syntaxTree, "a;", "goo.cs", 15, 4, 15, 6, hasMappedPath: false);

            var text = syntaxTree.GetText();

            AssertEx.Equal(new[]
            {
                "[|using System;\r\nclass X {\r\n|] -> : (0,0)-(1,11)",
                "[|int x;\r\nint y;\r\n|] -> banana.cs: (19,0)-(20,8)",
                "[|int z;\r\n|] -> banana.cs: (43,0)-(43,8)",
                "[|int w;\r\n|] -> : (8,0)-(8,8)",
                "[|int q;\r\nint f;\r\n#if false\r\n#line 17 \"d:\\twing.cs\"\r\n#endif\r\nint a;\r\n}|] -> : (0,0)-(0,0)"
            }, syntaxTree.GetLineMappings(default).Select(mapping => $"[|{text.GetSubText(text.Lines.GetTextSpan(mapping.Span))}|] -> {mapping.MappedSpan}"));
        }

        [Fact]
        public void TestLineMapping2()
        {
            string sampleProgram = @"using System;
class X {
#line 20
int x;
#line hidden
int y;
#line 30 ""baz""
int z;
#line hidden
int w;
#line 40
int v;
}";
            var resolver = new TestSourceResolver();

            SyntaxTree syntaxTree = SyntaxFactory.ParseSyntaxTree(sampleProgram, path: "c:\\goo.cs");

            AssertMappedSpanEqual(syntaxTree, "int x;", "c:\\goo.cs", 19, 0, 19, 6, hasMappedPath: false);
            AssertMappedSpanEqual(syntaxTree, "int y;", "c:\\goo.cs", 21, 0, 21, 6, hasMappedPath: false);
            AssertMappedSpanEqual(syntaxTree, "int z;", "baz", 29, 0, 29, 6, hasMappedPath: true);
            AssertMappedSpanEqual(syntaxTree, "int w;", "baz", 31, 0, 31, 6, hasMappedPath: true);
            AssertMappedSpanEqual(syntaxTree, "int v;", "baz", 39, 0, 39, 6, hasMappedPath: true);
        }

        [Fact]
        public void TestLineMapping_NoSyntaxTreePath()
        {
            string sampleProgram = @"using System;
#line 20
class X {}
";
            AssertMappedSpanEqual(SyntaxFactory.ParseSyntaxTree(sampleProgram, path: ""), "class X {}", "", 19, 0, 19, 10, hasMappedPath: false);
            AssertMappedSpanEqual(SyntaxFactory.ParseSyntaxTree(sampleProgram, path: "   "), "class X {}", "   ", 19, 0, 19, 10, hasMappedPath: false);
        }

        [Fact]
        public void TestInvalidLineMapping()
        {
            string sampleProgram = @"using System;
class X {
    int q; 
#line 0 ""firstdirective""
    int r; 
#line 20 ""seconddirective""
    int s; 
}";
            SyntaxTree syntaxTree = SyntaxFactory.ParseSyntaxTree(sampleProgram, path: "filename.cs");

            AssertMappedSpanEqual(syntaxTree, "int q", "filename.cs", 2, 4, 2, 9, hasMappedPath: false);
            AssertMappedSpanEqual(syntaxTree, "int r", "filename.cs", 4, 4, 4, 9, hasMappedPath: false); // invalid #line args
            AssertMappedSpanEqual(syntaxTree, "int s", "seconddirective", 19, 4, 19, 9, hasMappedPath: true);
        }

        [Fact]
        public void TestLineMappingNoDirectives()
        {
            string sampleProgram = @"using System;
class X {
int x;
}";
            SyntaxTree syntaxTree = SyntaxFactory.ParseSyntaxTree(sampleProgram, path: "c:\\goo.cs");

            AssertMappedSpanEqual(syntaxTree, "ing Sy", "c:\\goo.cs", 0, 2, 0, 8, hasMappedPath: false);
            AssertMappedSpanEqual(syntaxTree, "class X", "c:\\goo.cs", 1, 0, 1, 7, hasMappedPath: false);
            AssertMappedSpanEqual(syntaxTree, $"System;{Environment.NewLine}class X", "c:\\goo.cs", 0, 6, 1, 7, hasMappedPath: false);
            AssertMappedSpanEqual(syntaxTree, "x;", "c:\\goo.cs", 2, 4, 2, 6, hasMappedPath: false);

            Assert.Empty(syntaxTree.GetLineMappings(default));
        }

        [Fact]
        public void TestLineMappingFirstAndLastLineDirectives()
        {
            string sampleProgram = @"#line 20
class X {}
#line 30".NormalizeLineEndings();
            var syntaxTree = SyntaxFactory.ParseSyntaxTree(sampleProgram, path: "c:\\goo.cs");

            var text = syntaxTree.GetText();

            AssertEx.Equal(new[]
            {
                "[|class X {}\r\n|] -> : (19,0)-(19,12)",
            }, syntaxTree.GetLineMappings(default).Select(mapping => $"[|{text.GetSubText(text.Lines.GetTextSpan(mapping.Span))}|] -> {mapping.MappedSpan}"));
        }

        [Fact]
        public void TestLineMappingLastLineDirectiveFollowedByEmptyLine()
        {
            string sampleProgram = @"#line 30
".NormalizeLineEndings();

            var syntaxTree = SyntaxFactory.ParseSyntaxTree(sampleProgram, path: "c:\\goo.cs");

            var text = syntaxTree.GetText();

            AssertEx.Equal(new[]
            {
                "[||] -> : (29,0)-(29,0)",
            }, syntaxTree.GetLineMappings(default).Select(mapping => $"[|{text.GetSubText(text.Lines.GetTextSpan(mapping.Span))}|] -> {mapping.MappedSpan}"));
        }

        [Fact]
        public void TestLineMappingConsecutiveDirectives()
        {
            string sampleProgram =
@"#line hidden
#line default
class C {}
#line 5
#line 6
class D {}
".NormalizeLineEndings();

            var syntaxTree = SyntaxFactory.ParseSyntaxTree(sampleProgram, path: "c:\\goo.cs");

            var text = syntaxTree.GetText();

            AssertEx.Equal(new[]
            {
                "[|class C {}\r\n|] -> : (2,0)-(2,12)",
                "[|class D {}\r\n|] -> : (5,0)-(6,0)",
            }, syntaxTree.GetLineMappings(default).Select(mapping => $"[|{text.GetSubText(text.Lines.GetTextSpan(mapping.Span))}|] -> {mapping.MappedSpan}"));
        }

        [WorkItem(537005, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537005")]
        [Fact]
        public void TestMissingTokenAtEndOfLine()
        {
            string sampleProgram = @"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        int x
}
}";
            SyntaxTree syntaxTree = SyntaxFactory.ParseSyntaxTree(sampleProgram, path: "c:\\goo.cs");
            // verify missing semicolon diagnostic is on the same line
            var diags = syntaxTree.GetDiagnostics();
            Assert.Equal(1, diags.Count());
            var diag = diags.First();
            FileLinePositionSpan flps = diag.Location.GetLineSpan();
            // verify the diagnostic is positioned at the end of the line "int x" and has zero width
            Assert.Equal(flps, new FileLinePositionSpan("c:\\goo.cs", new LinePosition(8, 13), new LinePosition(8, 13)));

            sampleProgram = @"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        int x // dummy comment
}
}";
            syntaxTree = SyntaxFactory.ParseSyntaxTree(sampleProgram, path: "c:\\goo.cs");
            diags = syntaxTree.GetDiagnostics();
            diag = diags.First();
            flps = diag.Location.GetLineSpan();
            // verify missing semicolon diagnostic is on the same line and before the comment
            Assert.Equal(flps, new FileLinePositionSpan("c:\\goo.cs", new LinePosition(8, 13), new LinePosition(8, 13)));

            sampleProgram = @"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        int x /* dummy
multiline
comment*/ 
}
}";
            syntaxTree = SyntaxFactory.ParseSyntaxTree(sampleProgram, path: "c:\\goo.cs");
            diags = syntaxTree.GetDiagnostics();
            diag = diags.First();
            flps = diag.Location.GetLineSpan();
            // verify missing semicolon diagnostic is on the same line and before the comment
            Assert.Equal(flps, new FileLinePositionSpan("c:\\goo.cs", new LinePosition(8, 13), new LinePosition(8, 13)));
        }

        [WorkItem(537537, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537537")]
        [Fact]
        public void TestDiagnosticSpanForIdentifierExpectedError()
        {
            string sampleProgram = @"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        string 2131;
    }
}";
            SyntaxTree syntaxTree = SyntaxFactory.ParseSyntaxTree(sampleProgram, path: "c:\\goo.cs");
            var diags = syntaxTree.GetDiagnostics();
            // verify missing identifier diagnostic has the correct span
            Assert.NotEmpty(diags);
            var diag = diags.First();
            FileLinePositionSpan flps = diag.Location.GetLineSpan();
            // verify the diagnostic width spans the entire token "2131"
            Assert.Equal(flps, new FileLinePositionSpan("c:\\goo.cs", new LinePosition(8, 15), new LinePosition(8, 19)));
        }

        [WorkItem(540077, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540077")]
        [Fact]
        public void TestDiagnosticSpanForErrorAtLastToken()
        {
            string sampleProgram = @"using System;
using System.Collections.Generic;
using System.Linq;

class C
{
    int[] array = new int[
}";
            SyntaxTree syntaxTree = SyntaxFactory.ParseSyntaxTree(sampleProgram, path: "c:\\goo.cs");
            var diags = syntaxTree.GetDiagnostics();
            Assert.NotEmpty(diags);
            foreach (var diag in diags)
            {
                // verify the diagnostic span doesn't go past the text span
                Assert.InRange(diag.Location.SourceSpan.End, diag.Location.SourceSpan.Start, syntaxTree.GetText().Length);
            }
        }

        [WorkItem(537215, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537215")]
        [Fact]
        public void TestLineMappingForErrors()
        {
            string sampleProgram = @"class
end class";
            SyntaxTree syntaxTree = SyntaxFactory.ParseSyntaxTree(sampleProgram);

            // The below commented lines work fine. Please uncomment once bug is fixed.
            // Expected(tree.GetDiagnostics()[0].Location.GetLineSpan("", false), "", 1, 4, 1, 5);
            // Expected(tree.GetDiagnostics()[1].Location.GetLineSpan("", false), "", 1, 4, 1, 5);
            // Expected(tree.GetDiagnostics()[2].Location.GetLineSpan("", false), "", 1, 9, 1, 9);

            // This line throws ArgumentOutOfRangeException.
            var span = syntaxTree.GetDiagnostics().ElementAt(3).Location.GetLineSpan();
            Assert.Equal(span, new FileLinePositionSpan("", new LinePosition(1, 9), new LinePosition(1, 9)));
        }

        [Fact]
        public void TestEqualSourceLocations()
        {
            string sampleProgram = @"class
end class";
            SyntaxTree syntaxTree = SyntaxFactory.ParseSyntaxTree(sampleProgram);
            SyntaxTree tree2 = SyntaxFactory.ParseSyntaxTree(sampleProgram);
            SourceLocation loc1 = new SourceLocation(syntaxTree, new TextSpan(3, 4));
            SourceLocation loc2 = new SourceLocation(syntaxTree, new TextSpan(3, 4));
            SourceLocation loc3 = new SourceLocation(syntaxTree, new TextSpan(3, 7));
            SourceLocation loc4 = new SourceLocation(tree2, new TextSpan(3, 4));
            Assert.Equal(loc1, loc2);
            Assert.Equal(loc1.GetHashCode(), loc2.GetHashCode());
            Assert.NotEqual(loc1, loc3);
            Assert.NotEqual(loc3, loc4);
        }

        [WorkItem(541612, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541612")]
        [Fact]
        public void DiagnosticsGetLineSpanForErrorinTryCatch()
        {
            string sampleProgram = @"
class Program
{
    static void Main(string[] args)
    {
        try
        {
        }
        ct
    }
}";
            SyntaxTree syntaxTree = SyntaxFactory.ParseSyntaxTree(sampleProgram, path: "c:\\goo.cs");
            var token = syntaxTree.GetCompilationUnitRoot().FindToken(sampleProgram.IndexOf("ct", StringComparison.Ordinal));

            // Get the diagnostics from the ExpressionStatement Syntax node which is the current token's Parent's Parent
            var expressionDiags = syntaxTree.GetDiagnostics(token.Parent.Parent);

            expressionDiags.First().Location.GetLineSpan();

            foreach (var diag in expressionDiags)
            {
                // verify the diagnostic span doesn't go past the text span
                Assert.InRange(diag.Location.SourceSpan.Start, 0, syntaxTree.GetText().Length);
                Assert.InRange(diag.Location.SourceSpan.End, 0, syntaxTree.GetText().Length);
            }
        }

        [WorkItem(537926, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537926")]
        [ClrOnlyFact]
        public void TestSourceLocationToString()
        {
            string sampleProgram = @"using System;

class MainClass
{
    static void Main()
    {
#line 200
        int i;    // CS0168 on line 200
#line default
        char c;   // CS0168 on line 9
    }
}
".NormalizeLineEndings();

            SyntaxTree syntaxTree = SyntaxFactory.ParseSyntaxTree(sampleProgram);

            TextSpan span1 = new TextSpan(sampleProgram.IndexOf("i;", StringComparison.Ordinal), 2);
            TextSpan span2 = new TextSpan(sampleProgram.IndexOf("c;", StringComparison.Ordinal), 2);
            SourceLocation loc1 = new SourceLocation(syntaxTree, span1);
            SourceLocation loc2 = new SourceLocation(syntaxTree, span2);
            // GetDebuggerDisplay() is private
            // Assert.Equal("SourceLocation(@8:13)\"i;\"", loc1.GetDebuggerDisplay());
            Assert.Equal("SourceFile([91..93))", loc1.ToString());
            // Assert.Equal("SourceLocation(@10:14)\"c;\"", loc2.GetDebuggerDisplay());
            Assert.Equal("SourceFile([148..150))", loc2.ToString());
        }

        [Fact]
        public void TestExternalLocationFormatting()
        {
            Location location = Location.Create("test.txt", new TextSpan(), new LinePositionSpan(new LinePosition(2, 1), new LinePosition(3, 1)));
            var diagnostic = CodeAnalysis.Diagnostic.Create("CS0000", "", "msg", DiagnosticSeverity.Warning, DiagnosticSeverity.Warning, true, 1, location: location);

            Assert.Equal("test.txt(3,2): warning CS0000: msg", CSharpDiagnosticFormatter.Instance.Format(diagnostic, EnsureEnglishUICulture.PreferredOrNull));
        }

        [WorkItem(1097381, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1097381")]
        [Fact]
        public void TestDiagnosticsLocationsExistInsideTreeSpan()
        {
            var node = SyntaxFactory.IdentifierName(SyntaxFactory.Identifier(SyntaxFactory.TriviaList(SyntaxFactory.Whitespace("    ")), "x", default(SyntaxTriviaList)));

            // create node with error that would place itself outside the tree.
            var nodeWithBadError = node.Green.WithDiagnosticsGreen(new DiagnosticInfo[] { new SyntaxDiagnosticInfo(10, 10, ErrorCode.ERR_AbstractAndExtern) }).CreateRed();

            var tree = SyntaxFactory.SyntaxTree(nodeWithBadError);

            var treeSpan = tree.GetRoot().FullSpan;
            Assert.Equal(5, treeSpan.Length);

            var diagnostics = tree.GetDiagnostics().ToList();

            Assert.Equal(1, diagnostics.Count);
            Assert.Equal(5, diagnostics[0].Location.SourceSpan.Start);
            Assert.Equal(0, diagnostics[0].Location.SourceSpan.Length);

            Assert.True(treeSpan.Contains(diagnostics[0].Location.SourceSpan));

            var lineSpan = diagnostics[0].Location.GetLineSpan();
            Assert.Equal(0, lineSpan.StartLinePosition.Line);
            Assert.Equal(5, lineSpan.StartLinePosition.Character);
            Assert.Equal(0, lineSpan.EndLinePosition.Line);
            Assert.Equal(5, lineSpan.EndLinePosition.Character);
        }

        [WorkItem(1097381, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1097381")]
        [Fact]
        public void TestDiagnosticsLocationsExistInsideTreeSpan_ZeroWidthTree()
        {
            var node = SyntaxFactory.MissingToken(SyntaxKind.IdentifierToken);

            // create node with error that would place itself outside the tree.
            var nodeWithBadError = SyntaxFactory.IdentifierName(new SyntaxToken(node.Node.WithDiagnosticsGreen(new DiagnosticInfo[] { new SyntaxDiagnosticInfo(10, 10, ErrorCode.ERR_AbstractAndExtern) })));
            var tree = SyntaxFactory.SyntaxTree(nodeWithBadError);

            var treeSpan = tree.GetRoot().FullSpan;
            Assert.Equal(0, treeSpan.Length);

            var diagnostics = tree.GetDiagnostics().ToList();

            Assert.Equal(1, diagnostics.Count);
            Assert.Equal(0, diagnostics[0].Location.SourceSpan.Start);
            Assert.Equal(0, diagnostics[0].Location.SourceSpan.Length);

            Assert.True(treeSpan.Contains(diagnostics[0].Location.SourceSpan));

            var lineSpan = diagnostics[0].Location.GetLineSpan();
            Assert.Equal(0, lineSpan.StartLinePosition.Line);
            Assert.Equal(0, lineSpan.StartLinePosition.Character);
            Assert.Equal(0, lineSpan.EndLinePosition.Line);
            Assert.Equal(0, lineSpan.EndLinePosition.Character);
        }
    }
}
