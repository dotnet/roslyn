// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class FileKeywordRecommenderTests : KeywordRecommenderTests
{
    [Fact]
    public Task TestAtRoot()
        => VerifyKeywordAsync(
            @"$$");

    [Fact]
    public Task TestAfterClass()
        => VerifyKeywordAsync(
            """
            class C { }
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
    public Task TestAfterExternAlias()
        => VerifyKeywordAsync(
            """
            extern alias Goo;
            $$
            """);

    [Fact]
    public Task TestAfterUsing()
        => VerifyKeywordAsync("""
            using Goo;
            $$
            """);

    [Fact]
    public Task TestAfterGlobalUsing()
        => VerifyKeywordAsync(
            """
            global using Goo;
            $$
            """);

    [Fact]
    public Task TestAfterNamespace()
        => VerifyKeywordAsync("""
            namespace N {}
            $$
            """);

    [Fact]
    public Task TestInsideNamespace()
        => VerifyKeywordAsync(
            """
            namespace N {
                $$
            """);

    [Fact]
    public Task TestInsideFileScopedNamespace()
        => VerifyKeywordAsync(
            """
            namespace N;
            $$
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/81028")]
    public Task TestNotAfterExternKeyword()
        => VerifyAbsenceAsync(
            @"extern $$");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/81028")]
    public Task TestNotAfterExternKeyword_InsideNamespace()
        => VerifyAbsenceAsync("""
            namespace N {
                extern $$
            """);

    [Fact]
    public Task TestAfterPreviousExternAlias_InsideNamespace()
        => VerifyKeywordAsync(
            """
            namespace N {
               extern alias Goo;
               $$
            """);

    [Fact]
    public Task TestAfterUsing_InsideNamespace()
        => VerifyKeywordAsync("""
            namespace N {
                using Goo;
                $$
            """);

    [Fact]
    public Task TestAfterMember_InsideNamespace()
        => VerifyKeywordAsync("""
            namespace N {
                class C {}
                $$
            """);

    [Fact]
    public Task TestNotInClass()
        => VerifyAbsenceAsync(
            """
            class C {
                $$
            """);

    [Fact]
    public Task TestNotInStruct()
        => VerifyAbsenceAsync(
            """
            struct S {
                $$
            """);

    [Fact]
    public Task TestNotInInterface()
        => VerifyAbsenceAsync(
            """
            interface I {
                $$
            """);

    [Fact]
    public Task TestNotInRecord()
        => VerifyAbsenceAsync(
            """
            record R {
                $$
            """);

    [Fact]
    public Task TestNotAfterPublic()
        => VerifyAbsenceAsync(
            @"public $$");

    [Fact]
    public Task TestNotAfterInternal()
        => VerifyAbsenceAsync(
            @"internal $$");

    [Fact]
    public Task TestAfterStatic()
        => VerifyKeywordAsync(
            @"static $$");

    [Fact]
    public Task TestAfterPartial()
        => VerifyKeywordAsync(
            @"partial $$");

    [Fact]
    public Task TestAfterAbstract()
        => VerifyKeywordAsync(
            @"abstract $$");

    [Fact]
    public Task TestAfterSealed()
        => VerifyKeywordAsync(
            @"sealed $$");
}
