// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class WhenKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForCatchClause_AfterCatch()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"try {} catch $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForCatchClause_AfterCatchDeclaration1()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"try {} catch (Exception) $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForCatchClause_AfterCatchDeclaration2()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"try {} catch (Exception e) $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForCatchClause_AfterCatchDeclarationEmpty()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"try {} catch () $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForCatchClause_NotAfterTryBlock()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"try {} $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForCatchClause_NotAfterFilter1()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"try {} catch (Exception e) when $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForCatchClause_NotAfterFilter2()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"try {} catch (Exception e) when ($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForCatchClause_NotAfterFilter3()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"try {} catch (Exception e) when (true) $$"));
        }

        [WorkItem(24113, "https://github.com/dotnet/roslyn/issues/24113")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForSwitchCase_AfterDeclarationPattern() =>
            await VerifyKeywordAsync(AddInsideMethod(@"switch (1) { case int i $$ }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForSwitchCase_AfterDeclarationPattern_BeforeBreak() =>
            await VerifyKeywordAsync(AddInsideMethod(@"switch (1) { case int i $$ break; }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForSwitchCase_AfterDeclarationPattern_BeforeWhen() =>
            await VerifyKeywordAsync(AddInsideMethod(@"switch (1) { case int i $$ when }"));

        [WorkItem(25084, "https://github.com/dotnet/roslyn/issues/25084")]
        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [InlineData("int.MinValue")]
        [InlineData("1")]
        [InlineData("1 + 1")]
        [InlineData("true ? 1 : 1")]
        [InlineData("(1 + )")]
        public async Task TestForSwitchCase_AfterExpression(string expression) =>
            await VerifyKeywordAsync(AddInsideMethod($@"switch (1) {{ case {expression} $$ }}"));

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [InlineData("int.MinValue")]
        [InlineData("1")]
        [InlineData("1 + 1")]
        [InlineData("true ? 1 : 1")]
        [InlineData("(1 + )")]
        public async Task TestForSwitchCase_AfterExpression_BeforeBreak(string expression) =>
            await VerifyKeywordAsync(AddInsideMethod($@"switch (1) {{ case {expression} $$ break; }}"));

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [InlineData("int.MinValue")]
        [InlineData("1")]
        [InlineData("1 + 1")]
        [InlineData("true ? 1 : 1")]
        [InlineData("(1 + )")]
        public async Task TestForSwitchCase_AfterExpression_BeforeWhen(string expression) =>
            await VerifyKeywordAsync(AddInsideMethod($@"switch (1) {{ case {expression} $$ when }}"));

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [InlineData("int.")]
        [InlineData("1 +")]
        [InlineData("true ?")]
        [InlineData("true ? 1")]
        [InlineData("true ? 1 :")]
        [InlineData("(1")]
        [InlineData("(1 + 1")]
        public async Task TestForSwitchCase_NotAfterIncompleteExpression(string expression) =>
            await VerifyAbsenceAsync(AddInsideMethod($@"switch (1) {{ case {expression} $$ }}"));

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [InlineData("int.")]
        [InlineData("1 +")]
        [InlineData("true ?")]
        [InlineData("true ? 1")]
        [InlineData("true ? 1 :")]
        [InlineData("(1")]
        [InlineData("(1 + 1")]
        public async Task TestForSwitchCase_NotAfterIncompleteExpression_BeforeBreak(string expression) =>
            await VerifyAbsenceAsync(AddInsideMethod($@"switch (1) {{ case {expression} $$ break; }}"));

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [InlineData("int.")]
        [InlineData("1 +")]
        [InlineData("true ?")]
        [InlineData("true ? 1")]
        [InlineData("true ? 1 :")]
        [InlineData("(1")]
        [InlineData("(1 + 1")]
        public async Task TestForSwitchCase_NotAfterIncompleteExpression_BeforeWhen(string expression) =>
            await VerifyAbsenceAsync(AddInsideMethod($@"switch (1) {{ case {expression} $$ when }}"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForSwitchCase_NotInsideExpression() =>
            await VerifyAbsenceAsync(AddInsideMethod(@"switch (1) { case (1 + 1 $$) }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForSwitchCase_NotInsideExpression_BeforeBreak() =>
            await VerifyAbsenceAsync(AddInsideMethod(@"switch (1) { case (1 + 1 $$) break; }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForSwitchCase_NotInsideExpression_BeforeWhen() =>
            await VerifyAbsenceAsync(AddInsideMethod(@"switch (1) { case (1 + 1 $$) when }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForSwitchCase_NotAfterCase() =>
            await VerifyAbsenceAsync(AddInsideMethod(@"switch (1) { case $$ }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForSwitchCase_NotAfterCase_BeforeBreak() =>
            await VerifyAbsenceAsync(AddInsideMethod(@"switch (1) { case $$ break; }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForSwitchCase_NotAfterCase_BeforeWhen() =>
            await VerifyAbsenceAsync(AddInsideMethod(@"switch (1) { case $$ when }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForSwitchCase_NotAfterDefault() =>
            await VerifyAbsenceAsync(AddInsideMethod(@"switch (1) { default $$ }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForSwitchCase_NotAfterDefault_BeforeBreak() =>
            await VerifyAbsenceAsync(AddInsideMethod(@"switch (1) { default $$ break; }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForSwitchCase_NotAfterWhen() =>
            await VerifyAbsenceAsync(AddInsideMethod(@"switch (1) { case 1 when $$ }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForSwitchCase_NotAfterWhen_BeforeBreak() =>
            await VerifyAbsenceAsync(AddInsideMethod(@"switch (1) { case 1 when $$ break; }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForSwitchCase_NotAfterColon() =>
            await VerifyAbsenceAsync(AddInsideMethod(@"switch (1) { case 1: $$ }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForSwitchCase_NotAfterColon_BeforeBreak() =>
            await VerifyAbsenceAsync(AddInsideMethod(@"switch (1) { case 1: $$ break; }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForSwitchCase_NotInEmptySwitchStatement() =>
            await VerifyAbsenceAsync(AddInsideMethod(@"switch (1) { $$ }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForSwitchCase_SemanticCheck_NotAfterPredefinedType() =>
            await VerifyAbsenceAsync(AddInsideMethod(@"switch (new object()) { case int $$ }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForSwitchCase_SemanticCheck_NotAfterPredefinedType_BeforeBreak() =>
            await VerifyAbsenceAsync(AddInsideMethod(@"switch (new object()) { case int $$ break; }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForSwitchCase_SemanticCheck_NotAfterPredefinedType_BeforeWhen() =>
            await VerifyAbsenceAsync(AddInsideMethod(@"switch (new object()) { case int $$ when }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForSwitchCase_SemanticCheck_NotAfterGenericType() =>
            await VerifyAbsenceAsync(AddInsideMethod(@"switch (new object()) { case Dictionary<string, int> $$ }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForSwitchCase_SemanticCheck_NotAfterGenericType_BeforeBreak() =>
            await VerifyAbsenceAsync(AddInsideMethod(@"switch (new object()) { case Dictionary<string, int> $$ break; }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForSwitchCase_SemanticCheck_NotAfterGenericType_BeforeWhen() =>
            await VerifyAbsenceAsync(AddInsideMethod(@"switch (new object()) { case Dictionary<string, int> $$ when }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForSwitchCase_SemanticCheck_NotAfterCustomType() =>
            await VerifyAbsenceAsync(@"
class SyntaxNode { }
class C
{
    void M() { switch (new object()) { case SyntaxNode $$ } }
}");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForSwitchCase_SemanticCheck_NotAfterCustomType_BeforeBreak() =>
            await VerifyAbsenceAsync(@"
class SyntaxNode { }
class C
{
    void M() { switch (new object()) { case SyntaxNode $$ break; } }
}");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForSwitchCase_SemanticCheck_NotAfterCustomType_BeforeWhen() =>
            await VerifyAbsenceAsync(@"
class SyntaxNode { }
class C
{
    void M() { switch (new object()) { case SyntaxNode $$ when } }
}");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForSwitchCase_SemanticCheck_NotAfterTypeAlias() =>
            await VerifyAbsenceAsync(@"
using Type = System.String;
class C
{
    void M() { switch (new object()) { case Type $$ } }
}");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForSwitchCase_SemanticCheck_NotAfterTypeAlias_BeforeBreak() =>
            await VerifyAbsenceAsync(@"
using Type = System.String;
class C
{
    void M() { switch (new object()) { case Type $$ break; } }
}");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForSwitchCase_SemanticCheck_NotAfterTypeAlias_BeforeWhen() =>
            await VerifyAbsenceAsync(@"
using Type = System.String;
class C
{
    void M() { switch (new object()) { case Type $$ when } }
}");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForSwitchCase_SemanticCheck_NotAfterOverloadedTypeName() =>
    await VerifyAbsenceAsync(@"
class ValueTuple { }
class ValueTuple<T> { }
class C
{
    void M() { switch (new object()) { case ValueTuple $$ } }
}");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForSwitchCase_SemanticCheck_NotAfterOverloadedTypeName_BeforeBreak() =>
            await VerifyAbsenceAsync(@"
class ValueTuple { }
class ValueTuple<T> { }
class C
{
    void M() { switch (new object()) { case ValueTuple $$ break; } }
}");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForSwitchCase_SemanticCheck_NotAfterOverloadedTypeName_BeforeWhen() =>
            await VerifyAbsenceAsync(@"
class ValueTuple { }
class ValueTuple<T> { }
class C
{
    void M() { switch (new object()) { case ValueTuple $$ when } }
}");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForSwitchCase_SemanticCheck_AfterColorColor() =>
            await VerifyKeywordAsync(@"
class Color { }
class C
{
    const Color Color = null;
    void M() { switch (new object()) { case Color $$ } }
}");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForSwitchCase_SemanticCheck_AfterColorColor_BeforeBreak() =>
            await VerifyKeywordAsync(@"
class Color { }
class C
{
    const Color Color = null;
    void M() { switch (new object()) { case Color $$ break; } }
}");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForSwitchCase_SemanticCheck_AfterColorColor_BeforeWhen() =>
            await VerifyKeywordAsync(@"
class Color { }
class C
{
    const Color Color = null;
    void M() { switch (new object()) { case Color $$ when } }
}");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForSwitchCase_SemanticCheck_AfterOverloadedTypeNameColorColor() =>
    await VerifyKeywordAsync(@"
class Color<T> { }
class Color { }
class C
{
    const Color Color = null;
    void M() { switch (new object()) { case Color $$ } }
}");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForSwitchCase_SemanticCheck_AfterOverloadedTypeNameColorColor_BeforeBreak() =>
            await VerifyKeywordAsync(@"
class Color<T> { }
class Color { }
class C
{
    const Color Color = null;
    void M() { switch (new object()) { case Color $$ break; } }
}");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForSwitchCase_SemanticCheck_AfterOverloadedTypeNameColorColor_BeforeWhen() =>
            await VerifyKeywordAsync(@"
class Color<T> { }
class Color { }
class C
{
    const Color Color = null;
    void M() { switch (new object()) { case Color $$ when } }
}");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForSwitchCase_SemanticCheck_AfterLocalConstant() =>
            await VerifyKeywordAsync(AddInsideMethod(@"const object c = null; switch (new object()) { case c $$ }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForSwitchCase_SemanticCheck_AfterLocalConstant_BeforeBreak() =>
            await VerifyKeywordAsync(AddInsideMethod(@"const object c = null; switch (new object()) { case c $$ break; }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForSwitchCase_SemanticCheck_AfterLocalConstant_BeforeWhen() =>
            await VerifyKeywordAsync(AddInsideMethod(@"const object c = null; switch (new object()) { case c $$ when }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForSwitchCase_SemanticCheck_AfterUnknownName() =>
            await VerifyKeywordAsync(AddInsideMethod(@"switch (new object()) { case unknown $$ }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForSwitchCase_SemanticCheck_AfterUnknownName_BeforeBreak() =>
            await VerifyKeywordAsync(AddInsideMethod(@"switch (new object()) { case unknown $$ break; }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForSwitchCase_SemanticCheck_AfterUnknownName_BeforeWhen() =>
            await VerifyKeywordAsync(AddInsideMethod(@"switch (new object()) { case unknown $$ when }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForSwitchCase_SemanticCheck_NotAfterVar() =>
            await VerifyAbsenceAsync(AddInsideMethod(@"switch (new object()) { case var $$ }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForSwitchCase_SemanticCheck_NotAfterVar_BeforeBreak() =>
            await VerifyAbsenceAsync(AddInsideMethod(@"switch (new object()) { case var $$ break; }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForSwitchCase_SemanticCheck_NotAfterVar_BeforeWhen() =>
            await VerifyAbsenceAsync(AddInsideMethod(@"switch (new object()) { case var $$ when }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForSwitchCase_SemanticCheck_NotAfterClassVar() =>
            await VerifyAbsenceAsync(@"
class var { }
class C
{
    void M() { switch (new object()) { case var $$ } }
}");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForSwitchCase_SemanticCheck_NotAfterClassVar_BeforeBreak() =>
            await VerifyAbsenceAsync(@"
class var { }
class C
{
    void M() { switch (new object()) { case var $$ break; } }
}");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForSwitchCase_SemanticCheck_NotAfterClassVar_BeforeWhen() =>
            await VerifyAbsenceAsync(@"
class var { }
class C
{
    void M() { switch (new object()) { case var $$ when } }
}");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForSwitchCase_SemanticCheck_NotAfterTypeAliasVar()
        {
            await VerifyAbsenceAsync(@"
using var = System.String;
class C
{
    void M() { switch (new object()) { case var $$ } }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForSwitchCase_SemanticCheck_NotAfterTypeAliasVar_BeforeBreak()
        {
            await VerifyAbsenceAsync(@"
using var = System.String;
class C
{
    void M() { switch (new object()) { case var $$ break; } }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForSwitchCase_SemanticCheck_NotAfterTypeAliasVar_BeforeWhen()
        {
            await VerifyAbsenceAsync(@"
using var = System.String;
class C
{
    void M() { switch (new object()) { case var $$ when } }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForSwitchCase_SemanticCheck_AfterLocalConstantVar() =>
            await VerifyKeywordAsync(AddInsideMethod(@"const object var = null; switch (new object()) { case var $$ }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForSwitchCase_SemanticCheck_AfterLocalConstantVar_BeforeBreak() =>
            await VerifyKeywordAsync(AddInsideMethod(@"const object var = null; switch (new object()) { case var $$ break; }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForSwitchCase_SemanticCheck_AfterLocalConstantVar_BeforeWhen() =>
            await VerifyKeywordAsync(AddInsideMethod(@"const object var = null; switch (new object()) { case var $$ when }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForSwitchCase_SemanticCheck_AfterClassAndLocalConstantVar() =>
            await VerifyKeywordAsync(@"
class var { }
class C
{
    void M() { const object var = null; switch (new object()) { case var $$ } }
}");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForSwitchCase_SemanticCheck_AfterClassAndLocalConstantVar_BeforeBreak() =>
    await VerifyKeywordAsync(@"
class var { }
class C
{
    void M() { const object var = null; switch (new object()) { case var $$ break; } }
}");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForSwitchCase_SemanticCheck_AfterClassAndLocalConstantVar_BeforeWhen() =>
    await VerifyKeywordAsync(@"
class var { }
class C
{
    void M() { const object var = null; switch (new object()) { case var $$ when } }
}");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForSwitchCase_SemanticCheck_AfterTypeAliasAndFieldConstantVar()
        {
            await VerifyKeywordAsync(@"
using var = System.String;
class C
{
    const object var = null;
    void M() { switch (new object()) { case var $$ } }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForSwitchCase_SemanticCheck_AfterTypeAliasAndFieldConstantVar_BeforeBreak()
        {
            await VerifyKeywordAsync(@"
using var = System.String;
class C
{
    const object var = null;
    void M() { switch (new object()) { case var $$ break; } }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestForSwitchCase_SemanticCheck_AfterTypeAliasAndFieldConstantVar_BeforeWhen()
        {
            await VerifyKeywordAsync(@"
using var = System.String;
class C
{
    const object var = null;
    void M() { switch (new object()) { case var $$ when } }
}");
        }
    }
}
