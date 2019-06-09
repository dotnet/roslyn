// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class RefKeywordRecommenderTests : KeywordRecommenderTests
    {
        private async Task VerifyKeywordWithRefsAsync(SourceCodeKind kind, string text)
        {
            switch (kind)
            {
                case SourceCodeKind.Regular:
                    await VerifyWorkerAsync(text, absent: false, options: Options.Regular);
                    break;

                case SourceCodeKind.Script:
                    await VerifyWorkerAsync(text, absent: false, options: Options.Script);
                    break;
            }
        }

        private async Task VerifyAbsenceWithRefsAsync(SourceCodeKind kind, string text)
        {
            switch (kind)
            {
                case SourceCodeKind.Regular:
                    await VerifyWorkerAsync(text, absent: true, options: Options.Regular);
                    break;

                case SourceCodeKind.Script:
                    await VerifyWorkerAsync(text, absent: true, options: Options.Script);
                    break;
            }
        }

        private async Task VerifyKeywordWithRefsAsync(string text)
        {
            // run the verification in both context(normal and script)
            await VerifyKeywordWithRefsAsync(SourceCodeKind.Regular, text);
            await VerifyKeywordWithRefsAsync(SourceCodeKind.Script, text);
        }

        private async Task VerifyAbsenceWithRefsAsync(string text)
        {
            // run the verification in both context(normal and script)
            await VerifyAbsenceWithRefsAsync(SourceCodeKind.Regular, text);
            await VerifyAbsenceWithRefsAsync(SourceCodeKind.Script, text);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAtRoot_Interactive()
        {
            await VerifyKeywordWithRefsAsync(SourceCodeKind.Script,
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterClass_Interactive()
        {
            await VerifyKeywordWithRefsAsync(SourceCodeKind.Script,
@"class C { }
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterGlobalStatement_Interactive()
        {
            await VerifyKeywordWithRefsAsync(SourceCodeKind.Script,
@"System.Console.WriteLine();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterGlobalVariableDeclaration_Interactive()
        {
            await VerifyKeywordWithRefsAsync(SourceCodeKind.Script,
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

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInLambdaDeclaration()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = ($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInLambdaDeclaration2()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = (ref int a, $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInLambdaDeclaration3()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = (int a, $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInDelegateDeclaration()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = delegate ($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInDelegateDeclaration2()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = delegate (a, $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInDelegateDeclaration3()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = delegate (int a, $$"));
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


        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestEmptyStatement()
        {
            await VerifyKeywordWithRefsAsync(AddInsideMethod(
@"$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterReturn()
        {
            await VerifyKeywordWithRefsAsync(AddInsideMethod(
@"return $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInFor()
        {
            await VerifyKeywordWithRefsAsync(AddInsideMethod(
@"for ($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInFor()
        {
            await VerifyAbsenceWithRefsAsync(AddInsideMethod(
@"for (var $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInFor2()
        {
            await VerifyKeywordWithRefsAsync(AddInsideMethod(
@"for ($$;"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInFor3()
        {
            await VerifyKeywordWithRefsAsync(AddInsideMethod(
@"for ($$;;"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterVar()
        {
            await VerifyAbsenceWithRefsAsync(AddInsideMethod(
@"var $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInUsing()
        {
            await VerifyAbsenceWithRefsAsync(AddInsideMethod(
@"using ($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInsideStruct()
        {
            await VerifyKeywordWithRefsAsync(
@"struct S {
   $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInsideInterface()
        {
            await VerifyKeywordWithRefsAsync(
@"interface I {
   $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInsideClass()
        {
            await VerifyKeywordWithRefsAsync(
@"class C {
   $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPartial()
        {
            await VerifyKeywordAsync(@"partial $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedPartial()
        {
            await VerifyKeywordWithRefsAsync(
@"class C {
    partial $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterAbstract()
        {
            await VerifyKeywordWithRefsAsync(@"abstract $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedAbstract()
        {
            await VerifyKeywordWithRefsAsync(
@"class C {
    abstract $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterInternal()
        {
            await VerifyKeywordWithRefsAsync(SourceCodeKind.Regular, @"internal $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterInternal_Interactive()
        {
            await VerifyKeywordWithRefsAsync(SourceCodeKind.Script, @"internal $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedInternal()
        {
            await VerifyKeywordWithRefsAsync(
@"class C {
    internal $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPublic()
        {
            await VerifyKeywordWithRefsAsync(SourceCodeKind.Regular, @"public $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPublic_Interactive()
        {
            await VerifyKeywordWithRefsAsync(SourceCodeKind.Script, @"public $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedPublic()
        {
            await VerifyKeywordWithRefsAsync(
@"class C {
    public $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPrivate()
        {
            await VerifyKeywordWithRefsAsync(SourceCodeKind.Regular,
@"private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPrivate_Script()
        {
            await VerifyKeywordWithRefsAsync(SourceCodeKind.Script,
@"private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedPrivate()
        {
            await VerifyKeywordWithRefsAsync(
@"class C {
    private $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterProtected()
        {
            await VerifyKeywordWithRefsAsync(
@"protected $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedProtected()
        {
            await VerifyKeywordWithRefsAsync(
@"class C {
    protected $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterSealed()
        {
            await VerifyKeywordWithRefsAsync(@"sealed $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedSealed()
        {
            await VerifyKeywordWithRefsAsync(
@"class C {
    sealed $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterStatic()
        {
            await VerifyKeywordWithRefsAsync(SourceCodeKind.Regular, @"static $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterStatic_Interactive()
        {
            await VerifyKeywordWithRefsAsync(SourceCodeKind.Script, @"static $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterStatic_InClass()
        {
            await VerifyKeywordWithRefsAsync(
@"class C {
    static $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterStaticPublic()
        {
            await VerifyKeywordWithRefsAsync(SourceCodeKind.Regular, @"static public $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterStaticPublic_Interactive()
        {
            await VerifyKeywordWithRefsAsync(SourceCodeKind.Script, @"static public $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedStaticPublic()
        {
            await VerifyKeywordWithRefsAsync(
@"class C {
    static public $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterDelegate()
        {
            await VerifyKeywordWithRefsAsync(
@"delegate $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterAnonymousDelegate()
        {
            await VerifyAbsenceWithRefsAsync(AddInsideMethod(
@"var q = delegate $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterEvent()
        {
            await VerifyAbsenceWithRefsAsync(
@"class C {
    event $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterVoid()
        {
            await VerifyAbsenceWithRefsAsync(
@"class C {
    void $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterReadonly()
        {
            await VerifyKeywordWithRefsAsync(SourceCodeKind.Regular,
@"readonly $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInReadonlyStruct()
        {
            await VerifyKeywordWithRefsAsync(SourceCodeKind.Regular,
@"readonly $$ struct { }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInReadonlyStruct_AfterReadonly()
        {
            await VerifyKeywordWithRefsAsync(SourceCodeKind.Regular,
@"$$ readonly struct { }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNew()
        {
            await VerifyKeywordWithRefsAsync(SourceCodeKind.Regular,
@"new $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNewInClass()
        {
            await VerifyKeywordWithRefsAsync(
@"class C { new $$ }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNewInStruct()
        {
            await VerifyKeywordWithRefsAsync(
@"struct S { new $$ }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterNestedNew()
        {
            await VerifyKeywordWithRefsAsync(
@"class C {
   new $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInUnsafeBlock()
        {
            await VerifyKeywordWithRefsAsync(AddInsideMethod(
@"unsafe {
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInUnsafeMethod()
        {
            await VerifyKeywordWithRefsAsync(
@"class C {
   unsafe void Goo() {
     $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInUnsafeClass()
        {
            await VerifyKeywordWithRefsAsync(
@"unsafe class C {
   void Goo() {
     $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInMemberArrowMethod()
        {
            await VerifyKeywordWithRefsAsync(
@"unsafe class C {
   void Goo() {
     $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInMemberArrowProperty()
        {
            await VerifyKeywordWithRefsAsync(
@" class C {
       ref int Goo() => $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInMemberArrowIndexer()
        {
            await VerifyKeywordWithRefsAsync(
@" class C {
       ref int Goo => $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInLocalArrowMethod()
        {
            await VerifyKeywordWithRefsAsync(AddInsideMethod(
@" ref int Goo() => $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInArrowLambda()
        {
            await VerifyKeywordWithRefsAsync(AddInsideMethod(
@" D1 lambda = () => $$"));
        }

        [WorkItem(21889, "https://github.com/dotnet/roslyn/issues/21889")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInConditionalExpressionTrueBranch()
        {
            await VerifyKeywordWithRefsAsync(AddInsideMethod(@"
ref int x = ref true ? $$"));
        }

        [WorkItem(21889, "https://github.com/dotnet/roslyn/issues/21889")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInConditionalExpressionFalseBranch()
        {
            await VerifyKeywordWithRefsAsync(AddInsideMethod(@"
int x = 0;
ref int y = ref true ? ref x : $$"));
        }

        [WorkItem(22253, "https://github.com/dotnet/roslyn/issues/22253")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInLocalMethod()
        {
            await VerifyKeywordWithRefsAsync(AddInsideMethod(
@" void Goo(int test, $$) "));

        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestRefInFor()
        {
            await VerifyKeywordWithRefsAsync(AddInsideMethod(@"
for ($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestRefForeachVariable()
        {
            await VerifyKeywordWithRefsAsync(AddInsideMethod(@"
foreach ($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestRefExpressionInAssignment()
        {
            await VerifyKeywordWithRefsAsync(AddInsideMethod(@"
int x = 0;
ref int y = ref x;
y = $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestRefExpressionAfterReturn()
        {
            await VerifyKeywordWithRefsAsync(AddInsideMethod(@"
ref int x = ref (new int[1])[0];
return ref (x = $$"));
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
    }
}
