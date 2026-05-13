// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class ClosedKeywordRecommenderTests : KeywordRecommenderTests
{
    [Fact]
    public Task TestAtRoot()
        => VerifyKeywordAsync(
            "$$");

    [Fact]
    public Task TestAfterNamespace()
        => VerifyKeywordAsync(
            """
            namespace N {}
            $$
            """);

    [Fact]
    public Task TestInsideNamespace()
        => VerifyKeywordAsync(
            """
            namespace N {
                $$
            }
            """);

    [Fact]
    public Task TestAfterPublic()
        => VerifyKeywordAsync(
            "public $$");

    [Fact]
    public Task TestAfterFile()
        => VerifyKeywordAsync(SourceCodeKind.Regular,
            "file $$");

    [Fact]
    public Task TestAfterPartial()
        => VerifyKeywordAsync(
            "partial $$");

    [Fact]
    public Task TestInNestedType()
        => VerifyKeywordAsync(
            """
            class Outer {
                $$
            }
            """);

    [Fact]
    public Task TestNotAfterClosed()
        => VerifyAbsenceAsync(
            "closed $$");

    [Fact]
    public Task TestNotAfterSealed()
        => VerifyAbsenceAsync(
            "sealed $$");

    [Fact]
    public Task TestNotAfterStatic()
        => VerifyAbsenceAsync(
            "static $$");

    [Fact]
    public Task TestNotAfterAbstract()
        => VerifyAbsenceAsync(
            "abstract $$");
}
