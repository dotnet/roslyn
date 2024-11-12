// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.IntelliSense.CompletionSetSources;

[Trait(Traits.Feature, Traits.Features.Completion)]
public class TypeContextTests : AbstractContextTests
{
    protected override void CheckResult(bool validLocation, int position, SyntaxTree syntaxTree)
        => Assert.Equal(validLocation, syntaxTree.IsTypeContext(position, CancellationToken.None));

    [Fact]
    public void EmptyFile()
        => VerifyTrue(@"$$");

    [Fact]
    public void UsingDirective()
        => VerifyFalse(@"using $$");

    [Fact]
    public void InactiveRegion()
    {
        VerifyFalse("""
            #if false 
            $$
            #endif
            """);
    }

    [Fact]
    public void SingleLineComment1()
        => VerifyFalse(@"// $$");

    [Fact]
    public void SingleLineComment2()
    {
        VerifyTrue("""
            class C { 
            //
            $$
            """);
    }

    [Fact]
    public void MultiLineComment()
        => VerifyFalse(@"/*  $$   */");

    [Fact]
    public void SingleLineXmlComment()
        => VerifyFalse(@"/// $$");

    [Fact]
    public void MultiLineXmlComment()
        => VerifyFalse(@"/**  $$   */");

    [Fact]
    public void OpenStringLiteral()
        => VerifyFalse(AddInsideMethod("string s = \"$$"));

    [Fact]
    public void StringLiteral()
        => VerifyFalse(AddInsideMethod("string s = \"$$\";"));

    [Fact]
    public void OpenCharLiteral()
        => VerifyFalse(AddInsideMethod("char c = '$$"));

    [Fact]
    public void AssemblyAttribute()
        => VerifyTrue(@"[assembly: $$]");

    [Fact]
    public void TypeAttribute()
    {
        VerifyTrue("""
            [$$]
            class CL {}
            """);
    }

    [Fact]
    public void TypeParamAttribute()
        => VerifyTrue(@"class CL<[A$$]T> {}");

    [Fact]
    public void MethodAttribute()
    {
        VerifyTrue("""
            class CL {
                [$$]
                void Method() {}
            }
            """);
    }

    [Fact]
    public void MethodTypeParamAttribute()
    {
        VerifyTrue("""
            class CL{
                void Method<[A$$]T> () {}
            }
            """);
    }

    [Fact]
    public void MethodParamAttribute()
    {
        VerifyTrue("""
            class CL{
                void Method ([$$]int i) {}
            }
            """);
    }

    [Fact]
    public void NamespaceName()
        => VerifyFalse(@"namespace $$");

    [Fact]
    public void UnderNamespace()
        => VerifyFalse(@"namespace NS { $$");

    [Fact]
    public void OutsideOfType()
    {
        VerifyFalse("""
            namespace NS {
            class CL {}
            $$
            """);
    }

    [Fact]
    public void AfterDot()
        => VerifyFalse(@"[assembly: A.$$");

    [Fact]
    public void UsingAlias()
        => VerifyTrue(@"using MyType = $$");

    [Fact]
    public void IncompleteMember()
    {
        VerifyTrue("""
            class CL {
                $$
            """);
    }

    [Fact]
    public void IncompleteMemberAccessibility()
    {
        VerifyTrue("""
            class CL {
                public $$
            """);
    }

    [Fact]
    public void BadStatement()
        => VerifyTrue(AddInsideMethod(@"var t = $$)c"));

    [Fact]
    public void TypeTypeParameter()
        => VerifyFalse(@"class CL<$$");

    [Fact]
    public void TypeTypeParameterList()
        => VerifyFalse(@"class CL<T, $$");

    [Fact]
    public void CastExpressionTypePart()
        => VerifyTrue(AddInsideMethod(@"var t = ($$)c"));

    [Fact]
    public void ObjectCreationExpression()
        => VerifyTrue(AddInsideMethod(@"var t = new $$"));

    [Fact]
    public void ArrayCreationExpression()
        => VerifyTrue(AddInsideMethod(@"var t = new $$ ["));

    [Fact]
    public void StackAllocArrayCreationExpression()
        => VerifyTrue(AddInsideMethod(@"var t = stackalloc $$"));

    [Fact]
    public void FromClauseTypeOptPart()
        => VerifyTrue(AddInsideMethod(@"var t = from $$ c"));

    [Fact]
    public void JoinClause()
        => VerifyTrue(AddInsideMethod(@"var t = from c in C join $$ j"));

    [Fact]
    public void DeclarationStatement()
        => VerifyTrue(AddInsideMethod(@"$$ i ="));

    [Fact]
    public void VariableDeclaration()
        => VerifyTrue(AddInsideMethod(@"fixed($$"));

    [Fact]
    public void ForEachStatement()
        => VerifyTrue(AddInsideMethod(@"foreach($$"));

    [Fact]
    public void ForEachStatementNoToken()
        => VerifyFalse(AddInsideMethod(@"foreach $$"));

    [Fact]
    public void CatchDeclaration()
        => VerifyTrue(AddInsideMethod(@"try {} catch($$"));

    [Fact]
    public void FieldDeclaration()
    {
        VerifyTrue("""
            class CL {
                $$ i
            """);
    }

    [Fact]
    public void EventFieldDeclaration()
    {
        VerifyTrue("""
            class CL {
                event $$
            """);
    }

    [Fact]
    public void ConversionOperatorDeclaration()
    {
        VerifyTrue("""
            class CL {
                explicit operator $$
            """);
    }

    [Fact]
    public void ConversionOperatorDeclarationNoToken()
    {
        VerifyFalse("""
            class CL {
                explicit $$
            """);
    }

    [Fact]
    public void PropertyDeclaration()
    {
        VerifyTrue("""
            class CL {
                $$ Prop {
            """);
    }

    [Fact]
    public void EventDeclaration()
    {
        VerifyTrue("""
            class CL {
                event $$ Event {
            """);
    }

    [Fact]
    public void IndexerDeclaration()
    {
        VerifyTrue("""
            class CL {
                $$ this
            """);
    }

    [Fact]
    public void Parameter()
    {
        VerifyTrue("""
            class CL {
                void Method($$
            """);
    }

    [Fact]
    public void ArrayType()
    {
        VerifyTrue("""
            class CL {
                $$ [
            """);
    }

    [Fact]
    public void PointerType()
    {
        VerifyTrue("""
            class CL {
                $$ *
            """);
    }

    [Fact]
    public void NullableType()
    {
        VerifyTrue("""
            class CL {
                $$ ?
            """);
    }

    [Fact]
    public void DelegateDeclaration()
    {
        VerifyTrue("""
            class CL {
                delegate $$
            """);
    }

    [Fact]
    public void MethodDeclaration()
    {
        VerifyTrue("""
            class CL {
                $$ M(
            """);
    }

    [Fact]
    public void OperatorDeclaration()
    {
        VerifyTrue("""
            class CL {
                $$ operator
            """);
    }

    [Fact]
    public void ParenthesizedExpression()
        => VerifyTrue(AddInsideMethod(@"($$"));

    [Fact]
    public void InvocationExpression()
        => VerifyTrue(AddInsideMethod(@"$$("));

    [Fact]
    public void ElementAccessExpression()
        => VerifyTrue(AddInsideMethod(@"$$["));

    [Fact]
    public void Argument()
        => VerifyTrue(AddInsideMethod(@"i[$$"));

    [Fact]
    public void CastExpressionExpressionPart()
        => VerifyTrue(AddInsideMethod(@"(c)$$"));

    [Fact]
    public void FromClauseInPart()
        => VerifyTrue(AddInsideMethod(@"var t = from c in $$"));

    [Fact]
    public void LetClauseExpressionPart()
        => VerifyTrue(AddInsideMethod(@"var t = from c in C let n = $$"));

    [Fact]
    public void OrderingExpressionPart()
        => VerifyTrue(AddInsideMethod(@"var t = from c in C orderby $$"));

    [Fact]
    public void SelectClauseExpressionPart()
        => VerifyTrue(AddInsideMethod(@"var t = from c in C select $$"));

    [Fact]
    public void ExpressionStatement()
        => VerifyTrue(AddInsideMethod(@"$$"));

    [Fact]
    public void ReturnStatement()
        => VerifyTrue(AddInsideMethod(@"return $$"));

    [Fact]
    public void ThrowStatement()
        => VerifyTrue(AddInsideMethod(@"throw $$"));

    [Fact]
    public void YieldReturnStatement()
        => VerifyTrue(AddInsideMethod(@"yield return $$"));

    [Fact]
    public void ForEachStatementExpressionPart()
        => VerifyTrue(AddInsideMethod(@"foreach(T t in $$"));

    [Fact]
    public void UsingStatementExpressionPart()
        => VerifyTrue(AddInsideMethod(@"using($$"));

    [Fact]
    public void LockStatement()
        => VerifyTrue(AddInsideMethod(@"lock($$"));

    [Fact]
    public void EqualsValueClause()
        => VerifyTrue(AddInsideMethod(@"var i = $$"));

    [Fact]
    public void ForStatementInitializersPart()
        => VerifyTrue(AddInsideMethod(@"for($$"));

    [Fact]
    public void ForStatementConditionOptPart()
        => VerifyTrue(AddInsideMethod(@"for(i=0;$$"));

    [Fact]
    public void ForStatementIncrementorsPart()
        => VerifyTrue(AddInsideMethod(@"for(i=0;i>10;$$"));

    [Fact]
    public void DoStatementConditionPart()
        => VerifyTrue(AddInsideMethod(@"do {} while($$"));

    [Fact]
    public void WhileStatementConditionPart()
        => VerifyTrue(AddInsideMethod(@"while($$"));

    [Fact]
    public void ArrayRankSpecifierSizesPart()
        => VerifyTrue(AddInsideMethod(@"int [$$"));

    [Fact]
    public void PrefixUnaryExpression()
        => VerifyTrue(AddInsideMethod(@"+$$"));

    [Fact]
    public void PostfixUnaryExpression()
        => VerifyTrue(AddInsideMethod(@"$$++"));

    [Fact]
    public void BinaryExpressionLeftPart()
        => VerifyTrue(AddInsideMethod(@"$$ + 1"));

    [Fact]
    public void BinaryExpressionRightPart()
        => VerifyTrue(AddInsideMethod(@"1 + $$"));

    [Fact]
    public void AssignmentExpressionLeftPart()
        => VerifyTrue(AddInsideMethod(@"$$ = 1"));

    [Fact]
    public void AssignmentExpressionRightPart()
        => VerifyTrue(AddInsideMethod(@"1 = $$"));

    [Fact]
    public void ConditionalExpressionConditionPart()
        => VerifyTrue(AddInsideMethod(@"$$? 1:"));

    [Fact]
    public void ConditionalExpressionWhenTruePart()
        => VerifyTrue(AddInsideMethod(@"true? $$:"));

    [Fact]
    public void ConditionalExpressionWhenFalsePart()
        => VerifyTrue(AddInsideMethod(@"true? 1:$$"));

    [Fact]
    public void JoinClauseInExpressionPart()
        => VerifyTrue(AddInsideMethod(@"var t = from c in C join p in $$"));

    [Fact]
    public void JoinClauseLeftExpressionPart()
        => VerifyTrue(AddInsideMethod(@"var t = from c in C join p in P on $$"));

    [Fact]
    public void JoinClauseRightExpressionPart()
        => VerifyTrue(AddInsideMethod(@"var t = from c in C join p in P on id equals $$"));

    [Fact]
    public void WhereClauseConditionPart()
        => VerifyTrue(AddInsideMethod(@"var t = from c in C where $$"));

    [Fact]
    public void GroupClauseGroupExpressionPart()
        => VerifyTrue(AddInsideMethod(@"var t = from c in C group $$"));

    [Fact]
    public void GroupClauseByExpressionPart()
        => VerifyTrue(AddInsideMethod(@"var t = from c in C group g by $$"));

    [Fact]
    public void IfStatement()
        => VerifyTrue(AddInsideMethod(@"if ($$"));

    [Fact]
    public void SwitchStatement()
        => VerifyTrue(AddInsideMethod(@"switch($$"));

    [Fact]
    public void SwitchLabelCase()
    {
        VerifyTrue(AddInsideMethod("""
            switch(i)
                {
                    case $$
            """));
    }

    [Fact]
    public void InitializerExpression()
        => VerifyTrue(AddInsideMethod(@"var t = new [] { $$"));

    [Fact]
    public void TypeParameterConstraintClause()
        => VerifyTrue(@"class CL<T> where T : $$");

    [Fact]
    public void TypeParameterConstraintClauseList()
        => VerifyTrue(@"class CL<T> where T : A, $$");

    [Fact]
    public void TypeParameterConstraintClauseAnotherWhere()
        => VerifyFalse(@"class CL<T> where T : A where$$");

    [Fact]
    public void BaseList1()
        => VerifyTrue(@"class CL : $$");

    [Fact]
    public void BaseList2()
        => VerifyTrue(@"class CL : B, $$");

    [Fact]
    public void BaseListWhere()
        => VerifyFalse(@"class CL<T> : B where$$");

    [Fact]
    public void AliasedName()
        => VerifyFalse(AddInsideMethod(@"global::$$"));

    [Fact]
    public void ConstructorInitializer()
        => VerifyFalse(@"class C { C() : $$");

    [Fact]
    public void ExplicitInterfaceImplementationGeneric1()
        => VerifyFalse(@"class C { void IGoo<$$");

    [Fact]
    public void ExplicitInterfaceImplementationGenericList1()
        => VerifyFalse(@"class C { void IGoo<T,$$");

    [Fact]
    public void ExplicitInterfaceImplementationGeneric2()
        => VerifyTrue(@"class C { void IGoo<$$>.Method(");

    [Fact]
    public void ExplicitInterfaceImplementationGenericList2()
        => VerifyTrue(@"class C { void IGoo<T,$$>.Method(");

    [Fact]
    public void MemberDeclarationInScript()
        => VerifyOnlyInScript(@"private $$");
}
