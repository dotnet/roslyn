// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class OperatorKeywordRecommenderTests : KeywordRecommenderTests
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
    public Task TestNotInEmptyStatement()
        => VerifyAbsenceAsync(AddInsideMethod(
@"$$"));

    [Fact]
    public Task TestAfterImplicit()
        => VerifyKeywordAsync(
            """
            class Goo {
                public static implicit $$
            """);

    [Fact]
    public Task TestAfterExplicit()
        => VerifyKeywordAsync(
            """
            class Goo {
                public static explicit $$
            """);

    [Fact]
    public Task TestNotAfterType()
        => VerifyAbsenceAsync(
            """
            class Goo {
                int $$
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542271")]
    public Task TestAfterPublicStaticType()
        => VerifyAbsenceAsync(
            """
            class Goo {
                public static int $$
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542271")]
    public Task TestAfterPublicStaticExternType()
        => VerifyAbsenceAsync(
            """
            class Goo {
                public static extern int $$
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542271")]
    public Task TestAfterGenericType()
        => VerifyAbsenceAsync(
            """
            class Goo {
                public static IList<int> $$
            """);

    [Fact]
    public Task TestNotInInterface()
        => VerifyAbsenceAsync(
            """
            interface Goo {
                public static int $$
            """);

    [Fact]
    public Task TestWithinExtension1()
        => VerifyAbsenceAsync(
            """
            static class C
            {
                extension(string s)
                {
                    $$
                }
            }
            """, CSharpNextParseOptions);

    [Fact]
    public Task TestWithinExtension2()
        => VerifyKeywordAsync(
            """
            static class C
            {
                extension(string s)
                {
                    public static explicit $$
                }
            }
            """,
            CSharpNextParseOptions,
            CSharpNextScriptParseOptions);
}
