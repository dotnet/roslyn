// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class OutKeywordRecommenderTests : KeywordRecommenderTests
{
    [Fact]
    public Task TestNotAtRoot_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
@"$$");

    [Fact]
    public Task TestNotAfterClass_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
            """
            class C { }
            $$
            """);

    [Fact]
    public Task TestNotAfterGlobalStatement_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
            """
            System.Console.WriteLine();
            $$
            """);

    [Fact]
    public Task TestNotAfterGlobalVariableDeclaration_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
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
    public Task TestInterfaceTypeVarianceAfterAngle()
        => VerifyKeywordAsync(
@"interface IGoo<$$");

    [Fact]
    public Task TestInterfaceTypeVarianceNotAfterOut()
        => VerifyAbsenceAsync(
@"interface IGoo<in $$");

    [Fact]
    public Task TestInterfaceTypeVarianceAfterComma()
        => VerifyKeywordAsync(
@"interface IGoo<Goo, $$");

    [Fact]
    public Task TestInterfaceTypeVarianceAfterAttribute()
        => VerifyKeywordAsync(
@"interface IGoo<[Goo]$$");

    [Fact]
    public Task TestDelegateTypeVarianceAfterAngle()
        => VerifyKeywordAsync(
@"delegate void D<$$");

    [Fact]
    public Task TestDelegateTypeVarianceAfterComma()
        => VerifyKeywordAsync(
@"delegate void D<Goo, $$");

    [Fact]
    public Task TestDelegateTypeVarianceAfterAttribute()
        => VerifyKeywordAsync(
@"delegate void D<[Goo]$$");

    [Fact]
    public Task TestNotOutClassTypeVarianceAfterAngle()
        => VerifyAbsenceAsync(
@"class IGoo<$$");

    [Fact]
    public Task TestNotOutStructTypeVarianceAfterAngle()
        => VerifyAbsenceAsync(
@"struct IGoo<$$");

    [Fact]
    public Task TestNotOutBaseListAfterAngle()
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/24079")]
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
    public Task TestNotAfterRef()
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

    [Fact]
    public Task TestInLambdaDeclaration()
        => VerifyKeywordAsync(AddInsideMethod(
@"var q = ($$"));

    [Fact]
    public Task TestInLambdaDeclaration2()
        => VerifyKeywordAsync(AddInsideMethod(
@"var q = (ref int a, $$"));

    [Fact]
    public Task TestInLambdaDeclaration3()
        => VerifyKeywordAsync(AddInsideMethod(
@"var q = (int a, $$"));

    [Fact]
    public Task TestInDelegateDeclaration()
        => VerifyKeywordAsync(AddInsideMethod(
@"var q = delegate ($$"));

    [Fact]
    public Task TestInDelegateDeclaration2()
        => VerifyKeywordAsync(AddInsideMethod(
@"var q = delegate (a, $$"));

    [Fact]
    public Task TestInDelegateDeclaration3()
        => VerifyKeywordAsync(AddInsideMethod(
@"var q = delegate (int a, $$"));

    [Fact]
    public Task TestInCrefParameterList()
        => VerifyKeywordAsync("""
            Class c
            {
                /// <see cref="main($$"/>
                void main(out goo) { }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22253")]
    public Task TestInLocalFunction()
        => VerifyKeywordAsync(AddInsideMethod(
@"void F(int x, $$"));

    [Fact]
    public Task TestExtensionMethods_FirstParameter()
        => VerifyKeywordAsync(
            """
            static class Extensions {
                static void Extension($$
            """);

    [Fact]
    public Task TestExtensionMethods_FirstParameter_AfterThisKeyword()
        => VerifyAbsenceAsync(
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
}
