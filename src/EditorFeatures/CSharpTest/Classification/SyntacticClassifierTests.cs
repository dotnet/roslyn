// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Classification;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.Editor.UnitTests.Classification.FormattedClassifications;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Classification;

public sealed partial class SyntacticClassifierTests : AbstractCSharpClassifierTests
{
    protected override async Task<ImmutableArray<ClassifiedSpan>> GetClassificationSpansAsync(string code, ImmutableArray<TextSpan> spans, ParseOptions? options, TestHost testHost)
    {
        using var workspace = CreateWorkspace(code, options, testHost);
        var document = workspace.CurrentSolution.Projects.First().Documents.First();

        return await GetSyntacticClassificationsAsync(document, spans);
    }

    [Theory, CombinatorialData]
    public Task VarAtTypeMemberLevel(TestHost testHost)
        => TestAsync(
            """
            class C
            {
                var goo }
            """,
            testHost,
            Keyword("class"),
            Class("C"),
            Punctuation.OpenCurly,
            Identifier("var"),
            Field("goo"),
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task TestNamespace(TestHost testHost)
        => TestAsync(
            """
            namespace N
            {
            }
            """,
            testHost,
            Keyword("namespace"),
            Namespace("N"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task TestFileScopedNamespace(TestHost testHost)
        => TestAsync(
            """
            namespace N;

            """,
            testHost,
            Keyword("namespace"),
            Namespace("N"),
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    public Task VarAsLocalVariableType(TestHost testHost)
        => TestInMethodAsync("var goo = 42",
            testHost,
            Keyword("var"),
            Local("goo"),
            Operators.Equals,
            Number("42"));

    [Theory, CombinatorialData]
    public Task VarOptimisticallyColored(TestHost testHost)
        => TestInMethodAsync("var",
            testHost,
            Keyword("var"));

    [Theory, CombinatorialData]
    public Task VarNotColoredInClass(TestHost testHost)
        => TestInClassAsync("var",
            testHost,
            Identifier("var"));

    [Theory, CombinatorialData]
    public Task VarInsideLocalAndExpressions(TestHost testHost)
        => TestInMethodAsync(
@"var var = (var)var as var;",
            testHost,
            Keyword("var"),
            Local("var"),
            Operators.Equals,
            Punctuation.OpenParen,
            Identifier("var"),
            Punctuation.CloseParen,
            Identifier("var"),
            Keyword("as"),
            Identifier("var"),
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    public Task VarAsMethodParameter(TestHost testHost)
        => TestAsync(
            """
            class C
            {
                void M(var v)
                {
                }
            }
            """,
            testHost,
            Keyword("class"),
            Class("C"),
            Punctuation.OpenCurly,
            Keyword("void"),
            Method("M"),
            Punctuation.OpenParen,
            Identifier("var"),
            Parameter("v"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task YieldYield(TestHost testHost)
        => TestAsync(
            """
            using System.Collections.Generic;

            class yield
            {
                IEnumerable<yield> M()
                {
                    yield yield = new yield();
                    yield return yield;
                }
            }
            """,
            testHost,
            Keyword("using"),
            Identifier("System"),
            Operators.Dot,
            Identifier("Collections"),
            Operators.Dot,
            Identifier("Generic"),
            Punctuation.Semicolon,
            Keyword("class"),
            Class("yield"),
            Punctuation.OpenCurly,
            Identifier("IEnumerable"),
            Punctuation.OpenAngle,
            Identifier("yield"),
            Punctuation.CloseAngle,
            Method("M"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Identifier("yield"),
            Local("yield"),
            Operators.Equals,
            Keyword("new"),
            Identifier("yield"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.Semicolon,
            ControlKeyword("yield"),
            ControlKeyword("return"),
            Identifier("yield"),
            Punctuation.Semicolon,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task YieldYieldAsSpans(TestHost testHost)
        => TestAsync(
            """
            using System.Collections.Generic;

            class yield
            {
                IEnumerable<yield> M()
                {
                    [|yield yield = new yield();|]
                    [|yield return yield;|]
                }
            }
            """,
            testHost,
            Identifier("yield"),
            Local("yield"),
            Operators.Equals,
            Keyword("new"),
            Identifier("yield"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.Semicolon,
            ControlKeyword("yield"),
            ControlKeyword("return"),
            Identifier("yield"),
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    public Task YieldReturn(TestHost testHost)
        => TestInMethodAsync("yield return 42",
            testHost,
            ControlKeyword("yield"),
            ControlKeyword("return"),
            Number("42"));

    [Theory, CombinatorialData]
    public Task YieldFixed(TestHost testHost)
        => TestInMethodAsync(
            """
            yield return this.items[0]; yield break; fixed (int* i = 0) {
            }
            """,
            testHost,
            ControlKeyword("yield"),
            ControlKeyword("return"),
            Keyword("this"),
            Operators.Dot,
            Identifier("items"),
            Punctuation.OpenBracket,
            Number("0"),
            Punctuation.CloseBracket,
            Punctuation.Semicolon,
            ControlKeyword("yield"),
            ControlKeyword("break"),
            Punctuation.Semicolon,
            Keyword("fixed"),
            Punctuation.OpenParen,
            Keyword("int"),
            Operators.Asterisk,
            Local("i"),
            Operators.Equals,
            Number("0"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/40741")]
    public Task TestAwait(TestHost testHost)
        => TestAsync(
            """
            using System.Threading.Tasks;

            class X
            {
                async Task M()
                {
                    await M();
                }
            }
            """,
            testHost,
            Keyword("using"),
            Identifier("System"),
            Operators.Dot,
            Identifier("Threading"),
            Operators.Dot,
            Identifier("Tasks"),
            Punctuation.Semicolon,
            Keyword("class"),
            Class("X"),
            Punctuation.OpenCurly,
            Keyword("async"),
            Identifier("Task"),
            Method("M"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            ControlKeyword("await"),
            Identifier("M"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.Semicolon,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task PartialClass(TestHost testHost)
        => TestAsync("public partial class Goo",
            testHost,
            Keyword("public"),
            Keyword("partial"),
            Keyword("class"),
            Class("Goo"));

    [Theory, CombinatorialData]
    public Task PartialMethod(TestHost testHost)
        => TestInClassAsync(
            """
            public partial void M()
            {
            }
            """,
            testHost,
            Keyword("public"),
            Keyword("partial"),
            Keyword("void"),
            Method("M"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    /// <summary>
    /// Partial is only valid in a type declaration
    /// </summary>
    [Theory, CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/536313")]
    public Task PartialAsLocalVariableType(TestHost testHost)
        => TestInMethodAsync(
@"partial p1 = 42;",
            testHost,
            Identifier("partial"),
            Local("p1"),
            Operators.Equals,
            Number("42"),
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    public Task PartialClassStructInterface(TestHost testHost)
        => TestAsync(
            """
            partial class T1
            {
            }

            partial struct T2
            {
            }

            partial interface T3
            {
            }
            """,
            testHost,
            Keyword("partial"),
            Keyword("class"),
            Class("T1"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Keyword("partial"),
            Keyword("struct"),
            Struct("T2"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Keyword("partial"),
            Keyword("interface"),
            Interface("T3"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    private static readonly string[] s_contextualKeywordsOnlyValidInMethods = ["where", "from", "group", "join", "select", "into", "let", "by", "orderby", "on", "equals", "ascending", "descending"];

    /// <summary>
    /// Check for items only valid within a method declaration
    /// </summary>
    [Theory, CombinatorialData]
    public async Task ContextualKeywordsOnlyValidInMethods(TestHost testHost)
    {
        foreach (var kw in s_contextualKeywordsOnlyValidInMethods)
        {
            await TestInNamespaceAsync(kw + " goo",
                testHost,
                Identifier(kw),
                Field("goo"));
        }
    }

    [Theory, CombinatorialData]
    public Task VerbatimStringLiterals1(TestHost testHost)
        => TestInMethodAsync("""
            @"goo"
            """,
            testHost,
            Verbatim("""
                @"goo"
                """));

    [Theory, CombinatorialData]
    public Task VerbatimStringLiteralsUtf8_01(TestHost testHost)
        => TestInMethodAsync(@"@""goo""u8",
            testHost,
            Verbatim("""
                @"goo"
                """),
            Keyword("u8"));

    [Theory, CombinatorialData]
    public Task VerbatimStringLiteralsUtf8_02(TestHost testHost)
        => TestInMethodAsync(@"@""goo""U8",
            testHost,
            Verbatim("""
                @"goo"
                """),
            Keyword("U8"));

    /// <summary>
    /// Should show up as soon as we get the @\" typed out
    /// </summary>
    [Theory, CombinatorialData]
    public Task VerbatimStringLiterals2(TestHost testHost)
        => TestAsync("""
            @"
            """,
            testHost,
            Verbatim("""
                @"
                """));

    /// <summary>
    /// Parser does not currently support strings of this type
    /// </summary>
    [Theory, CombinatorialData]
    public Task VerbatimStringLiterals3(TestHost testHost)
        => TestAsync("""
            goo @"
            """,
            testHost,
            Identifier("goo"),
            Verbatim("""
                @"
                """));

    /// <summary>
    /// Uncompleted ones should span new lines
    /// </summary>
    [Theory, CombinatorialData]
    public Task VerbatimStringLiterals4(TestHost testHost)
        => TestAsync("""


            @" goo bar 


            """,
            testHost,
            Verbatim("""
                @" goo bar 


                """));

    [Theory, CombinatorialData]
    public Task VerbatimStringLiterals5(TestHost testHost)
        => TestInMethodAsync("""


            @" goo bar
            and 
            on a new line " 
            more stuff
            """,
            testHost,
            Verbatim("""
                @" goo bar
                and 
                on a new line "
                """),
            Identifier("more"),
            Local("stuff"));

    [Theory, CombinatorialData]
    public Task VerbatimStringLiteralsUtf8_03(TestHost testHost)
        => TestInMethodAsync("""


            @" goo bar
            and 
            on a new line "u8 
            more stuff
            """,
            testHost,
            Verbatim("""
                @" goo bar
                and 
                on a new line "
                """),
            Keyword("u8"),
            Identifier("more"),
            Local("stuff"));

    [Theory, CombinatorialData]
    public Task VerbatimStringLiteralsUtf8_04(TestHost testHost)
        => TestInMethodAsync("""


            @" goo bar
            and 
            on a new line "U8 
            more stuff
            """,
            testHost,
            Verbatim("""
                @" goo bar
                and 
                on a new line "
                """),
            Keyword("U8"),
            Identifier("more"),
            Local("stuff"));

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/44423")]
    [CombinatorialData]
    public async Task VerbatimStringLiterals6(bool script, TestHost testHost)
    {
        var code = @"string s = @""""""/*"";";

        var parseOptions = script ? Options.Script : null;

        await TestAsync(
            code,
            code,
            testHost,
            parseOptions,
            Keyword("string"),
            script ? Field("s") : Local("s"),
            Operators.Equals,
            Verbatim(""""
                @"""/*"
                """"),
            Punctuation.Semicolon);
    }

    [Theory, CombinatorialData]
    public async Task VerbatimStringLiteralsUtf8_05(bool script, TestHost testHost)
    {
        var code = @"string s = @""""""/*""u8;";

        var parseOptions = script ? Options.Script : null;

        await TestAsync(
            code,
            code,
            testHost,
            parseOptions,
            Keyword("string"),
            script ? Field("s") : Local("s"),
            Operators.Equals,
            Verbatim(""""
                @"""/*"
                """"),
            Keyword("u8"),
            Punctuation.Semicolon);
    }

    [Theory, CombinatorialData]
    public async Task VerbatimStringLiteralsUtf8_06(bool script, TestHost testHost)
    {
        var code = @"string s = @""""""/*""u8;";

        var parseOptions = script ? Options.Script : null;

        await TestAsync(
            code,
            code,
            testHost,
            parseOptions,
            Keyword("string"),
            script ? Field("s") : Local("s"),
            Operators.Equals,
            Verbatim(""""
                @"""/*"
                """"),
            Keyword("u8"),
            Punctuation.Semicolon);
    }

    [Theory, CombinatorialData]
    public Task StringLiteral1(TestHost testHost)
        => TestAsync("""
            "goo"
            """,
            testHost,
            String("""
                "goo"
                """));

    [Theory, CombinatorialData]
    public Task StringLiteralUtf8_01(TestHost testHost)
        => TestAsync("""
            "goo"u8
            """,
            testHost,
            String("""
                "goo"
                """),
            Keyword("u8"));

    [Theory, CombinatorialData]
    public Task StringLiteralUtf8_02(TestHost testHost)
        => TestAsync("""
            "goo"U8
            """,
            testHost,
            String("""
                "goo"
                """),
            Keyword("U8"));

    [Theory, CombinatorialData]
    public Task StringLiteral2(TestHost testHost)
        => TestAsync("""
            ""
            """,
            testHost,
            String("""
                ""
                """));

    [Theory, CombinatorialData]
    public Task StringLiteralUtf8_03(TestHost testHost)
        => TestAsync("""
            ""u8
            """,
            testHost,
            String("""
                ""
                """),
            Keyword("u8"));

    [Theory, CombinatorialData]
    public Task StringLiteralUtf8_04(TestHost testHost)
        => TestAsync("""
            ""U8
            """,
            testHost,
            String("""
                ""
                """),
            Keyword("U8"));

    [Theory, CombinatorialData]
    public Task CharacterLiteral1(TestHost testHost)
        => TestInMethodAsync(@"'f'",
            testHost,
            String("'f'"));

    [Theory, CombinatorialData]
    public Task LinqFrom1(TestHost testHost)
        => TestInExpressionAsync(@"from it in goo",
            testHost,
            Keyword("from"),
            Identifier("it"),
            Keyword("in"),
            Identifier("goo"));

    [Theory, CombinatorialData]
    public Task LinqFrom2(TestHost testHost)
        => TestInExpressionAsync(@"from it in goo.Bar()",
            testHost,
            Keyword("from"),
            Identifier("it"),
            Keyword("in"),
            Identifier("goo"),
            Operators.Dot,
            Identifier("Bar"),
            Punctuation.OpenParen,
            Punctuation.CloseParen);

    [Theory, CombinatorialData]
    public Task LinqFrom3(TestHost testHost)
        => TestInMethodAsync(@"from it in ",
            testHost,
            Keyword("from"),
            Identifier("it"),
            Keyword("in"));

    [Theory, CombinatorialData]
    public Task LinqFrom4(TestHost testHost)
        => TestInExpressionAsync(@"from it in ",
            testHost,
            Keyword("from"),
            Identifier("it"),
            Keyword("in"));

    [Theory, CombinatorialData]
    public Task LinqWhere1(TestHost testHost)
        => TestInExpressionAsync("from it in goo where it > 42",
            testHost,
            Keyword("from"),
            Identifier("it"),
            Keyword("in"),
            Identifier("goo"),
            Keyword("where"),
            Identifier("it"),
            Operators.GreaterThan,
            Number("42"));

    [Theory, CombinatorialData]
    public Task LinqWhere2(TestHost testHost)
        => TestInExpressionAsync("""
            from it in goo where it > "bar"
            """,
            testHost,
            Keyword("from"),
            Identifier("it"),
            Keyword("in"),
            Identifier("goo"),
            Keyword("where"),
            Identifier("it"),
            Operators.GreaterThan,
            String("""
                "bar"
                """));

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/44423")]
    [CombinatorialData]
    public async Task VarContextualKeywordAtNamespaceLevel(bool script, TestHost testHost)
    {
        var code = @"var goo = 2;";

        var parseOptions = script ? Options.Script : null;

        await TestAsync(code,
            code,
            testHost,
            parseOptions,
            script ? Identifier("var") : Keyword("var"),
            script ? Field("goo") : Local("goo"),
            Operators.Equals,
            Number("2"),
            Punctuation.Semicolon);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/44423")]
    [CombinatorialData]
    public async Task LinqKeywordsAtNamespaceLevel(bool script, TestHost testHost)
    {
        // the contextual keywords are actual keywords since we parse top level field declaration and only give a semantic error
        var code = """
            object goo = from goo in goo
                         join goo in goo on goo equals goo
                         group goo by goo into goo
                         let goo = goo
                         where goo
                         orderby goo ascending, goo descending
                         select goo;
            """;

        var parseOptions = script ? Options.Script : null;

        await TestAsync(
            code,
            code,
            testHost,
            parseOptions,
            Keyword("object"),
            script ? Field("goo") : Local("goo"),
            Operators.Equals,
            Keyword("from"),
            Identifier("goo"),
            Keyword("in"),
            Identifier("goo"),
            Keyword("join"),
            Identifier("goo"),
            Keyword("in"),
            Identifier("goo"),
            Keyword("on"),
            Identifier("goo"),
            Keyword("equals"),
            Identifier("goo"),
            Keyword("group"),
            Identifier("goo"),
            Keyword("by"),
            Identifier("goo"),
            Keyword("into"),
            Identifier("goo"),
            Keyword("let"),
            Identifier("goo"),
            Operators.Equals,
            Identifier("goo"),
            Keyword("where"),
            Identifier("goo"),
            Keyword("orderby"),
            Identifier("goo"),
            Keyword("ascending"),
            Punctuation.Comma,
            Identifier("goo"),
            Keyword("descending"),
            Keyword("select"),
            Identifier("goo"),
            Punctuation.Semicolon);
    }

    [Theory, CombinatorialData]
    public Task ContextualKeywordsAsFieldName(TestHost testHost)
        => TestAsync(
            """
            class C
            {
                int yield, get, set, value, add, remove, global, partial, where, alias;
            }
            """,
            testHost,
            Keyword("class"),
            Class("C"),
            Punctuation.OpenCurly,
            Keyword("int"),
            Field("yield"),
            Punctuation.Comma,
            Field("get"),
            Punctuation.Comma,
            Field("set"),
            Punctuation.Comma,
            Field("value"),
            Punctuation.Comma,
            Field("add"),
            Punctuation.Comma,
            Field("remove"),
            Punctuation.Comma,
            Field("global"),
            Punctuation.Comma,
            Field("partial"),
            Punctuation.Comma,
            Field("where"),
            Punctuation.Comma,
            Field("alias"),
            Punctuation.Semicolon,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task LinqKeywordsInFieldInitializer(TestHost testHost)
        => TestAsync(
            """
            class C
            {
                int a = from a in a
                        join a in a on a equals a
                        group a by a into a
                        let a = a
                        where a
                        orderby a ascending, a descending
                        select a;
            }
            """,
            testHost,
            Keyword("class"),
            Class("C"),
            Punctuation.OpenCurly,
            Keyword("int"),
            Field("a"),
            Operators.Equals,
            Keyword("from"),
            Identifier("a"),
            Keyword("in"),
            Identifier("a"),
            Keyword("join"),
            Identifier("a"),
            Keyword("in"),
            Identifier("a"),
            Keyword("on"),
            Identifier("a"),
            Keyword("equals"),
            Identifier("a"),
            Keyword("group"),
            Identifier("a"),
            Keyword("by"),
            Identifier("a"),
            Keyword("into"),
            Identifier("a"),
            Keyword("let"),
            Identifier("a"),
            Operators.Equals,
            Identifier("a"),
            Keyword("where"),
            Identifier("a"),
            Keyword("orderby"),
            Identifier("a"),
            Keyword("ascending"),
            Punctuation.Comma,
            Identifier("a"),
            Keyword("descending"),
            Keyword("select"),
            Identifier("a"),
            Punctuation.Semicolon,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task LinqKeywordsAsTypeName(TestHost testHost)
        => TestAsync(
            """
            class var
            {
            }

            struct from
            {
            }

            interface join
            {
            }

            enum on
            {
            }

            delegate equals { }
            class group
            {
            }

            class by
            {
            }

            class into
            {
            }

            class let
            {
            }

            class where
            {
            }

            class orderby
            {
            }

            class ascending
            {
            }

            class descending
            {
            }

            class select
            {
            }
            """,
            testHost,
            Keyword("class"),
            Class("var"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Keyword("struct"),
            Struct("from"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Keyword("interface"),
            Interface("join"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Keyword("enum"),
            Enum("on"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Keyword("delegate"),
            Identifier("equals"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Keyword("class"),
            Class("group"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Keyword("class"),
            Class("by"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Keyword("class"),
            Class("into"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Keyword("class"),
            Class("let"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Keyword("class"),
            Class("where"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Keyword("class"),
            Class("orderby"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Keyword("class"),
            Class("ascending"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Keyword("class"),
            Class("descending"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Keyword("class"),
            Class("select"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task LinqKeywordsAsMethodParameters(TestHost testHost)
        => TestAsync(
            """
            class C
            {
                orderby M(var goo, from goo, join goo, on goo, equals goo, group goo, by goo, into goo, let goo, where goo, orderby goo, ascending goo, descending goo, select goo)
                {
                }
            }
            """,
            testHost,
            Keyword("class"),
            Class("C"),
            Punctuation.OpenCurly,
            Identifier("orderby"),
            Method("M"),
            Punctuation.OpenParen,
            Identifier("var"),
            Parameter("goo"),
            Punctuation.Comma,
            Identifier("from"),
            Parameter("goo"),
            Punctuation.Comma,
            Identifier("join"),
            Parameter("goo"),
            Punctuation.Comma,
            Identifier("on"),
            Parameter("goo"),
            Punctuation.Comma,
            Identifier("equals"),
            Parameter("goo"),
            Punctuation.Comma,
            Identifier("group"),
            Parameter("goo"),
            Punctuation.Comma,
            Identifier("by"),
            Parameter("goo"),
            Punctuation.Comma,
            Identifier("into"),
            Parameter("goo"),
            Punctuation.Comma,
            Identifier("let"),
            Parameter("goo"),
            Punctuation.Comma,
            Identifier("where"),
            Parameter("goo"),
            Punctuation.Comma,
            Identifier("orderby"),
            Parameter("goo"),
            Punctuation.Comma,
            Identifier("ascending"),
            Parameter("goo"),
            Punctuation.Comma,
            Identifier("descending"),
            Parameter("goo"),
            Punctuation.Comma,
            Identifier("select"),
            Parameter("goo"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task LinqKeywordsInLocalVariableDeclarations(TestHost testHost)
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    var goo = (var)goo as var;
                    from goo = (from)goo as from;
                    join goo = (join)goo as join;
                    on goo = (on)goo as on;
                    equals goo = (equals)goo as equals;
                    group goo = (group)goo as group;
                    by goo = (by)goo as by;
                    into goo = (into)goo as into;
                    orderby goo = (orderby)goo as orderby;
                    ascending goo = (ascending)goo as ascending;
                    descending goo = (descending)goo as descending;
                    select goo = (select)goo as select;
                }
            }
            """,
            testHost,
            Keyword("class"),
            Class("C"),
            Punctuation.OpenCurly,
            Keyword("void"),
            Method("M"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Keyword("var"),
            Local("goo"),
            Operators.Equals,
            Punctuation.OpenParen,
            Identifier("var"),
            Punctuation.CloseParen,
            Identifier("goo"),
            Keyword("as"),
            Identifier("var"),
            Punctuation.Semicolon,
            Identifier("from"),
            Local("goo"),
            Operators.Equals,
            Punctuation.OpenParen,
            Identifier("from"),
            Punctuation.CloseParen,
            Identifier("goo"),
            Keyword("as"),
            Identifier("from"),
            Punctuation.Semicolon,
            Identifier("join"),
            Local("goo"),
            Operators.Equals,
            Punctuation.OpenParen,
            Identifier("join"),
            Punctuation.CloseParen,
            Identifier("goo"),
            Keyword("as"),
            Identifier("join"),
            Punctuation.Semicolon,
            Identifier("on"),
            Local("goo"),
            Operators.Equals,
            Punctuation.OpenParen,
            Identifier("on"),
            Punctuation.CloseParen,
            Identifier("goo"),
            Keyword("as"),
            Identifier("on"),
            Punctuation.Semicolon,
            Identifier("equals"),
            Local("goo"),
            Operators.Equals,
            Punctuation.OpenParen,
            Identifier("equals"),
            Punctuation.CloseParen,
            Identifier("goo"),
            Keyword("as"),
            Identifier("equals"),
            Punctuation.Semicolon,
            Identifier("group"),
            Local("goo"),
            Operators.Equals,
            Punctuation.OpenParen,
            Identifier("group"),
            Punctuation.CloseParen,
            Identifier("goo"),
            Keyword("as"),
            Identifier("group"),
            Punctuation.Semicolon,
            Identifier("by"),
            Local("goo"),
            Operators.Equals,
            Punctuation.OpenParen,
            Identifier("by"),
            Punctuation.CloseParen,
            Identifier("goo"),
            Keyword("as"),
            Identifier("by"),
            Punctuation.Semicolon,
            Identifier("into"),
            Local("goo"),
            Operators.Equals,
            Punctuation.OpenParen,
            Identifier("into"),
            Punctuation.CloseParen,
            Identifier("goo"),
            Keyword("as"),
            Identifier("into"),
            Punctuation.Semicolon,
            Identifier("orderby"),
            Local("goo"),
            Operators.Equals,
            Punctuation.OpenParen,
            Identifier("orderby"),
            Punctuation.CloseParen,
            Identifier("goo"),
            Keyword("as"),
            Identifier("orderby"),
            Punctuation.Semicolon,
            Identifier("ascending"),
            Local("goo"),
            Operators.Equals,
            Punctuation.OpenParen,
            Identifier("ascending"),
            Punctuation.CloseParen,
            Identifier("goo"),
            Keyword("as"),
            Identifier("ascending"),
            Punctuation.Semicolon,
            Identifier("descending"),
            Local("goo"),
            Operators.Equals,
            Punctuation.OpenParen,
            Identifier("descending"),
            Punctuation.CloseParen,
            Identifier("goo"),
            Keyword("as"),
            Identifier("descending"),
            Punctuation.Semicolon,
            Identifier("select"),
            Local("goo"),
            Operators.Equals,
            Punctuation.OpenParen,
            Identifier("select"),
            Punctuation.CloseParen,
            Identifier("goo"),
            Keyword("as"),
            Identifier("select"),
            Punctuation.Semicolon,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task LinqKeywordsAsFieldNames(TestHost testHost)
        => TestAsync(
            """
            class C
            {
                int var, from, join, on, into, equals, let, orderby, ascending, descending, select, group, by, partial;
            }
            """,
            testHost,
            Keyword("class"),
            Class("C"),
            Punctuation.OpenCurly,
            Keyword("int"),
            Field("var"),
            Punctuation.Comma,
            Field("from"),
            Punctuation.Comma,
            Field("join"),
            Punctuation.Comma,
            Field("on"),
            Punctuation.Comma,
            Field("into"),
            Punctuation.Comma,
            Field("equals"),
            Punctuation.Comma,
            Field("let"),
            Punctuation.Comma,
            Field("orderby"),
            Punctuation.Comma,
            Field("ascending"),
            Punctuation.Comma,
            Field("descending"),
            Punctuation.Comma,
            Field("select"),
            Punctuation.Comma,
            Field("group"),
            Punctuation.Comma,
            Field("by"),
            Punctuation.Comma,
            Field("partial"),
            Punctuation.Semicolon,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task LinqKeywordsAtFieldLevelInvalid(TestHost testHost)
        => TestAsync(
            """
            class C
            {
                string Property { from a in a join a in a on a equals a group a by a into a let a = a where a orderby a ascending, 
            a descending select a; }
            }
            """,
            testHost,
            Keyword("class"),
            Class("C"),
            Punctuation.OpenCurly,
            Keyword("string"),
            Property("Property"),
            Punctuation.OpenCurly,
            Identifier("from"),
            Identifier("a"),
            Keyword("in"),
            Identifier("a"),
            Identifier("join"),
            Identifier("a"),
            Keyword("in"),
            Identifier("a"),
            Identifier("on"),
            Identifier("a"),
            Identifier("equals"),
            Identifier("a"),
            Identifier("group"),
            Identifier("a"),
            Identifier("by"),
            Identifier("a"),
            Identifier("into"),
            Identifier("a"),
            Identifier("let"),
            Identifier("a"),
            Operators.Equals,
            Identifier("a"),
            Identifier("where"),
            Identifier("a"),
            Identifier("orderby"),
            Identifier("a"),
            Identifier("ascending"),
            Punctuation.Comma,
            Identifier("a"),
            Identifier("descending"),
            Identifier("select"),
            Identifier("a"),
            Punctuation.Semicolon,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task CommentSingle(TestHost testHost)
        => TestAsync("// goo",
            testHost,
            Comment("// goo"));

    [Theory, CombinatorialData]
    public Task CommentAsTrailingTrivia1(TestHost testHost)
        => TestAsync("class Bar { // goo",
            testHost,
            Keyword("class"),
            Class("Bar"),
            Punctuation.OpenCurly,
            Comment("// goo"));

    [Theory, CombinatorialData]
    public Task CommentAsLeadingTrivia1(TestHost testHost)
        => TestAsync("""

            class Bar { 
              // goo
              void Method1() { }
            }
            """,
            testHost,
            Keyword("class"),
            Class("Bar"),
            Punctuation.OpenCurly,
            Comment("// goo"),
            Keyword("void"),
            Method("Method1"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public async Task ShebangAsFirstCommentInScript(TestHost testHost)
    {
        var code = """
            #!/usr/bin/env scriptcs
            System.Console.WriteLine();
            """;

        var expected = new[]
        {
            Comment("#!/usr/bin/env scriptcs"),
            Identifier("System"),
            Operators.Dot,
            Identifier("Console"),
            Operators.Dot,
            Identifier("WriteLine"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.Semicolon
        };

        await TestAsync(code, code, testHost, Options.Script, expected);
    }

    [Theory, CombinatorialData]
    public async Task ShebangAsFirstCommentInNonScript(TestHost testHost)
    {
        var code = """
            #!/usr/bin/env scriptcs
            System.Console.WriteLine();
            """;

        var expected = new[]
        {
            Comment("#!/usr/bin/env scriptcs"),
            Identifier("System"),
            Operators.Dot,
            Identifier("Console"),
            Operators.Dot,
            Identifier("WriteLine"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.Semicolon
        };

        await TestAsync(code, code, testHost, Options.Regular, expected);
    }

    [Theory, CombinatorialData]
    public async Task ShebangNotAsFirstCommentInScript(TestHost testHost)
    {
        var code = """
             #!/usr/bin/env scriptcs
            System.Console.WriteLine();
            """;

        var expected = new[]
        {
            Comment("#!/usr/bin/env scriptcs"),
            Identifier("System"),
            Operators.Dot,
            Identifier("Console"),
            Operators.Dot,
            Identifier("WriteLine"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.Semicolon
        };

        await TestAsync(code, code, testHost, Options.Script, expected);
    }

    [Theory, CombinatorialData]
    public Task IgnoredDirective_01(TestHost testHost)
        => TestAsync("""
            #:unknown // comment
            Console.Write();
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword(":"),
            PPKeyword("unknown"),
            String(" // comment"),
            Identifier("Console"),
            Operators.Dot,
            Identifier("Write"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    public Task IgnoredDirective_02(TestHost testHost)
        => TestAsync("""
            #:sdk Test 2.1.0
            Console.Write();
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword(":"),
            PPKeyword("sdk"),
            String(" Test 2.1.0"),
            Identifier("Console"),
            Operators.Dot,
            Identifier("Write"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    public Task IgnoredDirective_03(TestHost testHost)
        => TestAsync("""
            #:no-space
            Console.Write();
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword(":"),
            PPKeyword("no-space"),
            Identifier("Console"),
            Operators.Dot,
            Identifier("Write"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    public Task IgnoredDirective_04(TestHost testHost)
        => TestAsync($"""
            #:sdk{'\t'}Test 2.1.0
            Console.Write();
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword(":"),
            PPKeyword("sdk"),
            String("\tTest 2.1.0"),
            Identifier("Console"),
            Operators.Dot,
            Identifier("Write"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    public Task CommentAsMethodBodyContent(TestHost testHost)
        => TestAsync("""

            class Bar { 
              void Method1() {
            // goo
            }
            }
            """,
            testHost,
            Keyword("class"),
            Class("Bar"),
            Punctuation.OpenCurly,
            Keyword("void"),
            Method("Method1"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Comment("// goo"),
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task CommentMix1(TestHost testHost)
        => TestAsync(
            """
            // comment1 /*
            class cl
            {
            }
            //comment2 */
            """,
            testHost,
            Comment("// comment1 /*"),
            Keyword("class"),
            Class("cl"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Comment("//comment2 */"));

    [Theory, CombinatorialData]
    public Task CommentMix2(TestHost testHost)
        => TestInMethodAsync(
@"/**/int /**/i = 0;",
            testHost,
            Comment("/**/"),
            Keyword("int"),
            Comment("/**/"),
            Local("i"),
            Operators.Equals,
            Number("0"),
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    public Task XmlDocCommentOnClass(TestHost testHost)
        => TestAsync("""

            /// <summary>something</summary>
            class Bar { }
            """,
            testHost,
            XmlDoc.Delimiter("///"),
            XmlDoc.Text(" "),
            XmlDoc.Delimiter("<"),
            XmlDoc.Name("summary"),
            XmlDoc.Delimiter(">"),
            XmlDoc.Text("something"),
            XmlDoc.Delimiter("</"),
            XmlDoc.Name("summary"),
            XmlDoc.Delimiter(">"),
            Keyword("class"),
            Class("Bar"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task XmlDocCommentOnClassWithIndent(TestHost testHost)
        => TestAsync("""

                /// <summary>
                /// something
                /// </summary>
                class Bar { }
            """,
            testHost,
            XmlDoc.Delimiter("///"),
            XmlDoc.Text(" "),
            XmlDoc.Delimiter("<"),
            XmlDoc.Name("summary"),
            XmlDoc.Delimiter(">"),
            XmlDoc.Delimiter("///"),
            XmlDoc.Text(" something"),
            XmlDoc.Delimiter("///"),
            XmlDoc.Text(" "),
            XmlDoc.Delimiter("</"),
            XmlDoc.Name("summary"),
            XmlDoc.Delimiter(">"),
            Keyword("class"),
            Class("Bar"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task XmlDocComment_EntityReference(TestHost testHost)
        => TestAsync("""

            /// <summary>&#65;</summary>
            class Bar { }
            """,
            testHost,
            XmlDoc.Delimiter("///"),
            XmlDoc.Text(" "),
            XmlDoc.Delimiter("<"),
            XmlDoc.Name("summary"),
            XmlDoc.Delimiter(">"),
            XmlDoc.EntityReference("&#65;"),
            XmlDoc.Delimiter("</"),
            XmlDoc.Name("summary"),
            XmlDoc.Delimiter(">"),
            Keyword("class"),
            Class("Bar"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task XmlDocComment_ExteriorTriviaInsideCloseTag(TestHost testHost)
        => TestAsync("""

            /// <summary>something</
            /// summary>
            class Bar { }
            """,
            testHost,
            XmlDoc.Delimiter("///"),
            XmlDoc.Text(" "),
            XmlDoc.Delimiter("<"),
            XmlDoc.Name("summary"),
            XmlDoc.Delimiter(">"),
            XmlDoc.Text("something"),
            XmlDoc.Delimiter("</"),
            XmlDoc.Delimiter("///"),
            XmlDoc.Name("summary"),
            XmlDoc.Delimiter(">"),
            Keyword("class"),
            Class("Bar"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory]
    [InlineData(TestHost.InProcess, "true", false)]
    [InlineData(TestHost.OutOfProcess, "true", false)]
    [InlineData(TestHost.InProcess, "return", true)]
    [InlineData(TestHost.OutOfProcess, "return", true)]
    [InlineData(TestHost.InProcess, "with", false)]
    [InlineData(TestHost.OutOfProcess, "with", false)]
    public Task XmlDocComment_LangWordAttribute_Keywords(TestHost testHost, string langword, bool isControlKeyword)
        => TestAsync(
            $$"""
            /// <summary>
            /// <see langword="{{langword}}"/>
            /// </summary>
            class MyClass
            {
            }
            """,
            testHost,
            XmlDoc.Delimiter("///"),
            XmlDoc.Text(" "),
            XmlDoc.Delimiter("<"),
            XmlDoc.Name("summary"),
            XmlDoc.Delimiter(">"),
            XmlDoc.Delimiter("///"),
            XmlDoc.Text(" "),
            XmlDoc.Delimiter("<"),
            XmlDoc.Name("see"),
            XmlDoc.AttributeName("langword"),
            XmlDoc.Delimiter("="),
            XmlDoc.AttributeQuotes("""
                "
                """),
            isControlKeyword ? ControlKeyword(langword) : Keyword(langword),
            XmlDoc.AttributeQuotes("""
                "
                """),
            XmlDoc.Delimiter("/>"),
            XmlDoc.Delimiter("///"),
            XmlDoc.Text(" "),
            XmlDoc.Delimiter("</"),
            XmlDoc.Name("summary"),
            XmlDoc.Delimiter(">"),
            Keyword("class"),
            Class("MyClass"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task XmlDocComment_LangWordAttribute_NonKeyword(TestHost testHost)
        => TestAsync(
            """
            /// <summary>
            /// <see langword="MyWord"/>
            /// </summary>
            class MyClass
            {
            }
            """, testHost,
            XmlDoc.Delimiter("///"),
            XmlDoc.Text(" "),
            XmlDoc.Delimiter("<"),
            XmlDoc.Name("summary"),
            XmlDoc.Delimiter(">"),
            XmlDoc.Delimiter("///"),
            XmlDoc.Text(" "),
            XmlDoc.Delimiter("<"),
            XmlDoc.Name("see"),
            XmlDoc.AttributeName("langword"),
            XmlDoc.Delimiter("="),
            XmlDoc.AttributeQuotes("""
                "
                """),
            XmlDoc.AttributeValue("MyWord"),
            XmlDoc.AttributeQuotes("""
                "
                """),
            XmlDoc.Delimiter("/>"),
            XmlDoc.Delimiter("///"),
            XmlDoc.Text(" "),
            XmlDoc.Delimiter("</"),
            XmlDoc.Name("summary"),
            XmlDoc.Delimiter(">"),
            Keyword("class"),
            Class("MyClass"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531155")]
    [CombinatorialData]
    public Task XmlDocComment_ExteriorTriviaInsideCRef(TestHost testHost)
        => TestAsync("""

            /// <see cref="System.
            /// Int32"/>
            class C
            {
            }
            """,
            testHost,
            XmlDoc.Delimiter("///"),
            XmlDoc.Text(" "),
            XmlDoc.Delimiter("<"),
            XmlDoc.Name("see"),
            XmlDoc.AttributeName("cref"),
            XmlDoc.Delimiter("="),
            XmlDoc.AttributeQuotes("""
                "
                """),
            Identifier("System"),
            Operators.Dot,
            XmlDoc.Delimiter("///"),
            Identifier("Int32"),
            XmlDoc.AttributeQuotes("""
                "
                """),
            XmlDoc.Delimiter("/>"),
            Keyword("class"),
            Class("C"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task XmlDocCommentOnClassWithExteriorTrivia(TestHost testHost)
        => TestAsync("""

            /// <summary>
            /// something
            /// </summary>
            class Bar { }
            """,
            testHost,
            XmlDoc.Delimiter("///"),
            XmlDoc.Text(" "),
            XmlDoc.Delimiter("<"),
            XmlDoc.Name("summary"),
            XmlDoc.Delimiter(">"),
            XmlDoc.Delimiter("///"),
            XmlDoc.Text(" something"),
            XmlDoc.Delimiter("///"),
            XmlDoc.Text(" "),
            XmlDoc.Delimiter("</"),
            XmlDoc.Name("summary"),
            XmlDoc.Delimiter(">"),
            Keyword("class"),
            Class("Bar"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task XmlDocComment_ExteriorTriviaNoText(TestHost testHost)
        => TestAsync("""
            ///<summary>
            ///something
            ///</summary>
            class Bar { }
            """,
            testHost,
            XmlDoc.Delimiter("///"),
            XmlDoc.Delimiter("<"),
            XmlDoc.Name("summary"),
            XmlDoc.Delimiter(">"),
            XmlDoc.Delimiter("///"),
            XmlDoc.Text("something"),
            XmlDoc.Delimiter("///"),
            XmlDoc.Delimiter("</"),
            XmlDoc.Name("summary"),
            XmlDoc.Delimiter(">"),
            Keyword("class"),
            Class("Bar"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task XmlDocComment_EmptyElement(TestHost testHost)
        => TestAsync("""

            /// <summary />
            class Bar { }
            """,
            testHost,
            XmlDoc.Delimiter("///"),
            XmlDoc.Text(" "),
            XmlDoc.Delimiter("<"),
            XmlDoc.Name("summary"),
            XmlDoc.Delimiter("/>"),
            Keyword("class"),
            Class("Bar"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task XmlDocComment_Attribute(TestHost testHost)
        => TestAsync("""

            /// <summary attribute="value">something</summary>
            class Bar { }
            """,
            testHost,
            XmlDoc.Delimiter("///"),
            XmlDoc.Text(" "),
            XmlDoc.Delimiter("<"),
            XmlDoc.Name("summary"),
            XmlDoc.AttributeName("attribute"),
            XmlDoc.Delimiter("="),
            XmlDoc.AttributeQuotes("""
                "
                """),
            XmlDoc.AttributeValue(@"value"),
            XmlDoc.AttributeQuotes("""
                "
                """),
            XmlDoc.Delimiter(">"),
            XmlDoc.Text("something"),
            XmlDoc.Delimiter("</"),
            XmlDoc.Name("summary"),
            XmlDoc.Delimiter(">"),
            Keyword("class"),
            Class("Bar"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task XmlDocComment_AttributeInEmptyElement(TestHost testHost)
        => TestAsync("""

            /// <summary attribute="value" />
            class Bar { }
            """,
            testHost,
            XmlDoc.Delimiter("///"),
            XmlDoc.Text(" "),
            XmlDoc.Delimiter("<"),
            XmlDoc.Name("summary"),
            XmlDoc.AttributeName("attribute"),
            XmlDoc.Delimiter("="),
            XmlDoc.AttributeQuotes("""
                "
                """),
            XmlDoc.AttributeValue(@"value"),
            XmlDoc.AttributeQuotes("""
                "
                """),
            XmlDoc.Delimiter("/>"),
            Keyword("class"),
            Class("Bar"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task XmlDocComment_ExtraSpaces(TestHost testHost)
        => TestAsync("""

            ///   <   summary   attribute    =   "value"     />
            class Bar { }
            """,
            testHost,
            XmlDoc.Delimiter("///"),
            XmlDoc.Text("   "),
            XmlDoc.Delimiter("<"),
            XmlDoc.Name("summary"),
            XmlDoc.AttributeName("attribute"),
            XmlDoc.Delimiter("="),
            XmlDoc.AttributeQuotes("""
                "
                """),
            XmlDoc.AttributeValue(@"value"),
            XmlDoc.AttributeQuotes("""
                "
                """),
            XmlDoc.Delimiter("/>"),
            Keyword("class"),
            Class("Bar"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task XmlDocComment_XmlComment(TestHost testHost)
        => TestAsync("""

            ///<!--comment-->
            class Bar { }
            """,
            testHost,
            XmlDoc.Delimiter("///"),
            XmlDoc.Delimiter("<!--"),
            XmlDoc.Comment("comment"),
            XmlDoc.Delimiter("-->"),
            Keyword("class"),
            Class("Bar"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task XmlDocComment_XmlCommentWithExteriorTrivia(TestHost testHost)
        => TestAsync("""

            ///<!--first
            ///second-->
            class Bar { }
            """,
            testHost,
            XmlDoc.Delimiter("///"),
            XmlDoc.Delimiter("<!--"),
            XmlDoc.Comment("first"),
            XmlDoc.Delimiter("///"),
            XmlDoc.Comment("second"),
            XmlDoc.Delimiter("-->"),
            Keyword("class"),
            Class("Bar"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task XmlDocComment_XmlCommentInElement(TestHost testHost)
        => TestAsync("""

            ///<summary><!--comment--></summary>
            class Bar { }
            """,
            testHost,
            XmlDoc.Delimiter("///"),
            XmlDoc.Delimiter("<"),
            XmlDoc.Name("summary"),
            XmlDoc.Delimiter(">"),
            XmlDoc.Delimiter("<!--"),
            XmlDoc.Comment("comment"),
            XmlDoc.Delimiter("-->"),
            XmlDoc.Delimiter("</"),
            XmlDoc.Name("summary"),
            XmlDoc.Delimiter(">"),
            Keyword("class"),
            Class("Bar"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/pull/31410")]
    public Task XmlDocComment_MalformedXmlDocComment(TestHost testHost)
        => TestAsync("""

            ///<summary>
            ///<a: b, c />.
            ///</summary>
            class C { }
            """,
            testHost,
            XmlDoc.Delimiter("///"),
            XmlDoc.Delimiter("<"),
            XmlDoc.Name("summary"),
            XmlDoc.Delimiter(">"),
            XmlDoc.Delimiter("///"),
            XmlDoc.Delimiter("<"),
            XmlDoc.Name("a"),
            XmlDoc.Name(":"),
            XmlDoc.Name("b"),
            XmlDoc.Text(","),
            XmlDoc.Text("c"),
            XmlDoc.Delimiter("/>"),
            XmlDoc.Text("."),
            XmlDoc.Delimiter("///"),
            XmlDoc.Delimiter("</"),
            XmlDoc.Name("summary"),
            XmlDoc.Delimiter(">"),
            Keyword("class"),
            Class("C"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task MultilineXmlDocComment_ExteriorTrivia(TestHost testHost)
        => TestAsync("""
            /**<summary>
            *comment
            *</summary>*/
            class Bar { }
            """,
            testHost,
            XmlDoc.Delimiter("/**"),
            XmlDoc.Delimiter("<"),
            XmlDoc.Name("summary"),
            XmlDoc.Delimiter(">"),
            XmlDoc.Delimiter("*"),
            XmlDoc.Text("comment"),
            XmlDoc.Delimiter("*"),
            XmlDoc.Delimiter("</"),
            XmlDoc.Name("summary"),
            XmlDoc.Delimiter(">"),
            XmlDoc.Delimiter("*/"),
            Keyword("class"),
            Class("Bar"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task XmlDocComment_CDataWithExteriorTrivia(TestHost testHost)
        => TestAsync("""

            ///<![CDATA[first
            ///second]]>
            class Bar { }
            """,
            testHost,
            XmlDoc.Delimiter("///"),
            XmlDoc.Delimiter("<![CDATA["),
            XmlDoc.CDataSection("first"),
            XmlDoc.Delimiter("///"),
            XmlDoc.CDataSection("second"),
            XmlDoc.Delimiter("]]>"),
            Keyword("class"),
            Class("Bar"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task XmlDocComment_ProcessingDirective(TestHost testHost)
        => TestAsync(
            """
            /// <summary><?goo
            /// ?></summary>
            public class Program
            {
                static void Main()
                {
                }
            }
            """,
            testHost,
            XmlDoc.Delimiter("///"),
            XmlDoc.Text(" "),
            XmlDoc.Delimiter("<"),
            XmlDoc.Name("summary"),
            XmlDoc.Delimiter(">"),
            XmlDoc.ProcessingInstruction("<?"),
            XmlDoc.ProcessingInstruction("goo"),
            XmlDoc.Delimiter("///"),
            XmlDoc.ProcessingInstruction(" "),
            XmlDoc.ProcessingInstruction("?>"),
            XmlDoc.Delimiter("</"),
            XmlDoc.Name("summary"),
            XmlDoc.Delimiter(">"),
            Keyword("public"),
            Keyword("class"),
            Class("Program"),
            Punctuation.OpenCurly,
            Keyword("static"),
            Keyword("void"),
            Method("Main"),
            Static("Main"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/536321")]
    public Task KeywordTypeParameters(TestHost testHost)
        => TestAsync(@"class C<int> { }",
            testHost,
            Keyword("class"),
            Class("C"),
            Punctuation.OpenAngle,
            Keyword("int"),
            Punctuation.CloseAngle,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/536853")]
    public Task TypeParametersWithAttribute(TestHost testHost)
        => TestAsync(@"class C<[Attr] T> { }",
            testHost,
            Keyword("class"),
            Class("C"),
            Punctuation.OpenAngle,
            Punctuation.OpenBracket,
            Identifier("Attr"),
            Punctuation.CloseBracket,
            TypeParameter("T"),
            Punctuation.CloseAngle,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task ClassTypeDeclaration1(TestHost testHost)
        => TestAsync("class C1 { } ",
            testHost,
            Keyword("class"),
            Class("C1"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task ClassTypeDeclaration2(TestHost testHost)
        => TestAsync("class ClassName1 { } ",
            testHost,
            Keyword("class"),
            Class("ClassName1"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task StructTypeDeclaration1(TestHost testHost)
        => TestAsync("struct Struct1 { }",
            testHost,
            Keyword("struct"),
            Struct("Struct1"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task InterfaceDeclaration1(TestHost testHost)
        => TestAsync("interface I1 { }",
            testHost,
            Keyword("interface"),
            Interface("I1"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task EnumDeclaration1(TestHost testHost)
        => TestAsync("enum Weekday { }",
            testHost,
            Keyword("enum"),
            Enum("Weekday"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, WorkItem(4302, "DevDiv_Projects/Roslyn")]
    [CombinatorialData]
    public Task ClassInEnum(TestHost testHost)
        => TestAsync("enum E { Min = System.Int32.MinValue }",
            testHost,
            Keyword("enum"),
            Enum("E"),
            Punctuation.OpenCurly,
            EnumMember("Min"),
            Operators.Equals,
            Identifier("System"),
            Operators.Dot,
            Identifier("Int32"),
            Operators.Dot,
            Identifier("MinValue"),
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task DelegateDeclaration1(TestHost testHost)
        => TestAsync("delegate void Action();",
            testHost,
            Keyword("delegate"),
            Keyword("void"),
            Delegate("Action"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    public Task GenericTypeArgument(TestHost testHost)
        => TestInMethodAsync(
            "C<T>",
            "M",
            "default(T)",
            testHost,
            Keyword("default"),
            Punctuation.OpenParen,
            Identifier("T"),
            Punctuation.CloseParen);

    [Theory, CombinatorialData]
    public Task GenericParameter(TestHost testHost)
        => TestAsync("class C1<P1> {}",
            testHost,
            Keyword("class"),
            Class("C1"),
            Punctuation.OpenAngle,
            TypeParameter("P1"),
            Punctuation.CloseAngle,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task GenericParameters(TestHost testHost)
        => TestAsync("class C1<P1,P2> {}",
            testHost,
            Keyword("class"),
            Class("C1"),
            Punctuation.OpenAngle,
            TypeParameter("P1"),
            Punctuation.Comma,
            TypeParameter("P2"),
            Punctuation.CloseAngle,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task GenericParameter_Interface(TestHost testHost)
        => TestAsync("interface I1<P1> {}",
            testHost,
            Keyword("interface"),
            Interface("I1"),
            Punctuation.OpenAngle,
            TypeParameter("P1"),
            Punctuation.CloseAngle,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task GenericParameter_Struct(TestHost testHost)
        => TestAsync("struct S1<P1> {}",
            testHost,
            Keyword("struct"),
            Struct("S1"),
            Punctuation.OpenAngle,
            TypeParameter("P1"),
            Punctuation.CloseAngle,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task GenericParameter_Delegate(TestHost testHost)
        => TestAsync("delegate void D1<P1> {}",
            testHost,
            Keyword("delegate"),
            Keyword("void"),
            Delegate("D1"),
            Punctuation.OpenAngle,
            TypeParameter("P1"),
            Punctuation.CloseAngle,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task GenericParameter_Method(TestHost testHost)
        => TestInClassAsync(
            """
            T M<T>(T t)
            {
                return default(T);
            }
            """,
            testHost,
            Identifier("T"),
            Method("M"),
            Punctuation.OpenAngle,
            TypeParameter("T"),
            Punctuation.CloseAngle,
            Punctuation.OpenParen,
            Identifier("T"),
            Parameter("t"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            ControlKeyword("return"),
            Keyword("default"),
            Punctuation.OpenParen,
            Identifier("T"),
            Punctuation.CloseParen,
            Punctuation.Semicolon,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task TernaryExpression(TestHost testHost)
        => TestInExpressionAsync("true ? 1 : 0",
            testHost,
            Keyword("true"),
            Operators.QuestionMark,
            Number("1"),
            Operators.Colon,
            Number("0"));

    [Theory, CombinatorialData]
    public Task BaseClass(TestHost testHost)
        => TestAsync(
            """
            class C : B
            {
            }
            """,
            testHost,
            Keyword("class"),
            Class("C"),
            Punctuation.Colon,
            Identifier("B"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task TestLabel(TestHost testHost)
        => TestInMethodAsync("goo:",
            testHost,
            Label("goo"),
            Punctuation.Colon);

    [Theory, CombinatorialData]
    public Task Attribute(TestHost testHost)
        => TestAsync(
@"[assembly: Goo]",
            testHost,
            Punctuation.OpenBracket,
            Keyword("assembly"),
            Punctuation.Colon,
            Identifier("Goo"),
            Punctuation.CloseBracket);

    [Theory, CombinatorialData]
    public Task TestAngleBracketsOnGenericConstraints_Bug932262(TestHost testHost)
        => TestAsync(
            """
            class C<T> where T : A<T>
            {
            }
            """,
            testHost,
            Keyword("class"),
            Class("C"),
            Punctuation.OpenAngle,
            TypeParameter("T"),
            Punctuation.CloseAngle,
            Keyword("where"),
            Identifier("T"),
            Punctuation.Colon,
            Identifier("A"),
            Punctuation.OpenAngle,
            Identifier("T"),
            Punctuation.CloseAngle,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task TestYieldPositive(TestHost testHost)
        => TestInMethodAsync(
@"yield return goo;",
            testHost,
            ControlKeyword("yield"),
            ControlKeyword("return"),
            Identifier("goo"),
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    public Task TestYieldNegative(TestHost testHost)
        => TestInMethodAsync(
@"int yield;",
            testHost,
            Keyword("int"),
            Local("yield"),
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    public Task TestFromPositive(TestHost testHost)
        => TestInExpressionAsync(
@"from x in y",
            testHost,
            Keyword("from"),
            Identifier("x"),
            Keyword("in"),
            Identifier("y"));

    [Theory, CombinatorialData]
    public Task TestFromNegative(TestHost testHost)
        => TestInMethodAsync(
@"int from;",
            testHost,
            Keyword("int"),
            Local("from"),
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    public Task AttributeTargetSpecifiersModule(TestHost testHost)
        => TestAsync(
@"[module: Obsolete]",
            testHost,
            Punctuation.OpenBracket,
            Keyword("module"),
            Punctuation.Colon,
            Identifier("Obsolete"),
            Punctuation.CloseBracket);

    [Theory, CombinatorialData]
    public Task AttributeTargetSpecifiersAssembly(TestHost testHost)
        => TestAsync(
@"[assembly: Obsolete]",
            testHost,
            Punctuation.OpenBracket,
            Keyword("assembly"),
            Punctuation.Colon,
            Identifier("Obsolete"),
            Punctuation.CloseBracket);

    [Theory, CombinatorialData]
    public Task AttributeTargetSpecifiersOnDelegate(TestHost testHost)
        => TestInClassAsync(
            """
            [type: A]
            [return: A]
            delegate void M();
            """,
            testHost,
            Punctuation.OpenBracket,
            Keyword("type"),
            Punctuation.Colon,
            Identifier("A"),
            Punctuation.CloseBracket,
            Punctuation.OpenBracket,
            Keyword("return"),
            Punctuation.Colon,
            Identifier("A"),
            Punctuation.CloseBracket,
            Keyword("delegate"),
            Keyword("void"),
            Delegate("M"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    public Task AttributeTargetSpecifiersOnMethod(TestHost testHost)
        => TestInClassAsync(
            """
            [return: A]
            [method: A]
            void M()
            {
            }
            """,
            testHost,
            Punctuation.OpenBracket,
            Keyword("return"),
            Punctuation.Colon,
            Identifier("A"),
            Punctuation.CloseBracket,
            Punctuation.OpenBracket,
            Keyword("method"),
            Punctuation.Colon,
            Identifier("A"),
            Punctuation.CloseBracket,
            Keyword("void"),
            Method("M"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task AttributeTargetSpecifiersOnCtor(TestHost testHost)
        => TestAsync(
            """
            class C
            {
                [method: A]
                C()
                {
                }
            }
            """,
            testHost,
            Keyword("class"),
            Class("C"),
            Punctuation.OpenCurly,
            Punctuation.OpenBracket,
            Keyword("method"),
            Punctuation.Colon,
            Identifier("A"),
            Punctuation.CloseBracket,
            Class("C"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task AttributeTargetSpecifiersOnDtor(TestHost testHost)
        => TestAsync(
            """
            class C
            {
                [method: A]
                ~C()
                {
                }
            }
            """,
            testHost,
            Keyword("class"),
            Class("C"),
            Punctuation.OpenCurly,
            Punctuation.OpenBracket,
            Keyword("method"),
            Punctuation.Colon,
            Identifier("A"),
            Punctuation.CloseBracket,
            Operators.Tilde,
            Class("C"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task AttributeTargetSpecifiersOnOperator(TestHost testHost)
        => TestInClassAsync(
            """
            [method: A]
            [return: A]
            static T operator +(T a, T b)
            {
            }
            """,
            testHost,
            Punctuation.OpenBracket,
            Keyword("method"),
            Punctuation.Colon,
            Identifier("A"),
            Punctuation.CloseBracket,
            Punctuation.OpenBracket,
            Keyword("return"),
            Punctuation.Colon,
            Identifier("A"),
            Punctuation.CloseBracket,
            Keyword("static"),
            Identifier("T"),
            Keyword("operator"),
            Operators.Plus,
            Punctuation.OpenParen,
            Identifier("T"),
            Parameter("a"),
            Punctuation.Comma,
            Identifier("T"),
            Parameter("b"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task AttributeTargetSpecifiersOnEventDeclaration(TestHost testHost)
        => TestInClassAsync(
            """
            [event: A]
            event A E
            {
                [param: Test]
                [method: Test]
                add
                {
                }

                [param: Test]
                [method: Test]
                remove
                {
                }
            }
            """,
            testHost,
            Punctuation.OpenBracket,
            Keyword("event"),
            Punctuation.Colon,
            Identifier("A"),
            Punctuation.CloseBracket,
            Keyword("event"),
            Identifier("A"),
            Event("E"),
            Punctuation.OpenCurly,
            Punctuation.OpenBracket,
            Keyword("param"),
            Punctuation.Colon,
            Identifier("Test"),
            Punctuation.CloseBracket,
            Punctuation.OpenBracket,
            Keyword("method"),
            Punctuation.Colon,
            Identifier("Test"),
            Punctuation.CloseBracket,
            Keyword("add"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.OpenBracket,
            Keyword("param"),
            Punctuation.Colon,
            Identifier("Test"),
            Punctuation.CloseBracket,
            Punctuation.OpenBracket,
            Keyword("method"),
            Punctuation.Colon,
            Identifier("Test"),
            Punctuation.CloseBracket,
            Keyword("remove"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task AttributeTargetSpecifiersOnPropertyAccessors(TestHost testHost)
        => TestInClassAsync(
            """
            int P
            {
                [return: T]
                [method: T]
                get
                {
                }

                [param: T]
                [method: T]
                set
                {
                }
            }
            """,
            testHost,
            Keyword("int"),
            Property("P"),
            Punctuation.OpenCurly,
            Punctuation.OpenBracket,
            Keyword("return"),
            Punctuation.Colon,
            Identifier("T"),
            Punctuation.CloseBracket,
            Punctuation.OpenBracket,
            Keyword("method"),
            Punctuation.Colon,
            Identifier("T"),
            Punctuation.CloseBracket,
            Keyword("get"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.OpenBracket,
            Keyword("param"),
            Punctuation.Colon,
            Identifier("T"),
            Punctuation.CloseBracket,
            Punctuation.OpenBracket,
            Keyword("method"),
            Punctuation.Colon,
            Identifier("T"),
            Punctuation.CloseBracket,
            Keyword("set"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task AttributeTargetSpecifiersOnIndexers(TestHost testHost)
        => TestInClassAsync(
            """
            [property: A]
            int this[int i] { get; set; }
            """,
            testHost,
            Punctuation.OpenBracket,
            Keyword("property"),
            Punctuation.Colon,
            Identifier("A"),
            Punctuation.CloseBracket,
            Keyword("int"),
            Keyword("this"),
            Punctuation.OpenBracket,
            Keyword("int"),
            Parameter("i"),
            Punctuation.CloseBracket,
            Punctuation.OpenCurly,
            Keyword("get"),
            Punctuation.Semicolon,
            Keyword("set"),
            Punctuation.Semicolon,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task AttributeTargetSpecifiersOnIndexerAccessors(TestHost testHost)
        => TestInClassAsync(
            """
            int this[int i]
            {
                [return: T]
                [method: T]
                get
                {
                }

                [param: T]
                [method: T]
                set
                {
                }
            }
            """,
            testHost,
            Keyword("int"),
            Keyword("this"),
            Punctuation.OpenBracket,
            Keyword("int"),
            Parameter("i"),
            Punctuation.CloseBracket,
            Punctuation.OpenCurly,
            Punctuation.OpenBracket,
            Keyword("return"),
            Punctuation.Colon,
            Identifier("T"),
            Punctuation.CloseBracket,
            Punctuation.OpenBracket,
            Keyword("method"),
            Punctuation.Colon,
            Identifier("T"),
            Punctuation.CloseBracket,
            Keyword("get"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.OpenBracket,
            Keyword("param"),
            Punctuation.Colon,
            Identifier("T"),
            Punctuation.CloseBracket,
            Punctuation.OpenBracket,
            Keyword("method"),
            Punctuation.Colon,
            Identifier("T"),
            Punctuation.CloseBracket,
            Keyword("set"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task AttributeTargetSpecifiersOnField(TestHost testHost)
        => TestInClassAsync(
            """
            [field: A]
            const int a = 0;
            """,
            testHost,
            Punctuation.OpenBracket,
            Keyword("field"),
            Punctuation.Colon,
            Identifier("A"),
            Punctuation.CloseBracket,
            Keyword("const"),
            Keyword("int"),
            Constant("a"),
            Static("a"),
            Operators.Equals,
            Number("0"),
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    public Task TestAllKeywords(TestHost testHost)
        => TestAsync(
            """
            using System;
            #region TaoRegion
            namespace MyNamespace
            {
                abstract class Goo : Bar
                {
                    bool goo = default(bool);
                    byte goo1;
                    char goo2;
                    const int goo3 = 999;
                    decimal goo4;

                    delegate void D();
                    delegate* managed<int, int> mgdfun;
                    delegate* unmanaged<int, int> unmgdfun;

                    double goo5;

                    enum MyEnum
                    {
                        one,
                        two,
                        three
                    };

                    event D MyEvent;

                    float goo6;
                    static int x;
                    long goo7;
                    sbyte goo8;
                    short goo9;
                    int goo10 = sizeof(int);
                    string goo11;
                    uint goo12;
                    ulong goo13;
                    volatile ushort goo14;

                    struct SomeStruct
                    {
                    }

                    protected virtual void someMethod()
                    {
                    }

                    public Goo(int i)
                    {
                        bool var = i is int;
                        try
                        {
                            while (true)
                            {
                                continue;
                                break;
                            }

                            switch (goo)
                            {
                                case true:
                                    break;
                                default:
                                    break;
                            }
                        }
                        catch (System.Exception)
                        {
                        }
                        finally
                        {
                        }

                        checked
                        {
                            int i2 = 10000;
                            i2++;
                        }

                        do
                        {
                        }
                        while (true);
                        if (false)
                        {
                        }
                        else
                        {
                        }

                        unsafe
                        {
                            fixed (int* p = &x)
                            {
                            }

                            char* buffer = stackalloc char[16];
                        }

                        for (int i1 = 0; i1 < 10; i1++)
                        {
                        }

                        System.Collections.ArrayList al = new System.Collections.ArrayList();
                        foreach (object o in al)
                        {
                            object o1 = o;
                        }

                        lock (this)
                        {
                        }
                    }

                    Goo method(Bar i, out int z)
                    {
                        z = 5;
                        return i as Goo;
                    }

                    public static explicit operator Goo(int i)
                    {
                        return new Baz(1);
                    }

                    public static implicit operator Goo(double x)
                    {
                        return new Baz(1);
                    }

                    public extern void doSomething();

                    internal void method2(object o)
                    {
                        if (o == null)
                            goto Output;
                        if (o is Baz)
                            return;
                        else
                            throw new System.Exception();
                        Output:
                        Console.WriteLine("Finished");
                    }
                }

                sealed class Baz : Goo
                {
                    readonly int field;

                    public Baz(int i) : base(i)
                    {
                    }

                    public void someOtherMethod(ref int i, System.Type c)
                    {
                        int f = 1;
                        someOtherMethod(ref f, typeof(int));
                    }

                    protected override void someMethod()
                    {
                        unchecked
                        {
                            int i = 1;
                            i++;
                        }
                    }

                    private void method(params object[] args)
                    {
                    }

                    private string aMethod(object o) => o switch
                    {
                        int => string.Empty,
                        _ when true => throw new System.Exception()
                    };
                }

                interface Bar
                {
                }
            }
            #endregion TaoRegion
            """,
            testHost,
            [new CSharpParseOptions(LanguageVersion.CSharp8)],
            Keyword("using"),
            Identifier("System"),
            Punctuation.Semicolon,
            PPKeyword("#"),
            PPKeyword("region"),
            PPText("TaoRegion"),
            Keyword("namespace"),
            Namespace("MyNamespace"),
            Punctuation.OpenCurly,
            Keyword("abstract"),
            Keyword("class"),
            Class("Goo"),
            Punctuation.Colon,
            Identifier("Bar"),
            Punctuation.OpenCurly,
            Keyword("bool"),
            Field("goo"),
            Operators.Equals,
            Keyword("default"),
            Punctuation.OpenParen,
            Keyword("bool"),
            Punctuation.CloseParen,
            Punctuation.Semicolon,
            Keyword("byte"),
            Field("goo1"),
            Punctuation.Semicolon,
            Keyword("char"),
            Field("goo2"),
            Punctuation.Semicolon,
            Keyword("const"),
            Keyword("int"),
            Constant("goo3"),
            Static("goo3"),
            Operators.Equals,
            Number("999"),
            Punctuation.Semicolon,
            Keyword("decimal"),
            Field("goo4"),
            Punctuation.Semicolon,
            Keyword("delegate"),
            Keyword("void"),
            Delegate("D"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.Semicolon,
            Keyword("delegate"),
            Operators.Asterisk,
            Keyword("managed"),
            Punctuation.OpenAngle,
            Keyword("int"),
            Punctuation.Comma,
            Keyword("int"),
            Punctuation.CloseAngle,
            Field("mgdfun"),
            Punctuation.Semicolon,
            Keyword("delegate"),
            Operators.Asterisk,
            Keyword("unmanaged"),
            Punctuation.OpenAngle,
            Keyword("int"),
            Punctuation.Comma,
            Keyword("int"),
            Punctuation.CloseAngle,
            Field("unmgdfun"),
            Punctuation.Semicolon,
            Keyword("double"),
            Field("goo5"),
            Punctuation.Semicolon,
            Keyword("enum"),
            Enum("MyEnum"),
            Punctuation.OpenCurly,
            EnumMember("one"),
            Punctuation.Comma,
            EnumMember("two"),
            Punctuation.Comma,
            EnumMember("three"),
            Punctuation.CloseCurly,
            Punctuation.Semicolon,
            Keyword("event"),
            Identifier("D"),
            Event("MyEvent"),
            Punctuation.Semicolon,
            Keyword("float"),
            Field("goo6"),
            Punctuation.Semicolon,
            Keyword("static"),
            Keyword("int"),
            Field("x"),
            Static("x"),
            Punctuation.Semicolon,
            Keyword("long"),
            Field("goo7"),
            Punctuation.Semicolon,
            Keyword("sbyte"),
            Field("goo8"),
            Punctuation.Semicolon,
            Keyword("short"),
            Field("goo9"),
            Punctuation.Semicolon,
            Keyword("int"),
            Field("goo10"),
            Operators.Equals,
            Keyword("sizeof"),
            Punctuation.OpenParen,
            Keyword("int"),
            Punctuation.CloseParen,
            Punctuation.Semicolon,
            Keyword("string"),
            Field("goo11"),
            Punctuation.Semicolon,
            Keyword("uint"),
            Field("goo12"),
            Punctuation.Semicolon,
            Keyword("ulong"),
            Field("goo13"),
            Punctuation.Semicolon,
            Keyword("volatile"),
            Keyword("ushort"),
            Field("goo14"),
            Punctuation.Semicolon,
            Keyword("struct"),
            Struct("SomeStruct"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Keyword("protected"),
            Keyword("virtual"),
            Keyword("void"),
            Method("someMethod"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Keyword("public"),
            Class("Goo"),
            Punctuation.OpenParen,
            Keyword("int"),
            Parameter("i"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Keyword("bool"),
            Local("var"),
            Operators.Equals,
            Identifier("i"),
            Keyword("is"),
            Keyword("int"),
            Punctuation.Semicolon,
            ControlKeyword("try"),
            Punctuation.OpenCurly,
            ControlKeyword("while"),
            Punctuation.OpenParen,
            Keyword("true"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            ControlKeyword("continue"),
            Punctuation.Semicolon,
            ControlKeyword("break"),
            Punctuation.Semicolon,
            Punctuation.CloseCurly,
            ControlKeyword("switch"),
            Punctuation.OpenParen,
            Identifier("goo"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            ControlKeyword("case"),
            Keyword("true"),
            Punctuation.Colon,
            ControlKeyword("break"),
            Punctuation.Semicolon,
            ControlKeyword("default"),
            Punctuation.Colon,
            ControlKeyword("break"),
            Punctuation.Semicolon,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly,
            ControlKeyword("catch"),
            Punctuation.OpenParen,
            Identifier("System"),
            Operators.Dot,
            Identifier("Exception"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            ControlKeyword("finally"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Keyword("checked"),
            Punctuation.OpenCurly,
            Keyword("int"),
            Local("i2"),
            Operators.Equals,
            Number("10000"),
            Punctuation.Semicolon,
            Identifier("i2"),
            Operators.PlusPlus,
            Punctuation.Semicolon,
            Punctuation.CloseCurly,
            ControlKeyword("do"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            ControlKeyword("while"),
            Punctuation.OpenParen,
            Keyword("true"),
            Punctuation.CloseParen,
            Punctuation.Semicolon,
            ControlKeyword("if"),
            Punctuation.OpenParen,
            Keyword("false"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            ControlKeyword("else"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Keyword("unsafe"),
            Punctuation.OpenCurly,
            Keyword("fixed"),
            Punctuation.OpenParen,
            Keyword("int"),
            Operators.Asterisk,
            Local("p"),
            Operators.Equals,
            Operators.Ampersand,
            Identifier("x"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Keyword("char"),
            Operators.Asterisk,
            Local("buffer"),
            Operators.Equals,
            Keyword("stackalloc"),
            Keyword("char"),
            Punctuation.OpenBracket,
            Number("16"),
            Punctuation.CloseBracket,
            Punctuation.Semicolon,
            Punctuation.CloseCurly,
            ControlKeyword("for"),
            Punctuation.OpenParen,
            Keyword("int"),
            Local("i1"),
            Operators.Equals,
            Number("0"),
            Punctuation.Semicolon,
            Identifier("i1"),
            Operators.LessThan,
            Number("10"),
            Punctuation.Semicolon,
            Identifier("i1"),
            Operators.PlusPlus,
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Identifier("System"),
            Operators.Dot,
            Identifier("Collections"),
            Operators.Dot,
            Identifier("ArrayList"),
            Local("al"),
            Operators.Equals,
            Keyword("new"),
            Identifier("System"),
            Operators.Dot,
            Identifier("Collections"),
            Operators.Dot,
            Identifier("ArrayList"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.Semicolon,
            ControlKeyword("foreach"),
            Punctuation.OpenParen,
            Keyword("object"),
            Local("o"),
            ControlKeyword("in"),
            Identifier("al"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Keyword("object"),
            Local("o1"),
            Operators.Equals,
            Identifier("o"),
            Punctuation.Semicolon,
            Punctuation.CloseCurly,
            Keyword("lock"),
            Punctuation.OpenParen,
            Keyword("this"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly,
            Identifier("Goo"),
            Method("method"),
            Punctuation.OpenParen,
            Identifier("Bar"),
            Parameter("i"),
            Punctuation.Comma,
            Keyword("out"),
            Keyword("int"),
            Parameter("z"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Identifier("z"),
            Operators.Equals,
            Number("5"),
            Punctuation.Semicolon,
            ControlKeyword("return"),
            Identifier("i"),
            Keyword("as"),
            Identifier("Goo"),
            Punctuation.Semicolon,
            Punctuation.CloseCurly,
            Keyword("public"),
            Keyword("static"),
            Keyword("explicit"),
            Keyword("operator"),
            Identifier("Goo"),
            Punctuation.OpenParen,
            Keyword("int"),
            Parameter("i"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            ControlKeyword("return"),
            Keyword("new"),
            Identifier("Baz"),
            Punctuation.OpenParen,
            Number("1"),
            Punctuation.CloseParen,
            Punctuation.Semicolon,
            Punctuation.CloseCurly,
            Keyword("public"),
            Keyword("static"),
            Keyword("implicit"),
            Keyword("operator"),
            Identifier("Goo"),
            Punctuation.OpenParen,
            Keyword("double"),
            Parameter("x"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            ControlKeyword("return"),
            Keyword("new"),
            Identifier("Baz"),
            Punctuation.OpenParen,
            Number("1"),
            Punctuation.CloseParen,
            Punctuation.Semicolon,
            Punctuation.CloseCurly,
            Keyword("public"),
            Keyword("extern"),
            Keyword("void"),
            Method("doSomething"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.Semicolon,
            Keyword("internal"),
            Keyword("void"),
            Method("method2"),
            Punctuation.OpenParen,
            Keyword("object"),
            Parameter("o"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            ControlKeyword("if"),
            Punctuation.OpenParen,
            Identifier("o"),
            Operators.EqualsEquals,
            Keyword("null"),
            Punctuation.CloseParen,
            ControlKeyword("goto"),
            Identifier("Output"),
            Punctuation.Semicolon,
            ControlKeyword("if"),
            Punctuation.OpenParen,
            Identifier("o"),
            Keyword("is"),
            Identifier("Baz"),
            Punctuation.CloseParen,
            ControlKeyword("return"),
            Punctuation.Semicolon,
            ControlKeyword("else"),
            ControlKeyword("throw"),
            Keyword("new"),
            Identifier("System"),
            Operators.Dot,
            Identifier("Exception"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.Semicolon,
            Label("Output"),
            Punctuation.Colon,
            Identifier("Console"),
            Operators.Dot,
            Identifier("WriteLine"),
            Punctuation.OpenParen,
            String("""
                "Finished"
                """),
            Punctuation.CloseParen,
            Punctuation.Semicolon,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly,
            Keyword("sealed"),
            Keyword("class"),
            Class("Baz"),
            Punctuation.Colon,
            Identifier("Goo"),
            Punctuation.OpenCurly,
            Keyword("readonly"),
            Keyword("int"),
            Field("field"),
            Punctuation.Semicolon,
            Keyword("public"),
            Class("Baz"),
            Punctuation.OpenParen,
            Keyword("int"),
            Parameter("i"),
            Punctuation.CloseParen,
            Punctuation.Colon,
            Keyword("base"),
            Punctuation.OpenParen,
            Identifier("i"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Keyword("public"),
            Keyword("void"),
            Method("someOtherMethod"),
            Punctuation.OpenParen,
            Keyword("ref"),
            Keyword("int"),
            Parameter("i"),
            Punctuation.Comma,
            Identifier("System"),
            Operators.Dot,
            Identifier("Type"),
            Parameter("c"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Keyword("int"),
            Local("f"),
            Operators.Equals,
            Number("1"),
            Punctuation.Semicolon,
            Identifier("someOtherMethod"),
            Punctuation.OpenParen,
            Keyword("ref"),
            Identifier("f"),
            Punctuation.Comma,
            Keyword("typeof"),
            Punctuation.OpenParen,
            Keyword("int"),
            Punctuation.CloseParen,
            Punctuation.CloseParen,
            Punctuation.Semicolon,
            Punctuation.CloseCurly,
            Keyword("protected"),
            Keyword("override"),
            Keyword("void"),
            Method("someMethod"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Keyword("unchecked"),
            Punctuation.OpenCurly,
            Keyword("int"),
            Local("i"),
            Operators.Equals,
            Number("1"),
            Punctuation.Semicolon,
            Identifier("i"),
            Operators.PlusPlus,
            Punctuation.Semicolon,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly,
            Keyword("private"),
            Keyword("void"),
            Method("method"),
            Punctuation.OpenParen,
            Keyword("params"),
            Keyword("object"),
            Punctuation.OpenBracket,
            Punctuation.CloseBracket,
            Parameter("args"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Keyword("private"),
            Keyword("string"),
            Method("aMethod"),
            Punctuation.OpenParen,
            Keyword("object"),
            Parameter("o"),
            Punctuation.CloseParen,
            Operators.EqualsGreaterThan,
            Identifier("o"),
            ControlKeyword("switch"),
            Punctuation.OpenCurly,
            Keyword("int"),
            Operators.EqualsGreaterThan,
            Keyword("string"),
            Operators.Dot,
            Identifier("Empty"),
            Punctuation.Comma,
            Keyword("_"),
            ControlKeyword("when"),
            Keyword("true"),
            Operators.EqualsGreaterThan,
            ControlKeyword("throw"),
            Keyword("new"),
            Identifier("System"),
            Operators.Dot,
            Identifier("Exception"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.CloseCurly,
            Punctuation.Semicolon,
            Punctuation.CloseCurly,
            Keyword("interface"),
            Interface("Bar"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly,
            PPKeyword("#"),
            PPKeyword("endregion"),
            PPText("TaoRegion"));

    [Theory, CombinatorialData]
    public Task TestAllOperators(TestHost testHost)
        => TestAsync(
            """
            using IO = System.IO;

            public class Goo<T>
            {
                public void method()
                {
                    int[] a = new int[5];
                    int[] var = {
                        1,
                        2,
                        3,
                        4,
                        5
                    };
                    int i = a[i];
                    Goo<T> f = new Goo<int>();
                    f.method();
                    i = i + i - i * i / i % i & i | i ^ i;
                    bool b = true & false | true ^ false;
                    b = !b;
                    i = ~i;
                    b = i < i && i > i;
                    int? ii = 5;
                    int f = true ? 1 : 0;
                    i++;
                    i--;
                    b = true && false || true;
                    i << 5;
                    i >> 5;
                    i >>> 5;
                    b = i == i && i != i && i <= i && i >= i;
                    i += 5.0;
                    i -= i;
                    i *= i;
                    i /= i;
                    i %= i;
                    i &= i;
                    i |= i;
                    i ^= i;
                    i <<= i;
                    i >>= i;
                    i >>>= i;
                    i ??= i;
                    object s = x => x + 1;
                    Point point;
                    unsafe
                    {
                        Point* p = &point;
                        p->x = 10;
                    }

                    IO::BinaryReader br = null;
                }
            }
            """,
            testHost,
            Keyword("using"),
            Identifier("IO"),
            Operators.Equals,
            Identifier("System"),
            Operators.Dot,
            Identifier("IO"),
            Punctuation.Semicolon,
            Keyword("public"),
            Keyword("class"),
            Class("Goo"),
            Punctuation.OpenAngle,
            TypeParameter("T"),
            Punctuation.CloseAngle,
            Punctuation.OpenCurly,
            Keyword("public"),
            Keyword("void"),
            Method("method"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Keyword("int"),
            Punctuation.OpenBracket,
            Punctuation.CloseBracket,
            Local("a"),
            Operators.Equals,
            Keyword("new"),
            Keyword("int"),
            Punctuation.OpenBracket,
            Number("5"),
            Punctuation.CloseBracket,
            Punctuation.Semicolon,
            Keyword("int"),
            Punctuation.OpenBracket,
            Punctuation.CloseBracket,
            Local("var"),
            Operators.Equals,
            Punctuation.OpenCurly,
            Number("1"),
            Punctuation.Comma,
            Number("2"),
            Punctuation.Comma,
            Number("3"),
            Punctuation.Comma,
            Number("4"),
            Punctuation.Comma,
            Number("5"),
            Punctuation.CloseCurly,
            Punctuation.Semicolon,
            Keyword("int"),
            Local("i"),
            Operators.Equals,
            Identifier("a"),
            Punctuation.OpenBracket,
            Identifier("i"),
            Punctuation.CloseBracket,
            Punctuation.Semicolon,
            Identifier("Goo"),
            Punctuation.OpenAngle,
            Identifier("T"),
            Punctuation.CloseAngle,
            Local("f"),
            Operators.Equals,
            Keyword("new"),
            Identifier("Goo"),
            Punctuation.OpenAngle,
            Keyword("int"),
            Punctuation.CloseAngle,
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.Semicolon,
            Identifier("f"),
            Operators.Dot,
            Identifier("method"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.Semicolon,
            Identifier("i"),
            Operators.Equals,
            Identifier("i"),
            Operators.Plus,
            Identifier("i"),
            Operators.Minus,
            Identifier("i"),
            Operators.Asterisk,
            Identifier("i"),
            Operators.Slash,
            Identifier("i"),
            Operators.Percent,
            Identifier("i"),
            Operators.Ampersand,
            Identifier("i"),
            Operators.Bar,
            Identifier("i"),
            Operators.Caret,
            Identifier("i"),
            Punctuation.Semicolon,
            Keyword("bool"),
            Local("b"),
            Operators.Equals,
            Keyword("true"),
            Operators.Ampersand,
            Keyword("false"),
            Operators.Bar,
            Keyword("true"),
            Operators.Caret,
            Keyword("false"),
            Punctuation.Semicolon,
            Identifier("b"),
            Operators.Equals,
            Operators.Exclamation,
            Identifier("b"),
            Punctuation.Semicolon,
            Identifier("i"),
            Operators.Equals,
            Operators.Tilde,
            Identifier("i"),
            Punctuation.Semicolon,
            Identifier("b"),
            Operators.Equals,
            Identifier("i"),
            Operators.LessThan,
            Identifier("i"),
            Operators.AmpersandAmpersand,
            Identifier("i"),
            Operators.GreaterThan,
            Identifier("i"),
            Punctuation.Semicolon,
            Keyword("int"),
            Operators.QuestionMark,
            Local("ii"),
            Operators.Equals,
            Number("5"),
            Punctuation.Semicolon,
            Keyword("int"),
            Local("f"),
            Operators.Equals,
            Keyword("true"),
            Operators.QuestionMark,
            Number("1"),
            Operators.Colon,
            Number("0"),
            Punctuation.Semicolon,
            Identifier("i"),
            Operators.PlusPlus,
            Punctuation.Semicolon,
            Identifier("i"),
            Operators.MinusMinus,
            Punctuation.Semicolon,
            Identifier("b"),
            Operators.Equals,
            Keyword("true"),
            Operators.AmpersandAmpersand,
            Keyword("false"),
            Operators.BarBar,
            Keyword("true"),
            Punctuation.Semicolon,
            Identifier("i"),
            Operators.LessThanLessThan,
            Number("5"),
            Punctuation.Semicolon,
            Identifier("i"),
            Operators.GreaterThanGreaterThan,
            Number("5"),
            Punctuation.Semicolon,
            Identifier("i"),
            Operators.GreaterThanGreaterThanGreaterThan,
            Number("5"),
            Punctuation.Semicolon,
            Identifier("b"),
            Operators.Equals,
            Identifier("i"),
            Operators.EqualsEquals,
            Identifier("i"),
            Operators.AmpersandAmpersand,
            Identifier("i"),
            Operators.ExclamationEquals,
            Identifier("i"),
            Operators.AmpersandAmpersand,
            Identifier("i"),
            Operators.LessThanEquals,
            Identifier("i"),
            Operators.AmpersandAmpersand,
            Identifier("i"),
            Operators.GreaterThanEquals,
            Identifier("i"),
            Punctuation.Semicolon,
            Identifier("i"),
            Operators.PlusEquals,
            Number("5.0"),
            Punctuation.Semicolon,
            Identifier("i"),
            Operators.MinusEquals,
            Identifier("i"),
            Punctuation.Semicolon,
            Identifier("i"),
            Operators.AsteriskEquals,
            Identifier("i"),
            Punctuation.Semicolon,
            Identifier("i"),
            Operators.SlashEquals,
            Identifier("i"),
            Punctuation.Semicolon,
            Identifier("i"),
            Operators.PercentEquals,
            Identifier("i"),
            Punctuation.Semicolon,
            Identifier("i"),
            Operators.AmpersandEquals,
            Identifier("i"),
            Punctuation.Semicolon,
            Identifier("i"),
            Operators.BarEquals,
            Identifier("i"),
            Punctuation.Semicolon,
            Identifier("i"),
            Operators.CaretEquals,
            Identifier("i"),
            Punctuation.Semicolon,
            Identifier("i"),
            Operators.LessThanLessThanEquals,
            Identifier("i"),
            Punctuation.Semicolon,
            Identifier("i"),
            Operators.GreaterThanGreaterThanEquals,
            Identifier("i"),
            Punctuation.Semicolon,
            Identifier("i"),
            Operators.GreaterThanGreaterThanGreaterThanEquals,
            Identifier("i"),
            Punctuation.Semicolon,
            Identifier("i"),
            Operators.QuestionQuestionEquals,
            Identifier("i"),
            Punctuation.Semicolon,
            Keyword("object"),
            Local("s"),
            Operators.Equals,
            Parameter("x"),
            Operators.EqualsGreaterThan,
            Identifier("x"),
            Operators.Plus,
            Number("1"),
            Punctuation.Semicolon,
            Identifier("Point"),
            Local("point"),
            Punctuation.Semicolon,
            Keyword("unsafe"),
            Punctuation.OpenCurly,
            Identifier("Point"),
            Operators.Asterisk,
            Local("p"),
            Operators.Equals,
            Operators.Ampersand,
            Identifier("point"),
            Punctuation.Semicolon,
            Identifier("p"),
            Operators.MinusGreaterThan,
            Identifier("x"),
            Operators.Equals,
            Number("10"),
            Punctuation.Semicolon,
            Punctuation.CloseCurly,
            Identifier("IO"),
            Operators.ColonColon,
            Identifier("BinaryReader"),
            Local("br"),
            Operators.Equals,
            Keyword("null"),
            Punctuation.Semicolon,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task TestPartialMethodWithNamePartial(TestHost testHost)
        => TestAsync(
            """
            partial class C
            {
                partial void partial(string bar);

                partial void partial(string baz)
                {
                }

                partial int Goo();

                partial int Goo()
                {
                }

                public partial void partial void
            }
            """,
            testHost,
            Keyword("partial"),
            Keyword("class"),
            Class("C"),
            Punctuation.OpenCurly,
            Keyword("partial"),
            Keyword("void"),
            Method("partial"),
            Punctuation.OpenParen,
            Keyword("string"),
            Parameter("bar"),
            Punctuation.CloseParen,
            Punctuation.Semicolon,
            Keyword("partial"),
            Keyword("void"),
            Method("partial"),
            Punctuation.OpenParen,
            Keyword("string"),
            Parameter("baz"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Keyword("partial"),
            Keyword("int"),
            Method("Goo"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.Semicolon,
            Keyword("partial"),
            Keyword("int"),
            Method("Goo"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Keyword("public"),
            Keyword("partial"),
            Keyword("void"),
            Field("partial"),
            Keyword("void"),
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task ValueInSetterAndAnonymousTypePropertyName(TestHost testHost)
        => TestAsync(
            """
            class C
            {
                int P
                {
                    set
                    {
                        var t = new { value = value };
                    }
                }
            }
            """,
            testHost,
            Keyword("class"),
            Class("C"),
            Punctuation.OpenCurly,
            Keyword("int"),
            Property("P"),
            Punctuation.OpenCurly,
            Keyword("set"),
            Punctuation.OpenCurly,
            Keyword("var"),
            Local("t"),
            Operators.Equals,
            Keyword("new"),
            Punctuation.OpenCurly,
            Identifier("value"),
            Operators.Equals,
            Identifier("value"),
            Punctuation.CloseCurly,
            Punctuation.Semicolon,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538680")]
    [CombinatorialData]
    public Task TestValueInLabel(TestHost testHost)
        => TestAsync(
            """
            class C
            {
                int X
                {
                    set
                    {
                    value:
                        ;
                    }
                }
            }
            """,
            testHost,
            Keyword("class"),
            Class("C"),
            Punctuation.OpenCurly,
            Keyword("int"),
            Property("X"),
            Punctuation.OpenCurly,
            Keyword("set"),
            Punctuation.OpenCurly,
            Label("value"),
            Punctuation.Colon,
            Punctuation.Semicolon,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541150")]
    [CombinatorialData]
    public Task TestGenericVar(TestHost testHost)
        => TestAsync(
            """
            using System;

            static class Program
            {
                static void Main()
                {
                    var x = 1;
                }
            }

            class var<T>
            {
            }
            """,
            testHost,
            Keyword("using"),
            Identifier("System"),
            Punctuation.Semicolon,
            Keyword("static"),
            Keyword("class"),
            Class("Program"),
            Static("Program"),
            Punctuation.OpenCurly,
            Keyword("static"),
            Keyword("void"),
            Method("Main"),
            Static("Main"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Keyword("var"),
            Local("x"),
            Operators.Equals,
            Number("1"),
            Punctuation.Semicolon,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly,
            Keyword("class"),
            Class("var"),
            Punctuation.OpenAngle,
            TypeParameter("T"),
            Punctuation.CloseAngle,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541154")]
    [CombinatorialData]
    public Task TestInaccessibleVar(TestHost testHost)
        => TestAsync(
            """
            using System;

            class A
            {
                private class var
                {
                }
            }

            class B : A
            {
                static void Main()
                {
                    var x = 1;
                }
            }
            """,
            testHost,
            Keyword("using"),
            Identifier("System"),
            Punctuation.Semicolon,
            Keyword("class"),
            Class("A"),
            Punctuation.OpenCurly,
            Keyword("private"),
            Keyword("class"),
            Class("var"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly,
            Keyword("class"),
            Class("B"),
            Punctuation.Colon,
            Identifier("A"),
            Punctuation.OpenCurly,
            Keyword("static"),
            Keyword("void"),
            Method("Main"),
            Static("Main"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Keyword("var"),
            Local("x"),
            Operators.Equals,
            Number("1"),
            Punctuation.Semicolon,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541613")]
    [CombinatorialData]
    public Task TestEscapedVar(TestHost testHost)
        => TestAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    @var v = 1;
                }
            }
            """,
            testHost,
            Keyword("class"),
            Class("Program"),
            Punctuation.OpenCurly,
            Keyword("static"),
            Keyword("void"),
            Method("Main"),
            Static("Main"),
            Punctuation.OpenParen,
            Keyword("string"),
            Punctuation.OpenBracket,
            Punctuation.CloseBracket,
            Parameter("args"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Identifier("@var"),
            Local("v"),
            Operators.Equals,
            Number("1"),
            Punctuation.Semicolon,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542432")]
    [CombinatorialData]
    public Task TestVar(TestHost testHost)
        => TestAsync(
            """
            class Program
            {
                class var<T>
                {
                }

                static var<int> GetVarT()
                {
                    return null;
                }

                static void Main()
                {
                    var x = GetVarT();
                    var y = new var<int>();
                }
            }
            """,
            testHost,
            Keyword("class"),
            Class("Program"),
            Punctuation.OpenCurly,
            Keyword("class"),
            Class("var"),
            Punctuation.OpenAngle,
            TypeParameter("T"),
            Punctuation.CloseAngle,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Keyword("static"),
            Identifier("var"),
            Punctuation.OpenAngle,
            Keyword("int"),
            Punctuation.CloseAngle,
            Method("GetVarT"),
            Static("GetVarT"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            ControlKeyword("return"),
            Keyword("null"),
            Punctuation.Semicolon,
            Punctuation.CloseCurly,
            Keyword("static"),
            Keyword("void"),
            Method("Main"),
            Static("Main"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Keyword("var"),
            Local("x"),
            Operators.Equals,
            Identifier("GetVarT"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.Semicolon,
            Keyword("var"),
            Local("y"),
            Operators.Equals,
            Keyword("new"),
            Identifier("var"),
            Punctuation.OpenAngle,
            Keyword("int"),
            Punctuation.CloseAngle,
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.Semicolon,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543123")]
    [CombinatorialData]
    public Task TestVar2(TestHost testHost)
        => TestAsync(
            """
            class Program
            {
                void Main(string[] args)
                {
                    foreach (var v in args)
                    {
                    }
                }
            }
            """,
            testHost,
            Keyword("class"),
            Class("Program"),
            Punctuation.OpenCurly,
            Keyword("void"),
            Method("Main"),
            Punctuation.OpenParen,
            Keyword("string"),
            Punctuation.OpenBracket,
            Punctuation.CloseBracket,
            Parameter("args"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            ControlKeyword("foreach"),
            Punctuation.OpenParen,
            Identifier("var"),
            Local("v"),
            ControlKeyword("in"),
            Identifier("args"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task InterpolatedStrings1(TestHost testHost)
        => TestInMethodAsync("""

            var x = "World";
            var y = $"Hello, {x}";

            """,
            testHost,
            Keyword("var"),
            Local("x"),
            Operators.Equals,
            String("""
                "World"
                """),
            Punctuation.Semicolon,
            Keyword("var"),
            Local("y"),
            Operators.Equals,
            String("""
                $"
                """),
            String("Hello, "),
            Punctuation.OpenCurly,
            Identifier("x"),
            Punctuation.CloseCurly,
            String("""
                "
                """),
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    public Task InterpolatedStrings2(TestHost testHost)
        => TestInMethodAsync("""

            var a = "Hello";
            var b = "World";
            var c = $"{a}, {b}";

            """,
            testHost,
            Keyword("var"),
            Local("a"),
            Operators.Equals,
            String("""
                "Hello"
                """),
            Punctuation.Semicolon,
            Keyword("var"),
            Local("b"),
            Operators.Equals,
            String("""
                "World"
                """),
            Punctuation.Semicolon,
            Keyword("var"),
            Local("c"),
            Operators.Equals,
            String("""
                $"
                """),
            Punctuation.OpenCurly,
            Identifier("a"),
            Punctuation.CloseCurly,
            String(", "),
            Punctuation.OpenCurly,
            Identifier("b"),
            Punctuation.CloseCurly,
            String("""
                "
                """),
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    public Task InterpolatedStrings3(TestHost testHost)
        => TestInMethodAsync("""

            var a = "Hello";
            var b = "World";
            var c = $@"{a}, {b}";

            """,
            testHost,
            Keyword("var"),
            Local("a"),
            Operators.Equals,
            String("""
                "Hello"
                """),
            Punctuation.Semicolon,
            Keyword("var"),
            Local("b"),
            Operators.Equals,
            String("""
                "World"
                """),
            Punctuation.Semicolon,
            Keyword("var"),
            Local("c"),
            Operators.Equals,
            Verbatim("""
                $@"
                """),
            Punctuation.OpenCurly,
            Identifier("a"),
            Punctuation.CloseCurly,
            Verbatim(", "),
            Punctuation.OpenCurly,
            Identifier("b"),
            Punctuation.CloseCurly,
            Verbatim("""
                "
                """),
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    public Task ExceptionFilter1(TestHost testHost)
        => TestInMethodAsync("""

            try
            {
            }
            catch when (true)
            {
            }

            """,
            testHost,
            ControlKeyword("try"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            ControlKeyword("catch"),
            ControlKeyword("when"),
            Punctuation.OpenParen,
            Keyword("true"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task ExceptionFilter2(TestHost testHost)
        => TestInMethodAsync("""

            try
            {
            }
            catch (System.Exception) when (true)
            {
            }

            """,
            testHost,
            ControlKeyword("try"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            ControlKeyword("catch"),
            Punctuation.OpenParen,
            Identifier("System"),
            Operators.Dot,
            Identifier("Exception"),
            Punctuation.CloseParen,
            ControlKeyword("when"),
            Punctuation.OpenParen,
            Keyword("true"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task OutVar(TestHost testHost)
        => TestInMethodAsync("""

            F(out var);
            """,
            testHost,
            Identifier("F"),
            Punctuation.OpenParen,
            Keyword("out"),
            Identifier("var"),
            Punctuation.CloseParen,
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    public Task ReferenceDirective(TestHost testHost)
        => TestAsync("""

            #r "file.dll"
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword("r"),
            String("""
                "file.dll"
                """));

    [Theory, CombinatorialData]
    public Task LoadDirective(TestHost testHost)
        => TestAsync("""

            #load "file.csx"
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword("load"),
            String("""
                "file.csx"
                """));

    [Theory, CombinatorialData]
    public Task IncompleteAwaitInNonAsyncContext(TestHost testHost)
        => TestInClassAsync("""

            void M()
            {
                var x = await
            }
            """,
            testHost,
            Keyword("void"),
            Method("M"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Keyword("var"),
            Local("x"),
            Operators.Equals,
            Keyword("await"),
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task CompleteAwaitInNonAsyncContext(TestHost testHost)
        => TestInClassAsync("""

            void M()
            {
                var x = await;
            }
            """,
            testHost,
            Keyword("void"),
            Method("M"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Keyword("var"),
            Local("x"),
            Operators.Equals,
            Identifier("await"),
            Punctuation.Semicolon,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task TupleDeclaration(TestHost testHost)
        => TestInMethodAsync("(int, string) x",
            testHost,
            ParseOptions(TestOptions.Regular, Options.Script),
            Punctuation.OpenParen,
            Keyword("int"),
            Punctuation.Comma,
            Keyword("string"),
            Punctuation.CloseParen,
            Local("x"));

    [Theory, CombinatorialData]
    public Task TupleDeclarationWithNames(TestHost testHost)
        => TestInMethodAsync("(int a, string b) x",
            testHost,
            ParseOptions(TestOptions.Regular, Options.Script),
            Punctuation.OpenParen,
            Keyword("int"),
            Identifier("a"),
            Punctuation.Comma,
            Keyword("string"),
            Identifier("b"),
            Punctuation.CloseParen,
            Local("x"));

    [Theory, CombinatorialData]
    public Task TupleLiteral(TestHost testHost)
        => TestInMethodAsync("var values = (1, 2)",
            testHost,
            ParseOptions(TestOptions.Regular, Options.Script),
            Keyword("var"),
            Local("values"),
            Operators.Equals,
            Punctuation.OpenParen,
            Number("1"),
            Punctuation.Comma,
            Number("2"),
            Punctuation.CloseParen);

    [Theory, CombinatorialData]
    public Task TupleLiteralWithNames(TestHost testHost)
        => TestInMethodAsync("var values = (a: 1, b: 2)",
            testHost,
            ParseOptions(TestOptions.Regular, Options.Script),
            Keyword("var"),
            Local("values"),
            Operators.Equals,
            Punctuation.OpenParen,
            Identifier("a"),
            Punctuation.Colon,
            Number("1"),
            Punctuation.Comma,
            Identifier("b"),
            Punctuation.Colon,
            Number("2"),
            Punctuation.CloseParen);

    [Theory, CombinatorialData]
    public Task TestConflictMarkers1(TestHost testHost)
        => TestAsync(
            """
            class C
            {
            <<<<<<< Start
                public void Goo();
            =======
                public void Bar();
            >>>>>>> End
            }
            """,
            testHost,
            Keyword("class"),
            Class("C"),
            Punctuation.OpenCurly,
            Comment("<<<<<<< Start"),
            Keyword("public"),
            Keyword("void"),
            Method("Goo"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.Semicolon,
            Comment("======="),
            Keyword("public"),
            Keyword("void"),
            Identifier("Bar"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.Semicolon,
            Comment(">>>>>>> End"),
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task TestConflictMarkers2(TestHost testHost)
        => TestAsync(
            """
            class C
            {
            <<<<<<< Start
                public void Goo();
            ||||||| Baseline
                int removed;
            =======
                public void Bar();
            >>>>>>> End
            }
            """,
            testHost,
            Keyword("class"),
            Class("C"),
            Punctuation.OpenCurly,
            Comment("<<<<<<< Start"),
            Keyword("public"),
            Keyword("void"),
            Method("Goo"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.Semicolon,
            Comment("||||||| Baseline"),
            Keyword("int"),
            Identifier("removed"),
            Punctuation.Semicolon,
            Comment("======="),
            Keyword("public"),
            Keyword("void"),
            Identifier("Bar"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.Semicolon,
            Comment(">>>>>>> End"),
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task TestUnmanagedConstraint_InsideMethod(TestHost testHost)
        => TestInMethodAsync("""

            var unmanaged = 0;
            unmanaged++;
            """,
            testHost,
            Keyword("var"),
            Local("unmanaged"),
            Operators.Equals,
            Number("0"),
            Punctuation.Semicolon,
            Identifier("unmanaged"),
            Operators.PlusPlus,
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    public Task TestUnmanagedConstraint_Type_Keyword(TestHost testHost)
        => TestAsync(
            "class X<T> where T : unmanaged { }",
            testHost,
            Keyword("class"),
            Class("X"),
            Punctuation.OpenAngle,
            TypeParameter("T"),
            Punctuation.CloseAngle,
            Keyword("where"),
            Identifier("T"),
            Punctuation.Colon,
            Keyword("unmanaged"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task TestUnmanagedConstraint_Type_ExistingInterface(TestHost testHost)
        => TestAsync("""

            interface unmanaged {}
            class X<T> where T : unmanaged { }
            """,
            testHost,
            Keyword("interface"),
            Interface("unmanaged"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Keyword("class"),
            Class("X"),
            Punctuation.OpenAngle,
            TypeParameter("T"),
            Punctuation.CloseAngle,
            Keyword("where"),
            Identifier("T"),
            Punctuation.Colon,
            Keyword("unmanaged"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task TestUnmanagedConstraint_Type_ExistingInterfaceButOutOfScope(TestHost testHost)
        => TestAsync("""

            namespace OtherScope
            {
                interface unmanaged {}
            }
            class X<T> where T : unmanaged { }
            """,
            testHost,
            Keyword("namespace"),
            Namespace("OtherScope"),
            Punctuation.OpenCurly,
            Keyword("interface"),
            Interface("unmanaged"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly,
            Keyword("class"),
            Class("X"),
            Punctuation.OpenAngle,
            TypeParameter("T"),
            Punctuation.CloseAngle,
            Keyword("where"),
            Identifier("T"),
            Punctuation.Colon,
            Keyword("unmanaged"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task TestUnmanagedConstraint_Method_Keyword(TestHost testHost)
        => TestAsync("""

            class X
            {
                void M<T>() where T : unmanaged { }
            }
            """,
            testHost,
            Keyword("class"),
            Class("X"),
            Punctuation.OpenCurly,
            Keyword("void"),
            Method("M"),
            Punctuation.OpenAngle,
            TypeParameter("T"),
            Punctuation.CloseAngle,
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Keyword("where"),
            Identifier("T"),
            Punctuation.Colon,
            Keyword("unmanaged"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task TestUnmanagedConstraint_Method_ExistingInterface(TestHost testHost)
        => TestAsync("""

            interface unmanaged {}
            class X
            {
                void M<T>() where T : unmanaged { }
            }
            """,
            testHost,
            Keyword("interface"),
            Interface("unmanaged"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Keyword("class"),
            Class("X"),
            Punctuation.OpenCurly,
            Keyword("void"),
            Method("M"),
            Punctuation.OpenAngle,
            TypeParameter("T"),
            Punctuation.CloseAngle,
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Keyword("where"),
            Identifier("T"),
            Punctuation.Colon,
            Keyword("unmanaged"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task TestUnmanagedConstraint_Method_ExistingInterfaceButOutOfScope(TestHost testHost)
        => TestAsync("""

            namespace OtherScope
            {
                interface unmanaged {}
            }
            class X
            {
                void M<T>() where T : unmanaged { }
            }
            """,
            testHost,
            Keyword("namespace"),
            Namespace("OtherScope"),
            Punctuation.OpenCurly,
            Keyword("interface"),
            Interface("unmanaged"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly,
            Keyword("class"),
            Class("X"),
            Punctuation.OpenCurly,
            Keyword("void"),
            Method("M"),
            Punctuation.OpenAngle,
            TypeParameter("T"),
            Punctuation.CloseAngle,
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Keyword("where"),
            Identifier("T"),
            Punctuation.Colon,
            Keyword("unmanaged"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task TestUnmanagedConstraint_Delegate_Keyword(TestHost testHost)
        => TestAsync(
            "delegate void D<T>() where T : unmanaged;",
            testHost,
            Keyword("delegate"),
            Keyword("void"),
            Delegate("D"),
            Punctuation.OpenAngle,
            TypeParameter("T"),
            Punctuation.CloseAngle,
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Keyword("where"),
            Identifier("T"),
            Punctuation.Colon,
            Keyword("unmanaged"),
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    public Task TestUnmanagedConstraint_Delegate_ExistingInterface(TestHost testHost)
        => TestAsync("""

            interface unmanaged {}
            delegate void D<T>() where T : unmanaged;
            """,
            testHost,
            Keyword("interface"),
            Interface("unmanaged"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Keyword("delegate"),
            Keyword("void"),
            Delegate("D"),
            Punctuation.OpenAngle,
            TypeParameter("T"),
            Punctuation.CloseAngle,
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Keyword("where"),
            Identifier("T"),
            Punctuation.Colon,
            Keyword("unmanaged"),
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    public Task TestUnmanagedConstraint_Delegate_ExistingInterfaceButOutOfScope(TestHost testHost)
        => TestAsync("""

            namespace OtherScope
            {
                interface unmanaged {}
            }
            delegate void D<T>() where T : unmanaged;
            """,
            testHost,
            Keyword("namespace"),
            Namespace("OtherScope"),
            Punctuation.OpenCurly,
            Keyword("interface"),
            Interface("unmanaged"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly,
            Keyword("delegate"),
            Keyword("void"),
            Delegate("D"),
            Punctuation.OpenAngle,
            TypeParameter("T"),
            Punctuation.CloseAngle,
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Keyword("where"),
            Identifier("T"),
            Punctuation.Colon,
            Keyword("unmanaged"),
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    public Task TestUnmanagedConstraint_LocalFunction_Keyword(TestHost testHost)
        => TestAsync("""

            class X
            {
                void N()
                {
                    void M<T>() where T : unmanaged { }
                }
            }
            """,
            testHost,
            Keyword("class"),
            Class("X"),
            Punctuation.OpenCurly,
            Keyword("void"),
            Method("N"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Keyword("void"),
            Method("M"),
            Punctuation.OpenAngle,
            TypeParameter("T"),
            Punctuation.CloseAngle,
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Keyword("where"),
            Identifier("T"),
            Punctuation.Colon,
            Keyword("unmanaged"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task TestUnmanagedConstraint_LocalFunction_ExistingInterface(TestHost testHost)
        => TestAsync("""

            interface unmanaged {}
            class X
            {
                void N()
                {
                    void M<T>() where T : unmanaged { }
                }
            }
            """,
            testHost,
            Keyword("interface"),
            Interface("unmanaged"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Keyword("class"),
            Class("X"),
            Punctuation.OpenCurly,
            Keyword("void"),
            Method("N"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Keyword("void"),
            Method("M"),
            Punctuation.OpenAngle,
            TypeParameter("T"),
            Punctuation.CloseAngle,
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Keyword("where"),
            Identifier("T"),
            Punctuation.Colon,
            Keyword("unmanaged"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task TestUnmanagedConstraint_LocalFunction_ExistingInterfaceButOutOfScope(TestHost testHost)
        => TestAsync("""

            namespace OtherScope
            {
                interface unmanaged {}
            }
            class X
            {
                void N()
                {
                    void M<T>() where T : unmanaged { }
                }
            }
            """,
            testHost,
            Keyword("namespace"),
            Namespace("OtherScope"),
            Punctuation.OpenCurly,
            Keyword("interface"),
            Interface("unmanaged"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly,
            Keyword("class"),
            Class("X"),
            Punctuation.OpenCurly,
            Keyword("void"),
            Method("N"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Keyword("void"),
            Method("M"),
            Punctuation.OpenAngle,
            TypeParameter("T"),
            Punctuation.CloseAngle,
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Keyword("where"),
            Identifier("T"),
            Punctuation.Colon,
            Keyword("unmanaged"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task TestDeclarationIsPattern(TestHost testHost)
        => TestInMethodAsync("""

            object foo;

            if (foo is Action action)
            {
            }
            """,
            testHost,
            Keyword("object"),
            Local("foo"),
            Punctuation.Semicolon,
            ControlKeyword("if"),
            Punctuation.OpenParen,
            Identifier("foo"),
            Keyword("is"),
            Identifier("Action"),
            Local("action"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task TestDeclarationSwitchPattern(TestHost testHost)
        => TestInMethodAsync("""

            object y;

            switch (y)
            {
                case int x:
                    break;
            }
            """,
testHost,
            Keyword("object"),
            Local("y"),
            Punctuation.Semicolon,
            ControlKeyword("switch"),
            Punctuation.OpenParen,
            Identifier("y"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            ControlKeyword("case"),
            Keyword("int"),
            Local("x"),
            Punctuation.Colon,
            ControlKeyword("break"),
            Punctuation.Semicolon,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task TestDeclarationExpression(TestHost testHost)
        => TestInMethodAsync("""

            int (foo, bar) = (1, 2);
            """,
            testHost,
            Keyword("int"),
            Punctuation.OpenParen,
            Local("foo"),
            Punctuation.Comma,
            Local("bar"),
            Punctuation.CloseParen,
            Operators.Equals,
            Punctuation.OpenParen,
            Number("1"),
            Punctuation.Comma,
            Number("2"),
            Punctuation.CloseParen,
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/18956")]
    public Task TestListPattern(TestHost testHost)
        => TestInMethodAsync("""

            switch (new int[0])
            {
                case [1, 2]:
                    break;
                case [1, .. var end]:
                    break;
            }
            """,
        testHost,
        ControlKeyword("switch"),
        Punctuation.OpenParen,
        Keyword("new"),
        Keyword("int"),
        Punctuation.OpenBracket,
        Number("0"),
        Punctuation.CloseBracket,
        Punctuation.CloseParen,
        Punctuation.OpenCurly,
        ControlKeyword("case"),
        Punctuation.OpenBracket,
        Number("1"),
        Punctuation.Comma,
        Number("2"),
        Punctuation.CloseBracket,
        Punctuation.Colon,
        ControlKeyword("break"),
        Punctuation.Semicolon,
        ControlKeyword("case"),
        Punctuation.OpenBracket,
        Number("1"),
        Punctuation.Comma,
        Punctuation.DotDot,
        Keyword("var"),
        Local("end"),
        Punctuation.CloseBracket,
        Punctuation.Colon,
        ControlKeyword("break"),
        Punctuation.Semicolon,
        Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/18956")]
    public Task TestListPattern2(TestHost testHost)
        => TestInMethodAsync("""

            _ = x switch
            {
                [var start, .. var end] => 1
            }
            """,
        testHost,
        Identifier("_"),
        Operators.Equals,
        Identifier("x"),
        ControlKeyword("switch"),
        Punctuation.OpenCurly,
        Punctuation.OpenBracket,
        Keyword("var"),
        Local("start"),
        Punctuation.Comma,
        Punctuation.DotDot,
        Keyword("var"),
        Local("end"),
        Punctuation.CloseBracket,
        Operators.EqualsGreaterThan,
        Number("1"),
        Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/18956")]
    public Task TestVarPattern(TestHost testHost)
        => TestInMethodAsync("""

            _ = 1 is var x;

            """,
        testHost,
        Identifier("_"),
        Operators.Equals,
        Number("1"),
        Keyword("is"),
        Keyword("var"),
        Local("x"),
        Punctuation.Semicolon);

    [Theory, CombinatorialData]
    public Task TestTupleTypeSyntax(TestHost testHost)
        => TestInClassAsync("""

            public (int a, int b) Get() => null;
            """,
            testHost,
            Keyword("public"),
            Punctuation.OpenParen,
            Keyword("int"),
            Identifier("a"),
            Punctuation.Comma,
            Keyword("int"),
            Identifier("b"),
            Punctuation.CloseParen,
            Method("Get"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Operators.EqualsGreaterThan,
            Keyword("null"),
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    public Task TestOutParameter(TestHost testHost)
        => TestInMethodAsync("""

            if (int.TryParse("1", out int x))
            {
            }
            """,
            testHost,
            ControlKeyword("if"),
            Punctuation.OpenParen,
            Keyword("int"),
            Operators.Dot,
            Identifier("TryParse"),
            Punctuation.OpenParen,
            String("""
                "1"
                """),
            Punctuation.Comma,
            Keyword("out"),
            Keyword("int"),
            Local("x"),
            Punctuation.CloseParen,
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task TestOutParameter2(TestHost testHost)
        => TestInClassAsync("""

            int F = int.TryParse("1", out int x) ? x : -1;

            """,
            testHost,
            Keyword("int"),
            Field("F"),
            Operators.Equals,
            Keyword("int"),
            Operators.Dot,
            Identifier("TryParse"),
            Punctuation.OpenParen,
            String("""
                "1"
                """),
            Punctuation.Comma,
            Keyword("out"),
            Keyword("int"),
            Local("x"),
            Punctuation.CloseParen,
            Operators.QuestionMark,
            Identifier("x"),
            Operators.Colon,
            Operators.Minus,
            Number("1"),
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    public Task TestUsingDirective(TestHost testHost)
        => TestAsync(@"using System.Collections.Generic;",
            testHost,
            Keyword("using"),
            Identifier("System"),
            Operators.Dot,
            Identifier("Collections"),
            Operators.Dot,
            Identifier("Generic"),
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    public Task TestUsingAliasDirectiveForIdentifier(TestHost testHost)
        => TestAsync(@"using Col = System.Collections;",
            testHost,
            Keyword("using"),
            Identifier("Col"),
            Operators.Equals,
            Identifier("System"),
            Operators.Dot,
            Identifier("Collections"),
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    public Task TestUsingAliasDirectiveForClass(TestHost testHost)
        => TestAsync(@"using Con = System.Console;",
            testHost,
            Keyword("using"),
            Identifier("Con"),
            Operators.Equals,
            Identifier("System"),
            Operators.Dot,
            Identifier("Console"),
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    public Task TestUsingStaticDirective(TestHost testHost)
        => TestAsync(@"using static System.Console;",
            testHost,
            Keyword("using"),
            Keyword("static"),
            Identifier("System"),
            Operators.Dot,
            Identifier("Console"),
            Punctuation.Semicolon);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/33039")]
    [CombinatorialData]
    public Task ForEachVariableStatement(TestHost testHost)
        => TestInMethodAsync("""

            foreach (var (x, y) in new[] { (1, 2) });

            """,
            testHost,
            ControlKeyword("foreach"),
            Punctuation.OpenParen,
            Identifier("var"),
            Punctuation.OpenParen,
            Local("x"),
            Punctuation.Comma,
            Local("y"),
            Punctuation.CloseParen,
            ControlKeyword("in"),
            Keyword("new"),
            Punctuation.OpenBracket,
            Punctuation.CloseBracket,
            Punctuation.OpenCurly,
            Punctuation.OpenParen,
            Number("1"),
            Punctuation.Comma,
            Number("2"),
            Punctuation.CloseParen,
            Punctuation.CloseCurly,
            Punctuation.CloseParen,
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    public Task CatchDeclarationStatement(TestHost testHost)
        => TestInMethodAsync("""

            try { } catch (Exception ex) { }

            """,
            testHost,
            ControlKeyword("try"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            ControlKeyword("catch"),
            Punctuation.OpenParen,
            Identifier("Exception"),
            Local("ex"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task TestNotNullConstraint_InsideMethod(TestHost testHost)
        => TestInMethodAsync("""

            var notnull = 0;
            notnull++;
            """,
            testHost,
            Keyword("var"),
            Local("notnull"),
            Operators.Equals,
            Number("0"),
            Punctuation.Semicolon,
            Identifier("notnull"),
            Operators.PlusPlus,
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    public Task TestNotNullConstraint_Type_Keyword(TestHost testHost)
        => TestAsync(
            "class X<T> where T : notnull { }",
            testHost,
            Keyword("class"),
            Class("X"),
            Punctuation.OpenAngle,
            TypeParameter("T"),
            Punctuation.CloseAngle,
            Keyword("where"),
            Identifier("T"),
            Punctuation.Colon,
            Keyword("notnull"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task TestNotNullConstraint_Type_ExistingInterface(TestHost testHost)
        => TestAsync("""

            interface notnull {}
            class X<T> where T : notnull { }
            """,
            testHost,
            Keyword("interface"),
            Interface("notnull"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Keyword("class"),
            Class("X"),
            Punctuation.OpenAngle,
            TypeParameter("T"),
            Punctuation.CloseAngle,
            Keyword("where"),
            Identifier("T"),
            Punctuation.Colon,
            Keyword("notnull"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task TestNotNullConstraint_Type_ExistingInterfaceButOutOfScope(TestHost testHost)
        => TestAsync("""

            namespace OtherScope
            {
                interface notnull {}
            }
            class X<T> where T : notnull { }
            """,
            testHost,
            Keyword("namespace"),
            Namespace("OtherScope"),
            Punctuation.OpenCurly,
            Keyword("interface"),
            Interface("notnull"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly,
            Keyword("class"),
            Class("X"),
            Punctuation.OpenAngle,
            TypeParameter("T"),
            Punctuation.CloseAngle,
            Keyword("where"),
            Identifier("T"),
            Punctuation.Colon,
            Keyword("notnull"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task TestNotNullConstraint_Method_Keyword(TestHost testHost)
        => TestAsync("""

            class X
            {
                void M<T>() where T : notnull { }
            }
            """,
            testHost,
            Keyword("class"),
            Class("X"),
            Punctuation.OpenCurly,
            Keyword("void"),
            Method("M"),
            Punctuation.OpenAngle,
            TypeParameter("T"),
            Punctuation.CloseAngle,
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Keyword("where"),
            Identifier("T"),
            Punctuation.Colon,
            Keyword("notnull"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task TestNotNullConstraint_Method_ExistingInterface(TestHost testHost)
        => TestAsync("""

            interface notnull {}
            class X
            {
                void M<T>() where T : notnull { }
            }
            """,
            testHost,
            Keyword("interface"),
            Interface("notnull"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Keyword("class"),
            Class("X"),
            Punctuation.OpenCurly,
            Keyword("void"),
            Method("M"),
            Punctuation.OpenAngle,
            TypeParameter("T"),
            Punctuation.CloseAngle,
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Keyword("where"),
            Identifier("T"),
            Punctuation.Colon,
            Keyword("notnull"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task TestNotNullConstraint_Method_ExistingInterfaceButOutOfScope(TestHost testHost)
        => TestAsync("""

            namespace OtherScope
            {
                interface notnull {}
            }
            class X
            {
                void M<T>() where T : notnull { }
            }
            """,
            testHost,
            Keyword("namespace"),
            Namespace("OtherScope"),
            Punctuation.OpenCurly,
            Keyword("interface"),
            Interface("notnull"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly,
            Keyword("class"),
            Class("X"),
            Punctuation.OpenCurly,
            Keyword("void"),
            Method("M"),
            Punctuation.OpenAngle,
            TypeParameter("T"),
            Punctuation.CloseAngle,
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Keyword("where"),
            Identifier("T"),
            Punctuation.Colon,
            Keyword("notnull"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task TestNotNullConstraint_Delegate_Keyword(TestHost testHost)
        => TestAsync(
            "delegate void D<T>() where T : notnull;",
            testHost,
            Keyword("delegate"),
            Keyword("void"),
            Delegate("D"),
            Punctuation.OpenAngle,
            TypeParameter("T"),
            Punctuation.CloseAngle,
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Keyword("where"),
            Identifier("T"),
            Punctuation.Colon,
            Keyword("notnull"),
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    public Task TestNotNullConstraint_Delegate_ExistingInterface(TestHost testHost)
        => TestAsync("""

            interface notnull {}
            delegate void D<T>() where T : notnull;
            """,
            testHost,
            Keyword("interface"),
            Interface("notnull"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Keyword("delegate"),
            Keyword("void"),
            Delegate("D"),
            Punctuation.OpenAngle,
            TypeParameter("T"),
            Punctuation.CloseAngle,
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Keyword("where"),
            Identifier("T"),
            Punctuation.Colon,
            Keyword("notnull"),
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    public Task TestNotNullConstraint_Delegate_ExistingInterfaceButOutOfScope(TestHost testHost)
        => TestAsync("""

            namespace OtherScope
            {
                interface notnull {}
            }
            delegate void D<T>() where T : notnull;
            """,
            testHost,
            Keyword("namespace"),
            Namespace("OtherScope"),
            Punctuation.OpenCurly,
            Keyword("interface"),
            Interface("notnull"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly,
            Keyword("delegate"),
            Keyword("void"),
            Delegate("D"),
            Punctuation.OpenAngle,
            TypeParameter("T"),
            Punctuation.CloseAngle,
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Keyword("where"),
            Identifier("T"),
            Punctuation.Colon,
            Keyword("notnull"),
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    public Task TestNotNullConstraint_LocalFunction_Keyword(TestHost testHost)
        => TestAsync("""

            class X
            {
                void N()
                {
                    void M<T>() where T : notnull { }
                }
            }
            """,
            testHost,
            Keyword("class"),
            Class("X"),
            Punctuation.OpenCurly,
            Keyword("void"),
            Method("N"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Keyword("void"),
            Method("M"),
            Punctuation.OpenAngle,
            TypeParameter("T"),
            Punctuation.CloseAngle,
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Keyword("where"),
            Identifier("T"),
            Punctuation.Colon,
            Keyword("notnull"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task TestNotNullConstraint_LocalFunction_ExistingInterface(TestHost testHost)
        => TestAsync("""

            interface notnull {}
            class X
            {
                void N()
                {
                    void M<T>() where T : notnull { }
                }
            }
            """,
            testHost,
            Keyword("interface"),
            Interface("notnull"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Keyword("class"),
            Class("X"),
            Punctuation.OpenCurly,
            Keyword("void"),
            Method("N"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Keyword("void"),
            Method("M"),
            Punctuation.OpenAngle,
            TypeParameter("T"),
            Punctuation.CloseAngle,
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Keyword("where"),
            Identifier("T"),
            Punctuation.Colon,
            Keyword("notnull"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task TestNotNullConstraint_LocalFunction_ExistingInterfaceButOutOfScope(TestHost testHost)
        => TestAsync("""

            namespace OtherScope
            {
                interface notnull {}
            }
            class X
            {
                void N()
                {
                    void M<T>() where T : notnull { }
                }
            }
            """,
            testHost,
            Keyword("namespace"),
            Namespace("OtherScope"),
            Punctuation.OpenCurly,
            Keyword("interface"),
            Interface("notnull"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly,
            Keyword("class"),
            Class("X"),
            Punctuation.OpenCurly,
            Keyword("void"),
            Method("N"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Keyword("void"),
            Method("M"),
            Punctuation.OpenAngle,
            TypeParameter("T"),
            Punctuation.CloseAngle,
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Keyword("where"),
            Identifier("T"),
            Punctuation.Colon,
            Keyword("notnull"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/45807")]
    public Task FunctionPointer(TestHost testHost)
        => TestAsync("""

            class C
            {
                delegate* unmanaged[Stdcall, SuppressGCTransition] <int, int> x;
            }
            """,
            testHost,
            Keyword("class"),
            Class("C"),
            Punctuation.OpenCurly,
            Keyword("delegate"),
            Operators.Asterisk,
            Keyword("unmanaged"),
            Punctuation.OpenBracket,
            Identifier("Stdcall"),
            Punctuation.Comma,
            Identifier("SuppressGCTransition"),
            Punctuation.CloseBracket,
            Punctuation.OpenAngle,
            Keyword("int"),
            Punctuation.Comma,
            Keyword("int"),
            Punctuation.CloseAngle,
            Field("x"),
            Punctuation.Semicolon,
            Punctuation.CloseCurly);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48094")]
    public async Task TestXmlAttributeNameSpan1()
    {
        var source = @"/// <param name=""value""></param>";
        using var workspace = CreateWorkspace(source, options: null, TestHost.InProcess);
        var document = workspace.CurrentSolution.Projects.First().Documents.First();

        var classifications = await GetSyntacticClassificationsAsync(document, [new TextSpan(0, source.Length)]);
        AssertEx.Equal(
        [
            new ClassifiedSpan(ClassificationTypeNames.XmlDocCommentDelimiter, new TextSpan(0, 3)),
            new ClassifiedSpan(ClassificationTypeNames.XmlDocCommentText, new TextSpan(3, 1)),
            new ClassifiedSpan(ClassificationTypeNames.XmlDocCommentDelimiter, new TextSpan(4, 1)),
            new ClassifiedSpan(ClassificationTypeNames.XmlDocCommentName, new TextSpan(5, 5)),
            new ClassifiedSpan(ClassificationTypeNames.XmlDocCommentAttributeName, new TextSpan(11, 4)),
            new ClassifiedSpan(ClassificationTypeNames.XmlDocCommentDelimiter, new TextSpan(15, 1)),
            new ClassifiedSpan(ClassificationTypeNames.XmlDocCommentAttributeQuotes, new TextSpan(16, 1)),
            new ClassifiedSpan(ClassificationTypeNames.Identifier, new TextSpan(17, 5)),
            new ClassifiedSpan(ClassificationTypeNames.XmlDocCommentAttributeQuotes, new TextSpan(22, 1)),
            new ClassifiedSpan(ClassificationTypeNames.XmlDocCommentDelimiter, new TextSpan(23, 1)),
            new ClassifiedSpan(ClassificationTypeNames.XmlDocCommentDelimiter, new TextSpan(24, 2)),
            new ClassifiedSpan(ClassificationTypeNames.XmlDocCommentName, new TextSpan(26, 5)),
            new ClassifiedSpan(ClassificationTypeNames.XmlDocCommentDelimiter, new TextSpan(31, 1))
        ], classifications);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48094")]
    public async Task TestXmlAttributeNameSpan2()
    {
        var source = """

            /// <param
            /// name="value"></param>
            """;
        using var workspace = CreateWorkspace(source, options: null, TestHost.InProcess);
        var document = workspace.CurrentSolution.Projects.First().Documents.First();

        var classifications = await GetSyntacticClassificationsAsync(document, [new TextSpan(0, source.Length)]);
        AssertEx.Equal(
        [
            new ClassifiedSpan(ClassificationTypeNames.XmlDocCommentDelimiter, new TextSpan(2, 3)),
            new ClassifiedSpan(ClassificationTypeNames.XmlDocCommentText, new TextSpan(5, 1)),
            new ClassifiedSpan(ClassificationTypeNames.XmlDocCommentDelimiter, new TextSpan(6, 1)),
            new ClassifiedSpan(ClassificationTypeNames.XmlDocCommentName, new TextSpan(7, 5)),
            new ClassifiedSpan(ClassificationTypeNames.XmlDocCommentDelimiter, new TextSpan(14, 3)),
            new ClassifiedSpan(ClassificationTypeNames.XmlDocCommentAttributeName, new TextSpan(18, 4)),
            new ClassifiedSpan(ClassificationTypeNames.XmlDocCommentDelimiter, new TextSpan(22, 1)),
            new ClassifiedSpan(ClassificationTypeNames.XmlDocCommentAttributeQuotes, new TextSpan(23, 1)),
            new ClassifiedSpan(ClassificationTypeNames.Identifier, new TextSpan(24, 5)),
            new ClassifiedSpan(ClassificationTypeNames.XmlDocCommentAttributeQuotes, new TextSpan(29, 1)),
            new ClassifiedSpan(ClassificationTypeNames.XmlDocCommentDelimiter, new TextSpan(30, 1)),
            new ClassifiedSpan(ClassificationTypeNames.XmlDocCommentDelimiter, new TextSpan(31, 2)),
            new ClassifiedSpan(ClassificationTypeNames.XmlDocCommentName, new TextSpan(33, 5)),
            new ClassifiedSpan(ClassificationTypeNames.XmlDocCommentDelimiter, new TextSpan(38, 1))
        ], classifications);
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/52290")]
    [CombinatorialData]
    public Task TestStaticLocalFunction(TestHost testHost)
        => TestAsync("""

            class C
            {
                public static void M()
                {
                    static void LocalFunc() { }
                }
            }
            """,
            testHost,
            Keyword("class"),
            Class("C"),
            Punctuation.OpenCurly,
            Keyword("public"),
            Keyword("static"),
            Keyword("void"),
            Method("M"),
            Static("M"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Keyword("static"),
            Keyword("void"),
            Method("LocalFunc"),
            Static("LocalFunc"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/52290")]
    [CombinatorialData]
    public Task TestConstantLocalVariable(TestHost testHost)
        => TestAsync("""

            class C
            {
                public static void M()
                {
                    const int Zero = 0;
                }
            }
            """,
            testHost,
            Keyword("class"),
            Class("C"),
            Punctuation.OpenCurly,
            Keyword("public"),
            Keyword("static"),
            Keyword("void"),
            Method("M"),
            Static("M"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Keyword("const"),
            Keyword("int"),
            Constant("Zero"),
            Static("Zero"),
            Operators.Equals,
            Number("0"),
            Punctuation.Semicolon,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task TestRawStringLiteral(TestHost testHost)
        => TestAsync(""""

            class C
            {
                public static void M(int x)
                {
                    var s = """Hello world""";
                }
            }
            """",
            testHost,
            Keyword("class"),
            Class("C"),
            Punctuation.OpenCurly,
            Keyword("public"),
            Keyword("static"),
            Keyword("void"),
            Method("M"),
            Static("M"),
            Punctuation.OpenParen,
            Keyword("int"),
            Parameter("x"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Keyword("var"),
            Local("s"),
            Operators.Equals,
            String(""""
                """Hello world"""
                """"),
            Punctuation.Semicolon,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task TestRawStringLiteralUtf8_01(TestHost testHost)
        => TestAsync(""""

            class C
            {
                public static void M(int x)
                {
                    var s = """Hello world"""u8;
                }
            }
            """",
            testHost,
            Keyword("class"),
            Class("C"),
            Punctuation.OpenCurly,
            Keyword("public"),
            Keyword("static"),
            Keyword("void"),
            Method("M"),
            Static("M"),
            Punctuation.OpenParen,
            Keyword("int"),
            Parameter("x"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Keyword("var"),
            Local("s"),
            Operators.Equals,
            String(""""
                """Hello world"""
                """"),
            Keyword("u8"),
            Punctuation.Semicolon,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task TestRawStringLiteralUtf8_02(TestHost testHost)
        => TestAsync(""""

            class C
            {
                public static void M(int x)
                {
                    var s = """Hello world"""U8;
                }
            }
            """",
            testHost,
            Keyword("class"),
            Class("C"),
            Punctuation.OpenCurly,
            Keyword("public"),
            Keyword("static"),
            Keyword("void"),
            Method("M"),
            Static("M"),
            Punctuation.OpenParen,
            Keyword("int"),
            Parameter("x"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Keyword("var"),
            Local("s"),
            Operators.Equals,
            String(""""
                """Hello world"""
                """"),
            Keyword("U8"),
            Punctuation.Semicolon,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task TestRawStringLiteralMultiline(TestHost testHost)
        => TestAsync(""""

            class C
            {
                public static void M(int x)
                {
                    var s = """
                  Hello world
               """;
                }
            }
            """",
            testHost,
            Keyword("class"),
            Class("C"),
            Punctuation.OpenCurly,
            Keyword("public"),
            Keyword("static"),
            Keyword("void"),
            Method("M"),
            Static("M"),
            Punctuation.OpenParen,
            Keyword("int"),
            Parameter("x"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Keyword("var"),
            Local("s"),
            Operators.Equals,
            String(""""
                """
                      Hello world
                   """
                """"),
            Punctuation.Semicolon,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task TestRawStringLiteralMultilineUtf8_01(TestHost testHost)
        => TestAsync(""""

            class C
            {
                public static void M(int x)
                {
                    var s = """
                  Hello world
               """u8;
                }
            }
            """",
            testHost,
            Keyword("class"),
            Class("C"),
            Punctuation.OpenCurly,
            Keyword("public"),
            Keyword("static"),
            Keyword("void"),
            Method("M"),
            Static("M"),
            Punctuation.OpenParen,
            Keyword("int"),
            Parameter("x"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Keyword("var"),
            Local("s"),
            Operators.Equals,
            String(""""
                """
                      Hello world
                   """
                """"),
            Keyword("u8"),
            Punctuation.Semicolon,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task TestRawStringLiteralMultilineUtf8_02(TestHost testHost)
        => TestAsync(""""

            class C
            {
                public static void M(int x)
                {
                    var s = """
                  Hello world
               """U8;
                }
            }
            """",
            testHost,
            Keyword("class"),
            Class("C"),
            Punctuation.OpenCurly,
            Keyword("public"),
            Keyword("static"),
            Keyword("void"),
            Method("M"),
            Static("M"),
            Punctuation.OpenParen,
            Keyword("int"),
            Parameter("x"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Keyword("var"),
            Local("s"),
            Operators.Equals,
            String(""""
                """
                      Hello world
                   """
                """"),
            Keyword("U8"),
            Punctuation.Semicolon,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task TestRawStringLiteralInterpolation1(TestHost testHost)
        => TestAsync(""""

            class C
            {
                public static void M(int x)
                {
                    var s = $"""{x}""";
                }
            }
            """",
            testHost,
            Keyword("class"),
            Class("C"),
            Punctuation.OpenCurly,
            Keyword("public"),
            Keyword("static"),
            Keyword("void"),
            Method("M"),
            Static("M"),
            Punctuation.OpenParen,
            Keyword("int"),
            Parameter("x"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Keyword("var"),
            Local("s"),
            Operators.Equals,
            String(""""
                $"""
                """"),
            Punctuation.OpenCurly,
            Identifier("x"),
            Punctuation.CloseCurly,
            String(""""
                """
                """"),
            Punctuation.Semicolon,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task TestRawStringLiteralInterpolation2(TestHost testHost)
        => TestAsync(""""

            class C
            {
                public static void M(int x)
                {
                    var s = $$"""{{x}}""";
                }
            }
            """",
            testHost,
            Keyword("class"),
            Class("C"),
            Punctuation.OpenCurly,
            Keyword("public"),
            Keyword("static"),
            Keyword("void"),
            Method("M"),
            Static("M"),
            Punctuation.OpenParen,
            Keyword("int"),
            Parameter("x"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Keyword("var"),
            Local("s"),
            Operators.Equals,
            String(""""
                $$"""
                """"),
            PunctuationText("{{"),
            Identifier("x"),
            PunctuationText("}}"),
            String(""""
                """
                """"),
            Punctuation.Semicolon,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task TestRawStringLiteralInterpolation3(TestHost testHost)
        => TestAsync(""""

            class C
            {
                public static void M(int x)
                {
                    var s = $$"""{{{x}}}""";
                }
            }
            """",
            testHost,
            Keyword("class"),
            Class("C"),
            Punctuation.OpenCurly,
            Keyword("public"),
            Keyword("static"),
            Keyword("void"),
            Method("M"),
            Static("M"),
            Punctuation.OpenParen,
            Keyword("int"),
            Parameter("x"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Keyword("var"),
            Local("s"),
            Operators.Equals,
            String(""""
                $$"""
                """"),
            String("{"),
            PunctuationText("{{"),
            Identifier("x"),
            PunctuationText("}}"),
            String("}"),
            String(""""
                """
                """"),
            Punctuation.Semicolon,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task CheckedUserDefinedOperators_01(TestHost testHost)
        => TestInClassAsync(
            """

            static T operator checked -(T a)
            {
            }
            """,
            testHost,
            Keyword("static"),
            Identifier("T"),
            Keyword("operator"),
            Keyword("checked"),
            Operators.Minus,
            Punctuation.OpenParen,
            Identifier("T"),
            Parameter("a"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task CheckedUserDefinedOperators_02(TestHost testHost)
        => TestInClassAsync(
            """

            static T operator checked +(T a, T b)
            {
            }
            """,
            testHost,
            Keyword("static"),
            Identifier("T"),
            Keyword("operator"),
            Keyword("checked"),
            Operators.Plus,
            Punctuation.OpenParen,
            Identifier("T"),
            Parameter("a"),
            Punctuation.Comma,
            Identifier("T"),
            Parameter("b"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task CheckedUserDefinedOperators_03(TestHost testHost)
        => TestInClassAsync(
            """

            static explicit operator checked T(T a)
            {
            }
            """,
            testHost,
            Keyword("static"),
            Keyword("explicit"),
            Keyword("operator"),
            Keyword("checked"),
            Identifier("T"),
            Punctuation.OpenParen,
            Identifier("T"),
            Parameter("a"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task CheckedUserDefinedOperators_04(TestHost testHost)
        => TestInClassAsync(
            """

            static T I1.operator checked -(T a)
            {
            }
            """,
            testHost,
            Keyword("static"),
            Identifier("T"),
            Identifier("I1"),
            Operators.Dot,
            Keyword("operator"),
            Keyword("checked"),
            Operators.Minus,
            Punctuation.OpenParen,
            Identifier("T"),
            Parameter("a"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task CheckedUserDefinedOperators_05(TestHost testHost)
        => TestInClassAsync(
            """

            static T I1.operator checked +(T a, T b)
            {
            }
            """,
            testHost,
            Keyword("static"),
            Identifier("T"),
            Identifier("I1"),
            Operators.Dot,
            Keyword("operator"),
            Keyword("checked"),
            Operators.Plus,
            Punctuation.OpenParen,
            Identifier("T"),
            Parameter("a"),
            Punctuation.Comma,
            Identifier("T"),
            Parameter("b"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task CheckedUserDefinedOperators_06(TestHost testHost)
        => TestInClassAsync(
            """

            static explicit I1.operator checked T(T a)
            {
            }
            """,
            testHost,
            Keyword("static"),
            Keyword("explicit"),
            Identifier("I1"),
            Operators.Dot,
            Keyword("operator"),
            Keyword("checked"),
            Identifier("T"),
            Punctuation.OpenParen,
            Identifier("T"),
            Parameter("a"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task UnsignedRightShift_01(TestHost testHost)
        => TestInClassAsync(
            """

            static T operator >>>(T a, int b)
            {
            }
            """,
            testHost,
            Keyword("static"),
            Identifier("T"),
            Keyword("operator"),
            Operators.GreaterThanGreaterThanGreaterThan,
            Punctuation.OpenParen,
            Identifier("T"),
            Parameter("a"),
            Punctuation.Comma,
            Keyword("int"),
            Parameter("b"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task UnsignedRightShift_02(TestHost testHost)
        => TestInClassAsync(
            """

            static T I1.operator checked >>>(T a, T b)
            {
            }
            """,
            testHost,
            Keyword("static"),
            Identifier("T"),
            Identifier("I1"),
            Operators.Dot,
            Keyword("operator"),
            Keyword("checked"),
            Operators.GreaterThanGreaterThanGreaterThan,
            Punctuation.OpenParen,
            Identifier("T"),
            Parameter("a"),
            Punctuation.Comma,
            Identifier("T"),
            Parameter("b"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task InstanceIncrementOperator_01(TestHost testHost, [CombinatorialValues("++", "--")] string op)
        => TestInClassAsync(
            $$$"""

            void operator {{{op}}}()
            {
            }
            """,
            testHost,
            Keyword("void"),
            Keyword("operator"),
            op == "++" ? Operators.PlusPlus : Operators.MinusMinus,
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task InstanceIncrementOperator_02(TestHost testHost, [CombinatorialValues("++", "--")] string op)
        => TestInClassAsync(
            $$$"""

            void I1.operator checked {{{op}}}()
            {
            }
            """,
            testHost,
            Keyword("void"),
            Identifier("I1"),
            Operators.Dot,
            Keyword("operator"),
            Keyword("checked"),
            op == "++" ? Operators.PlusPlus : Operators.MinusMinus,
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task InstanceCompoundAssignmentOperator_01(TestHost testHost, [CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        => TestInClassAsync(
            $$$"""

            void operator {{{op}}}(T a)
            {
            }
            """,
            testHost,
            Keyword("void"),
            Keyword("operator"),
            CompoundAssignmentOperatorClassification(op),
            Punctuation.OpenParen,
            Identifier("T"),
            Parameter("a"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    private static FormattedClassification CompoundAssignmentOperatorClassification(string op)
    {
        switch (op)
        {
            case "+=": return Operators.PlusEquals;
            case "-=": return Operators.MinusEquals;
            case "*=": return Operators.AsteriskEquals;
            case "/=": return Operators.SlashEquals;
            case "%=": return Operators.PercentEquals;
            case "&=": return Operators.AmpersandEquals;
            case "|=": return Operators.BarEquals;
            case "^=": return Operators.CaretEquals;
            case "<<=": return Operators.LessThanLessThanEquals;
            case ">>=": return Operators.GreaterThanGreaterThanEquals;
            case ">>>=": return Operators.GreaterThanGreaterThanGreaterThanEquals;
            default:
                throw ExceptionUtilities.UnexpectedValue(op);
        }
    }

    [Theory, CombinatorialData]
    public Task InstanceCompoundAssignmentOperator_02(TestHost testHost, [CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        => TestInClassAsync(
            $$$"""

            void I1.operator checked {{{op}}}(T a)
            {
            }
            """,
            testHost,
            Keyword("void"),
            Identifier("I1"),
            Operators.Dot,
            Keyword("operator"),
            Keyword("checked"),
            CompoundAssignmentOperatorClassification(op),
            Punctuation.OpenParen,
            Identifier("T"),
            Parameter("a"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task TestExclamationExclamation(TestHost testHost)
        => TestAsync(
            """
            class C
            {
                void M(string v!!)
                {
                }
            }
            """,
            testHost,
            Keyword("class"),
            Class("C"),
            Punctuation.OpenCurly,
            Keyword("void"),
            Method("M"),
            Punctuation.OpenParen,
            Keyword("string"),
            Parameter("v"),
            Operators.Exclamation,
            Operators.Exclamation,
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    /// <seealso cref="SemanticClassifierTests.LocalFunctionUse"/>
    /// <seealso cref="TotalClassifierTests.LocalFunctionDeclarationAndUse"/>
    [Theory, CombinatorialData]
    public Task LocalFunctionDeclaration(TestHost testHost)
        => TestAsync(
            """
            using System;

            class C
            {
                void M(Action action)
                {
                    [|localFunction();
                    staticLocalFunction();

                    M(localFunction);
                    M(staticLocalFunction);

                    void localFunction() { }
                    static void staticLocalFunction() { }|]
                }
            }

            """,
            testHost,
            Identifier("localFunction"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.Semicolon,
            Identifier("staticLocalFunction"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.Semicolon,
            Identifier("M"),
            Punctuation.OpenParen,
            Identifier("localFunction"),
            Punctuation.CloseParen,
            Punctuation.Semicolon,
            Identifier("M"),
            Punctuation.OpenParen,
            Identifier("staticLocalFunction"),
            Punctuation.CloseParen,
            Punctuation.Semicolon,
            Keyword("void"),
            Method("localFunction"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Keyword("static"),
            Keyword("void"),
            Method("staticLocalFunction"),
            Static("staticLocalFunction"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task ScopedParameter(TestHost testHost)
        => TestInMethodAsync(@"interface I { void F(scoped R r); }",
            testHost,
            Keyword("interface"),
            Interface("I"),
            Punctuation.OpenCurly,
            Keyword("void"),
            Method("F"),
            Punctuation.OpenParen,
            Keyword("scoped"),
            Identifier("R"),
            Parameter("r"),
            Punctuation.CloseParen,
            Punctuation.Semicolon,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task ScopedLocalDeclaration(TestHost testHost)
        => TestInMethodAsync(@"scoped var v;",
            testHost,
            Keyword("scoped"),
            Identifier("var"),
            Local("v"),
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    public Task ScopedOutDeclaration(TestHost testHost)
        => TestInMethodAsync(@"F(x, out scoped R y);",
            testHost,
            Identifier("F"),
            Punctuation.OpenParen,
            Identifier("x"),
            Punctuation.Comma,
            Keyword("out"),
            Keyword("scoped"),
            Identifier("R"),
            Local("y"),
            Punctuation.CloseParen,
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    public Task LambdaDefaultParameter_01(TestHost testHost)
        => TestAsync(
            """
            using System;
            class Program
            {
                void M()
                {
                    const int n = 100;
                    const int m = 200;
                    var lam = (int x = n + m) => x;
                }
            }

            """,
            testHost,
            Keyword("using"),
            Identifier("System"),
            Punctuation.Semicolon,
            Keyword("class"),
            Class("Program"),
            Punctuation.OpenCurly,
            Keyword("void"),
            Method("M"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Keyword("const"),
            Keyword("int"),
            Constant("n"),
            Static("n"),
            Operators.Equals,
            Number("100"),
            Punctuation.Semicolon,
            Keyword("const"),
            Keyword("int"),
            Constant("m"),
            Static("m"),
            Operators.Equals,
            Number("200"),
            Punctuation.Semicolon,
            Keyword("var"),
            Local("lam"),
            Operators.Equals,
            Punctuation.OpenParen,
            Keyword("int"),
            Parameter("x"),
            Operators.Equals,
            Identifier("n"),
            Operators.Plus,
            Identifier("m"),
            Punctuation.CloseParen,
            Operators.EqualsGreaterThan,
            Identifier("x"),
            Punctuation.Semicolon,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task LambdaDefaultParameter_02(TestHost testHost)
        => TestAsync(
            """
            class Program
            {
                void M()
                {
                    var lam = (string s = "a string") => s;
                }
            }

            """,
            testHost,
            Keyword("class"),
            Class("Program"),
            Punctuation.OpenCurly,
            Keyword("void"),
            Method("M"),
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Keyword("var"),
            Local("lam"),
            Operators.Equals,
            Punctuation.OpenParen,
            Keyword("string"),
            Parameter("s"),
            Operators.Equals,
            String("""
                "a string"
                """),
            Punctuation.CloseParen,
            Operators.EqualsGreaterThan,
            Identifier("s"),
            Punctuation.Semicolon,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task LambdaParamsArray(TestHost testHost)
        => TestInMethodAsync("var lam = (params int[] xs) => xs.Length;",
            testHost,
            Keyword("var"),
            Local("lam"),
            Operators.Equals,
            Punctuation.OpenParen,
            Keyword("params"),
            Keyword("int"),
            Punctuation.OpenBracket,
            Punctuation.CloseBracket,
            Parameter("xs"),
            Punctuation.CloseParen,
            Operators.EqualsGreaterThan,
            Identifier("xs"),
            Operators.Dot,
            Identifier("Length"),
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    public Task LambdaParamsArray_Multiple(TestHost testHost)
        => TestInMethodAsync("var lam = (int a, int b = 1, params int[] xs, params int[] ys.Length) => { };",
            testHost,
            Keyword("var"),
            Local("lam"),
            Operators.Equals,
            Punctuation.OpenParen,
            Keyword("int"),
            Local("a"),
            Punctuation.Comma,
            Keyword("int"),
            Identifier("b"),
            Operators.Equals,
            Number("1"),
            Punctuation.Comma,
            Keyword("params"),
            Keyword("int"),
            Punctuation.OpenBracket,
            Punctuation.CloseBracket,
            Local("xs"),
            Punctuation.Comma,
            Keyword("params"),
            Keyword("int"),
            Punctuation.OpenBracket,
            Punctuation.CloseBracket,
            Local("ys"),
            Operators.Dot,
            Identifier("Length"),
            Punctuation.CloseParen,
            Operators.EqualsGreaterThan,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.Semicolon);

    [Theory, CombinatorialData]
    public Task TestAllowsRefStructConstraint_01(TestHost testHost)
        => TestAsync(
            "class X<T> where T : allows ref struct { }",
            testHost,
            Keyword("class"),
            Class("X"),
            Punctuation.OpenAngle,
            TypeParameter("T"),
            Punctuation.CloseAngle,
            Keyword("where"),
            Identifier("T"),
            Punctuation.Colon,
            Keyword("allows"),
            Keyword("ref"),
            Keyword("struct"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task TestAllowsRefStructConstraint_02(TestHost testHost)
        => TestAsync(
            "class X { void M<T>() where T : allows ref struct { } }",
            testHost,
            Keyword("class"),
            Class("X"),
            Punctuation.OpenCurly,
            Keyword("void"),
            Method("M"),
            Punctuation.OpenAngle,
            TypeParameter("T"),
            Punctuation.CloseAngle,
            Punctuation.OpenParen,
            Punctuation.CloseParen,
            Keyword("where"),
            Identifier("T"),
            Punctuation.Colon,
            Keyword("allows"),
            Keyword("ref"),
            Keyword("struct"),
            Punctuation.OpenCurly,
            Punctuation.CloseCurly,
            Punctuation.CloseCurly);
}
