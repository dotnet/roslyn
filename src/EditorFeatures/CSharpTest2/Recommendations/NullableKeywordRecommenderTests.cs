// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class NullableKeywordRecommenderTests : KeywordRecommenderTests
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

    [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public Task TestNotInGlobalUsingAlias()
        => VerifyAbsenceAsync(
@"global using Goo = $$");

    [Fact]
    public Task TestNotInEmptyStatement()
        => VerifyAbsenceAsync(AddInsideMethod(
@"$$"));

    [Fact]
    public Task TestAfterHash()
        => VerifyKeywordAsync(
@"#$$");

    [Fact]
    public Task TestAfterHashAndSpace()
        => VerifyKeywordAsync(
@"# $$");

    [Fact]
    public Task TestNotAfterHashAndNullable()
        => VerifyAbsenceAsync(
@"#nullable $$");

    [Fact]
    public async Task TestNotAfterPragma()
        => await VerifyAbsenceAsync(@"#pragma $$");

    [Fact]
    public async Task TestNotAfterPragmaWarning()
        => await VerifyAbsenceAsync(@"#pragma warning $$");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63594")]
    public async Task TestNotAfterPragmaWarningDisable()
        => await VerifyAbsenceAsync(@"#pragma warning disable $$");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63594")]
    public async Task TestNotAfterPragmaWarningEnable()
        => await VerifyAbsenceAsync(@"#pragma warning enable $$");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63594")]
    public async Task TestNotAfterPragmaWarningRestore()
        => await VerifyAbsenceAsync(@"#pragma warning restore $$");

    [Fact]
    public async Task TestNotAfterPragmaWarningSafeOnly()
        => await VerifyAbsenceAsync(@"#pragma warning safeonly $$");

    [Fact]
    public async Task TestNotAfterPragmaWarningSafeOnlyNullable()
        => await VerifyAbsenceAsync(@"#pragma warning safeonly nullable $$");

    [Fact]
    public async Task TestNotAfterPragmaWarningRestoreNullable()
        => await VerifyAbsenceAsync(@"#pragma warning restore nullable, $$");

    [Fact]
    public async Task TestNotAfterPragmaWarningDisableId()
        => await VerifyAbsenceAsync(@"#pragma warning disable 114, $$");
}
