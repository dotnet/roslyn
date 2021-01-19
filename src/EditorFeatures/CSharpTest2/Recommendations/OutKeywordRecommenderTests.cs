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
    public class OutKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAtRoot_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterClass_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"class C { }
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterGlobalStatement_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"System.Console.WriteLine();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterGlobalVariableDeclaration_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
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
        public async Task TestInterfaceTypeVarianceAfterAngle()
        {
            VerifyKeyword(
@"interface IGoo<$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInterfaceTypeVarianceNotAfterOut()
        {
            VerifyAbsence(
@"interface IGoo<in $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInterfaceTypeVarianceAfterComma()
        {
            VerifyKeyword(
@"interface IGoo<Goo, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInterfaceTypeVarianceAfterAttribute()
        {
            VerifyKeyword(
@"interface IGoo<[Goo]$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestDelegateTypeVarianceAfterAngle()
        {
            VerifyKeyword(
@"delegate void D<$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestDelegateTypeVarianceAfterComma()
        {
            VerifyKeyword(
@"delegate void D<Goo, $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestDelegateTypeVarianceAfterAttribute()
        {
            VerifyKeyword(
@"delegate void D<[Goo]$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotOutClassTypeVarianceAfterAngle()
        {
            VerifyAbsence(
@"class IGoo<$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotOutStructTypeVarianceAfterAngle()
        {
            VerifyAbsence(
@"struct IGoo<$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotOutBaseListAfterAngle()
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
        [WorkItem(24079, "https://github.com/dotnet/roslyn/issues/24079")]
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
        public async Task TestNotAfterRef()
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

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInLambdaDeclaration()
        {
            VerifyKeyword(AddInsideMethod(
@"var q = ($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInLambdaDeclaration2()
        {
            VerifyKeyword(AddInsideMethod(
@"var q = (ref int a, $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInLambdaDeclaration3()
        {
            VerifyKeyword(AddInsideMethod(
@"var q = (int a, $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInDelegateDeclaration()
        {
            VerifyKeyword(AddInsideMethod(
@"var q = delegate ($$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInDelegateDeclaration2()
        {
            VerifyKeyword(AddInsideMethod(
@"var q = delegate (a, $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInDelegateDeclaration3()
        {
            VerifyKeyword(AddInsideMethod(
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

            VerifyKeyword(text);
        }

        [WorkItem(22253, "https://github.com/dotnet/roslyn/issues/22253")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInLocalFunction()
        {
            VerifyKeyword(AddInsideMethod(
@"void F(int x, $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestExtensionMethods_FirstParameter()
        {
            VerifyKeyword(
@"static class Extensions {
    static void Extension($$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestExtensionMethods_FirstParameter_AfterThisKeyword()
        {
            VerifyAbsence(
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
