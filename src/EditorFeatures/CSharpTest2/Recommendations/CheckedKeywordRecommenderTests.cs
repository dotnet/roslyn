// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class CheckedKeywordRecommenderTests : KeywordRecommenderTests
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
    public Task TestEmptyStatement()
        => VerifyKeywordAsync(AddInsideMethod(
@"$$"));

    [Fact]
    public Task TestInExpression()
        => VerifyKeywordAsync(AddInsideMethod(
@"var q = $$"));

    [Fact]
    public Task TestNotInPreProcessor()
        => VerifyAbsenceAsync(
@"#if a || $$");

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

    [Fact]
    public Task TestNotAfterImplicitOperator_01()
        => VerifyAbsenceAsync(
            """
            class Goo {
                public static implicit operator $$
            """);

    [Fact]
    public Task TestNotAfterImplicitOperator_02()
        => VerifyAbsenceAsync(
            """
            class Goo {
                public static implicit operator $$ int (Goo x){}
            """);

    [Fact]
    public Task TestAfterExplicitOperator_01()
        => VerifyKeywordAsync(
            """
            class Goo {
                public static explicit operator $$
            """);

    [Fact]
    public Task TestAfterExplicitOperator_02()
        => VerifyKeywordAsync(
            """
            class Goo {
                public static explicit /* some comment */ operator $$
            """);

    [Fact]
    public Task TestAfterExplicitOperator_03()
        => VerifyKeywordAsync(
            """
            class Goo {
                public static explicit operator $$ int (Goo x){}
            """);

    [Fact]
    public Task TestAfterOperator_01()
        => VerifyKeywordAsync(
            """
            class Goo { 
                public static int operator $$
            """);

    [Fact]
    public Task TestAfterOperator_02()
        => VerifyKeywordAsync(
            """
            class Goo { 
                public static int /* some comment */ operator $$
            """);

    [Fact]
    public Task TestAfterOperator_03()
        => VerifyKeywordAsync(
            """
            class Goo { 
                public static int operator $$ -(Goo x){}
            """);

    [Fact]
    public Task TestAfterOperator_04()
        => VerifyKeywordAsync(
            """
            class Goo { 
                public static int operator $$ -(Goo x, Goo y){}
            """);

    [Fact]
    public Task TestNotAfterOperator()
        => VerifyAbsenceAsync(
@"operator $$");

    [Fact]
    public Task TestNotAfterImplicitOperator_ExplicitImplementation_01()
        => VerifyAbsenceAsync(
            """
            class Goo {
                public static implicit I1.operator $$
            """);

    [Fact]
    public Task TestNotAfterImplicitOperator_ExplicitImplementation_02()
        => VerifyAbsenceAsync(
            """
            class Goo {
                public static implicit I1.operator $$ int (Goo x){}
            """);

    [Fact]
    public Task TestAfterExplicitOperator_ExplicitImplementation_01()
        => VerifyKeywordAsync(
            """
            class Goo {
                public static explicit I1.operator $$
            """);

    [Fact]
    public Task TestAfterExplicitOperator_ExplicitImplementation_02()
        => VerifyKeywordAsync(
            """
            class Goo {
                public static explicit /* some comment */ I1.operator $$
            """);

    [Fact]
    public Task TestAfterExplicitOperator_ExplicitImplementation_03()
        => VerifyKeywordAsync(
            """
            class Goo {
                public static explicit I1. /* some comment */ operator $$
            """);

    [Fact]
    public Task TestAfterExplicitOperator_ExplicitImplementation_04()
        => VerifyKeywordAsync(
            """
            class Goo {
                public static explicit I1.operator $$ int (Goo x){}
            """);

    [Fact]
    public Task TestAfterOperator_ExplicitImplementation_01()
        => VerifyKeywordAsync(
            """
            class Goo { 
                public static int I1.operator $$
            """);

    [Fact]
    public Task TestAfterOperator_ExplicitImplementation_02()
        => VerifyKeywordAsync(
            """
            class Goo { 
                public static int /* some comment */ I1.operator $$
            """);

    [Fact]
    public Task TestAfterOperator_ExplicitImplementation_03()
        => VerifyKeywordAsync(
            """
            class Goo { 
                public static int I1. /* some comment */ operator $$
            """);

    [Fact]
    public Task TestAfterOperator_ExplicitImplementation_04()
        => VerifyKeywordAsync(
            """
            class Goo { 
                public static int I1.operator $$ -(Goo x){}
            """);

    [Fact]
    public Task TestAfterOperator_ExplicitImplementation_05()
        => VerifyKeywordAsync(
            """
            class Goo { 
                public static int I1.operator $$ -(Goo x, Goo y){}
            """);
}
