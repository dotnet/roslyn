// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class WhenKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForCatchClause_AfterCatch()
        {
            VerifyKeyword(AddInsideMethod(
@"try {} catch $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForCatchClause_AfterCatchDeclaration1()
        {
            VerifyKeyword(AddInsideMethod(
@"try {} catch (Exception) $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForCatchClause_AfterCatchDeclaration2()
        {
            VerifyKeyword(AddInsideMethod(
@"try {} catch (Exception e) $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForCatchClause_AfterCatchDeclarationEmpty()
        {
            VerifyKeyword(AddInsideMethod(
@"try {} catch () $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForCatchClause_NotAfterTryBlock()
        {
            VerifyAbsence(AddInsideMethod(
@"try {} $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForCatchClause_NotAfterFilter1()
        {
            VerifyAbsence(AddInsideMethod(
@"try {} catch (Exception e) when $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForCatchClause_NotAfterFilter2()
        {
            VerifyAbsence(AddInsideMethod(
@"try {} catch (Exception e) when ($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForCatchClause_NotAfterFilter3()
        {
            VerifyAbsence(AddInsideMethod(
@"try {} catch (Exception e) when (true) $$"));
        }

        [WorkItem(24113, "https://github.com/dotnet/roslyn/issues/24113")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForSwitchCase_AfterDeclarationPattern() =>
            VerifyKeyword(AddInsideMethod(@"switch (1) { case int i $$ }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForSwitchCase_AfterDeclarationPattern_BeforeBreak() =>
            VerifyKeyword(AddInsideMethod(@"switch (1) { case int i $$ break; }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForSwitchCase_AfterDeclarationPattern_BeforeWhen() =>
            VerifyKeyword(AddInsideMethod(@"switch (1) { case int i $$ when }"));

        [WorkItem(25084, "https://github.com/dotnet/roslyn/issues/25084")]
        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [InlineData("int.MinValue")]
        [InlineData("1")]
        [InlineData("1 + 1")]
        [InlineData("true ? 1 : 1")]
        [InlineData("(1 + )")]
        public void TestForSwitchCase_AfterExpression(string expression) =>
            VerifyKeyword(AddInsideMethod($@"switch (1) {{ case {expression} $$ }}"));

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [InlineData("int.MinValue")]
        [InlineData("1")]
        [InlineData("1 + 1")]
        [InlineData("true ? 1 : 1")]
        [InlineData("(1 + )")]
        public void TestForSwitchCase_AfterExpression_BeforeBreak(string expression) =>
            VerifyKeyword(AddInsideMethod($@"switch (1) {{ case {expression} $$ break; }}"));

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [InlineData("int.MinValue")]
        [InlineData("1")]
        [InlineData("1 + 1")]
        [InlineData("true ? 1 : 1")]
        [InlineData("(1 + )")]
        public void TestForSwitchCase_AfterExpression_BeforeWhen(string expression) =>
            VerifyKeyword(AddInsideMethod($@"switch (1) {{ case {expression} $$ when }}"));

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [InlineData("int.")]
        [InlineData("1 +")]
        [InlineData("true ?")]
        [InlineData("true ? 1")]
        [InlineData("true ? 1 :")]
        [InlineData("(1")]
        [InlineData("(1 + 1")]
        public void TestForSwitchCase_NotAfterIncompleteExpression(string expression) =>
            VerifyAbsence(AddInsideMethod($@"switch (1) {{ case {expression} $$ }}"));

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [InlineData("int.")]
        [InlineData("1 +")]
        [InlineData("true ?")]
        [InlineData("true ? 1")]
        [InlineData("true ? 1 :")]
        [InlineData("(1")]
        [InlineData("(1 + 1")]
        public void TestForSwitchCase_NotAfterIncompleteExpression_BeforeBreak(string expression) =>
            VerifyAbsence(AddInsideMethod($@"switch (1) {{ case {expression} $$ break; }}"));

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [InlineData("int.")]
        [InlineData("1 +")]
        [InlineData("true ?")]
        [InlineData("true ? 1")]
        [InlineData("true ? 1 :")]
        [InlineData("(1")]
        [InlineData("(1 + 1")]
        public void TestForSwitchCase_NotAfterIncompleteExpression_BeforeWhen(string expression) =>
            VerifyAbsence(AddInsideMethod($@"switch (1) {{ case {expression} $$ when }}"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForSwitchCase_NotInsideExpression() =>
            VerifyAbsence(AddInsideMethod(@"switch (1) { case (1 + 1 $$) }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForSwitchCase_NotInsideExpression_BeforeBreak() =>
            VerifyAbsence(AddInsideMethod(@"switch (1) { case (1 + 1 $$) break; }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForSwitchCase_NotInsideExpression_BeforeWhen() =>
            VerifyAbsence(AddInsideMethod(@"switch (1) { case (1 + 1 $$) when }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForSwitchCase_NotAfterCase() =>
            VerifyAbsence(AddInsideMethod(@"switch (1) { case $$ }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForSwitchCase_NotAfterCase_BeforeBreak() =>
            VerifyAbsence(AddInsideMethod(@"switch (1) { case $$ break; }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForSwitchCase_NotAfterCase_BeforeWhen() =>
            VerifyAbsence(AddInsideMethod(@"switch (1) { case $$ when }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForSwitchCase_NotAfterDefault() =>
            VerifyAbsence(AddInsideMethod(@"switch (1) { default $$ }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForSwitchCase_NotAfterDefault_BeforeBreak() =>
            VerifyAbsence(AddInsideMethod(@"switch (1) { default $$ break; }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForSwitchCase_NotAfterWhen() =>
            VerifyAbsence(AddInsideMethod(@"switch (1) { case 1 when $$ }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForSwitchCase_NotAfterWhen_BeforeBreak() =>
            VerifyAbsence(AddInsideMethod(@"switch (1) { case 1 when $$ break; }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForSwitchCase_NotAfterColon() =>
            VerifyAbsence(AddInsideMethod(@"switch (1) { case 1: $$ }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForSwitchCase_NotAfterColon_BeforeBreak() =>
            VerifyAbsence(AddInsideMethod(@"switch (1) { case 1: $$ break; }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForSwitchCase_NotInEmptySwitchStatement() =>
            VerifyAbsence(AddInsideMethod(@"switch (1) { $$ }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForSwitchCase_SemanticCheck_NotAfterPredefinedType() =>
            VerifyKeyword(AddInsideMethod(@"switch (new object()) { case int $$ }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForSwitchCase_SemanticCheck_NotAfterPredefinedType_BeforeBreak() =>
            VerifyKeyword(AddInsideMethod(@"switch (new object()) { case int $$ break; }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForSwitchCase_SemanticCheck_NotAfterPredefinedType_BeforeWhen() =>
            VerifyKeyword(AddInsideMethod(@"switch (new object()) { case int $$ when }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForSwitchCase_SemanticCheck_NotAfterGenericType() =>
            VerifyKeyword(AddInsideMethod(@"switch (new object()) { case Dictionary<string, int> $$ }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForSwitchCase_SemanticCheck_NotAfterGenericType_BeforeBreak() =>
            VerifyKeyword(AddInsideMethod(@"switch (new object()) { case Dictionary<string, int> $$ break; }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForSwitchCase_SemanticCheck_NotAfterGenericType_BeforeWhen() =>
            VerifyKeyword(AddInsideMethod(@"switch (new object()) { case Dictionary<string, int> $$ when }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForSwitchCase_SemanticCheck_NotAfterCustomType() =>
            VerifyKeyword(@"
class SyntaxNode { }
class C
{
    void M() { switch (new object()) { case SyntaxNode $$ } }
}");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForSwitchCase_SemanticCheck_NotAfterCustomType_BeforeBreak() =>
            VerifyKeyword(@"
class SyntaxNode { }
class C
{
    void M() { switch (new object()) { case SyntaxNode $$ break; } }
}");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForSwitchCase_SemanticCheck_NotAfterCustomType_BeforeWhen() =>
            VerifyKeyword(@"
class SyntaxNode { }
class C
{
    void M() { switch (new object()) { case SyntaxNode $$ when } }
}");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForSwitchCase_SemanticCheck_NotAfterTypeAlias() =>
            VerifyKeyword(@"
using Type = System.String;
class C
{
    void M() { switch (new object()) { case Type $$ } }
}");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForSwitchCase_SemanticCheck_NotAfterTypeAlias_BeforeBreak() =>
            VerifyKeyword(@"
using Type = System.String;
class C
{
    void M() { switch (new object()) { case Type $$ break; } }
}");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForSwitchCase_SemanticCheck_NotAfterTypeAlias_BeforeWhen() =>
            VerifyKeyword(@"
using Type = System.String;
class C
{
    void M() { switch (new object()) { case Type $$ when } }
}");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForSwitchCase_SemanticCheck_NotAfterOverloadedTypeName() =>
    VerifyKeyword(@"
class ValueTuple { }
class ValueTuple<T> { }
class C
{
    void M() { switch (new object()) { case ValueTuple $$ } }
}");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForSwitchCase_SemanticCheck_NotAfterOverloadedTypeName_BeforeBreak() =>
            VerifyKeyword(@"
class ValueTuple { }
class ValueTuple<T> { }
class C
{
    void M() { switch (new object()) { case ValueTuple $$ break; } }
}");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForSwitchCase_SemanticCheck_NotAfterOverloadedTypeName_BeforeWhen() =>
            VerifyKeyword(@"
class ValueTuple { }
class ValueTuple<T> { }
class C
{
    void M() { switch (new object()) { case ValueTuple $$ when } }
}");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForSwitchCase_SemanticCheck_AfterColorColor() =>
            VerifyKeyword(@"
class Color { }
class C
{
    const Color Color = null;
    void M() { switch (new object()) { case Color $$ } }
}");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForSwitchCase_SemanticCheck_AfterColorColor_BeforeBreak() =>
            VerifyKeyword(@"
class Color { }
class C
{
    const Color Color = null;
    void M() { switch (new object()) { case Color $$ break; } }
}");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForSwitchCase_SemanticCheck_AfterColorColor_BeforeWhen() =>
            VerifyKeyword(@"
class Color { }
class C
{
    const Color Color = null;
    void M() { switch (new object()) { case Color $$ when } }
}");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForSwitchCase_SemanticCheck_AfterOverloadedTypeNameColorColor() =>
    VerifyKeyword(@"
class Color<T> { }
class Color { }
class C
{
    const Color Color = null;
    void M() { switch (new object()) { case Color $$ } }
}");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForSwitchCase_SemanticCheck_AfterOverloadedTypeNameColorColor_BeforeBreak() =>
            VerifyKeyword(@"
class Color<T> { }
class Color { }
class C
{
    const Color Color = null;
    void M() { switch (new object()) { case Color $$ break; } }
}");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForSwitchCase_SemanticCheck_AfterOverloadedTypeNameColorColor_BeforeWhen() =>
            VerifyKeyword(@"
class Color<T> { }
class Color { }
class C
{
    const Color Color = null;
    void M() { switch (new object()) { case Color $$ when } }
}");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForSwitchCase_SemanticCheck_AfterLocalConstant() =>
            VerifyKeyword(AddInsideMethod(@"const object c = null; switch (new object()) { case c $$ }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForSwitchCase_SemanticCheck_AfterLocalConstant_BeforeBreak() =>
            VerifyKeyword(AddInsideMethod(@"const object c = null; switch (new object()) { case c $$ break; }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForSwitchCase_SemanticCheck_AfterLocalConstant_BeforeWhen() =>
            VerifyKeyword(AddInsideMethod(@"const object c = null; switch (new object()) { case c $$ when }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForSwitchCase_SemanticCheck_AfterUnknownName() =>
            VerifyKeyword(AddInsideMethod(@"switch (new object()) { case unknown $$ }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForSwitchCase_SemanticCheck_AfterUnknownName_BeforeBreak() =>
            VerifyKeyword(AddInsideMethod(@"switch (new object()) { case unknown $$ break; }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForSwitchCase_SemanticCheck_AfterUnknownName_BeforeWhen() =>
            VerifyKeyword(AddInsideMethod(@"switch (new object()) { case unknown $$ when }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForSwitchCase_SemanticCheck_NotAfterVar() =>
            VerifyAbsence(AddInsideMethod(@"switch (new object()) { case var $$ }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForSwitchCase_SemanticCheck_NotAfterVar_BeforeBreak() =>
            VerifyAbsence(AddInsideMethod(@"switch (new object()) { case var $$ break; }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForSwitchCase_SemanticCheck_NotAfterVar_BeforeWhen() =>
            VerifyAbsence(AddInsideMethod(@"switch (new object()) { case var $$ when }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForSwitchCase_SemanticCheck_NotAfterClassVar() =>
            VerifyAbsence(@"
class var { }
class C
{
    void M() { switch (new object()) { case var $$ } }
}");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForSwitchCase_SemanticCheck_NotAfterClassVar_BeforeBreak() =>
            VerifyAbsence(@"
class var { }
class C
{
    void M() { switch (new object()) { case var $$ break; } }
}");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForSwitchCase_SemanticCheck_NotAfterClassVar_BeforeWhen() =>
            VerifyAbsence(@"
class var { }
class C
{
    void M() { switch (new object()) { case var $$ when } }
}");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForSwitchCase_SemanticCheck_NotAfterTypeAliasVar()
        {
            VerifyAbsence(@"
using var = System.String;
class C
{
    void M() { switch (new object()) { case var $$ } }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForSwitchCase_SemanticCheck_NotAfterTypeAliasVar_BeforeBreak()
        {
            VerifyAbsence(@"
using var = System.String;
class C
{
    void M() { switch (new object()) { case var $$ break; } }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForSwitchCase_SemanticCheck_NotAfterTypeAliasVar_BeforeWhen()
        {
            VerifyAbsence(@"
using var = System.String;
class C
{
    void M() { switch (new object()) { case var $$ when } }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForSwitchCase_SemanticCheck_AfterLocalConstantVar() =>
            VerifyAbsence(AddInsideMethod(@"const object var = null; switch (new object()) { case var $$ }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForSwitchCase_SemanticCheck_AfterLocalConstantVar_BeforeBreak() =>
            VerifyAbsence(AddInsideMethod(@"const object var = null; switch (new object()) { case var $$ break; }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForSwitchCase_SemanticCheck_AfterLocalConstantVar_BeforeWhen() =>
            VerifyAbsence(AddInsideMethod(@"const object var = null; switch (new object()) { case var $$ when }"));

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForSwitchCase_SemanticCheck_AfterClassAndLocalConstantVar() =>
            VerifyAbsence(@"
class var { }
class C
{
    void M() { const object var = null; switch (new object()) { case var $$ } }
}");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForSwitchCase_SemanticCheck_AfterClassAndLocalConstantVar_BeforeBreak() =>
    VerifyAbsence(@"
class var { }
class C
{
    void M() { const object var = null; switch (new object()) { case var $$ break; } }
}");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForSwitchCase_SemanticCheck_AfterClassAndLocalConstantVar_BeforeWhen() =>
    VerifyAbsence(@"
class var { }
class C
{
    void M() { const object var = null; switch (new object()) { case var $$ when } }
}");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForSwitchCase_SemanticCheck_AfterTypeAliasAndFieldConstantVar()
        {
            VerifyAbsence(@"
using var = System.String;
class C
{
    const object var = null;
    void M() { switch (new object()) { case var $$ } }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForSwitchCase_SemanticCheck_AfterTypeAliasAndFieldConstantVar_BeforeBreak()
        {
            VerifyAbsence(@"
using var = System.String;
class C
{
    const object var = null;
    void M() { switch (new object()) { case var $$ break; } }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestForSwitchCase_SemanticCheck_AfterTypeAliasAndFieldConstantVar_BeforeWhen()
        {
            VerifyAbsence(@"
using var = System.String;
class C
{
    const object var = null;
    void M() { switch (new object()) { case var $$ when } }
}");
        }
    }
}
