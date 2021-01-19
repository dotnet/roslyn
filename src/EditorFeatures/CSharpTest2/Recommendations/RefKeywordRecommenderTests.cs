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
    public class RefKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAtRoot()
        {
            VerifyKeyword(
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterClass()
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
        public async Task TestNotAfterAngle()
        {
            VerifyAbsence(
@"interface IGoo<$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInterfaceTypeVarianceNotAfterIn()
        {
            VerifyAbsence(
@"interface IGoo<in $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInterfaceTypeVarianceNotAfterComma()
        {
            VerifyAbsence(
@"interface IGoo<Goo, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInterfaceTypeVarianceNotAfterAttribute()
        {
            VerifyAbsence(
@"interface IGoo<[Goo]$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestDelegateTypeVarianceNotAfterAngle()
        {
            VerifyAbsence(
@"delegate void D<$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestDelegateTypeVarianceNotAfterComma()
        {
            VerifyAbsence(
@"delegate void D<Goo, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestDelegateTypeVarianceNotAfterAttribute()
        {
            VerifyAbsence(
@"delegate void D<[Goo]$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotRefBaseListAfterAngle()
        {
            VerifyAbsence(
@"interface IGoo : Bar<$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInGenericMethod()
        {
            VerifyAbsence(
@"interface IGoo {
    void Goo<$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterRef()
        {
            VerifyAbsence(
@"class C {
    void Goo(ref $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterOut()
        {
            VerifyAbsence(
@"class C {
    void Goo(out $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterMethodOpenParen()
        {
            VerifyKeyword(
@"class C {
    void Goo($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterMethodComma()
        {
            VerifyKeyword(
@"class C {
    void Goo(int i, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterMethodAttribute()
        {
            VerifyKeyword(
@"class C {
    void Goo(int i, [Goo]$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterConstructorOpenParen()
        {
            VerifyKeyword(
@"class C {
    public C($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterConstructorComma()
        {
            VerifyKeyword(
@"class C {
    public C(int i, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterConstructorAttribute()
        {
            VerifyKeyword(
@"class C {
    public C(int i, [Goo]$$");
        }

        [WorkItem(933972, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/933972")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterThisConstructorInitializer()
        {
            VerifyKeyword(
@"class C {
    public C():this($$");
        }

        [WorkItem(933972, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/933972")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterThisConstructorInitializerNamedArgument()
        {
            VerifyKeyword(
@"class C {
    public C():this(Goo:$$");
        }

        [WorkItem(933972, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/933972")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterBaseConstructorInitializer()
        {
            VerifyKeyword(
@"class C {
    public C():base($$");
        }

        [WorkItem(933972, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/933972")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterBaseConstructorInitializerNamedArgument()
        {
            VerifyKeyword(
@"class C {
    public C():base(5, Goo:$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterDelegateOpenParen()
        {
            VerifyKeyword(
@"delegate void D($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterDelegateComma()
        {
            VerifyKeyword(
@"delegate void D(int i, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterDelegateAttribute()
        {
            VerifyKeyword(
@"delegate void D(int i, [Goo]$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterOperator()
        {
            VerifyAbsence(
@"class C {
    static int operator +($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterDestructor()
        {
            VerifyAbsence(
@"class C {
    ~C($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterIndexer()
        {
            VerifyAbsence(
@"class C {
    int this[$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInObjectCreationAfterOpenParen()
        {
            VerifyKeyword(
@"class C {
    void Goo() {
      new Bar($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterRefParam()
        {
            VerifyAbsence(
@"class C {
    void Goo() {
      new Bar(ref $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterOutParam()
        {
            VerifyAbsence(
@"class C {
    void Goo() {
      new Bar(out $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInObjectCreationAfterComma()
        {
            VerifyKeyword(
@"class C {
    void Goo() {
      new Bar(baz, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInObjectCreationAfterSecondComma()
        {
            VerifyKeyword(
@"class C {
    void Goo() {
      new Bar(baz, quux, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInObjectCreationAfterSecondNamedParam()
        {
            VerifyKeyword(
@"class C {
    void Goo() {
      new Bar(baz: 4, quux: $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInInvocationExpression()
        {
            VerifyKeyword(
@"class C {
    void Goo() {
      Bar($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInInvocationAfterComma()
        {
            VerifyKeyword(
@"class C {
    void Goo() {
      Bar(baz, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInInvocationAfterSecondComma()
        {
            VerifyKeyword(
@"class C {
    void Goo() {
      Bar(baz, quux, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInInvocationAfterSecondNamedParam()
        {
            VerifyKeyword(
@"class C {
    void Goo() {
      Bar(baz: 4, quux: $$");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInLambdaDeclaration(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"var q = ($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInLambdaDeclaration2(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"var q = (ref int a, $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInLambdaDeclaration3(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"var q = (int a, $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInDelegateDeclaration(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"var q = delegate ($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInDelegateDeclaration2(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"var q = delegate (a, $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInDelegateDeclaration3(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
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

            VerifyKeyword(text);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestEmptyStatement(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"$$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestAfterReturn(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"return $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInFor(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"for ($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestNotInFor(bool topLevelStatement)
        {
            VerifyAbsence(AddInsideMethod(
@"for (var $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInFor2(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"for ($$;", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInFor3(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"for ($$;;", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestNotAfterVar(bool topLevelStatement)
        {
            VerifyAbsence(AddInsideMethod(
@"var $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestNotInUsing(bool topLevelStatement)
        {
            VerifyAbsence(AddInsideMethod(
@"using ($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
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
            VerifyKeyword(
@"interface I {
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
        public async Task TestAfterNestedPartial()
        {
            VerifyKeyword(
@"class C {
    partial $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterAbstract()
            => VerifyKeyword(@"abstract $$");

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedAbstract()
        {
            VerifyKeyword(
@"class C {
    abstract $$");
        }

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
            VerifyKeyword(
@"private $$");
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
        public async Task TestAfterStatic_InClass()
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
        public async Task TestAfterDelegate()
        {
            VerifyKeyword(
@"delegate $$");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestNotAfterAnonymousDelegate(bool topLevelStatement)
        {
            VerifyAbsence(AddInsideMethod(
@"var q = delegate $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterEvent()
        {
            VerifyAbsence(
@"class C {
    event $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterVoid()
        {
            VerifyAbsence(
@"class C {
    void $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterReadonly()
        {
            VerifyKeyword(
@"readonly $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInReadonlyStruct()
        {
            VerifyKeyword(
@"readonly $$ struct { }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInReadonlyStruct_AfterReadonly()
        {
            VerifyKeyword(
@"$$ readonly struct { }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [WorkItem(44423, "https://github.com/dotnet/roslyn/issues/44423")]
        public async Task TestAfterNew()
        {
            VerifyAbsence(
@"new $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNewInClass()
        {
            VerifyKeyword(
@"class C { new $$ }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNewInStruct()
        {
            VerifyKeyword(
@"struct S { new $$ }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedNew()
        {
            VerifyKeyword(
@"class C {
   new $$");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInUnsafeBlock(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@"unsafe {
    $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInUnsafeMethod()
        {
            VerifyKeyword(
@"class C {
   unsafe void Goo() {
     $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInUnsafeClass()
        {
            VerifyKeyword(
@"unsafe class C {
   void Goo() {
     $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInMemberArrowMethod()
        {
            VerifyKeyword(
@"unsafe class C {
   void Goo() {
     $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInMemberArrowProperty()
        {
            VerifyKeyword(
@" class C {
       ref int Goo() => $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInMemberArrowIndexer()
        {
            VerifyKeyword(
@" class C {
       ref int Goo => $$");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInLocalArrowMethod(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
@" ref int Goo() => $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestInArrowLambda(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(
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
            VerifyWorker(
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
            VerifyWorker(
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
            VerifyKeyword(AddInsideMethod(
@" void Goo(int test, $$) ", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestRefInFor(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(@"
for ($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestRefForeachVariable(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(@"
foreach ($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestRefExpressionInAssignment(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(@"
int x = 0;
ref int y = ref x;
y = $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        [CombinatorialData]
        public async Task TestRefExpressionAfterReturn(bool topLevelStatement)
        {
            VerifyKeyword(AddInsideMethod(@"
ref int x = ref (new int[1])[0];
return ref (x = $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestExtensionMethods_FirstParameter()
        {
            VerifyKeyword(
@"static class Extensions {
    static void Extension($$");
        }

        [WorkItem(30339, "https://github.com/dotnet/roslyn/issues/30339")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestExtensionMethods_FirstParameter_AfterThisKeyword()
        {
            VerifyKeyword(
@"static class Extensions {
    static void Extension(this $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestExtensionMethods_SecondParameter()
        {
            VerifyKeyword(
@"static class Extensions {
    static void Extension(this int i, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestExtensionMethods_SecondParameter_AfterThisKeyword()
        {
            VerifyAbsence(
@"static class Extensions {
    static void Extension(this int i, this $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestExtensionMethods_FirstParameter_NonStaticClass()
        {
            VerifyKeyword(
@"class Extensions {
    static void Extension($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestExtensionMethods_FirstParameter_AfterThisKeyword_NonStaticClass()
        {
            VerifyAbsence(
@"class Extensions {
    static void Extension(this $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestExtensionMethods_SecondParameter_NonStaticClass()
        {
            VerifyKeyword(
@"class Extensions {
    static void Extension(this int i, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestExtensionMethods_SecondParameter_AfterThisKeyword_NonStaticClass()
        {
            VerifyAbsence(
@"class Extensions {
    static void Extension(this int i, this $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestExtensionMethods_FirstParameter_NonStaticMethod()
        {
            VerifyKeyword(
@"static class Extensions {
    void Extension($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestExtensionMethods_FirstParameter_AfterThisKeyword_NonStaticMethod()
        {
            VerifyAbsence(
@"static class Extensions {
    void Extension(this $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestExtensionMethods_SecondParameter_NonStaticMethod()
        {
            VerifyKeyword(
@"static class Extensions {
    void Extension(this int i, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestExtensionMethods_SecondParameter_AfterThisKeyword_NonStaticMethod()
        {
            VerifyAbsence(
@"static class Extensions {
    void Extension(this int i, this $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInFunctionPointerTypeNoExistingModifiers()
        {
            VerifyKeyword(@"
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
            VerifyAbsence($@"
class C
{{
    delegate*<{modifier} $$");
        }
    }
}
