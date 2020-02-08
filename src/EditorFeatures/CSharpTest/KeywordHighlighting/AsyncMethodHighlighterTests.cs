﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.CSharp.KeywordHighlighting.KeywordHighlighters;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.KeywordHighlighting
{
    public class AsyncMethodHighlighterTests : AbstractCSharpKeywordHighlighterTests
    {
        internal override IHighlighter CreateHighlighter()
            => new AsyncAwaitHighlighter();

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestExample1_1()
        {
            await TestAsync(
@"using System;
using System.Threading.Tasks;

class AsyncExample
{
    {|Cursor:[|async|]|} Task<int> AsyncMethod()
    {
        int hours = 24;
        return hours;
    }

    async Task UseAsync()
    {
        Func<Task<int>> lambda = async () =>
        {
            return await AsyncMethod();
        };
        int result = await AsyncMethod();
        Task<int> resultTask = AsyncMethod();
        result = await resultTask;
        result = await lambda();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestExample2_1()
        {
            await TestAsync(
@"using System;
using System.Threading.Tasks;

class AsyncExample
{
    async Task<int> AsyncMethod()
    {
        int hours = 24;
        return hours;
    }

    {|Cursor:[|async|]|} Task UseAsync()
    {
        Func<Task<int>> lambda = async () =>
        {
            return await AsyncMethod();
        };
        int result = [|await|] AsyncMethod();
        Task<int> resultTask = AsyncMethod();
        result = [|await|] resultTask;
        result = [|await|] lambda();
    }
}");
        }
    }
}
