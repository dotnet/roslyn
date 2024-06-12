// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public class ManagedKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact]
        public async Task TestInFunctionPointerDeclaration()
        {
            await VerifyKeywordAsync(
                """
                class Test {
                    unsafe void N() {
                        delegate* $$
                """);
        }

        [Fact]
        public async Task TestInFunctionPointerDeclarationTouchingAsterisk()
        {
            await VerifyKeywordAsync(
                """
                class Test {
                    unsafe void N() {
                        delegate*$$
                """);
        }
    }
}
