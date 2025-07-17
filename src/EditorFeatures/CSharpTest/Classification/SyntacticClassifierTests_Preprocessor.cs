// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.Editor.UnitTests.Classification.FormattedClassifications;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Classification;

[Trait(Traits.Feature, Traits.Features.Classification)]
public partial class SyntacticClassifierTests
{
    [Theory, CombinatorialData]
    public Task PP_IfTrue(TestHost testHost)
        => TestInMethodAsync("""
            #if true
            #endif
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword("if"),
            Keyword("true"),
            PPKeyword("#"),
            PPKeyword("endif"));

    [Theory, CombinatorialData]
    public Task PP_IfTrueWithComment(TestHost testHost)
        => TestInMethodAsync("""
            #if true //Goo
            #endif
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword("if"),
            Keyword("true"),
            Comment("//Goo"),
            PPKeyword("#"),
            PPKeyword("endif"));

    [Theory, CombinatorialData]
    public Task PP_IfFalse(TestHost testHost)
        => TestInMethodAsync("""
            #if false
            #endif
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword("if"),
            Keyword("false"),
            PPKeyword("#"),
            PPKeyword("endif"));

    [Theory, CombinatorialData]
    public Task PP_IfGOO(TestHost testHost)
        => TestInMethodAsync("""
            #if GOO
            #endif
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword("if"),
            Identifier("GOO"),
            PPKeyword("#"),
            PPKeyword("endif"));

    [Theory, CombinatorialData]
    public Task PP_IfNotTrue(TestHost testHost)
        => TestInMethodAsync("""
            #if !true
            #endif
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword("if"),
            Operators.Exclamation,
            Keyword("true"),
            PPKeyword("#"),
            PPKeyword("endif"));

    [Theory, CombinatorialData]
    public Task PP_IfNotFalse(TestHost testHost)
        => TestInMethodAsync("""
            #if !false
            #endif
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword("if"),
            Operators.Exclamation,
            Keyword("false"),
            PPKeyword("#"),
            PPKeyword("endif"));

    [Theory, CombinatorialData]
    public Task PP_IfNotGOO(TestHost testHost)
        => TestInMethodAsync("""
            #if !GOO
            #endif
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword("if"),
            Operators.Exclamation,
            Identifier("GOO"),
            PPKeyword("#"),
            PPKeyword("endif"));

    [Theory, CombinatorialData]
    public Task PP_IfTrueWithParens(TestHost testHost)
        => TestInMethodAsync("""
            #if (true)
            #endif
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword("if"),
            Punctuation.OpenParen,
            Keyword("true"),
            Punctuation.CloseParen,
            PPKeyword("#"),
            PPKeyword("endif"));

    [Theory, CombinatorialData]
    public Task PP_IfFalseWithParens(TestHost testHost)
        => TestInMethodAsync("""
            #if (false)
            #endif
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword("if"),
            Punctuation.OpenParen,
            Keyword("false"),
            Punctuation.CloseParen,
            PPKeyword("#"),
            PPKeyword("endif"));

    [Theory, CombinatorialData]
    public Task PP_IfGOOWithParens(TestHost testHost)
        => TestInMethodAsync("""
            #if (GOO)
            #endif
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword("if"),
            Punctuation.OpenParen,
            Identifier("GOO"),
            Punctuation.CloseParen,
            PPKeyword("#"),
            PPKeyword("endif"));

    [Theory, CombinatorialData]
    public Task PP_IfOrExpression(TestHost testHost)
        => TestInMethodAsync("""
            #if GOO || BAR
            #endif
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword("if"),
            Identifier("GOO"),
            Operators.BarBar,
            Identifier("BAR"),
            PPKeyword("#"),
            PPKeyword("endif"));

    [Theory, CombinatorialData]
    public Task PP_IfAndExpression(TestHost testHost)
        => TestInMethodAsync("""
            #if GOO && BAR
            #endif
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword("if"),
            Identifier("GOO"),
            Operators.AmpersandAmpersand,
            Identifier("BAR"),
            PPKeyword("#"),
            PPKeyword("endif"));

    [Theory, CombinatorialData]
    public Task PP_IfOrAndExpression(TestHost testHost)
        => TestInMethodAsync("""
            #if GOO || BAR && BAZ
            #endif
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword("if"),
            Identifier("GOO"),
            Operators.BarBar,
            Identifier("BAR"),
            Operators.AmpersandAmpersand,
            Identifier("BAZ"),
            PPKeyword("#"),
            PPKeyword("endif"));

    [Theory, CombinatorialData]
    public Task PP_IfOrExpressionWithParens(TestHost testHost)
        => TestInMethodAsync("""
            #if (GOO || BAR)
            #endif
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword("if"),
            Punctuation.OpenParen,
            Identifier("GOO"),
            Operators.BarBar,
            Identifier("BAR"),
            Punctuation.CloseParen,
            PPKeyword("#"),
            PPKeyword("endif"));

    [Theory, CombinatorialData]
    public Task PP_IfAndExpressionWithParens(TestHost testHost)
        => TestInMethodAsync("""
            #if (GOO && BAR)
            #endif
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword("if"),
            Punctuation.OpenParen,
            Identifier("GOO"),
            Operators.AmpersandAmpersand,
            Identifier("BAR"),
            Punctuation.CloseParen,
            PPKeyword("#"),
            PPKeyword("endif"));

    [Theory, CombinatorialData]
    public Task PP_IfOrAndExpressionWithParens(TestHost testHost)
        => TestInMethodAsync("""
            #if GOO || (BAR && BAZ)
            #endif
            """,
            testHost,
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

    [Theory, CombinatorialData]
    public Task PP_If1(TestHost testHost)
        => TestAsync("#if goo",
            testHost,
            PPKeyword("#"),
            PPKeyword("if"),
            Identifier("goo"));

    [Theory, CombinatorialData]
    public Task PP_If2(TestHost testHost)
        => TestAsync(" #if goo",
            testHost,
            PPKeyword("#"),
            PPKeyword("if"),
            Identifier("goo"));

    [Theory, CombinatorialData]
    public Task PP_If3(TestHost testHost)
        => TestAsync("""
            #if goo
            #endif
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword("if"),
            Identifier("goo"),
            PPKeyword("#"),
            PPKeyword("endif"));

    [Theory, CombinatorialData]
    public Task PP_If4(TestHost testHost)
        => TestAsync("""
            #if
            #endif
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword("if"),
            PPKeyword("#"),
            PPKeyword("endif"));

    [Theory, CombinatorialData]
    public Task PP_If5(TestHost testHost)
        => TestAsync("""
            #if
            aoeu
            aoeu
            #endif
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword("if"),
            Inactive("""
                aoeu
                aoeu

                """),
            PPKeyword("#"),
            PPKeyword("endif"));

    [Theory, CombinatorialData]
    public Task PP_If6(TestHost testHost)
        => TestAsync("""
            #if
            #else
            aeu
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword("if"),
            PPKeyword("#"),
            PPKeyword("else"),
            Identifier("aeu"));

    [Theory, CombinatorialData]
    public Task PP_If7(TestHost testHost)
        => TestAsync("""
            #if
            #else
            #endif
            aeu
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword("if"),
            PPKeyword("#"),
            PPKeyword("else"),
            PPKeyword("#"),
            PPKeyword("endif"),
            Identifier("aeu"));

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/44423")]
    [CombinatorialData]
    public async Task PP_If8(bool script, TestHost testHost)
    {
        var code =
            """
            #if
            #else
            aoeu
            aoeu
            aou
            #endif
            aeu
            """;

        var parseOptions = script ? Options.Script : null;

        await TestAsync(
            code,
            code,
            testHost,
            parseOptions,
            PPKeyword("#"),
            PPKeyword("if"),
            PPKeyword("#"),
            PPKeyword("else"),
            Identifier("aoeu"),
            script ? Field("aoeu") : Local("aoeu"),
            Identifier("aou"),
            PPKeyword("#"),
            PPKeyword("endif"),
            script ? Field("aeu") : Identifier("aeu"));
    }

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/44423")]
    [CombinatorialData]
    public async Task PP_If9(bool script, TestHost testHost)
    {
        var code =
            """
            #if //Goo1
            #else //Goo2
            aoeu
            aoeu
            aou
            #endif //Goo3
            aeu
            """;

        var parseOptions = script ? Options.Script : null;

        await TestAsync(
            code,
            code,
            testHost,
            parseOptions,
            PPKeyword("#"),
            PPKeyword("if"),
            Comment("//Goo1"),
            PPKeyword("#"),
            PPKeyword("else"),
            Comment("//Goo2"),
            Identifier("aoeu"),
            script ? Field("aoeu") : Local("aoeu"),
            Identifier("aou"),
            PPKeyword("#"),
            PPKeyword("endif"),
            Comment("//Goo3"),
            script ? Field("aeu") : Identifier("aeu"));
    }

    [Theory, CombinatorialData]
    public Task PP_Region1(TestHost testHost)
        => TestAsync("#region Goo",
            testHost,
            PPKeyword("#"),
            PPKeyword("region"),
            PPText("Goo"));

    [Theory, CombinatorialData]
    public Task PP_Region2(TestHost testHost)
        => TestAsync("   #region goo",
            testHost,
            PPKeyword("#"),
            PPKeyword("region"),
            PPText("goo"));

    [Theory, CombinatorialData]
    public Task PP_EndRegion1(TestHost testHost)
        => TestAsync("#endregion",
            testHost,
            PPKeyword("#"),
            PPKeyword("endregion"));

    [Theory, CombinatorialData]
    public Task PP_EndRegion2(TestHost testHost)
        => TestAsync("   #endregion",
            testHost,
            PPKeyword("#"),
            PPKeyword("endregion"));

    [Theory, CombinatorialData]
    public Task PP_EndRegion3(TestHost testHost)
        => TestAsync("#endregion adsf",
            testHost,
            PPKeyword("#"),
            PPKeyword("endregion"),
            PPText("adsf"));

    [Theory, CombinatorialData]
    public Task PP_EndRegion4(TestHost testHost)
        => TestAsync("   #endregion adsf",
            testHost,
            PPKeyword("#"),
            PPKeyword("endregion"),
            PPText("adsf"));

    [Theory, CombinatorialData]
    public Task PP_RegionEndRegion1(TestHost testHost)
        => TestAsync(
            """
            #region
            #endregion
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword("region"),
            PPKeyword("#"),
            PPKeyword("endregion"));

    [Theory, CombinatorialData]
    public Task PP_CommentAfterRegion1(TestHost testHost)
        => TestAsync(
            """
            #region adsf //comment
            #endregion
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword("region"),
            PPText("adsf //comment"),
            PPKeyword("#"),
            PPKeyword("endregion"));

    [Theory, CombinatorialData]
    public Task PP_CommentAfterRegion2(TestHost testHost)
        => TestAsync(
            """
            #region //comment
            #endregion
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword("region"),
            PPText("//comment"),
            PPKeyword("#"),
            PPKeyword("endregion"));

    [Theory, CombinatorialData]
    public Task PP_CommentAfterEndRegion1(TestHost testHost)
        => TestAsync(
            """
            #region
            #endregion adsf //comment
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword("region"),
            PPKeyword("#"),
            PPKeyword("endregion"),
            PPText("adsf //comment"));

    [Theory, CombinatorialData]
    public Task PP_CommentAfterEndRegion2(TestHost testHost)
        => TestAsync(
            """
            #region
            #endregion //comment
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword("region"),
            PPKeyword("#"),
            PPKeyword("endregion"),
            Comment("//comment"));

    [Theory, CombinatorialData]
    public Task PP_DeclarationDirectives(TestHost testHost)
        => TestAsync(
            """
            #define A
            #undef B
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword("define"),
            Identifier("A"),
            PPKeyword("#"),
            PPKeyword("undef"),
            Identifier("B"));

    [Theory, CombinatorialData]
    public Task PP_IfElseEndIfDirectives(TestHost testHost)
        => TestAsync("""
            #if true
            #elif DEBUG
            #else
            #endif
            """,
            testHost,
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

    [Theory, CombinatorialData]
    public Task PP_DefineDirective(TestHost testHost)
        => TestAsync(@"#define GOO",
            testHost,
            PPKeyword("#"),
            PPKeyword("define"),
            Identifier("GOO"));

    [Theory, CombinatorialData]
    public Task PP_DefineDirectiveWithCommentAndNoName(TestHost testHost)
        => TestAsync(@"#define //Goo",
            testHost,
            PPKeyword("#"),
            PPKeyword("define"),
            Comment("//Goo"));

    [Theory, CombinatorialData]
    public Task PP_DefineDirectiveWithComment(TestHost testHost)
        => TestAsync(@"#define GOO //Goo",
            testHost,
            PPKeyword("#"),
            PPKeyword("define"),
            Identifier("GOO"),
            Comment("//Goo"));

    [Theory, CombinatorialData]
    public Task PP_UndefDirectives(TestHost testHost)
        => TestAsync(@"#undef GOO",
            testHost,
            PPKeyword("#"),
            PPKeyword("undef"),
            Identifier("GOO"));

    [Theory, CombinatorialData]
    public Task PP_UndefDirectiveWithCommentAndNoName(TestHost testHost)
        => TestAsync(@"#undef //Goo",
            testHost,
            PPKeyword("#"),
            PPKeyword("undef"),
            Comment("//Goo"));

    [Theory, CombinatorialData]
    public Task PP_UndefDirectiveWithComment(TestHost testHost)
        => TestAsync(@"#undef GOO //Goo",
            testHost,
            PPKeyword("#"),
            PPKeyword("undef"),
            Identifier("GOO"),
            Comment("//Goo"));

    [Theory, CombinatorialData]
    public Task PP_ErrorDirective(TestHost testHost)
        => TestAsync(@"#error GOO",
            testHost,
            PPKeyword("#"),
            PPKeyword("error"),
            PPText("GOO"));

    [Theory, CombinatorialData]
    public Task PP_ErrorDirectiveWithComment(TestHost testHost)
        => TestAsync(@"#error GOO //Goo",
            testHost,
            PPKeyword("#"),
            PPKeyword("error"),
            PPText("GOO //Goo"));

    [Theory, CombinatorialData]
    public Task PP_WarningDirective(TestHost testHost)
        => TestAsync(@"#warning GOO",
            testHost,
            PPKeyword("#"),
            PPKeyword("warning"),
            PPText("GOO"));

    [Theory, CombinatorialData]
    public Task PP_WarningDirectiveWithComment(TestHost testHost)
        => TestAsync(@"#warning GOO //Goo",
            testHost,
            PPKeyword("#"),
            PPKeyword("warning"),
            PPText("GOO //Goo"));

    [Theory, CombinatorialData]
    public Task PP_LineHidden(TestHost testHost)
        => TestAsync(@"#line hidden",
            testHost,
            PPKeyword("#"),
            PPKeyword("line"),
            PPKeyword("hidden"));

    [Theory, CombinatorialData]
    public Task PP_LineHiddenWithComment(TestHost testHost)
        => TestAsync(@"#line hidden //Goo",
            testHost,
            PPKeyword("#"),
            PPKeyword("line"),
            PPKeyword("hidden"),
            Comment("//Goo"));

    [Theory, CombinatorialData]
    public Task PP_LineDefault(TestHost testHost)
        => TestAsync(@"#line default",
            testHost,
            PPKeyword("#"),
            PPKeyword("line"),
            PPKeyword("default"));

    [Theory, CombinatorialData]
    public Task PP_LineDefaultWithComment(TestHost testHost)
        => TestAsync(@"#line default //Goo",
            testHost,
            PPKeyword("#"),
            PPKeyword("line"),
            PPKeyword("default"),
            Comment("//Goo"));

    [Theory, CombinatorialData]
    public Task PP_LineNumber(TestHost testHost)
        => TestAsync(@"#line 100",
            testHost,
            PPKeyword("#"),
            PPKeyword("line"),
            Number("100"));

    [Theory, CombinatorialData]
    public Task PP_LineNumberWithComment(TestHost testHost)
        => TestAsync(@"#line 100 //Goo",
            testHost,
            PPKeyword("#"),
            PPKeyword("line"),
            Number("100"),
            Comment("//Goo"));

    [Theory, CombinatorialData]
    public Task PP_LineNumberWithFilename(TestHost testHost)
        => TestAsync("""
            #line 100 "C:\Goo"
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword("line"),
            Number("100"),
            String("""
                "C:\Goo"
                """));

    [Theory, CombinatorialData]
    public Task PP_LineNumberWithFilenameAndComment(TestHost testHost)
        => TestAsync(@"#line 100 ""C:\Goo"" //Goo",
            testHost,
            PPKeyword("#"),
            PPKeyword("line"),
            Number("100"),
            String("""
                "C:\Goo"
                """),
            Comment("//Goo"));

    [Theory, CombinatorialData]
    public Task PP_LineSpanWithCharacterOffset(TestHost testHost)
        => TestAsync("""
            #line (1, 2) - (3, 4) 5 "file.txt"
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword("line"),
            Punctuation.OpenParen,
            Number("1"),
            Punctuation.Comma,
            Number("2"),
            Punctuation.CloseParen,
            Operators.Minus,
            Punctuation.OpenParen,
            Number("3"),
            Punctuation.Comma,
            Number("4"),
            Punctuation.CloseParen,
            Number("5"),
            String("""
                "file.txt"
                """));

    [Theory, CombinatorialData]
    public Task PP_LineSpanWithComment(TestHost testHost)
        => TestAsync(@"#line (1, 2) - (3, 4) """" //comment",
            testHost,
            PPKeyword("#"),
            PPKeyword("line"),
            Punctuation.OpenParen,
            Number("1"),
            Punctuation.Comma,
            Number("2"),
            Punctuation.CloseParen,
            Operators.Minus,
            Punctuation.OpenParen,
            Number("3"),
            Punctuation.Comma,
            Number("4"),
            Punctuation.CloseParen,
            String("""
                ""
                """),
            Comment("//comment"));

    [Theory, CombinatorialData]
    public Task PP_NullableEnable(TestHost testHost)
        => TestAsync(@"#nullable enable",
            testHost,
            PPKeyword("#"),
            PPKeyword("nullable"),
            PPKeyword("enable"));

    [Theory, CombinatorialData]
    public Task PP_NullableEnableWithComment(TestHost testHost)
        => TestAsync(@"#nullable enable //Goo",
            testHost,
            PPKeyword("#"),
            PPKeyword("nullable"),
            PPKeyword("enable"),
            Comment("//Goo"));

    [Theory, CombinatorialData]
    public Task PP_NullableEnableWarnings(TestHost testHost)
        => TestAsync(@"#nullable enable warnings",
            testHost,
            PPKeyword("#"),
            PPKeyword("nullable"),
            PPKeyword("enable"),
            PPKeyword("warnings"));

    [Theory, CombinatorialData]
    public Task PP_NullableEnableWarningsWithComment(TestHost testHost)
        => TestAsync(@"#nullable enable warnings //Goo",
            testHost,
            PPKeyword("#"),
            PPKeyword("nullable"),
            PPKeyword("enable"),
            PPKeyword("warnings"),
            Comment("//Goo"));

    [Theory, CombinatorialData]
    public Task PP_NullableEnableAnnotations(TestHost testHost)
        => TestAsync(@"#nullable enable annotations",
            testHost,
            PPKeyword("#"),
            PPKeyword("nullable"),
            PPKeyword("enable"),
            PPKeyword("annotations"));

    [Theory, CombinatorialData]
    public Task PP_NullableEnableAnnotationsWithComment(TestHost testHost)
        => TestAsync(@"#nullable enable annotations //Goo",
            testHost,
            PPKeyword("#"),
            PPKeyword("nullable"),
            PPKeyword("enable"),
            PPKeyword("annotations"),
            Comment("//Goo"));

    [Theory, CombinatorialData]
    public Task PP_NullableDisable(TestHost testHost)
        => TestAsync(@"#nullable disable",
            testHost,
            PPKeyword("#"),
            PPKeyword("nullable"),
            PPKeyword("disable"));

    [Theory, CombinatorialData]
    public Task PP_NullableDisableWithComment(TestHost testHost)
        => TestAsync(@"#nullable disable //Goo",
            testHost,
            PPKeyword("#"),
            PPKeyword("nullable"),
            PPKeyword("disable"),
            Comment("//Goo"));

    [Theory, CombinatorialData]
    public Task PP_PragmaChecksum1(TestHost testHost)
        => TestAsync(
@"#pragma checksum stuff",
            testHost,
            PPKeyword("#"),
            PPKeyword("pragma"),
            PPKeyword("checksum"),
            PPText("stuff"));

    [Theory, CombinatorialData]
    public Task PP_PragmaChecksum2(TestHost testHost)
        => TestAsync(
            """
            #pragma checksum "file.txt" "{00000000-0000-0000-0000-000000000000}" "2453"
            """,
            testHost,
            PPKeyword("#"),
            PPKeyword("pragma"),
            PPKeyword("checksum"),
            String("""
                "file.txt"
                """),
            String("""
                "{00000000-0000-0000-0000-000000000000}"
                """),
            String("""
                "2453"
                """));

    [Theory, CombinatorialData]
    public Task PP_PragmaChecksum3(TestHost testHost)
        => TestAsync(
@"#pragma checksum ""file.txt"" ""{00000000-0000-0000-0000-000000000000}"" ""2453"" // Goo",
            testHost,
            PPKeyword("#"),
            PPKeyword("pragma"),
            PPKeyword("checksum"),
            String("""
                "file.txt"
                """),
            String("""
                "{00000000-0000-0000-0000-000000000000}"
                """),
            String("""
                "2453"
                """),
            Comment("// Goo"));

    [Theory, CombinatorialData]
    public Task PP_PragmaWarningDisableOne(TestHost testHost)
        => TestAsync(@"#pragma warning disable 100",
            testHost,
            PPKeyword("#"),
            PPKeyword("pragma"),
            PPKeyword("warning"),
            PPKeyword("disable"),
            Number("100"));

    [Theory, CombinatorialData]
    public Task PP_PragmaWarningDisableOneWithComment(TestHost testHost)
        => TestAsync(@"#pragma warning disable 100 //Goo",
            testHost,
            PPKeyword("#"),
            PPKeyword("pragma"),
            PPKeyword("warning"),
            PPKeyword("disable"),
            Number("100"),
            Comment("//Goo"));

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/30783")]
    public Task PP_PragmaWarningDisableAllWithComment(TestHost testHost)
        => TestAsync(@"#pragma warning disable //Goo",
            testHost,
            PPKeyword("#"),
            PPKeyword("pragma"),
            PPKeyword("warning"),
            PPKeyword("disable"),
            Comment("//Goo"));

    [Theory, CombinatorialData]
    public Task PP_PragmaWarningRestoreOne(TestHost testHost)
        => TestAsync(@"#pragma warning restore 100",
            testHost,
            PPKeyword("#"),
            PPKeyword("pragma"),
            PPKeyword("warning"),
            PPKeyword("restore"),
            Number("100"));

    [Theory, CombinatorialData]
    public Task PP_PragmaWarningRestoreOneWithComment(TestHost testHost)
        => TestAsync(@"#pragma warning restore 100 //Goo",
            testHost,
            PPKeyword("#"),
            PPKeyword("pragma"),
            PPKeyword("warning"),
            PPKeyword("restore"),
            Number("100"),
            Comment("//Goo"));

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/30783")]
    public Task PP_PragmaWarningRestoreAllWithComment(TestHost testHost)
        => TestAsync(@"#pragma warning restore //Goo",
            testHost,
            PPKeyword("#"),
            PPKeyword("pragma"),
            PPKeyword("warning"),
            PPKeyword("restore"),
            Comment("//Goo"));

    [Theory, CombinatorialData]
    public Task PP_PragmaWarningDisableTwo(TestHost testHost)
        => TestAsync(@"#pragma warning disable 100, 101",
            testHost,
            PPKeyword("#"),
            PPKeyword("pragma"),
            PPKeyword("warning"),
            PPKeyword("disable"),
            Number("100"),
            Punctuation.Comma,
            Number("101"));

    [Theory, CombinatorialData]
    public Task PP_PragmaWarningRestoreTwo(TestHost testHost)
        => TestAsync(@"#pragma warning restore 100, 101",
            testHost,
            PPKeyword("#"),
            PPKeyword("pragma"),
            PPKeyword("warning"),
            PPKeyword("restore"),
            Number("100"),
            Punctuation.Comma,
            Number("101"));

    [Theory, CombinatorialData]
    public Task PP_PragmaWarningDisableThree(TestHost testHost)
        => TestAsync(@"#pragma warning disable 100, 101, 102",
            testHost,
            PPKeyword("#"),
            PPKeyword("pragma"),
            PPKeyword("warning"),
            PPKeyword("disable"),
            Number("100"),
            Punctuation.Comma,
            Number("101"),
            Punctuation.Comma,
            Number("102"));

    [Theory, CombinatorialData]
    public Task PP_PragmaWarningRestoreThree(TestHost testHost)
        => TestAsync(@"#pragma warning restore 100, 101, 102",
            testHost,
            PPKeyword("#"),
            PPKeyword("pragma"),
            PPKeyword("warning"),
            PPKeyword("restore"),
            Number("100"),
            Punctuation.Comma,
            Number("101"),
            Punctuation.Comma,
            Number("102"));

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/75583")]
    public Task PP_AfterNonWhiteSpaceOnLine(TestHost testHost)
        => TestAsync("""
            if (#if false
            true
            #else
            false
            #endif
            ) { }
            """,
            testHost,
            ControlKeyword("if"),
            Punctuation.OpenParen,
            Keyword("true"),
            PPKeyword("#"),
            PPKeyword("else"),
            Keyword("false"),
            PPKeyword("#"),
            PPKeyword("endif"),
            Punctuation.CloseParen,
            Punctuation.OpenCurly,
            Punctuation.CloseCurly);

    [Theory, CombinatorialData]
    public Task DiscardInOutDeclaration(TestHost testHost)
        => TestInMethodAsync(
            code: @"M2(out var _);",
            testHost: testHost,
expected: Classifications(Identifier("M2"), Punctuation.OpenParen, Keyword("out"), Identifier("var"),
                Keyword("_"), Punctuation.CloseParen, Punctuation.Semicolon));

    [Theory, CombinatorialData]
    public Task DiscardInCasePattern(TestHost testHost)
        => TestInMethodAsync(
            code: @"switch (1) { case int _: }",
            testHost: testHost,
expected: Classifications(ControlKeyword("switch"), Punctuation.OpenParen, Number("1"), Punctuation.CloseParen,
                Punctuation.OpenCurly, ControlKeyword("case"), Keyword("int"), Keyword("_"), Punctuation.Colon, Punctuation.CloseCurly));

    [Theory, CombinatorialData]
    public Task DiscardInDeconstruction(TestHost testHost)
        => TestInMethodAsync(
            code: @"var (x, _) = (1, 2);",
            testHost: testHost,
expected: Classifications(Identifier("var"), Punctuation.OpenParen, Local("x"), Punctuation.Comma,
                Keyword("_"), Punctuation.CloseParen, Operators.Equals, Punctuation.OpenParen, Number("1"),
                Punctuation.Comma, Number("2"), Punctuation.CloseParen, Punctuation.Semicolon));

    [Theory, CombinatorialData]
    public Task DiscardInDeconstruction2(TestHost testHost)
        => TestInMethodAsync(
            code: @"(var _, var _) = (1, 2);",
            testHost: testHost,
expected: Classifications(Punctuation.OpenParen, Identifier("var"), Keyword("_"), Punctuation.Comma,
                Identifier("var"), Keyword("_"), Punctuation.CloseParen, Operators.Equals, Punctuation.OpenParen,
                Number("1"), Punctuation.Comma, Number("2"), Punctuation.CloseParen, Punctuation.Semicolon));

    [Theory, CombinatorialData]
    public Task ShortDiscardInDeconstruction(TestHost testHost)
        => TestInMethodAsync(
            code: @"int x; (_, x) = (1, 2);",
            testHost: testHost,
expected: Classifications(Keyword("int"), Local("x"), Punctuation.Semicolon, Punctuation.OpenParen,
                Identifier("_"), Punctuation.Comma, Identifier("x"), Punctuation.CloseParen, Operators.Equals,
                Punctuation.OpenParen, Number("1"), Punctuation.Comma, Number("2"), Punctuation.CloseParen,
                Punctuation.Semicolon));

    [Theory, CombinatorialData]
    public Task ShortDiscardInOutDeclaration(TestHost testHost)
        => TestInMethodAsync(
            code: @"M2(out _);",
            testHost: testHost,
expected: Classifications(Identifier("M2"), Punctuation.OpenParen, Keyword("out"), Identifier("_"), Punctuation.CloseParen,
                Punctuation.Semicolon));

    [Theory, CombinatorialData]
    public Task ShortDiscardInAssignment(TestHost testHost)
        => TestInMethodAsync(
            code: @"_ = 1;",
            testHost: testHost,
            expected: Classifications(Identifier("_"), Operators.Equals, Number("1"), Punctuation.Semicolon));

    [Theory, CombinatorialData]
    public Task UnderscoreInLambda(TestHost testHost)
        => TestInMethodAsync(
            code: @"x = (_) => 1;",
            testHost: testHost,
expected: Classifications(Identifier("x"), Operators.Equals, Punctuation.OpenParen, Parameter("_"), Punctuation.CloseParen,
                Operators.EqualsGreaterThan, Number("1"), Punctuation.Semicolon));

    [Theory, CombinatorialData]
    public Task DiscardInLambda(TestHost testHost)
        => TestInMethodAsync(
            code: @"x = (_, _) => 1;",
            testHost: testHost,
expected: Classifications(Identifier("x"), Operators.Equals, Punctuation.OpenParen, Parameter("_"), Punctuation.Comma, Parameter("_"), Punctuation.CloseParen,
                Operators.EqualsGreaterThan, Number("1"), Punctuation.Semicolon));

    [Theory, CombinatorialData]
    public Task UnderscoreInAssignment(TestHost testHost)
        => TestInMethodAsync(code: @"int _; _ = 1;",
            testHost: testHost,
expected: Classifications(Keyword("int"), Local("_"), Punctuation.Semicolon, Identifier("_"), Operators.Equals,
                Number("1"), Punctuation.Semicolon));
}
