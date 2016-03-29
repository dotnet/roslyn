// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class NewKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAtRoot_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterClass_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
@"class C { }
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterGlobalStatement_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
@"System.Console.WriteLine();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterGlobalVariableDeclaration_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
@"int i = 0;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInUsingAlias()
        {
            await VerifyAbsenceAsync(
@"using Foo = $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestEmptyStatement()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNewTypeParameterConstraint()
        {
            await VerifyKeywordAsync(
@"class C<T> where T : $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterTypeParameterConstraint2()
        {
            await VerifyKeywordAsync(
@"class C<T>
    where T : $$
    where U : U");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterMethodTypeParameterConstraint()
        {
            await VerifyKeywordAsync(
@"class C {
    void Foo<T>()
      where T : $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterMethodTypeParameterConstraint2()
        {
            await VerifyKeywordAsync(
@"class C {
    void Foo<T>()
      where T : $$
      where U : T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterClassTypeParameterConstraint()
        {
            await VerifyKeywordAsync(
@"class C<T> where T : class, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterStructTypeParameterConstraint()
        {
            await VerifyAbsenceAsync(
@"class C<T> where T : struct, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterSimpleTypeParameterConstraint()
        {
            await VerifyKeywordAsync(
@"class C<T> where T : IFoo, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestStartOfExpression()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInParenthesizedExpression()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = ($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestPlusEquals()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"q += $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestMinusEquals()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"q -= $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestTimesEquals()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"q *= $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestDivideEquals()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"q /= $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestModEquals()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"q %= $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestXorEquals()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"q ^= $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAndEquals()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"q &= $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestOrEquals()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"q |= $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestLeftShiftEquals()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"q <<= $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestRightShiftEquals()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"q >>= $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterMinus()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"- $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPlus()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"+ $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNot()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"! $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterTilde()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"~ $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterBinaryTimes()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"a * $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterBinaryDivide()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"a / $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterBinaryMod()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"a % $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterBinaryPlus()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"a + $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterBinaryMinus()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"a - $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterBinaryLeftShift()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"a << $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterBinaryRightShift()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"a >> $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterBinaryLessThan()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"a < $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterBinaryGreaterThan()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"a > $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterEqualsEquals()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"a == $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNotEquals()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"a != $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterLessThanEquals()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"a <= $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterGreaterThanEquals()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"a >= $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNullable()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"a ?? $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterArrayRankSpecifier1()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"new int[ $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterArrayRankSpecifier2()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"new int[expr, $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterConditional1()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"a ? $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterConditional2()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"a ? expr | $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInArgument1()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"Foo( $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInArgument2()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"Foo(expr, $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInArgument3()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"new Foo( $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInArgument4()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"new Foo(expr, $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterRef()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"Foo(ref $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterOut()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"Foo(out $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestLambda()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"Action<int> a = i => $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInCollectionInitializer1()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"new System.Collections.Generic.List<int>() { $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInCollectionInitializer2()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"new System.Collections.Generic.List<int>() { expr, $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInForeachIn()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"foreach (var v in $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInFromIn()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = from x in $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInJoinIn()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = from x in y
          join a in $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInJoinOn()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = from x in y
          join a in b on $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInJoinEquals()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = from x in y
          join a in b on equals $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestWhere()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = from x in y
          where $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestOrderby1()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = from x in y
          orderby $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestOrderby2()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = from x in y
          orderby a, $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestOrderby3()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = from x in y
          orderby a ascending, $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterSelect()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = from x in y
          select $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterGroup()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = from x in y
          group $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterGroupBy()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = from x in y
          group expr by $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterReturn()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"return $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterYieldReturn()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"yield return $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterAttributeReturn()
        {
            await VerifyAbsenceAsync(
@"[return $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterThrow()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"throw $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInWhile()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"while ($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInUsing()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"using ($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInLock()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"lock ($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInIf()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"if ($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInSwitch()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"switch ($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInCompilationUnit()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Regular, @"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInCompilationUnit_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script, @"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterExtern()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Regular, @"extern alias Foo;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterExtern_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script, @"extern alias Foo;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterUsing()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Regular, @"using Foo;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterUsing_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script, @"using Foo;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterNamespace()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Regular, @"namespace N {}
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterTypeDeclaration()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Regular, @"class C {}
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterDelegateDeclaration()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Regular, @"delegate void Foo();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterMethodInClass()
        {
            await VerifyKeywordAsync(
@"class C {
  void Foo() {}
  $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterFieldInClass()
        {
            await VerifyKeywordAsync(
@"class C {
  int i;
  $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPropertyInClass()
        {
            await VerifyKeywordAsync(
@"class C {
  int i { get; }
  $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotBeforeUsing()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Regular,
@"$$
using Foo;");
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/9880"), Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotBeforeUsing_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"$$
using Foo;");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterAssemblyAttribute()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Regular, @"[assembly: foo]
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterAssemblyAttribute_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script, @"[assembly: foo]
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterRootAttribute()
        {
            await VerifyAbsenceAsync(@"[foo]
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedAttribute()
        {
            await VerifyKeywordAsync(
@"class C {
  [foo]
  $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInsideStruct()
        {
            await VerifyKeywordAsync(
@"struct S {
   $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInsideInterface()
        {
            await VerifyKeywordAsync(
@"interface I {
   $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInsideClass()
        {
            await VerifyKeywordAsync(
@"class C {
   $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterPartial()
        {
            await VerifyAbsenceAsync(@"partial $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterAbstract()
        {
            await VerifyAbsenceAsync(@"abstract $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterInternal()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Regular, @"internal $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterInternal_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script, @"internal $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterPublic()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Regular, @"public $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPublic_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script, @"public $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterStaticInternal()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Regular, @"static internal $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterStaticInternal_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script, @"static internal $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterInternalStatic()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Regular, @"internal static $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterInternalStatic_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script, @"internal static $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterInvalidInternal()
        {
            await VerifyAbsenceAsync(@"virtual internal $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterClass()
        {
            await VerifyAbsenceAsync(@"class $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterPrivate()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Regular,
@"private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPrivate_Script()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
@"private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterSealed()
        {
            await VerifyAbsenceAsync(@"sealed $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterStatic()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Regular, @"static $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterStatic_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script, @"static $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedStatic()
        {
            await VerifyKeywordAsync(
@"class C {
    static $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedInternal()
        {
            await VerifyKeywordAsync(
@"class C {
    internal $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedPrivate()
        {
            await VerifyKeywordAsync(
@"class C {
    private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterDelegate()
        {
            await VerifyAbsenceAsync(@"delegate $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedAbstract()
        {
            await VerifyKeywordAsync(
@"class C {
    abstract $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedVirtual()
        {
            await VerifyKeywordAsync(
@"class C {
    virtual $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterNestedNew()
        {
            await VerifyAbsenceAsync(@"class C {
    new $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterNestedOverride()
        {
            await VerifyAbsenceAsync(@"class C {
    override $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedSealed()
        {
            await VerifyKeywordAsync(
@"class C {
    sealed $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInProperty()
        {
            await VerifyAbsenceAsync(
@"class C {
    int Foo { $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInPropertyAfterAccessor()
        {
            await VerifyAbsenceAsync(
@"class C {
    int Foo { get; $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInPropertyAfterAccessibility()
        {
            await VerifyAbsenceAsync(
@"class C {
    int Foo { get; protected $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInPropertyAfterInternal()
        {
            await VerifyAbsenceAsync(
@"class C {
    int Foo { get; internal $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterCastType1()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"return (LeafSegment)$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterCastType2()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"return (LeafSegment)(object)$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterParenthesizedExpression()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"return (a + b)$$"));
        }

        [WorkItem(538264, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538264")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInConstMemberInitializer1()
        {
            // User could say "new int()" here.
            await VerifyKeywordAsync(
@"class E {
    const int a = $$
}");
        }

        [WorkItem(538264, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538264")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInConstLocalInitializer1()
        {
            // User could say "new int()" here.
            await VerifyKeywordAsync(
@"class E {
  void Foo() {
    const int a = $$
  }
}");
        }

        [WorkItem(538264, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538264")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInMemberInitializer1()
        {
            await VerifyKeywordAsync(
@"class E {
    int a = $$
}");
        }

        [WorkItem(538804, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInTypeOf()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"typeof($$"));
        }

        [WorkItem(538804, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInDefault()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"default($$"));
        }

        [WorkItem(538804, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInSizeOf()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"sizeof($$"));
        }

        [WorkItem(544219, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544219")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInObjectInitializerMemberContext()
        {
            await VerifyAbsenceAsync(@"
class C
{
    public int x, y;
    void M()
    {
        var c = new C { x = 2, y = 3, $$");
        }

        [WorkItem(544486, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544486")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInsideInitOfConstFieldDecl()
        {
            // user could say "new int()" here.
            await VerifyKeywordAsync(
@"class C
{
    const int value = $$");
        }

        [WorkItem(544998, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544998")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInsideStructParameterInitializer()
        {
            await VerifyKeywordAsync(
@"struct C
{
    void M(C c = $$
}");
        }
    }
}
