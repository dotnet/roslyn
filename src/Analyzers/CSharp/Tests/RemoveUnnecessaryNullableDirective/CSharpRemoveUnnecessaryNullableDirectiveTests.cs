// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.RemoveUnnecessaryNullableDirective;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.RemoveUnnecessaryNullableDirective;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.UnitTests.RemoveUnnecessaryNullableDirective;

using VerifyCS = CSharpCodeFixVerifier<
    CSharpRemoveUnnecessaryNullableDirectiveDiagnosticAnalyzer,
    CSharpRemoveUnnecessaryNullableDirectiveCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryNullableDirective)]
public sealed class CSharpRemoveUnnecessaryNullableDirectiveTests
{
    [Theory]
    [InlineData(NullableContextOptions.Annotations, NullableContextOptions.Annotations)]
    [InlineData(NullableContextOptions.Annotations, NullableContextOptions.Enable)]
    [InlineData(NullableContextOptions.Warnings, NullableContextOptions.Warnings)]
    [InlineData(NullableContextOptions.Warnings, NullableContextOptions.Enable)]
    [InlineData(NullableContextOptions.Enable, NullableContextOptions.Annotations)]
    [InlineData(NullableContextOptions.Enable, NullableContextOptions.Warnings)]
    [InlineData(NullableContextOptions.Enable, NullableContextOptions.Enable)]
    public Task TestUnnecessaryDisableDiffersFromCompilation(NullableContextOptions compilationContext, NullableContextOptions codeContext)
        => VerifyCodeFixAsync(
            compilationContext,
            $$"""
            [|#nullable {{GetDisableDirectiveContext(codeContext)}}|]
            class Program
            {
            }
            """,
            $$"""
            class Program
            {
            }
            """);

    [Fact]
    public Task TestUnnecessaryDisableEnumDeclaration()
        => VerifyCodeFixAsync(
            NullableContextOptions.Enable,
            """
            [|#nullable disable|]
            enum EnumName
            {
                First,
                Second,
            }
            """,
            """
            enum EnumName
            {
                First,
                Second,
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/65401")]
    public Task TestUnnecessaryDisableEnumDeclaration_WithAttribute()
        => VerifyCodeFixAsync(
            NullableContextOptions.Enable,
            """
            [|#nullable disable|]
            using System;

            [CLSCompliant(false)]
            enum EnumName
            {
                First,
                Second,
            }
            """,
            """
            using System;

            [CLSCompliant(false)]
            enum EnumName
            {
                First,
                Second,
            }
            """);

    [Fact]
    public Task TestUnnecessaryDisableEnumDeclarationWithFileHeader()
        => VerifyCodeFixAsync(
            NullableContextOptions.Enable,
            """
            // File Header

            [|#nullable disable|]

            enum EnumName
            {
                First,
                Second,
            }
            """,
            """
            // File Header

            enum EnumName
            {
                First,
                Second,
            }
            """);

    [Fact]
    public Task TestUnnecessaryDirectiveWithNamespaceAndDerivedType()
        => VerifyCodeFixAsync(
            NullableContextOptions.Enable,
            """
            [|#nullable disable|]

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
    public Task TestUnnecessaryDirectiveWithNamespaceAndDerivedFromQualifiedBaseType()
        => VerifyCodeFixAsync(
            NullableContextOptions.Enable,
            """
            [|#nullable disable|]

            namespace X.Y
            {
                class ProgramException : System.Exception
                {
                }
            }
            """,
            """

            namespace X.Y
            {
                class ProgramException : System.Exception
                {
                }
            }
            """);

    [Fact]
    public Task TestUnnecessaryDirectiveWithQualifiedUsingDirectives()
        => VerifyCodeFixAsync(
            NullableContextOptions.Enable,
            """
            [|#nullable disable|]

            using System;
            using System.Runtime.InteropServices;
            using CustomException = System.Exception;
            using static System.String;
            """,
            """

            using System;
            using System.Runtime.InteropServices;
            using CustomException = System.Exception;
            using static System.String;
            """);

    [Theory]
    [InlineData("disable")]
    [InlineData("restore")]
    public Task TestUnnecessaryDisableAtEndOfFile(string keyword)
        => VerifyCodeFixAsync(
            NullableContextOptions.Disable,
            $$"""
            #nullable enable
            struct StructName
            {
                string Field;
            }
            [|#nullable {{keyword}}|]

            """,
            $$"""
            #nullable enable
            struct StructName
            {
                string Field;
            }
            
            """);

    [Fact]
    public async Task TestUnnecessaryDisableIgnoredWhenFollowedByConditionalDirective()
    {
        var code =
            """
            #nullable enable
            struct StructName
            {
                string Field;
            }
            #nullable disable
            #if false
            #endif
            """;

        await VerifyCodeFixAsync(NullableContextOptions.Disable, code, code);
    }

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
