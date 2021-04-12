// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.MakeStatementAsynchronous;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.MakeStatementAsynchronous
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsMakeStatementAsynchronous)]
    public class CSharpMakeStatementAsynchronousCodeFixTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        public CSharpMakeStatementAsynchronousCodeFixTests(ITestOutputHelper logger)
           : base(logger)
        {
        }

        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new CSharpMakeStatementAsynchronousCodeFixProvider());

        private static readonly TestParameters s_asyncStreamsFeature = new TestParameters(parseOptions: new CSharpParseOptions(LanguageVersion.CSharp8));

        [Fact]
        public async Task FixAllForeach()
        {
            await TestInRegularAndScript1Async(
IAsyncEnumerable + @"
class Program
{
    void M(System.Collections.Generic.IAsyncEnumerable<int> collection)
    {
        foreach (var i in {|FixAllInDocument:collection|}) { }
        foreach (var j in collection) { }
    }
}",
IAsyncEnumerable + @"
class Program
{
    void M(System.Collections.Generic.IAsyncEnumerable<int> collection)
    {
        await foreach (var i in collection) { }
        await foreach (var j in collection) { }
    }
}", parameters: s_asyncStreamsFeature);
        }

        [Fact]
        public async Task FixAllForeachDeconstruction()
        {
            await TestInRegularAndScript1Async(
IAsyncEnumerable + @"
class Program
{
    void M(System.Collections.Generic.IAsyncEnumerable<(int, int)> collection)
    {
        foreach (var (i, j) in {|FixAllInDocument:collection|}) { }
        foreach (var (k, l) in collection) { }
    }
}",
IAsyncEnumerable + @"
class Program
{
    void M(System.Collections.Generic.IAsyncEnumerable<(int, int)> collection)
    {
        await foreach (var (i, j) in collection) { }
        await foreach (var (k, l) in collection) { }
    }
}", parameters: s_asyncStreamsFeature);
        }

        [Fact]
        public async Task FixAllUsingStatement()
        {
            await TestInRegularAndScript1Async(
IAsyncEnumerable + @"
class Program
{
    void M(System.IAsyncDisposable disposable)
    {
        using (var i = {|FixAllInDocument:disposable|}) { }
        using (var j = disposable) { }
    }
}",
IAsyncEnumerable + @"
class Program
{
    void M(System.IAsyncDisposable disposable)
    {
        await using (var i = disposable) { }
        await using (var j = disposable) { }
    }
}", parameters: s_asyncStreamsFeature);
        }

        [Fact]
        public async Task FixAllUsingDeclaration()
        {
            await TestInRegularAndScript1Async(
IAsyncEnumerable + @"
class Program
{
    void M(System.IAsyncDisposable disposable)
    {
        using var i = {|FixAllInDocument:disposable|};
        using var j = disposable;
    }
}",
IAsyncEnumerable + @"
class Program
{
    void M(System.IAsyncDisposable disposable)
    {
        await using var i = disposable;
        await using var j = disposable;
    }
}", parameters: s_asyncStreamsFeature);
        }

        [Fact]
        public async Task FixForeach()
        {
            await TestInRegularAndScript1Async(
IAsyncEnumerable + @"
class Program
{
    void M(System.Collections.Generic.IAsyncEnumerable<int> collection)
    {
        foreach (var i in [|collection|])
        {
        }
    }
}",
IAsyncEnumerable + @"
class Program
{
    void M(System.Collections.Generic.IAsyncEnumerable<int> collection)
    {
        await foreach (var i in collection)
        {
        }
    }
}", parameters: s_asyncStreamsFeature);
        }

        [Fact]
        public async Task FixForeachDeconstruction()
        {
            await TestInRegularAndScript1Async(
IAsyncEnumerable + @"
class Program
{
    void M(System.Collections.Generic.IAsyncEnumerable<(int, int)> collection)
    {
        foreach (var (i, j) in collection[||])
        {
        }
    }
}",
IAsyncEnumerable + @"
class Program
{
    void M(System.Collections.Generic.IAsyncEnumerable<(int, int)> collection)
    {
        await foreach (var (i, j) in collection)
        {
        }
    }
}", parameters: s_asyncStreamsFeature);
        }

        [Fact]
        public async Task FixUsingStatement()
        {
            await TestInRegularAndScript1Async(
IAsyncEnumerable + @"
class Program
{
    void M(System.IAsyncDisposable disposable)
    {
        using (var i = disposable[||])
        {
        }
    }
}",
IAsyncEnumerable + @"
class Program
{
    void M(System.IAsyncDisposable disposable)
    {
        await using (var i = disposable)
        {
        }
    }
}", parameters: s_asyncStreamsFeature);
        }

        [Fact]
        public async Task FixUsingDeclaration()
        {
            await TestInRegularAndScript1Async(
IAsyncEnumerable + @"
class Program
{
    void M(System.IAsyncDisposable disposable)
    {
        using var i = disposable[||];
    }
}",
IAsyncEnumerable + @"
class Program
{
    void M(System.IAsyncDisposable disposable)
    {
        await using var i = disposable;
    }
}", parameters: s_asyncStreamsFeature);
        }
    }
}
