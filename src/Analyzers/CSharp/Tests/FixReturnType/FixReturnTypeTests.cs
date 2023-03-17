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

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.FixReturnType
{
    using VerifyCS = CSharpCodeFixVerifier<
        EmptyDiagnosticAnalyzer,
        CSharpFixReturnTypeCodeFixProvider>;

    [Trait(Traits.Feature, Traits.Features.CodeActionsFixReturnType)]
    public class FixReturnTypeTests
    {
        [Fact]
        public async Task Simple()
        {
            await VerifyCS.VerifyCodeFixAsync("""
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
        }

        [Fact]
        public async Task Simple_WithTrivia()
        {
            await VerifyCS.VerifyCodeFixAsync("""
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
            // Note: the formatting change is introduced by Formatter.FormatAsync
        }

        [Fact]
        public async Task ReturnString()
        {
            await VerifyCS.VerifyCodeFixAsync("""
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
        }

        [Fact]
        public async Task ReturnNull()
        {
            await VerifyCS.VerifyCodeFixAsync("""
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/65302")]
        public async Task ReturnTypelessTuple()
        {
            await VerifyCS.VerifyCodeFixAsync("""
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/65302")]
        public async Task ReturnTypelessTuple_Nested()
        {
            await VerifyCS.VerifyCodeFixAsync("""
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/65302")]
        public async Task ReturnTypelessTuple_Async()
        {
            await VerifyCS.VerifyCodeFixAsync("""
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/65302")]
        public async Task ReturnTypelessTuple_Nested_Async()
        {
            await VerifyCS.VerifyCodeFixAsync("""
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
        }

        [Fact]
        public async Task ReturnLambda()
        {
            await new VerifyCS.Test
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
        }

        [Fact]
        public async Task ReturnC()
        {
            await VerifyCS.VerifyCodeFixAsync("""
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
        }

        [Fact]
        public async Task ReturnString_AsyncVoid()
        {
            await VerifyCS.VerifyCodeFixAsync("""
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
        }

        [Fact]
        public async Task ReturnString_AsyncVoid_WithUsing()
        {
            await VerifyCS.VerifyCodeFixAsync("""
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
        }

        [Fact]
        public async Task ReturnString_AsyncTask()
        {
            await VerifyCS.VerifyCodeFixAsync("""
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
        }

        [Fact]
        public async Task ReturnString_LocalFunction()
        {
            await VerifyCS.VerifyCodeFixAsync("""
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
        }

        [Fact]
        public async Task ReturnString_AsyncVoid_LocalFunction()
        {
            await VerifyCS.VerifyCodeFixAsync("""
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
        }

        [Fact]
        public async Task ExpressionBodied()
        {
            await VerifyCS.VerifyCodeFixAsync("""
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
        }

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
        public async Task TestAnonymousTypeTopLevel()
        {
            await VerifyCS.VerifyCodeFixAsync("""
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/53574")]
        public async Task TestAnonymousTypeTopNested()
        {
            await VerifyCS.VerifyCodeFixAsync("""
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/64901")]
        public async Task ReturnString_ValueTask()
        {
            await new VerifyCS.Test
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/64901")]
        public async Task ReturnString_CustomTaskType()
        {
            var markup = """
                using System.Runtime.CompilerServices;
                
                [AsyncMethodBuilder(typeof(C))]
                class C
                {
                    async C M()
                    {
                        {|CS1997:return|} "";
                    }
                }
                """;

            await new VerifyCS.Test
            {
                TestCode = markup,
                FixedCode = markup,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net60
            }.RunAsync();
        }
    }
}
