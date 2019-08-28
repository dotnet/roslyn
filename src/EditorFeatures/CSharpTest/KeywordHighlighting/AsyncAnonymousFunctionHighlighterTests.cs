// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.CSharp.KeywordHighlighting.KeywordHighlighters;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.KeywordHighlighting
{
    public class AsyncAnonymousFunctionHighlighterTests : AbstractCSharpKeywordHighlighterTests
    {
        internal override IHighlighter CreateHighlighter()
        {
            return new AsyncAwaitHighlighter();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestSimpleLambda()
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestParenthesizedLambda()
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestAnonymousMethod()
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
}");
        }
    }
}
