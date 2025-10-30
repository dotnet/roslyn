// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class RefKeywordRecommenderTests : KeywordRecommenderTests
{
    [Fact]
    public Task TestAtRoot()
        => VerifyKeywordAsync(
@"$$");

    [Fact]
    public Task TestNotAfterClass()
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
    public Task TestNotAfterAngle()
        => VerifyAbsenceAsync(
@"interface IGoo<$$");

    [Fact]
    public Task TestInterfaceTypeVarianceNotAfterIn()
        => VerifyAbsenceAsync(
@"interface IGoo<in $$");

    [Fact]
    public Task TestInterfaceTypeVarianceNotAfterComma()
        => VerifyAbsenceAsync(
@"interface IGoo<Goo, $$");

    [Fact]
    public Task TestInterfaceTypeVarianceNotAfterAttribute()
        => VerifyAbsenceAsync(
@"interface IGoo<[Goo]$$");

    [Fact]
    public Task TestDelegateTypeVarianceNotAfterAngle()
        => VerifyAbsenceAsync(
@"delegate void D<$$");

    [Fact]
    public Task TestDelegateTypeVarianceNotAfterComma()
        => VerifyAbsenceAsync(
@"delegate void D<Goo, $$");

    [Fact]
    public Task TestDelegateTypeVarianceNotAfterAttribute()
        => VerifyAbsenceAsync(
@"delegate void D<[Goo]$$");

    [Fact]
    public Task TestNotRefBaseListAfterAngle()
        => VerifyAbsenceAsync(
@"interface IGoo : Bar<$$");

    [Fact]
    public Task TestNotInGenericMethod()
        => VerifyAbsenceAsync(
            """
            interface IGoo {
                void Goo<$$
            """);

    [Fact]
    public Task TestNotAfterRef()
        => VerifyAbsenceAsync(
            """
            class C {
                void Goo(ref $$
            """);

    [Fact]
    public Task TestNotAfterOut()
        => VerifyAbsenceAsync(
            """
            class C {
                void Goo(out $$
            """);

    [Fact]
    public Task TestAfterMethodOpenParen()
        => VerifyKeywordAsync(
            """
            class C {
                void Goo($$
            """);

    [Fact]
    public Task TestAfterMethodComma()
        => VerifyKeywordAsync(
            """
            class C {
                void Goo(int i, $$
            """);

    [Fact]
    public Task TestAfterMethodAttribute()
        => VerifyKeywordAsync(
            """
            class C {
                void Goo(int i, [Goo]$$
            """);

    [Fact]
    public Task TestAfterConstructorOpenParen()
        => VerifyKeywordAsync(
            """
            class C {
                public C($$
            """);

    [Fact]
    public Task TestAfterConstructorComma()
        => VerifyKeywordAsync(
            """
            class C {
                public C(int i, $$
            """);

    [Fact]
    public Task TestAfterConstructorAttribute()
        => VerifyKeywordAsync(
            """
            class C {
                public C(int i, [Goo]$$
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/933972")]
    public Task TestAfterThisConstructorInitializer()
        => VerifyKeywordAsync(
            """
            class C {
                public C():this($$
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/933972")]
    public Task TestAfterThisConstructorInitializerNamedArgument()
        => VerifyKeywordAsync(
            """
            class C {
                public C():this(Goo:$$
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/933972")]
    public Task TestAfterBaseConstructorInitializer()
        => VerifyKeywordAsync(
            """
            class C {
                public C():base($$
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/933972")]
    public Task TestAfterBaseConstructorInitializerNamedArgument()
        => VerifyKeywordAsync(
            """
            class C {
                public C():base(5, Goo:$$
            """);

    [Fact]
    public Task TestAfterDelegateOpenParen()
        => VerifyKeywordAsync(
@"delegate void D($$");

    [Fact]
    public Task TestAfterDelegateComma()
        => VerifyKeywordAsync(
@"delegate void D(int i, $$");

    [Fact]
    public Task TestAfterDelegateAttribute()
        => VerifyKeywordAsync(
@"delegate void D(int i, [Goo]$$");

    [Fact]
    public Task TestNotAfterOperator()
        => VerifyAbsenceAsync(
            """
            class C {
                static int operator +($$
            """);

    [Fact]
    public Task TestNotAfterDestructor()
        => VerifyAbsenceAsync(
            """
            class C {
                ~C($$
            """);

    [Fact]
    public Task TestNotAfterIndexer()
        => VerifyAbsenceAsync(
            """
            class C {
                int this[$$
            """);

    [Fact]
    public Task TestInObjectCreationAfterOpenParen()
        => VerifyKeywordAsync(
            """
            class C {
                void Goo() {
                  new Bar($$
            """);

    [Fact]
    public Task TestNotAfterRefParam()
        => VerifyAbsenceAsync(
            """
            class C {
                void Goo() {
                  new Bar(ref $$
            """);

    [Fact]
    public Task TestNotAfterOutParam()
        => VerifyAbsenceAsync(
            """
            class C {
                void Goo() {
                  new Bar(out $$
            """);

    [Fact]
    public Task TestInObjectCreationAfterComma()
        => VerifyKeywordAsync(
            """
            class C {
                void Goo() {
                  new Bar(baz, $$
            """);

    [Fact]
    public Task TestInObjectCreationAfterSecondComma()
        => VerifyKeywordAsync(
            """
            class C {
                void Goo() {
                  new Bar(baz, quux, $$
            """);

    [Fact]
    public Task TestInObjectCreationAfterSecondNamedParam()
        => VerifyKeywordAsync(
            """
            class C {
                void Goo() {
                  new Bar(baz: 4, quux: $$
            """);

    [Fact]
    public Task TestInInvocationExpression()
        => VerifyKeywordAsync(
            """
            class C {
                void Goo() {
                  Bar($$
            """);

    [Fact]
    public Task TestInInvocationAfterComma()
        => VerifyKeywordAsync(
            """
            class C {
                void Goo() {
                  Bar(baz, $$
            """);

    [Fact]
    public Task TestInInvocationAfterSecondComma()
        => VerifyKeywordAsync(
            """
            class C {
                void Goo() {
                  Bar(baz, quux, $$
            """);

    [Fact]
    public Task TestInInvocationAfterSecondNamedParam()
        => VerifyKeywordAsync(
            """
            class C {
                void Goo() {
                  Bar(baz: 4, quux: $$
            """);

    [Theory, CombinatorialData]
    public Task TestInLambdaDeclaration(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"var q = ($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestInLambdaDeclaration2(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"var q = (ref int a, $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestInLambdaDeclaration3(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"var q = (int a, $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestInDelegateDeclaration(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"var q = delegate ($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestInDelegateDeclaration2(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"var q = delegate (a, $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestInDelegateDeclaration3(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"var q = delegate (int a, $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Fact]
    public Task TestInCrefParameterList()
        => VerifyKeywordAsync("""
            Class c
            {
                /// <see cref="main($$"/>
                void main(out goo) { }
            }
            """);

    [Theory, CombinatorialData]
    public Task TestEmptyStatement(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"$$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestAfterReturn(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"return $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestInFor(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"for ($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestNotInFor(bool topLevelStatement)
        => VerifyAbsenceAsync(AddInsideMethod(
@"for (var $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestInFor2(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"for ($$;", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestInFor3(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"for ($$;;", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestNotAfterVar(bool topLevelStatement)
        => VerifyAbsenceAsync(AddInsideMethod(
@"var $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestNotInUsing(bool topLevelStatement)
        => VerifyAbsenceAsync(AddInsideMethod(
@"using ($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

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
    public async Task TestAfterPartial()
        => await VerifyKeywordAsync(@"partial $$");

    [Fact]
    public Task TestAfterNestedPartial()
        => VerifyKeywordAsync(
            """
            class C {
                partial $$
            """);

    [Fact]
    public async Task TestAfterAbstract()
        => await VerifyKeywordAsync(@"abstract $$");

    [Fact]
    public Task TestAfterNestedAbstract()
        => VerifyKeywordAsync(
            """
            class C {
                abstract $$
            """);

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
        => VerifyKeywordAsync(
@"private $$");

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
    public Task TestAfterStatic_InClass()
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
    public Task TestAfterDelegate()
        => VerifyKeywordAsync(
@"delegate $$");

    [Theory, CombinatorialData]
    public Task TestNotAfterAnonymousDelegate(bool topLevelStatement)
        => VerifyAbsenceAsync(AddInsideMethod(
@"var q = delegate $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Fact]
    public Task TestNotAfterEvent()
        => VerifyAbsenceAsync(
            """
            class C {
                event $$
            """);

    [Fact]
    public Task TestNotAfterVoid()
        => VerifyAbsenceAsync(
            """
            class C {
                void $$
            """);

    [Fact]
    public Task TestAfterReadonly()
        => VerifyKeywordAsync(
@"readonly $$");

    [Fact]
    public Task TestInReadonlyStruct()
        => VerifyKeywordAsync(
@"readonly $$ struct { }");

    [Fact]
    public Task TestInReadonlyStruct_AfterReadonly()
        => VerifyKeywordAsync(
@"$$ readonly struct { }");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44423")]
    public Task TestAfterNew()
        => VerifyAbsenceAsync(
@"new $$");

    [Fact]
    public Task TestAfterNewInClass()
        => VerifyKeywordAsync(
@"class C { new $$ }");

    [Fact]
    public Task TestAfterNewInStruct()
        => VerifyKeywordAsync(
@"struct S { new $$ }");

    [Fact]
    public Task TestAfterNestedNew()
        => VerifyKeywordAsync(
            """
            class C {
               new $$
            """);

    [Theory, CombinatorialData]
    public Task TestInUnsafeBlock(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
            """
            unsafe {
                $$
            """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Fact]
    public Task TestInUnsafeMethod()
        => VerifyKeywordAsync(
            """
            class C {
               unsafe void Goo() {
                 $$
            """);

    [Fact]
    public Task TestInUnsafeClass()
        => VerifyKeywordAsync(
            """
            unsafe class C {
               void Goo() {
                 $$
            """);

    [Fact]
    public Task TestInMemberArrowMethod()
        => VerifyKeywordAsync(
            """
            unsafe class C {
               void Goo() {
                 $$
            """);

    [Fact]
    public Task TestInMemberArrowProperty()
        => VerifyKeywordAsync(
            """
            class C {
                  ref int Goo() => $$
            """);

    [Fact]
    public Task TestInMemberArrowIndexer()
        => VerifyKeywordAsync(
            """
            class C {
                  ref int Goo => $$
            """);

    [Theory, CombinatorialData]
    public Task TestInLocalArrowMethod(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@" ref int Goo() => $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestInArrowLambda(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@" D1 lambda = () => $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/21889")]
    [InlineData(SourceCodeKind.Regular, true)]
    [InlineData(SourceCodeKind.Regular, false)]
    [InlineData(SourceCodeKind.Script, true, Skip = "https://github.com/dotnet/roslyn/issues/44630")]
    [InlineData(SourceCodeKind.Script, false)]
    public Task TestInConditionalExpressionTrueBranch(SourceCodeKind sourceCodeKind, bool topLevelStatement)
        => VerifyWorkerAsync(
            AddInsideMethod("""
                ref int x = ref true ? $$
                """, topLevelStatement: topLevelStatement),
            absent: false,
            options: sourceCodeKind == SourceCodeKind.Script ? Options.Script : CSharp9ParseOptions);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/21889")]
    [InlineData(SourceCodeKind.Regular, true)]
    [InlineData(SourceCodeKind.Regular, false)]
    [InlineData(SourceCodeKind.Script, true, Skip = "https://github.com/dotnet/roslyn/issues/44630")]
    [InlineData(SourceCodeKind.Script, false)]
    public Task TestInConditionalExpressionFalseBranch(SourceCodeKind sourceCodeKind, bool topLevelStatement)
        => VerifyWorkerAsync(
            AddInsideMethod("""
                int x = 0;
                ref int y = ref true ? ref x : $$
                """, topLevelStatement: topLevelStatement),
            absent: false,
            options: sourceCodeKind == SourceCodeKind.Script ? Options.Script : CSharp9ParseOptions);

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/22253")]
    [CombinatorialData]
    public Task TestInLocalMethod(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@" void Goo(int test, $$) ", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestRefInFor(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod("""
            for ($$
            """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestRefForeachVariable(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod("""
            foreach ($$
            """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestRefExpressionInAssignment(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod("""
            int x = 0;
            ref int y = ref x;
            y = $$
            """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    public Task TestRefExpressionAfterReturn(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod("""
            ref int x = ref (new int[1])[0];
            return ref (x = $$
            """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Fact]
    public Task TestExtensionMethods_FirstParameter()
        => VerifyKeywordAsync(
            """
            static class Extensions {
                static void Extension($$
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30339")]
    public Task TestExtensionMethods_FirstParameter_AfterThisKeyword()
        => VerifyKeywordAsync(
            """
            static class Extensions {
                static void Extension(this $$
            """);

    [Fact]
    public Task TestExtensionMethods_SecondParameter()
        => VerifyKeywordAsync(
            """
            static class Extensions {
                static void Extension(this int i, $$
            """);

    [Fact]
    public Task TestExtensionMethods_SecondParameter_AfterThisKeyword()
        => VerifyAbsenceAsync(
            """
            static class Extensions {
                static void Extension(this int i, this $$
            """);

    [Fact]
    public Task TestExtensionMethods_FirstParameter_NonStaticClass()
        => VerifyKeywordAsync(
            """
            class Extensions {
                static void Extension($$
            """);

    [Fact]
    public Task TestExtensionMethods_FirstParameter_AfterThisKeyword_NonStaticClass()
        => VerifyAbsenceAsync(
            """
            class Extensions {
                static void Extension(this $$
            """);

    [Fact]
    public Task TestExtensionMethods_SecondParameter_NonStaticClass()
        => VerifyKeywordAsync(
            """
            class Extensions {
                static void Extension(this int i, $$
            """);

    [Fact]
    public Task TestExtensionMethods_SecondParameter_AfterThisKeyword_NonStaticClass()
        => VerifyAbsenceAsync(
            """
            class Extensions {
                static void Extension(this int i, this $$
            """);

    [Fact]
    public Task TestExtensionMethods_FirstParameter_NonStaticMethod()
        => VerifyKeywordAsync(
            """
            static class Extensions {
                void Extension($$
            """);

    [Fact]
    public Task TestExtensionMethods_FirstParameter_AfterThisKeyword_NonStaticMethod()
        => VerifyAbsenceAsync(
            """
            static class Extensions {
                void Extension(this $$
            """);

    [Fact]
    public Task TestExtensionMethods_SecondParameter_NonStaticMethod()
        => VerifyKeywordAsync(
            """
            static class Extensions {
                void Extension(this int i, $$
            """);

    [Fact]
    public Task TestExtensionMethods_SecondParameter_AfterThisKeyword_NonStaticMethod()
        => VerifyAbsenceAsync(
            """
            static class Extensions {
                void Extension(this int i, this $$
            """);

    [Fact]
    public Task TestInFunctionPointerTypeNoExistingModifiers()
        => VerifyKeywordAsync("""
            class C
            {
                delegate*<$$
            """);

    [Theory]
    [InlineData("in")]
    [InlineData("out")]
    [InlineData("ref")]
    [InlineData("ref readonly")]
    public Task TestNotInFunctionPointerTypeExistingModifiers(string modifier)
        => VerifyAbsenceAsync($$"""
            class C
            {
                delegate*<{{modifier}} $$
            """);

    [Fact]
    public Task TestAfterNamespace()
        => VerifyKeywordAsync(
            """
            namespace N { }
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58906")]
    public Task TestInPotentialLambdaParamListParsedAsCastOnDifferentLines()
        => VerifyKeywordAsync(
            """
            class C
            {
                static void Main(string[] args)
                {
                    var f = ($$)
                    Main(null);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58906")]
    public Task TestInPotentialLambdaParamListParsedAsCastOnSameLine()
        => VerifyKeywordAsync(
            """
            class C
            {
                static void Main(string[] args)
                {
                    var f = ($$)Main(null);
                }
            }
            """);

    [Fact]
    public Task TestAfterScoped()
        => VerifyKeywordAsync(
            """
            class C
            {
                void M()
                {
                    scoped $$
                }
            }
            """);

    [Fact]
    public Task TestInParameterAfterScoped()
        => VerifyKeywordAsync("""
            class C
            {
                void M(scoped $$)
            }
            """);

    [Fact]
    public Task TestInParameterAfterThisScoped()
        => VerifyKeywordAsync("""
            static class C
            {
                static void M(this scoped $$)
            }
            """);

    [Fact]
    public Task TestInAnonymousMethodParameterAfterScoped()
        => VerifyKeywordAsync("""
            class C
            {
                void M()
                {
                    var x = delegate (scoped $$) { };
                }
            }
            """);

    [Fact]
    public Task TestAfterAllowsInTypeParameterConstraint()
        => VerifyKeywordAsync(
            """
            class C<T> where T : allows $$
            """);

    [Fact]
    public Task TestAfterAllowsInTypeParameterConstraint2()
        => VerifyKeywordAsync(
            """
            class C<T>
                where T : allows $$
                where U : U
            """);

    [Fact]
    public Task TestAfterAllowsInMethodTypeParameterConstraint()
        => VerifyKeywordAsync(
            """
            class C {
                void Goo<T>()
                  where T : allows $$
            """);

    [Fact]
    public Task TestAfterAllowsInMethodTypeParameterConstraint2()
        => VerifyKeywordAsync(
            """
            class C {
                void Goo<T>()
                  where T : allows $$
                  where U : T
            """);

    [Fact]
    public Task TestNotAfterClassTypeParameterConstraint()
        => VerifyAbsenceAsync(
            """
            class C<T> where T : class, allows $$
            """);

    [Fact]
    public Task TestAfterStructTypeParameterConstraint()
        => VerifyKeywordAsync(
            """
            class C<T> where T : struct, allows $$
            """);

    [Fact]
    public Task TestAfterSimpleTypeParameterConstraint()
        => VerifyKeywordAsync(
            """
            class C<T> where T : IGoo, allows $$
            """);

    [Fact]
    public Task TestAfterConstructorTypeParameterConstraint()
        => VerifyKeywordAsync(
            """
            class C<T> where T : new(), allows $$
            """);

    [Fact]
    public Task TestNotAfterGenericNameInTypeParameterConstraint()
        => VerifyAbsenceAsync(
            """
            class C<T> where T : allows<int> $$
            """);

    [Fact]
    public Task TestNotAfterGenericNameInTypeParameterConstraint2()
        => VerifyAbsenceAsync(
            """
            class C<T>
                where T : allows<int> $$
                where U : U
            """);

    [Fact]
    public Task TestNotAfterGenericNameAfterClassTypeParameterConstraint()
        => VerifyAbsenceAsync(
            """
            class C<T> where T : class, allows<int> $$
            """);

    [Fact]
    public Task TestNotAfterGenericNameAfterStructTypeParameterConstraint()
        => VerifyAbsenceAsync(
            """
            class C<T> where T : struct, allows<int> $$
            """);

    [Fact]
    public Task TestNotAfterGenericNameAfterSimpleTypeParameterConstraint()
        => VerifyAbsenceAsync(
            """
            class C<T> where T : IGoo, allows<int> $$
            """);

    [Fact]
    public Task TestNotAfterGenericNameAfterConstructorTypeParameterConstraint()
        => VerifyAbsenceAsync(
            """
            class C<T> where T : new(), allows<int> $$
            """);

    [Fact]
    public Task TestNotAfterRefInTypeParameterConstraint()
        => VerifyAbsenceAsync(
            """
            class C<T> where T : allows ref $$
            """);

    [Fact]
    public Task TestNotAfterRefInTypeParameterConstraint2()
        => VerifyAbsenceAsync(
            """
            class C<T>
                where T : allows ref $$
                where U : U
            """);

    [Fact]
    public Task TestNotAfterRefAfterClassTypeParameterConstraint()
        => VerifyAbsenceAsync(
            """
            class C<T> where T : class, allows ref $$
            """);

    [Fact]
    public Task TestNotAfterRefAfterStructTypeParameterConstraint()
        => VerifyAbsenceAsync(
            """
            class C<T> where T : struct, allows ref $$
            """);

    [Fact]
    public Task TestNotAfterRefAfterSimpleTypeParameterConstraint()
        => VerifyAbsenceAsync(
            """
            class C<T> where T : IGoo, allows ref $$
            """);

    [Fact]
    public Task TestNotAfterRefAfterConstructorTypeParameterConstraint()
        => VerifyAbsenceAsync(
            """
            class C<T> where T : new(), allows ref $$
            """);

    [Fact]
    public Task TestWithinExtension()
        => VerifyKeywordAsync(
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
