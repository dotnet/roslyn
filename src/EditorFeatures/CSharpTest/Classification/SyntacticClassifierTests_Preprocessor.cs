// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Classification
{
    public partial class SyntacticClassifierTests
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_IfTrue()
        {
            var code =
@"#if true
#endif";
            TestInMethod(code,
                PPKeyword("#"),
                PPKeyword("if"),
                Keyword("true"),
                PPKeyword("#"),
                PPKeyword("endif"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_IfTrueWithComment()
        {
            var code =
@"#if true //Foo
#endif";
            TestInMethod(code,
                PPKeyword("#"),
                PPKeyword("if"),
                Keyword("true"),
                Comment("//Foo"),
                PPKeyword("#"),
                PPKeyword("endif"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_IfFalse()
        {
            var code =
@"#if false
#endif";
            TestInMethod(code,
                PPKeyword("#"),
                PPKeyword("if"),
                Keyword("false"),
                PPKeyword("#"),
                PPKeyword("endif"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_IfFOO()
        {
            var code =
@"#if FOO
#endif";
            TestInMethod(code,
                PPKeyword("#"),
                PPKeyword("if"),
                Identifier("FOO"),
                PPKeyword("#"),
                PPKeyword("endif"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_IfNotTrue()
        {
            var code =
@"#if !true
#endif";
            TestInMethod(code,
                PPKeyword("#"),
                PPKeyword("if"),
                Operators.Exclamation,
                Keyword("true"),
                PPKeyword("#"),
                PPKeyword("endif"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_IfNotFalse()
        {
            var code =
@"#if !false
#endif";
            TestInMethod(code,
                PPKeyword("#"),
                PPKeyword("if"),
                Operators.Exclamation,
                Keyword("false"),
                PPKeyword("#"),
                PPKeyword("endif"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_IfNotFOO()
        {
            var code =
@"#if !FOO
#endif";
            TestInMethod(code,
                PPKeyword("#"),
                PPKeyword("if"),
                Operators.Exclamation,
                Identifier("FOO"),
                PPKeyword("#"),
                PPKeyword("endif"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_IfTrueWithParens()
        {
            var code =
@"#if (true)
#endif";
            TestInMethod(code,
                PPKeyword("#"),
                PPKeyword("if"),
                Punctuation.OpenParen,
                Keyword("true"),
                Punctuation.CloseParen,
                PPKeyword("#"),
                PPKeyword("endif"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_IfFalseWithParens()
        {
            var code =
@"#if (false)
#endif";
            TestInMethod(code,
                PPKeyword("#"),
                PPKeyword("if"),
                Punctuation.OpenParen,
                Keyword("false"),
                Punctuation.CloseParen,
                PPKeyword("#"),
                PPKeyword("endif"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_IfFOOWithParens()
        {
            var code =
@"#if (FOO)
#endif";
            TestInMethod(code,
                PPKeyword("#"),
                PPKeyword("if"),
                Punctuation.OpenParen,
                Identifier("FOO"),
                Punctuation.CloseParen,
                PPKeyword("#"),
                PPKeyword("endif"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_IfOrExpression()
        {
            var code =
@"#if FOO || BAR
#endif";

            TestInMethod(code,
                PPKeyword("#"),
                PPKeyword("if"),
                Identifier("FOO"),
                Operators.DoublePipe,
                Identifier("BAR"),
                PPKeyword("#"),
                PPKeyword("endif"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_IfAndExpression()
        {
            var code =
@"#if FOO && BAR
#endif";

            TestInMethod(code,
                PPKeyword("#"),
                PPKeyword("if"),
                Identifier("FOO"),
                Operators.DoubleAmpersand,
                Identifier("BAR"),
                PPKeyword("#"),
                PPKeyword("endif"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_IfOrAndExpression()
        {
            var code =
@"#if FOO || BAR && BAZ
#endif";

            TestInMethod(code,
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_IfOrExpressionWithParens()
        {
            var code =
@"#if (FOO || BAR)
#endif";

            TestInMethod(code,
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_IfAndExpressionWithParens()
        {
            var code =
@"#if (FOO && BAR)
#endif";

            TestInMethod(code,
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_IfOrAndExpressionWithParens()
        {
            var code =
@"#if FOO || (BAR && BAZ)
#endif";

            TestInMethod(code,
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_If1()
        {
            Test("#if foo",
                PPKeyword("#"),
                PPKeyword("if"),
                Identifier("foo"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_If2()
        {
            Test(" #if foo",
                PPKeyword("#"),
                PPKeyword("if"),
                Identifier("foo"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_If3()
        {
            var code =
@"#if foo
#endif";
            Test(code,
                PPKeyword("#"),
                PPKeyword("if"),
                Identifier("foo"),
                PPKeyword("#"),
                PPKeyword("endif"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_If4()
        {
            var code =
@"#if
#endif";
            Test(code,
                PPKeyword("#"),
                PPKeyword("if"),
                PPKeyword("#"),
                PPKeyword("endif"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_If5()
        {
            var code =
@"#if
aoeu
aoeu
#endif";
            var start = code.IndexOf("#endif", StringComparison.Ordinal);
            Test(code,
                PPKeyword("#"),
                PPKeyword("if"),
                Inactive(@"aoeu
aoeu
"), PPKeyword("#"),
     PPKeyword("endif"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_If6()
        {
            var code =
@"#if
#else
aeu";
            Test(code,
                PPKeyword("#"),
                PPKeyword("if"),
                PPKeyword("#"),
                PPKeyword("else"),
                Identifier("aeu"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_If7()
        {
            var code =
@"#if
#else
#endif
aeu";
            Test(code,
                PPKeyword("#"),
                PPKeyword("if"),
                PPKeyword("#"),
                PPKeyword("else"),
                PPKeyword("#"),
                PPKeyword("endif"),
                Identifier("aeu"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_If8()
        {
            var code =
@"#if
#else
aoeu
aoeu
aou
#endif
aeu";
            Test(code,
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_If9()
        {
            var code =
@"#if //Foo1
#else //Foo2
aoeu
aoeu
aou
#endif //Foo3
aeu";
            Test(code,
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_Region1()
        {
            Test("#region Foo",
                PPKeyword("#"),
                PPKeyword("region"),
                PPText("Foo"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_Region2()
        {
            Test("   #region foo",
                PPKeyword("#"),
                PPKeyword("region"),
                PPText("foo"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_EndRegion1()
        {
            Test("#endregion",
                PPKeyword("#"),
                PPKeyword("endregion"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_EndRegion2()
        {
            Test("   #endregion",
                PPKeyword("#"),
                PPKeyword("endregion"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_EndRegion3()
        {
            Test("#endregion adsf",
                PPKeyword("#"),
                PPKeyword("endregion"),
                PPText("adsf"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_EndRegion4()
        {
            Test("   #endregion adsf",
                PPKeyword("#"),
                PPKeyword("endregion"),
                PPText("adsf"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_RegionEndRegion1()
        {
            Test(
@"#region
#endregion",
                PPKeyword("#"),
                PPKeyword("region"),
                PPKeyword("#"),
                PPKeyword("endregion"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_CommentAfterRegion1()
        {
            Test(
@"#region adsf //comment
#endregion",
                PPKeyword("#"),
                PPKeyword("region"),
                PPText("adsf //comment"),
                PPKeyword("#"),
                PPKeyword("endregion"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_CommentAfterRegion2()
        {
            Test(
@"#region //comment
#endregion",
                PPKeyword("#"),
                PPKeyword("region"),
                PPText("//comment"),
                PPKeyword("#"),
                PPKeyword("endregion"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_CommentAfterEndRegion1()
        {
            Test(
@"#region
#endregion adsf //comment",
                PPKeyword("#"),
                PPKeyword("region"),
                PPKeyword("#"),
                PPKeyword("endregion"),
                PPText("adsf //comment"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_CommentAfterEndRegion2()
        {
            Test(
@"#region
#endregion //comment",
                PPKeyword("#"),
                PPKeyword("region"),
                PPKeyword("#"),
                PPKeyword("endregion"),
                Comment("//comment"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_DeclarationDirectives()
        {
            Test(
@"#define A
#undef B",
                PPKeyword("#"),
                PPKeyword("define"),
                Identifier("A"),
                PPKeyword("#"),
                PPKeyword("undef"),
                Identifier("B"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_IfElseEndIfDirectives()
        {
            var code =
@"#if true
#elif DEBUG
#else
#endif";
            Test(code,
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_DefineDirective()
        {
            var code = @"#define FOO";
            Test(code,
                PPKeyword("#"),
                PPKeyword("define"),
                Identifier("FOO"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_DefineDirectiveWithCommentAndNoName()
        {
            var code = @"#define //Foo";
            Test(code,
                PPKeyword("#"),
                PPKeyword("define"),
                Comment("//Foo"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_DefineDirectiveWithComment()
        {
            var code = @"#define FOO //Foo";
            Test(code,
                PPKeyword("#"),
                PPKeyword("define"),
                Identifier("FOO"),
                Comment("//Foo"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_UndefDirectives()
        {
            var code = @"#undef FOO";

            Test(code,
                PPKeyword("#"),
                PPKeyword("undef"),
                Identifier("FOO"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_UndefDirectiveWithCommentAndNoName()
        {
            var code = @"#undef //Foo";
            Test(code,
                PPKeyword("#"),
                PPKeyword("undef"),
                Comment("//Foo"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_UndefDirectiveWithComment()
        {
            var code = @"#undef FOO //Foo";
            Test(code,
                PPKeyword("#"),
                PPKeyword("undef"),
                Identifier("FOO"),
                Comment("//Foo"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_ErrorDirective()
        {
            var code = @"#error FOO";

            Test(code,
                PPKeyword("#"),
                PPKeyword("error"),
                PPText("FOO"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_ErrorDirectiveWithComment()
        {
            var code = @"#error FOO //Foo";

            Test(code,
                PPKeyword("#"),
                PPKeyword("error"),
                PPText("FOO //Foo"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_WarningDirective()
        {
            var code = @"#warning FOO";

            Test(code,
                PPKeyword("#"),
                PPKeyword("warning"),
                PPText("FOO"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_WarningDirectiveWithComment()
        {
            var code = @"#warning FOO //Foo";

            Test(code,
                PPKeyword("#"),
                PPKeyword("warning"),
                PPText("FOO //Foo"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_LineHidden()
        {
            var code = @"#line hidden";

            Test(code,
                PPKeyword("#"),
                PPKeyword("line"),
                PPKeyword("hidden"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_LineHiddenWithComment()
        {
            var code = @"#line hidden //Foo";

            Test(code,
                PPKeyword("#"),
                PPKeyword("line"),
                PPKeyword("hidden"),
                Comment("//Foo"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_LineDefault()
        {
            var code = @"#line default";

            Test(code,
                PPKeyword("#"),
                PPKeyword("line"),
                PPKeyword("default"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_LineDefaultWithComment()
        {
            var code = @"#line default //Foo";

            Test(code,
                PPKeyword("#"),
                PPKeyword("line"),
                PPKeyword("default"),
                Comment("//Foo"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_LineNumber()
        {
            var code = @"#line 100";

            Test(code,
                PPKeyword("#"),
                PPKeyword("line"),
                Number("100"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_LineNumberWithComment()
        {
            var code = @"#line 100 //Foo";

            Test(code,
                PPKeyword("#"),
                PPKeyword("line"),
                Number("100"),
                Comment("//Foo"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_LineNumberWithFilename()
        {
            var code = @"#line 100 ""C:\Foo""";

            Test(code,
                PPKeyword("#"),
                PPKeyword("line"),
                Number("100"),
                String("\"C:\\Foo\""));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_LineNumberWithFilenameAndComment()
        {
            var code = @"#line 100 ""C:\Foo"" //Foo";

            Test(code,
                PPKeyword("#"),
                PPKeyword("line"),
                Number("100"),
                String("\"C:\\Foo\""),
                Comment("//Foo"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_PragmaChecksum1()
        {
            Test(
@"#pragma checksum stuff",
                PPKeyword("#"),
                PPKeyword("pragma"),
                PPKeyword("checksum"),
                PPText("stuff"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_PragmaChecksum2()
        {
            Test(
@"#pragma checksum ""file.txt"" ""{00000000-0000-0000-0000-000000000000}"" ""2453""",
                PPKeyword("#"),
                PPKeyword("pragma"),
                PPKeyword("checksum"),
                String("\"file.txt\""),
                String("\"{00000000-0000-0000-0000-000000000000}\""),
                String("\"2453\""));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_PragmaChecksum3()
        {
            Test(
@"#pragma checksum ""file.txt"" ""{00000000-0000-0000-0000-000000000000}"" ""2453"" // Foo",
                PPKeyword("#"),
                PPKeyword("pragma"),
                PPKeyword("checksum"),
                String("\"file.txt\""),
                String("\"{00000000-0000-0000-0000-000000000000}\""),
                String("\"2453\""),
                Comment("// Foo"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_PragmaWarningDisableOne()
        {
            var code = @"#pragma warning disable 100";

            Test(code,
                PPKeyword("#"),
                PPKeyword("pragma"),
                PPKeyword("warning"),
                PPKeyword("disable"),
                Number("100"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_PragmaWarningDisableOneWithComment()
        {
            var code = @"#pragma warning disable 100 //Foo";

            Test(code,
                PPKeyword("#"),
                PPKeyword("pragma"),
                PPKeyword("warning"),
                PPKeyword("disable"),
                Number("100"),
                Comment("//Foo"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_PragmaWarningRestoreOne()
        {
            var code = @"#pragma warning restore 100";

            Test(code,
                PPKeyword("#"),
                PPKeyword("pragma"),
                PPKeyword("warning"),
                PPKeyword("restore"),
                Number("100"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_PragmaWarningRestoreOneWithComment()
        {
            var code = @"#pragma warning restore 100 //Foo";

            Test(code,
                PPKeyword("#"),
                PPKeyword("pragma"),
                PPKeyword("warning"),
                PPKeyword("restore"),
                Number("100"),
                Comment("//Foo"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_PragmaWarningDisableTwo()
        {
            var code = @"#pragma warning disable 100, 101";

            Test(code,
                PPKeyword("#"),
                PPKeyword("pragma"),
                PPKeyword("warning"),
                PPKeyword("disable"),
                Number("100"),
                Punctuation.Comma,
                Number("101"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_PragmaWarningRestoreTwo()
        {
            var code = @"#pragma warning restore 100, 101";

            Test(code,
                PPKeyword("#"),
                PPKeyword("pragma"),
                PPKeyword("warning"),
                PPKeyword("restore"),
                Number("100"),
                Punctuation.Comma,
                Number("101"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_PragmaWarningDisableThree()
        {
            var code = @"#pragma warning disable 100, 101, 102";

            Test(code,
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.Classification)]
        public void PP_PragmaWarningRestoreThree()
        {
            var code = @"#pragma warning restore 100, 101, 102";

            Test(code,
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
