// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public class NewKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact]
        public async Task TestAtRoot()
        {
            await VerifyKeywordAsync(
@"$$");
        }

        [Fact]
        public async Task TestAfterClass()
        {
            await VerifyKeywordAsync(
                """
                class C { }
                $$
                """);
        }

        [Fact]
        public async Task TestAfterGlobalStatement()
        {
            await VerifyKeywordAsync(
                """
                System.Console.WriteLine();
                $$
                """);
        }

        [Fact]
        public async Task TestAfterGlobalVariableDeclaration()
        {
            await VerifyKeywordAsync(
                """
                int i = 0;
                $$
                """);
        }

        [Fact]
        public async Task TestNotInUsingAlias()
        {
            await VerifyAbsenceAsync(
@"using Goo = $$");
        }

        [Fact]
        public async Task TestNotInGlobalUsingAlias()
        {
            await VerifyAbsenceAsync(
@"global using Goo = $$");
        }

        [Theory, CombinatorialData]
        public async Task TestEmptyStatement(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"$$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Fact]
        public async Task TestAfterNewTypeParameterConstraint()
        {
            await VerifyKeywordAsync(
@"class C<T> where T : $$");
        }

        [Fact]
        public async Task TestAfterTypeParameterConstraint2()
        {
            await VerifyKeywordAsync(
                """
                class C<T>
                    where T : $$
                    where U : U
                """);
        }

        [Fact]
        public async Task TestAfterMethodTypeParameterConstraint()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    void Goo<T>()
                      where T : $$
                """);
        }

        [Fact]
        public async Task TestAfterMethodTypeParameterConstraint2()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    void Goo<T>()
                      where T : $$
                      where U : T
                """);
        }

        [Fact]
        public async Task TestAfterClassTypeParameterConstraint()
        {
            await VerifyKeywordAsync(
@"class C<T> where T : class, $$");
        }

        [Fact]
        public async Task TestNotAfterStructTypeParameterConstraint()
        {
            await VerifyAbsenceAsync(
@"class C<T> where T : struct, $$");
        }

        [Fact]
        public async Task TestAfterSimpleTypeParameterConstraint()
        {
            await VerifyKeywordAsync(
@"class C<T> where T : IGoo, $$");
        }

        [Theory, CombinatorialData]
        public async Task TestStartOfExpression(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        [WorkItem("https://github.com/dotnet/roslyn/issues/34324")]
        public async Task TestAfterNullCoalescingAssignment(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"q ??= $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestInParenthesizedExpression(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = ($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestPlusEquals(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"q += $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestMinusEquals(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"q -= $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestTimesEquals(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"q *= $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestDivideEquals(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"q /= $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestModEquals(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"q %= $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestXorEquals(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"q ^= $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestAndEquals(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"q &= $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestOrEquals(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"q |= $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestLeftShiftEquals(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"q <<= $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestRightShiftEquals(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"q >>= $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestAfterMinus(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"- $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestAfterPlus(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"+ $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestAfterNot(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"! $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestAfterTilde(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"~ $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestAfterBinaryTimes(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"a * $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestAfterBinaryDivide(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"a / $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestAfterBinaryMod(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"a % $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestAfterBinaryPlus(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"a + $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestAfterBinaryMinus(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"a - $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestAfterBinaryLeftShift(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"a << $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestAfterBinaryRightShift(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"a >> $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestAfterBinaryLessThan(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"a < $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestAfterBinaryGreaterThan(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"a > $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestAfterEqualsEquals(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"a == $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestAfterNotEquals(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"a != $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestAfterLessThanEquals(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"a <= $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestAfterGreaterThanEquals(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"a >= $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestAfterNullable(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"a ?? $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestAfterArrayRankSpecifier1(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"new int[ $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestAfterArrayRankSpecifier2(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"new int[expr, $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestAfterConditional1(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"a ? $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [InlineData(false)]
        // <Metalama> The test should be skipped, but it's not. Uncomment after a merge conflict.
        // [InlineData(true, Skip = "https://github.com/dotnet/roslyn/issues/44443")]
        // </Metalama>
        public async Task TestAfterConditional2(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"a ? expr | $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestInArgument1(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"Goo( $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestInArgument2(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"Goo(expr, $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestInArgument3(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"new Goo( $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestInArgument4(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"new Goo(expr, $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestAfterRef(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"Goo(ref $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestAfterOut(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"Goo(out $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestLambda(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"Action<int> a = i => $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestInCollectionInitializer1(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"new System.Collections.Generic.List<int>() { $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestInCollectionInitializer2(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"new System.Collections.Generic.List<int>() { expr, $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestInForeachIn(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"foreach (var v in $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestInAwaitForeachIn(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"await foreach (var v in $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestInFromIn(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = from x in $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestInJoinIn(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                var q = from x in y
                          join a in $$
                """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestInJoinOn(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                var q = from x in y
                          join a in b on $$
                """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestInJoinEquals(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                var q = from x in y
                          join a in b on equals $$
                """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestWhere(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                var q = from x in y
                          where $$
                """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestOrderby1(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                var q = from x in y
                          orderby $$
                """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestOrderby2(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                var q = from x in y
                          orderby a, $$
                """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestOrderby3(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                var q = from x in y
                          orderby a ascending, $$
                """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestAfterSelect(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                var q = from x in y
                          select $$
                """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestAfterGroup(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                var q = from x in y
                          group $$
                """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestAfterGroupBy(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                var q = from x in y
                          group expr by $$
                """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestAfterReturn(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"return $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestAfterYieldReturn(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"yield return $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Fact]
        public async Task TestNotAfterAttributeReturn()
        {
            await VerifyAbsenceAsync(
@"[return $$");
        }

        [Theory, CombinatorialData]
        public async Task TestAfterThrow(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"throw $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestInWhile(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"while ($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestInUsing(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"using ($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestInAwaitUsing(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"await using ($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestInLock(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"lock ($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestInIf(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"if ($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestInSwitch(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"switch ($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Fact]
        public async Task TestAfterExtern()
        {
            await VerifyKeywordAsync("""
                extern alias Goo;
                $$
                """);
        }

        [Fact]
        public async Task TestAfterUsing()
        {
            await VerifyKeywordAsync("""
                using Goo;
                $$
                """);
        }

        [Fact]
        public async Task TestAfterGlobalUsing()
        {
            await VerifyKeywordAsync(
                """
                global using Goo;
                $$
                """);
        }

        [Fact]
        public async Task TestAfterNamespace()
        {
            await VerifyKeywordAsync("""
                namespace N {}
                $$
                """);
        }

        [Fact]
        public async Task TestAfterFileScopedNamespace()
        {
            await VerifyAbsenceAsync(
                """
                namespace N;
                $$
                """);
        }

        [Fact]
        public async Task TestAfterDelegateDeclaration()
        {
            await VerifyKeywordAsync("""
                delegate void Goo();
                $$
                """);
        }

        [Fact]
        public async Task TestAfterMethodInClass()
        {
            await VerifyKeywordAsync(
                """
                class C {
                  void Goo() {}
                  $$
                """);
        }

        [Fact]
        public async Task TestAfterFieldInClass()
        {
            await VerifyKeywordAsync(
                """
                class C {
                  int i;
                  $$
                """);
        }

        [Fact]
        public async Task TestAfterPropertyInClass()
        {
            await VerifyKeywordAsync(
                """
                class C {
                  int i { get; }
                  $$
                """);
        }

        [Fact]
        public async Task TestNotBeforeUsing()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Regular,
                """
                $$
                using Goo;
                """);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/9880")]
        public async Task TestNotBeforeUsing_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
                """
                $$
                using Goo;
                """);
        }

        [Fact]
        public async Task TestNotBeforeGlobalUsing()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Regular,
                """
                $$
                global using Goo;
                """);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/9880")]
        public async Task TestNotBeforeGlobalUsing_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
                """
                $$
                global using Goo;
                """);
        }

        [Fact]
        public async Task TestAfterAssemblyAttribute()
        {
            await VerifyKeywordAsync("""
                [assembly: goo]
                $$
                """);
        }

        [Fact]
        public async Task TestAfterRootAttribute()
        {
            await VerifyKeywordAsync(SourceCodeKind.Regular, """
                [goo]
                $$
                """);
        }

        [Fact]
        public async Task TestAfterRootAttribute_Interactive()
        {
            // The global function could be hiding a member inherited from System.Object.
            await VerifyKeywordAsync(SourceCodeKind.Script, """
                [goo]
                $$
                """);
        }

        [Fact]
        public async Task TestAfterNestedAttribute()
        {
            await VerifyKeywordAsync(
                """
                class C {
                  [goo]
                  $$
                """);
        }

        [Fact]
        public async Task TestInsideStruct()
        {
            await VerifyKeywordAsync(
                """
                struct S {
                   $$
                """);
        }

        [Fact]
        public async Task TestInsideInterface()
        {
            await VerifyKeywordAsync(
                """
                interface I {
                   $$
                """);
        }

        [Fact]
        public async Task TestInsideClass()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   $$
                """);
        }

        [Fact]
        public async Task TestNotAfterPartial()
            => await VerifyAbsenceAsync(@"partial $$");

        [Fact]
        public async Task TestNotAfterAbstract()
            => await VerifyAbsenceAsync(@"abstract $$");

        [Fact]
        public async Task TestNotAfterInternal()
            => await VerifyAbsenceAsync(SourceCodeKind.Regular, @"internal $$");

        [Fact]
        public async Task TestAfterInternal_Interactive()
            => await VerifyKeywordAsync(SourceCodeKind.Script, @"internal $$");

        [Fact]
        public async Task TestNotAfterPublic()
            => await VerifyAbsenceAsync(SourceCodeKind.Regular, @"public $$");

        [Fact]
        public async Task TestAfterPublic_Interactive()
            => await VerifyKeywordAsync(SourceCodeKind.Script, @"public $$");

        [Fact]
        public async Task TestNotAfterStaticInternal()
            => await VerifyAbsenceAsync(SourceCodeKind.Regular, @"static internal $$");

        [Fact]
        public async Task TestAfterStaticInternal_Interactive()
            => await VerifyKeywordAsync(SourceCodeKind.Script, @"static internal $$");

        [Fact]
        public async Task TestNotAfterInternalStatic()
            => await VerifyAbsenceAsync(SourceCodeKind.Regular, @"internal static $$");

        [Fact]
        public async Task TestAfterInternalStatic_Interactive()
            => await VerifyKeywordAsync(SourceCodeKind.Script, @"internal static $$");

        [Fact]
        public async Task TestNotAfterInvalidInternal()
            => await VerifyAbsenceAsync(@"virtual internal $$");

        [Fact]
        public async Task TestNotAfterClass()
            => await VerifyAbsenceAsync(@"class $$");

        [Fact]
        public async Task TestNotAfterPrivate()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Regular,
@"private $$");
        }

        [Fact]
        public async Task TestAfterPrivate_Script()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
@"private $$");
        }

        [Fact]
        public async Task TestNotAfterSealed()
            => await VerifyAbsenceAsync(@"sealed $$");

        [Fact]
        public async Task TestNotAfterStatic()
            => await VerifyAbsenceAsync(SourceCodeKind.Regular, @"static $$");

        [Fact]
        public async Task TestAfterStatic_Interactive()
            => await VerifyKeywordAsync(SourceCodeKind.Script, @"static $$");

        [Fact]
        public async Task TestAfterNestedStatic()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    static $$
                """);
        }

        [Fact]
        public async Task TestAfterNestedInternal()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    internal $$
                """);
        }

        [Fact]
        public async Task TestAfterNestedPrivate()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    private $$
                """);
        }

        [Fact]
        public async Task TestNotAfterDelegate()
            => await VerifyAbsenceAsync(@"delegate $$");

        [Fact]
        public async Task TestAfterNestedAbstract()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    abstract $$
                """);
        }

        [Fact]
        public async Task TestAfterNestedVirtual()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    virtual $$
                """);
        }

        [Fact]
        public async Task TestNotAfterNestedNew()
        {
            await VerifyAbsenceAsync("""
                class C {
                    new $$
                """);
        }

        [Fact]
        public async Task TestNotAfterNestedOverride()
        {
            await VerifyAbsenceAsync("""
                class C {
                    override $$
                """);
        }

        [Fact]
        public async Task TestAfterNestedSealed()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    sealed $$
                """);
        }

        [Fact]
        public async Task TestNotInProperty()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    int Goo { $$
                """);
        }

        [Fact]
        public async Task TestNotInPropertyAfterAccessor()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    int Goo { get; $$
                """);
        }

        [Fact]
        public async Task TestNotInPropertyAfterAccessibility()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    int Goo { get; protected $$
                """);
        }

        [Fact]
        public async Task TestNotInPropertyAfterInternal()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    int Goo { get; internal $$
                """);
        }

        [Fact]
        public async Task TestAfterCastType1()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"return (LeafSegment)$$"));
        }

        [Fact]
        public async Task TestAfterCastType2()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"return (LeafSegment)(object)$$"));
        }

        [Fact]
        public async Task TestNotAfterParenthesizedExpression()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"return (a + b)$$"));
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538264")]
        public async Task TestInConstMemberInitializer1()
        {
            // User could say "new int()" here.
            await VerifyKeywordAsync(
                """
                class E {
                    const int a = $$
                }
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538264")]
        public async Task TestInConstLocalInitializer1()
        {
            // User could say "new int()" here.
            await VerifyKeywordAsync(
                """
                class E {
                  void Goo() {
                    const int a = $$
                  }
                }
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538264")]
        public async Task TestInMemberInitializer1()
        {
            await VerifyKeywordAsync(
                """
                class E {
                    int a = $$
                }
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
        public async Task TestNotInTypeOf()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"typeof($$"));
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
        public async Task TestNotInDefault()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"default($$"));
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
        public async Task TestNotInSizeOf()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"sizeof($$"));
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544219")]
        public async Task TestNotInObjectInitializerMemberContext()
        {
            await VerifyAbsenceAsync("""
                class C
                {
                    public int x, y;
                    void M()
                    {
                        var c = new C { x = 2, y = 3, $$
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544486")]
        public async Task TestInsideInitOfConstFieldDecl()
        {
            // user could say "new int()" here.
            await VerifyKeywordAsync(
                """
                class C
                {
                    const int value = $$
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544998")]
        public async Task TestInsideStructParameterInitializer()
        {
            await VerifyKeywordAsync(
                """
                struct C
                {
                    void M(C c = $$
                }
                """);
        }

        [Fact]
        public async Task TestAfterRefExpression()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"ref int x = ref $$"));
        }

        [Fact]
        public async Task TestInRawStringInterpolation_SingleLine()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """"
                var x = $"""{$$}"""
                """"));
        }

        [Fact]
        public async Task TestInRawStringInterpolation_SingleLine_MultiBrace()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """"
                var x = ${|#0:|}$"""{{$$}}"""
                """"));
        }

        [Fact]
        public async Task TestInRawStringInterpolation_SingleLineIncomplete()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var x = $""""""{$$"));
        }

        [Fact]
        public async Task TestInRawStringInterpolation_MultiLine()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """"
                var x = $"""
                {$$}
                """
                """"));
        }

        [Fact]
        public async Task TestInRawStringInterpolation_MultiLine_MultiBrace()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """"
                var x = ${|#0:|}$"""
                {{$$}}
                """
                """"));
        }

        [Fact]
        public async Task TestInRawStringInterpolation_MultiLineIncomplete()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """"
                var x = $"""
                {$$
                """"));
        }

        #region Collection expressions

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
        public async Task TestInCollectionExpressions_BeforeFirstElementToVar()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                var x = [$$
                """));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
        public async Task TestInCollectionExpressions_BeforeFirstElementToReturn()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    IEnumerable<string> M() => [$$
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
        public async Task TestInCollectionExpressions_AfterFirstElementToVar()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                var x = [new object(), $$
                """));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
        public async Task TestInCollectionExpressions_AfterFirstElementToReturn()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    IEnumerable<string> M() => [string.Empty, $$
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
        public async Task TestInCollectionExpressions_SpreadBeforeFirstElementToReturn()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    IEnumerable<string> M() => [.. $$
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
        public async Task TestInCollectionExpressions_SpreadAfterFirstElementToReturn()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    IEnumerable<string> M() => [string.Empty, .. $$
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
        public async Task TestInCollectionExpressions_ParenAtFirstElementToReturn()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    IEnumerable<string> M() => [($$
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
        public async Task TestInCollectionExpressions_ParenAfterFirstElementToReturn()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    IEnumerable<string> M() => [string.Empty, ($$
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
        public async Task TestInCollectionExpressions_ParenSpreadAtFirstElementToReturn()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    IEnumerable<string> M() => [.. ($$
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
        public async Task TestInCollectionExpressions_ParenSpreadAfterFirstElementToReturn()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    IEnumerable<string> M() => [string.Empty, .. ($$
                }
                """);
        }

        #endregion
    }
}
