// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.Editor.UnitTests.Classification.FormattedClassifications;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Classification
{
    [Trait(Traits.Feature, Traits.Features.Classification)]
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
@"#if true //Goo
#endif";
            await TestInMethodAsync(code,
                PPKeyword("#"),
                PPKeyword("if"),
                Keyword("true"),
                Comment("//Goo"),
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
        public async Task PP_IfGOO()
        {
            var code =
@"#if GOO
#endif";
            await TestInMethodAsync(code,
                PPKeyword("#"),
                PPKeyword("if"),
                Identifier("GOO"),
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
        public async Task PP_IfNotGOO()
        {
            var code =
@"#if !GOO
#endif";
            await TestInMethodAsync(code,
                PPKeyword("#"),
                PPKeyword("if"),
                Operators.Exclamation,
                Identifier("GOO"),
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
        public async Task PP_IfGOOWithParens()
        {
            var code =
@"#if (GOO)
#endif";
            await TestInMethodAsync(code,
                PPKeyword("#"),
                PPKeyword("if"),
                Punctuation.OpenParen,
                Identifier("GOO"),
                Punctuation.CloseParen,
                PPKeyword("#"),
                PPKeyword("endif"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_IfOrExpression()
        {
            var code =
@"#if GOO || BAR
#endif";

            await TestInMethodAsync(code,
                PPKeyword("#"),
                PPKeyword("if"),
                Identifier("GOO"),
                Operators.BarBar,
                Identifier("BAR"),
                PPKeyword("#"),
                PPKeyword("endif"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_IfAndExpression()
        {
            var code =
@"#if GOO && BAR
#endif";

            await TestInMethodAsync(code,
                PPKeyword("#"),
                PPKeyword("if"),
                Identifier("GOO"),
                Operators.AmpersandAmpersand,
                Identifier("BAR"),
                PPKeyword("#"),
                PPKeyword("endif"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_IfOrAndExpression()
        {
            var code =
@"#if GOO || BAR && BAZ
#endif";

            await TestInMethodAsync(code,
                PPKeyword("#"),
                PPKeyword("if"),
                Identifier("GOO"),
                Operators.BarBar,
                Identifier("BAR"),
                Operators.AmpersandAmpersand,
                Identifier("BAZ"),
                PPKeyword("#"),
                PPKeyword("endif"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_IfOrExpressionWithParens()
        {
            var code =
@"#if (GOO || BAR)
#endif";

            await TestInMethodAsync(code,
                PPKeyword("#"),
                PPKeyword("if"),
                Punctuation.OpenParen,
                Identifier("GOO"),
                Operators.BarBar,
                Identifier("BAR"),
                Punctuation.CloseParen,
                PPKeyword("#"),
                PPKeyword("endif"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_IfAndExpressionWithParens()
        {
            var code =
@"#if (GOO && BAR)
#endif";

            await TestInMethodAsync(code,
                PPKeyword("#"),
                PPKeyword("if"),
                Punctuation.OpenParen,
                Identifier("GOO"),
                Operators.AmpersandAmpersand,
                Identifier("BAR"),
                Punctuation.CloseParen,
                PPKeyword("#"),
                PPKeyword("endif"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_IfOrAndExpressionWithParens()
        {
            var code =
@"#if GOO || (BAR && BAZ)
#endif";

            await TestInMethodAsync(code,
                PPKeyword("#"),
                PPKeyword("if"),
                Identifier("GOO"),
                Operators.BarBar,
                Punctuation.OpenParen,
                Identifier("BAR"),
                Operators.AmpersandAmpersand,
                Identifier("BAZ"),
                Punctuation.CloseParen,
                PPKeyword("#"),
                PPKeyword("endif"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_If1()
        {
            await TestAsync("#if goo",
                PPKeyword("#"),
                PPKeyword("if"),
                Identifier("goo"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_If2()
        {
            await TestAsync(" #if goo",
                PPKeyword("#"),
                PPKeyword("if"),
                Identifier("goo"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_If3()
        {
            var code =
@"#if goo
#endif";
            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("if"),
                Identifier("goo"),
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
                Field("aoeu"),
                Identifier("aou"),
                PPKeyword("#"),
                PPKeyword("endif"),
                Field("aeu"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_If9()
        {
            var code =
@"#if //Goo1
#else //Goo2
aoeu
aoeu
aou
#endif //Goo3
aeu";
            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("if"),
                Comment("//Goo1"),
                PPKeyword("#"),
                PPKeyword("else"),
                Comment("//Goo2"),
                Identifier("aoeu"),
                Field("aoeu"),
                Identifier("aou"),
                PPKeyword("#"),
                PPKeyword("endif"),
                Comment("//Goo3"),
                Field("aeu"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_Region1()
        {
            await TestAsync("#region Goo",
                PPKeyword("#"),
                PPKeyword("region"),
                PPText("Goo"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_Region2()
        {
            await TestAsync("   #region goo",
                PPKeyword("#"),
                PPKeyword("region"),
                PPText("goo"));
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
            var code = @"#define GOO";
            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("define"),
                Identifier("GOO"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_DefineDirectiveWithCommentAndNoName()
        {
            var code = @"#define //Goo";
            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("define"),
                Comment("//Goo"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_DefineDirectiveWithComment()
        {
            var code = @"#define GOO //Goo";
            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("define"),
                Identifier("GOO"),
                Comment("//Goo"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_UndefDirectives()
        {
            var code = @"#undef GOO";

            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("undef"),
                Identifier("GOO"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_UndefDirectiveWithCommentAndNoName()
        {
            var code = @"#undef //Goo";
            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("undef"),
                Comment("//Goo"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_UndefDirectiveWithComment()
        {
            var code = @"#undef GOO //Goo";
            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("undef"),
                Identifier("GOO"),
                Comment("//Goo"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_ErrorDirective()
        {
            var code = @"#error GOO";

            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("error"),
                PPText("GOO"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_ErrorDirectiveWithComment()
        {
            var code = @"#error GOO //Goo";

            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("error"),
                PPText("GOO //Goo"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_WarningDirective()
        {
            var code = @"#warning GOO";

            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("warning"),
                PPText("GOO"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_WarningDirectiveWithComment()
        {
            var code = @"#warning GOO //Goo";

            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("warning"),
                PPText("GOO //Goo"));
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
            var code = @"#line hidden //Goo";

            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("line"),
                PPKeyword("hidden"),
                Comment("//Goo"));
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
            var code = @"#line default //Goo";

            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("line"),
                PPKeyword("default"),
                Comment("//Goo"));
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
            var code = @"#line 100 //Goo";

            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("line"),
                Number("100"),
                Comment("//Goo"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_LineNumberWithFilename()
        {
            var code = @"#line 100 ""C:\Goo""";

            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("line"),
                Number("100"),
                String("\"C:\\Goo\""));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_LineNumberWithFilenameAndComment()
        {
            var code = @"#line 100 ""C:\Goo"" //Goo";

            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("line"),
                Number("100"),
                String("\"C:\\Goo\""),
                Comment("//Goo"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_NullableEnable()
        {
            var code = @"#nullable enable";

            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("nullable"),
                PPKeyword("enable"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_NullableEnableWithComment()
        {
            var code = @"#nullable enable //Goo";

            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("nullable"),
                PPKeyword("enable"),
                Comment("//Goo"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_NullableEnableWarnings()
        {
            var code = @"#nullable enable warnings";

            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("nullable"),
                PPKeyword("enable"),
                PPKeyword("warnings"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_NullableEnableWarningsWithComment()
        {
            var code = @"#nullable enable warnings //Goo";

            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("nullable"),
                PPKeyword("enable"),
                PPKeyword("warnings"),
                Comment("//Goo"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_NullableEnableAnnotations()
        {
            var code = @"#nullable enable annotations";

            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("nullable"),
                PPKeyword("enable"),
                PPKeyword("annotations"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_NullableEnableAnnotationsWithComment()
        {
            var code = @"#nullable enable annotations //Goo";

            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("nullable"),
                PPKeyword("enable"),
                PPKeyword("annotations"),
                Comment("//Goo"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_NullableDisable()
        {
            var code = @"#nullable disable";

            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("nullable"),
                PPKeyword("disable"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        public async Task PP_NullableDisableWithComment()
        {
            var code = @"#nullable disable //Goo";

            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("nullable"),
                PPKeyword("disable"),
                Comment("//Goo"));
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
@"#pragma checksum ""file.txt"" ""{00000000-0000-0000-0000-000000000000}"" ""2453"" // Goo",
                PPKeyword("#"),
                PPKeyword("pragma"),
                PPKeyword("checksum"),
                String("\"file.txt\""),
                String("\"{00000000-0000-0000-0000-000000000000}\""),
                String("\"2453\""),
                Comment("// Goo"));
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
            var code = @"#pragma warning disable 100 //Goo";

            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("pragma"),
                PPKeyword("warning"),
                PPKeyword("disable"),
                Number("100"),
                Comment("//Goo"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        [WorkItem(30783, "https://github.com/dotnet/roslyn/issues/30783")]
        public async Task PP_PragmaWarningDisableAllWithComment()
        {
            var code = @"#pragma warning disable //Goo";

            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("pragma"),
                PPKeyword("warning"),
                PPKeyword("disable"),
                Comment("//Goo"));
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
            var code = @"#pragma warning restore 100 //Goo";

            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("pragma"),
                PPKeyword("warning"),
                PPKeyword("restore"),
                Number("100"),
                Comment("//Goo"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Classification)]
        [WorkItem(30783, "https://github.com/dotnet/roslyn/issues/30783")]
        public async Task PP_PragmaWarningRestoreAllWithComment()
        {
            var code = @"#pragma warning restore //Goo";

            await TestAsync(code,
                PPKeyword("#"),
                PPKeyword("pragma"),
                PPKeyword("warning"),
                PPKeyword("restore"),
                Comment("//Goo"));
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

        [Fact]
        public async Task DiscardInOutDeclaration()
        {
            await TestInMethodAsync(
                code: @"M2(out var _);",
                expected: Classifications(Identifier("M2"), Punctuation.OpenParen, Keyword("out"), Identifier("var"),
                    Keyword("_"), Punctuation.CloseParen, Punctuation.Semicolon));
        }

        [Fact]
        public async Task DiscardInCasePattern()
        {
            await TestInMethodAsync(
                code: @"switch (1) { case int _: }",
                expected: Classifications(ControlKeyword("switch"), Punctuation.OpenParen, Number("1"), Punctuation.CloseParen,
                    Punctuation.OpenCurly, ControlKeyword("case"), Keyword("int"), Keyword("_"), Punctuation.Colon, Punctuation.CloseCurly));
        }

        [Fact]
        public async Task DiscardInDeconstruction()
        {
            await TestInMethodAsync(
                code: @"var (x, _) = (1, 2);",
                expected: Classifications(Identifier("var"), Punctuation.OpenParen, Local("x"), Punctuation.Comma,
                    Keyword("_"), Punctuation.CloseParen, Operators.Equals, Punctuation.OpenParen, Number("1"),
                    Punctuation.Comma, Number("2"), Punctuation.CloseParen, Punctuation.Semicolon));
        }

        [Fact]
        public async Task DiscardInDeconstruction2()
        {
            await TestInMethodAsync(
                code: @"(var _, var _) = (1, 2);",
                expected: Classifications(Punctuation.OpenParen, Identifier("var"), Keyword("_"), Punctuation.Comma,
                    Identifier("var"), Keyword("_"), Punctuation.CloseParen, Operators.Equals, Punctuation.OpenParen,
                    Number("1"), Punctuation.Comma, Number("2"), Punctuation.CloseParen, Punctuation.Semicolon));
        }

        [Fact]
        public async Task ShortDiscardInDeconstruction()
        {
            await TestInMethodAsync(
                code: @"int x; (_, x) = (1, 2);",
                expected: Classifications(Keyword("int"), Local("x"), Punctuation.Semicolon, Punctuation.OpenParen,
                    Identifier("_"), Punctuation.Comma, Identifier("x"), Punctuation.CloseParen, Operators.Equals,
                    Punctuation.OpenParen, Number("1"), Punctuation.Comma, Number("2"), Punctuation.CloseParen,
                    Punctuation.Semicolon));
        }

        [Fact]
        public async Task ShortDiscardInOutDeclaration()
        {
            await TestInMethodAsync(
                code: @"M2(out _);",
                expected: Classifications(Identifier("M2"), Punctuation.OpenParen, Keyword("out"), Identifier("_"), Punctuation.CloseParen,
                    Punctuation.Semicolon));
        }

        [Fact]
        public async Task ShortDiscardInAssignment()
        {
            await TestInMethodAsync(
                code: @"_ = 1;",
                expected: Classifications(Identifier("_"), Operators.Equals, Number("1"), Punctuation.Semicolon));
        }

        [Fact]
        public async Task UnderscoreInLambda()
        {
            await TestInMethodAsync(
                code: @"x = (_) => 1;",
                expected: Classifications(Identifier("x"), Operators.Equals, Punctuation.OpenParen, Parameter("_"), Punctuation.CloseParen,
                    Operators.EqualsGreaterThan, Number("1"), Punctuation.Semicolon));
        }

        [Fact]
        public async Task DiscardInLambda()
        {
            await TestInMethodAsync(
                code: @"x = (_, _) => 1;",
                expected: Classifications(Identifier("x"), Operators.Equals, Punctuation.OpenParen, Parameter("_"), Punctuation.Comma, Parameter("_"), Punctuation.CloseParen,
                    Operators.EqualsGreaterThan, Number("1"), Punctuation.Semicolon));
        }

        [Fact]
        public async Task UnderscoreInAssignment()
        {
            await TestInMethodAsync(code: @"int _; _ = 1;",
                expected: Classifications(Keyword("int"), Local("_"), Punctuation.Semicolon, Identifier("_"), Operators.Equals,
                    Number("1"), Punctuation.Semicolon));
        }
    }
}
