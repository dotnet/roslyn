// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.CSharp.KeywordHighlighting.KeywordHighlighters;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.KeywordHighlighting
{
    public class AsyncMethodHighlighterTests : AbstractCSharpKeywordHighlighterTests
    {
        internal override IHighlighter CreateHighlighter()
        {
            return new AsyncMethodHighlighter();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestExample1_1()
        {
            Test(
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
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.KeywordHighlighting)]
        public void TestExample2_1()
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
}
");
        }
    }
}
