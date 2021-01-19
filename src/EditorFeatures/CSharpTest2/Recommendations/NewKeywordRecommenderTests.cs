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
    public class NewKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAtRoot()
        {
            VerifyKeyword(
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterClass()
        {
            VerifyKeyword(
@"class C { }
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterGlobalStatement()
        {
            VerifyKeyword(
@"System.Console.WriteLine();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterGlobalVariableDeclaration()
        {
            VerifyKeyword(
@"int i = 0;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInUsingAlias()
        {
            VerifyAbsence(
@"using Goo = $$");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestEmptyStatement(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"$$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNewTypeParameterConstraint()
        {
            VerifyKeyword(
@"class C<T> where T : $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterTypeParameterConstraint2()
        {
            VerifyKeyword(
@"class C<T>
    where T : $$
    where U : U");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterMethodTypeParameterConstraint()
        {
            VerifyKeyword(
@"class C {
    void Goo<T>()
      where T : $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterMethodTypeParameterConstraint2()
        {
            VerifyKeyword(
@"class C {
    void Goo<T>()
      where T : $$
      where U : T");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterClassTypeParameterConstraint()
        {
            VerifyKeyword(
@"class C<T> where T : class, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterStructTypeParameterConstraint()
        {
            VerifyAbsence(
@"class C<T> where T : struct, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterSimpleTypeParameterConstraint()
        {
            VerifyKeyword(
@"class C<T> where T : IGoo, $$");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestStartOfExpression(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"var q = $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        [WorkItem(34324, "https://github.com/dotnet/roslyn/issues/34324")]
        public async Task TestAfterNullCoalescingAssignment(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"q ??= $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInParenthesizedExpression(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"var q = ($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestPlusEquals(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"q += $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestMinusEquals(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"q -= $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestTimesEquals(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"q *= $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestDivideEquals(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"q /= $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestModEquals(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"q %= $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestXorEquals(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"q ^= $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAndEquals(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"q &= $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestOrEquals(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"q |= $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestLeftShiftEquals(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"q <<= $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestRightShiftEquals(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"q >>= $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterMinus(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"- $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterPlus(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"+ $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterNot(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"! $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterTilde(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"~ $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterBinaryTimes(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"a * $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterBinaryDivide(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"a / $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterBinaryMod(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"a % $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterBinaryPlus(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"a + $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterBinaryMinus(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"a - $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterBinaryLeftShift(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"a << $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterBinaryRightShift(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"a >> $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterBinaryLessThan(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"a < $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterBinaryGreaterThan(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"a > $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterEqualsEquals(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"a == $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterNotEquals(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"a != $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterLessThanEquals(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"a <= $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterGreaterThanEquals(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"a >= $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterNullable(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"a ?? $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterArrayRankSpecifier1(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"new int[ $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterArrayRankSpecifier2(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"new int[expr, $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterConditional1(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"a ? $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [InlineData(false)]
        [InlineData(true, Skip = "https://github.com/dotnet/roslyn/issues/44443")]
        public async Task TestAfterConditional2(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"a ? expr | $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInArgument1(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"Goo( $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInArgument2(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"Goo(expr, $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInArgument3(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"new Goo( $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInArgument4(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"new Goo(expr, $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterRef(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"Goo(ref $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterOut(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"Goo(out $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestLambda(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"Action<int> a = i => $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInCollectionInitializer1(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"new System.Collections.Generic.List<int>() { $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInCollectionInitializer2(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"new System.Collections.Generic.List<int>() { expr, $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInForeachIn(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"foreach (var v in $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInAwaitForeachIn(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"await foreach (var v in $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInFromIn(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"var q = from x in $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInJoinIn(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"var q = from x in y
          join a in $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInJoinOn(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"var q = from x in y
          join a in b on $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInJoinEquals(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"var q = from x in y
          join a in b on equals $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestWhere(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"var q = from x in y
          where $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestOrderby1(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"var q = from x in y
          orderby $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestOrderby2(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"var q = from x in y
          orderby a, $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestOrderby3(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"var q = from x in y
          orderby a ascending, $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterSelect(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"var q = from x in y
          select $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterGroup(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"var q = from x in y
          group $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterGroupBy(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"var q = from x in y
          group expr by $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterReturn(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"return $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterYieldReturn(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"yield return $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterAttributeReturn()
        {
            VerifyAbsence(
@"[return $$");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterThrow(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"throw $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInWhile(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"while ($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInUsing(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"using ($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInAwaitUsing(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"await using ($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInLock(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"lock ($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInIf(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"if ($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInSwitch(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"switch ($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterExtern()
        {
            VerifyKeyword(@"extern alias Goo;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterUsing()
        {
            VerifyKeyword(@"using Goo;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNamespace()
        {
            VerifyKeyword(@"namespace N {}
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterDelegateDeclaration()
        {
            VerifyKeyword(@"delegate void Goo();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterMethodInClass()
        {
            VerifyKeyword(
@"class C {
  void Goo() {}
  $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterFieldInClass()
        {
            VerifyKeyword(
@"class C {
  int i;
  $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPropertyInClass()
        {
            VerifyKeyword(
@"class C {
  int i { get; }
  $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotBeforeUsing()
        {
            VerifyAbsence(SourceCodeKind.Regular,
@"$$
using Goo;");
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/9880"), Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotBeforeUsing_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"$$
using Goo;");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterAssemblyAttribute()
        {
            VerifyKeyword(@"[assembly: goo]
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterRootAttribute()
        {
            VerifyAbsence(SourceCodeKind.Regular, @"[goo]
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterRootAttribute_Interactive()
        {
            // The global function could be hiding a member inherited from System.Object.
            VerifyKeyword(SourceCodeKind.Script, @"[goo]
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedAttribute()
        {
            VerifyKeyword(
@"class C {
  [goo]
  $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInsideStruct()
        {
            VerifyKeyword(
@"struct S {
   $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInsideInterface()
        {
            VerifyKeyword(
@"interface I {
   $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInsideClass()
        {
            VerifyKeyword(
@"class C {
   $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterPartial()
            => VerifyAbsence(@"partial $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterAbstract()
            => VerifyAbsence(@"abstract $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterInternal()
            => VerifyAbsence(SourceCodeKind.Regular, @"internal $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterInternal_Interactive()
            => VerifyKeyword(SourceCodeKind.Script, @"internal $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterPublic()
            => VerifyAbsence(SourceCodeKind.Regular, @"public $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPublic_Interactive()
            => VerifyKeyword(SourceCodeKind.Script, @"public $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterStaticInternal()
            => VerifyAbsence(SourceCodeKind.Regular, @"static internal $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterStaticInternal_Interactive()
            => VerifyKeyword(SourceCodeKind.Script, @"static internal $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterInternalStatic()
            => VerifyAbsence(SourceCodeKind.Regular, @"internal static $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterInternalStatic_Interactive()
            => VerifyKeyword(SourceCodeKind.Script, @"internal static $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterInvalidInternal()
            => VerifyAbsence(@"virtual internal $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterClass()
            => VerifyAbsence(@"class $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterPrivate()
        {
            VerifyAbsence(SourceCodeKind.Regular,
@"private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPrivate_Script()
        {
            VerifyKeyword(SourceCodeKind.Script,
@"private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterSealed()
            => VerifyAbsence(@"sealed $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterStatic()
            => VerifyAbsence(SourceCodeKind.Regular, @"static $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterStatic_Interactive()
            => VerifyKeyword(SourceCodeKind.Script, @"static $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedStatic()
        {
            VerifyKeyword(
@"class C {
    static $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedInternal()
        {
            VerifyKeyword(
@"class C {
    internal $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedPrivate()
        {
            VerifyKeyword(
@"class C {
    private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterDelegate()
            => VerifyAbsence(@"delegate $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedAbstract()
        {
            VerifyKeyword(
@"class C {
    abstract $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedVirtual()
        {
            VerifyKeyword(
@"class C {
    virtual $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterNestedNew()
        {
            VerifyAbsence(@"class C {
    new $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterNestedOverride()
        {
            VerifyAbsence(@"class C {
    override $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedSealed()
        {
            VerifyKeyword(
@"class C {
    sealed $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInProperty()
        {
            VerifyAbsence(
@"class C {
    int Goo { $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInPropertyAfterAccessor()
        {
            VerifyAbsence(
@"class C {
    int Goo { get; $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInPropertyAfterAccessibility()
        {
            VerifyAbsence(
@"class C {
    int Goo { get; protected $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInPropertyAfterInternal()
        {
            VerifyAbsence(
@"class C {
    int Goo { get; internal $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterCastType1()
        {
            VerifyKeyword(AddInsideMethod(
@"return (LeafSegment)$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterCastType2()
        {
            VerifyKeyword(AddInsideMethod(
@"return (LeafSegment)(object)$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterParenthesizedExpression()
        {
            VerifyAbsence(AddInsideMethod(
@"return (a + b)$$"));
        }

        [WorkItem(538264, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538264")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInConstMemberInitializer1()
        {
            // User could say "new int()" here.
            VerifyKeyword(
@"class E {
    const int a = $$
}");
        }

        [WorkItem(538264, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538264")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInConstLocalInitializer1()
        {
            // User could say "new int()" here.
            VerifyKeyword(
@"class E {
  void Goo() {
    const int a = $$
  }
}");
        }

        [WorkItem(538264, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538264")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInMemberInitializer1()
        {
            VerifyKeyword(
@"class E {
    int a = $$
}");
        }

        [WorkItem(538804, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInTypeOf()
        {
            VerifyAbsence(AddInsideMethod(
@"typeof($$"));
        }

        [WorkItem(538804, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInDefault()
        {
            VerifyAbsence(AddInsideMethod(
@"default($$"));
        }

        [WorkItem(538804, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInSizeOf()
        {
            VerifyAbsence(AddInsideMethod(
@"sizeof($$"));
        }

        [WorkItem(544219, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544219")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInObjectInitializerMemberContext()
        {
            VerifyAbsence(@"
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
            VerifyKeyword(
@"class C
{
    const int value = $$");
        }

        [WorkItem(544998, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544998")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInsideStructParameterInitializer()
        {
            VerifyKeyword(
@"struct C
{
    void M(C c = $$
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterRefExpression()
        {
            VerifyKeyword(AddInsideMethod(
@"ref int x = ref $$"));
        }
    }
}
