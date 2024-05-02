// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public sealed class ExtensionKeywordRecommenderTests : KeywordRecommenderTests
    {
        private static readonly CSharpParseOptions s_previewParseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);

        [Fact]
        public async Task TestNotAtRoot()
        {
            await VerifyAbsenceAsync(@"$$");
        }

        [Fact]
        public async Task TestNotAfterClassDeclaration()
        {
            await VerifyAbsenceAsync(
                """
                class C { }
                $$
                """);
        }

        [Fact]
        public async Task TestAfterImplicitTopLevel()
        {
            await VerifyKeywordAsync(
                """
                implicit $$
                """);
        }

        [Fact]
        public async Task TestAfterExplicitTopLevel()
        {
            await VerifyKeywordAsync(
                """
                explicit $$
                """);
        }

        [Fact]
        public async Task TestAfterImplicitWithAccessibilityTopLevel()
        {
            await VerifyKeywordAsync(
                """
                public implicit $$
                """);
        }

        [Fact]
        public async Task TestAfterExplicitWithAccessibilityTopLevel()
        {
            await VerifyKeywordAsync(
                """
                public implicit $$
                """);
        }

        [Fact]
        public async Task TestAfterNew()
        {
            await VerifyKeywordAsync(
                """
                new implicit $$
                """);
        }

        [Fact]
        public async Task TestAfterStatic()
        {
            await VerifyKeywordAsync(
                """
                static implicit $$
                """);
        }

        [Fact]
        public async Task TestAfterFile()
        {
            await VerifyKeywordAsync(
                """
                file implicit $$
                """);
        }

        [Fact]
        public async Task TestAfterUnsafe()
        {
            await VerifyKeywordAsync(
                """
                unsafe implicit $$
                """);
        }

        [Fact]
        public async Task TestInsideClassDeclaration()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    explicit $$
                }
                """);
        }

        [Fact]
        public async Task TestWithAttribute()
        {
            await VerifyKeywordAsync(
                """
                [Goo]
                implicit $$
                """);
        }

        [Fact]
        public async Task TestWithGlobalAttribute()
        {
            await VerifyKeywordAsync(
                """
                [assembly: Goo]
                implicit $$
                """);
        }

        [Fact]
        public async Task TestAfterUsingDeclaration()
        {
            await VerifyKeywordAsync(
                """
                using X;
                implicit $$
                """);
        }

        [Fact]
        public async Task TestAfterExtensionDeclaration()
        {
            await VerifyKeywordAsync(
                """
                implicit extension E { }
                implicit $$
                """);
        }

        [Fact]
        public async Task TestNotAfterExtensionKeyword()
        {
            await VerifyAbsenceAsync(
                """
                implicit extension $$
                """);
        }
    }
}
