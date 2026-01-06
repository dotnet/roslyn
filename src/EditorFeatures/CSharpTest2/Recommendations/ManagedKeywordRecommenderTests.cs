// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
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
}
