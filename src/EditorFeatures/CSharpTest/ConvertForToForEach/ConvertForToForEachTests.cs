// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.ConvertForToForEach;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertForToForEach
{
    public class ConvertForToForEachTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new CSharpConvertForToForEachCodeRefactoringProvider();

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestArray1()
        {
            await TestInRegularAndScript1Async(
@"using System;

class C
{
    void Test(string[] array)
    {
        [||]for (int i = 0; i < array.Length; i++)
        {
            Console.WriteLine(array[i]);
        }
    }
}",
@"using System;

class C
{
    void Test(string[] array)
    {
        foreach (string {|Rename:v|} in array)
        {
            Console.WriteLine(v);
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestPostIncrement()
        {
            await TestInRegularAndScript1Async(
@"using System;

class C
{
    void Test(string[] array)
    {
        [||]for (int i = 0; i < array.Length; ++i)
        {
            Console.WriteLine(array[i]);
        }
    }
}",
@"using System;

class C
{
    void Test(string[] array)
    {
        foreach (string {|Rename:v|} in array)
        {
            Console.WriteLine(v);
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestArrayPlusEqualsIncrementor()
        {
            await TestInRegularAndScript1Async(
@"using System;

class C
{
    void Test(string[] array)
    {
        [||]for (int i = 0; i < array.Length; i += 1)
        {
            Console.WriteLine(array[i]);
        }
    }
}",
@"using System;

class C
{
    void Test(string[] array)
    {
        foreach (string {|Rename:v|} in array)
        {
            Console.WriteLine(v);
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestMissingWithoutIncrementor()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void Test(string[] array)
    {
        [||]for (int i = 0; i < array.Length; )
        {
            Console.WriteLine(array[i]);
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestMissingWithoutIncorrectIncrementor1()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void Test(string[] array)
    {
        [||]for (int i = 0; i < array.Length; i += 2)
        {
            Console.WriteLine(array[i]);
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestMissingWithoutIncorrectIncrementor2()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void Test(string[] array)
    {
        [||]for (int i = 0; i < array.Length; j += 2)
        {
            Console.WriteLine(array[i]);
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestMissingWithoutCondition()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void Test(string[] array)
    {
        [||]for (int i = 0; ; i++)
        {
            Console.WriteLine(array[i]);
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestMissingWithIncorrectCondition1()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void Test(string[] array)
    {
        [||]for (int i = 0; j < array.Length; i++)
        {
            Console.WriteLine(array[i]);
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestMissingWithIncorrectCondition2()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void Test(string[] array)
    {
        [||]for (int i = 0; i < GetLength(array); i++)
        {
            Console.WriteLine(array[i]);
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestWithoutInitializer()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void Test(string[] array)
    {
        [||]for (; i < array.Length; i++)
        {
            Console.WriteLine(array[i]);
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestWithUninitializedVariable()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void Test(string[] array)
    {
        [||]for (int i; i < array.Length; i++)
        {
            Console.WriteLine(array[i]);
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertForToForEach)]
        public async Task TestWithMultipleVariables()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class C
{
    void Test(string[] array)
    {
        [||]for (int i = 0, j = 0; i < array.Length; i++)
        {
            Console.WriteLine(array[i]);
        }
    }
}");
        }
    }
}
