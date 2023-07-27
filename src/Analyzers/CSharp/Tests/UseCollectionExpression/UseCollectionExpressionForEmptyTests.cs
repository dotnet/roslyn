// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.UseCollectionExpression;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.UnitTests.UseCollectionExpression;

using VerifyCS = CSharpCodeFixVerifier<
    CSharpUseCollectionExpressionForEmptyDiagnosticAnalyzer,
    CSharpUseCollectionExpressionForEmptyCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionExpression)]
public class UseCollectionExpressionForEmptyTests
{
    [Fact]
    public async Task ArrayEmpty1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            using System;

            class C
            {
                void M()
                {
                    var v = Array.Empty<int>();
                }
            }
            """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task ArrayEmpty2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            using System;

            class C
            {
                void M()
                {
                    int[] v = Array.Empty<int>();
                }
            }
            """,
            FixedCode = """
            using System;

            class C
            {
                void M()
                {
                    int[] v = [];
                }
            }
            """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task ArrayEmpty3()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            using System;
            class C
            {
                void M()
                {
                    object[] v = Array.Empty<string>();
                }
            }
            """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task ArrayEmpty5()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            using System;
            class C
            {
                void M()
                {
                    IEnumerable<string> v = Array.Empty<string>();
                }
            }
            """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task ArrayEmpty6()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            using System;

            class C
            {
                void M()
                {
                    string[] v = Array.Empty<string>();
                }
            }
            """,
            FixedCode = """
            using System;

            class C
            {
                void M()
                {
                    string[] v = [];
                }
            }
            """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task ArrayEmpty7()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            using System;

            class C
            {
                void M()
                {
                    string[] v = Array.Empty<string?>();
                }
            }
            """,
            FixedCode = """
            using System;

            class C
            {
                void M()
                {
                    string[] v = [];
                }
            }
            """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task ArrayEmpty8()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            using System;

            class C
            {
                void M()
                {
                    string?[] v = Array.Empty<string>();
                }
            }
            """,
            FixedCode = """
            using System;

            class C
            {
                void M()
                {
                    string?[] v = [];
                }
            }
            """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task ArrayEmpty9()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            using System;

            class C
            {
                void M()
                {
                    string?[] v = Array.Empty<string?>();
                }
            }
            """,
            FixedCode = """
            using System;

            class C
            {
                void M()
                {
                    string?[] v = [];
                }
            }
            """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task ArrayEmpty10()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            using System;

            class C
            {
                void M()
                {
                    string[]? v = Array.Empty<string>();
                }
            }
            """,
            FixedCode = """
            using System;

            class C
            {
                void M()
                {
                    string[]? v = [];
                }
            }
            """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestTrivia()
    {
        await new VerifyCS.Test
        {
            TestCode = """
            using System;

            class C
            {
                void M()
                {
                    int[] v = /*goo*/ Array.Empty<int>() /*bar*/;
                }
            }
            """,
            FixedCode = """
            using System;

            class C
            {
                void M()
                {
                    int[] v = /*goo*/ [] /*bar*/;
                }
            }
            """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }
}
