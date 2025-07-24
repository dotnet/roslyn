// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Editor.CSharp.TextStructureNavigation;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities.TextStructureNavigation;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.TextStructureNavigation;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.TextStructureNavigator)]
public sealed class TextStructureNavigatorTests : AbstractTextStructureNavigatorTests
{
    protected override string ContentType => ContentTypeNames.CSharpContentType;

    protected override EditorTestWorkspace CreateWorkspace(string code)
        => EditorTestWorkspace.CreateCSharp(code);

    [Fact]
    public void Empty()
        => AssertExtent("""$${|Insignificant:|}""");

    [WpfFact]
    public void Whitespace()
    {
        AssertExtent(
            """$${|Insignificant:   |}""");

        AssertExtent(
            """{|Insignificant: $$  |}""");

        AssertExtent(
            """{|Insignificant:   $$|}""");
    }

    [WpfFact]
    public void EndOfFile()
        => AssertExtent(
            """using System{|Significant:;|}$$""");

    [Fact]
    public void NewLine()
    {
        AssertExtent(
            """
            class Class1 {$${|Insignificant:
            |}
            }
            """);
        AssertExtent(
            """
            class Class1 {
            $${|Insignificant:
            |}}
            """);
    }

    [WpfFact]
    public void SingleLineComment()
    {
        AssertExtent(
            """$${|Significant:// Comment  |}""");

        // It is important that this returns just the comment banner. Returning the whole comment
        // means Ctrl+Right before the slash will cause it to jump across the entire comment
        AssertExtent(
            """{|Significant:/$$/|} Comment  """);

        AssertExtent(
            """// {|Significant:Co$$mment|}  """);

        AssertExtent(
            """// {|Significant:($$)|} test""");
    }

    [WpfFact]
    public void MultiLineComment()
    {
        AssertExtent(
            """{|Significant:$$/* Comment */|}""");

        // It is important that this returns just the comment banner. Returning the whole comment
        // means Ctrl+Right before the slash will cause it to jump across the entire comment
        AssertExtent(
            """{|Significant:/$$*|} Comment */""");

        AssertExtent(
            """/* {|Significant:Co$$mment|} */""");

        AssertExtent(
            """/* {|Significant:($$)|} test */""");

        AssertExtent(
            """/* () test {|Significant:$$*/|}""");

        // It is important that this returns just the comment banner. Returning the whole comment
        // means Ctrl+Left after the slash will cause it to jump across the entire comment
        AssertExtent(
            """/* () test {|Significant:*$$/|}""");

        AssertExtent(
            """/* () test {|Significant:*/|}$$""");
    }

    [WpfFact]
    public void Keyword()
    {
        AssertExtent(
            """public {|Significant:$$class|} Class1""");

        AssertExtent(
            """public {|Significant:c$$lass|} Class1""");

        AssertExtent(
            """public {|Significant:cl$$ass|} Class1""");

        AssertExtent(
            """public {|Significant:cla$$ss|} Class1""");

        AssertExtent(
            """public {|Significant:clas$$s|} Class1""");
    }

    [WpfFact]
    public void Identifier()
    {
        AssertExtent(
            """public class {|Significant:$$SomeClass|} : IDisposable""");

        AssertExtent(
            """public class {|Significant:S$$omeClass|} : IDisposable""");

        AssertExtent(
            """public class {|Significant:So$$meClass|} : IDisposable""");

        AssertExtent(
            """public class {|Significant:Som$$eClass|} : IDisposable""");

        AssertExtent(
            """public class {|Significant:Some$$Class|} : IDisposable""");

        AssertExtent(
            """public class {|Significant:SomeC$$lass|} : IDisposable""");

        AssertExtent(
            """public class {|Significant:SomeCl$$ass|} : IDisposable""");

        AssertExtent(
            """public class {|Significant:SomeCla$$ss|} : IDisposable""");

        AssertExtent(
            """public class {|Significant:SomeClas$$s|} : IDisposable""");
    }

    [WpfFact]
    public void EscapedIdentifier()
    {
        AssertExtent(
            """public enum {|Significant:$$@interface|} : int""");

        AssertExtent(
            """public enum {|Significant:@$$interface|} : int""");

        AssertExtent(
            """public enum {|Significant:@i$$nterface|} : int""");

        AssertExtent(
            """public enum {|Significant:@in$$terface|} : int""");

        AssertExtent(
            """public enum {|Significant:@int$$erface|} : int""");

        AssertExtent(
            """public enum {|Significant:@inte$$rface|} : int""");

        AssertExtent(
            """public enum {|Significant:@inter$$face|} : int""");

        AssertExtent(
            """public enum {|Significant:@interf$$ace|} : int""");

        AssertExtent(
            """public enum {|Significant:@interfa$$ce|} : int""");

        AssertExtent(
            """public enum {|Significant:@interfac$$e|} : int""");
    }

    [WpfFact]
    public void Number()
    {
        AssertExtent(
            """class Test { private double num   = -{|Significant:$$1.234678e10|}; }""");

        AssertExtent(
            """class Test { private double num   = -{|Significant:1$$.234678e10|}; }""");

        AssertExtent(
            """class Test { private double num   = -{|Significant:1.$$234678e10|}; }""");

        AssertExtent(
            """class Test { private double num   = -{|Significant:1.2$$34678e10|}; }""");

        AssertExtent(
            """class Test { private double num   = -{|Significant:1.23$$4678e10|}; }""");

        AssertExtent(
            """class Test { private double num   = -{|Significant:1.234$$678e10|}; }""");

        AssertExtent(
            """class Test { private double num   = -{|Significant:1.2346$$78e10|}; }""");

        AssertExtent(
            """class Test { private double num   = -{|Significant:1.23467$$8e10|}; }""");

        AssertExtent(
            """class Test { private double num   = -{|Significant:1.234678$$e10|}; }""");

        AssertExtent(
            """class Test { private double num   = -{|Significant:1.234678e$$10|}; }""");

        AssertExtent(
            """class Test { private double num   = -{|Significant:1.234678e1$$0|}; }""");
    }

    [WpfTheory]
    [InlineData("""class Test { private string s1 = {|Significant:$$"|} () test  "; }""")]
    [InlineData("""class Test { private string s1 = "{|Insignificant:$$ |}() test  "; }""")]
    [InlineData("""class Test { private string s1 = " {|Significant:$$()|} test  "; }""")]
    [InlineData("""class Test { private string s1 = " () test{|Insignificant:$$  |}"; }""")]
    [InlineData("""class Test { private string s1 = " () test  {|Significant:$$"|}; }""")]
    [InlineData("""class Test { private string s1 = " () test  "{|Significant:$$;|} }""")]
    public void String(string content)
        => AssertExtent(content);

    [WpfTheory]
    [InlineData("""class Test { private string s1 = {|Significant:$$"|} () test  "u8; }""")]
    [InlineData("""class Test { private string s1 = "{|Insignificant:$$ |}() test  "u8; }""")]
    [InlineData("""class Test { private string s1 = " {|Significant:$$()|} test  "u8; }""")]
    [InlineData("""class Test { private string s1 = " () test{|Insignificant:$$  |}"u8; }""")]
    [InlineData("""class Test { private string s1 = " () test  {|Significant:$$"u8|}; }""")]
    [InlineData("""class Test { private string s1 = " () test  "u8{|Significant:$$;|} }""")]
    public void Utf8String(string content)
        => AssertExtent(content);

    [WpfFact]
    public void InterpolatedString1()
    {
        AssertExtent(
             """class Test { string x = "hello"; string s = {|Significant:$$$"|} { x } hello"; }""");

        AssertExtent(
            """class Test { string x = "hello"; string s = $"{|Insignificant:$$ |}{ x } hello"; }""");

        AssertExtent(
            """class Test { string x = "hello"; string s = $" {|Significant:$${|} x } hello"; }""");

        AssertExtent(
            """class Test { string x = "hello"; string s = $" {{|Insignificant:$$ |}x } hello"; }""");

        AssertExtent(
            """class Test { string x = "hello"; string s = $" { {|Significant:$$x|} } hello"; }""");

        AssertExtent(
            """class Test { string x = "hello"; string s = $" { x{|Insignificant:$$ |}} hello"; }""");

        AssertExtent(
            """class Test { string x = "hello"; string s = $" { x {|Significant:$$}|} hello"; }""");

        AssertExtent(
            """class Test { string x = "hello"; string s = $" { x }{|Insignificant:$$ |}hello"; }""");

        AssertExtent(
            """class Test { string x = "hello"; string s = $" { x } {|Significant:$$hello|}"; }""");

        AssertExtent(
            """class Test { string x = "hello"; string s = $" { x } hello{|Significant:$$"|}; }""");
    }

    [WpfFact, WorkItem("""https://github.com/dotnet/roslyn/issues/59581""")]
    public void TestRawStringContent()
    {
        AssertExtent(
            """"
            string s = """
                Hello
                    {|Significant:$$World|}!
                :)
                """;
            """");

        AssertExtent(
            """"
            string s = """
                Hello
                    {|Significant:W$$orld|}!
                :)
                """;
            """");

        AssertExtent(
            """"
            string s = """
                Hello
                    {|Significant:Wo$$rld|}!
                :)
                """;
            """");

        AssertExtent(
            """"
            string s = """
                Hello
                    {|Significant:Wor$$ld|}!
                :)
                """;
            """");

        AssertExtent(
            """"
            string s = """
                Hello
                    {|Significant:Worl$$d|}!
                :)
                """;
            """");

        AssertExtent(
            """"
            string s = """
                Hello
                    World{|Significant:$$!|}
                :)
                """;
            """");
    }

    [WpfTheory, WorkItem("""https://github.com/dotnet/roslyn/issues/59581""")]
    [InlineData(
        """"
        string s = {|Significant:$$"""|}
            Hello
                World!
            :)
            """;
        """")]
    [InlineData(
        """"
        string s = {|Significant:"$$""|}
            Hello
                World!
            :)
            """;
        """")]
    [InlineData(
        """"
        string s = {|Significant:""$$"|}
            Hello
                World!
            :)
            """;
        """")]
    public void TestRawStringDelimiter1(string content)
        => AssertExtent(content);

    [WpfTheory, WorkItem("""https://github.com/dotnet/roslyn/issues/59581""")]
    [InlineData(
        """"
        string s = """
            Hello
                World!
            :)
            {|Significant:$$"""|};
        """")]
    [InlineData(
        """"
        string s = """
            Hello
                World!
            :)
            {|Significant:"$$""|};
        """")]
    [InlineData(
        """"
        string s = """
            Hello
                World!
            :)
            {|Significant:""$$"|};
        """")]
    public void TestRawStringDelimiter2(string content)
        => AssertExtent(content);

    [WpfTheory]
    [InlineData(
        """"
        string s = """
            Hello
                World!
            :)
            {|Significant:$$"""u8|};
        """")]
    [InlineData(
        """"
        string s = """
            Hello
                World!
            :)
            {|Significant:"$$""u8|};
        """")]
    [InlineData(
        """"
        string s = """
            Hello
                World!
            :)
            {|Significant:""$$"u8|};
        """")]
    [InlineData(
        """"
        string s = """
            Hello
                World!
            :)
            {|Significant:"""$$u8|};
        """")]
    [InlineData(
        """"
        string s = """
            Hello
                World!
            :)
            {|Significant:"""u$$8|};
        """")]
    public void TestUtf8RawStringDelimiter(string content)
        => AssertExtent(content);

    private static void TestNavigator(
        string code,
        Func<ITextStructureNavigator, SnapshotSpan, SnapshotSpan> func,
        int startPosition,
        int startLength,
        int endPosition,
        int endLength)
    {
        TestNavigator(code, func, startPosition, startLength, endPosition, endLength, null);
        TestNavigator(code, func, startPosition, startLength, endPosition, endLength, TestOptions.Script);
    }

    private static void TestNavigator(
        string code,
        Func<ITextStructureNavigator, SnapshotSpan, SnapshotSpan> func,
        int startPosition,
        int startLength,
        int endPosition,
        int endLength,
        CSharpParseOptions? options)
    {
        using var workspace = EditorTestWorkspace.CreateCSharp(code, options);
        var buffer = workspace.Documents.First().GetTextBuffer();

        var provider = Assert.IsType<CSharpTextStructureNavigatorProvider>(
            workspace.GetService<ITextStructureNavigatorProvider>(ContentTypeNames.CSharpContentType));

        var navigator = provider.CreateTextStructureNavigator(buffer);

        var actualSpan = func(navigator, new SnapshotSpan(buffer.CurrentSnapshot, startPosition, startLength));
        var expectedSpan = new SnapshotSpan(buffer.CurrentSnapshot, endPosition, endLength);
        Assert.Equal(expectedSpan, actualSpan.Span);
    }

    [WpfFact]
    public void GetSpanOfEnclosingTest()
    {
        // First operation returns span of 'Class1'
        TestNavigator(
            """class Class1 { }""", (n, s) => n.GetSpanOfEnclosing(s), 10, 0, 6, 6);

        // Second operation returns span of 'class Class1 { }'
        TestNavigator(
            """class Class1 { }""", (n, s) => n.GetSpanOfEnclosing(s), 6, 6, 0, 16);

        // Last operation does nothing
        TestNavigator(
            """class Class1 { }""", (n, s) => n.GetSpanOfEnclosing(s), 0, 16, 0, 16);
    }

    [WpfFact]
    public void GetSpanOfFirstChildTest()
    {
        // Go from 'class Class1 { }' to 'class'
        TestNavigator(
            """
            class Class1
            {
            }
            """, (n, s) => n.GetSpanOfFirstChild(s), 0, 16, 0, 5);

        // Next operation should do nothing as we're at the bottom
        TestNavigator(
            """
            class Class1
            {
            }
            """, (n, s) => n.GetSpanOfFirstChild(s), 0, 5, 0, 5);
    }

    [WpfFact]
    public void GetSpanOfNextSiblingTest()
    {
        // Go from 'class' to 'Class1'
        TestNavigator(
            """
            class Class1
            {
            }
            """, (n, s) => n.GetSpanOfNextSibling(s), 0, 5, 6, 6);
    }

    [WpfFact]
    public void GetSpanOfPreviousSiblingTest()
    {
        // Go from '{' to 'Class1'
        TestNavigator(
            """class Class1 { }""", (n, s) => n.GetSpanOfPreviousSibling(s), 13, 1, 6, 6);
    }

    [WpfTheory]
    [InlineData("""Console.WriteLine("{|Significant:$$=============|}");""")]
    [InlineData("""Console.WriteLine("{|Significant:=$$============|}");""")]
    [InlineData("""Console.WriteLine("{|Significant:$$=============|}"u8);""")]
    [InlineData("""Console.WriteLine("{|Significant:=$$============|}"u8);""")]
    [InlineData(""""Console.WriteLine("""{|Significant:$$=============|}""");"""")]
    [InlineData(""""Console.WriteLine("""{|Significant:=$$============|}""");"""")]
    [InlineData(""""Console.WriteLine("""{|Significant:$$=============|}"""u8);"""")]
    [InlineData(""""Console.WriteLine("""{|Significant:=$$============|}"""u8);"""")]
    public void TestClampStringLiteral(string content)
        => AssertExtent(content);

    [WpfTheory, WorkItem("https://github.com/dotnet/roslyn/issues/77401")]
    [InlineData(@"{|Significant:""|}$$")]
    [InlineData(@"{|Significant:""""""|}$$")]
    [InlineData(""""
        """{|Significant:u8|}$$
        """")]
    public void TestClampStringLiteral_Invalid(string content)
        => AssertExtent(content);
}
