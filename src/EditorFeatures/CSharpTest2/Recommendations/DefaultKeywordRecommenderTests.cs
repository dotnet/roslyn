// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class DefaultKeywordRecommenderTests : KeywordRecommenderTests
{
    [Fact]
    public Task TestAtRoot_Interactive()
        => VerifyKeywordAsync(SourceCodeKind.Script,
@"$$");

    [Fact]
    public Task TestAfterClass_Interactive()
        => VerifyKeywordAsync(SourceCodeKind.Script,
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
    public Task TestAfterGlobalVariableDeclaration_Interactive()
        => VerifyKeywordAsync(SourceCodeKind.Script,
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
    public Task TestNotInPreprocessor1()
        => VerifyAbsenceAsync(
            """
            class C {
            #$$
            """);

    [Fact]
    public Task TestNotInPreprocessor2()
        => VerifyAbsenceAsync(
            """
            class C {
            #if $$
            """);

    [Fact]
    public Task TestAfterHash()
        => VerifyKeywordAsync(
@"#line $$");

    [Fact]
    public Task TestAfterHashAndSpace()
        => VerifyKeywordAsync(
@"# line $$");

    [Fact]
    public Task TestInEmptyStatement()
        => VerifyKeywordAsync(AddInsideMethod(
@"$$"));

    [Fact]
    public Task TestInExpression()
        => VerifyKeywordAsync(AddInsideMethod(
@"var q = $$"));

    [Fact]
    public Task TestAfterSwitch()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            switch (expr) {
                $$
            """));

    [Fact]
    public Task TestAfterCase()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            switch (expr) {
                case 0:
                $$
            """));

    [Fact]
    public Task TestAfterDefault()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            switch (expr) {
                default:
                $$
            """));

    [Fact]
    public Task TestAfterOneStatement()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            switch (expr) {
                default:
                  Console.WriteLine();
                $$
            """));

    [Fact]
    public Task TestAfterTwoStatements()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            switch (expr) {
                default:
                  Console.WriteLine();
                  Console.WriteLine();
                $$
            """));

    [Fact]
    public Task TestAfterBlock()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            switch (expr) {
                default: {
                }
                $$
            """));

    [Fact]
    public Task TestAfterIfElse()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            switch (expr) {
                default:
                  if (goo) {
                  } else {
                  }
                $$
            """));

    [Fact]
    public Task TestAfterIncompleteStatement()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            switch (expr) {
                default:
                   Console.WriteLine(
                $$
            """));

    [Fact]
    public Task TestInsideBlock()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            switch (expr) {
                default: {
                  $$
            """));

    [Fact]
    public Task TestAfterCompleteIf()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            switch (expr) {
                default:
                  if (goo)
                    Console.WriteLine();
                $$
            """));

    [Fact]
    public Task TestAfterIncompleteIf()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            switch (expr) {
                default:
                  if (goo)
                    $$
            """));

    [Fact]
    public Task TestAfterWhile()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            switch (expr) {
                default:
                  while (true) {
                  }
                $$
            """));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/552717")]
    public Task TestNotAfterGotoInSwitch()
        => VerifyAbsenceAsync(AddInsideMethod(
            """
            switch (expr) {
                default:
                  goto $$
            """));

    [Fact]
    public Task TestNotAfterGotoOutsideSwitch()
        => VerifyAbsenceAsync(AddInsideMethod(
@"goto $$"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
    public Task TestNotInTypeOf()
        => VerifyAbsenceAsync(AddInsideMethod(
@"typeof($$"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
    public Task TestNotInDefault()
        => VerifyAbsenceAsync(AddInsideMethod(
@"default($$"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
    public Task TestNotInSizeOf()
        => VerifyAbsenceAsync(AddInsideMethod(
@"sizeof($$"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544219")]
    public Task TestNotInObjectInitializerMemberContext()
        => VerifyAbsenceAsync("""
            class C
            {
                public int x, y;
                void M()
                {
                    var c = new C { x = 2, y = 3, $$
            """);

    [Fact]
    public Task TestAfterRefExpression()
        => VerifyKeywordAsync(AddInsideMethod(
@"ref int x = ref $$"));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46283")]
    public Task TestInTypeParameterConstraint()
        => VerifyKeywordAsync(
            """
            class C
            {
                void M<T>() where T : $$
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46283")]
    public Task TestInTypeParameterConstraint_InOverride()
        => VerifyKeywordAsync(
            """
            class C : Base
            {
                public override void M<T>() where T : $$
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46283")]
    public Task TestInTypeParameterConstraint_InExplicitInterfaceImplementation()
        => VerifyKeywordAsync(
            """
            class C : I
            {
                public void I.M<T>() where T : $$
                {
                }
            }
            """);

    [Fact]
    public Task TestAfterLambdaOpenParen()
        => VerifyKeywordAsync(
@"var lam = ($$");

    [Fact]
    public Task TestAfterLambdaComma()
        => VerifyKeywordAsync(
@"var lam = (int i, $$");

    [Fact]
    public Task TestLambdaDefaultParameterValue()
        => VerifyKeywordAsync(
@"var lam = (int i = $$");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36472")]
    public Task InAmbiguousCast1()
        => VerifyKeywordAsync(
            """
            class C
            {
                static void Main(string[] args)
                {
                    (int i, string s) tuple;
                    tuple = ($$)
                    Main(args);
                }
            }
            """);
}
