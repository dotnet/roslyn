// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class ReferenceKeywordRecommenderTests : KeywordRecommenderTests
{
    [Fact]
    public Task TestNotAtRoot_Interactive()
        => VerifyAbsenceAsync(
@"$$");

    [Fact]
    public Task TestNotAfterClass_Interactive()
        => VerifyAbsenceAsync(
            """
            class C { }
            $$
            """);

    [Fact]
    public Task TestNotAfterGlobalStatement_Interactive()
        => VerifyAbsenceAsync(
            """
            System.Console.WriteLine();
            $$
            """);

    [Fact]
    public Task TestNotAfterGlobalVariableDeclaration_Interactive()
        => VerifyAbsenceAsync(
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
    public Task TestNotAfterHash()
        => VerifyAbsenceAsync(SourceCodeKind.Regular,
@"#$$");

    [Fact]
    public Task TestAfterHash_Interactive()
        => VerifyKeywordAsync(SourceCodeKind.Script,
@"#$$");

    [Fact]
    public Task TestNotAfterHashAndSpace()
        => VerifyAbsenceAsync(SourceCodeKind.Regular,
@"# $$");

    [Fact]
    public Task TestAfterHashAndSpace_Interactive()
        => VerifyKeywordAsync(SourceCodeKind.Script,
@"# $$");

    [Fact]
    public Task TestNestedPreprocessor()
        => VerifyKeywordAsync(SourceCodeKind.Script,
            """
            #if true
                #$$
            #endif
            """);

    [Fact]
    public Task TestBeforeUsing()
        => VerifyKeywordAsync(SourceCodeKind.Script,
            """
            #$$
            using System;
            """);

    [Fact]
    public Task TestBeforeGlobalUsing()
        => VerifyKeywordAsync(SourceCodeKind.Script,
            """
            #$$
            global using System;
            """);

    [Fact]
    public Task TestNotAfterUsing()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
            """
            using System;
            #$$
            """);

    [Fact]
    public Task TestNotAfterGlobalUsing()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
            """
            global using System;
            #$$
            """);
}
