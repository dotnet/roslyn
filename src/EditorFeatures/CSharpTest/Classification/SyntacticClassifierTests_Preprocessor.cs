// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Classification
{
    public partial class SyntacticClassifierTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_IfTrue()
        {
            var code =
@"#if true
#endif";
            await TestInMethodAsync(code,
                PPKeyword("#"),
                PPKeyword("if"),
                Keyword("true"),
                PPKeyword("#"),
                PPKeyword("endif"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_IfTrueWithComment()
        {
            var code =
@"#if true //Foo
#endif";
            await TestInMethodAsync(code,
                PPKeyword("#"),
                PPKeyword("if"),
                Keyword("true"),
                Comment("//Foo"),
                PPKeyword("#"),
                PPKeyword("endif"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_IfFalse()
        {
            var code =
@"#if false
#endif";
            await TestInMethodAsync(code,
                PPKeyword("#"),
                PPKeyword("if"),
                Keyword("false"),
                PPKeyword("#"),
                PPKeyword("endif"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_IfFOO()
        {
            var code =
@"#if FOO
#endif";
            await TestInMethodAsync(code,
                PPKeyword("#"),
                PPKeyword("if"),
                Identifier("FOO"),
                PPKeyword("#"),
                PPKeyword("endif"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_IfNotTrue()
        {
            var code =
@"#if !true
#endif";
            await TestInMethodAsync(code,
                PPKeyword("#"),
                PPKeyword("if"),
                Operators.Exclamation,
                Keyword("true"),
                PPKeyword("#"),
                PPKeyword("endif"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_IfNotFalse()
        {
            var code =
@"#if !false
#endif";
            await TestInMethodAsync(code,
                PPKeyword("#"),
                PPKeyword("if"),
                Operators.Exclamation,
                Keyword("false"),
                PPKeyword("#"),
                PPKeyword("endif"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_IfNotFOO()
        {
            var code =
@"#if !FOO
#endif";
            await TestInMethodAsync(code,
                PPKeyword("#"),
                PPKeyword("if"),
                Operators.Exclamation,
                Identifier("FOO"),
                PPKeyword("#"),
                PPKeyword("endif"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_IfTrueWithParens()
        {
            var code =
@"#if (true)
#endif";
            await TestInMethodAsync(code,
                PPKeyword("#"),
                PPKeyword("if"),
                Punctuation.OpenParen,
                Keyword("true"),
                Punctuation.CloseParen,
                PPKeyword("#"),
                PPKeyword("endif"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_IfFalseWithParens()
        {
            var code =
@"#if (false)
#endif";
            await TestInMethodAsync(code,
                PPKeyword("#"),
                PPKeyword("if"),
                Punctuation.OpenParen,
                Keyword("false"),
                Punctuation.CloseParen,
                PPKeyword("#"),
                PPKeyword("endif"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_IfFOOWithParens()
        {
            var code =
@"#if (FOO)
#endif";
            await TestInMethodAsync(code,
                PPKeyword("#"),
                PPKeyword("if"),
                Punctuation.OpenParen,
                Identifier("FOO"),
                Punctuation.CloseParen,
                PPKeyword("#"),
                PPKeyword("endif"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_IfOrExpression()
        {
            var code =
@"#if FOO || BAR
#endif";

            await TestInMethodAsync(code,
                PPKeyword("#"),
                PPKeyword("if"),
                Identifier("FOO"),
                Operators.DoublePipe,
                Identifier("BAR"),
                PPKeyword("#"),
                PPKeyword("endif"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_IfAndExpression()
        {
            var code =
@"#if FOO && BAR
#endif";

            await TestInMethodAsync(code,
                PPKeyword("#"),
                PPKeyword("if"),
                Identifier("FOO"),
                Operators.DoubleAmpersand,
                Identifier("BAR"),
                PPKeyword("#"),
                PPKeyword("endif"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_IfOrAndExpression()
        {
            var code =
@"#if FOO || BAR && BAZ
#endif";

            await TestInMethodAsync(code,
                PPKeyword("#"),
                PPKeyword("if"),
                Identifier("FOO"),
                Operators.DoublePipe,
                Identifier("BAR"),
                Operators.DoubleAmpersand,
                Identifier("BAZ"),
                PPKeyword("#"),
                PPKeyword("endif"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_IfOrExpressionWithParens()
        {
            var code =
@"#if (FOO || BAR)
#endif";

            await TestInMethodAsync(code,
                PPKeyword("#"),
                PPKeyword("if"),
                Punctuation.OpenParen,
                Identifier("FOO"),
                Operators.DoublePipe,
                Identifier("BAR"),
                Punctuation.CloseParen,
                PPKeyword("#"),
                PPKeyword("endif"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_IfAndExpressionWithParens()
        {
            var code =
@"#if (FOO && BAR)
#endif";

            await TestInMethodAsync(code,
                PPKeyword("#"),
                PPKeyword("if"),
                Punctuation.OpenParen,
                Identifier("FOO"),
                Operators.DoubleAmpersand,
                Identifier("BAR"),
                Punctuation.CloseParen,
                PPKeyword("#"),
                PPKeyword("endif"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_IfOrAndExpressionWithParens()
        {
            var code =
@"#if FOO || (BAR && BAZ)
#endif";

            await TestInMethodAsync(code,
                PPKeyword("#"),
                PPKeyword("if"),
                Identifier("FOO"),
                Operators.DoublePipe,
                Punctuation.OpenParen,
                Identifier("BAR"),
                Operators.DoubleAmpersand,
                Identifier("BAZ"),
                Punctuation.CloseParen,
                PPKeyword("#"),
                PPKeyword("endif"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_If1()
        {
            await TestAsync("#if foo",
                PPKeyword("#"),
                PPKeyword("if"),
                Identifier("foo"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_If2()
        {
            await TestAsync(" #if foo",
                PPKeyword("#"),
                PPKeyword("if"),
                Identifier("foo"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_If3()
        {
            var code =
@"#if foo
#endif";
            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("if"),
                Identifier("foo"),
                PPKeyword("#"),
                PPKeyword("endif"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_If4()
        {
            var code =
@"#if
#endif";
            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("if"),
                PPKeyword("#"),
                PPKeyword("endif"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_If5()
        {
            var code =
@"#if
aoeu
aoeu
#endif";
            var start = code.IndexOf("#endif", StringComparison.Ordinal);
            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("if"),
                Inactive(@"aoeu
aoeu
"), PPKeyword("#"),
     PPKeyword("endif"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_If6()
        {
            var code =
@"#if
#else
aeu";
            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("if"),
                PPKeyword("#"),
                PPKeyword("else"),
                Identifier("aeu"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_If7()
        {
            var code =
@"#if
#else
#endif
aeu";
            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("if"),
                PPKeyword("#"),
                PPKeyword("else"),
                PPKeyword("#"),
                PPKeyword("endif"),
                Identifier("aeu"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_If8()
        {
            var code =
@"#if
#else
aoeu
aoeu
aou
#endif
aeu";
            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("if"),
                PPKeyword("#"),
                PPKeyword("else"),
                Identifier("aoeu"),
                Identifier("aoeu"),
                Identifier("aou"),
                PPKeyword("#"),
                PPKeyword("endif"),
                Identifier("aeu"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_If9()
        {
            var code =
@"#if //Foo1
#else //Foo2
aoeu
aoeu
aou
#endif //Foo3
aeu";
            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("if"),
                Comment("//Foo1"),
                PPKeyword("#"),
                PPKeyword("else"),
                Comment("//Foo2"),
                Identifier("aoeu"),
                Identifier("aoeu"),
                Identifier("aou"),
                PPKeyword("#"),
                PPKeyword("endif"),
                Comment("//Foo3"),
                Identifier("aeu"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_Region1()
        {
            await TestAsync("#region Foo",
                PPKeyword("#"),
                PPKeyword("region"),
                PPText("Foo"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_Region2()
        {
            await TestAsync("   #region foo",
                PPKeyword("#"),
                PPKeyword("region"),
                PPText("foo"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_EndRegion1()
        {
            await TestAsync("#endregion",
                PPKeyword("#"),
                PPKeyword("endregion"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_EndRegion2()
        {
            await TestAsync("   #endregion",
                PPKeyword("#"),
                PPKeyword("endregion"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_EndRegion3()
        {
            await TestAsync("#endregion adsf",
                PPKeyword("#"),
                PPKeyword("endregion"),
                PPText("adsf"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_EndRegion4()
        {
            await TestAsync("   #endregion adsf",
                PPKeyword("#"),
                PPKeyword("endregion"),
                PPText("adsf"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_RegionEndRegion1()
        {
            await TestAsync(
@"#region
#endregion",
                PPKeyword("#"),
                PPKeyword("region"),
                PPKeyword("#"),
                PPKeyword("endregion"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_CommentAfterRegion1()
        {
            await TestAsync(
@"#region adsf //comment
#endregion",
                PPKeyword("#"),
                PPKeyword("region"),
                PPText("adsf //comment"),
                PPKeyword("#"),
                PPKeyword("endregion"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_CommentAfterRegion2()
        {
            await TestAsync(
@"#region //comment
#endregion",
                PPKeyword("#"),
                PPKeyword("region"),
                PPText("//comment"),
                PPKeyword("#"),
                PPKeyword("endregion"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_CommentAfterEndRegion1()
        {
            await TestAsync(
@"#region
#endregion adsf //comment",
                PPKeyword("#"),
                PPKeyword("region"),
                PPKeyword("#"),
                PPKeyword("endregion"),
                PPText("adsf //comment"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_CommentAfterEndRegion2()
        {
            await TestAsync(
@"#region
#endregion //comment",
                PPKeyword("#"),
                PPKeyword("region"),
                PPKeyword("#"),
                PPKeyword("endregion"),
                Comment("//comment"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_DeclarationDirectives()
        {
            await TestAsync(
@"#define A
#undef B",
                PPKeyword("#"),
                PPKeyword("define"),
                Identifier("A"),
                PPKeyword("#"),
                PPKeyword("undef"),
                Identifier("B"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_IfElseEndIfDirectives()
        {
            var code =
@"#if true
#elif DEBUG
#else
#endif";
            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("if"),
                Keyword("true"),
                PPKeyword("#"),
                PPKeyword("elif"),
                Identifier("DEBUG"),
                PPKeyword("#"),
                PPKeyword("else"),
                PPKeyword("#"),
                PPKeyword("endif"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_DefineDirective()
        {
            var code = @"#define FOO";
            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("define"),
                Identifier("FOO"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_DefineDirectiveWithCommentAndNoName()
        {
            var code = @"#define //Foo";
            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("define"),
                Comment("//Foo"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_DefineDirectiveWithComment()
        {
            var code = @"#define FOO //Foo";
            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("define"),
                Identifier("FOO"),
                Comment("//Foo"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_UndefDirectives()
        {
            var code = @"#undef FOO";

            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("undef"),
                Identifier("FOO"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_UndefDirectiveWithCommentAndNoName()
        {
            var code = @"#undef //Foo";
            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("undef"),
                Comment("//Foo"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_UndefDirectiveWithComment()
        {
            var code = @"#undef FOO //Foo";
            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("undef"),
                Identifier("FOO"),
                Comment("//Foo"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_ErrorDirective()
        {
            var code = @"#error FOO";

            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("error"),
                PPText("FOO"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_ErrorDirectiveWithComment()
        {
            var code = @"#error FOO //Foo";

            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("error"),
                PPText("FOO //Foo"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_WarningDirective()
        {
            var code = @"#warning FOO";

            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("warning"),
                PPText("FOO"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_WarningDirectiveWithComment()
        {
            var code = @"#warning FOO //Foo";

            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("warning"),
                PPText("FOO //Foo"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_LineHidden()
        {
            var code = @"#line hidden";

            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("line"),
                PPKeyword("hidden"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_LineHiddenWithComment()
        {
            var code = @"#line hidden //Foo";

            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("line"),
                PPKeyword("hidden"),
                Comment("//Foo"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_LineDefault()
        {
            var code = @"#line default";

            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("line"),
                PPKeyword("default"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_LineDefaultWithComment()
        {
            var code = @"#line default //Foo";

            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("line"),
                PPKeyword("default"),
                Comment("//Foo"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_LineNumber()
        {
            var code = @"#line 100";

            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("line"),
                Number("100"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_LineNumberWithComment()
        {
            var code = @"#line 100 //Foo";

            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("line"),
                Number("100"),
                Comment("//Foo"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_LineNumberWithFilename()
        {
            var code = @"#line 100 ""C:\Foo""";

            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("line"),
                Number("100"),
                String("\"C:\\Foo\""));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_LineNumberWithFilenameAndComment()
        {
            var code = @"#line 100 ""C:\Foo"" //Foo";

            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("line"),
                Number("100"),
                String("\"C:\\Foo\""),
                Comment("//Foo"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_PragmaChecksum1()
        {
            await TestAsync(
@"#pragma checksum stuff",
                PPKeyword("#"),
                PPKeyword("pragma"),
                PPKeyword("checksum"),
                PPText("stuff"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_PragmaChecksum2()
        {
            await TestAsync(
@"#pragma checksum ""file.txt"" ""{00000000-0000-0000-0000-000000000000}"" ""2453""",
                PPKeyword("#"),
                PPKeyword("pragma"),
                PPKeyword("checksum"),
                String("\"file.txt\""),
                String("\"{00000000-0000-0000-0000-000000000000}\""),
                String("\"2453\""));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_PragmaChecksum3()
        {
            await TestAsync(
@"#pragma checksum ""file.txt"" ""{00000000-0000-0000-0000-000000000000}"" ""2453"" // Foo",
                PPKeyword("#"),
                PPKeyword("pragma"),
                PPKeyword("checksum"),
                String("\"file.txt\""),
                String("\"{00000000-0000-0000-0000-000000000000}\""),
                String("\"2453\""),
                Comment("// Foo"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_PragmaWarningDisableOne()
        {
            var code = @"#pragma warning disable 100";

            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("pragma"),
                PPKeyword("warning"),
                PPKeyword("disable"),
                Number("100"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_PragmaWarningDisableOneWithComment()
        {
            var code = @"#pragma warning disable 100 //Foo";

            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("pragma"),
                PPKeyword("warning"),
                PPKeyword("disable"),
                Number("100"),
                Comment("//Foo"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_PragmaWarningRestoreOne()
        {
            var code = @"#pragma warning restore 100";

            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("pragma"),
                PPKeyword("warning"),
                PPKeyword("restore"),
                Number("100"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_PragmaWarningRestoreOneWithComment()
        {
            var code = @"#pragma warning restore 100 //Foo";

            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("pragma"),
                PPKeyword("warning"),
                PPKeyword("restore"),
                Number("100"),
                Comment("//Foo"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_PragmaWarningDisableTwo()
        {
            var code = @"#pragma warning disable 100, 101";

            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("pragma"),
                PPKeyword("warning"),
                PPKeyword("disable"),
                Number("100"),
                Punctuation.Comma,
                Number("101"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_PragmaWarningRestoreTwo()
        {
            var code = @"#pragma warning restore 100, 101";

            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("pragma"),
                PPKeyword("warning"),
                PPKeyword("restore"),
                Number("100"),
                Punctuation.Comma,
                Number("101"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_PragmaWarningDisableThree()
        {
            var code = @"#pragma warning disable 100, 101, 102";

            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("pragma"),
                PPKeyword("warning"),
                PPKeyword("disable"),
                Number("100"),
                Punctuation.Comma,
                Number("101"),
                Punctuation.Comma,
                Number("102"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_PragmaWarningRestoreThree()
        {
            var code = @"#pragma warning restore 100, 101, 102";

            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("pragma"),
                PPKeyword("warning"),
                PPKeyword("restore"),
                Number("100"),
                Punctuation.Comma,
                Number("101"),
                Punctuation.Comma,
                Number("102"));
        }
    }
}
