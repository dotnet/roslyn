// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class EnableKeywordRecommenderTests : KeywordRecommenderTests
{
    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31130")]
    public async Task TestAfterNullable()
        => await VerifyKeywordAsync(@"#nullable $$");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31130")]
    public Task TestNotAfterNullableAndNewline()
        => VerifyAbsenceAsync("""
            #nullable 
            $$

            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31130")]
    public async Task TestNotAfterHash()
        => await VerifyAbsenceAsync(@"#$$");

    [Fact]
    public async Task TestNotAtRoot_Interactive()
        => await VerifyAbsenceAsync(SourceCodeKind.Script, @"$$");

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
    public async Task TestNotInUsingAlias()
        => await VerifyAbsenceAsync(@"using Goo = $$");

    [Fact]
    public async Task TestNotInGlobalUsingAlias()
        => await VerifyAbsenceAsync(@"global using Goo = $$");

    [Fact]
    public async Task TestNotInEmptyStatement()
        => await VerifyAbsenceAsync(AddInsideMethod(@"$$"));

    [Fact]
    public async Task TestNotAfterPragma()
        => await VerifyAbsenceAsync(@"#pragma $$");

    [Fact]
    public async Task TestAfterPragmaWarning()
        => await VerifyKeywordAsync(@"#pragma warning $$");

    [Fact]
    public async Task TestNotAfterPragmaWarningEnable()
        => await VerifyAbsenceAsync(@"#pragma warning enable $$");
}
