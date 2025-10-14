// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Analyzers.RemoveUnnecessaryNullableDirective;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.RemoveUnnecessaryNullableDirective;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.UnitTests.RemoveUnnecessaryNullableDirective;

using VerifyCS = CSharpCodeFixVerifier<
    CSharpRemoveRedundantNullableDirectiveDiagnosticAnalyzer,
    CSharpRemoveUnnecessaryNullableDirectiveCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryNullableDirective)]
public sealed class CSharpRemoveRedundantNullableDirectiveTests
{
    [Theory]
    [InlineData(NullableContextOptions.Disable, NullableContextOptions.Annotations)]
    [InlineData(NullableContextOptions.Disable, NullableContextOptions.Warnings)]
    [InlineData(NullableContextOptions.Disable, NullableContextOptions.Enable)]
    [InlineData(NullableContextOptions.Annotations, NullableContextOptions.Warnings)]
    [InlineData(NullableContextOptions.Annotations, NullableContextOptions.Enable)]
    [InlineData(NullableContextOptions.Warnings, NullableContextOptions.Annotations)]
    [InlineData(NullableContextOptions.Warnings, NullableContextOptions.Enable)]
    public Task TestRedundantEnableDiffersFromCompilation(NullableContextOptions compilationContext, NullableContextOptions codeContext)
        => VerifyCodeFixAsync(
            compilationContext,
            $$"""
            #nullable {{GetEnableDirectiveContext(codeContext)}}
            [|#nullable {{GetEnableDirectiveContext(codeContext)}}|]
            class Program
            {
            }
            """,
            $$"""
            #nullable {{GetEnableDirectiveContext(codeContext)}}
            class Program
            {
            }
            """);

    [Theory]
    [InlineData(NullableContextOptions.Annotations, NullableContextOptions.Annotations)]
    [InlineData(NullableContextOptions.Warnings, NullableContextOptions.Warnings)]
    [InlineData(NullableContextOptions.Enable, NullableContextOptions.Annotations)]
    [InlineData(NullableContextOptions.Enable, NullableContextOptions.Warnings)]
    [InlineData(NullableContextOptions.Enable, NullableContextOptions.Enable)]
    public Task TestRedundantEnableMatchesCompilation(NullableContextOptions compilationContext, NullableContextOptions codeContext)
        => VerifyCodeFixAsync(
            compilationContext,
            $$"""
            [|#nullable {{GetEnableDirectiveContext(codeContext)}}|]
            [|#nullable {{GetEnableDirectiveContext(codeContext)}}|]
            class Program
            {
            }
            """,
            """
            class Program
            {
            }
            """);

    [Theory]
    [InlineData(NullableContextOptions.Annotations, NullableContextOptions.Annotations)]
    [InlineData(NullableContextOptions.Annotations, NullableContextOptions.Enable)]
    [InlineData(NullableContextOptions.Warnings, NullableContextOptions.Warnings)]
    [InlineData(NullableContextOptions.Warnings, NullableContextOptions.Enable)]
    [InlineData(NullableContextOptions.Enable, NullableContextOptions.Annotations)]
    [InlineData(NullableContextOptions.Enable, NullableContextOptions.Warnings)]
    [InlineData(NullableContextOptions.Enable, NullableContextOptions.Enable)]
    public Task TestRedundantDisableDiffersFromCompilation(NullableContextOptions compilationContext, NullableContextOptions codeContext)
        => VerifyCodeFixAsync(
            compilationContext,
            $$"""
            #nullable {{GetDisableDirectiveContext(codeContext)}}
            [|#nullable {{GetDisableDirectiveContext(codeContext)}}|]
            class Program
            {
            }
            """,
            $$"""
            #nullable {{GetDisableDirectiveContext(codeContext)}}
            class Program
            {
            }
            """);

    [Theory]
    [InlineData(NullableContextOptions.Disable, NullableContextOptions.Annotations)]
    [InlineData(NullableContextOptions.Disable, NullableContextOptions.Warnings)]
    [InlineData(NullableContextOptions.Disable, NullableContextOptions.Enable)]
    [InlineData(NullableContextOptions.Annotations, NullableContextOptions.Warnings)]
    [InlineData(NullableContextOptions.Warnings, NullableContextOptions.Annotations)]
    public Task TestRedundantDisableMatchesCompilation(NullableContextOptions compilationContext, NullableContextOptions codeContext)
        => VerifyCodeFixAsync(
            compilationContext,
            $$"""
            [|#nullable {{GetDisableDirectiveContext(codeContext)}}|]
            [|#nullable {{GetDisableDirectiveContext(codeContext)}}|]
            class Program
            {
            }
            """,
            """
            class Program
            {
            }
            """);

    [Theory, CombinatorialData]
    public async Task TestRedundantRestoreDiffersFromPriorContext(NullableContextOptions compilationContext)
    {
        var enable = compilationContext != NullableContextOptions.Enable;
        await VerifyCodeFixAsync(
            compilationContext,
            $$"""
            #nullable {{(enable ? "enable" : "disable")}}
            #nullable restore
            [|#nullable restore|]
            class Program
            {
            }
            """,
            $$"""
            #nullable {{(enable ? "enable" : "disable")}}
            #nullable restore
            class Program
            {
            }
            """);
    }

    [Theory, CombinatorialData]
    public Task TestRedundantRestoreMatchesCompilation(NullableContextOptions compilationContext)
        => VerifyCodeFixAsync(
            compilationContext,
            $$"""
            [|#nullable restore|]
            class Program
            {
            }
            """,
            """
            class Program
            {
            }
            """);

    [Fact]
    public Task TestRedundantDirectiveWithFileHeader()
        => VerifyCodeFixAsync(
            NullableContextOptions.Enable,
            """
            // File Header

            [|#nullable enable|]

            class Program
            {
            }
            """,
            """
            // File Header

            class Program
            {
            }
            """);

    [Fact]
    public Task TestRedundantDirectiveBetweenUsingAndNamespace()
        => VerifyCodeFixAsync(
            NullableContextOptions.Enable,
            """
            using System;
            
            [|#nullable enable|]

            namespace MyNamespace
            {
                class MyClass
                {
                }
            }
            """,
            """
            using System;

            namespace MyNamespace
            {
                class MyClass
                {
                }
            }
            """);

    [Fact]
    public Task TestRedundantDirectiveBetweenUsingAndNamespace2()
        => VerifyCodeFixAsync(
            NullableContextOptions.Enable,
            """
            using System;
            [|#nullable enable|]

            namespace MyNamespace
            {
                class MyClass
                {
                }
            }
            """,
            """
            using System;

            namespace MyNamespace
            {
                class MyClass
                {
                }
            }
            """);

    [Fact]
    public Task TestRedundantDirectiveBetweenUsingAndNamespace3()
        => VerifyCodeFixAsync(
            NullableContextOptions.Enable,
            """
            using System;

            [|#nullable enable|]
            namespace MyNamespace
            {
                class MyClass
                {
                }
            }
            """,
            """
            using System;

            namespace MyNamespace
            {
                class MyClass
                {
                }
            }
            """);

    [Fact]
    public Task TestRedundantDirectiveWithNamespaceAndDerivedType()
        => VerifyCodeFixAsync(
            NullableContextOptions.Enable,
            """
            [|#nullable enable|]

            using System;

            namespace X.Y
            {
                class ProgramException : Exception
                {
                }
            }
            """,
            """

            using System;

            namespace X.Y
            {
                class ProgramException : Exception
                {
                }
            }
            """);

    [Fact]
    public Task TestRedundantDirectiveMultiple1()
        => VerifyCodeFixAsync(
            NullableContextOptions.Enable,
            """
            using System;

            [|#nullable enable|]
            [|#nullable enable|]

            namespace MyNamespace
            {
                class MyClass
                {
                }
            }
            """,
            """
            using System;

            namespace MyNamespace
            {
                class MyClass
                {
                }
            }
            """);

    [Fact]
    public Task TestRedundantDirectiveMultiple2()
        => VerifyCodeFixAsync(
            NullableContextOptions.Enable,
            """
            using System;

            [|#nullable enable|]

            [|#nullable enable|]

            namespace MyNamespace
            {
                class MyClass
                {
                }
            }
            """,
            """
            using System;

            namespace MyNamespace
            {
                class MyClass
                {
                }
            }
            """);

    [Fact]
    public Task TestRedundantDirectiveMultiple3()
        => VerifyCodeFixAsync(
            NullableContextOptions.Enable,
            """
            using System;
            [|#nullable enable|]
            [|#nullable enable|]

            namespace MyNamespace
            {
                class MyClass
                {
                }
            }
            """,
            """
            using System;

            namespace MyNamespace
            {
                class MyClass
                {
                }
            }
            """);

    [Fact]
    public Task TestRedundantDirectiveMultiple4()
        => VerifyCodeFixAsync(
            NullableContextOptions.Enable,
            """
            using System;
            [|#nullable enable|]

            [|#nullable enable|]

            namespace MyNamespace
            {
                class MyClass
                {
                }
            }
            """,
            """
            using System;

            namespace MyNamespace
            {
                class MyClass
                {
                }
            }
            """);

    [Fact]
    public Task TestRedundantDirectiveMultiple5()
        => VerifyCodeFixAsync(
            NullableContextOptions.Enable,
            """
            using System;

            [|#nullable enable|]
            [|#nullable enable|]
            namespace MyNamespace
            {
                class MyClass
                {
                }
            }
            """,
            """
            using System;

            namespace MyNamespace
            {
                class MyClass
                {
                }
            }
            """);

    private static string GetDisableDirectiveContext(NullableContextOptions options)
    {
        return options switch
        {
            NullableContextOptions.Warnings => "disable warnings",
            NullableContextOptions.Annotations => "disable annotations",
            NullableContextOptions.Enable => "disable",
            _ => throw ExceptionUtilities.UnexpectedValue(options),
        };
    }

    private static string GetEnableDirectiveContext(NullableContextOptions options)
    {
        return options switch
        {
            NullableContextOptions.Warnings => "enable warnings",
            NullableContextOptions.Annotations => "enable annotations",
            NullableContextOptions.Enable => "enable",
            _ => throw ExceptionUtilities.UnexpectedValue(options),
        };
    }

    private static Task VerifyCodeFixAsync(NullableContextOptions compilationNullableContextOptions, string source, string fixedSource)
        => new VerifyCS.Test
        {
            TestCode = source,
            FixedCode = fixedSource,
            SolutionTransforms =
            {
                (solution, projectId) =>
                {
                    var compilationOptions = (CSharpCompilationOptions?)solution.GetRequiredProject(projectId).CompilationOptions;
                    Contract.ThrowIfNull(compilationOptions);

                    return solution.WithProjectCompilationOptions(projectId, compilationOptions.WithNullableContextOptions(compilationNullableContextOptions));
                },
            },
        }.RunAsync();
}
