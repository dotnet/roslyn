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
    public class TypeContextTests : AbstractContextTests
    {
        protected override void CheckResult(bool validLocation, int position, SyntaxTree syntaxTree)
        {
            Assert.Equal(validLocation, syntaxTree.IsTypeContext(position, CancellationToken.None));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EmptyFile()
        {
            VerifyFalse(@"$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void UsingDirective()
        {
            VerifyFalse(@"using $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InactiveRegion()
        {
            VerifyFalse(@"#if false 
$$
#endif");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void SingleLineComment1()
        {
            VerifyFalse(@"// $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void SingleLineComment2()
        {
            VerifyTrue(@"class C { 
//
$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void MultiLineComment()
        {
            VerifyFalse(@"/*  $$   */");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void SingleLineXmlComment()
        {
            VerifyFalse(@"/// $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void MultiLineXmlComment()
        {
            VerifyFalse(@"/**  $$   */");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void OpenStringLiteral()
        {
            VerifyFalse(AddInsideMethod("string s = \"$$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void StringLiteral()
        {
            VerifyFalse(AddInsideMethod("string s = \"$$\";"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void OpenCharLiteral()
        {
            VerifyFalse(AddInsideMethod("char c = '$$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AssemblyAttribute()
        {
            VerifyTrue(@"[assembly: $$]");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TypeAttribute()
        {
            VerifyTrue(@"[$$]
class CL {}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TypeParamAttribute()
        {
            VerifyTrue(@"class CL<[A$$]T> {}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void MethodAttribute()
        {
            VerifyTrue(@"class CL {
    [$$]
    void Method() {}
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void MethodTypeParamAttribute()
        {
            VerifyTrue(@"class CL{
    void Method<[A$$]T> () {}
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void MethodParamAttribute()
        {
            VerifyTrue(@"class CL{
    void Method ([$$]int i) {}
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NamespaceName()
        {
            VerifyFalse(@"namespace $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void UnderNamespace()
        {
            VerifyFalse(@"namespace NS { $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void OutsideOfType()
        {
            VerifyFalse(@"namespace NS {
class CL {}
$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AfterDot()
        {
            VerifyFalse(@"[assembly: A.$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void UsingAlias()
        {
            VerifyTrue(@"using MyType = $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void IncompleteMember()
        {
            VerifyTrue(@"class CL {
    $$
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void IncompleteMemberAccessibility()
        {
            VerifyTrue(@"class CL {
    public $$
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void BadStatement()
        {
            VerifyTrue(AddInsideMethod(@"var t = $$)c"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TypeTypeParameter()
        {
            VerifyFalse(@"class CL<$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TypeTypeParameterList()
        {
            VerifyFalse(@"class CL<T, $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CastExpressionTypePart()
        {
            VerifyTrue(AddInsideMethod(@"var t = ($$)c"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ObjectCreationExpression()
        {
            VerifyTrue(AddInsideMethod(@"var t = new $$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ArrayCreationExpression()
        {
            VerifyTrue(AddInsideMethod(@"var t = new $$ ["));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void StackAllocArrayCreationExpression()
        {
            VerifyTrue(AddInsideMethod(@"var t = stackalloc $$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void FromClauseTypeOptPart()
        {
            VerifyTrue(AddInsideMethod(@"var t = from $$ c"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void JoinClause()
        {
            VerifyTrue(AddInsideMethod(@"var t = from c in C join $$ j"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void DeclarationStatement()
        {
            VerifyTrue(AddInsideMethod(@"$$ i ="));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void VariableDeclaration()
        {
            VerifyTrue(AddInsideMethod(@"fixed($$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ForEachStatement()
        {
            VerifyTrue(AddInsideMethod(@"foreach($$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ForEachStatementNoToken()
        {
            VerifyFalse(AddInsideMethod(@"foreach $$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CatchDeclaration()
        {
            VerifyTrue(AddInsideMethod(@"try {} catch($$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void FieldDeclaration()
        {
            VerifyTrue(@"class CL {
    $$ i");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EventFieldDeclaration()
        {
            VerifyTrue(@"class CL {
    event $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ConversionOperatorDeclaration()
        {
            VerifyTrue(@"class CL {
    explicit operator $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ConversionOperatorDeclarationNoToken()
        {
            VerifyFalse(@"class CL {
    explicit $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void PropertyDeclaration()
        {
            VerifyTrue(@"class CL {
    $$ Prop {");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EventDeclaration()
        {
            VerifyTrue(@"class CL {
    event $$ Event {");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void IndexerDeclaration()
        {
            VerifyTrue(@"class CL {
    $$ this");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void Parameter()
        {
            VerifyTrue(@"class CL {
    void Method($$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ArrayType()
        {
            VerifyTrue(@"class CL {
    $$ [");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void PointerType()
        {
            VerifyTrue(@"class CL {
    $$ *");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NullableType()
        {
            VerifyTrue(@"class CL {
    $$ ?");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void DelegateDeclaration()
        {
            VerifyTrue(@"class CL {
    delegate $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void MethodDeclaration()
        {
            VerifyTrue(@"class CL {
    $$ M(");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void OperatorDeclaration()
        {
            VerifyTrue(@"class CL {
    $$ operator");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ParenthesizedExpression()
        {
            VerifyTrue(AddInsideMethod(@"($$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InvocationExpression()
        {
            VerifyTrue(AddInsideMethod(@"$$("));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ElementAccessExpression()
        {
            VerifyTrue(AddInsideMethod(@"$$["));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void Argument()
        {
            VerifyTrue(AddInsideMethod(@"i[$$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CastExpressionExpressionPart()
        {
            VerifyTrue(AddInsideMethod(@"(c)$$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void FromClauseInPart()
        {
            VerifyTrue(AddInsideMethod(@"var t = from c in $$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void LetClauseExpressionPart()
        {
            VerifyTrue(AddInsideMethod(@"var t = from c in C let n = $$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void OrderingExpressionPart()
        {
            VerifyTrue(AddInsideMethod(@"var t = from c in C orderby $$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void SelectClauseExpressionPart()
        {
            VerifyTrue(AddInsideMethod(@"var t = from c in C select $$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ExpressionStatement()
        {
            VerifyTrue(AddInsideMethod(@"$$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ReturnStatement()
        {
            VerifyTrue(AddInsideMethod(@"return $$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ThrowStatement()
        {
            VerifyTrue(AddInsideMethod(@"throw $$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void YieldReturnStatement()
        {
            VerifyTrue(AddInsideMethod(@"yield return $$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ForEachStatementExpressionPart()
        {
            VerifyTrue(AddInsideMethod(@"foreach(T t in $$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void UsingStatementExpressionPart()
        {
            VerifyTrue(AddInsideMethod(@"using($$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void LockStatement()
        {
            VerifyTrue(AddInsideMethod(@"lock($$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EqualsValueClause()
        {
            VerifyTrue(AddInsideMethod(@"var i = $$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ForStatementInitializersPart()
        {
            VerifyTrue(AddInsideMethod(@"for($$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ForStatementConditionOptPart()
        {
            VerifyTrue(AddInsideMethod(@"for(i=0;$$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ForStatementIncrementorsPart()
        {
            VerifyTrue(AddInsideMethod(@"for(i=0;i>10;$$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void DoStatementConditionPart()
        {
            VerifyTrue(AddInsideMethod(@"do {} while($$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void WhileStatementConditionPart()
        {
            VerifyTrue(AddInsideMethod(@"while($$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ArrayRankSpecifierSizesPart()
        {
            VerifyTrue(AddInsideMethod(@"int [$$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void PrefixUnaryExpression()
        {
            VerifyTrue(AddInsideMethod(@"+$$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void PostfixUnaryExpression()
        {
            VerifyTrue(AddInsideMethod(@"$$++"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void BinaryExpressionLeftPart()
        {
            VerifyTrue(AddInsideMethod(@"$$ + 1"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void BinaryExpressionRightPart()
        {
            VerifyTrue(AddInsideMethod(@"1 + $$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AssignmentExpressionLeftPart()
        {
            VerifyTrue(AddInsideMethod(@"$$ = 1"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AssignmentExpressionRightPart()
        {
            VerifyTrue(AddInsideMethod(@"1 = $$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ConditionalExpressionConditionPart()
        {
            VerifyTrue(AddInsideMethod(@"$$? 1:"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ConditionalExpressionWhenTruePart()
        {
            VerifyTrue(AddInsideMethod(@"true? $$:"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ConditionalExpressionWhenFalsePart()
        {
            VerifyTrue(AddInsideMethod(@"true? 1:$$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void JoinClauseInExpressionPart()
        {
            VerifyTrue(AddInsideMethod(@"var t = from c in C join p in $$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void JoinClauseLeftExpressionPart()
        {
            VerifyTrue(AddInsideMethod(@"var t = from c in C join p in P on $$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void JoinClauseRightExpressionPart()
        {
            VerifyTrue(AddInsideMethod(@"var t = from c in C join p in P on id equals $$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void WhereClauseConditionPart()
        {
            VerifyTrue(AddInsideMethod(@"var t = from c in C where $$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void GroupClauseGroupExpressionPart()
        {
            VerifyTrue(AddInsideMethod(@"var t = from c in C group $$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void GroupClauseByExpressionPart()
        {
            VerifyTrue(AddInsideMethod(@"var t = from c in C group g by $$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void IfStatement()
        {
            VerifyTrue(AddInsideMethod(@"if ($$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void SwitchStatement()
        {
            VerifyTrue(AddInsideMethod(@"switch($$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void SwitchLabelCase()
        {
            VerifyTrue(AddInsideMethod(@"switch(i)
    {
        case $$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void InitializerExpression()
        {
            VerifyTrue(AddInsideMethod(@"var t = new [] { $$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TypeParameterConstraintClause()
        {
            VerifyTrue(@"class CL<T> where T : $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TypeParameterConstraintClauseList()
        {
            VerifyTrue(@"class CL<T> where T : A, $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TypeParameterConstraintClauseAnotherWhere()
        {
            VerifyFalse(@"class CL<T> where T : A where$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void BaseList1()
        {
            VerifyTrue(@"class CL : $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void BaseList2()
        {
            VerifyTrue(@"class CL : B, $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void BaseListWhere()
        {
            VerifyFalse(@"class CL<T> : B where$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AliasedName()
        {
            VerifyFalse(AddInsideMethod(@"global::$$"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ConstructorInitializer()
        {
            VerifyFalse(@"class C { C() : $$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ExplicitInterfaceImplementationGeneric1()
        {
            VerifyFalse(@"class C { void IFoo<$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ExplicitInterfaceImplementationGenericList1()
        {
            VerifyFalse(@"class C { void IFoo<T,$$");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ExplicitInterfaceImplementationGeneric2()
        {
            VerifyTrue(@"class C { void IFoo<$$>.Method(");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ExplicitInterfaceImplementationGenericList2()
        {
            VerifyTrue(@"class C { void IFoo<T,$$>.Method(");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void MemberDeclarationInScript()
        {
            VerifyOnlyInScript(@"private $$");
        }
    }
}
