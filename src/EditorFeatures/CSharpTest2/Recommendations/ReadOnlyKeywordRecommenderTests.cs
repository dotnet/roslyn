// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class ReadOnlyKeywordRecommenderTests : KeywordRecommenderTests
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

    [Fact]
    public Task TestNotInEmptyStatement()
        => VerifyAbsenceAsync(AddInsideMethod(
@"$$"));

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
        => VerifyKeywordAsync("""
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
        => VerifyKeywordAsync(
            """
            namespace N;
            $$
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66319")]
    public Task TestFileKeywordInsideNamespace()
        => VerifyKeywordAsync(
            """
            namespace N {
            file $$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66319")]
    public Task TestFileKeywordInsideNamespaceBeforeClass()
        => VerifyKeywordAsync(
            """
            namespace N {
            file $$
            class C {}
            }
            """);

    [Fact]
    public Task TestAfterTypeDeclaration()
        => VerifyKeywordAsync("""
            class C {}
            $$
            """);

    [Fact]
    public Task TestAfterDelegateDeclaration()
        => VerifyKeywordAsync("""
            delegate void Goo();
            $$
            """);

    [Fact]
    public Task TestAfterMethod()
        => VerifyKeywordAsync(
            """
            class C {
              void Goo() {}
              $$
            """);

    [Fact]
    public Task TestAfterField()
        => VerifyKeywordAsync(
            """
            class C {
              int i;
              $$
            """);

    [Fact]
    public Task TestAfterProperty()
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

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/9880"), Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
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

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/9880"), Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
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
        => VerifyKeywordAsync("""
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
        => VerifyKeywordAsync("""
            interface I {
               $$
            """);

    [Fact]
    public Task TestNotInsideEnum()
        => VerifyAbsenceAsync("""
            enum E {
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
    public async Task TestAfterAbstract()
        => await VerifyKeywordAsync(@"abstract $$");

    [Fact]
    public async Task TestAfterInternal()
        => await VerifyKeywordAsync(@"internal $$");

    [Fact]
    public Task TestAfterNestedInternal()
        => VerifyKeywordAsync(
            """
            class C {
                internal $$
            """);

    [Fact]
    public async Task TestAfterPublic()
        => await VerifyKeywordAsync(@"public $$");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66319")]
    public async Task TestAfterFile()
        => await VerifyKeywordAsync(SourceCodeKind.Regular, @"file $$");

    [Fact]
    public Task TestAfterNestedPublic()
        => VerifyKeywordAsync(
            """
            class C {
                public $$
            """);

    [Fact]
    public Task TestAfterPrivate()
        => VerifyKeywordAsync(@"private $$");

    [Fact]
    public Task TestAfterNestedPrivate()
        => VerifyKeywordAsync(
            """
            class C {
                private $$
            """);

    [Fact]
    public Task TestAfterProtected()
        => VerifyKeywordAsync(
@"protected $$");

    [Fact]
    public Task TestAfterNestedProtected()
        => VerifyKeywordAsync(
            """
            class C {
                protected $$
            """);

    [Fact]
    public async Task TestAfterSealed()
        => await VerifyKeywordAsync(@"sealed $$");

    [Fact]
    public Task TestAfterNestedSealed()
        => VerifyKeywordAsync(
            """
            class C {
                sealed $$
            """);

    [Fact]
    public async Task TestAfterStatic()
        => await VerifyKeywordAsync(@"static $$");

    [Fact]
    public Task TestAfterNestedStatic()
        => VerifyKeywordAsync(
            """
            class C {
                static $$
            """);

    [Fact]
    public async Task TestAfterStaticPublic()
        => await VerifyKeywordAsync(@"static public $$");

    [Fact]
    public Task TestAfterNestedStaticPublic()
        => VerifyKeywordAsync(
            """
            class C {
                static public $$
            """);

    [Fact]
    public async Task TestNotAfterDelegate()
        => await VerifyAbsenceAsync(@"delegate $$");

    [Fact]
    public Task TestNotAfterEvent()
        => VerifyAbsenceAsync(
            """
            class C {
                event $$
            """);

    [Fact]
    public Task TestNotAfterConst()
        => VerifyAbsenceAsync(
            """
            class C {
                const $$
            """);

    [Fact]
    public Task TestNotAfterPublicReadOnly()
        => VerifyAbsenceAsync(
            """
            class C {
                public readonly $$
            """);

    [Fact]
    public Task TestNotAfterReadOnlyPartial()
        => VerifyAbsenceAsync(
            """
            class C {
                readonly partial $$
            """);

    [Fact]
    public Task TestNotAfterReadOnly()
        => VerifyAbsenceAsync(
            """
            class C {
                readonly $$
            """);

    [Fact]
    public Task TestNotAfterVolatile()
        => VerifyAbsenceAsync(
            """
            class C {
                volatile $$
            """);

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
    public Task TestAfterNestedNew()
        => VerifyKeywordAsync(
            """
            class C {
               new $$
            """);

    [Fact]
    public Task TestNotInMethod()
        => VerifyAbsenceAsync(
            """
            class C {
               void Goo() {
                 $$
            """);

    [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    public Task TestRefReadonlyAsParameterModifierInMethods()
        => VerifyKeywordAsync("""
            class Program
            {
                public static void Test(ref $$ p) { }
            }
            """);

    [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    public Task TestRefReadonlyAsParameterModifierInSecondParameter()
        => VerifyKeywordAsync("""
            class Program
            {
                public static void Test(int p1, ref $$ p2) { }
            }
            """);

    [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    public Task TestRefReadonlyAsParameterModifierInDelegates()
        => VerifyKeywordAsync("""
            public delegate int Delegate(ref $$ int p);
            """);

    [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
    [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
    [CombinatorialData]
    public Task TestRefReadonlyAsParameterModifierInLocalFunctions(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"void localFunc(ref $$ int p) { }", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    public Task TestRefReadonlyAsParameterModifierInLambdaExpressions()
        => VerifyKeywordAsync("""
            public delegate int Delegate(ref int p);

            class Program
            {
                public static void Test()
                {
                    Delegate lambda = (ref $$ int p) => p;
                }
            }
            """);

    [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    public Task TestRefReadonlyAsParameterModifierInAnonymousMethods()
        => VerifyKeywordAsync("""
            public delegate int Delegate(ref int p);

            class Program
            {
                public static void Test()
                {
                    Delegate anonymousDelegate = delegate (ref $$ int p) { return p; };
                }
            }
            """);

    [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    public Task TestRefReadonlyAsModifierInMethodReturnTypes()
        => VerifyKeywordAsync("""
            class Program
            {
                public ref $$ int Test()
                {
                    return ref x;
                }
            }
            """);

    [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    public Task TestRefReadonlyAsModifierInGlobalMemberDeclaration()
        => VerifyKeywordAsync("""
            public ref $$
            """);

    [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    public Task TestRefReadonlyAsModifierInDelegateReturnType()
        => VerifyKeywordAsync("""
            public delegate ref $$ int Delegate();

            class Program
            {
            }
            """);

    [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    public Task TestRefReadonlyAsModifierInMemberDeclaration()
        => VerifyKeywordAsync("""
            class Program
            {
                public ref $$ int Test { get; set; }
            }
            """);

    [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
    [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/25569")]
    [CombinatorialData]
    public Task TestRefReadonlyInStatementContext(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"ref $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
    [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
    [CombinatorialData]
    public Task TestRefReadonlyInLocalDeclaration(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"ref $$ int local;", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
    [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
    [CombinatorialData]
    public Task TestRefReadonlyInLocalFunction(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"ref $$ int Function();", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
    [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
    [CombinatorialData]
    public Task TestRefReadonlyNotInRefExpression(bool topLevelStatement)
        => VerifyAbsenceAsync(AddInsideMethod(
@"ref int x = ref $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions,
            // Top level statement with script tested below, so skip it here.
            scriptOptions: topLevelStatement ? CSharp9ParseOptions : null);

    [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
    [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
    public Task TestRefReadonlyInRefExpression_TopLevelStatementScript()
        => VerifyKeywordAsync(AddInsideMethod(
@"ref int x = ref $$", topLevelStatement: true), options: CSharp9ParseOptions.WithKind(SourceCodeKind.Script));

    [Fact]
    public Task TestInFunctionPointerTypeAfterRef()
        => VerifyKeywordAsync("""
            class C
            {
                delegate*<ref $$
            """);

    [Fact]
    public Task TestNotInFunctionPointerTypeWithoutRef()
        => VerifyAbsenceAsync("""
            class C
            {
                delegate*<$$
            """);

    [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    [InlineData("in")]
    [InlineData("out")]
    [InlineData("ref readonly")]
    public Task TestNotInFunctionPointerTypeAfterOtherRefModifier(string modifier)
        => VerifyAbsenceAsync($$"""
            class C
            {
                delegate*<{{modifier}} $$
            """);

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
            """, CSharpNextParseOptions);
}
