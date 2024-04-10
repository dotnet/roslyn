// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.KeywordHighlighting.KeywordHighlighters;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.KeywordHighlighting
{
    [Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
    public class AsyncAnonymousFunctionHighlighterTests : AbstractCSharpKeywordHighlighterTests
    {
        internal override Type GetHighlighterType()
            => typeof(AsyncAwaitHighlighter);

        [Fact]
        public async Task TestSimpleLambda()
        {
            await TestAsync(
                """
                using System;
                using System.Threading.Tasks;

                class AsyncExample
                {
                    async Task<int> AsyncMethod()
                    {
                        int hours = 24;
                        return hours;
                    }

                    async Task UseAsync()
                    {
                        Func<int, Task<int>> lambda = {|Cursor:[|async|]|} _ =>
                        {
                            return [|await|] AsyncMethod();
                        };
                        int result = await AsyncMethod();
                        Task<int> resultTask = AsyncMethod();
                        result = await resultTask;
                        result = await lambda(0);
                    }
                }
                """);
        }

        [Fact]
        public async Task TestParenthesizedLambda()
        {
            await TestAsync(
                """
                using System;
                using System.Threading.Tasks;

                class AsyncExample
                {
                    async Task<int> AsyncMethod()
                    {
                        int hours = 24;
                        return hours;
                    }

                    async Task UseAsync()
                    {
                        Func<Task<int>> lambda = {|Cursor:[|async|]|} () =>
                        {
                            return [|await|] AsyncMethod();
                        };
                        int result = await AsyncMethod();
                        Task<int> resultTask = AsyncMethod();
                        result = await resultTask;
                        result = await lambda();
                    }
                }
                """);
        }

        [Fact]
        public async Task TestAnonymousMethod()
        {
            await TestAsync(
                """
                using System;
                using System.Threading.Tasks;

                class AsyncExample
                {
                    async Task<int> AsyncMethod()
                    {
                        int hours = 24;
                        return hours;
                    }

                    async Task UseAsync()
                    {
                        Func<Task<int>> lambda = {|Cursor:[|async|]|} delegate
                        {
                            return [|await|] AsyncMethod();
                        };
                        int result = await AsyncMethod();
                        Task<int> resultTask = AsyncMethod();
                        result = await resultTask;
                        result = await lambda();
                    }
                }
                """);
        }
    }
}
