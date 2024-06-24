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
    public class RefKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact]
        public async Task TestAtRoot()
        {
            await VerifyKeywordAsync(
@"$$");
        }

        [Fact]
        public async Task TestNotAfterClass()
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
        public async Task TestNotAfterAngle()
        {
            await VerifyAbsenceAsync(
@"interface IGoo<$$");
        }

        [Fact]
        public async Task TestInterfaceTypeVarianceNotAfterIn()
        {
            await VerifyAbsenceAsync(
@"interface IGoo<in $$");
        }

        [Fact]
        public async Task TestInterfaceTypeVarianceNotAfterComma()
        {
            await VerifyAbsenceAsync(
@"interface IGoo<Goo, $$");
        }

        [Fact]
        public async Task TestInterfaceTypeVarianceNotAfterAttribute()
        {
            await VerifyAbsenceAsync(
@"interface IGoo<[Goo]$$");
        }

        [Fact]
        public async Task TestDelegateTypeVarianceNotAfterAngle()
        {
            await VerifyAbsenceAsync(
@"delegate void D<$$");
        }

        [Fact]
        public async Task TestDelegateTypeVarianceNotAfterComma()
        {
            await VerifyAbsenceAsync(
@"delegate void D<Goo, $$");
        }

        [Fact]
        public async Task TestDelegateTypeVarianceNotAfterAttribute()
        {
            await VerifyAbsenceAsync(
@"delegate void D<[Goo]$$");
        }

        [Fact]
        public async Task TestNotRefBaseListAfterAngle()
        {
            await VerifyAbsenceAsync(
@"interface IGoo : Bar<$$");
        }

        [Fact]
        public async Task TestNotInGenericMethod()
        {
            await VerifyAbsenceAsync(
                """
                interface IGoo {
                    void Goo<$$
                """);
        }

        [Fact]
        public async Task TestNotAfterRef()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    void Goo(ref $$
                """);
        }

        [Fact]
        public async Task TestNotAfterOut()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    void Goo(out $$
                """);
        }

        [Fact]
        public async Task TestAfterMethodOpenParen()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    void Goo($$
                """);
        }

        [Fact]
        public async Task TestAfterMethodComma()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    void Goo(int i, $$
                """);
        }

        [Fact]
        public async Task TestAfterMethodAttribute()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    void Goo(int i, [Goo]$$
                """);
        }

        [Fact]
        public async Task TestAfterConstructorOpenParen()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    public C($$
                """);
        }

        [Fact]
        public async Task TestAfterConstructorComma()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    public C(int i, $$
                """);
        }

        [Fact]
        public async Task TestAfterConstructorAttribute()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    public C(int i, [Goo]$$
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/933972")]
        public async Task TestAfterThisConstructorInitializer()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    public C():this($$
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/933972")]
        public async Task TestAfterThisConstructorInitializerNamedArgument()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    public C():this(Goo:$$
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/933972")]
        public async Task TestAfterBaseConstructorInitializer()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    public C():base($$
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/933972")]
        public async Task TestAfterBaseConstructorInitializerNamedArgument()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    public C():base(5, Goo:$$
                """);
        }

        [Fact]
        public async Task TestAfterDelegateOpenParen()
        {
            await VerifyKeywordAsync(
@"delegate void D($$");
        }

        [Fact]
        public async Task TestAfterDelegateComma()
        {
            await VerifyKeywordAsync(
@"delegate void D(int i, $$");
        }

        [Fact]
        public async Task TestAfterDelegateAttribute()
        {
            await VerifyKeywordAsync(
@"delegate void D(int i, [Goo]$$");
        }

        [Fact]
        public async Task TestNotAfterOperator()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    static int operator +($$
                """);
        }

        [Fact]
        public async Task TestNotAfterDestructor()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    ~C($$
                """);
        }

        [Fact]
        public async Task TestNotAfterIndexer()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    int this[$$
                """);
        }

        [Fact]
        public async Task TestInObjectCreationAfterOpenParen()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    void Goo() {
                      new Bar($$
                """);
        }

        [Fact]
        public async Task TestNotAfterRefParam()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    void Goo() {
                      new Bar(ref $$
                """);
        }

        [Fact]
        public async Task TestNotAfterOutParam()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    void Goo() {
                      new Bar(out $$
                """);
        }

        [Fact]
        public async Task TestInObjectCreationAfterComma()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    void Goo() {
                      new Bar(baz, $$
                """);
        }

        [Fact]
        public async Task TestInObjectCreationAfterSecondComma()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    void Goo() {
                      new Bar(baz, quux, $$
                """);
        }

        [Fact]
        public async Task TestInObjectCreationAfterSecondNamedParam()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    void Goo() {
                      new Bar(baz: 4, quux: $$
                """);
        }

        [Fact]
        public async Task TestInInvocationExpression()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    void Goo() {
                      Bar($$
                """);
        }

        [Fact]
        public async Task TestInInvocationAfterComma()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    void Goo() {
                      Bar(baz, $$
                """);
        }

        [Fact]
        public async Task TestInInvocationAfterSecondComma()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    void Goo() {
                      Bar(baz, quux, $$
                """);
        }

        [Fact]
        public async Task TestInInvocationAfterSecondNamedParam()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    void Goo() {
                      Bar(baz: 4, quux: $$
                """);
        }

        [Theory, CombinatorialData]
        public async Task TestInLambdaDeclaration(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = ($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestInLambdaDeclaration2(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = (ref int a, $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestInLambdaDeclaration3(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = (int a, $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestInDelegateDeclaration(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = delegate ($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestInDelegateDeclaration2(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = delegate (a, $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestInDelegateDeclaration3(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = delegate (int a, $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Fact]
        public async Task TestInCrefParameterList()
        {
            var text = """
                Class c
                {
                    /// <see cref="main($$"/>
                    void main(out goo) { }
                }
                """;

            await VerifyKeywordAsync(text);
        }

        [Theory, CombinatorialData]
        public async Task TestEmptyStatement(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"$$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestAfterReturn(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"return $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestInFor(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"for ($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestNotInFor(bool topLevelStatement)
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"for (var $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestInFor2(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"for ($$;", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestInFor3(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"for ($$;;", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestNotAfterVar(bool topLevelStatement)
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"var $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestNotInUsing(bool topLevelStatement)
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"using ($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
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
        public async Task TestAfterPartial()
            => await VerifyKeywordAsync(@"partial $$");

        [Fact]
        public async Task TestAfterNestedPartial()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    partial $$
                """);
        }

        [Fact]
        public async Task TestAfterAbstract()
            => await VerifyKeywordAsync(@"abstract $$");

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
            await VerifyKeywordAsync(
@"private $$");
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
        public async Task TestAfterStatic_InClass()
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
        public async Task TestAfterDelegate()
        {
            await VerifyKeywordAsync(
@"delegate $$");
        }

        [Theory, CombinatorialData]
        public async Task TestNotAfterAnonymousDelegate(bool topLevelStatement)
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"var q = delegate $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

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
        public async Task TestNotAfterVoid()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    void $$
                """);
        }

        [Fact]
        public async Task TestAfterReadonly()
        {
            await VerifyKeywordAsync(
@"readonly $$");
        }

        [Fact]
        public async Task TestInReadonlyStruct()
        {
            await VerifyKeywordAsync(
@"readonly $$ struct { }");
        }

        [Fact]
        public async Task TestInReadonlyStruct_AfterReadonly()
        {
            await VerifyKeywordAsync(
@"$$ readonly struct { }");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44423")]
        public async Task TestAfterNew()
        {
            // new ref struct S
            await VerifyKeywordAsync(
@"new $$");
        }

        [Fact]
        public async Task TestAfterNewInClass()
        {
            await VerifyKeywordAsync(
@"class C { new $$ }");
        }

        [Fact]
        public async Task TestAfterNewInStruct()
        {
            await VerifyKeywordAsync(
@"struct S { new $$ }");
        }

        [Fact]
        public async Task TestAfterNestedNew()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   new $$
                """);
        }

        [Theory, CombinatorialData]
        public async Task TestInUnsafeBlock(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                unsafe {
                    $$
                """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Fact]
        public async Task TestInUnsafeMethod()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   unsafe void Goo() {
                     $$
                """);
        }

        [Fact]
        public async Task TestInUnsafeClass()
        {
            await VerifyKeywordAsync(
                """
                unsafe class C {
                   void Goo() {
                     $$
                """);
        }

        [Fact]
        public async Task TestInMemberArrowMethod()
        {
            await VerifyKeywordAsync(
                """
                unsafe class C {
                   void Goo() {
                     $$
                """);
        }

        [Fact]
        public async Task TestInMemberArrowProperty()
        {
            await VerifyKeywordAsync(
                """
                class C {
                      ref int Goo() => $$
                """);
        }

        [Fact]
        public async Task TestInMemberArrowIndexer()
        {
            await VerifyKeywordAsync(
                """
                class C {
                      ref int Goo => $$
                """);
        }

        [Theory, CombinatorialData]
        public async Task TestInLocalArrowMethod(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@" ref int Goo() => $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestInArrowLambda(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@" D1 lambda = () => $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/21889")]
        [InlineData(true)]
        [InlineData(false)]
        public async Task TestInConditionalExpressionTrueBranch_Regular(bool topLevelStatement)
        {
            await VerifyWorkerAsync(
                AddInsideMethod("""
                    ref int x = ref true ? $$
                    """, topLevelStatement: topLevelStatement),
                absent: false,
                options: CSharp9ParseOptions);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/44630"), WorkItem("https://github.com/dotnet/roslyn/issues/21889")]
        public async Task TestInConditionalExpressionTrueBranch_Script_TopLevelStatement()
        {
            await VerifyWorkerAsync(
                AddInsideMethod("""
                    ref int x = ref true ? $$
                    """, topLevelStatement: true),
                absent: false,
                options: Options.Script);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21889")]
        public async Task TestInConditionalExpressionTrueBranch_Script_NotTopLevelStatement()
        {
            await VerifyWorkerAsync(
                AddInsideMethod("""
                    ref int x = ref true ? $$
                    """, topLevelStatement: false),
                absent: false,
                options: Options.Script);
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/21889")]
        [InlineData(true)]
        [InlineData(false)]
        public async Task TestInConditionalExpressionFalseBranch_Regular(bool topLevelStatement)
        {
            await VerifyWorkerAsync(
                AddInsideMethod("""
                    int x = 0;
                    ref int y = ref true ? ref x : $$
                    """, topLevelStatement: topLevelStatement),
                absent: false,
                options: CSharp9ParseOptions);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/44630"), WorkItem("https://github.com/dotnet/roslyn/issues/21889")]
        public async Task TestInConditionalExpressionFalseBranch_Script_TopLevelStatement()
        {
            await VerifyWorkerAsync(
                AddInsideMethod("""
                    int x = 0;
                    ref int y = ref true ? ref x : $$
                    """, topLevelStatement: true),
                absent: false,
                options: Options.Script);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21889")]
        public async Task TestInConditionalExpressionFalseBranch_Script_NotTopLevelStatement()
        {
            await VerifyWorkerAsync(
                AddInsideMethod("""
                    int x = 0;
                    ref int y = ref true ? ref x : $$
                    """, topLevelStatement: false),
                absent: false,
                options: Options.Script);
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/22253")]
        [CombinatorialData]
        public async Task TestInLocalMethod(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@" void Goo(int test, $$) ", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

        }

        [Theory, CombinatorialData]
        public async Task TestRefInFor(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod("""
                for ($$
                """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestRefForeachVariable(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod("""
                foreach ($$
                """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestRefExpressionInAssignment(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod("""
                int x = 0;
                ref int y = ref x;
                y = $$
                """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, CombinatorialData]
        public async Task TestRefExpressionAfterReturn(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod("""
                ref int x = ref (new int[1])[0];
                return ref (x = $$
                """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Fact]
        public async Task TestExtensionMethods_FirstParameter()
        {
            await VerifyKeywordAsync(
                """
                static class Extensions {
                    static void Extension($$
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30339")]
        public async Task TestExtensionMethods_FirstParameter_AfterThisKeyword()
        {
            await VerifyKeywordAsync(
                """
                static class Extensions {
                    static void Extension(this $$
                """);
        }

        [Fact]
        public async Task TestExtensionMethods_SecondParameter()
        {
            await VerifyKeywordAsync(
                """
                static class Extensions {
                    static void Extension(this int i, $$
                """);
        }

        [Fact]
        public async Task TestExtensionMethods_SecondParameter_AfterThisKeyword()
        {
            await VerifyAbsenceAsync(
                """
                static class Extensions {
                    static void Extension(this int i, this $$
                """);
        }

        [Fact]
        public async Task TestExtensionMethods_FirstParameter_NonStaticClass()
        {
            await VerifyKeywordAsync(
                """
                class Extensions {
                    static void Extension($$
                """);
        }

        [Fact]
        public async Task TestExtensionMethods_FirstParameter_AfterThisKeyword_NonStaticClass()
        {
            await VerifyAbsenceAsync(
                """
                class Extensions {
                    static void Extension(this $$
                """);
        }

        [Fact]
        public async Task TestExtensionMethods_SecondParameter_NonStaticClass()
        {
            await VerifyKeywordAsync(
                """
                class Extensions {
                    static void Extension(this int i, $$
                """);
        }

        [Fact]
        public async Task TestExtensionMethods_SecondParameter_AfterThisKeyword_NonStaticClass()
        {
            await VerifyAbsenceAsync(
                """
                class Extensions {
                    static void Extension(this int i, this $$
                """);
        }

        [Fact]
        public async Task TestExtensionMethods_FirstParameter_NonStaticMethod()
        {
            await VerifyKeywordAsync(
                """
                static class Extensions {
                    void Extension($$
                """);
        }

        [Fact]
        public async Task TestExtensionMethods_FirstParameter_AfterThisKeyword_NonStaticMethod()
        {
            await VerifyAbsenceAsync(
                """
                static class Extensions {
                    void Extension(this $$
                """);
        }

        [Fact]
        public async Task TestExtensionMethods_SecondParameter_NonStaticMethod()
        {
            await VerifyKeywordAsync(
                """
                static class Extensions {
                    void Extension(this int i, $$
                """);
        }

        [Fact]
        public async Task TestExtensionMethods_SecondParameter_AfterThisKeyword_NonStaticMethod()
        {
            await VerifyAbsenceAsync(
                """
                static class Extensions {
                    void Extension(this int i, this $$
                """);
        }

        [Fact]
        public async Task TestInFunctionPointerTypeNoExistingModifiers()
        {
            await VerifyKeywordAsync("""
                class C
                {
                    delegate*<$$
                """);
        }

        [Theory]
        [InlineData("in")]
        [InlineData("out")]
        [InlineData("ref")]
        [InlineData("ref readonly")]
        public async Task TestNotInFunctionPointerTypeExistingModifiers(string modifier)
        {
            await VerifyAbsenceAsync($@"
class C
{{
    delegate*<{modifier} $$");
        }

        [Fact]
        public async Task TestAfterNamespace()
        {
            await VerifyKeywordAsync(
                """
                namespace N { }
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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58906")]
        public async Task TestInPotentialLambdaParamListParsedAsCastOnDifferentLines()
        {
            await VerifyKeywordAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58906")]
        public async Task TestInPotentialLambdaParamListParsedAsCastOnSameLine()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    static void Main(string[] args)
                    {
                        var f = ($$)Main(null);
                    }
                }
                """);
        }

        [Fact]
        public async Task TestAfterScoped()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    void M()
                    {
                        scoped $$
                    }
                }
                """);
        }

        [Fact]
        public async Task TestInParameterAfterScoped()
        {
            await VerifyKeywordAsync("""
                class C
                {
                    void M(scoped $$)
                }
                """);
        }

        [Fact]
        public async Task TestInParameterAfterThisScoped()
        {
            await VerifyKeywordAsync("""
                static class C
                {
                    static void M(this scoped $$)
                }
                """);
        }

        [Fact]
        public async Task TestInAnonymousMethodParameterAfterScoped()
        {
            await VerifyKeywordAsync("""
                class C
                {
                    void M()
                    {
                        var x = delegate (scoped $$) { };
                    }
                }
                """);
        }

        [Fact]
        public async Task TestNotInExtensionForType()
        {
            await VerifyAbsenceAsync(
                """
                implicit extension E for $$
                """);
        }

        [Fact]
        public async Task TestInsideExtension()
        {
            await VerifyKeywordAsync(
                """
                implicit extension E
                {
                    $$
                """);
        }

        [Fact]
        public async Task TestAfterAllowsInTypeParameterConstraint()
        {
            await VerifyKeywordAsync(
                """
                class C<T> where T : allows $$
                """);
        }

        [Fact]
        public async Task TestAfterAllowsInTypeParameterConstraint2()
        {
            await VerifyKeywordAsync(
                """
                class C<T>
                    where T : allows $$
                    where U : U
                """);
        }

        [Fact]
        public async Task TestAfterAllowsInMethodTypeParameterConstraint()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    void Goo<T>()
                      where T : allows $$
                """);
        }

        [Fact]
        public async Task TestAfterAllowsInMethodTypeParameterConstraint2()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    void Goo<T>()
                      where T : allows $$
                      where U : T
                """);
        }

        [Fact]
        public async Task TestNotAfterClassTypeParameterConstraint()
        {
            await VerifyAbsenceAsync(
                """
                class C<T> where T : class, allows $$
                """);
        }

        [Fact]
        public async Task TestAfterStructTypeParameterConstraint()
        {
            await VerifyKeywordAsync(
                """
                class C<T> where T : struct, allows $$
                """);
        }

        [Fact]
        public async Task TestAfterSimpleTypeParameterConstraint()
        {
            await VerifyKeywordAsync(
                """
                class C<T> where T : IGoo, allows $$
                """);
        }

        [Fact]
        public async Task TestAfterConstructorTypeParameterConstraint()
        {
            await VerifyKeywordAsync(
                """
                class C<T> where T : new(), allows $$
                """);
        }

        [Fact]
        public async Task TestNotAfterGenericNameInTypeParameterConstraint()
        {
            await VerifyAbsenceAsync(
                """
                class C<T> where T : allows<int> $$
                """);
        }

        [Fact]
        public async Task TestNotAfterGenericNameInTypeParameterConstraint2()
        {
            await VerifyAbsenceAsync(
                """
                class C<T>
                    where T : allows<int> $$
                    where U : U
                """);
        }

        [Fact]
        public async Task TestNotAfterGenericNameAfterClassTypeParameterConstraint()
        {
            await VerifyAbsenceAsync(
                """
                class C<T> where T : class, allows<int> $$
                """);
        }

        [Fact]
        public async Task TestNotAfterGenericNameAfterStructTypeParameterConstraint()
        {
            await VerifyAbsenceAsync(
                """
                class C<T> where T : struct, allows<int> $$
                """);
        }

        [Fact]
        public async Task TestNotAfterGenericNameAfterSimpleTypeParameterConstraint()
        {
            await VerifyAbsenceAsync(
                """
                class C<T> where T : IGoo, allows<int> $$
                """);
        }

        [Fact]
        public async Task TestNotAfterGenericNameAfterConstructorTypeParameterConstraint()
        {
            await VerifyAbsenceAsync(
                """
                class C<T> where T : new(), allows<int> $$
                """);
        }

        [Fact]
        public async Task TestNotAfterRefInTypeParameterConstraint()
        {
            await VerifyAbsenceAsync(
                """
                class C<T> where T : allows ref $$
                """);
        }

        [Fact]
        public async Task TestNotAfterRefInTypeParameterConstraint2()
        {
            await VerifyAbsenceAsync(
                """
                class C<T>
                    where T : allows ref $$
                    where U : U
                """);
        }

        [Fact]
        public async Task TestNotAfterRefAfterClassTypeParameterConstraint()
        {
            await VerifyAbsenceAsync(
                """
                class C<T> where T : class, allows ref $$
                """);
        }

        [Fact]
        public async Task TestNotAfterRefAfterStructTypeParameterConstraint()
        {
            await VerifyAbsenceAsync(
                """
                class C<T> where T : struct, allows ref $$
                """);
        }

        [Fact]
        public async Task TestNotAfterRefAfterSimpleTypeParameterConstraint()
        {
            await VerifyAbsenceAsync(
                """
                class C<T> where T : IGoo, allows ref $$
                """);
        }

        [Fact]
        public async Task TestNotAfterRefAfterConstructorTypeParameterConstraint()
        {
            await VerifyAbsenceAsync(
                """
                class C<T> where T : new(), allows ref $$
                """);
        }
    }
}
