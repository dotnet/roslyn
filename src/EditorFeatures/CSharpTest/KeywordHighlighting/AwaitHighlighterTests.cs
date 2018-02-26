// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.CSharp.KeywordHighlighting.KeywordHighlighters;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.KeywordHighlighting
{
    public class AwaitHighlighterTests : AbstractCSharpKeywordHighlighterTests
    {
        private class CombinedHighlighter : IHighlighter
        {
            private readonly IHighlighter highlighter0;
            private readonly IHighlighter highlighter1;

            public CombinedHighlighter(IHighlighter highlighter0, IHighlighter highlighter1)
            {
                this.highlighter0 = highlighter0;
                this.highlighter1 = highlighter1;
            }

            public IEnumerable<TextSpan> GetHighlights(SyntaxNode root, int position, CancellationToken cancellationToken)
                => highlighter0.GetHighlights(root, position, cancellationToken).Concat(
                   highlighter1.GetHighlights(root, position, cancellationToken));
        }

        internal override IHighlighter CreateHighlighter()
        {
            return new CombinedHighlighter(new AsyncMethodHighlighter(), new AsyncParenthesizedLambdaHighlighter());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestExample2_2()
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestExample2_3()
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestExample2_4()
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestExample3_2()
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
        Func<Task<int>> lambda = [|async|] () =>
        {
            return {|Cursor:[|await|]|} AsyncMethod();
        };
        int result = await AsyncMethod();
        Task<int> resultTask = AsyncMethod();
        result = await resultTask;
        result = await lambda();
    }
}");
        }

        [WorkItem(573625, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/573625")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestNestedAwaits1()
        {
            await TestAsync(
@"using System;
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
}");
        }

        [WorkItem(573625, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/573625")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public async Task TestNestedAwaits2()
        {
            await TestAsync(
@"using System;
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
}");
        }
    }
}
