// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.MakeStatementAsynchronous;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.MakeStatementAsynchronous
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsMakeStatementAsynchronous)]
    public class CSharpMakeStatementAsynchronousCodeFixTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new CSharpMakeStatementAsynchronousCodeFixProvider());

        private static readonly TestParameters s_asyncStreamsFeature = new TestParameters(parseOptions: new CSharpParseOptions(LanguageVersion.CSharp8));

        private readonly string AsyncStreams = @"
namespace System.Collections.Generic
{
    public interface IAsyncEnumerable<out T>
    {
        IAsyncEnumerator<T> GetAsyncEnumerator();
    }

    public interface IAsyncEnumerator<out T> : System.IAsyncDisposable
    {
        System.Threading.Tasks.ValueTask<bool> MoveNextAsync();
        T Current { get; }
    }
}
namespace System
{
    public interface IAsyncDisposable
    {
        System.Threading.Tasks.ValueTask DisposeAsync();
    }
}
";

        [Fact]
        public async Task FixAllForeach()
        {
            await TestInRegularAndScript1Async(
AsyncStreams + @"
class Program
{
    void M(System.Collections.Generic.IAsyncEnumerable<int> collection)
    {
        foreach (var i in {|FixAllInDocument:collection|}) { }
        foreach (var j in collection) { }
    }
}",
AsyncStreams + @"
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
AsyncStreams + @"
class Program
{
    void M(System.Collections.Generic.IAsyncEnumerable<(int, int)> collection)
    {
        foreach (var (i, j) in {|FixAllInDocument:collection|}) { }
        foreach (var (k, l) in collection) { }
    }
}",
AsyncStreams + @"
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
AsyncStreams + @"
class Program
{
    void M(System.IAsyncDisposable disposable)
    {
        using (var i = {|FixAllInDocument:disposable|}) { }
        using (var j = disposable) { }
    }
}",
AsyncStreams + @"
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
AsyncStreams + @"
class Program
{
    void M(System.IAsyncDisposable disposable)
    {
        using var i = {|FixAllInDocument:disposable|};
        using var j = disposable;
    }
}",
AsyncStreams + @"
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
AsyncStreams + @"
class Program
{
    void M(System.Collections.Generic.IAsyncEnumerable<int> collection)
    {
        foreach (var i in [|collection|])
        {
        }
    }
}",
AsyncStreams + @"
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
AsyncStreams + @"
class Program
{
    void M(System.Collections.Generic.IAsyncEnumerable<(int, int)> collection)
    {
        foreach (var (i, j) in collection[||])
        {
        }
    }
}",
AsyncStreams + @"
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
AsyncStreams + @"
class Program
{
    void M(System.IAsyncDisposable disposable)
    {
        using (var i = disposable[||])
        {
        }
    }
}",
AsyncStreams + @"
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
AsyncStreams + @"
class Program
{
    void M(System.IAsyncDisposable disposable)
    {
        using var i = disposable[||];
    }
}",
AsyncStreams + @"
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
