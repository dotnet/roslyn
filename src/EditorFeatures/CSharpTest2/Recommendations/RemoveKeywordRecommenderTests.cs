// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class RemoveKeywordRecommenderTests : KeywordRecommenderTests
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
    public Task TestAfterEvent()
        => VerifyKeywordAsync(
            """
            class C {
               event Goo Bar { $$
            """);

    [Fact]
    public Task TestAfterAttribute()
        => VerifyKeywordAsync(
            """
            class C {
               event Goo Bar { [Bar] $$
            """);

    [Fact]
    public Task TestAfterAdd()
        => VerifyKeywordAsync(
            """
            class C {
               event Goo Bar { add { } $$
            """);

    [Fact]
    public Task TestAfterAddAndAttribute()
        => VerifyKeywordAsync(
            """
            class C {
               event Goo Bar { add { } [Bar] $$
            """);

    [Fact]
    public Task TestAfterAddBlock()
        => VerifyKeywordAsync(
            """
            class C {
               event Goo Bar { add { } $$
            """);

    [Fact]
    public Task TestNotAfterRemoveKeyword()
        => VerifyAbsenceAsync(
            """
            class C {
               event Goo Bar { remove $$
            """);

    [Fact]
    public Task TestNotAfterRemoveAccessor()
        => VerifyAbsenceAsync(
            """
            class C {
               event Goo Bar { remove { } $$
            """);

    [Fact]
    public Task TestNotInProperty()
        => VerifyAbsenceAsync(
            """
            class C {
               int Goo { $$
            """);
}
