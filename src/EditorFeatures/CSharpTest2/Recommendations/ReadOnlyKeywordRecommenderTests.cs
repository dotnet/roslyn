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
        public void TestAtRoot()
        {
            VerifyKeyword(
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterClass()
        {
            VerifyKeyword(
@"class C { }
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterGlobalStatement()
        {
            VerifyKeyword(
@"System.Console.WriteLine();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterGlobalVariableDeclaration()
        {
            VerifyKeyword(
@"int i = 0;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInUsingAlias()
        {
            VerifyAbsence(
@"using Goo = $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInEmptyStatement()
        {
            VerifyAbsence(AddInsideMethod(
@"$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterExtern()
        {
            VerifyKeyword(@"extern alias Goo;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterUsing()
        {
            VerifyKeyword(@"using Goo;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterNamespace()
        {
            VerifyKeyword(@"namespace N {}
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterTypeDeclaration()
        {
            VerifyKeyword(@"class C {}
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterDelegateDeclaration()
        {
            VerifyKeyword(@"delegate void Goo();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterMethod()
        {
            VerifyKeyword(
@"class C {
  void Goo() {}
  $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterField()
        {
            VerifyKeyword(
@"class C {
  int i;
  $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterProperty()
        {
            VerifyKeyword(
@"class C {
  int i { get; }
  $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotBeforeUsing()
        {
            VerifyAbsence(SourceCodeKind.Regular,
@"$$
using Goo;");
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/9880"), Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotBeforeUsing_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"$$
using Goo;");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterAssemblyAttribute()
        {
            VerifyKeyword(@"[assembly: goo]
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterRootAttribute()
        {
            VerifyKeyword(@"[goo]
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterNestedAttribute()
        {
            VerifyKeyword(
@"class C {
  [goo]
  $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInsideStruct()
        {
            VerifyKeyword(
@"struct S {
   $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInsideInterface()
        {
            VerifyKeyword(@"interface I {
   $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInsideEnum()
        {
            VerifyAbsence(@"enum E {
   $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInsideClass()
        {
            VerifyKeyword(
@"class C {
   $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterPartial()
            => VerifyKeyword(@"partial $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterAbstract()
            => VerifyKeyword(@"abstract $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterInternal()
            => VerifyKeyword(@"internal $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterNestedInternal()
        {
            VerifyKeyword(
@"class C {
    internal $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterPublic()
            => VerifyKeyword(@"public $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterNestedPublic()
        {
            VerifyKeyword(
@"class C {
    public $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterPrivate()
        {
            VerifyKeyword(@"private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterNestedPrivate()
        {
            VerifyKeyword(
@"class C {
    private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterProtected()
        {
            VerifyKeyword(
@"protected $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterNestedProtected()
        {
            VerifyKeyword(
@"class C {
    protected $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterSealed()
            => VerifyKeyword(@"sealed $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterNestedSealed()
        {
            VerifyKeyword(
@"class C {
    sealed $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterStatic()
            => VerifyKeyword(@"static $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterNestedStatic()
        {
            VerifyKeyword(
@"class C {
    static $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterStaticPublic()
            => VerifyKeyword(@"static public $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterNestedStaticPublic()
        {
            VerifyKeyword(
@"class C {
    static public $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterDelegate()
            => VerifyAbsence(@"delegate $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterEvent()
        {
            VerifyAbsence(
@"class C {
    event $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterConst()
        {
            VerifyAbsence(
@"class C {
    const $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterReadOnly()
        {
            VerifyKeyword(
@"class C {
    readonly $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterVolatile()
        {
            VerifyAbsence(
@"class C {
    volatile $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterRef()
            => VerifyKeyword(@"ref $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInRefStruct()
            => VerifyKeyword(@"ref $$ struct { }");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInRefStructBeforeRef()
            => VerifyKeyword(@"$$ ref struct { }");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(44423, "https://github.com/dotnet/roslyn/issues/44423")]
        public void TestAfterNew()
            => VerifyAbsence(@"new $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterNewInClass()
            => VerifyKeyword(@"class C { new $$ }");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterNestedNew()
        {
            VerifyKeyword(
@"class C {
   new $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInMethod()
        {
            VerifyAbsence(
@"class C {
   void Goo() {
     $$");
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TestRefReadonlyNotAsParameterModifierInMethods()
        {
            VerifyAbsence(@"
class Program
{
    public static void Test(ref $$ p) { }
}");
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TestRefReadonlyNotAsParameterModifierInSecondParameter()
        {
            VerifyAbsence(@"
class Program
{
    public static void Test(int p1, ref $$ p2) { }
}");
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TestRefReadonlyNotAsParameterModifierInDelegates()
        {
            VerifyAbsence(@"
public delegate int Delegate(ref $$ int p);");
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        [CombinatorialData]
        public void TestRefReadonlyNotAsParameterModifierInLocalFunctions(bool topLevelStatement)
        {
            VerifyAbsence(AddInsideMethod(
@"void localFunc(ref $$ int p) { }", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TestRefReadonlyNotAsParameterModifierInLambdaExpressions()
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
        public void TestRefReadonlyNotAsParameterModifierInAnonymousMethods()
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
        public void TestRefReadonlyAsModifierInMethodReturnTypes()
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
        public void TestRefReadonlyAsModifierInGlobalMemberDeclaration()
        {
            VerifyKeyword(@"
public ref $$ ");
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TestRefReadonlyAsModifierInDelegateReturnType()
        {
            VerifyKeyword(@"
public delegate ref $$ int Delegate();

class Program
{
}");
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void TestRefReadonlyAsModifierInMemberDeclaration()
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
        public void TestRefReadonlyInStatementContext(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"ref $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        [CombinatorialData]
        public void TestRefReadonlyInLocalDeclaration(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"ref $$ int local;", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        [CombinatorialData]
        public void TestRefReadonlyInLocalFunction(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"ref $$ int Function();", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        [CombinatorialData]
        public void TestRefReadonlyNotInRefExpression(bool topLevelStatement)
        {
            VerifyAbsence(AddInsideMethod(
@"ref int x = ref $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestInFunctionPointerTypeAfterRef()
        {
            VerifyKeyword(@"
class C
{
    delegate*<ref $$");
        }

        [Fact]
        public void TestNotInFunctionPointerTypeWithoutRef()
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
        public void TestNotInFunctionPointerTypeAfterOtherRefModifier(string modifier)
        {
            VerifyAbsence($@"
class C
{{
    delegate*<{modifier} $$");
        }
    }
}
