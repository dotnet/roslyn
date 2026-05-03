// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class WarningsKeywordRecommenderTests : KeywordRecommenderTests
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
    public Task TestAfterHash()
        => VerifyAbsenceAsync(
@"#$$");

    [Fact]
    public Task TestAfterHashAndSpace()
        => VerifyAbsenceAsync(
@"# $$");

    [Fact]
    public Task TestAfterPragma()
        => VerifyAbsenceAsync(
@"#pragma $$");

    [Fact]
    public Task TestAfterNullable()
        => VerifyAbsenceAsync(
@"#nullable $$");

    [Fact]
    public Task TestAfterNullableEnable()
        => VerifyKeywordAsync(
@"#nullable enable $$");

    [Fact]
    public Task TestAfterNullableDisable()
        => VerifyKeywordAsync(
@"#nullable disable $$");

    [Fact]
    public Task TestAfterNullableRestore()
        => VerifyKeywordAsync(
@"#nullable restore $$");

    [Fact]
    public Task TestAfterNullableBadSetting()
        => VerifyAbsenceAsync(
@"#nullable true $$");
}
