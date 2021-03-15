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
    }
}
