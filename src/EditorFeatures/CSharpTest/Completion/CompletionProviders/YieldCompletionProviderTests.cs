// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders
{
    public class YieldCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        internal override Type GetCompletionProviderType() => typeof(YieldCompletionProvider);

        private async Task VerifyYieldCompletionsAsync(string markup)
        {
            await VerifyItemExistsAsync(markup, "yield return", glyph: (int)Glyph.Keyword);
            await VerifyItemExistsAsync(markup, "yield break", glyph: (int)Glyph.Keyword);
        }

        private async Task VerifyYieldCompletionsAreAbsentAsync(string markup)
        {
            await VerifyItemIsAbsentAsync(markup, "yield return");
            await VerifyItemIsAbsentAsync(markup, "yield break");
        }

        [Fact]
        public async Task TestNotAtRoot()
        {
            await VerifyYieldCompletionsAreAbsentAsync("$$");
        }

        [Fact]
        public async Task TestNotAfterClass()
        {
            await VerifyYieldCompletionsAreAbsentAsync("""
                class C {}
                $$
                """);
        }

        [Fact]
        public async Task TestNotAfterGlobalStatement()
        {
            await VerifyYieldCompletionsAreAbsentAsync("""
                System.Console.WriteLine();
                $$
                """);
        }

        [Fact]
        public async Task TestNotAfterGlobalVariableDeclaration()
        {
            await VerifyYieldCompletionsAreAbsentAsync("""
                int i = 0;
                $$
                """);
        }

        [Fact]
        public async Task TestNotInUsingAlias()
        {
            await VerifyYieldCompletionsAreAbsentAsync("""
                using Goo = $$
                """);
        }

        [Fact]
        public async Task TestNotInGlobalUsingAlias()
        {
            await VerifyYieldCompletionsAreAbsentAsync("""
                global using Goo = $$
                """);
        }

        [Fact]
        public async Task TestEmptyStatement()
        {
            await VerifyYieldCompletionsAsync(AddInsideMethod("$$"));
        }

        [Fact]
        public async Task TestBeforeStatement()
        {
            await VerifyYieldCompletionsAsync(AddInsideMethod("""
                $$
                return;
                """));
        }

        [Fact]
        public async Task TestAfterStatement()
        {
            await VerifyYieldCompletionsAsync(AddInsideMethod("""
                return;
                $$
                """));
        }

        [Fact]
        public async Task TestAfterBlock()
        {
            await VerifyYieldCompletionsAsync(AddInsideMethod("""
                if (true)
                {
                }
                $$
                """));
        }

        [Fact]
        public async Task TestNotAfterYield()
        {
            await VerifyYieldCompletionsAreAbsentAsync(AddInsideMethod("""
                yield $$
                """));
        }

        [Fact]
        public async Task TestNotInClass()
        {
            await VerifyYieldCompletionsAreAbsentAsync("""
                class C
                {
                    $$
                }
                """);
        }
    }
}
