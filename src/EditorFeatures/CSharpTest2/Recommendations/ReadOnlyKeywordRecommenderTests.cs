// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class ReadOnlyKeywordRecommenderTests : KeywordRecommenderTests
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
        public async Task TestNotInEmptyStatement()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"$$"));
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
        public async Task TestAfterNamespace()
        {
            await VerifyKeywordAsync(@"namespace N {}
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterTypeDeclaration()
        {
            await VerifyKeywordAsync(@"class C {}
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterDelegateDeclaration()
        {
            await VerifyKeywordAsync(@"delegate void Goo();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterMethod()
        {
            await VerifyKeywordAsync(
@"class C {
  void Goo() {}
  $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterField()
        {
            await VerifyKeywordAsync(
@"class C {
  int i;
  $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterProperty()
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
        public async Task TestAfterAssemblyAttribute()
        {
            await VerifyKeywordAsync(@"[assembly: goo]
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterRootAttribute()
        {
            await VerifyKeywordAsync(@"[goo]
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
            await VerifyKeywordAsync(@"interface I {
   $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInsideEnum()
        {
            await VerifyAbsenceAsync(@"enum E {
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
        public async Task TestAfterPartial()
            => await VerifyKeywordAsync(@"partial $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterAbstract()
            => await VerifyKeywordAsync(@"abstract $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterInternal()
            => await VerifyKeywordAsync(@"internal $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedInternal()
        {
            await VerifyKeywordAsync(
@"class C {
    internal $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPublic()
            => await VerifyKeywordAsync(@"public $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedPublic()
        {
            await VerifyKeywordAsync(
@"class C {
    public $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPrivate()
        {
            await VerifyKeywordAsync(@"private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedPrivate()
        {
            await VerifyKeywordAsync(
@"class C {
    private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterProtected()
        {
            await VerifyKeywordAsync(
@"protected $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedProtected()
        {
            await VerifyKeywordAsync(
@"class C {
    protected $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterSealed()
            => await VerifyKeywordAsync(@"sealed $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedSealed()
        {
            await VerifyKeywordAsync(
@"class C {
    sealed $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterStatic()
            => await VerifyKeywordAsync(@"static $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedStatic()
        {
            await VerifyKeywordAsync(
@"class C {
    static $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterStaticPublic()
            => await VerifyKeywordAsync(@"static public $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedStaticPublic()
        {
            await VerifyKeywordAsync(
@"class C {
    static public $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterDelegate()
            => await VerifyAbsenceAsync(@"delegate $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterEvent()
        {
            await VerifyAbsenceAsync(
@"class C {
    event $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterConst()
        {
            await VerifyAbsenceAsync(
@"class C {
    const $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterReadOnly()
        {
            await VerifyKeywordAsync(
@"class C {
    readonly $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterVolatile()
        {
            await VerifyAbsenceAsync(
@"class C {
    volatile $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterRef()
            => await VerifyKeywordAsync(@"ref $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInRefStruct()
            => await VerifyKeywordAsync(@"ref $$ struct { }");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInRefStructBeforeRef()
            => await VerifyKeywordAsync(@"$$ ref struct { }");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(44423, "https://github.com/dotnet/roslyn/issues/44423")]
        public async Task TestAfterNew()
            => await VerifyAbsenceAsync(@"new $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNewInClass()
            => await VerifyKeywordAsync(@"class C { new $$ }");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedNew()
        {
            await VerifyKeywordAsync(
@"class C {
   new $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInMethod()
        {
            await VerifyAbsenceAsync(
@"class C {
   void Goo() {
     $$");
        }

        [Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.ReadOnlyReferences)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestRefReadonlyNotAsParameterModifierInMethods()
        {
            await VerifyAbsenceAsync(@"
class Program
{
    public static void Test(ref $$ p) { }
}");
        }

        [Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.ReadOnlyReferences)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestRefReadonlyNotAsParameterModifierInSecondParameter()
        {
            await VerifyAbsenceAsync(@"
class Program
{
    public static void Test(int p1, ref $$ p2) { }
}");
        }

        [Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.ReadOnlyReferences)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestRefReadonlyNotAsParameterModifierInDelegates()
        {
            await VerifyAbsenceAsync(@"
public delegate int Delegate(ref $$ int p);");
        }

        [Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.ReadOnlyReferences)]
        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        [CombinatorialData]
        public async Task TestRefReadonlyNotAsParameterModifierInLocalFunctions(bool topLevelStatement)
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"void localFunc(ref $$ int p) { }", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.ReadOnlyReferences)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestRefReadonlyNotAsParameterModifierInLambdaExpressions()
        {
            await VerifyAbsenceAsync(@"
public delegate int Delegate(ref int p);

class Program
{
    public static void Test()
    {
        // This is bad. We can't put 'ref $ int p' like in the other tests here because in this scenario:
        // 'Delegate lambda = (ref r int p) => p;' (partially written 'readonly' keyword),
        // the syntax tree is completely broken and there is no lambda expression at all here.
        // 'ref' starts a new local declaration and therefore we do offer 'readonly'.
        // Fixing that would have to involve either changing the parser or doing some really nasty hacks.
        // Delegate lambda = (ref $$ int p) => p;
    }
}");
        }

        [Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.ReadOnlyReferences)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestRefReadonlyNotAsParameterModifierInAnonymousMethods()
        {
            await VerifyAbsenceAsync(@"
public delegate int Delegate(ref int p);

class Program
{
    public static void Test()
    {
        Delegate anonymousDelegate = delegate (ref $$ int p) { return p; };
    }
}");
        }

        [Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.ReadOnlyReferences)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestRefReadonlyAsModifierInMethodReturnTypes()
        {
            await VerifyKeywordAsync(@"
class Program
{
    public ref $$ int Test()
    {
        return ref x;
    }
}");
        }

        [Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.ReadOnlyReferences)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestRefReadonlyAsModifierInGlobalMemberDeclaration()
        {
            await VerifyKeywordAsync(@"
public ref $$ ");
        }

        [Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.ReadOnlyReferences)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestRefReadonlyAsModifierInDelegateReturnType()
        {
            await VerifyKeywordAsync(@"
public delegate ref $$ int Delegate();

class Program
{
}");
        }

        [Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.ReadOnlyReferences)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestRefReadonlyAsModifierInMemberDeclaration()
        {
            await VerifyKeywordAsync(@"
class Program
{
    public ref $$ int Test { get; set; }
}");
        }

        [Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.ReadOnlyReferences)]
        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(25569, "https://github.com/dotnet/roslyn/issues/25569")]
        [CombinatorialData]
        public async Task TestRefReadonlyInStatementContext(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"ref $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.ReadOnlyReferences)]
        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        [CombinatorialData]
        public async Task TestRefReadonlyInLocalDeclaration(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"ref $$ int local;", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.ReadOnlyReferences)]
        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        [CombinatorialData]
        public async Task TestRefReadonlyInLocalFunction(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"ref $$ int Function();", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.ReadOnlyReferences)]
        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        [CombinatorialData]
        public async Task TestRefReadonlyNotInRefExpression(bool topLevelStatement)
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"ref int x = ref $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInFunctionPointerTypeAfterRef()
        {
            await VerifyKeywordAsync(@"
class C
{
    delegate*<ref $$");
        }

        [Fact]
        public async Task TestNotInFunctionPointerTypeWithoutRef()
        {
            await VerifyAbsenceAsync(@"
class C
{
    delegate*<$$");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [InlineData("in")]
        [InlineData("out")]
        [InlineData("ref readonly")]
        public async Task TestNotInFunctionPointerTypeAfterOtherRefModifier(string modifier)
        {
            await VerifyAbsenceAsync($@"
class C
{{
    delegate*<{modifier} $$");
        }
    }
}
