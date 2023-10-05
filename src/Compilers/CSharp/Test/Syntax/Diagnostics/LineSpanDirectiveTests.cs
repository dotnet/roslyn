// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class LineSpanDirectiveTests : CSharpTestBase
    {
        [Fact]
        public void LineSpanDirective_SingleLine()
        {
            string sourceA =
@"         A1(); A2(); A3(); //123
//4567890
".NormalizeLineEndings();
            var textA = SourceText.From(sourceA);

            string sourceB =
@"class Program
{
    static void Main()
    {
#line (1, 16) - (1, 26) 14 ""a.cs""
        B1(); A2(); A3(); B4();
        B5();
    }
}
".NormalizeLineEndings();

            var treeB = SyntaxFactory.ParseSyntaxTree(sourceB, path: "b.cs");
            treeB.GetDiagnostics().Verify();

            var actualLineMappings = GetLineMappings(treeB);
            var expectedLineMappings = new[]
            {
                "(0,0)-(3,7) -> : (0,0)-(3,7)",
                "(5,0)-(9,0),14 -> a.cs: (0,15)-(0,26)",
            };
            AssertEx.Equal(expectedLineMappings, actualLineMappings);

            var statements = GetStatementsAndExpressionBodies(treeB);
            var actualTextSpans = statements.SelectAsArray(s => GetTextMapping(textA, treeB, s));
            var expectedTextSpans = new[]
            {
                (@"B1();", @"[|A2(); A3();|]"),
                (@"A2();", @"[|A2();|]"),
                (@"A3();", @"[|A3();|]"),
                (@"B4();", @"[|//123|]"),
                (@"B5();", @"[|0|]"),
            };
            AssertEx.Equal(expectedTextSpans, actualTextSpans);
        }

        [Fact]
        public void LineSpanDirective_MultiLine()
        {
            string sourceA =
@"         A1(); A2(); A3(); //123
//4567890
//ABCDEF
".NormalizeLineEndings();
            var textA = SourceText.From(sourceA);

            string sourceB =
@"class Program
{
    static void Main()
    {
#line (1, 16) - (5, 26) 14 ""a.cs""
        B1(); A2(); A3(); B4();
        B5();
    }
}
".NormalizeLineEndings();

            var treeB = SyntaxFactory.ParseSyntaxTree(sourceB, path: "b.cs");
            treeB.GetDiagnostics().Verify();

            var actualLineMappings = GetLineMappings(treeB);
            var expectedLineMappings = new[]
            {
                "(0,0)-(3,7) -> : (0,0)-(3,7)",
                "(5,0)-(9,0),14 -> a.cs: (0,15)-(4,26)",
            };
            AssertEx.Equal(expectedLineMappings, actualLineMappings);

            var statements = GetStatementsAndExpressionBodies(treeB);
            var actualTextSpans = statements.SelectAsArray(s => GetTextMapping(textA, treeB, s));
            var expectedTextSpans = new[]
            {
                (@"B1();", @"[|A2(); A3(); //123
//4567890
//ABCDEF
|]".NormalizeLineEndings()),
                (@"A2();", @"[|A2();|]"),
                (@"A3();", @"[|A3();|]"),
                (@"B4();", @"[|//123|]"),
                (@"B5();", @"[|0|]"),
            };
            AssertEx.Equal(expectedTextSpans, actualTextSpans);
        }

        [Fact]
        public void InvalidSpans()
        {
            string source =
@"class Program
{
    static void Main()
    {
#line (10, 20) - (10, 20) ""A""
        F();
#line (10, 20) - (10, 19) ""B""
        F();
#line (10, 20) - (9, 20) ""C""
        F();
#line (10, 20) - (11, 19) ""D""
        F();
    }
    static void F() { }
}".NormalizeLineEndings();

            var tree = SyntaxFactory.ParseSyntaxTree(source);
            var comp = CreateCompilation(tree);
            comp.VerifyDiagnostics(
                // (9,18): error CS8939: The #line directive end position must be greater than or equal to the start position
                // #line (10, 20) - (9, 20) "C"
                Diagnostic(ErrorCode.ERR_LineSpanDirectiveEndLessThanStart, "(9, 20)").WithLocation(9, 18),
                // A(11,18): error CS8939: The #line directive end position must be greater than or equal to the start position
                // #line (10, 20) - (10, 19) "B"
                Diagnostic(ErrorCode.ERR_LineSpanDirectiveEndLessThanStart, "(10, 19)").WithLocation(11, 18));

            var actualLineMappings = GetLineMappings(tree);
            var expectedLineMappings = new[]
            {
                "(0,0)-(3,7) -> : (0,0)-(3,7)",
                "(5,0)-(5,14) -> A: (9,19)-(9,20)",
                "(7,0)-(7,14) -> : (7,0)-(7,14)",
                "(9,0)-(9,14) -> : (9,0)-(9,14)",
                "(11,0)-(14,1) -> D: (9,19)-(10,19)",
            };
            AssertEx.Equal(expectedLineMappings, actualLineMappings);
        }

        // 1. First and subsequent spans
        [WorkItem(4747, "https://github.com/dotnet/csharplang/issues/4747")]
        [Fact]
        public void LineSpanDirective_Example1()
        {
            string sourceA =
@"         A();B(
);C();
    D();
".NormalizeLineEndings();
            var textA = SourceText.From(sourceA);

            string sourceB =
@"class Program
{
 static void Main() {
#line (1,10)-(1,15) 2 ""a"" // 3
  A();B(              // 4
);C();                // 5
    D();              // 6
 }
 static void A() { }
 static void B() { }
 static void C() { }
 static void D() { }
}".NormalizeLineEndings();

            var treeB = SyntaxFactory.ParseSyntaxTree(sourceB, path: "b.cs");
            treeB.GetDiagnostics().Verify();

            var actualLineMappings = GetLineMappings(treeB);
            var expectedLineMappings = new[]
            {
                "(0,0)-(2,23) -> : (0,0)-(2,23)",
                "(4,0)-(12,1),2 -> a: (0,9)-(0,15)",
            };
            AssertEx.Equal(expectedLineMappings, actualLineMappings);

            var statements = GetStatementsAndExpressionBodies(treeB);
            var actualTextSpans = statements.SelectAsArray(s => GetTextMapping(textA, treeB, s));
            var expectedTextSpans = new[]
            {
                (@"A();", @"[|A();|]"),
                (@"B(              // 4...", @"[|B(
);|]".NormalizeLineEndings()),
                (@"C();", @"[|C();|]"),
                (@"D();", @"[|D();|]"),
            };
            AssertEx.Equal(expectedTextSpans, actualTextSpans);
        }

        // 2. Character offset
        [WorkItem(4747, "https://github.com/dotnet/csharplang/issues/4747")]
        [Fact]
        public void LineSpanDirective_Example2()
        {
            string sourceA =
@"@page ""/""
@F(() => 1+1,
   () => 2+2
)".NormalizeLineEndings();
            var textA = SourceText.From(sourceA);

            string sourceB =
@"#line hidden
class Page
{
void Render()
{
#line (2,2)-(4,1) 15 ""page.razor"" // spanof('F(...)')
  _builder.Add(F(() => 1+1,       // 5
   () => 2+2                      // 6
));                               // 7
#line hidden
}
}".NormalizeLineEndings();

            var treeB = SyntaxFactory.ParseSyntaxTree(sourceB, path: "page.razor.g.cs");
            treeB.GetDiagnostics().Verify();

            var actualLineMappings = GetLineMappings(treeB);
            var expectedLineMappings = new[]
            {
                "(1,0)-(4,3) -> : (0,0)-(0,0)",
                "(6,0)-(8,40),15 -> page.razor: (1,1)-(3,1)",
                "(10,0)-(11,1) -> : (0,0)-(0,0)",
            };
            AssertEx.Equal(expectedLineMappings, actualLineMappings);

            var textB = SourceText.From(sourceB);
            var actualVisibility = textB.Lines.Select(line => treeB.GetLineVisibility(line.Start)).ToImmutableArray();
            var expectedVisibility = new[]
            {
                LineVisibility.BeforeFirstLineDirective,
                LineVisibility.Hidden,
                LineVisibility.Hidden,
                LineVisibility.Hidden,
                LineVisibility.Hidden,
                LineVisibility.Hidden,
                LineVisibility.Visible,
                LineVisibility.Visible,
                LineVisibility.Visible,
                LineVisibility.Visible,
                LineVisibility.Hidden,
                LineVisibility.Hidden,
            };
            AssertEx.Equal(expectedVisibility, actualVisibility);

            var statements = GetStatementsAndExpressionBodies(treeB);
            var actualTextSpans = statements.SelectAsArray(s => GetTextMapping(textA, treeB, s));
            var expectedTextSpans = new[]
            {
                (@"_builder.Add(F(() => 1+1,       // 5...", @"[|F(() => 1+1,
   () => 2+2
)|]".NormalizeLineEndings()),
                (@"1+1", @"[|1+1|]"),
                (@"2+2", @"[|2+2|]"),
            };
            AssertEx.Equal(expectedTextSpans, actualTextSpans);
        }

        // 3. Razor: Single-line span
        [WorkItem(4747, "https://github.com/dotnet/csharplang/issues/4747")]
        [Fact]
        public void LineSpanDirective_Example3()
        {
            string sourceA =
@"@page ""/""
Time: @DateTime.Now
".NormalizeLineEndings();
            var textA = SourceText.From(sourceA);

            string sourceB =
@"#line hidden
class Page
{
void Render()
{
  _builder.Add(""Time:"");
#line (2,8)-(2,19) 15 ""page.razor"" // spanof('DateTime.Now')
  _builder.Add(DateTime.Now);
#line hidden
}
}".NormalizeLineEndings();

            var treeB = SyntaxFactory.ParseSyntaxTree(sourceB, path: "page.razor.g.cs");
            treeB.GetDiagnostics().Verify();

            var actualLineMappings = GetLineMappings(treeB);
            var expectedLineMappings = new[]
            {
                "(1,0)-(5,26) -> : (0,0)-(0,0)",
                "(7,0)-(7,31),15 -> page.razor: (1,7)-(1,19)",
                "(9,0)-(10,1) -> : (0,0)-(0,0)",
            };
            AssertEx.Equal(expectedLineMappings, actualLineMappings);

            var statements = GetStatementsAndExpressionBodies(treeB);
            var actualTextSpans = statements.SelectAsArray(s => GetTextMapping(textA, treeB, s));
            var expectedTextSpans = new[]
            {
                (@"_builder.Add(""Time:"");", @"[||]"),
                (@"_builder.Add(DateTime.Now);", @"[|DateTime.Now|]"),
            };
            AssertEx.Equal(expectedTextSpans, actualTextSpans);
        }

        // 4. Razor: Multi-line span
        [WorkItem(4747, "https://github.com/dotnet/csharplang/issues/4747")]
        [Fact]
        public void LineSpanDirective_Example4()
        {
            string sourceA =
@"@page ""/""
@JsonToHtml(@""
{
  """"key1"""": """"value1"""",
  """"key2"""": """"value2""""
}"")".NormalizeLineEndings();
            var textA = SourceText.From(sourceA);

            string sourceB =
@"#line hidden
class Page
{
void Render()
{
#line (2,2)-(6,3) 15 ""page.razor"" // spanof('JsonToHtml(...)')
  _builder.Add(JsonToHtml(@""
{
  """"key1"""": """"value1"""",
  """"key2"""": """"value2""""
}""));
#line hidden
}
}".NormalizeLineEndings();

            var treeB = SyntaxFactory.ParseSyntaxTree(sourceB, path: "page.razor.g.cs");
            treeB.GetDiagnostics().Verify();

            var actualLineMappings = GetLineMappings(treeB);
            var expectedLineMappings = new[]
            {
                "(1,0)-(4,3) -> : (0,0)-(0,0)",
                "(6,0)-(10,7),15 -> page.razor: (1,1)-(5,3)",
                "(12,0)-(13,1) -> : (0,0)-(0,0)",
            };
            AssertEx.Equal(expectedLineMappings, actualLineMappings);

            var statements = GetStatementsAndExpressionBodies(treeB);
            var actualTextSpans = statements.SelectAsArray(s => GetTextMapping(textA, treeB, s));
            var expectedTextSpans = new[]
            {
                (@"_builder.Add(JsonToHtml(@""...", @"[|JsonToHtml(@""
{
  """"key1"""": """"value1"""",
  """"key2"""": """"value2""""
}"")|]".NormalizeLineEndings()),
            };
            AssertEx.Equal(expectedTextSpans, actualTextSpans);
        }

        // 5i. Razor: block constructs
        [WorkItem(4747, "https://github.com/dotnet/csharplang/issues/4747")]
        [Fact]
        public void LineSpanDirective_Example5i()
        {
            string sourceA =
@"@Html.Helper(() =>
{
    <p>Hello World</p>
    @DateTime.Now
})".NormalizeLineEndings();
            var textA = SourceText.From(sourceA);

            string sourceB =
@"using System;
class Page
{
    Builder _builder;
    void Execute()
    {
#line (1, 2) - (5, 2) 21 ""a.razor"" // spanof('HtmlHelper(() => { ... })')
        _builder.Add(Html.Helper(() =>
#line 2 ""a.razor"" // lineof('{')
        {
#line (4, 6) - (4, 17) 25 ""a.razor"" // spanof('DateTime.Now')
            _builder.Add(DateTime.Now);
#line 5 ""a.razor"" // lineof('})')
        })
#line hidden
        );
    }
}".NormalizeLineEndings();

            var treeB = SyntaxFactory.ParseSyntaxTree(sourceB, path: "a.razor.g.cs");
            treeB.GetDiagnostics().Verify();

            var actualLineMappings = GetLineMappings(treeB);
            var expectedLineMappings = new[]
            {
                "(0,0)-(5,7) -> : (0,0)-(5,7)",
                "(7,0)-(7,40),21 -> a.razor: (0,1)-(4,2)",
                "(9,0)-(9,11) -> a.razor: (1,0)-(1,11)",
                "(11,0)-(11,41),25 -> a.razor: (3,5)-(3,17)",
                "(13,0)-(13,12) -> a.razor: (4,0)-(4,12)",
                "(15,0)-(17,1) -> : (0,0)-(0,0)",
            };
            AssertEx.Equal(expectedLineMappings, actualLineMappings);

            var statements = GetStatementsAndExpressionBodies(treeB);
            var actualTextSpans = statements.SelectAsArray(s => GetTextMapping(textA, treeB, s));
            var expectedTextSpans = new[]
            {
                (@"_builder.Add(Html.Helper(() =>...", @"[|Html.Helper(() =>
{
    <p>Hello World</p>
    @DateTime.Now
})|]".NormalizeLineEndings()),
                (@"_builder.Add(DateTime.Now);", @"[|DateTime.Now|]"),
            };
            AssertEx.Equal(expectedTextSpans, actualTextSpans);
        }

        private static ImmutableArray<SyntaxNode> GetStatementsAndExpressionBodies(SyntaxTree tree)
        {
            var builder = ArrayBuilder<SyntaxNode>.GetInstance();
            foreach (var syntax in tree.GetRoot().DescendantNodesAndSelf())
            {
                switch (syntax)
                {
                    case ExpressionStatementSyntax:
                        builder.Add(syntax);
                        break;
                    case ParenthesizedLambdaExpressionSyntax lambda:
                        builder.AddIfNotNull(lambda.ExpressionBody);
                        break;
                    case SimpleLambdaExpressionSyntax lambda:
                        builder.AddIfNotNull(lambda.ExpressionBody);
                        break;
                }
            }
            return builder.ToImmutableAndFree();
        }

        private static ImmutableArray<string> GetLineMappings(SyntaxTree tree)
        {
            var directives = tree.GetRoot().DescendantNodesAndSelf(descendIntoTrivia: true).OfType<DirectiveTriviaSyntax>();
            foreach (var directive in directives)
            {
                Assert.NotEqual(SyntaxKind.None, directive.DirectiveNameToken.Kind());
            }
            return tree.GetLineMappings().Select(mapping => mapping.ToString()!).ToImmutableArray();
        }

        private static (string, string) GetTextMapping(SourceText mappedText, SyntaxTree unmappedText, SyntaxNode syntax)
        {
            return (getDescription(syntax), getMapping(mappedText, unmappedText, syntax));

            static string getDescription(SyntaxNode syntax)
            {
                var description = syntax.ToString();
                int index = description.IndexOfAny(new[] { '\r', '\n' });
                return index < 0 ?
                    description :
                    description.Substring(0, index) + "...";
            }

            static string getMapping(SourceText mappedText, SyntaxTree unmappedText, SyntaxNode syntax)
            {
                var mappedLineAndPositionSpan = unmappedText.GetMappedLineSpanAndVisibility(syntax.Span, out _);
                var span = getTextSpan(mappedText.Lines, mappedLineAndPositionSpan.Span);
                return $"[|{mappedText.GetSubText(span)}|]";
            }

            static TextSpan getTextSpan(TextLineCollection lines, LinePositionSpan span)
            {
                return TextSpan.FromBounds(getTextPosition(lines, span.Start), getTextPosition(lines, span.End));
            }

            static int getTextPosition(TextLineCollection lines, LinePosition position)
            {
                if (position.Line < lines.Count)
                {
                    var line = lines[position.Line];
                    return Math.Min(line.Start + position.Character, line.End);
                }
                return (lines.Count == 0) ? 0 : lines[^1].End;
            }
        }

        [Fact]
        public void Diagnostics_01()
        {
            var source =
@"class Program
{
    static void Main()
    {
#line (3, 3) - (6, 6) 8 ""a.txt""
        A();
#line default
        B();
#line (1, 1) - (1, 100) ""b.txt""
        C();
    }
}".NormalizeLineEndings();
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // b.txt(1,9): error CS0103: The name 'C' does not exist in the current context
                //         C();
                Diagnostic(ErrorCode.ERR_NameNotInContext, "C").WithArguments("C").WithLocation(1, 9),
                // a.txt(3,3): error CS0103: The name 'A' does not exist in the current context
                //         A();
                Diagnostic(ErrorCode.ERR_NameNotInContext, "A").WithArguments("A").WithLocation(3, 3),
                // (8,9): error CS0103: The name 'B' does not exist in the current context
                //         B();
                Diagnostic(ErrorCode.ERR_NameNotInContext, "B").WithArguments("B").WithLocation(8, 9));
        }

        [Fact]
        public void Diagnostics_02()
        {
            var source =
@"class Program
{
    static void Main()
    {
#line (100, 1) - (100, ) 1 ""a.txt""
        A();
#line (200, 1) - (200, 100) 2 ""b.txt""
        B();
#line (300, 1) - (300, 100) x ""c.txt""
        C();
    }
}".NormalizeLineEndings();

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (5,24): error CS8938: The #line directive value is missing or out of range
                // #line (100, 1) - (100, ) 1 "a.txt"
                Diagnostic(ErrorCode.ERR_LineSpanDirectiveInvalidValue, ")").WithLocation(5, 24),
                // (6,9): error CS0103: The name 'A' does not exist in the current context
                //         A();
                Diagnostic(ErrorCode.ERR_NameNotInContext, "A").WithArguments("A").WithLocation(6, 9),
                // (10,9): error CS0103: The name 'C' does not exist in the current context
                //         C();
                Diagnostic(ErrorCode.ERR_NameNotInContext, "C").WithArguments("C").WithLocation(10, 9),
                // b.txt(200,7): error CS0103: The name 'B' does not exist in the current context
                //         B();
                Diagnostic(ErrorCode.ERR_NameNotInContext, "B").WithArguments("B").WithLocation(200, 7),
                // b.txt(201,29): error CS1578: Quoted file name, single-line comment or end-of-line expected
                // #line (300, 1) - (300, 100) x "c.txt"
                Diagnostic(ErrorCode.ERR_MissingPPFile, "x").WithLocation(201, 29));

            var tree = comp.SyntaxTrees[0];
            var actualLineMappings = GetLineMappings(tree);
            var expectedLineMappings = new[]
            {
                "(0,0)-(3,7) -> : (0,0)-(3,7)",
                "(5,0)-(5,14) -> : (5,0)-(5,14)",
                "(7,0)-(7,14),2 -> b.txt: (199,0)-(199,100)",
                "(9,0)-(11,1) -> : (9,0)-(11,1)"
            };
            AssertEx.Equal(expectedLineMappings, actualLineMappings);
        }

        [Fact, WorkItem("https://github.com/dotnet/razor/issues/9051")]
        public void Diagnostics_03()
        {
            var source =
@"class Program
{
    static void Main()
    {
#line (3, 3) - (6, 6) ""a.txt""
A(); // 1
#line (3, 3) - (6, 6) 8 ""a.txt""
        A(); // 2
#line 3
  A(); // 3
    }
}".NormalizeLineEndings();
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // a.txt(3,3): error CS0103: The name 'A' does not exist in the current context
                // A(); // 1
                Diagnostic(ErrorCode.ERR_NameNotInContext, "A").WithArguments("A").WithLocation(3, 3),
                // a.txt(3,3): error CS0103: The name 'A' does not exist in the current context
                //         A(); // 2
                Diagnostic(ErrorCode.ERR_NameNotInContext, "A").WithArguments("A").WithLocation(3, 3),
                // (3,3): error CS0103: The name 'A' does not exist in the current context
                //   A(); // 3
                Diagnostic(ErrorCode.ERR_NameNotInContext, "A").WithArguments("A").WithLocation(3, 3));
        }

        [Fact]
        public void SequencePoints()
        {
            var source =
@"class Program
{
    static void Main()
    {
#line (3, 3) - (6, 6) 8 ""a.txt""
        A();
#line default
        B();
#line (1, 1) - (1, 100) ""b.txt""
        C();
#line (2, 3) - (2, 4) 9 ""a.txt""
        D();
    }
    static void A() { }
    static void B() { }
    static void C() { }
    static void D() { }
}".NormalizeLineEndings();
            var verifier = CompileAndVerify(source, options: TestOptions.DebugDll);
            verifier.VerifyIL("Program.Main", sequencePoints: "Program.Main", expectedIL:
@"{
  // Code size       26 (0x1a)
  .maxstack  0
 -IL_0000:  nop
 -IL_0001:  call       ""void Program.A()""
  IL_0006:  nop
 -IL_0007:  call       ""void Program.B()""
  IL_000c:  nop
 -IL_000d:  call       ""void Program.C()""
  IL_0012:  nop
 -IL_0013:  call       ""void Program.D()""
  IL_0018:  nop
 -IL_0019:  ret
}");
            verifier.VerifyPdb("Program.Main", expectedPdb:
@"<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
    <file id=""2"" name=""a.txt"" language=""C#"" />
    <file id=""3"" name=""b.txt"" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Program"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""5"" endLine=""4"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""3"" startColumn=""3"" endLine=""3"" endColumn=""7"" document=""2"" />
        <entry offset=""0x7"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""13"" document=""1"" />
        <entry offset=""0xd"" startLine=""1"" startColumn=""9"" endLine=""1"" endColumn=""13"" document=""3"" />
        <entry offset=""0x13"" startLine=""2"" startColumn=""3"" endLine=""2"" endColumn=""5"" document=""2"" />
        <entry offset=""0x19"" startLine=""3"" startColumn=""5"" endLine=""3"" endColumn=""6"" document=""2"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>
");
        }
    }
}
