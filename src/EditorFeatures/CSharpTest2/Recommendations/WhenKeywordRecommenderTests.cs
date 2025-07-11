// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class WhenKeywordRecommenderTests : KeywordRecommenderTests
{
    [Fact]
    public Task TestForCatchClause_AfterCatch()
        => VerifyKeywordAsync(AddInsideMethod(
@"try {} catch $$"));

    [Fact]
    public Task TestForCatchClause_AfterCatchDeclaration1()
        => VerifyKeywordAsync(AddInsideMethod(
@"try {} catch (Exception) $$"));

    [Fact]
    public Task TestForCatchClause_AfterCatchDeclaration2()
        => VerifyKeywordAsync(AddInsideMethod(
@"try {} catch (Exception e) $$"));

    [Fact]
    public Task TestForCatchClause_AfterCatchDeclarationEmpty()
        => VerifyKeywordAsync(AddInsideMethod(
@"try {} catch () $$"));

    [Fact]
    public Task TestForCatchClause_NotAfterTryBlock()
        => VerifyAbsenceAsync(AddInsideMethod(
@"try {} $$"));

    [Fact]
    public Task TestForCatchClause_NotAfterFilter1()
        => VerifyAbsenceAsync(AddInsideMethod(
@"try {} catch (Exception e) when $$"));

    [Fact]
    public Task TestForCatchClause_NotAfterFilter2()
        => VerifyAbsenceAsync(AddInsideMethod(
@"try {} catch (Exception e) when ($$"));

    [Fact]
    public Task TestForCatchClause_NotAfterFilter3()
        => VerifyAbsenceAsync(AddInsideMethod(
@"try {} catch (Exception e) when (true) $$"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/24113")]
    public async Task TestForSwitchCase_AfterDeclarationPattern()
        => await VerifyKeywordAsync(AddInsideMethod(@"switch (1) { case int i $$ }"));

    [Fact]
    public async Task TestForSwitchCase_AfterDeclarationPattern_BeforeBreak()
        => await VerifyKeywordAsync(AddInsideMethod(@"switch (1) { case int i $$ break; }"));

    [Fact]
    public async Task TestForSwitchCase_AfterDeclarationPattern_BeforeWhen()
        => await VerifyKeywordAsync(AddInsideMethod(@"switch (1) { case int i $$ when }"));

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/25084")]
    [InlineData("int.MinValue")]
    [InlineData("1")]
    [InlineData("1 + 1")]
    [InlineData("true ? 1 : 1")]
    [InlineData("(1 + )")]
    public async Task TestForSwitchCase_AfterExpression(string expression)
        => await VerifyKeywordAsync(AddInsideMethod($@"switch (1) {{ case {expression} $$ }}"));

    [Theory]
    [InlineData("int.MinValue")]
    [InlineData("1")]
    [InlineData("1 + 1")]
    [InlineData("true ? 1 : 1")]
    [InlineData("(1 + )")]
    public async Task TestForSwitchCase_AfterExpression_BeforeBreak(string expression)
        => await VerifyKeywordAsync(AddInsideMethod($@"switch (1) {{ case {expression} $$ break; }}"));

    [Theory]
    [InlineData("int.MinValue")]
    [InlineData("1")]
    [InlineData("1 + 1")]
    [InlineData("true ? 1 : 1")]
    [InlineData("(1 + )")]
    public async Task TestForSwitchCase_AfterExpression_BeforeWhen(string expression)
        => await VerifyKeywordAsync(AddInsideMethod($@"switch (1) {{ case {expression} $$ when }}"));

    [Theory]
    [InlineData("int.")]
    [InlineData("1 +")]
    [InlineData("true ?")]
    [InlineData("true ? 1")]
    [InlineData("true ? 1 :")]
    [InlineData("(1")]
    [InlineData("(1 + 1")]
    public async Task TestForSwitchCase_NotAfterIncompleteExpression(string expression)
        => await VerifyAbsenceAsync(AddInsideMethod($@"switch (1) {{ case {expression} $$ }}"));

    [Theory]
    [InlineData("int.")]
    [InlineData("1 +")]
    [InlineData("true ?")]
    [InlineData("true ? 1")]
    [InlineData("true ? 1 :")]
    [InlineData("(1")]
    [InlineData("(1 + 1")]
    public async Task TestForSwitchCase_NotAfterIncompleteExpression_BeforeBreak(string expression)
        => await VerifyAbsenceAsync(AddInsideMethod($@"switch (1) {{ case {expression} $$ break; }}"));

    [Theory]
    [InlineData("int.")]
    [InlineData("1 +")]
    [InlineData("true ?")]
    [InlineData("true ? 1")]
    [InlineData("true ? 1 :")]
    [InlineData("(1")]
    [InlineData("(1 + 1")]
    public async Task TestForSwitchCase_NotAfterIncompleteExpression_BeforeWhen(string expression)
        => await VerifyAbsenceAsync(AddInsideMethod($@"switch (1) {{ case {expression} $$ when }}"));

    [Fact]
    public async Task TestForSwitchCase_NotInsideExpression()
        => await VerifyAbsenceAsync(AddInsideMethod(@"switch (1) { case (1 + 1 $$) }"));

    [Fact]
    public async Task TestForSwitchCase_NotInsideExpression_BeforeBreak()
        => await VerifyAbsenceAsync(AddInsideMethod(@"switch (1) { case (1 + 1 $$) break; }"));

    [Fact]
    public async Task TestForSwitchCase_NotInsideExpression_BeforeWhen()
        => await VerifyAbsenceAsync(AddInsideMethod(@"switch (1) { case (1 + 1 $$) when }"));

    [Fact]
    public async Task TestForSwitchCase_NotAfterCase()
        => await VerifyAbsenceAsync(AddInsideMethod(@"switch (1) { case $$ }"));

    [Fact]
    public async Task TestForSwitchCase_NotAfterCase_BeforeBreak()
        => await VerifyAbsenceAsync(AddInsideMethod(@"switch (1) { case $$ break; }"));

    [Fact]
    public async Task TestForSwitchCase_NotAfterCase_BeforeWhen()
        => await VerifyAbsenceAsync(AddInsideMethod(@"switch (1) { case $$ when }"));

    [Fact]
    public async Task TestForSwitchCase_NotAfterDefault()
        => await VerifyAbsenceAsync(AddInsideMethod(@"switch (1) { default $$ }"));

    [Fact]
    public async Task TestForSwitchCase_NotAfterDefault_BeforeBreak()
        => await VerifyAbsenceAsync(AddInsideMethod(@"switch (1) { default $$ break; }"));

    [Fact]
    public async Task TestForSwitchCase_NotAfterWhen()
        => await VerifyAbsenceAsync(AddInsideMethod(@"switch (1) { case 1 when $$ }"));

    [Fact]
    public async Task TestForSwitchCase_NotAfterWhen_BeforeBreak()
        => await VerifyAbsenceAsync(AddInsideMethod(@"switch (1) { case 1 when $$ break; }"));

    [Fact]
    public async Task TestForSwitchCase_NotAfterColon()
        => await VerifyAbsenceAsync(AddInsideMethod(@"switch (1) { case 1: $$ }"));

    [Fact]
    public async Task TestForSwitchCase_NotAfterColon_BeforeBreak()
        => await VerifyAbsenceAsync(AddInsideMethod(@"switch (1) { case 1: $$ break; }"));

    [Fact]
    public async Task TestForSwitchCase_NotInEmptySwitchStatement()
        => await VerifyAbsenceAsync(AddInsideMethod(@"switch (1) { $$ }"));

    [Fact]
    public async Task TestForSwitchCase_SemanticCheck_NotAfterPredefinedType()
        => await VerifyKeywordAsync(AddInsideMethod(@"switch (new object()) { case int $$ }"));

    [Fact]
    public async Task TestForSwitchCase_SemanticCheck_NotAfterPredefinedType_BeforeBreak()
        => await VerifyKeywordAsync(AddInsideMethod(@"switch (new object()) { case int $$ break; }"));

    [Fact]
    public async Task TestForSwitchCase_SemanticCheck_NotAfterPredefinedType_BeforeWhen()
        => await VerifyKeywordAsync(AddInsideMethod(@"switch (new object()) { case int $$ when }"));

    [Fact]
    public async Task TestForSwitchCase_SemanticCheck_NotAfterGenericType()
        => await VerifyKeywordAsync(AddInsideMethod(@"switch (new object()) { case Dictionary<string, int> $$ }"));

    [Fact]
    public async Task TestForSwitchCase_SemanticCheck_NotAfterGenericType_BeforeBreak()
        => await VerifyKeywordAsync(AddInsideMethod(@"switch (new object()) { case Dictionary<string, int> $$ break; }"));

    [Fact]
    public async Task TestForSwitchCase_SemanticCheck_NotAfterGenericType_BeforeWhen()
        => await VerifyKeywordAsync(AddInsideMethod(@"switch (new object()) { case Dictionary<string, int> $$ when }"));

    [Fact]
    public async Task TestForSwitchCase_SemanticCheck_NotAfterCustomType()
        => await VerifyKeywordAsync("""
            class SyntaxNode { }
            class C
            {
                void M() { switch (new object()) { case SyntaxNode $$ } }
            }
            """);

    [Fact]
    public async Task TestForSwitchCase_SemanticCheck_NotAfterCustomType_BeforeBreak()
        => await VerifyKeywordAsync("""
            class SyntaxNode { }
            class C
            {
                void M() { switch (new object()) { case SyntaxNode $$ break; } }
            }
            """);

    [Fact]
    public async Task TestForSwitchCase_SemanticCheck_NotAfterCustomType_BeforeWhen()
        => await VerifyKeywordAsync("""
            class SyntaxNode { }
            class C
            {
                void M() { switch (new object()) { case SyntaxNode $$ when } }
            }
            """);

    [Fact]
    public async Task TestForSwitchCase_SemanticCheck_NotAfterTypeAlias()
        => await VerifyKeywordAsync("""
            using Type = System.String;
            class C
            {
                void M() { switch (new object()) { case Type $$ } }
            }
            """);

    [Fact]
    public async Task TestForSwitchCase_SemanticCheck_NotAfterTypeAlias_BeforeBreak()
        => await VerifyKeywordAsync("""
            using Type = System.String;
            class C
            {
                void M() { switch (new object()) { case Type $$ break; } }
            }
            """);

    [Fact]
    public async Task TestForSwitchCase_SemanticCheck_NotAfterTypeAlias_BeforeWhen()
        => await VerifyKeywordAsync("""
            using Type = System.String;
            class C
            {
                void M() { switch (new object()) { case Type $$ when } }
            }
            """);

    [Fact]
    public async Task TestForSwitchCase_SemanticCheck_NotAfterOverloadedTypeName()
=> await VerifyKeywordAsync("""
    class ValueTuple { }
    class ValueTuple<T> { }
    class C
    {
        void M() { switch (new object()) { case ValueTuple $$ } }
    }
    """);

    [Fact]
    public async Task TestForSwitchCase_SemanticCheck_NotAfterOverloadedTypeName_BeforeBreak()
        => await VerifyKeywordAsync("""
            class ValueTuple { }
            class ValueTuple<T> { }
            class C
            {
                void M() { switch (new object()) { case ValueTuple $$ break; } }
            }
            """);

    [Fact]
    public async Task TestForSwitchCase_SemanticCheck_NotAfterOverloadedTypeName_BeforeWhen()
        => await VerifyKeywordAsync("""
            class ValueTuple { }
            class ValueTuple<T> { }
            class C
            {
                void M() { switch (new object()) { case ValueTuple $$ when } }
            }
            """);

    [Fact]
    public async Task TestForSwitchCase_SemanticCheck_AfterColorColor()
        => await VerifyKeywordAsync("""
            class Color { }
            class C
            {
                const Color Color = null;
                void M() { switch (new object()) { case Color $$ } }
            }
            """);

    [Fact]
    public async Task TestForSwitchCase_SemanticCheck_AfterColorColor_BeforeBreak()
        => await VerifyKeywordAsync("""
            class Color { }
            class C
            {
                const Color Color = null;
                void M() { switch (new object()) { case Color $$ break; } }
            }
            """);

    [Fact]
    public async Task TestForSwitchCase_SemanticCheck_AfterColorColor_BeforeWhen()
        => await VerifyKeywordAsync("""
            class Color { }
            class C
            {
                const Color Color = null;
                void M() { switch (new object()) { case Color $$ when } }
            }
            """);

    [Fact]
    public async Task TestForSwitchCase_SemanticCheck_AfterOverloadedTypeNameColorColor()
=> await VerifyKeywordAsync("""
    class Color<T> { }
    class Color { }
    class C
    {
        const Color Color = null;
        void M() { switch (new object()) { case Color $$ } }
    }
    """);

    [Fact]
    public async Task TestForSwitchCase_SemanticCheck_AfterOverloadedTypeNameColorColor_BeforeBreak()
        => await VerifyKeywordAsync("""
            class Color<T> { }
            class Color { }
            class C
            {
                const Color Color = null;
                void M() { switch (new object()) { case Color $$ break; } }
            }
            """);

    [Fact]
    public async Task TestForSwitchCase_SemanticCheck_AfterOverloadedTypeNameColorColor_BeforeWhen()
        => await VerifyKeywordAsync("""
            class Color<T> { }
            class Color { }
            class C
            {
                const Color Color = null;
                void M() { switch (new object()) { case Color $$ when } }
            }
            """);

    [Fact]
    public async Task TestForSwitchCase_SemanticCheck_AfterLocalConstant()
        => await VerifyKeywordAsync(AddInsideMethod(@"const object c = null; switch (new object()) { case c $$ }"));

    [Fact]
    public async Task TestForSwitchCase_SemanticCheck_AfterLocalConstant_BeforeBreak()
        => await VerifyKeywordAsync(AddInsideMethod(@"const object c = null; switch (new object()) { case c $$ break; }"));

    [Fact]
    public async Task TestForSwitchCase_SemanticCheck_AfterLocalConstant_BeforeWhen()
        => await VerifyKeywordAsync(AddInsideMethod(@"const object c = null; switch (new object()) { case c $$ when }"));

    [Fact]
    public async Task TestForSwitchCase_SemanticCheck_AfterUnknownName()
        => await VerifyKeywordAsync(AddInsideMethod(@"switch (new object()) { case unknown $$ }"));

    [Fact]
    public async Task TestForSwitchCase_SemanticCheck_AfterUnknownName_BeforeBreak()
        => await VerifyKeywordAsync(AddInsideMethod(@"switch (new object()) { case unknown $$ break; }"));

    [Fact]
    public async Task TestForSwitchCase_SemanticCheck_AfterUnknownName_BeforeWhen()
        => await VerifyKeywordAsync(AddInsideMethod(@"switch (new object()) { case unknown $$ when }"));

    [Fact]
    public async Task TestForSwitchCase_SemanticCheck_NotAfterVar()
        => await VerifyAbsenceAsync(AddInsideMethod(@"switch (new object()) { case var $$ }"));

    [Fact]
    public async Task TestForSwitchCase_SemanticCheck_NotAfterVar_BeforeBreak()
        => await VerifyAbsenceAsync(AddInsideMethod(@"switch (new object()) { case var $$ break; }"));

    [Fact]
    public async Task TestForSwitchCase_SemanticCheck_NotAfterVar_BeforeWhen()
        => await VerifyAbsenceAsync(AddInsideMethod(@"switch (new object()) { case var $$ when }"));

    [Fact]
    public async Task TestForSwitchCase_SemanticCheck_NotAfterClassVar()
        => await VerifyAbsenceAsync("""
            class var { }
            class C
            {
                void M() { switch (new object()) { case var $$ } }
            }
            """);

    [Fact]
    public async Task TestForSwitchCase_SemanticCheck_NotAfterClassVar_BeforeBreak()
        => await VerifyAbsenceAsync("""
            class var { }
            class C
            {
                void M() { switch (new object()) { case var $$ break; } }
            }
            """);

    [Fact]
    public async Task TestForSwitchCase_SemanticCheck_NotAfterClassVar_BeforeWhen()
        => await VerifyAbsenceAsync("""
            class var { }
            class C
            {
                void M() { switch (new object()) { case var $$ when } }
            }
            """);

    [Fact]
    public Task TestForSwitchCase_SemanticCheck_NotAfterTypeAliasVar()
        => VerifyAbsenceAsync("""
            using var = System.String;
            class C
            {
                void M() { switch (new object()) { case var $$ } }
            }
            """);

    [Fact]
    public Task TestForSwitchCase_SemanticCheck_NotAfterTypeAliasVar_BeforeBreak()
        => VerifyAbsenceAsync("""
            using var = System.String;
            class C
            {
                void M() { switch (new object()) { case var $$ break; } }
            }
            """);

    [Fact]
    public Task TestForSwitchCase_SemanticCheck_NotAfterTypeAliasVar_BeforeWhen()
        => VerifyAbsenceAsync("""
            using var = System.String;
            class C
            {
                void M() { switch (new object()) { case var $$ when } }
            }
            """);

    [Fact]
    public async Task TestForSwitchCase_SemanticCheck_AfterLocalConstantVar()
        => await VerifyAbsenceAsync(AddInsideMethod(@"const object var = null; switch (new object()) { case var $$ }"));

    [Fact]
    public async Task TestForSwitchCase_SemanticCheck_AfterLocalConstantVar_BeforeBreak()
        => await VerifyAbsenceAsync(AddInsideMethod(@"const object var = null; switch (new object()) { case var $$ break; }"));

    [Fact]
    public async Task TestForSwitchCase_SemanticCheck_AfterLocalConstantVar_BeforeWhen()
        => await VerifyAbsenceAsync(AddInsideMethod(@"const object var = null; switch (new object()) { case var $$ when }"));

    [Fact]
    public async Task TestForSwitchCase_SemanticCheck_AfterClassAndLocalConstantVar()
        => await VerifyAbsenceAsync("""
            class var { }
            class C
            {
                void M() { const object var = null; switch (new object()) { case var $$ } }
            }
            """);

    [Fact]
    public async Task TestForSwitchCase_SemanticCheck_AfterClassAndLocalConstantVar_BeforeBreak()
=> await VerifyAbsenceAsync("""
    class var { }
    class C
    {
        void M() { const object var = null; switch (new object()) { case var $$ break; } }
    }
    """);

    [Fact]
    public async Task TestForSwitchCase_SemanticCheck_AfterClassAndLocalConstantVar_BeforeWhen()
=> await VerifyAbsenceAsync("""
    class var { }
    class C
    {
        void M() { const object var = null; switch (new object()) { case var $$ when } }
    }
    """);

    [Fact]
    public Task TestForSwitchCase_SemanticCheck_AfterTypeAliasAndFieldConstantVar()
        => VerifyAbsenceAsync("""
            using var = System.String;
            class C
            {
                const object var = null;
                void M() { switch (new object()) { case var $$ } }
            }
            """);

    [Fact]
    public Task TestForSwitchCase_SemanticCheck_AfterTypeAliasAndFieldConstantVar_BeforeBreak()
        => VerifyAbsenceAsync("""
            using var = System.String;
            class C
            {
                const object var = null;
                void M() { switch (new object()) { case var $$ break; } }
            }
            """);

    [Fact]
    public Task TestForSwitchCase_SemanticCheck_AfterTypeAliasAndFieldConstantVar_BeforeWhen()
        => VerifyAbsenceAsync("""
            using var = System.String;
            class C
            {
                const object var = null;
                void M() { switch (new object()) { case var $$ when } }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44480")]
    public Task TestAfterSwitchExpressionPattern1()
        => VerifyKeywordAsync("""
            using var = System.String;
            class C
            {
                void M(int i)
                {
                    _ = i switch
                    {
                        < 0 $$ => 1,
                    };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44480")]
    public Task TestAfterSwitchExpressionPattern2()
        => VerifyKeywordAsync("""
            using var = System.String;
            class C
            {
                void M(int i)
                {
                    _ = i switch
                    {
                        4 $$ => 1,
                    };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44480")]
    public Task TestAfterSwitchExpressionPattern3()
        => VerifyKeywordAsync("""
            using var = System.String;
            class C
            {
                void M(int i)
                {
                    _ = i switch
                    {
                        int $$ => 1,
                    };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44480")]
    public Task TestAfterSwitchExpressionPattern4()
        => VerifyKeywordAsync("""
            using var = System.String;
            class C
            {
                void M(int i)
                {
                    _ = i switch
                    {
                        int $$ or 1 => 1,
                    };
                }
            }
            """);
}
