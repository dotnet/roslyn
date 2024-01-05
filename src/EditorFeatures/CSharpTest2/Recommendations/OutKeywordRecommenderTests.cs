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
    public class OutKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact]
        public async Task TestNotAtRoot_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"$$");
        }

        [Fact]
        public async Task TestNotAfterClass_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
                """
                class C { }
                $$
                """);
        }

        [Fact]
        public async Task TestNotAfterGlobalStatement_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
                """
                System.Console.WriteLine();
                $$
                """);
        }

        [Fact]
        public async Task TestNotAfterGlobalVariableDeclaration_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
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
        public async Task TestInterfaceTypeVarianceAfterAngle()
        {
            await VerifyKeywordAsync(
@"interface IGoo<$$");
        }

        [Fact]
        public async Task TestInterfaceTypeVarianceNotAfterOut()
        {
            await VerifyAbsenceAsync(
@"interface IGoo<in $$");
        }

        [Fact]
        public async Task TestInterfaceTypeVarianceAfterComma()
        {
            await VerifyKeywordAsync(
@"interface IGoo<Goo, $$");
        }

        [Fact]
        public async Task TestInterfaceTypeVarianceAfterAttribute()
        {
            await VerifyKeywordAsync(
@"interface IGoo<[Goo]$$");
        }

        [Fact]
        public async Task TestDelegateTypeVarianceAfterAngle()
        {
            await VerifyKeywordAsync(
@"delegate void D<$$");
        }

        [Fact]
        public async Task TestDelegateTypeVarianceAfterComma()
        {
            await VerifyKeywordAsync(
@"delegate void D<Goo, $$");
        }

        [Fact]
        public async Task TestDelegateTypeVarianceAfterAttribute()
        {
            await VerifyKeywordAsync(
@"delegate void D<[Goo]$$");
        }

        [Fact]
        public async Task TestNotOutClassTypeVarianceAfterAngle()
        {
            await VerifyAbsenceAsync(
@"class IGoo<$$");
        }

        [Fact]
        public async Task TestNotOutStructTypeVarianceAfterAngle()
        {
            await VerifyAbsenceAsync(
@"struct IGoo<$$");
        }

        [Fact]
        public async Task TestNotOutBaseListAfterAngle()
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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/24079")]
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
        public async Task TestNotAfterRef()
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

        [Fact]
        public async Task TestInLambdaDeclaration()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = ($$"));
        }

        [Fact]
        public async Task TestInLambdaDeclaration2()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = (ref int a, $$"));
        }

        [Fact]
        public async Task TestInLambdaDeclaration3()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = (int a, $$"));
        }

        [Fact]
        public async Task TestInDelegateDeclaration()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = delegate ($$"));
        }

        [Fact]
        public async Task TestInDelegateDeclaration2()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = delegate (a, $$"));
        }

        [Fact]
        public async Task TestInDelegateDeclaration3()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = delegate (int a, $$"));
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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22253")]
        public async Task TestInLocalFunction()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"void F(int x, $$"));
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

        [Fact]
        public async Task TestExtensionMethods_FirstParameter_AfterThisKeyword()
        {
            await VerifyAbsenceAsync(
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
    }
}
