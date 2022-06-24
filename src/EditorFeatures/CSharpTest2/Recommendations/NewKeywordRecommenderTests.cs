// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            await VerifyKeywordAsync(
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterClass()
        {
            await VerifyKeywordAsync(
@"class C { }
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterGlobalStatement()
        {
            await VerifyKeywordAsync(
@"System.Console.WriteLine();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterGlobalVariableDeclaration()
        {
            await VerifyKeywordAsync(
@"int i = 0;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInUsingAlias()
        {
            await VerifyAbsenceAsync(
@"using Goo = $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInGlobalUsingAlias()
        {
            await VerifyAbsenceAsync(
@"global using Goo = $$");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestEmptyStatement(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"$$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
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
    void Goo<T>()
      where T : $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterMethodTypeParameterConstraint2()
        {
            await VerifyKeywordAsync(
@"class C {
    void Goo<T>()
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
@"class C<T> where T : IGoo, $$");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestStartOfExpression(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        [WorkItem(34324, "https://github.com/dotnet/roslyn/issues/34324")]
        public async Task TestAfterNullCoalescingAssignment(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"q ??= $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInParenthesizedExpression(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = ($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestPlusEquals(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"q += $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestMinusEquals(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"q -= $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestTimesEquals(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"q *= $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestDivideEquals(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"q /= $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestModEquals(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"q %= $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestXorEquals(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"q ^= $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAndEquals(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"q &= $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestOrEquals(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"q |= $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestLeftShiftEquals(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"q <<= $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestRightShiftEquals(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"q >>= $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterMinus(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"- $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterPlus(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"+ $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterNot(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"! $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterTilde(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"~ $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterBinaryTimes(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"a * $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterBinaryDivide(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"a / $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterBinaryMod(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"a % $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterBinaryPlus(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"a + $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterBinaryMinus(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"a - $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterBinaryLeftShift(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"a << $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterBinaryRightShift(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"a >> $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterBinaryLessThan(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"a < $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterBinaryGreaterThan(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"a > $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterEqualsEquals(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"a == $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterNotEquals(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"a != $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterLessThanEquals(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"a <= $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterGreaterThanEquals(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"a >= $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterNullable(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"a ?? $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterArrayRankSpecifier1(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"new int[ $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterArrayRankSpecifier2(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"new int[expr, $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterConditional1(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"a ? $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [InlineData(false)]
        [InlineData(true, Skip = "https://github.com/dotnet/roslyn/issues/44443")]
        public async Task TestAfterConditional2(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"a ? expr | $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInArgument1(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"Goo( $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInArgument2(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"Goo(expr, $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInArgument3(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"new Goo( $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInArgument4(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"new Goo(expr, $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterRef(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"Goo(ref $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterOut(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"Goo(out $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestLambda(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"Action<int> a = i => $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInCollectionInitializer1(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"new System.Collections.Generic.List<int>() { $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInCollectionInitializer2(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"new System.Collections.Generic.List<int>() { expr, $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInForeachIn(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"foreach (var v in $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInAwaitForeachIn(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"await foreach (var v in $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInFromIn(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = from x in $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInJoinIn(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = from x in y
          join a in $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInJoinOn(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = from x in y
          join a in b on $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInJoinEquals(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = from x in y
          join a in b on equals $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestWhere(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = from x in y
          where $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestOrderby1(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = from x in y
          orderby $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestOrderby2(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = from x in y
          orderby a, $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestOrderby3(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = from x in y
          orderby a ascending, $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterSelect(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = from x in y
          select $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterGroup(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = from x in y
          group $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterGroupBy(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = from x in y
          group expr by $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterReturn(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"return $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterYieldReturn(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"yield return $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterAttributeReturn()
        {
            await VerifyAbsenceAsync(
@"[return $$");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterThrow(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"throw $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInWhile(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"while ($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInUsing(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"using ($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInAwaitUsing(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"await using ($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInLock(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"lock ($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInIf(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"if ($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInSwitch(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"switch ($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterExtern()
        {
            await VerifyKeywordAsync(@"extern alias Goo;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterUsing()
        {
            await VerifyKeywordAsync(@"using Goo;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterGlobalUsing()
        {
            await VerifyKeywordAsync(
@"global using Goo;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNamespace()
        {
            await VerifyKeywordAsync(@"namespace N {}
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterFileScopedNamespace()
        {
            await VerifyAbsenceAsync(
@"namespace N;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterDelegateDeclaration()
        {
            await VerifyKeywordAsync(@"delegate void Goo();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterMethodInClass()
        {
            await VerifyKeywordAsync(
@"class C {
  void Goo() {}
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
using Goo;");
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/9880"), Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotBeforeUsing_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"$$
using Goo;");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotBeforeGlobalUsing()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Regular,
@"$$
global using Goo;");
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/9880"), Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotBeforeGlobalUsing_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"$$
global using Goo;");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterAssemblyAttribute()
        {
            await VerifyKeywordAsync(@"[assembly: goo]
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterRootAttribute()
        {
            await VerifyKeywordAsync(SourceCodeKind.Regular, @"[goo]
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterRootAttribute_Interactive()
        {
            // The global function could be hiding a member inherited from System.Object.
            await VerifyKeywordAsync(SourceCodeKind.Script, @"[goo]
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedAttribute()
        {
            await VerifyKeywordAsync(
@"class C {
  [goo]
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
            => await VerifyAbsenceAsync(@"partial $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterAbstract()
            => await VerifyAbsenceAsync(@"abstract $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterInternal()
            => await VerifyAbsenceAsync(SourceCodeKind.Regular, @"internal $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterInternal_Interactive()
            => await VerifyKeywordAsync(SourceCodeKind.Script, @"internal $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterPublic()
            => await VerifyAbsenceAsync(SourceCodeKind.Regular, @"public $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPublic_Interactive()
            => await VerifyKeywordAsync(SourceCodeKind.Script, @"public $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterStaticInternal()
            => await VerifyAbsenceAsync(SourceCodeKind.Regular, @"static internal $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterStaticInternal_Interactive()
            => await VerifyKeywordAsync(SourceCodeKind.Script, @"static internal $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterInternalStatic()
            => await VerifyAbsenceAsync(SourceCodeKind.Regular, @"internal static $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterInternalStatic_Interactive()
            => await VerifyKeywordAsync(SourceCodeKind.Script, @"internal static $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterInvalidInternal()
            => await VerifyAbsenceAsync(@"virtual internal $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterClass()
            => await VerifyAbsenceAsync(@"class $$");

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
            => await VerifyAbsenceAsync(@"sealed $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterStatic()
            => await VerifyAbsenceAsync(SourceCodeKind.Regular, @"static $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterStatic_Interactive()
            => await VerifyKeywordAsync(SourceCodeKind.Script, @"static $$");

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
            => await VerifyAbsenceAsync(@"delegate $$");

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
    int Goo { $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInPropertyAfterAccessor()
        {
            await VerifyAbsenceAsync(
@"class C {
    int Goo { get; $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInPropertyAfterAccessibility()
        {
            await VerifyAbsenceAsync(
@"class C {
    int Goo { get; protected $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInPropertyAfterInternal()
        {
            await VerifyAbsenceAsync(
@"class C {
    int Goo { get; internal $$");
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
  void Goo() {
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

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterRefExpression()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"ref int x = ref $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInRawStringInterpolation_SingleLine()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var x = $""""""{$$}"""""""));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInRawStringInterpolation_SingleLine_MultiBrace()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var x = ${|#0:|}$""""""{{$$}}"""""""));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInRawStringInterpolation_SingleLineIncomplete()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var x = $""""""{$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInRawStringInterpolation_MultiLine()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var x = $""""""
{$$}
"""""""));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInRawStringInterpolation_MultiLine_MultiBrace()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var x = ${|#0:|}$""""""
{{$$}}
"""""""));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInRawStringInterpolation_MultiLineIncomplete()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var x = $""""""
{$$"));
        }
    }
}
