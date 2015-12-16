// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class NewKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AtRoot_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script,
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterClass_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script,
@"class C { }
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterGlobalStatement_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script,
@"System.Console.WriteLine();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterGlobalVariableDeclaration_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script,
@"int i = 0;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInUsingAlias()
        {
            VerifyAbsence(
@"using Foo = $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void EmptyStatement()
        {
            VerifyKeyword(AddInsideMethod(
@"$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterNewTypeParameterConstraint()
        {
            VerifyKeyword(
@"class C<T> where T : $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterTypeParameterConstraint2()
        {
            VerifyKeyword(
@"class C<T>
    where T : $$
    where U : U");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterMethodTypeParameterConstraint()
        {
            VerifyKeyword(
@"class C {
    void Foo<T>()
      where T : $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterMethodTypeParameterConstraint2()
        {
            VerifyKeyword(
@"class C {
    void Foo<T>()
      where T : $$
      where U : T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterClassTypeParameterConstraint()
        {
            VerifyKeyword(
@"class C<T> where T : class, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterStructTypeParameterConstraint()
        {
            VerifyAbsence(
@"class C<T> where T : struct, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterSimpleTypeParameterConstraint()
        {
            VerifyKeyword(
@"class C<T> where T : IFoo, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void StartOfExpression()
        {
            VerifyKeyword(AddInsideMethod(
@"var q = $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InParenthesizedExpression()
        {
            VerifyKeyword(AddInsideMethod(
@"var q = ($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void PlusEquals()
        {
            VerifyKeyword(AddInsideMethod(
@"q += $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void MinusEquals()
        {
            VerifyKeyword(AddInsideMethod(
@"q -= $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TimesEquals()
        {
            VerifyKeyword(AddInsideMethod(
@"q *= $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void DivideEquals()
        {
            VerifyKeyword(AddInsideMethod(
@"q /= $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void ModEquals()
        {
            VerifyKeyword(AddInsideMethod(
@"q %= $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void XorEquals()
        {
            VerifyKeyword(AddInsideMethod(
@"q ^= $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AndEquals()
        {
            VerifyKeyword(AddInsideMethod(
@"q &= $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void OrEquals()
        {
            VerifyKeyword(AddInsideMethod(
@"q |= $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void LeftShiftEquals()
        {
            VerifyKeyword(AddInsideMethod(
@"q <<= $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void RightShiftEquals()
        {
            VerifyKeyword(AddInsideMethod(
@"q >>= $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterMinus()
        {
            VerifyKeyword(AddInsideMethod(
@"- $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterPlus()
        {
            VerifyKeyword(AddInsideMethod(
@"+ $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterNot()
        {
            VerifyKeyword(AddInsideMethod(
@"! $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterTilde()
        {
            VerifyKeyword(AddInsideMethod(
@"~ $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterBinaryTimes()
        {
            VerifyKeyword(AddInsideMethod(
@"a * $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterBinaryDivide()
        {
            VerifyKeyword(AddInsideMethod(
@"a / $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterBinaryMod()
        {
            VerifyKeyword(AddInsideMethod(
@"a % $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterBinaryPlus()
        {
            VerifyKeyword(AddInsideMethod(
@"a + $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterBinaryMinus()
        {
            VerifyKeyword(AddInsideMethod(
@"a - $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterBinaryLeftShift()
        {
            VerifyKeyword(AddInsideMethod(
@"a << $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterBinaryRightShift()
        {
            VerifyKeyword(AddInsideMethod(
@"a >> $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterBinaryLessThan()
        {
            VerifyKeyword(AddInsideMethod(
@"a < $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterBinaryGreaterThan()
        {
            VerifyKeyword(AddInsideMethod(
@"a > $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterEqualsEquals()
        {
            VerifyKeyword(AddInsideMethod(
@"a == $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterNotEquals()
        {
            VerifyKeyword(AddInsideMethod(
@"a != $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterLessThanEquals()
        {
            VerifyKeyword(AddInsideMethod(
@"a <= $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterGreaterThanEquals()
        {
            VerifyKeyword(AddInsideMethod(
@"a >= $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterNullable()
        {
            VerifyKeyword(AddInsideMethod(
@"a ?? $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterArrayRankSpecifier1()
        {
            VerifyKeyword(AddInsideMethod(
@"new int[ $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterArrayRankSpecifier2()
        {
            VerifyKeyword(AddInsideMethod(
@"new int[expr, $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterConditional1()
        {
            VerifyKeyword(AddInsideMethod(
@"a ? $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterConditional2()
        {
            VerifyKeyword(AddInsideMethod(
@"a ? expr | $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InArgument1()
        {
            VerifyKeyword(AddInsideMethod(
@"Foo( $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InArgument2()
        {
            VerifyKeyword(AddInsideMethod(
@"Foo(expr, $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InArgument3()
        {
            VerifyKeyword(AddInsideMethod(
@"new Foo( $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InArgument4()
        {
            VerifyKeyword(AddInsideMethod(
@"new Foo(expr, $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterRef()
        {
            VerifyKeyword(AddInsideMethod(
@"Foo(ref $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterOut()
        {
            VerifyKeyword(AddInsideMethod(
@"Foo(out $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void Lambda()
        {
            VerifyKeyword(AddInsideMethod(
@"Action<int> a = i => $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InCollectionInitializer1()
        {
            VerifyKeyword(AddInsideMethod(
@"new System.Collections.Generic.List<int>() { $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InCollectionInitializer2()
        {
            VerifyKeyword(AddInsideMethod(
@"new System.Collections.Generic.List<int>() { expr, $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InForeachIn()
        {
            VerifyKeyword(AddInsideMethod(
@"foreach (var v in $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InFromIn()
        {
            VerifyKeyword(AddInsideMethod(
@"var q = from x in $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InJoinIn()
        {
            VerifyKeyword(AddInsideMethod(
@"var q = from x in y
          join a in $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InJoinOn()
        {
            VerifyKeyword(AddInsideMethod(
@"var q = from x in y
          join a in b on $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InJoinEquals()
        {
            VerifyKeyword(AddInsideMethod(
@"var q = from x in y
          join a in b on equals $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void Where()
        {
            VerifyKeyword(AddInsideMethod(
@"var q = from x in y
          where $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void Orderby1()
        {
            VerifyKeyword(AddInsideMethod(
@"var q = from x in y
          orderby $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void Orderby2()
        {
            VerifyKeyword(AddInsideMethod(
@"var q = from x in y
          orderby a, $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void Orderby3()
        {
            VerifyKeyword(AddInsideMethod(
@"var q = from x in y
          orderby a ascending, $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterSelect()
        {
            VerifyKeyword(AddInsideMethod(
@"var q = from x in y
          select $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterGroup()
        {
            VerifyKeyword(AddInsideMethod(
@"var q = from x in y
          group $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterGroupBy()
        {
            VerifyKeyword(AddInsideMethod(
@"var q = from x in y
          group expr by $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterReturn()
        {
            VerifyKeyword(AddInsideMethod(
@"return $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterYieldReturn()
        {
            VerifyKeyword(AddInsideMethod(
@"yield return $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterAttributeReturn()
        {
            VerifyAbsence(
@"[return $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterThrow()
        {
            VerifyKeyword(AddInsideMethod(
@"throw $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InWhile()
        {
            VerifyKeyword(AddInsideMethod(
@"while ($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InUsing()
        {
            VerifyKeyword(AddInsideMethod(
@"using ($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InLock()
        {
            VerifyKeyword(AddInsideMethod(
@"lock ($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InIf()
        {
            VerifyKeyword(AddInsideMethod(
@"if ($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InSwitch()
        {
            VerifyKeyword(AddInsideMethod(
@"switch ($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInCompilationUnit()
        {
            VerifyAbsence(SourceCodeKind.Regular, @"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInCompilationUnit_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script, @"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterExtern()
        {
            VerifyAbsence(SourceCodeKind.Regular, @"extern alias Foo;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterExtern_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script, @"extern alias Foo;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterUsing()
        {
            VerifyAbsence(SourceCodeKind.Regular, @"using Foo;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterUsing_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script, @"using Foo;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterNamespace()
        {
            VerifyAbsence(SourceCodeKind.Regular, @"namespace N {}
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterTypeDeclaration()
        {
            VerifyAbsence(SourceCodeKind.Regular, @"class C {}
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterDelegateDeclaration()
        {
            VerifyAbsence(SourceCodeKind.Regular, @"delegate void Foo();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterMethodInClass()
        {
            VerifyKeyword(
@"class C {
  void Foo() {}
  $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterFieldInClass()
        {
            VerifyKeyword(
@"class C {
  int i;
  $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterPropertyInClass()
        {
            VerifyKeyword(
@"class C {
  int i { get; }
  $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotBeforeUsing()
        {
            VerifyAbsence(SourceCodeKind.Regular,
@"$$
using Foo;");
        }

        [WpfFact(Skip = "528041"), Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotBeforeUsing_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"$$
using Foo;");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterAssemblyAttribute()
        {
            VerifyAbsence(SourceCodeKind.Regular, @"[assembly: foo]
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterAssemblyAttribute_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script, @"[assembly: foo]
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterRootAttribute()
        {
            VerifyAbsence(@"[foo]
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterNestedAttribute()
        {
            VerifyKeyword(
@"class C {
  [foo]
  $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InsideStruct()
        {
            VerifyKeyword(
@"struct S {
   $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InsideInterface()
        {
            VerifyKeyword(
@"interface I {
   $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InsideClass()
        {
            VerifyKeyword(
@"class C {
   $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterPartial()
        {
            VerifyAbsence(@"partial $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterAbstract()
        {
            VerifyAbsence(@"abstract $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterInternal()
        {
            VerifyAbsence(SourceCodeKind.Regular, @"internal $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterInternal_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script, @"internal $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterPublic()
        {
            VerifyAbsence(SourceCodeKind.Regular, @"public $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterPublic_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script, @"public $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterStaticInternal()
        {
            VerifyAbsence(SourceCodeKind.Regular, @"static internal $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterStaticInternal_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script, @"static internal $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterInternalStatic()
        {
            VerifyAbsence(SourceCodeKind.Regular, @"internal static $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterInternalStatic_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script, @"internal static $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterInvalidInternal()
        {
            VerifyAbsence(@"virtual internal $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterClass()
        {
            VerifyAbsence(@"class $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterPrivate()
        {
            VerifyAbsence(SourceCodeKind.Regular,
@"private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterPrivate_Script()
        {
            VerifyKeyword(SourceCodeKind.Script,
@"private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterSealed()
        {
            VerifyAbsence(@"sealed $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterStatic()
        {
            VerifyAbsence(SourceCodeKind.Regular, @"static $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterStatic_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script, @"static $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterNestedStatic()
        {
            VerifyKeyword(
@"class C {
    static $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterNestedInternal()
        {
            VerifyKeyword(
@"class C {
    internal $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterNestedPrivate()
        {
            VerifyKeyword(
@"class C {
    private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterDelegate()
        {
            VerifyAbsence(@"delegate $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterNestedAbstract()
        {
            VerifyKeyword(
@"class C {
    abstract $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterNestedVirtual()
        {
            VerifyKeyword(
@"class C {
    virtual $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterNestedNew()
        {
            VerifyAbsence(@"class C {
    new $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterNestedOverride()
        {
            VerifyAbsence(@"class C {
    override $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterNestedSealed()
        {
            VerifyKeyword(
@"class C {
    sealed $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInProperty()
        {
            VerifyAbsence(
@"class C {
    int Foo { $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInPropertyAfterAccessor()
        {
            VerifyAbsence(
@"class C {
    int Foo { get; $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInPropertyAfterAccessibility()
        {
            VerifyAbsence(
@"class C {
    int Foo { get; protected $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInPropertyAfterInternal()
        {
            VerifyAbsence(
@"class C {
    int Foo { get; internal $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterCastType1()
        {
            VerifyKeyword(AddInsideMethod(
@"return (LeafSegment)$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterCastType2()
        {
            VerifyKeyword(AddInsideMethod(
@"return (LeafSegment)(object)$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterParenthesizedExpression()
        {
            VerifyAbsence(AddInsideMethod(
@"return (a + b)$$"));
        }

        [WorkItem(538264)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InConstMemberInitializer1()
        {
            // User could say "new int()" here.
            VerifyKeyword(
@"class E {
    const int a = $$
}");
        }

        [WorkItem(538264)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InConstLocalInitializer1()
        {
            // User could say "new int()" here.
            VerifyKeyword(
@"class E {
  void Foo() {
    const int a = $$
  }
}");
        }

        [WorkItem(538264)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InMemberInitializer1()
        {
            VerifyKeyword(
@"class E {
    int a = $$
}");
        }

        [WorkItem(538804)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInTypeOf()
        {
            VerifyAbsence(AddInsideMethod(
@"typeof($$"));
        }

        [WorkItem(538804)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInDefault()
        {
            VerifyAbsence(AddInsideMethod(
@"default($$"));
        }

        [WorkItem(538804)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInSizeOf()
        {
            VerifyAbsence(AddInsideMethod(
@"sizeof($$"));
        }

        [WorkItem(544219)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInObjectInitializerMemberContext()
        {
            VerifyAbsence(@"
class C
{
    public int x, y;
    void M()
    {
        var c = new C { x = 2, y = 3, $$");
        }

        [WorkItem(544486)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InsideInitOfConstFieldDecl()
        {
            // user could say "new int()" here.
            VerifyKeyword(
@"class C
{
    const int value = $$");
        }

        [WorkItem(544998)]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void InsideStructParameterInitializer()
        {
            VerifyKeyword(
@"struct C
{
    void M(C c = $$
}");
        }
    }
}
