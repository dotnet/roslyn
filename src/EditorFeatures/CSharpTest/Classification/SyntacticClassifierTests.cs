// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.Editor.UnitTests.Classification.FormattedClassifications;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Classification
{
    public partial class SyntacticClassifierTests : AbstractCSharpClassifierTests
    {
        protected override Task<ImmutableArray<ClassifiedSpan>> GetClassificationSpansAsync(string code, TextSpan span, ParseOptions options)
        {
            using (var workspace = TestWorkspace.CreateCSharp(code, parseOptions: options))
            {
                var document = workspace.CurrentSolution.Projects.First().Documents.First();

                return GetSyntacticClassificationsAsync(document, span);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task VarAtTypeMemberLevel()
        {
            await TestAsync(
@"class C
{
    var goo }",
                Keyword("class"),
                Class("C"),
                Punctuation.OpenCurly,
                Identifier("var"),
                Field("goo"),
                Punctuation.CloseCurly);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task VarAsLocalVariableType()
        {
            await TestInMethodAsync("var goo = 42",
                Keyword("var"),
                Local("goo"),
                Operators.Equals,
                Number("42"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task VarOptimisticallyColored()
        {
            await TestInMethodAsync("var",
                Keyword("var"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task VarNotColoredInClass()
        {
            await TestInClassAsync("var",
                Identifier("var"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task VarInsideLocalAndExpressions()
        {
            await TestInMethodAsync(
@"var var = (var)var as var;",
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task VarAsMethodParameter()
        {
            await TestAsync(
@"class C
{
    void M(var v)
    {
    }
}",
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task YieldYield()
        {
            await TestAsync(
@"using System.Collections.Generic;

class yield
{
    IEnumerable<yield> M()
    {
        yield yield = new yield();
        yield return yield;
    }
}",
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task YieldReturn()
        {
            await TestInMethodAsync("yield return 42",
                ControlKeyword("yield"),
                ControlKeyword("return"),
                Number("42"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task YieldFixed()
        {
            await TestInMethodAsync(
@"yield return this.items[0]; yield break; fixed (int* i = 0) {
}",
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PartialClass()
        {
            await TestAsync("public partial class Goo",
                Keyword("public"),
                Keyword("partial"),
                Keyword("class"),
                Class("Goo"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PartialMethod()
        {
            await TestInClassAsync(
@"public partial void M()
{
}",
                Keyword("public"),
                Keyword("partial"),
                Keyword("void"),
                Method("M"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Punctuation.OpenCurly,
                Punctuation.CloseCurly);
        }

        /// <summary>
        /// Partial is only valid in a type declaration
        /// </summary>
        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        [WorkItem(536313, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/536313")]
        public async Task PartialAsLocalVariableType()
        {
            await TestInMethodAsync(
@"partial p1 = 42;",
                Identifier("partial"),
                Local("p1"),
                Operators.Equals,
                Number("42"),
                Punctuation.Semicolon);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PartialClassStructInterface()
        {
            await TestAsync(
@"partial class T1
{
}

partial struct T2
{
}

partial interface T3
{
}",
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
        }

        private static readonly string[] s_contextualKeywordsOnlyValidInMethods = new string[] { "where", "from", "group", "join", "select", "into", "let", "by", "orderby", "on", "equals", "ascending", "descending" };

        /// <summary>
        /// Check for items only valid within a method declaration
        /// </summary>
        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task ContextualKeywordsOnlyValidInMethods()
        {
            foreach (var kw in s_contextualKeywordsOnlyValidInMethods)
            {
                await TestInNamespaceAsync(kw + " goo",
                    Identifier(kw),
                    Field("goo"));
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task VerbatimStringLiterals1()
        {
            await TestInMethodAsync(@"@""goo""",
                Verbatim(@"@""goo"""));
        }

        /// <summary>
        /// Should show up as soon as we get the @\" typed out
        /// </summary>
        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task VerbatimStringLiterals2()
        {
            await TestAsync(@"@""",
                Verbatim(@"@"""));
        }

        /// <summary>
        /// Parser does not currently support strings of this type
        /// </summary>
        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task VerbatimStringLiteral3()
        {
            await TestAsync(@"goo @""",
                Identifier("goo"),
                Verbatim(@"@"""));
        }

        /// <summary>
        /// Uncompleted ones should span new lines
        /// </summary>
        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task VerbatimStringLiteral4()
        {
            var code = @"

@"" goo bar 

";
            await TestAsync(code,
                Verbatim(@"@"" goo bar 

"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task VerbatimStringLiteral5()
        {
            var code = @"

@"" goo bar
and 
on a new line "" 
more stuff";
            await TestInMethodAsync(code,
                Verbatim(@"@"" goo bar
and 
on a new line """),
                Identifier("more"),
                Local("stuff"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task VerbatimStringLiteral6()
        {
            await TestAsync(
@"string s = @""""""/*"";",
                Keyword("string"),
                Field("s"),
                Operators.Equals,
                Verbatim(@"@""""""/*"""),
                Punctuation.Semicolon);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task StringLiteral1()
        {
            await TestAsync(@"""goo""",
                String(@"""goo"""));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task StringLiteral2()
        {
            await TestAsync(@"""""",
                String(@""""""));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task CharacterLiteral1()
        {
            var code = @"'f'";
            await TestInMethodAsync(code,
                String("'f'"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task LinqFrom1()
        {
            var code = @"from it in goo";
            await TestInExpressionAsync(code,
                Keyword("from"),
                Identifier("it"),
                Keyword("in"),
                Identifier("goo"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task LinqFrom2()
        {
            var code = @"from it in goo.Bar()";
            await TestInExpressionAsync(code,
                Keyword("from"),
                Identifier("it"),
                Keyword("in"),
                Identifier("goo"),
                Operators.Dot,
                Identifier("Bar"),
                Punctuation.OpenParen,
                Punctuation.CloseParen);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task LinqFrom3()
        {
            // query expression are not statement expressions, but the parser parses them anyways to give better errors
            var code = @"from it in ";
            await TestInMethodAsync(code,
                Keyword("from"),
                Identifier("it"),
                Keyword("in"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task LinqFrom4()
        {
            var code = @"from it in ";
            await TestInExpressionAsync(code,
                Keyword("from"),
                Identifier("it"),
                Keyword("in"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task LinqWhere1()
        {
            var code = "from it in goo where it > 42";
            await TestInExpressionAsync(code,
                Keyword("from"),
                Identifier("it"),
                Keyword("in"),
                Identifier("goo"),
                Keyword("where"),
                Identifier("it"),
                Operators.GreaterThan,
                Number("42"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task LinqWhere2()
        {
            var code = @"from it in goo where it > ""bar""";
            await TestInExpressionAsync(code,
                Keyword("from"),
                Identifier("it"),
                Keyword("in"),
                Identifier("goo"),
                Keyword("where"),
                Identifier("it"),
                Operators.GreaterThan,
                String(@"""bar"""));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task VarContextualKeywordAtNamespaceLevel()
        {
            var code = @"var goo = 2;";
            await TestAsync(code,
                code,
                Identifier("var"),
                Field("goo"),
                Operators.Equals,
                Number("2"),
                Punctuation.Semicolon);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task LinqKeywordsAtNamespaceLevel()
        {
            // the contextual keywords are actual keywords since we parse top level field declaration and only give a semantic error
            await TestAsync(
@"object goo = from goo in goo
             join goo in goo on goo equals goo
             group goo by goo into goo
             let goo = goo
             where goo
             orderby goo ascending, goo descending
             select goo;",
                Keyword("object"),
                Field("goo"),
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

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task ContextualKeywordsAsFieldName()
        {
            await TestAsync(
@"class C
{
    int yield, get, set, value, add, remove, global, partial, where, alias;
}",
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task LinqKeywordsInFieldInitializer()
        {
            await TestAsync(
@"class C
{
    int a = from a in a
            join a in a on a equals a
            group a by a into a
            let a = a
            where a
            orderby a ascending, a descending
            select a;
}",
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task LinqKeywordsAsTypeName()
        {
            await TestAsync(
@"class var
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
}",
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task LinqKeywordsAsMethodParameters()
        {
            await TestAsync(
@"class C
{
    orderby M(var goo, from goo, join goo, on goo, equals goo, group goo, by goo, into goo, let goo, where goo, orderby goo, ascending goo, descending goo, select goo)
    {
    }
}",
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task LinqKeywordsInLocalVariableDeclarations()
        {
            await TestAsync(
@"class C
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
}",
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task LinqKeywordsAsFieldNames()
        {
            await TestAsync(
@"class C
{
    int var, from, join, on, into, equals, let, orderby, ascending, descending, select, group, by, partial;
}",
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task LinqKeywordsAtFieldLevelInvalid()
        {
            await TestAsync(
@"class C
{
    string Property { from a in a join a in a on a equals a group a by a into a let a = a where a orderby a ascending, 
a descending select a; }
}",
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task CommentSingle()
        {
            var code = "// goo";

            await TestAsync(code,
                Comment("// goo"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task CommentAsTrailingTrivia1()
        {
            var code = "class Bar { // goo";
            await TestAsync(code,
                Keyword("class"),
                Class("Bar"),
                Punctuation.OpenCurly,
                Comment("// goo"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task CommentAsLeadingTrivia1()
        {
            var code = @"
class Bar { 
  // goo
  void Method1() { }
}";
            await TestAsync(code,
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task ShebangAsFirstCommentInScript()
        {
            var code = @"#!/usr/bin/env scriptcs
System.Console.WriteLine();";

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

            await TestAsync(code, code, parseOptions: Options.Script, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task ShebangAsFirstCommentInNonScript()
        {
            var code = @"#!/usr/bin/env scriptcs
System.Console.WriteLine();";

            var expected = new[]
            {
                PPKeyword("#"),
                PPText("!/usr/bin/env scriptcs"),
                Identifier("System"),
                Operators.Dot,
                Identifier("Console"),
                Operators.Dot,
                Identifier("WriteLine"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Punctuation.Semicolon
            };

            await TestAsync(code, code, parseOptions: Options.Regular, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task ShebangNotAsFirstCommentInScript()
        {
            var code = @" #!/usr/bin/env scriptcs
System.Console.WriteLine();";

            var expected = new[]
            {
                PPKeyword("#"),
                PPText("!/usr/bin/env scriptcs"),
                Identifier("System"),
                Operators.Dot,
                Identifier("Console"),
                Operators.Dot,
                Identifier("WriteLine"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Punctuation.Semicolon
            };

            await TestAsync(code, code, parseOptions: Options.Script, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task CommentAsMethodBodyContent()
        {
            var code = @"
class Bar { 
  void Method1() {
// goo
}
}";
            await TestAsync(code,
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task CommentMix1()
        {
            await TestAsync(
@"// comment1 /*
class cl
{
}
//comment2 */",
                Comment("// comment1 /*"),
                Keyword("class"),
                Class("cl"),
                Punctuation.OpenCurly,
                Punctuation.CloseCurly,
                Comment("//comment2 */"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task CommentMix2()
        {
            await TestInMethodAsync(
@"/**/int /**/i = 0;",
                Comment("/**/"),
                Keyword("int"),
                Comment("/**/"),
                Local("i"),
                Operators.Equals,
                Number("0"),
                Punctuation.Semicolon);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task XmlDocCommentOnClass()
        {
            var code = @"
/// <summary>something</summary>
class Bar { }";
            await TestAsync(code,
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task XmlDocCommentOnClassWithIndent()
        {
            var code = @"
    /// <summary>
    /// something
    /// </summary>
    class Bar { }";
            await TestAsync(code,
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task XmlDocComment_EntityReference()
        {
            var code = @"
/// <summary>&#65;</summary>
class Bar { }";
            await TestAsync(code,
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task XmlDocComment_ExteriorTriviaInsideCloseTag()
        {
            var code = @"
/// <summary>something</
/// summary>
class Bar { }";
            await TestAsync(code,
                XmlDoc.Delimiter("///"),
                XmlDoc.Text(" "),
                XmlDoc.Delimiter("<"),
                XmlDoc.Name("summary"),
                XmlDoc.Delimiter(">"),
                XmlDoc.Text("something"),
                XmlDoc.Delimiter("</"),
                XmlDoc.Delimiter("///"),
                XmlDoc.Name(" "),
                XmlDoc.Name("summary"),
                XmlDoc.Delimiter(">"),
                Keyword("class"),
                Class("Bar"),
                Punctuation.OpenCurly,
                Punctuation.CloseCurly);
        }

        [WorkItem(531155, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531155")]
        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task XmlDocComment_ExteriorTriviaInsideCRef()
        {
            var code = @"
/// <see cref=""System.
/// Int32""/>
class C
{
}";
            await TestAsync(code,
                XmlDoc.Delimiter("///"),
                XmlDoc.Text(" "),
                XmlDoc.Delimiter("<"),
                XmlDoc.Name("see"),
                XmlDoc.AttributeName(" "),
                XmlDoc.AttributeName("cref"),
                XmlDoc.Delimiter("="),
                XmlDoc.AttributeQuotes("\""),
                Identifier("System"),
                Operators.Dot,
                XmlDoc.Delimiter("///"),
                Identifier("Int32"),
                XmlDoc.AttributeQuotes("\""),
                XmlDoc.Delimiter("/>"),
                Keyword("class"),
                Class("C"),
                Punctuation.OpenCurly,
                Punctuation.CloseCurly);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task XmlDocCommentOnClassWithExteriorTrivia()
        {
            var code = @"
/// <summary>
/// something
/// </summary>
class Bar { }";
            await TestAsync(code,
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task XmlDocComment_ExteriorTriviaNoText()
        {
            var code =
@"///<summary>
///something
///</summary>
class Bar { }";
            await TestAsync(code,
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task XmlDocComment_EmptyElement()
        {
            var code = @"
/// <summary />
class Bar { }";
            await TestAsync(code,
                XmlDoc.Delimiter("///"),
                XmlDoc.Text(" "),
                XmlDoc.Delimiter("<"),
                XmlDoc.Name("summary"),
                XmlDoc.Delimiter(" "),
                XmlDoc.Delimiter("/>"),
                Keyword("class"),
                Class("Bar"),
                Punctuation.OpenCurly,
                Punctuation.CloseCurly);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task XmlDocComment_Attribute()
        {
            var code = @"
/// <summary attribute=""value"">something</summary>
class Bar { }";

            await TestAsync(code,
                XmlDoc.Delimiter("///"),
                XmlDoc.Text(" "),
                XmlDoc.Delimiter("<"),
                XmlDoc.Name("summary"),
                XmlDoc.AttributeName(" "),
                XmlDoc.AttributeName("attribute"),
                XmlDoc.Delimiter("="),
                XmlDoc.AttributeQuotes(@""""),
                XmlDoc.AttributeValue(@"value"),
                XmlDoc.AttributeQuotes(@""""),
                XmlDoc.Delimiter(">"),
                XmlDoc.Text("something"),
                XmlDoc.Delimiter("</"),
                XmlDoc.Name("summary"),
                XmlDoc.Delimiter(">"),
                Keyword("class"),
                Class("Bar"),
                Punctuation.OpenCurly,
                Punctuation.CloseCurly);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task XmlDocComment_AttributeInEmptyElement()
        {
            var code = @"
/// <summary attribute=""value"" />
class Bar { }";
            await TestAsync(code,
                XmlDoc.Delimiter("///"),
                XmlDoc.Text(" "),
                XmlDoc.Delimiter("<"),
                XmlDoc.Name("summary"),
                XmlDoc.AttributeName(" "),
                XmlDoc.AttributeName("attribute"),
                XmlDoc.Delimiter("="),
                XmlDoc.AttributeQuotes(@""""),
                XmlDoc.AttributeValue(@"value"),
                XmlDoc.AttributeQuotes(@""""),
                XmlDoc.Delimiter(" "),
                XmlDoc.Delimiter("/>"),
                Keyword("class"),
                Class("Bar"),
                Punctuation.OpenCurly,
                Punctuation.CloseCurly);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task XmlDocComment_ExtraSpaces()
        {
            var code = @"
///   <   summary   attribute    =   ""value""     />
class Bar { }";
            await TestAsync(code,
                XmlDoc.Delimiter("///"),
                XmlDoc.Text("   "),
                XmlDoc.Delimiter("<"),
                XmlDoc.Name("   "),
                XmlDoc.Name("summary"),
                XmlDoc.AttributeName("   "),
                XmlDoc.AttributeName("attribute"),
                XmlDoc.Delimiter("    "),
                XmlDoc.Delimiter("="),
                XmlDoc.AttributeQuotes("   "),
                XmlDoc.AttributeQuotes(@""""),
                XmlDoc.AttributeValue(@"value"),
                XmlDoc.AttributeQuotes(@""""),
                XmlDoc.Delimiter("     "),
                XmlDoc.Delimiter("/>"),
                Keyword("class"),
                Class("Bar"),
                Punctuation.OpenCurly,
                Punctuation.CloseCurly);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task XmlDocComment_XmlComment()
        {
            var code = @"
///<!--comment-->
class Bar { }";
            await TestAsync(code,
                XmlDoc.Delimiter("///"),
                XmlDoc.Delimiter("<!--"),
                XmlDoc.Comment("comment"),
                XmlDoc.Delimiter("-->"),
                Keyword("class"),
                Class("Bar"),
                Punctuation.OpenCurly,
                Punctuation.CloseCurly);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task XmlDocComment_XmlCommentWithExteriorTrivia()
        {
            var code = @"
///<!--first
///second-->
class Bar { }";
            await TestAsync(code,
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task XmlDocComment_XmlCommentInElement()
        {
            var code = @"
///<summary><!--comment--></summary>
class Bar { }";
            await TestAsync(code,
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        [WorkItem(31410, "https://github.com/dotnet/roslyn/pull/31410")]
        public async Task XmlDocComment_MalformedXmlDocComment()
        {
            var code = @"
///<summary>
///<a: b, c />.
///</summary>
class C { }";
            await TestAsync(code,
                XmlDoc.Delimiter("///"),
                XmlDoc.Delimiter("<"),
                XmlDoc.Name("summary"),
                XmlDoc.Delimiter(">"),
                XmlDoc.Delimiter("///"),
                XmlDoc.Delimiter("<"),
                XmlDoc.Name("a"),
                XmlDoc.Name(":"),
                XmlDoc.Name(" "),
                XmlDoc.Name("b"),
                XmlDoc.Text(","),
                XmlDoc.Name(" "),
                XmlDoc.Text("c"),
                XmlDoc.Delimiter(" "),
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task MultilineXmlDocComment_ExteriorTrivia()
        {
            var code =
@"/**<summary>
*comment
*</summary>*/
class Bar { }";
            await TestAsync(code,
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task XmlDocComment_CDataWithExteriorTrivia()
        {
            var code = @"
///<![CDATA[first
///second]]>
class Bar { }";
            await TestAsync(code,
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task XmlDocComment_ProcessingDirective()
        {
            await TestAsync(
@"/// <summary><?goo
/// ?></summary>
public class Program
{
    static void Main()
    {
    }
}",
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        [WorkItem(536321, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/536321")]
        public async Task KeywordTypeParameters()
        {
            var code = @"class C<int> { }";
            await TestAsync(code,
                Keyword("class"),
                Class("C"),
                Punctuation.OpenAngle,
                Keyword("int"),
                Punctuation.CloseAngle,
                Punctuation.OpenCurly,
                Punctuation.CloseCurly);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        [WorkItem(536853, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/536853")]
        public async Task TypeParametersWithAttribute()
        {
            var code = @"class C<[Attr] T> { }";
            await TestAsync(code,
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task ClassTypeDeclaration1()
        {
            var code = "class C1 { } ";
            await TestAsync(code,
                Keyword("class"),
                Class("C1"),
                Punctuation.OpenCurly,
                Punctuation.CloseCurly);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task ClassTypeDeclaration2()
        {
            var code = "class ClassName1 { } ";
            await TestAsync(code,
                Keyword("class"),
                Class("ClassName1"),
                Punctuation.OpenCurly,
                Punctuation.CloseCurly);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task StructTypeDeclaration1()
        {
            var code = "struct Struct1 { }";
            await TestAsync(code,
                Keyword("struct"),
                Struct("Struct1"),
                Punctuation.OpenCurly,
                Punctuation.CloseCurly);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task InterfaceDeclaration1()
        {
            var code = "interface I1 { }";
            await TestAsync(code,
                Keyword("interface"),
                Interface("I1"),
                Punctuation.OpenCurly,
                Punctuation.CloseCurly);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task EnumDeclaration1()
        {
            var code = "enum Weekday { }";
            await TestAsync(code,
                Keyword("enum"),
                Enum("Weekday"),
                Punctuation.OpenCurly,
                Punctuation.CloseCurly);
        }

        [WorkItem(4302, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task ClassInEnum()
        {
            var code = "enum E { Min = System.Int32.MinValue }";
            await TestAsync(code,
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task DelegateDeclaration1()
        {
            var code = "delegate void Action();";
            await TestAsync(code,
                Keyword("delegate"),
                Keyword("void"),
                Delegate("Action"),
                Punctuation.OpenParen,
                Punctuation.CloseParen,
                Punctuation.Semicolon);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task GenericTypeArgument()
        {
            await TestInMethodAsync("C<T>", "M", "default(T)",
                Keyword("default"),
                Punctuation.OpenParen,
                Identifier("T"),
                Punctuation.CloseParen);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task GenericParameter()
        {
            var code = "class C1<P1> {}";
            await TestAsync(code,
                Keyword("class"),
                Class("C1"),
                Punctuation.OpenAngle,
                TypeParameter("P1"),
                Punctuation.CloseAngle,
                Punctuation.OpenCurly,
                Punctuation.CloseCurly);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task GenericParameters()
        {
            var code = "class C1<P1,P2> {}";
            await TestAsync(code,
                Keyword("class"),
                Class("C1"),
                Punctuation.OpenAngle,
                TypeParameter("P1"),
                Punctuation.Comma,
                TypeParameter("P2"),
                Punctuation.CloseAngle,
                Punctuation.OpenCurly,
                Punctuation.CloseCurly);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task GenericParameter_Interface()
        {
            var code = "interface I1<P1> {}";
            await TestAsync(code,
                Keyword("interface"),
                Interface("I1"),
                Punctuation.OpenAngle,
                TypeParameter("P1"),
                Punctuation.CloseAngle,
                Punctuation.OpenCurly,
                Punctuation.CloseCurly);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task GenericParameter_Struct()
        {
            var code = "struct S1<P1> {}";
            await TestAsync(code,
                Keyword("struct"),
                Struct("S1"),
                Punctuation.OpenAngle,
                TypeParameter("P1"),
                Punctuation.CloseAngle,
                Punctuation.OpenCurly,
                Punctuation.CloseCurly);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task GenericParameter_Delegate()
        {
            var code = "delegate void D1<P1> {}";
            await TestAsync(code,
                Keyword("delegate"),
                Keyword("void"),
                Delegate("D1"),
                Punctuation.OpenAngle,
                TypeParameter("P1"),
                Punctuation.CloseAngle,
                Punctuation.OpenCurly,
                Punctuation.CloseCurly);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task GenericParameter_Method()
        {
            await TestInClassAsync(
@"T M<T>(T t)
{
    return default(T);
}",
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TernaryExpression()
        {
            await TestInExpressionAsync("true ? 1 : 0",
                Keyword("true"),
                Operators.QuestionMark,
                Number("1"),
                Operators.Colon,
                Number("0"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task BaseClass()
        {
            await TestAsync(
@"class C : B
{
}",
                Keyword("class"),
                Class("C"),
                Punctuation.Colon,
                Identifier("B"),
                Punctuation.OpenCurly,
                Punctuation.CloseCurly);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestLabel()
        {
            await TestInMethodAsync("goo:",
                Label("goo"),
                Punctuation.Colon);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task Attribute()
        {
            await TestAsync(
@"[assembly: Goo]",
                Punctuation.OpenBracket,
                Keyword("assembly"),
                Punctuation.Colon,
                Identifier("Goo"),
                Punctuation.CloseBracket);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestAngleBracketsOnGenericConstraints_Bug932262()
        {
            await TestAsync(
@"class C<T> where T : A<T>
{
}",
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestYieldPositive()
        {
            await TestInMethodAsync(
@"yield return goo;",

                ControlKeyword("yield"),
                ControlKeyword("return"),
                Identifier("goo"),
                Punctuation.Semicolon);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestYieldNegative()
        {
            await TestInMethodAsync(
@"int yield;",

                Keyword("int"),
                Local("yield"),
                Punctuation.Semicolon);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestFromPositive()
        {
            await TestInExpressionAsync(
@"from x in y",

                Keyword("from"),
                Identifier("x"),
                Keyword("in"),
                Identifier("y"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestFromNegative()
        {
            await TestInMethodAsync(
@"int from;",

                Keyword("int"),
                Local("from"),
                Punctuation.Semicolon);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task AttributeTargetSpecifiersModule()
        {
            await TestAsync(
@"[module: Obsolete]",
                Punctuation.OpenBracket,
                Keyword("module"),
                Punctuation.Colon,
                Identifier("Obsolete"),
                Punctuation.CloseBracket);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task AttributeTargetSpecifiersAssembly()
        {
            await TestAsync(
@"[assembly: Obsolete]",
                Punctuation.OpenBracket,
                Keyword("assembly"),
                Punctuation.Colon,
                Identifier("Obsolete"),
                Punctuation.CloseBracket);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task AttributeTargetSpecifiersOnDelegate()
        {
            await TestInClassAsync(
@"[type: A]
[return: A]
delegate void M();",
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task AttributeTargetSpecifiersOnMethod()
        {
            await TestInClassAsync(
@"[return: A]
[method: A]
void M()
{
}",
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task AttributeTargetSpecifiersOnCtor()
        {
            await TestAsync(
@"class C
{
    [method: A]
    C()
    {
    }
}",
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task AttributeTargetSpecifiersOnDtor()
        {
            await TestAsync(
@"class C
{
    [method: A]
    ~C()
    {
    }
}",
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task AttributeTargetSpecifiersOnOperator()
        {
            await TestInClassAsync(
@"[method: A]
[return: A]
static T operator +(T a, T b)
{
}",
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task AttributeTargetSpecifiersOnEventDeclaration()
        {
            await TestInClassAsync(
@"[event: A]
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
}",
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task AttributeTargetSpecifiersOnPropertyAccessors()
        {
            await TestInClassAsync(
@"int P
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
}",
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task AttributeTargetSpecifiersOnIndexers()
        {
            await TestInClassAsync(
@"[property: A]
int this[int i] { get; set; }",
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task AttributeTargetSpecifiersOnIndexerAccessors()
        {
            await TestInClassAsync(
@"int this[int i]
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
}",
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task AttributeTargetSpecifiersOnField()
        {
            await TestInClassAsync(
@"[field: A]
const int a = 0;",
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestAllKeywords()
        {
            await TestAsync(
@"using System;
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
            Console.WriteLine(""Finished"");
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
#endregion TaoRegion",
                new[] { new CSharpParseOptions(LanguageVersion.CSharp8) },
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
                String(@"""Finished"""),
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestAllOperators()
        {
            await TestAsync(
@"using IO = System.IO;

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
        object s = x => x + 1;
        Point point;
        unsafe
        {
            Point* p = &point;
            p->x = 10;
        }

        IO::BinaryReader br = null;
    }
}",
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestPartialMethodWithNamePartial()
        {
            await TestAsync(
@"partial class C
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
}",
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task ValueInSetterAndAnonymousTypePropertyName()
        {
            await TestAsync(
@"class C
{
    int P
    {
        set
        {
            var t = new { value = value };
        }
    }
}",
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
        }

        [WorkItem(538680, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538680")]
        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestValueInLabel()
        {
            await TestAsync(
@"class C
{
    int X
    {
        set
        {
        value:
            ;
        }
    }
}",
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
        }

        [WorkItem(541150, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541150")]
        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestGenericVar()
        {
            await TestAsync(
@"using System;

static class Program
{
    static void Main()
    {
        var x = 1;
    }
}

class var<T>
{
}",
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
        }

        [WorkItem(541154, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541154")]
        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestInaccessibleVar()
        {
            await TestAsync(
@"using System;

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
}",
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
        }

        [WorkItem(541613, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541613")]
        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestEscapedVar()
        {
            await TestAsync(
@"class Program
{
    static void Main(string[] args)
    {
        @var v = 1;
    }
}",
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
        }

        [WorkItem(542432, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542432")]
        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestVar()
        {
            await TestAsync(
@"class Program
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
}",
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
        }

        [WorkItem(543123, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543123")]
        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestVar2()
        {
            await TestAsync(
@"class Program
{
    void Main(string[] args)
    {
        foreach (var v in args)
        {
        }
    }
}",
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task InterpolatedStrings1()
        {
            var code = @"
var x = ""World"";
var y = $""Hello, {x}"";
";
            await TestInMethodAsync(code,
                Keyword("var"),
                Local("x"),
                Operators.Equals,
                String("\"World\""),
                Punctuation.Semicolon,
                Keyword("var"),
                Local("y"),
                Operators.Equals,
                String("$\""),
                String("Hello, "),
                Punctuation.OpenCurly,
                Identifier("x"),
                Punctuation.CloseCurly,
                String("\""),
                Punctuation.Semicolon);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task InterpolatedStrings2()
        {
            var code = @"
var a = ""Hello"";
var b = ""World"";
var c = $""{a}, {b}"";
";
            await TestInMethodAsync(code,
                Keyword("var"),
                Local("a"),
                Operators.Equals,
                String("\"Hello\""),
                Punctuation.Semicolon,
                Keyword("var"),
                Local("b"),
                Operators.Equals,
                String("\"World\""),
                Punctuation.Semicolon,
                Keyword("var"),
                Local("c"),
                Operators.Equals,
                String("$\""),
                Punctuation.OpenCurly,
                Identifier("a"),
                Punctuation.CloseCurly,
                String(", "),
                Punctuation.OpenCurly,
                Identifier("b"),
                Punctuation.CloseCurly,
                String("\""),
                Punctuation.Semicolon);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task InterpolatedStrings3()
        {
            var code = @"
var a = ""Hello"";
var b = ""World"";
var c = $@""{a}, {b}"";
";
            await TestInMethodAsync(code,
                Keyword("var"),
                Local("a"),
                Operators.Equals,
                String("\"Hello\""),
                Punctuation.Semicolon,
                Keyword("var"),
                Local("b"),
                Operators.Equals,
                String("\"World\""),
                Punctuation.Semicolon,
                Keyword("var"),
                Local("c"),
                Operators.Equals,
                Verbatim("$@\""),
                Punctuation.OpenCurly,
                Identifier("a"),
                Punctuation.CloseCurly,
                Verbatim(", "),
                Punctuation.OpenCurly,
                Identifier("b"),
                Punctuation.CloseCurly,
                Verbatim("\""),
                Punctuation.Semicolon);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task ExceptionFilter1()
        {
            var code = @"
try
{
}
catch when (true)
{
}
";
            await TestInMethodAsync(code,
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task ExceptionFilter2()
        {
            var code = @"
try
{
}
catch (System.Exception) when (true)
{
}
";
            await TestInMethodAsync(code,
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task OutVar()
        {
            var code = @"
F(out var);";
            await TestInMethodAsync(code,
                Identifier("F"),
                Punctuation.OpenParen,
                Keyword("out"),
                Identifier("var"),
                Punctuation.CloseParen,
                Punctuation.Semicolon);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task ReferenceDirective()
        {
            var code = @"
#r ""file.dll""";
            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("r"),
                String("\"file.dll\""));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task LoadDirective()
        {
            var code = @"
#load ""file.csx""";
            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("load"),
                String("\"file.csx\""));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task IncompleteAwaitInNonAsyncContext()
        {
            var code = @"
void M()
{
    var x = await
}";
            await TestInClassAsync(code,
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task CompleteAwaitInNonAsyncContext()
        {
            var code = @"
void M()
{
    var x = await;
}";
            await TestInClassAsync(code,
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TupleDeclaration()
        {
            await TestInMethodAsync("(int, string) x",
                ParseOptions(TestOptions.Regular, Options.Script),
                Punctuation.OpenParen,
                Keyword("int"),
                Punctuation.Comma,
                Keyword("string"),
                Punctuation.CloseParen,
                Local("x"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TupleDeclarationWithNames()
        {
            await TestInMethodAsync("(int a, string b) x",
                ParseOptions(TestOptions.Regular, Options.Script),
                Punctuation.OpenParen,
                Keyword("int"),
                Identifier("a"),
                Punctuation.Comma,
                Keyword("string"),
                Identifier("b"),
                Punctuation.CloseParen,
                Local("x"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TupleLiteral()
        {
            await TestInMethodAsync("var values = (1, 2)",
                ParseOptions(TestOptions.Regular, Options.Script),
                Keyword("var"),
                Local("values"),
                Operators.Equals,
                Punctuation.OpenParen,
                Number("1"),
                Punctuation.Comma,
                Number("2"),
                Punctuation.CloseParen);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TupleLiteralWithNames()
        {
            await TestInMethodAsync("var values = (a: 1, b: 2)",
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestConflictMarkers1()
        {
            await TestAsync(
@"class C
{
<<<<<<< Start
    public void Goo();
=======
    public void Bar();
>>>>>>> End
}",
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestUnmanagedConstraint_InsideMethod()
        {
            await TestInMethodAsync(@"
var unmanaged = 0;
unmanaged++;",
                Keyword("var"),
                Local("unmanaged"),
                Operators.Equals,
                Number("0"),
                Punctuation.Semicolon,
                Identifier("unmanaged"),
                Operators.PlusPlus,
                Punctuation.Semicolon);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestUnmanagedConstraint_Type_Keyword()
        {
            await TestAsync(
                "class X<T> where T : unmanaged { }",
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestUnmanagedConstraint_Type_ExistingInterface()
        {
            await TestAsync(@"
interface unmanaged {}
class X<T> where T : unmanaged { }",
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestUnmanagedConstraint_Type_ExistingInterfaceButOutOfScope()
        {
            await TestAsync(@"
namespace OtherScope
{
    interface unmanaged {}
}
class X<T> where T : unmanaged { }",
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestUnmanagedConstraint_Method_Keyword()
        {
            await TestAsync(@"
class X
{
    void M<T>() where T : unmanaged { }
}",
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestUnmanagedConstraint_Method_ExistingInterface()
        {
            await TestAsync(@"
interface unmanaged {}
class X
{
    void M<T>() where T : unmanaged { }
}",
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestUnmanagedConstraint_Method_ExistingInterfaceButOutOfScope()
        {
            await TestAsync(@"
namespace OtherScope
{
    interface unmanaged {}
}
class X
{
    void M<T>() where T : unmanaged { }
}",
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestUnmanagedConstraint_Delegate_Keyword()
        {
            await TestAsync(
                "delegate void D<T>() where T : unmanaged;",
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestUnmanagedConstraint_Delegate_ExistingInterface()
        {
            await TestAsync(@"
interface unmanaged {}
delegate void D<T>() where T : unmanaged;",
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestUnmanagedConstraint_Delegate_ExistingInterfaceButOutOfScope()
        {
            await TestAsync(@"
namespace OtherScope
{
    interface unmanaged {}
}
delegate void D<T>() where T : unmanaged;",
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestUnmanagedConstraint_LocalFunction_Keyword()
        {
            await TestAsync(@"
class X
{
    void N()
    {
        void M<T>() where T : unmanaged { }
    }
}",
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestUnmanagedConstraint_LocalFunction_ExistingInterface()
        {
            await TestAsync(@"
interface unmanaged {}
class X
{
    void N()
    {
        void M<T>() where T : unmanaged { }
    }
}",
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestUnmanagedConstraint_LocalFunction_ExistingInterfaceButOutOfScope()
        {
            await TestAsync(@"
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
}",
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestDeclarationIsPattern()
        {
            await TestInMethodAsync(@"
object foo;

if (foo is Action action)
{
}",
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestDeclarationSwitchPattern()
        {
            await TestInMethodAsync(@"
object y;

switch (y)
{
    case int x:
        break;
}",

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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestDeclarationExpression()
        {
            await TestInMethodAsync(@"
int (foo, bar) = (1, 2);",
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestTupleTypeSyntax()
        {
            await TestInClassAsync(@"
public (int a, int b) Get() => null;",
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestOutParameter()
        {
            await TestInMethodAsync(@"
if (int.TryParse(""1"", out int x))
{
}",
                ControlKeyword("if"),
                Punctuation.OpenParen,
                Keyword("int"),
                Operators.Dot,
                Identifier("TryParse"),
                Punctuation.OpenParen,
                String(@"""1"""),
                Punctuation.Comma,
                Keyword("out"),
                Keyword("int"),
                Local("x"),
                Punctuation.CloseParen,
                Punctuation.CloseParen,
                Punctuation.OpenCurly,
                Punctuation.CloseCurly);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestOutParameter2()
        {
            await TestInClassAsync(@"
int F = int.TryParse(""1"", out int x) ? x : -1;
",
                Keyword("int"),
                Field("F"),
                Operators.Equals,
                Keyword("int"),
                Operators.Dot,
                Identifier("TryParse"),
                Punctuation.OpenParen,
                String(@"""1"""),
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestUsingDirective()
        {
            var code = @"using System.Collections.Generic;";

            await TestAsync(code,
                Keyword("using"),
                Identifier("System"),
                Operators.Dot,
                Identifier("Collections"),
                Operators.Dot,
                Identifier("Generic"),
                Punctuation.Semicolon);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestUsingAliasDirectiveForIdentifier()
        {
            var code = @"using Col = System.Collections;";

            await TestAsync(code,
                Keyword("using"),
                Identifier("Col"),
                Operators.Equals,
                Identifier("System"),
                Operators.Dot,
                Identifier("Collections"),
                Punctuation.Semicolon);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestUsingAliasDirectiveForClass()
        {
            var code = @"using Con = System.Console;";

            await TestAsync(code,
                Keyword("using"),
                Identifier("Con"),
                Operators.Equals,
                Identifier("System"),
                Operators.Dot,
                Identifier("Console"),
                Punctuation.Semicolon);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestUsingStaticDirective()
        {
            var code = @"using static System.Console;";

            await TestAsync(code,
                Keyword("using"),
                Keyword("static"),
                Identifier("System"),
                Operators.Dot,
                Identifier("Console"),
                Punctuation.Semicolon);
        }

        [WorkItem(33039, "https://github.com/dotnet/roslyn/issues/33039")]
        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task ForEachVariableStatement()
        {
            await TestInMethodAsync(@"
foreach (var (x, y) in new[] { (1, 2) });
",
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task CatchDeclarationStatement()
        {
            await TestInMethodAsync(@"
try { } catch (Exception ex) { }
",
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestNotNullConstraint_InsideMethod()
        {
            await TestInMethodAsync(@"
var notnull = 0;
notnull++;",
                Keyword("var"),
                Local("notnull"),
                Operators.Equals,
                Number("0"),
                Punctuation.Semicolon,
                Identifier("notnull"),
                Operators.PlusPlus,
                Punctuation.Semicolon);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestNotNullConstraint_Type_Keyword()
        {
            await TestAsync(
                "class X<T> where T : notnull { }",
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestNotNullConstraint_Type_ExistingInterface()
        {
            await TestAsync(@"
interface notnull {}
class X<T> where T : notnull { }",
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestNotNullConstraint_Type_ExistingInterfaceButOutOfScope()
        {
            await TestAsync(@"
namespace OtherScope
{
    interface notnull {}
}
class X<T> where T : notnull { }",
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestNotNullConstraint_Method_Keyword()
        {
            await TestAsync(@"
class X
{
    void M<T>() where T : notnull { }
}",
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestNotNullConstraint_Method_ExistingInterface()
        {
            await TestAsync(@"
interface notnull {}
class X
{
    void M<T>() where T : notnull { }
}",
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestNotNullConstraint_Method_ExistingInterfaceButOutOfScope()
        {
            await TestAsync(@"
namespace OtherScope
{
    interface notnull {}
}
class X
{
    void M<T>() where T : notnull { }
}",
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestNotNullConstraint_Delegate_Keyword()
        {
            await TestAsync(
                "delegate void D<T>() where T : notnull;",
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestNotNullConstraint_Delegate_ExistingInterface()
        {
            await TestAsync(@"
interface notnull {}
delegate void D<T>() where T : notnull;",
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestNotNullConstraint_Delegate_ExistingInterfaceButOutOfScope()
        {
            await TestAsync(@"
namespace OtherScope
{
    interface notnull {}
}
delegate void D<T>() where T : notnull;",
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestNotNullConstraint_LocalFunction_Keyword()
        {
            await TestAsync(@"
class X
{
    void N()
    {
        void M<T>() where T : notnull { }
    }
}",
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestNotNullConstraint_LocalFunction_ExistingInterface()
        {
            await TestAsync(@"
interface notnull {}
class X
{
    void N()
    {
        void M<T>() where T : notnull { }
    }
}",
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task TestNotNullConstraint_LocalFunction_ExistingInterfaceButOutOfScope()
        {
            await TestAsync(@"
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
}",
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
        }
    }
}
