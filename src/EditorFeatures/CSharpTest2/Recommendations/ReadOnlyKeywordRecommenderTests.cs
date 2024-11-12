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
    public class ReadOnlyKeywordRecommenderTests : KeywordRecommenderTests
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

        [Fact]
        public async Task TestNotInEmptyStatement()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"$$"));
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
            await VerifyKeywordAsync("""
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
            await VerifyKeywordAsync(
                """
                namespace N;
                $$
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66319")]
        public async Task TestFileKeywordInsideNamespace()
        {
            await VerifyKeywordAsync(
                """
                namespace N {
                file $$
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66319")]
        public async Task TestFileKeywordInsideNamespaceBeforeClass()
        {
            await VerifyKeywordAsync(
                """
                namespace N {
                file $$
                class C {}
                }
                """);
        }

        [Fact]
        public async Task TestAfterTypeDeclaration()
        {
            await VerifyKeywordAsync("""
                class C {}
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
        public async Task TestAfterMethod()
        {
            await VerifyKeywordAsync(
                """
                class C {
                  void Goo() {}
                  $$
                """);
        }

        [Fact]
        public async Task TestAfterField()
        {
            await VerifyKeywordAsync(
                """
                class C {
                  int i;
                  $$
                """);
        }

        [Fact]
        public async Task TestAfterProperty()
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

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/9880"), Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
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

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/9880"), Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
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
            await VerifyKeywordAsync("""
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
            await VerifyKeywordAsync("""
                interface I {
                   $$
                """);
        }

        [Fact]
        public async Task TestNotInsideEnum()
        {
            await VerifyAbsenceAsync("""
                enum E {
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
        public async Task TestAfterAbstract()
            => await VerifyKeywordAsync(@"abstract $$");

        [Fact]
        public async Task TestAfterInternal()
            => await VerifyKeywordAsync(@"internal $$");

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
        public async Task TestAfterPublic()
            => await VerifyKeywordAsync(@"public $$");

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66319")]
        public async Task TestAfterFile()
            => await VerifyKeywordAsync(SourceCodeKind.Regular, @"file $$");

        [Fact]
        public async Task TestAfterNestedPublic()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    public $$
                """);
        }

        [Fact]
        public async Task TestAfterPrivate()
        {
            await VerifyKeywordAsync(@"private $$");
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
        public async Task TestAfterProtected()
        {
            await VerifyKeywordAsync(
@"protected $$");
        }

        [Fact]
        public async Task TestAfterNestedProtected()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    protected $$
                """);
        }

        [Fact]
        public async Task TestAfterSealed()
            => await VerifyKeywordAsync(@"sealed $$");

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
        public async Task TestAfterStatic()
            => await VerifyKeywordAsync(@"static $$");

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
        public async Task TestAfterStaticPublic()
            => await VerifyKeywordAsync(@"static public $$");

        [Fact]
        public async Task TestAfterNestedStaticPublic()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    static public $$
                """);
        }

        [Fact]
        public async Task TestNotAfterDelegate()
            => await VerifyAbsenceAsync(@"delegate $$");

        [Fact]
        public async Task TestNotAfterEvent()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    event $$
                """);
        }

        [Fact]
        public async Task TestNotAfterConst()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    const $$
                """);
        }

        [Fact]
        public async Task TestNotAfterPublicReadOnly()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    public readonly $$
                """);
        }

        [Fact]
        public async Task TestNotAfterReadOnlyPartial()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    readonly partial $$
                """);
        }

        [Fact]
        public async Task TestNotAfterReadOnly()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    readonly $$
                """);
        }

        [Fact]
        public async Task TestNotAfterVolatile()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    volatile $$
                """);
        }

        [Fact]
        public async Task TestAfterRef()
            => await VerifyKeywordAsync(@"ref $$");

        [Fact]
        public async Task TestInRefStruct()
            => await VerifyKeywordAsync(@"ref $$ struct { }");

        [Fact]
        public async Task TestInRefStructBeforeRef()
            => await VerifyKeywordAsync(@"$$ ref struct { }");

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44423")]
        public async Task TestAfterNew()
            => await VerifyAbsenceAsync(@"new $$");

        [Fact]
        public async Task TestAfterNewInClass()
            => await VerifyKeywordAsync(@"class C { new $$ }");

        [Fact]
        public async Task TestAfterNestedNew()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   new $$
                """);
        }

        [Fact]
        public async Task TestNotInMethod()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                   void Goo() {
                     $$
                """);
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestRefReadonlyAsParameterModifierInMethods()
        {
            await VerifyKeywordAsync("""
                class Program
                {
                    public static void Test(ref $$ p) { }
                }
                """);
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestRefReadonlyAsParameterModifierInSecondParameter()
        {
            await VerifyKeywordAsync("""
                class Program
                {
                    public static void Test(int p1, ref $$ p2) { }
                }
                """);
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestRefReadonlyAsParameterModifierInDelegates()
        {
            await VerifyKeywordAsync("""
                public delegate int Delegate(ref $$ int p);
                """);
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        [CombinatorialData]
        public async Task TestRefReadonlyAsParameterModifierInLocalFunctions(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"void localFunc(ref $$ int p) { }", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestRefReadonlyAsParameterModifierInLambdaExpressions()
        {
            await VerifyKeywordAsync("""
                public delegate int Delegate(ref int p);

                class Program
                {
                    public static void Test()
                    {
                        Delegate lambda = (ref $$ int p) => p;
                    }
                }
                """);
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestRefReadonlyAsParameterModifierInAnonymousMethods()
        {
            await VerifyKeywordAsync("""
                public delegate int Delegate(ref int p);

                class Program
                {
                    public static void Test()
                    {
                        Delegate anonymousDelegate = delegate (ref $$ int p) { return p; };
                    }
                }
                """);
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestRefReadonlyAsModifierInMethodReturnTypes()
        {
            await VerifyKeywordAsync("""
                class Program
                {
                    public ref $$ int Test()
                    {
                        return ref x;
                    }
                }
                """);
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestRefReadonlyAsModifierInGlobalMemberDeclaration()
        {
            await VerifyKeywordAsync("""
                public ref $$
                """);
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestRefReadonlyAsModifierInDelegateReturnType()
        {
            await VerifyKeywordAsync("""
                public delegate ref $$ int Delegate();

                class Program
                {
                }
                """);
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestRefReadonlyAsModifierInMemberDeclaration()
        {
            await VerifyKeywordAsync("""
                class Program
                {
                    public ref $$ int Test { get; set; }
                }
                """);
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        [WorkItem("https://github.com/dotnet/roslyn/issues/25569")]
        [CombinatorialData]
        public async Task TestRefReadonlyInStatementContext(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"ref $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        [CombinatorialData]
        public async Task TestRefReadonlyInLocalDeclaration(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"ref $$ int local;", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        [CombinatorialData]
        public async Task TestRefReadonlyInLocalFunction(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"ref $$ int Function();", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        [CombinatorialData]
        public async Task TestRefReadonlyNotInRefExpression(bool topLevelStatement)
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"ref int x = ref $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions,
                // Top level statement with script tested below, so skip it here.
                scriptOptions: topLevelStatement ? CSharp9ParseOptions : null);
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestRefReadonlyInRefExpression_TopLevelStatementScript()
        {
            // Recognized as parameter context, so readonly keyword is suggested.
            await VerifyKeywordAsync(AddInsideMethod(
@"ref int x = ref $$", topLevelStatement: true), options: CSharp9ParseOptions.WithKind(SourceCodeKind.Script));
        }

        [Fact]
        public async Task TestInFunctionPointerTypeAfterRef()
        {
            await VerifyKeywordAsync("""
                class C
                {
                    delegate*<ref $$
                """);
        }

        [Fact]
        public async Task TestNotInFunctionPointerTypeWithoutRef()
        {
            await VerifyAbsenceAsync("""
                class C
                {
                    delegate*<$$
                """);
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
