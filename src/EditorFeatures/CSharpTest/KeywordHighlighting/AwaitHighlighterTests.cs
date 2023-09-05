﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.KeywordHighlighting.KeywordHighlighters;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.KeywordHighlighting
{
    [Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
    public class AwaitHighlighterTests : AbstractCSharpKeywordHighlighterTests
    {
        internal override Type GetHighlighterType()
            => typeof(AsyncAwaitHighlighter);

        [Fact]
        public async Task TestExample2_2()
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

                    [|async|] Task UseAsync()
                    {
                        Func<Task<int>> lambda = async () =>
                        {
                            return await AsyncMethod();
                        };
                        int result = {|Cursor:[|await|]|} AsyncMethod();
                        Task<int> resultTask = AsyncMethod();
                        result = [|await|] resultTask;
                        result = [|await|] lambda();
                    }
                }
                """);
        }

        [Fact]
        public async Task TestExample2_3()
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

                    [|async|] Task UseAsync()
                    {
                        Func<Task<int>> lambda = async () =>
                        {
                            return await AsyncMethod();
                        };
                        int result = [|await|] AsyncMethod();
                        Task<int> resultTask = AsyncMethod();
                        result = {|Cursor:[|await|]|} resultTask;
                        result = [|await|] lambda();
                    }
                }
                """);
        }

        [Fact]
        public async Task TestExample2_4()
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

                    [|async|] Task UseAsync()
                    {
                        Func<Task<int>> lambda = async () =>
                        {
                            return await AsyncMethod();
                        };
                        int result = [|await|] AsyncMethod();
                        Task<int> resultTask = AsyncMethod();
                        result = [|await|] resultTask;
                        result = {|Cursor:[|await|]|} lambda();
                    }
                }
                """);
        }

        [Fact]
        public async Task TestExample3_2()
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
                        Func<Task<int>> lambda = [|async|] () =>
                        {
                            return {|Cursor:[|await|]|} AsyncMethod();
                        };
                        int result = await AsyncMethod();
                        Task<int> resultTask = AsyncMethod();
                        result = await resultTask;
                        result = await lambda();
                    }
                }
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/573625")]
        public async Task TestNestedAwaits1()
        {
            await TestAsync(
                """
                using System;
                using System.Threading.Tasks;

                class AsyncExample
                {
                    async Task<Task<int>> AsyncMethod()
                    {
                        return NewMethod();
                    }

                    private static Task<int> NewMethod()
                    {
                        int hours = 24;
                        return hours;
                    }

                    async Task UseAsync()
                    {
                        Func<Task<int>> lambda = [|async|] () =>
                        {
                            return {|Cursor:[|await await|]|} AsyncMethod();
                        };
                        int result = await await AsyncMethod();
                        Task<Task<int>> resultTask = AsyncMethod();
                        result = await await resultTask;
                        result = await lambda();
                    }
                }
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/573625")]
        public async Task TestNestedAwaits2()
        {
            await TestAsync(
                """
                using System;
                using System.Threading.Tasks;

                class AsyncExample
                {
                    async Task<Task<int>> AsyncMethod()
                    {
                        return NewMethod();
                    }

                    private static Task<int> NewMethod()
                    {
                        int hours = 24;
                        return hours;
                    }

                    [|async|] Task UseAsync()
                    {
                        Func<Task<int>> lambda = async () =>
                        {
                            return await await AsyncMethod();
                        };
                        int result = {|Cursor:[|await await|]|} AsyncMethod();
                        Task<Task<int>> resultTask = AsyncMethod();
                        result = [|await await|] resultTask;
                        result = [|await|] lambda();
                    }
                }
                """);
        }

        [Fact]
        public async Task TestAwaitUsing_OnAsync()
        {
            await TestAsync(
                """
                using System.Threading.Tasks;

                class C
                {
                    {|Cursor:[|async|]|} Task M()
                    {
                        [|await|] using (var x = new object());
                    }
                }
                """);
        }

        [Fact]
        public async Task TestAwaitUsing_OnAwait()
        {
            await TestAsync(
                """
                using System.Threading.Tasks;

                class C
                {
                    [|async|] Task M()
                    {
                        {|Cursor:[|await|]|} using (var x = new object());
                    }
                }
                """);
        }

        [Fact]
        public async Task TestAwaitUsingDeclaration_OnAsync()
        {
            await TestAsync(
                """
                using System.Threading.Tasks;

                class C
                {
                    {|Cursor:[|async|]|} Task M()
                    {
                        [|await|] using var x = new object();
                    }
                }
                """);
        }

        [Fact]
        public async Task TestAwaitUsingDeclaration_OnAwait()
        {
            await TestAsync(
                """
                using System.Threading.Tasks;

                class C
                {
                    [|async|] Task M()
                    {
                        {|Cursor:[|await|]|} using var x = new object();
                    }
                }
                """);
        }

        [Fact]
        public async Task TestAwaitForEach_OnAsync()
        {
            await TestAsync(
                """
                using System.Threading.Tasks;

                class C
                {
                    {|Cursor:[|async|]|} Task M()
                    {
                        foreach [|await|] (var n in new int[] { });
                    }
                }
                """);
        }

        [Fact]
        public async Task TestAwaitForEach_OnAwait()
        {
            await TestAsync(
                """
                using System.Threading.Tasks;

                class C
                {
                    [|async|] Task M()
                    {
                        foreach {|Cursor:[|await|]|} (var n in new int[] { });
                    }
                }
                """);
        }

        [Fact]
        public async Task TestForEachVariableAwait_OnAsync()
        {
            await TestAsync(
                """
                using System.Threading.Tasks;

                class C
                {
                    {|Cursor:[|async|]|} Task M()
                    {
                        foreach [|await|] (var (a, b) in new (int, int)[] { });
                    }
                }
                """);
        }

        [Fact]
        public async Task TestForEachVariableAwait_OnAwait()
        {
            await TestAsync(
                """
                using System.Threading.Tasks;

                class C
                {
                    [|async|] Task M()
                    {
                        foreach {|Cursor:[|await|]|} (var (a, b) in new (int, int)[] { });
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60400")]
        public async Task TestTopLevelStatements()
        {
            await TestAsync(
                """
                [|await|] Task.Delay(1000);
                {|Cursor:[|await|]|} Task.Run(() => { })
                """);
        }
    }
}
