// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.IntelliSense.CompletionSetSources
{
    public class NamespaceContextTests : AbstractContextTests
    {
        protected override void CheckResult(bool validLocation, int position, SyntaxTree syntaxTree)
        {
            Assert.Equal(validLocation, syntaxTree.IsNamespaceContext(position, CancellationToken.None));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EmptyFile()
        {
            VerifyFalse(@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void UsingDirective()
        {
            VerifyTrue(@"using $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InactiveRegion()
        {
            VerifyFalse(@"#if false 
$$
#endif");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void SingleLineComment1()
        {
            VerifyFalse(@"// $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void SingleLineComment2()
        {
            VerifyTrue(@"class C { 
//
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void MultiLineComment()
        {
            VerifyFalse(@"/*  $$   */");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void SingleLineXmlComment()
        {
            VerifyFalse(@"/// $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void MultiLineXmlComment()
        {
            VerifyFalse(@"/**  $$   */");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void OpenStringLiteral()
        {
            VerifyFalse(AddInsideMethod("string s = \"$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void StringLiteral()
        {
            VerifyFalse(AddInsideMethod("string s = \"$$\";"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void OpenCharLiteral()
        {
            VerifyFalse(AddInsideMethod("char c = '$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AssemblyAttribute()
        {
            VerifyTrue(@"[assembly: $$]");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TypeAttribute()
        {
            VerifyTrue(@"[$$]
class CL {}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TypeParamAttribute()
        {
            VerifyTrue(@"class CL<[A$$]T> {}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void MethodAttribute()
        {
            VerifyTrue(@"class CL {
    [$$]
    void Method() {}
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void MethodTypeParamAttribute()
        {
            VerifyTrue(@"class CL{
    void Method<[A$$]T> () {}
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void MethodParamAttribute()
        {
            VerifyTrue(@"class CL{
    void Method ([$$]int i) {}
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NamespaceName()
        {
            VerifyFalse(@"namespace $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void UnderNamespace()
        {
            VerifyFalse(@"namespace NS { $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void OutsideOfType()
        {
            VerifyFalse(@"namespace NS {
class CL {}
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AfterDot()
        {
            VerifyFalse(@"[assembly: A.$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void UsingAlias1()
        {
            VerifyTrue(@"using MyType = $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void UsingAlias2()
        {
            VerifyFalse(@"using $$ = System");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void IncompleteMember()
        {
            VerifyTrue(@"class CL {
    $$
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void IncompleteMemberAccessibility()
        {
            VerifyTrue(@"class CL {
    public $$
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void BadStatement()
        {
            VerifyTrue(AddInsideMethod(@"var t = $$)c"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TypeTypeParameter()
        {
            VerifyFalse(@"class CL<$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TypeTypeParameterList()
        {
            VerifyFalse(@"class CL<T, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CastExpressionTypePart()
        {
            VerifyTrue(AddInsideMethod(@"var t = ($$)c"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ObjectCreationExpression()
        {
            VerifyTrue(AddInsideMethod(@"var t = new $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ArrayCreationExpression()
        {
            VerifyTrue(AddInsideMethod(@"var t = new $$ ["));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void StackAllocArrayCreationExpression()
        {
            VerifyTrue(AddInsideMethod(@"var t = stackalloc $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void FromClauseTypeOptPart()
        {
            VerifyTrue(AddInsideMethod(@"var t = from $$ c"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void JoinClause()
        {
            VerifyTrue(AddInsideMethod(@"var t = from c in C join $$ j"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void DeclarationStatement()
        {
            VerifyTrue(AddInsideMethod(@"$$ i ="));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void FixedVariableDeclaration()
        {
            VerifyTrue(AddInsideMethod(@"fixed($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ForEachStatement()
        {
            VerifyTrue(AddInsideMethod(@"foreach($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ForEachStatementNoToken()
        {
            VerifyFalse(AddInsideMethod(@"foreach $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CatchDeclaration()
        {
            VerifyTrue(AddInsideMethod(@"try {} catch($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void FieldDeclaration()
        {
            VerifyTrue(@"class CL {
    $$ i");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EventFieldDeclaration()
        {
            VerifyTrue(@"class CL {
    event $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ConversionOperatorDeclaration()
        {
            VerifyTrue(@"class CL {
    explicit operator $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ConversionOperatorDeclarationNoToken()
        {
            VerifyFalse(@"class CL {
    explicit $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void PropertyDeclaration()
        {
            VerifyTrue(@"class CL {
    $$ Prop {");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EventDeclaration()
        {
            VerifyTrue(@"class CL {
    event $$ Event {");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void IndexerDeclaration()
        {
            VerifyTrue(@"class CL {
    $$ this");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void Parameter()
        {
            VerifyTrue(@"class CL {
    void Method($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ArrayType()
        {
            VerifyTrue(@"class CL {
    $$ [");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void PointerType()
        {
            VerifyTrue(@"class CL {
    $$ *");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NullableType()
        {
            VerifyTrue(@"class CL {
    $$ ?");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void DelegateDeclaration()
        {
            VerifyTrue(@"class CL {
    delegate $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void MethodDeclaration()
        {
            VerifyTrue(@"class CL {
    $$ M(");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void OperatorDeclaration()
        {
            VerifyTrue(@"class CL {
    $$ operator");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ParenthesizedExpression()
        {
            VerifyTrue(AddInsideMethod(@"($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InvocationExpression()
        {
            VerifyTrue(AddInsideMethod(@"$$("));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ElementAccessExpression()
        {
            VerifyTrue(AddInsideMethod(@"$$["));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void Argument()
        {
            VerifyTrue(AddInsideMethod(@"i[$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CastExpressionExpressionPart()
        {
            VerifyTrue(AddInsideMethod(@"(c)$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void FromClauseInPart()
        {
            VerifyTrue(AddInsideMethod(@"var t = from c in $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void LetClauseExpressionPart()
        {
            VerifyTrue(AddInsideMethod(@"var t = from c in C let n = $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void OrderingExpressionPart()
        {
            VerifyTrue(AddInsideMethod(@"var t = from c in C orderby $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void SelectClauseExpressionPart()
        {
            VerifyTrue(AddInsideMethod(@"var t = from c in C select $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ExpressionStatement()
        {
            VerifyTrue(AddInsideMethod(@"$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ReturnStatement()
        {
            VerifyTrue(AddInsideMethod(@"return $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ThrowStatement()
        {
            VerifyTrue(AddInsideMethod(@"throw $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void YieldReturnStatement()
        {
            VerifyTrue(AddInsideMethod(@"yield return $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ForEachStatementExpressionPart()
        {
            VerifyTrue(AddInsideMethod(@"foreach(T t in $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void UsingStatementExpressionPart()
        {
            VerifyTrue(AddInsideMethod(@"using($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void LockStatement()
        {
            VerifyTrue(AddInsideMethod(@"lock($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EqualsValueClause()
        {
            VerifyTrue(AddInsideMethod(@"var i = $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ForStatementInitializersPart()
        {
            VerifyTrue(AddInsideMethod(@"for($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ForStatementConditionOptPart()
        {
            VerifyTrue(AddInsideMethod(@"for(i=0;$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ForStatementIncrementorsPart()
        {
            VerifyTrue(AddInsideMethod(@"for(i=0;i>10;$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void DoStatementConditionPart()
        {
            VerifyTrue(AddInsideMethod(@"do {} while($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void WhileStatementConditionPart()
        {
            VerifyTrue(AddInsideMethod(@"while($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ArrayRankSpecifierSizesPart()
        {
            VerifyTrue(AddInsideMethod(@"int [$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void PrefixUnaryExpression()
        {
            VerifyTrue(AddInsideMethod(@"+$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void PostfixUnaryExpression()
        {
            VerifyTrue(AddInsideMethod(@"$$++"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void BinaryExpressionLeftPart()
        {
            VerifyTrue(AddInsideMethod(@"$$ + 1"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void BinaryExpressionRightPart()
        {
            VerifyTrue(AddInsideMethod(@"1 + $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AssignmentExpressionLeftPart()
        {
            VerifyTrue(AddInsideMethod(@"$$ = 1"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AssignmentExpressionRightPart()
        {
            VerifyTrue(AddInsideMethod(@"1 = $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ConditionalExpressionConditionPart()
        {
            VerifyTrue(AddInsideMethod(@"$$? 1:"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ConditionalExpressionWhenTruePart()
        {
            VerifyTrue(AddInsideMethod(@"true? $$:"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ConditionalExpressionWhenFalsePart()
        {
            VerifyTrue(AddInsideMethod(@"true? 1:$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void JoinClauseInExpressionPart()
        {
            VerifyTrue(AddInsideMethod(@"var t = from c in C join p in $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void JoinClauseLeftExpressionPart()
        {
            VerifyTrue(AddInsideMethod(@"var t = from c in C join p in P on $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void JoinClauseRightExpressionPart()
        {
            VerifyTrue(AddInsideMethod(@"var t = from c in C join p in P on id equals $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void WhereClauseConditionPart()
        {
            VerifyTrue(AddInsideMethod(@"var t = from c in C where $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void GroupClauseGroupExpressionPart()
        {
            VerifyTrue(AddInsideMethod(@"var t = from c in C group $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void GroupClauseByExpressionPart()
        {
            VerifyTrue(AddInsideMethod(@"var t = from c in C group g by $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void IfStatement()
        {
            VerifyTrue(AddInsideMethod(@"if ($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void SwitchStatement()
        {
            VerifyTrue(AddInsideMethod(@"switch($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void SwitchLabelCase()
        {
            VerifyTrue(AddInsideMethod(@"switch(i)
    {
        case $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InitializerExpression()
        {
            VerifyTrue(AddInsideMethod(@"var t = new [] { $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TypeParameterConstraintClause()
        {
            VerifyTrue(@"class CL<T> where T : $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TypeParameterConstraintClauseList()
        {
            VerifyTrue(@"class CL<T> where T : A, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TypeParameterConstraintClauseAnotherWhere()
        {
            VerifyFalse(@"class CL<T> where T : A where$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void BaseList1()
        {
            VerifyTrue(@"class CL : $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void BaseList2()
        {
            VerifyTrue(@"class CL : B, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void BaseListWhere()
        {
            VerifyFalse(@"class CL<T> : B where$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AliasedName()
        {
            VerifyTrue(AddInsideMethod(@"global::$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ConstructorInitializer()
        {
            VerifyFalse(@"class C { C() : $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ExplicitInterfaceImplementationGeneric1()
        {
            VerifyFalse(@"class C { void IFoo<$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ExplicitInterfaceImplementationGenericList1()
        {
            VerifyFalse(@"class C { void IFoo<T,$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ExplicitInterfaceImplementationGeneric2()
        {
            VerifyTrue(@"class C { void IFoo<$$>.Method(");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ExplicitInterfaceImplementationGenericList2()
        {
            VerifyTrue(@"class C { void IFoo<T,$$>.Method(");
        }
    }
}
