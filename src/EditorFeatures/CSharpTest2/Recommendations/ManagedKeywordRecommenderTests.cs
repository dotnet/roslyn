// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class ManagedKeywordRecommenderTests : KeywordRecommenderTests
{
    [Fact]
    public Task TestInFunctionPointerDeclaration()
        => VerifyKeywordAsync(
            """
            class Test {
                unsafe void N() {
                    delegate* $$
            """);

    [Fact]
    public Task TestInFunctionPointerDeclarationTouchingAsterisk()
        => VerifyKeywordAsync(
            """
            class Test {
                unsafe void N() {
                    delegate*$$
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/81015")]
    public Task TestInCastExpressionAfterTyping()
        => VerifyKeywordAsync(
            """
            class C
            {
                unsafe static void M()
                {
                    _ = (delegate*$$)&M;
                }
            }
            """);
}
