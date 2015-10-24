// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.CSharp.KeywordHighlighting.KeywordHighlighters;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.KeywordHighlighting
{
    public class AwaitHighlighterTests : AbstractCSharpKeywordHighlighterTests
    {
        internal override IHighlighter CreateHighlighter()
        {
            return new AwaitHighlighter();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestExample2_2()
        {
            Test(
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
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestExample2_3()
        {
            Test(
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
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestExample2_4()
        {
            Test(
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
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestExample3_2()
        {
            Test(
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
}
");
        }

        [WorkItem(573625)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestNestedAwaits1()
        {
            Test(
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
}
");
        }

        [WorkItem(573625)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestNestedAwaits2()
        {
            Test(
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
}
");
        }
    }
}
