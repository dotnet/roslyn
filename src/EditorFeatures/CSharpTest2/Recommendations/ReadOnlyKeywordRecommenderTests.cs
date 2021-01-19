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
    public class ReadOnlyKeywordRecommenderTests : KeywordRecommenderTests
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

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInEmptyStatement()
        {
            VerifyAbsence(AddInsideMethod(
@"$$"));
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
        public async Task TestAfterTypeDeclaration()
        {
            VerifyKeyword(@"class C {}
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterDelegateDeclaration()
        {
            VerifyKeyword(@"delegate void Goo();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterMethod()
        {
            VerifyKeyword(
@"class C {
  void Goo() {}
  $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterField()
        {
            VerifyKeyword(
@"class C {
  int i;
  $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterProperty()
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
        public async Task TestAfterRootAttribute()
        {
            VerifyKeyword(@"[goo]
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
            VerifyKeyword(@"interface I {
   $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInsideEnum()
        {
            VerifyAbsence(@"enum E {
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
        public async Task TestAfterPartial()
            => VerifyKeyword(@"partial $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterAbstract()
            => VerifyKeyword(@"abstract $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterInternal()
            => VerifyKeyword(@"internal $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedInternal()
        {
            VerifyKeyword(
@"class C {
    internal $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPublic()
            => VerifyKeyword(@"public $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedPublic()
        {
            VerifyKeyword(
@"class C {
    public $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPrivate()
        {
            VerifyKeyword(@"private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedPrivate()
        {
            VerifyKeyword(
@"class C {
    private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterProtected()
        {
            VerifyKeyword(
@"protected $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedProtected()
        {
            VerifyKeyword(
@"class C {
    protected $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterSealed()
            => VerifyKeyword(@"sealed $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedSealed()
        {
            VerifyKeyword(
@"class C {
    sealed $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterStatic()
            => VerifyKeyword(@"static $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedStatic()
        {
            VerifyKeyword(
@"class C {
    static $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterStaticPublic()
            => VerifyKeyword(@"static public $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedStaticPublic()
        {
            VerifyKeyword(
@"class C {
    static public $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterDelegate()
            => VerifyAbsence(@"delegate $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterEvent()
        {
            VerifyAbsence(
@"class C {
    event $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterConst()
        {
            VerifyAbsence(
@"class C {
    const $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterReadOnly()
        {
            VerifyKeyword(
@"class C {
    readonly $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterVolatile()
        {
            VerifyAbsence(
@"class C {
    volatile $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterRef()
            => VerifyKeyword(@"ref $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInRefStruct()
            => VerifyKeyword(@"ref $$ struct { }");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInRefStructBeforeRef()
            => VerifyKeyword(@"$$ ref struct { }");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(44423, "https://github.com/dotnet/roslyn/issues/44423")]
        public async Task TestAfterNew()
            => VerifyAbsence(@"new $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNewInClass()
            => VerifyKeyword(@"class C { new $$ }");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedNew()
        {
            VerifyKeyword(
@"class C {
   new $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInMethod()
        {
            VerifyAbsence(
@"class C {
   void Goo() {
     $$");
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestRefReadonlyNotAsParameterModifierInMethods()
        {
            VerifyAbsence(@"
class Program
{
    public static void Test(ref $$ p) { }
}");
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestRefReadonlyNotAsParameterModifierInSecondParameter()
        {
            VerifyAbsence(@"
class Program
{
    public static void Test(int p1, ref $$ p2) { }
}");
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestRefReadonlyNotAsParameterModifierInDelegates()
        {
            VerifyAbsence(@"
public delegate int Delegate(ref $$ int p);");
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        [CombinatorialData]
        public async Task TestRefReadonlyNotAsParameterModifierInLocalFunctions(bool topLevelStatement)
        {
            VerifyAbsence(AddInsideMethod(
@"void localFunc(ref $$ int p) { }", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestRefReadonlyNotAsParameterModifierInLambdaExpressions()
        {
            VerifyAbsence(@"
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

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestRefReadonlyNotAsParameterModifierInAnonymousMethods()
        {
            VerifyAbsence(@"
public delegate int Delegate(ref int p);

class Program
{
    public static void Test()
    {
        Delegate anonymousDelegate = delegate (ref $$ int p) { return p; };
    }
}");
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestRefReadonlyAsModifierInMethodReturnTypes()
        {
            VerifyKeyword(@"
class Program
{
    public ref $$ int Test()
    {
        return ref x;
    }
}");
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestRefReadonlyAsModifierInGlobalMemberDeclaration()
        {
            VerifyKeyword(@"
public ref $$ ");
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestRefReadonlyAsModifierInDelegateReturnType()
        {
            VerifyKeyword(@"
public delegate ref $$ int Delegate();

class Program
{
}");
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestRefReadonlyAsModifierInMemberDeclaration()
        {
            VerifyKeyword(@"
class Program
{
    public ref $$ int Test { get; set; }
}");
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem(25569, "https://github.com/dotnet/roslyn/issues/25569")]
        [CombinatorialData]
        public async Task TestRefReadonlyInStatementContext(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"ref $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        [CombinatorialData]
        public async Task TestRefReadonlyInLocalDeclaration(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"ref $$ int local;", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        [CombinatorialData]
        public async Task TestRefReadonlyInLocalFunction(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"ref $$ int Function();", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        [CombinatorialData]
        public async Task TestRefReadonlyNotInRefExpression(bool topLevelStatement)
        {
            VerifyAbsence(AddInsideMethod(
@"ref int x = ref $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInFunctionPointerTypeAfterRef()
        {
            VerifyKeyword(@"
class C
{
    delegate*<ref $$");
        }

        [Fact]
        public async Task TestNotInFunctionPointerTypeWithoutRef()
        {
            VerifyAbsence(@"
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
            VerifyAbsence($@"
class C
{{
    delegate*<{modifier} $$");
        }
    }
}
