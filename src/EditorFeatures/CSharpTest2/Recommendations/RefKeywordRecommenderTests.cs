// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class RefKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAtRoot()
        {
            await VerifyKeywordAsync(
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterClass()
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

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterAngle()
        {
            await VerifyAbsenceAsync(
@"interface IGoo<$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInterfaceTypeVarianceNotAfterIn()
        {
            await VerifyAbsenceAsync(
@"interface IGoo<in $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInterfaceTypeVarianceNotAfterComma()
        {
            await VerifyAbsenceAsync(
@"interface IGoo<Goo, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInterfaceTypeVarianceNotAfterAttribute()
        {
            await VerifyAbsenceAsync(
@"interface IGoo<[Goo]$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestDelegateTypeVarianceNotAfterAngle()
        {
            await VerifyAbsenceAsync(
@"delegate void D<$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestDelegateTypeVarianceNotAfterComma()
        {
            await VerifyAbsenceAsync(
@"delegate void D<Goo, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestDelegateTypeVarianceNotAfterAttribute()
        {
            await VerifyAbsenceAsync(
@"delegate void D<[Goo]$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotRefBaseListAfterAngle()
        {
            await VerifyAbsenceAsync(
@"interface IGoo : Bar<$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInGenericMethod()
        {
            await VerifyAbsenceAsync(
@"interface IGoo {
    void Goo<$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterRef()
        {
            await VerifyAbsenceAsync(
@"class C {
    void Goo(ref $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterOut()
        {
            await VerifyAbsenceAsync(
@"class C {
    void Goo(out $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterMethodOpenParen()
        {
            await VerifyKeywordAsync(
@"class C {
    void Goo($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterMethodComma()
        {
            await VerifyKeywordAsync(
@"class C {
    void Goo(int i, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterMethodAttribute()
        {
            await VerifyKeywordAsync(
@"class C {
    void Goo(int i, [Goo]$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterConstructorOpenParen()
        {
            await VerifyKeywordAsync(
@"class C {
    public C($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterConstructorComma()
        {
            await VerifyKeywordAsync(
@"class C {
    public C(int i, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterConstructorAttribute()
        {
            await VerifyKeywordAsync(
@"class C {
    public C(int i, [Goo]$$");
        }

        [WorkItem(933972, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/933972")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterThisConstructorInitializer()
        {
            await VerifyKeywordAsync(
@"class C {
    public C():this($$");
        }

        [WorkItem(933972, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/933972")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterThisConstructorInitializerNamedArgument()
        {
            await VerifyKeywordAsync(
@"class C {
    public C():this(Goo:$$");
        }

        [WorkItem(933972, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/933972")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterBaseConstructorInitializer()
        {
            await VerifyKeywordAsync(
@"class C {
    public C():base($$");
        }

        [WorkItem(933972, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/933972")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterBaseConstructorInitializerNamedArgument()
        {
            await VerifyKeywordAsync(
@"class C {
    public C():base(5, Goo:$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterDelegateOpenParen()
        {
            await VerifyKeywordAsync(
@"delegate void D($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterDelegateComma()
        {
            await VerifyKeywordAsync(
@"delegate void D(int i, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterDelegateAttribute()
        {
            await VerifyKeywordAsync(
@"delegate void D(int i, [Goo]$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterOperator()
        {
            await VerifyAbsenceAsync(
@"class C {
    static int operator +($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterDestructor()
        {
            await VerifyAbsenceAsync(
@"class C {
    ~C($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterIndexer()
        {
            await VerifyAbsenceAsync(
@"class C {
    int this[$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInObjectCreationAfterOpenParen()
        {
            await VerifyKeywordAsync(
@"class C {
    void Goo() {
      new Bar($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterRefParam()
        {
            await VerifyAbsenceAsync(
@"class C {
    void Goo() {
      new Bar(ref $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterOutParam()
        {
            await VerifyAbsenceAsync(
@"class C {
    void Goo() {
      new Bar(out $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInObjectCreationAfterComma()
        {
            await VerifyKeywordAsync(
@"class C {
    void Goo() {
      new Bar(baz, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInObjectCreationAfterSecondComma()
        {
            await VerifyKeywordAsync(
@"class C {
    void Goo() {
      new Bar(baz, quux, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInObjectCreationAfterSecondNamedParam()
        {
            await VerifyKeywordAsync(
@"class C {
    void Goo() {
      new Bar(baz: 4, quux: $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInInvocationExpression()
        {
            await VerifyKeywordAsync(
@"class C {
    void Goo() {
      Bar($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInInvocationAfterComma()
        {
            await VerifyKeywordAsync(
@"class C {
    void Goo() {
      Bar(baz, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInInvocationAfterSecondComma()
        {
            await VerifyKeywordAsync(
@"class C {
    void Goo() {
      Bar(baz, quux, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInInvocationAfterSecondNamedParam()
        {
            await VerifyKeywordAsync(
@"class C {
    void Goo() {
      Bar(baz: 4, quux: $$");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInLambdaDeclaration(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = ($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInLambdaDeclaration2(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = (ref int a, $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInLambdaDeclaration3(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = (int a, $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInDelegateDeclaration(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = delegate ($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInDelegateDeclaration2(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = delegate (a, $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInDelegateDeclaration3(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = delegate (int a, $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInCrefParameterList()
        {
            var text = @"Class c
{
    /// <see cref=""main($$""/>
    void main(out goo) { }
}";

            await VerifyKeywordAsync(text);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestEmptyStatement(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"$$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
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
        public async Task TestInFor(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"for ($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestNotInFor(bool topLevelStatement)
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"for (var $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInFor2(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"for ($$;", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInFor3(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"for ($$;;", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestNotAfterVar(bool topLevelStatement)
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"var $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestNotInUsing(bool topLevelStatement)
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"using ($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
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
        public async Task TestAfterPartial()
            => await VerifyKeywordAsync(@"partial $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedPartial()
        {
            await VerifyKeywordAsync(
@"class C {
    partial $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterAbstract()
            => await VerifyKeywordAsync(@"abstract $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedAbstract()
        {
            await VerifyKeywordAsync(
@"class C {
    abstract $$");
        }

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
            await VerifyKeywordAsync(
@"private $$");
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
        public async Task TestAfterStatic_InClass()
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
        public async Task TestAfterDelegate()
        {
            await VerifyKeywordAsync(
@"delegate $$");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestNotAfterAnonymousDelegate(bool topLevelStatement)
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"var q = delegate $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterEvent()
        {
            await VerifyAbsenceAsync(
@"class C {
    event $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterVoid()
        {
            await VerifyAbsenceAsync(
@"class C {
    void $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterReadonly()
        {
            await VerifyKeywordAsync(
@"readonly $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInReadonlyStruct()
        {
            await VerifyKeywordAsync(
@"readonly $$ struct { }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInReadonlyStruct_AfterReadonly()
        {
            await VerifyKeywordAsync(
@"$$ readonly struct { }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(44423, "https://github.com/dotnet/roslyn/issues/44423")]
        public async Task TestAfterNew()
        {
            await VerifyAbsenceAsync(
@"new $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNewInClass()
        {
            await VerifyKeywordAsync(
@"class C { new $$ }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNewInStruct()
        {
            await VerifyKeywordAsync(
@"struct S { new $$ }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedNew()
        {
            await VerifyKeywordAsync(
@"class C {
   new $$");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInUnsafeBlock(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"unsafe {
    $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInUnsafeMethod()
        {
            await VerifyKeywordAsync(
@"class C {
   unsafe void Goo() {
     $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInUnsafeClass()
        {
            await VerifyKeywordAsync(
@"unsafe class C {
   void Goo() {
     $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInMemberArrowMethod()
        {
            await VerifyKeywordAsync(
@"unsafe class C {
   void Goo() {
     $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInMemberArrowProperty()
        {
            await VerifyKeywordAsync(
@" class C {
       ref int Goo() => $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInMemberArrowIndexer()
        {
            await VerifyKeywordAsync(
@" class C {
       ref int Goo => $$");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInLocalArrowMethod(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@" ref int Goo() => $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInArrowLambda(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@" D1 lambda = () => $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [WorkItem(21889, "https://github.com/dotnet/roslyn/issues/21889")]
        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [InlineData(SourceCodeKind.Regular, true)]
        [InlineData(SourceCodeKind.Regular, false)]
        [InlineData(SourceCodeKind.Script, true, Skip = "https://github.com/dotnet/roslyn/issues/44630")]
        [InlineData(SourceCodeKind.Script, false)]
        public async Task TestInConditionalExpressionTrueBranch(SourceCodeKind sourceCodeKind, bool topLevelStatement)
        {
            await VerifyWorkerAsync(
                AddInsideMethod(@"
ref int x = ref true ? $$", topLevelStatement: topLevelStatement),
                absent: false,
                options: sourceCodeKind == SourceCodeKind.Script ? Options.Script : CSharp9ParseOptions);
        }

        [WorkItem(21889, "https://github.com/dotnet/roslyn/issues/21889")]
        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [InlineData(SourceCodeKind.Regular, true)]
        [InlineData(SourceCodeKind.Regular, false)]
        [InlineData(SourceCodeKind.Script, true, Skip = "https://github.com/dotnet/roslyn/issues/44630")]
        [InlineData(SourceCodeKind.Script, false)]
        public async Task TestInConditionalExpressionFalseBranch(SourceCodeKind sourceCodeKind, bool topLevelStatement)
        {
            await VerifyWorkerAsync(
                AddInsideMethod(@"
int x = 0;
ref int y = ref true ? ref x : $$", topLevelStatement: topLevelStatement),
                absent: false,
                options: sourceCodeKind == SourceCodeKind.Script ? Options.Script : CSharp9ParseOptions);
        }

        [WorkItem(22253, "https://github.com/dotnet/roslyn/issues/22253")]
        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInLocalMethod(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@" void Goo(int test, $$) ", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestRefInFor(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(@"
for ($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestRefForeachVariable(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(@"
foreach ($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestRefExpressionInAssignment(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(@"
int x = 0;
ref int y = ref x;
y = $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestRefExpressionAfterReturn(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(@"
ref int x = ref (new int[1])[0];
return ref (x = $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestExtensionMethods_FirstParameter()
        {
            await VerifyKeywordAsync(
@"static class Extensions {
    static void Extension($$");
        }

        [WorkItem(30339, "https://github.com/dotnet/roslyn/issues/30339")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestExtensionMethods_FirstParameter_AfterThisKeyword()
        {
            await VerifyKeywordAsync(
@"static class Extensions {
    static void Extension(this $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestExtensionMethods_SecondParameter()
        {
            await VerifyKeywordAsync(
@"static class Extensions {
    static void Extension(this int i, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestExtensionMethods_SecondParameter_AfterThisKeyword()
        {
            await VerifyAbsenceAsync(
@"static class Extensions {
    static void Extension(this int i, this $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestExtensionMethods_FirstParameter_NonStaticClass()
        {
            await VerifyKeywordAsync(
@"class Extensions {
    static void Extension($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestExtensionMethods_FirstParameter_AfterThisKeyword_NonStaticClass()
        {
            await VerifyAbsenceAsync(
@"class Extensions {
    static void Extension(this $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestExtensionMethods_SecondParameter_NonStaticClass()
        {
            await VerifyKeywordAsync(
@"class Extensions {
    static void Extension(this int i, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestExtensionMethods_SecondParameter_AfterThisKeyword_NonStaticClass()
        {
            await VerifyAbsenceAsync(
@"class Extensions {
    static void Extension(this int i, this $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestExtensionMethods_FirstParameter_NonStaticMethod()
        {
            await VerifyKeywordAsync(
@"static class Extensions {
    void Extension($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestExtensionMethods_FirstParameter_AfterThisKeyword_NonStaticMethod()
        {
            await VerifyAbsenceAsync(
@"static class Extensions {
    void Extension(this $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestExtensionMethods_SecondParameter_NonStaticMethod()
        {
            await VerifyKeywordAsync(
@"static class Extensions {
    void Extension(this int i, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestExtensionMethods_SecondParameter_AfterThisKeyword_NonStaticMethod()
        {
            await VerifyAbsenceAsync(
@"static class Extensions {
    void Extension(this int i, this $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInFunctionPointerTypeNoExistingModifiers()
        {
            await VerifyKeywordAsync(@"
class C
{
    delegate*<$$");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNamespace()
        {
            await VerifyKeywordAsync(
@"namespace N { }
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterFileScopedNamespace()
        {
            await VerifyKeywordAsync(
@"namespace N;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(58906, "https://github.com/dotnet/roslyn/issues/58906")]
        public async Task TestInPotentialLambdaParamListParsedAsCastOnDifferentLines()
        {
            await VerifyKeywordAsync(
@"class C
{
    static void Main(string[] args)
    {
        var f = ($$)
        Main(null);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(58906, "https://github.com/dotnet/roslyn/issues/58906")]
        public async Task TestInPotentialLambdaParamListParsedAsCastOnSameLine()
        {
            await VerifyKeywordAsync(
@"class C
{
    static void Main(string[] args)
    {
        var f = ($$)Main(null);
    }
}");
        }
    }
}
