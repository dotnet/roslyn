// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.MakeStatementAsynchronous;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.MakeStatementAsynchronous;

using VerifyCS = CSharpCodeFixVerifier<
    EmptyDiagnosticAnalyzer,
    CSharpMakeStatementAsynchronousCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsMakeStatementAsynchronous)]
public sealed class CSharpMakeStatementAsynchronousCodeFixTests
{
    [Fact]
    public Task FixAllForeach()
        => new VerifyCS.Test()
        {
            TestCode = """
                class Program
                {
                    void M(System.Collections.Generic.IAsyncEnumerable<int> collection)
                    {
                        foreach (var i in {|CS8414:collection|}) { }
                        foreach (var j in {|CS8414:collection|}) { }
                    }
                }
                """,
            FixedCode = """
                class Program
                {
                    void M(System.Collections.Generic.IAsyncEnumerable<int> collection)
                    {
                        {|CS4033:await|} foreach (var i in collection) { }
                        {|CS4033:await|} foreach (var j in collection) { }
                    }
                }
                """,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60
        }.RunAsync();

    [Fact]
    public Task FixAllForeachDeconstruction()
        => new VerifyCS.Test()
        {
            TestCode = """
                class Program
                {
                    void M(System.Collections.Generic.IAsyncEnumerable<(int, int)> collection)
                    {
                        foreach (var ({|CS8130:i|}, {|CS8130:j|}) in {|CS8414:collection|}) { }
                        foreach (var ({|CS8130:k|}, {|CS8130:l|}) in {|CS8414:collection|}) { }
                    }
                }
                """,
            FixedCode = """
                class Program
                {
                    void M(System.Collections.Generic.IAsyncEnumerable<(int, int)> collection)
                    {
                        {|CS4033:await|} foreach (var (i, j) in collection) { }
                        {|CS4033:await|} foreach (var (k, l) in collection) { }
                    }
                }
                """,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60
        }.RunAsync();

    [Fact]
    public Task FixAllUsingStatement()
        => new VerifyCS.Test()
        {
            TestCode = """
                class Program
                {
                    void M(System.IAsyncDisposable disposable)
                    {
                        using ({|CS8418:var i = disposable|}) { }
                        using ({|CS8418:var j = disposable|}) { }
                    }
                }
                """,
            FixedCode = """
                class Program
                {
                    void M(System.IAsyncDisposable disposable)
                    {
                        {|CS4033:await|} using (var i = disposable) { }
                        {|CS4033:await|} using (var j = disposable) { }
                    }
                }
                """,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60
        }.RunAsync();

    [Fact]
    public Task FixAllUsingDeclaration()
        => new VerifyCS.Test()
        {
            TestCode = """
                class Program
                {
                    void M(System.IAsyncDisposable disposable)
                    {
                        {|CS8418:using var i = disposable;|}
                        {|CS8418:using var j = disposable;|}
                    }
                }
                """,
            FixedCode = """
                class Program
                {
                    void M(System.IAsyncDisposable disposable)
                    {
                        {|CS4033:await|} using var i = disposable;
                        {|CS4033:await|} using var j = disposable;
                    }
                }
                """,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60
        }.RunAsync();

    [Fact]
    public Task FixForeach()
        => new VerifyCS.Test()
        {
            TestCode = """
                class Program
                {
                    void M(System.Collections.Generic.IAsyncEnumerable<int> collection)
                    {
                        foreach (var i in {|CS8414:collection|})
                        {
                        }
                    }
                }
                """,
            FixedCode = """
                class Program
                {
                    void M(System.Collections.Generic.IAsyncEnumerable<int> collection)
                    {
                        {|CS4033:await|} foreach (var i in collection)
                        {
                        }
                    }
                }
                """,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60
        }.RunAsync();

    [Fact]
    public Task FixForeachDeconstruction()
        => new VerifyCS.Test()
        {
            TestCode = """
                class Program
                {
                    void M(System.Collections.Generic.IAsyncEnumerable<(int, int)> collection)
                    {
                        foreach (var ({|CS8130:i|}, {|CS8130:j|}) in {|CS8414:collection|})
                        {
                        }
                    }
                }
                """,
            FixedCode = """
                class Program
                {
                    void M(System.Collections.Generic.IAsyncEnumerable<(int, int)> collection)
                    {
                        {|CS4033:await|} foreach (var (i, j) in collection)
                        {
                        }
                    }
                }
                """,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60
        }.RunAsync();

    [Fact]
    public Task FixUsingStatement()
        => new VerifyCS.Test()
        {
            TestCode = """
                class Program
                {
                    void M(System.IAsyncDisposable disposable)
                    {
                        using ({|CS8418:var i = disposable|})
                        {
                        }
                    }
                }
                """,
            FixedCode = """
                class Program
                {
                    void M(System.IAsyncDisposable disposable)
                    {
                        {|CS4033:await|} using (var i = disposable)
                        {
                        }
                    }
                }
                """,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60
        }.RunAsync();

    [Fact]
    public Task FixUsingDeclaration()
        => new VerifyCS.Test()
        {
            TestCode = """
                class Program
                {
                    void M(System.IAsyncDisposable disposable)
                    {
                        {|CS8418:using var i = disposable;|}
                    }
                }
                """,
            FixedCode = """
                class Program
                {
                    void M(System.IAsyncDisposable disposable)
                    {
                        {|CS4033:await|} using var i = disposable;
                    }
                }
                """,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60
        }.RunAsync();
}
