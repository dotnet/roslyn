// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.ConvertTypeOfToNameOf;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertTypeOfToNameOf;

using VerifyCS = CSharpCodeFixVerifier<CSharpConvertTypeOfToNameOfDiagnosticAnalyzer,
    CSharpConvertTypeOfToNameOfCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.ConvertTypeOfToNameOf)]
public sealed partial class ConvertTypeOfToNameOfTests
{
    private static readonly LanguageVersion CSharp14 = LanguageVersion.Preview;

    [Fact]
    public Task BasicType()
        => VerifyCS.VerifyCodeFixAsync("""
            class Test
            {
                void Method()
                {
                    var typeName = [|typeof(Test).Name|];
                }
            }
            """, """
            class Test
            {
                void Method()
                {
                    var typeName = nameof(Test);
                }
            }
            """);

    [Fact]
    public Task ClassLibraryType()
        => VerifyCS.VerifyCodeFixAsync("""
            class Test
            {
                void Method()
                {
                    var typeName = [|typeof(System.String).Name|];
                }
            }
            """, """
            class Test
            {
                void Method()
                {
                    var typeName = nameof(System.String);
                }
            }
            """);

    [Fact]
    public Task ClassLibraryTypeWithUsing()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;

            class Test
            {
                void Method()
                {
                    var typeName = [|typeof(String).Name|];
                }
            }
            """, """
            using System;

            class Test
            {
                void Method()
                {
                    var typeName = nameof(String);
                }
            }
            """);

    [Fact]
    public Task NestedCall()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;

            class Test
            {
                void Method()
                {
                    var typeName = Foo([|typeof(System.String).Name|]);
                }

                int Foo(String typeName) {
                    return 0;
                }
            }
            """, """
            using System;

            class Test
            {
                void Method()
                {
                    var typeName = Foo(nameof(String));
                }

                int Foo(String typeName) {
                    return 0;
                }
            }
            """);

    [Fact]
    public async Task NotOnVariableContainingType()
    {
        var text = """
            using System;

            class Test
            {
                void Method()
                {
                    var typeVar = typeof(String);
                    var typeName = typeVar.Name;
                }
            }
            """;
        await VerifyCS.VerifyCodeFixAsync(text, text);
    }

    [Fact]
    public Task PrimitiveType()
        => VerifyCS.VerifyCodeFixAsync("""
            class Test
            {
                void Method()
                {
                        var typeName = [|typeof(int).Name|];
                }
            }
            """, """
            class Test
            {
                void Method()
                {
                        var typeName = nameof(System.Int32);
                }
            }
            """);

    [Fact]
    public Task PrimitiveTypeWithUsing()
        => VerifyCS.VerifyCodeFixAsync("""
            using System;

            class Test
            {
                void Method()
                {
                        var typeName = [|typeof(int).Name|];
                }
            }
            """, """
            using System;

            class Test
            {
                void Method()
                {
                        var typeName = nameof(Int32);
                }
            }
            """);

    [Fact]
    public async Task NotOnGenericType()
    {
        var text = """
            class Test<T>
            {
                void Method()
                {
                    var typeName = typeof(T).Name;
                }
            }
            """;
        await VerifyCS.VerifyCodeFixAsync(text, text);
    }

    [Fact]
    public async Task NotOnSimilarStatements()
    {
        var text = """
            class Test
            {
                void Method()
                {
                    var typeName1 = typeof(Test);
                    var typeName2 = typeof(Test).ToString();
                    var typeName3 = typeof(Test).FullName;
                }
            }
            """;
        await VerifyCS.VerifyCodeFixAsync(text, text);
    }

    [Fact]
    public Task GenericType_CSharp13()
        => new VerifyCS.Test
        {
            TestCode = """
                class Test
                {
                    class Goo<T> 
                    { 
                        void M() 
                        {
                            _ = typeof(Goo<int>).Name;
                        }
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp13,
        }.RunAsync();

    [Fact]
    public Task GenericType_CSharp14()
        => new VerifyCS.Test
        {
            TestCode = """
                class Test
                {
                    class Goo<T> 
                    { 
                        void M() 
                        {
                            _ = typeof(Goo<int>).Name;
                        }
                    }
                }
                """,
            LanguageVersion = CSharp14,
        }.RunAsync();

    [Fact]
    public Task UnboundGenericType_CSharp13()
        => new VerifyCS.Test
        {
            TestCode = """
                class Test
                {
                    class Goo<T> 
                    { 
                        void M() 
                        {
                            _ = typeof(Goo<>).Name;
                        }
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp13,
        }.RunAsync();

    [Fact]
    public Task UnboundGenericType_CSharp14()
        => new VerifyCS.Test
        {
            TestCode = """
                class Test
                {
                    class Goo<T> 
                    { 
                        void M() 
                        {
                            _ = typeof(Goo<>).Name;
                        }
                    }
                }
                """,
            LanguageVersion = CSharp14,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47129")]
    public Task NestedInGenericType()
        => VerifyCS.VerifyCodeFixAsync("""
            class Test
            {
                class Goo<T> 
                { 
                    class Bar 
                    { 
                        void M() 
                        {
                            _ = [|typeof(Bar).Name|];
                        }
                    }
                }
            }
            """, """
            class Test
            {
                class Goo<T> 
                { 
                    class Bar 
                    { 
                        void M() 
                        {
                            _ = nameof(Bar);
                        }
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47129")]
    public Task NestedInGenericType2_CSharp13()
        => new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Collections.Generic;

                class Test
                {
                    public void M()
                    {
                        Console.WriteLine([|typeof(List<int>.Enumerator).Name|]);
                    }
                }
                """,
            FixedCode = """
                using System;
                using System.Collections.Generic;

                class Test
                {
                    public void M()
                    {
                        Console.WriteLine(nameof(List<Int32>.Enumerator));
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp13,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47129")]
    public Task NestedInGenericType2_CSharp14()
        => new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Collections.Generic;

                class Test
                {
                    public void M()
                    {
                        Console.WriteLine([|typeof(List<int>.Enumerator).Name|]);
                    }
                }
                """,
            FixedCode = """
                using System;
                using System.Collections.Generic;

                class Test
                {
                    public void M()
                    {
                        Console.WriteLine(nameof(List<>.Enumerator));
                    }
                }
                """,
            LanguageVersion = CSharp14,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47129")]
    public Task NestedInGenericType_UnboundTypeof_CSharp13()
        => new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Collections.Generic;

                class Test
                {
                    public void M()
                    {
                        Console.WriteLine([|typeof(List<>.Enumerator).Name|]);
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp13,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47129")]
    public Task NestedInGenericType_UnboundTypeof_CSharp14()
        => new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Collections.Generic;

                class Test
                {
                    public void M()
                    {
                        Console.WriteLine([|typeof(List<>.Enumerator).Name|]);
                    }
                }
                """,
            FixedCode = """
                using System;
                using System.Collections.Generic;

                class Test
                {
                    public void M()
                    {
                        Console.WriteLine(nameof(List<>.Enumerator));
                    }
                }
                """,
            LanguageVersion = CSharp14,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/54233")]
    public async Task NotOnVoid()
    {
        var text = """
            class C
            {
                void M()
                {
                    var x = typeof(void).Name;
                }
            }
            """;
        await VerifyCS.VerifyCodeFixAsync(text, text);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47128")]
    public Task TestNint()
        => new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M()
                    {
                        Console.WriteLine([|typeof(nint).Name|]);
                    }
                }
                """,
            FixedCode = """
                using System;

                class C
                {
                    void M()
                    {
                        Console.WriteLine(nameof(IntPtr));
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp10,
        }.RunAsync();
}
