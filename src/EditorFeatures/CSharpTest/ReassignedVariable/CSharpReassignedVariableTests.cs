// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.ReassignedVariable;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ReassignedVariable
{
    public class CSharpReassignedVariableTests : AbstractReassignedVariableTests
    {
        protected override TestWorkspace CreateWorkspace(string markup)
            => TestWorkspace.CreateCSharp(markup);

        [Fact]
        public async Task TestNoParameterReassignment()
        {
            await TestAsync(
@"class C
{
    void M(int p)
    {
    }
}");
        }

        [Fact]
        public async Task TestParameterReassignment()
        {
            await TestAsync(
@"class C
{
    void M(int [|p|])
    {
        [|p|] = 1;
    }
}");
        }

        [Fact]
        public async Task TestParameterReassignmentWhenReadAfter()
        {
            await TestAsync(
@"
using System;
class C
{
    void M(int [|p|])
    {
        [|p|] = 1;
        Console.WriteLine([|p|]);
    }
}");
        }

        [Fact]
        public async Task TestParameterReassignmentWhenReadBefore()
        {
            await TestAsync(
@"
using System;
class C
{
    void M(int [|p|])
    {
        Console.WriteLine([|p|]);
        [|p|] = 1;
    }
}");
        }

        [Fact]
        public async Task TestParameterReassignmentWhenReadWithDefaultValue()
        {
            await TestAsync(
@"
using System;
class C
{
    void M(int [|p|] = 1)
    {
        Console.WriteLine([|p|]);
        [|p|] = 1;
    }
}");
        }

        [Fact]
        public async Task TestParameterWithExprBodyWithReassignment()
        {
            await TestAsync(
@"
using System;
class C
{
    void M(int [|p|]) => Console.WriteLine([|p|]++);
}");
        }

        [Fact]
        public async Task TestLocalFunctionWithExprBodyWithReassignment()
        {
            await TestAsync(
@"
using System;
class C
{
    void M()
    {
        void Local(int [|p|])
            => Console.WriteLine([|p|]++);
}");
        }

        [Fact]
        public async Task TestIndexerWithWriteInExprBody()
        {
            await TestAsync(
@"
using System;
class C
{
    int this[int [|p|]] => [|p|]++;
}");
        }

        [Fact]
        public async Task TestIndexerWithWriteInGetter1()
        {
            await TestAsync(
@"
using System;
class C
{
    int this[int [|p|]] { get => [|p|]++; }
}");
        }

        [Fact]
        public async Task TestIndexerWithWriteInGetter2()
        {
            await TestAsync(
@"
using System;
class C
{
    int this[int [|p|]] { get { [|p|]++; } }
}");
        }

        [Fact]
        public async Task TestIndexerWithWriteInSetter1()
        {
            await TestAsync(
@"
using System;
class C
{
    int this[int [|p|]] { set => [|p|]++; }
}");
        }

        [Fact]
        public async Task TestIndexerWithWriteInSetter2()
        {
            await TestAsync(
@"
using System;
class C
{
    int this[int [|p|]] { set { [|p|]++; } }
}");
        }

        [Fact]
        public async Task TestPropertyWithAssignmentToValue1()
        {
            await TestAsync(
@"
using System;
class C
{
    int Goo { set => [|value|] = [|value|] + 1; }
}");
        }

        [Fact]
        public async Task TestPropertyWithAssignmentToValue2()
        {
            await TestAsync(
@"
using System;
class C
{
    int Goo { set { [|value|] = [|value|] + 1; } }
}");
        }

        [Fact]
        public async Task TestLambdaParameterWithoutReassignment()
        {
            await TestAsync(
@"
using System;
class C
{
    void M()
    {
        Action<int> a = x => Console.WriteLine(x);
    }
}");
        }

        [Fact]
        public async Task TestLambdaParameterWithReassignment()
        {
            await TestAsync(
@"
using System;
class C
{
    void M()
    {
        Action<int> a = [|x|] => Console.WriteLine([|x|]++);
    }
}");
        }

        [Fact]
        public async Task TestLambdaParameterWithReassignment2()
        {
            await TestAsync(
@"
using System;
class C
{
    void M()
    {
        Action<int> a = (int [|x|]) => Console.WriteLine([|x|]++);
    }
}");
        }

        [Fact]
        public async Task TestLocalWithoutInitializerWithoutReassignment()
        {
            await TestAsync(
@"
using System;
class C
{
    void M(bool b)
    {
        int p;
        if (b)
            p = 1;
        else
            p = 2;

        Console.WriteLine(p);
    }
}");
        }

        [Fact]
        public async Task TestLocalWithoutInitializerWithReassignment()
        {
            await TestAsync(
@"
using System;
class C
{
    void M(bool b)
    {
        int [|p|];
        if (b)
            [|p|] = 1;
        else
            [|p|] = 2;

        [|p|] = 0;
        Console.WriteLine([|p|]);
    }
}");
        }
    }
}
