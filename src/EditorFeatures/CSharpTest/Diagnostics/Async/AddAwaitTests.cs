// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.Async;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.Async
{
    public partial class AddAwaitTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)]
        public void BadAsyncReturnOperand1()
        {
            var initial =
@"using System.Threading.Tasks;

class Program
{
    async Task<int> Test()
    {
        return 3;
    }

    async Task<int> Test2()
    {
        [|return Test();|]
    }
}";

            var expected =
@"using System.Threading.Tasks;

class Program
{
    async Task<int> Test()
    {
        return 3;
    }

    async Task<int> Test2()
    {
        return await Test();
    }
}";
            Test(initial, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)]
        public void TaskNotAwaited()
        {
            var initial =
@"using System.Threading.Tasks;
class Program
{
    async void Test()
    {
        [|Task.Delay(3);|]
    }
}";

            var expected =
@"using System.Threading.Tasks;
class Program
{
    async void Test()
    {
        await Task.Delay(3);
    }
}";
            Test(initial, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)]
        public void FunctionNotAwaited()
        {
            var initial =
@"using System.Threading.Tasks;
class Program
{
    Task AwaitableFunction()
    {
        return Task.FromResult(true);
    }

    async void Test()
    {
        [|AwaitableFunction();|]
    }
}";

            var expected =
@"using System.Threading.Tasks;
class Program
{
    Task AwaitableFunction()
    {
        return Task.FromResult(true);
    }

    async void Test()
    {
        await AwaitableFunction();
    }
}";
            Test(initial, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)]
        public void TestAssignmentExpression()
        {
            Test(
@"using System . Threading . Tasks ; class TestClass { private async Task MyTestMethod1Async ( ) { int myInt = [|MyIntMethodAsync ( )|] ; } private Task < int > MyIntMethodAsync ( ) { return Task . FromResult ( result : 1 ) ; } } ",
@"using System . Threading . Tasks ; class TestClass { private async Task MyTestMethod1Async ( ) { int myInt = await MyIntMethodAsync ( ) ; } private Task < int > MyIntMethodAsync ( ) { return Task . FromResult ( result : 1 ) ; } } ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)]
        public void TestAssignmentExpressionWithConversion()
        {
            Test(
@"using System . Threading . Tasks ; class TestClass { private async Task MyTestMethod1Async ( ) { long myInt = [|MyIntMethodAsync ( )|] ; } private Task < int > MyIntMethodAsync ( ) { return Task . FromResult ( result : 1 ) ; } } ",
@"using System . Threading . Tasks ; class TestClass { private async Task MyTestMethod1Async ( ) { long myInt = await MyIntMethodAsync ( ) ; } private Task < int > MyIntMethodAsync ( ) { return Task . FromResult ( result : 1 ) ; } } ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)]
        public void TestAssignmentExpressionWithConversionInNonAsyncFunction()
        {
            TestMissing(
@"using System . Threading . Tasks ; class TestClass { private Task MyTestMethod1Async ( ) { long myInt = [|MyIntMethodAsync ( )|] ; } private Task < int > MyIntMethodAsync ( ) { return Task . FromResult ( result : 1 ) ; } } ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)]
        public void TestAssignmentExpressionWithConversionInAsyncFunction()
        {
            Test(
@"using System . Threading . Tasks ; class TestClass { private async Task MyTestMethod1Async ( ) { long myInt = [|MyIntMethodAsync ( )|] ; } private Task < object > MyIntMethodAsync ( ) { return Task . FromResult ( new object ( ) ) ; } } ",
@"using System . Threading . Tasks ; class TestClass { private async Task MyTestMethod1Async ( ) { long myInt = await MyIntMethodAsync ( ) ; } private Task < object > MyIntMethodAsync ( ) { return Task . FromResult ( new object ( ) ) ; } } ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)]
        public void Test()
        {
            Test(
@"using System ; using System . Threading . Tasks ; class TestClass { private async Task MyTestMethod1Async ( ) { Action lambda = async ( ) => { int myInt = [|MyIntMethodAsync ( )|] ; } ; } private Task < int > MyIntMethodAsync ( ) { return Task . FromResult ( result : 1 ) ; } } ",
@"using System ; using System . Threading . Tasks ; class TestClass { private async Task MyTestMethod1Async ( ) { Action lambda = async ( ) => { int myInt = await MyIntMethodAsync ( ) ; } ; } private Task < int > MyIntMethodAsync ( ) { return Task . FromResult ( result : 1 ) ; } } ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)]
        public void TestAssignmentExpression2()
        {
            Test(
@"using System ; using System . Threading . Tasks ; class TestClass { private async Task MyTestMethod1Async ( ) { Func < Task > lambda = async ( ) => { int myInt = [|MyIntMethodAsync ( )|] ; } ; } private Task < int > MyIntMethodAsync ( ) { return Task . FromResult ( result : 1 ) ; } } ",
@"using System ; using System . Threading . Tasks ; class TestClass { private async Task MyTestMethod1Async ( ) { Func < Task > lambda = async ( ) => { int myInt = await MyIntMethodAsync ( ) ; } ; } private Task < int > MyIntMethodAsync ( ) { return Task . FromResult ( result : 1 ) ; } } ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)]
        public void TestAssignmentExpression3()
        {
            TestMissing(
@"using System ; using System . Threading . Tasks ; class TestClass { private async Task MyTestMethod1Async ( ) { Func < Task > lambda = ( ) => { int myInt = MyInt [||] MethodAsync ( ) ; } ; } private Task < int > MyIntMethodAsync ( ) { return Task . FromResult ( result : 1 ) ; } } ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)]
        public void TestAssignmentExpression4()
        {
            TestMissing(
@"using System ; using System . Threading . Tasks ; class TestClass { private async Task MyTestMethod1Async ( ) { Action lambda = ( ) => { int myInt = MyIntM [||] ethodAsync ( ) ; } ; } private Task < int > MyIntMethodAsync ( ) { return Task . FromResult ( result : 1 ) ; } } ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)]
        public void TestAssignmentExpression5()
        {
            Test(
@"using System ; using System . Threading . Tasks ; class TestClass { private async Task MyTestMethod1Async ( ) { Action @delegate = async delegate { int myInt = [|MyIntMethodAsync ( )|] ; } ; } private Task < int > MyIntMethodAsync ( ) { return Task . FromResult ( result : 1 ) ; } } ",
@"using System ; using System . Threading . Tasks ; class TestClass { private async Task MyTestMethod1Async ( ) { Action @delegate = async delegate { int myInt = await MyIntMethodAsync ( ) ; } ; } private Task < int > MyIntMethodAsync ( ) { return Task . FromResult ( result : 1 ) ; } } ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)]
        public void TestAssignmentExpression6()
        {
            Test(
@"using System ; using System . Threading . Tasks ; class TestClass { private async Task MyTestMethod1Async ( ) { Func < Task > @delegate = async delegate { int myInt = [|MyIntMethodAsync ( )|] ; } ; } private Task < int > MyIntMethodAsync ( ) { return Task . FromResult ( result : 1 ) ; } } ",
@"using System ; using System . Threading . Tasks ; class TestClass { private async Task MyTestMethod1Async ( ) { Func < Task > @delegate = async delegate { int myInt = await MyIntMethodAsync ( ) ; } ; } private Task < int > MyIntMethodAsync ( ) { return Task . FromResult ( result : 1 ) ; } } ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)]
        public void TestAssignmentExpression7()
        {
            TestMissing(
@"using System ; using System . Threading . Tasks ; class TestClass { private async Task MyTestMethod1Async ( ) { Action @delegate = delegate { int myInt = MyInt [||] MethodAsync ( ) ; } ; } private Task < int > MyIntMethodAsync ( ) { return Task . FromResult ( result : 1 ) ; } } ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddAwait)]
        public void TestAssignmentExpression8()
        {
            TestMissing(
@"using System ; using System . Threading . Tasks ; class TestClass { private async Task MyTestMethod1Async ( ) { Func < Task > @delegate = delegate { int myInt = MyIntM [||] ethodAsync ( ) ; } ; } private Task < int > MyIntMethodAsync ( ) { return Task . FromResult ( result : 1 ) ; } } ");
        }

        internal override Tuple<DiagnosticAnalyzer, CodeFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
        {
            return new Tuple<DiagnosticAnalyzer, CodeFixProvider>(null, new CSharpAddAwaitCodeFixProvider());
        }
    }
}
