// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.FixReturnType;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.FixReturnType;

using VerifyCS = CSharpCodeFixVerifier<
    EmptyDiagnosticAnalyzer,
    CSharpFixReturnTypeCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsFixReturnType)]
public sealed class FixReturnTypeTests
{
    [Fact]
    public Task Simple()
        => VerifyCS.VerifyCodeFixAsync("""
            class C
            {
                void M()
                {
                    {|CS0127:return|} 1;
                }
            }
            """, """
            class C
            {
                int M()
                {
                    return 1;
                }
            }
            """);

    [Fact]
    public Task Simple_WithTrivia()
        => VerifyCS.VerifyCodeFixAsync("""
            class C
            {
                /*A*/ void /*B*/ M()
                {
                    {|CS0127:return|} 1;
                }
            }
            """, """
            class C
            {
                /*A*/
                int /*B*/ M()
                {
                    return 1;
                }
            }
            """);

    [Fact]
    public Task ReturnString()
        => VerifyCS.VerifyCodeFixAsync("""
            class C
            {
                void M()
                {
                    {|CS0127:return|} "";
                }
            }
            """, """
            class C
            {
                string M()
                {
                    return "";
                }
            }
            """);

    [Fact]
    public Task ReturnNull()
        => VerifyCS.VerifyCodeFixAsync("""
            class C
            {
                void M()
                {
                    {|CS0127:return|} null;
                }
            }
            """, """
            class C
            {
                object M()
                {
                    return null;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/65302")]
    public Task ReturnTypelessTuple()
        => VerifyCS.VerifyCodeFixAsync("""
            class C
            {
                void M()
                {
                    {|CS0127:return|} (null, string.Empty);
                }
            }
            """, """
            class C
            {
                (object, string) M()
                {
                    return (null, string.Empty);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/65302")]
    public Task ReturnTypelessTuple_Nested()
        => VerifyCS.VerifyCodeFixAsync("""
            class C
            {
                void M()
                {
                    {|CS0127:return|} ((5, null), string.Empty);
                }
            }
            """, """
            class C
            {
                ((int, object), string) M()
                {
                    return ((5, null), string.Empty);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/65302")]
    public Task ReturnTypelessTuple_Async()
        => VerifyCS.VerifyCodeFixAsync("""
            class C
            {
                async void M()
                {
                    {|CS0127:return|} (null, string.Empty);
                }
            }
            """, """
            class C
            {
                async System.Threading.Tasks.Task<(object, string)> M()
                {
                    return (null, string.Empty);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/65302")]
    public Task ReturnTypelessTuple_Nested_Async()
        => VerifyCS.VerifyCodeFixAsync("""
            class C
            {
                async void M()
                {
                    {|CS0127:return|} ((5, null), string.Empty);
                }
            }
            """, """
            class C
            {
                async System.Threading.Tasks.Task<((int, object), string)> M()
                {
                    return ((5, null), string.Empty);
                }
            }
            """);

    [Fact]
    public Task ReturnLambda()
        => new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void M()
                    {
                        {|CS0127:return|} () => {};
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    object M()
                    {
                        return () => {};
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp10
        }.RunAsync();

    [Fact]
    public Task ReturnC()
        => VerifyCS.VerifyCodeFixAsync("""
            class C
            {
                void M()
                {
                    {|CS0127:return|} new C();
                }
            }
            """, """
            class C
            {
                C M()
                {
                    return new C();
                }
            }
            """);

    [Fact]
    public Task ReturnString_AsyncVoid()
        => VerifyCS.VerifyCodeFixAsync("""
            class C
            {
                async void M()
                {
                    {|CS0127:return|} "";
                }
            }
            """, """
            class C
            {
                async System.Threading.Tasks.Task<string> M()
                {
                    return "";
                }
            }
            """);

    [Fact]
    public Task ReturnString_AsyncVoid_WithUsing()
        => VerifyCS.VerifyCodeFixAsync("""
            using System.Threading.Tasks;

            class C
            {
                async void M()
                {
                    {|CS0127:return|} "";
                }
            }
            """, """
            using System.Threading.Tasks;

            class C
            {
                async Task<string> M()
                {
                    return "";
                }
            }
            """);

    [Fact]
    public Task ReturnString_AsyncTask()
        => VerifyCS.VerifyCodeFixAsync("""
            class C
            {
                async System.Threading.Tasks.Task M()
                {
                    {|CS1997:return|} "";
                }
            }
            """, """
            class C
            {
                async System.Threading.Tasks.Task<string> M()
                {
                    return "";
                }
            }
            """);

    [Fact]
    public Task ReturnString_LocalFunction()
        => VerifyCS.VerifyCodeFixAsync("""
            class C
            {
                void M()
                {
                    void local()
                    {
                        {|CS0127:return|} "";
                    }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    string local()
                    {
                        return "";
                    }
                }
            }
            """);

    [Fact]
    public Task ReturnString_AsyncVoid_LocalFunction()
        => VerifyCS.VerifyCodeFixAsync("""
            class C
            {
                void M()
                {
                    async void local()
                    {
                        {|CS0127:return|} "";
                    }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    async System.Threading.Tasks.Task<string> local()
                    {
                        return "";
                    }
                }
            }
            """);

    [Fact]
    public Task ExpressionBodied()
        => VerifyCS.VerifyCodeFixAsync("""
            class C
            {
                void M() => {|CS0201:1|};
            }
            """, """
            class C
            {
                int M() => 1;
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47089")]
    public async Task ExpressionAndReturnTypeAreVoid()
    {
        var markup = """
            using System;

            class C
            {
                void M()
                {
                    {|CS0127:return|} Console.WriteLine();
                }
            }
            """;
        await VerifyCS.VerifyCodeFixAsync(markup, markup);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/53574")]
    public Task TestAnonymousTypeTopLevel()
        => VerifyCS.VerifyCodeFixAsync("""
            class C
            {
                public void Method()
                {
                    {|CS0127:return|} new { A = 0, B = 1 };
                }
            }
            """, """
            class C
            {
                public object Method()
                {
                    return new { A = 0, B = 1 };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/53574")]
    public Task TestAnonymousTypeTopNested()
        => VerifyCS.VerifyCodeFixAsync("""
            class C
            {
                public void Method()
                {
                    {|CS0127:return|} new[] { new { A = 0, B = 1 } };
                }
            }
            """, """
            class C
            {
                public object Method()
                {
                    return new[] { new { A = 0, B = 1 } };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/64901")]
    public Task ReturnString_ValueTask()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Threading.Tasks;
            
                class C
                {
                    async ValueTask M()
                    {
                        {|CS1997:return|} "";
                    }
                }
                """,
            FixedCode = """
                using System.Threading.Tasks;
            
                class C
                {
                    async ValueTask<string> M()
                    {
                        return "";
                    }
                }
                """,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/64901")]
    public Task ReturnString_CustomTaskType()
        => new VerifyCS.Test
        {
            TestCode = """
            using System.Runtime.CompilerServices;
            
            [AsyncMethodBuilder(typeof(C))]
            class C
            {
                async C M()
                {
                    {|CS1997:return|} "";
                }
            }
            """,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79630")]
    public Task TestAnonymousTypeElement1()
        => VerifyCS.VerifyCodeFixAsync("""
            class C
            {
                public void Method()
                {
                    var v = new { A = 0, B = 1 };
                    var a = new[] { v };
                    {|CS0127:return|} v;
                }
            }
            """, """
            class C
            {
                public object Method()
                {
                    var v = new { A = 0, B = 1 };
                    var a = new[] { v };
                    return v;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79630")]
    public Task TestAnonymousTypeElement2()
        => VerifyCS.VerifyCodeFixAsync("""
            using System.Threading.Tasks;

            class C
            {
                public async Task Method()
                {
                    await Task.CompletedTask;
                    var v = new { A = 0, B = 1 };
                    var a = new[] { v };
                    {|CS1997:return|} v;
                }
            }
            """, """
            using System.Threading.Tasks;

            class C
            {
                public async Task<object> Method()
                {
                    await Task.CompletedTask;
                    var v = new { A = 0, B = 1 };
                    var a = new[] { v };
                    return v;
                }
            }
            """);
}
