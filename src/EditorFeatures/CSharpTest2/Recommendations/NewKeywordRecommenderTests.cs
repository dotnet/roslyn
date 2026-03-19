// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class NewKeywordRecommenderTests : KeywordRecommenderTests
{
    [Fact]
    public Task TestAtRoot()
        => VerifyKeywordAsync(
@"$$");

    [Fact]
    public Task TestAfterClass()
        => VerifyKeywordAsync(
            """
            class C { }
            $$
            """);

    [Fact]
    public Task TestAfterGlobalStatement()
        => VerifyKeywordAsync(
            """
            System.Console.WriteLine();
            $$
            """);

    [Fact]
    public Task TestAfterGlobalVariableDeclaration()
        => VerifyKeywordAsync(
            """
            int i = 0;
            $$
            """);

    [Fact]
    public Task TestNotInUsingAlias()
        => VerifyAbsenceAsync(
@"using Goo = $$");

    [Fact]
    public Task TestNotInGlobalUsingAlias()
        => VerifyAbsenceAsync(
@"global using Goo = $$");

    [Theory, CombinatorialData]
    public Task TestEmptyStatement(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"$$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Fact]
    public Task TestAfterNewTypeParameterConstraint()
        => VerifyKeywordAsync(
@"class C<T> where T : $$");

    [Fact]
    public Task TestAfterTypeParameterConstraint2()
        => VerifyKeywordAsync(
            """
            class C<T>
                where T : $$
                where U : U
            """);

    [Fact]
    public Task TestAfterMethodTypeParameterConstraint()
        => VerifyKeywordAsync(
            """
            class C {
                void Goo<T>()
                  where T : $$
            """);

    [Fact]
    public Task TestAfterMethodTypeParameterConstraint2()
        => VerifyKeywordAsync(
            """
            class C {
                void Goo<T>()
                  where T : $$
                  where U : T
            """);

    [Fact]
    public Task TestAfterClassTypeParameterConstraint()
        => VerifyKeywordAsync(
@"class C<T> where T : class, $$");

    [Fact]
    public Task TestNotAfterStructTypeParameterConstraint()
        => VerifyAbsenceAsync(
@"class C<T> where T : struct, $$");

    [Fact]
    public Task TestAfterSimpleTypeParameterConstraint()
        => VerifyKeywordAsync(
@"class C<T> where T : IGoo, $$");

    [Theory, CombinatorialData]
    public Task TestStartOfExpression(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"var q = $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/34324")]
    public Task TestAfterNullCoalescingAssignment(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"q ??= $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestInParenthesizedExpression(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"var q = ($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestPlusEquals(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"q += $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestMinusEquals(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"q -= $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestTimesEquals(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"q *= $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestDivideEquals(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"q /= $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestModEquals(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"q %= $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestXorEquals(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"q ^= $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAndEquals(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"q &= $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestOrEquals(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"q |= $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestLeftShiftEquals(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"q <<= $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestRightShiftEquals(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"q >>= $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAfterMinus(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"- $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAfterPlus(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"+ $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAfterNot(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"! $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAfterTilde(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"~ $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAfterBinaryTimes(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"a * $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAfterBinaryDivide(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"a / $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAfterBinaryMod(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"a % $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAfterBinaryPlus(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"a + $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAfterBinaryMinus(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"a - $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAfterBinaryLeftShift(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"a << $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAfterBinaryRightShift(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"a >> $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAfterBinaryLessThan(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"a < $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAfterBinaryGreaterThan(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"a > $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAfterEqualsEquals(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"a == $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAfterNotEquals(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"a != $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAfterLessThanEquals(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"a <= $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAfterGreaterThanEquals(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"a >= $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAfterNullable(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"a ?? $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAfterArrayRankSpecifier1(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"new int[ $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAfterArrayRankSpecifier2(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"new int[expr, $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAfterConditional1(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"a ? $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory]
    [InlineData(false)]
    [InlineData(true, Skip = "https://github.com/dotnet/roslyn/issues/44443")]
    public Task TestAfterConditional2(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"a ? expr | $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestInArgument1(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"Goo( $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestInArgument2(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"Goo(expr, $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestInArgument3(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"new Goo( $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestInArgument4(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"new Goo(expr, $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAfterRef(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"Goo(ref $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAfterOut(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"Goo(out $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestLambda(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"Action<int> a = i => $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestInCollectionInitializer1(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"new System.Collections.Generic.List<int>() { $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestInCollectionInitializer2(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"new System.Collections.Generic.List<int>() { expr, $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestInForeachIn(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"foreach (var v in $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestInAwaitForeachIn(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"await foreach (var v in $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestInFromIn(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"var q = from x in $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestInJoinIn(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
            """
            var q = from x in y
                      join a in $$
            """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestInJoinOn(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
            """
            var q = from x in y
                      join a in b on $$
            """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestInJoinEquals(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
            """
            var q = from x in y
                      join a in b on equals $$
            """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestWhere(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
            """
            var q = from x in y
                      where $$
            """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestOrderby1(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
            """
            var q = from x in y
                      orderby $$
            """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestOrderby2(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
            """
            var q = from x in y
                      orderby a, $$
            """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestOrderby3(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
            """
            var q = from x in y
                      orderby a ascending, $$
            """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAfterSelect(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
            """
            var q = from x in y
                      select $$
            """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAfterGroup(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
            """
            var q = from x in y
                      group $$
            """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAfterGroupBy(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
            """
            var q = from x in y
                      group expr by $$
            """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAfterReturn(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"return $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAfterYieldReturn(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"yield return $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Fact]
    public Task TestNotAfterAttributeReturn()
        => VerifyAbsenceAsync(
@"[return $$");

    [Theory, CombinatorialData]
    public Task TestAfterThrow(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"throw $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestInWhile(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"while ($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestInUsing(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"using ($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestInAwaitUsing(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"await using ($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestInLock(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"lock ($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestInIf(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"if ($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestInSwitch(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"switch ($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Fact]
    public Task TestAfterExtern()
        => VerifyKeywordAsync("""
            extern alias Goo;
            $$
            """);

    [Fact]
    public Task TestAfterUsing()
        => VerifyKeywordAsync("""
            using Goo;
            $$
            """);

    [Fact]
    public Task TestAfterGlobalUsing()
        => VerifyKeywordAsync(
            """
            global using Goo;
            $$
            """);

    [Fact]
    public Task TestAfterNamespace()
        => VerifyKeywordAsync("""
            namespace N {}
            $$
            """);

    [Fact]
    public Task TestAfterFileScopedNamespace()
        => VerifyAbsenceAsync(
            """
            namespace N;
            $$
            """);

    [Fact]
    public Task TestAfterDelegateDeclaration()
        => VerifyKeywordAsync("""
            delegate void Goo();
            $$
            """);

    [Fact]
    public Task TestAfterMethodInClass()
        => VerifyKeywordAsync(
            """
            class C {
              void Goo() {}
              $$
            """);

    [Fact]
    public Task TestAfterFieldInClass()
        => VerifyKeywordAsync(
            """
            class C {
              int i;
              $$
            """);

    [Fact]
    public Task TestAfterPropertyInClass()
        => VerifyKeywordAsync(
            """
            class C {
              int i { get; }
              $$
            """);

    [Fact]
    public Task TestNotBeforeUsing()
        => VerifyAbsenceAsync(SourceCodeKind.Regular,
            """
            $$
            using Goo;
            """);

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/9880")]
    public Task TestNotBeforeUsing_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
            """
            $$
            using Goo;
            """);

    [Fact]
    public Task TestNotBeforeGlobalUsing()
        => VerifyAbsenceAsync(SourceCodeKind.Regular,
            """
            $$
            global using Goo;
            """);

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/9880")]
    public Task TestNotBeforeGlobalUsing_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
            """
            $$
            global using Goo;
            """);

    [Fact]
    public Task TestAfterAssemblyAttribute()
        => VerifyKeywordAsync("""
            [assembly: goo]
            $$
            """);

    [Fact]
    public Task TestAfterRootAttribute()
        => VerifyKeywordAsync(SourceCodeKind.Regular, """
            [goo]
            $$
            """);

    [Fact]
    public Task TestAfterRootAttribute_Interactive()
        => VerifyKeywordAsync(SourceCodeKind.Script, """
            [goo]
            $$
            """);

    [Fact]
    public Task TestAfterNestedAttribute()
        => VerifyKeywordAsync(
            """
            class C {
              [goo]
              $$
            """);

    [Fact]
    public Task TestInsideStruct()
        => VerifyKeywordAsync(
            """
            struct S {
               $$
            """);

    [Fact]
    public Task TestInsideInterface()
        => VerifyKeywordAsync(
            """
            interface I {
               $$
            """);

    [Fact]
    public Task TestInsideClass()
        => VerifyKeywordAsync(
            """
            class C {
               $$
            """);

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
    public Task TestNotAfterPrivate()
        => VerifyAbsenceAsync(SourceCodeKind.Regular,
@"private $$");

    [Fact]
    public Task TestAfterPrivate_Script()
        => VerifyKeywordAsync(SourceCodeKind.Script,
@"private $$");

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
    public Task TestAfterNestedStatic()
        => VerifyKeywordAsync(
            """
            class C {
                static $$
            """);

    [Fact]
    public Task TestAfterNestedInternal()
        => VerifyKeywordAsync(
            """
            class C {
                internal $$
            """);

    [Fact]
    public Task TestAfterNestedPrivate()
        => VerifyKeywordAsync(
            """
            class C {
                private $$
            """);

    [Fact]
    public async Task TestNotAfterDelegate()
        => await VerifyAbsenceAsync(@"delegate $$");

    [Fact]
    public Task TestAfterNestedAbstract()
        => VerifyKeywordAsync(
            """
            class C {
                abstract $$
            """);

    [Fact]
    public Task TestAfterNestedVirtual()
        => VerifyKeywordAsync(
            """
            class C {
                virtual $$
            """);

    [Fact]
    public Task TestNotAfterNestedNew()
        => VerifyAbsenceAsync("""
            class C {
                new $$
            """);

    [Fact]
    public Task TestNotAfterNestedOverride()
        => VerifyAbsenceAsync("""
            class C {
                override $$
            """);

    [Fact]
    public Task TestAfterNestedSealed()
        => VerifyKeywordAsync(
            """
            class C {
                sealed $$
            """);

    [Fact]
    public Task TestNotInProperty()
        => VerifyAbsenceAsync(
            """
            class C {
                int Goo { $$
            """);

    [Fact]
    public Task TestNotInPropertyAfterAccessor()
        => VerifyAbsenceAsync(
            """
            class C {
                int Goo { get; $$
            """);

    [Fact]
    public Task TestNotInPropertyAfterAccessibility()
        => VerifyAbsenceAsync(
            """
            class C {
                int Goo { get; protected $$
            """);

    [Fact]
    public Task TestNotInPropertyAfterInternal()
        => VerifyAbsenceAsync(
            """
            class C {
                int Goo { get; internal $$
            """);

    [Fact]
    public Task TestAfterCastType1()
        => VerifyKeywordAsync(AddInsideMethod(
@"return (LeafSegment)$$"));

    [Fact]
    public Task TestAfterCastType2()
        => VerifyKeywordAsync(AddInsideMethod(
@"return (LeafSegment)(object)$$"));

    [Fact]
    public Task TestNotAfterParenthesizedExpression()
        => VerifyAbsenceAsync(AddInsideMethod(
@"return (a + b)$$"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538264")]
    public Task TestInConstMemberInitializer1()
        => VerifyKeywordAsync(
            """
            class E {
                const int a = $$
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538264")]
    public Task TestInConstLocalInitializer1()
        => VerifyKeywordAsync(
            """
            class E {
              void Goo() {
                const int a = $$
              }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538264")]
    public Task TestInMemberInitializer1()
        => VerifyKeywordAsync(
            """
            class E {
                int a = $$
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
    public Task TestNotInTypeOf()
        => VerifyAbsenceAsync(AddInsideMethod(
@"typeof($$"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
    public Task TestNotInDefault()
        => VerifyAbsenceAsync(AddInsideMethod(
@"default($$"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
    public Task TestNotInSizeOf()
        => VerifyAbsenceAsync(AddInsideMethod(
@"sizeof($$"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544219")]
    public Task TestNotInObjectInitializerMemberContext()
        => VerifyAbsenceAsync("""
            class C
            {
                public int x, y;
                void M()
                {
                    var c = new C { x = 2, y = 3, $$
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544486")]
    public Task TestInsideInitOfConstFieldDecl()
        => VerifyKeywordAsync(
            """
            class C
            {
                const int value = $$
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544998")]
    public Task TestInsideStructParameterInitializer()
        => VerifyKeywordAsync(
            """
            struct C
            {
                void M(C c = $$
            }
            """);

    [Fact]
    public Task TestAfterRefExpression()
        => VerifyKeywordAsync(AddInsideMethod(
@"ref int x = ref $$"));

    [Fact]
    public Task TestInRawStringInterpolation_SingleLine()
        => VerifyKeywordAsync(AddInsideMethod(
            """"
            var x = $"""{$$}"""
            """"));

    [Fact]
    public Task TestInRawStringInterpolation_SingleLine_MultiBrace()
        => VerifyKeywordAsync(AddInsideMethod(
            """"
            var x = ${|#0:|}$"""{{$$}}"""
            """"));

    [Fact]
    public Task TestInRawStringInterpolation_SingleLineIncomplete()
        => VerifyKeywordAsync(AddInsideMethod(
@"var x = $""""""{$$"));

    [Fact]
    public Task TestInRawStringInterpolation_MultiLine()
        => VerifyKeywordAsync(AddInsideMethod(
            """"
            var x = $"""
            {$$}
            """
            """"));

    [Fact]
    public Task TestInRawStringInterpolation_MultiLine_MultiBrace()
        => VerifyKeywordAsync(AddInsideMethod(
            """"
            var x = ${|#0:|}$"""
            {{$$}}
            """
            """"));

    [Fact]
    public Task TestInRawStringInterpolation_MultiLineIncomplete()
        => VerifyKeywordAsync(AddInsideMethod(
            """"
            var x = $"""
            {$$
            """"));

    #region Collection expressions

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public Task TestInCollectionExpressions_BeforeFirstElementToVar()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            var x = [$$
            """));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public Task TestInCollectionExpressions_BeforeFirstElementToReturn()
        => VerifyKeywordAsync(
            """
            class C
            {
                IEnumerable<string> M() => [$$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public Task TestInCollectionExpressions_AfterFirstElementToVar()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            var x = [new object(), $$
            """));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public Task TestInCollectionExpressions_AfterFirstElementToReturn()
        => VerifyKeywordAsync(
            """
            class C
            {
                IEnumerable<string> M() => [string.Empty, $$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public Task TestInCollectionExpressions_SpreadBeforeFirstElementToReturn()
        => VerifyKeywordAsync(
            """
            class C
            {
                IEnumerable<string> M() => [.. $$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public Task TestInCollectionExpressions_SpreadAfterFirstElementToReturn()
        => VerifyKeywordAsync(
            """
            class C
            {
                IEnumerable<string> M() => [string.Empty, .. $$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public Task TestInCollectionExpressions_ParenAtFirstElementToReturn()
        => VerifyKeywordAsync(
            """
            class C
            {
                IEnumerable<string> M() => [($$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public Task TestInCollectionExpressions_ParenAfterFirstElementToReturn()
        => VerifyKeywordAsync(
            """
            class C
            {
                IEnumerable<string> M() => [string.Empty, ($$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public Task TestInCollectionExpressions_ParenSpreadAtFirstElementToReturn()
        => VerifyKeywordAsync(
            """
            class C
            {
                IEnumerable<string> M() => [.. ($$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70677")]
    public Task TestInCollectionExpressions_ParenSpreadAfterFirstElementToReturn()
        => VerifyKeywordAsync(
            """
            class C
            {
                IEnumerable<string> M() => [string.Empty, .. ($$
            }
            """);

    #endregion

    [Fact]
    public Task TestWithinExtension()
        => VerifyAbsenceAsync(
            """
            static class C
            {
                extension(string s)
                {
                    $$
                }
            }
            """,
            CSharpNextParseOptions,
            CSharpNextScriptParseOptions);
}
