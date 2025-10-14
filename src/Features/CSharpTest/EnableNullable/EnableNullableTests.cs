// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.EnableNullable;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions.EnableNullable;

using VerifyCS = CSharpCodeRefactoringVerifier<EnableNullableCodeRefactoringProvider>;

[UseExportProvider]
public sealed class EnableNullableTests
{
    private static readonly Func<Solution, ProjectId, Solution> s_enableNullableInFixedSolution =
        (solution, projectId) =>
        {
            var project = solution.GetRequiredProject(projectId);
            var document = project.Documents.First();

            // Only the input solution contains '#nullable enable' or '#nullable  enable' in the first document
            if (!Regex.IsMatch(document.GetTextSynchronously(CancellationToken.None).ToString(), "#nullable  ?enable"))
            {
                var compilationOptions = (CSharpCompilationOptions)solution.GetRequiredProject(projectId).CompilationOptions!;
                solution = solution.WithProjectCompilationOptions(projectId, compilationOptions.WithNullableContextOptions(NullableContextOptions.Enable));
            }

            return solution;
        };

    private static readonly Func<Solution, ProjectId, Solution> s_enableNullableInFixedSolutionFromRestoreKeyword =
        (solution, projectId) =>
        {
            var project = solution.GetRequiredProject(projectId);
            var document = project.Documents.First();

            // Only the input solution contains '#nullable restore' or '#nullable  restore' in the first document
            if (!Regex.IsMatch(document.GetTextSynchronously(CancellationToken.None).ToString(), "#nullable  ?restore"))
            {
                var compilationOptions = (CSharpCompilationOptions)solution.GetRequiredProject(projectId).CompilationOptions!;
                solution = solution.WithProjectCompilationOptions(projectId, compilationOptions.WithNullableContextOptions(NullableContextOptions.Enable));
            }

            return solution;
        };

    private static readonly Func<Solution, ProjectId, Solution> s_enableNullableInFixedSolutionFromDisableKeyword =
        s_enableNullableInFixedSolutionFromRestoreKeyword;

    [Theory]
    [InlineData("$$#nullable enable")]
    [InlineData("#$$nullable enable")]
    [InlineData("#null$$able enable")]
    [InlineData("#nullable$$ enable")]
    [InlineData("#nullable $$ enable")]
    [InlineData("#nullable $$enable")]
    [InlineData("#nullable ena$$ble")]
    [InlineData("#nullable enable$$")]
    public async Task EnabledOnNullableEnable(string directive)
    {
        var code1 = $$"""

            {{directive}}

            class Example
            {
              string? value;
            }

            """;
        var code2 = """

            class Example2
            {
              string value;
            }

            """;
        var code3 = """

            class Example3
            {
            #nullable enable
              string? value;
            #nullable restore
            }

            """;
        var code4 = """

            #nullable disable

            class Example4
            {
              string value;
            }

            """;

        var fixedCode1 = """


            class Example
            {
              string? value;
            }

            """;
        var fixedCode2 = """

            #nullable disable

            class Example2
            {
              string value;
            }

            """;
        var fixedCode3 = """

            #nullable disable

            class Example3
            {
            #nullable restore
              string? value;
            #nullable disable
            }

            """;
        var fixedCode4 = """

            #nullable disable

            class Example4
            {
              string value;
            }

            """;

        await new VerifyCS.Test
        {
            TestState =
            {
                Sources =
                {
                    code1,
                    code2,
                    code3,
                    code4,
                },
            },
            FixedState =
            {
                Sources =
                {
                    fixedCode1,
                    fixedCode2,
                    fixedCode3,
                    fixedCode4,
                },
            },
            SolutionTransforms = { s_enableNullableInFixedSolution },
        }.RunAsync();
    }

    [Fact]
    public async Task PlacementAfterHeader()
    {
        var code1 = """

            #nullable enable$$

            class Example
            {
              string? value;
            }

            """;
        var code2 = """
            // File header line 1
            // File header line 2

            class Example2
            {
              string value;
            }

            """;
        var code3 = """
            #region File Header
            // File header line 1
            // File header line 2
            #endregion

            class Example3
            {
              string value;
            }

            """;

        var fixedCode1 = """


            class Example
            {
              string? value;
            }

            """;
        var fixedCode2 = """
            // File header line 1
            // File header line 2

            #nullable disable

            class Example2
            {
              string value;
            }

            """;
        var fixedCode3 = """
            #region File Header
            // File header line 1
            // File header line 2
            #endregion

            #nullable disable

            class Example3
            {
              string value;
            }

            """;

        await new VerifyCS.Test
        {
            TestState =
            {
                Sources =
                {
                    code1,
                    code2,
                    code3,
                },
            },
            FixedState =
            {
                Sources =
                {
                    fixedCode1,
                    fixedCode2,
                    fixedCode3,
                },
            },
            SolutionTransforms = { s_enableNullableInFixedSolution },
        }.RunAsync();
    }

    [Fact]
    public async Task PlacementBeforeDocComment()
    {
        var code1 = """

            #nullable enable$$

            class Example
            {
              string? value;
            }

            """;
        var code2 = """
            // Line comment
            class Example2
            {
              string value;
            }

            """;
        var code3 = """
            /*
             * Block comment
             */
            class Example3
            {
              string value;
            }

            """;
        var code4 = """
            /// <summary>Single line doc comment</summary>
            class Example4
            {
              string value;
            }

            """;
        var code5 = """
            /**
             * Multi-line doc comment
             */
            class Example5
            {
              string value;
            }

            """;

        var fixedCode1 = """


            class Example
            {
              string? value;
            }

            """;
        var fixedCode2 = """
            // Line comment
            #nullable disable

            class Example2
            {
              string value;
            }

            """;
        var fixedCode3 = """
            /*
             * Block comment
             */
            #nullable disable

            class Example3
            {
              string value;
            }

            """;
        var fixedCode4 = """
            #nullable disable

            /// <summary>Single line doc comment</summary>
            class Example4
            {
              string value;
            }

            """;
        var fixedCode5 = """
            #nullable disable

            /**
             * Multi-line doc comment
             */
            class Example5
            {
              string value;
            }

            """;

        await new VerifyCS.Test
        {
            TestState =
            {
                Sources =
                {
                    code1,
                    code2,
                    code3,
                    code4,
                    code5,
                },
            },
            FixedState =
            {
                Sources =
                {
                    fixedCode1,
                    fixedCode2,
                    fixedCode3,
                    fixedCode4,
                    fixedCode5,
                },
            },
            SolutionTransforms = { s_enableNullableInFixedSolution },
        }.RunAsync();
    }

    [Fact]
    public async Task OmitLeadingRestore()
    {
        var code1 = """

            #nullable enable$$

            class Example
            {
              string? value;
            }

            """;
        var code2 = """

            #nullable enable

            class Example2
            {
              string? value;
            }

            """;
        var code3 = """

            #nullable enable warnings

            class Example3
            {
              string value;
            }

            """;
        var code4 = """

            #nullable enable annotations

            class Example4
            {
              string? value;
            }

            """;

        var fixedCode1 = """


            class Example
            {
              string? value;
            }

            """;
        var fixedCode2 = """


            class Example2
            {
              string? value;
            }

            """;
        var fixedCode3 = """

            #nullable disable

            #nullable restore warnings

            class Example3
            {
              string value;
            }

            """;
        var fixedCode4 = """

            #nullable disable

            #nullable restore annotations

            class Example4
            {
              string? value;
            }

            """;

        await new VerifyCS.Test
        {
            TestState =
            {
                Sources =
                {
                    code1,
                    code2,
                    code3,
                    code4,
                },
            },
            FixedState =
            {
                Sources =
                {
                    fixedCode1,
                    fixedCode2,
                    fixedCode3,
                    fixedCode4,
                },
            },
            SolutionTransforms = { s_enableNullableInFixedSolution },
        }.RunAsync();
    }

    [Fact]
    public async Task IgnoreGeneratedCode()
    {
        var code1 = """

            #nullable enable$$

            class Example
            {
              string? value;
            }

            """;
        var generatedCode1 = """
            // <auto-generated/>

            #nullable enable

            class Example2
            {
              string? value;
            }

            """;
        var generatedCode2 = """
            // <auto-generated/>

            #nullable disable

            class Example3
            {
              string value;
            }

            """;
        var generatedCode3 = """
            // <auto-generated/>

            #nullable restore

            class Example4
            {
              string {|#0:value|};
            }

            """;

        var fixedCode1 = """


            class Example
            {
              string? value;
            }

            """;

        await new VerifyCS.Test
        {
            TestState =
            {
                Sources =
                {
                    code1,
                    generatedCode1,
                    generatedCode2,
                    generatedCode3,
                },
            },
            FixedState =
            {
                Sources =
                {
                    fixedCode1,
                    generatedCode1,
                    generatedCode2,
                    generatedCode3,
                },
                ExpectedDiagnostics =
                {
                    // /0/Test3.cs(7,10): error CS8618: Non-nullable field 'value' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable.
                    DiagnosticResult.CompilerError("CS8618").WithSpan("/0/Test3.cs", 7, 10, 7, 15).WithSpan("/0/Test3.cs", 7, 10, 7, 15).WithArguments("field", "value"),
                },
            },
            SolutionTransforms = { s_enableNullableInFixedSolution },
        }.RunAsync();
    }

    [Theory]
    [InlineData(NullableContextOptions.Annotations)]
    [InlineData(NullableContextOptions.Warnings)]
    [InlineData(NullableContextOptions.Enable)]
    public Task DisabledIfSetInProject(NullableContextOptions nullableContextOptions)
        => new VerifyCS.Test
        {
            TestCode = """

            #nullable enable$$

            """,
            SolutionTransforms =
            {
                (solution, projectId) =>
                {
                    var compilationOptions = (CSharpCompilationOptions)solution.GetRequiredProject(projectId).CompilationOptions!;
                    return solution.WithProjectCompilationOptions(projectId, compilationOptions.WithNullableContextOptions(nullableContextOptions));
                },
            },
        }.RunAsync();

    [Theory]
    [InlineData(LanguageVersion.CSharp1)]
    [InlineData(LanguageVersion.CSharp2)]
    [InlineData(LanguageVersion.CSharp3)]
    [InlineData(LanguageVersion.CSharp4)]
    [InlineData(LanguageVersion.CSharp5)]
    [InlineData(LanguageVersion.CSharp6)]
    [InlineData(LanguageVersion.CSharp7)]
    [InlineData(LanguageVersion.CSharp7_1)]
    [InlineData(LanguageVersion.CSharp7_2)]
    [InlineData(LanguageVersion.CSharp7_3)]
    public async Task DisabledForUnsupportedLanguageVersion(LanguageVersion languageVersion)
    {
        var code = """

            #{|#0:nullable|} enable$$

            """;

        var error = languageVersion switch
        {
            LanguageVersion.CSharp1 => "CS8022",
            LanguageVersion.CSharp2 => "CS8023",
            LanguageVersion.CSharp3 => "CS8024",
            LanguageVersion.CSharp4 => "CS8025",
            LanguageVersion.CSharp5 => "CS8026",
            LanguageVersion.CSharp6 => "CS8059",
            LanguageVersion.CSharp7 => "CS8107",
            LanguageVersion.CSharp7_1 => "CS8302",
            LanguageVersion.CSharp7_2 => "CS8320",
            LanguageVersion.CSharp7_3 => "CS8370",
            _ => throw ExceptionUtilities.Unreachable(),
        };

        // /0/Test0.cs(2,2): error [error]: Feature 'nullable reference types' is not available in C# [version]. Please use language version 8.0 or greater.
        var expected = DiagnosticResult.CompilerError(error).WithLocation(0);
        if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName == "en")
        {
            expected = expected.WithArguments("nullable reference types", "8.0");
        }

        await new VerifyCS.Test
        {
            TestCode = code,
            ExpectedDiagnostics = { expected },
            FixedCode = code,
            LanguageVersion = languageVersion,
        }.RunAsync();
    }

    [Theory]
    [InlineData("$$#nullable restore")]
    [InlineData("#$$nullable restore")]
    [InlineData("#null$$able restore")]
    [InlineData("#nullable$$ restore")]
    [InlineData("#nullable $$ restore")]
    [InlineData("#nullable $$restore")]
    [InlineData("#nullable res$$tore")]
    [InlineData("#nullable restore$$")]
    public async Task EnabledOnNullableRestore(string directive)
    {
        var code1 = $$"""

            {{directive}}

            class Example
            {
              string value;
            }

            """;
        var code2 = """

            class Example2
            {
              string value;
            }

            """;
        var code3 = """

            class Example3
            {
            #nullable enable
              string? value;
            #nullable restore
            }

            """;
        var code4 = """

            #nullable disable

            class Example4
            {
              string value;
            }

            """;

        var fixedDirective = directive.Replace("$$", "").Replace("restore", "disable");

        var fixedCode1 = $$"""

            {{fixedDirective}}

            class Example
            {
              string value;
            }

            """;
        var fixedCode2 = """

            #nullable disable

            class Example2
            {
              string value;
            }

            """;
        var fixedCode3 = """

            #nullable disable

            class Example3
            {
            #nullable restore
              string? value;
            #nullable disable
            }

            """;
        var fixedCode4 = """

            #nullable disable

            class Example4
            {
              string value;
            }

            """;

        await new VerifyCS.Test
        {
            TestState =
            {
                Sources =
                {
                    code1,
                    code2,
                    code3,
                    code4,
                },
            },
            FixedState =
            {
                Sources =
                {
                    fixedCode1,
                    fixedCode2,
                    fixedCode3,
                    fixedCode4,
                },
            },
            SolutionTransforms = { s_enableNullableInFixedSolutionFromRestoreKeyword },
        }.RunAsync();
    }

    [Theory]
    [InlineData("$$#nullable disable")]
    [InlineData("#$$nullable disable")]
    [InlineData("#null$$able disable")]
    [InlineData("#nullable$$ disable")]
    [InlineData("#nullable $$ disable")]
    [InlineData("#nullable $$disable")]
    [InlineData("#nullable dis$$able")]
    [InlineData("#nullable disable$$")]
    public async Task EnabledOnNullableDisable(string directive)
    {
        var code1 = $$"""

            {{directive}}

            class Example
            {
              string value;
            }

            #nullable restore

            """;
        var code2 = """

            class Example2
            {
              string value;
            }

            """;
        var code3 = """

            class Example3
            {
            #nullable enable
              string? value;
            #nullable restore
            }

            """;
        var code4 = """

            #nullable disable

            class Example4
            {
              string value;
            }

            """;

        var fixedDirective = directive.Replace("$$", "");

        var fixedCode1 = $$"""

            {{fixedDirective}}

            class Example
            {
              string value;
            }

            #nullable disable

            """;
        var fixedCode2 = """

            #nullable disable

            class Example2
            {
              string value;
            }

            """;
        var fixedCode3 = """

            #nullable disable

            class Example3
            {
            #nullable restore
              string? value;
            #nullable disable
            }

            """;
        var fixedCode4 = """

            #nullable disable

            class Example4
            {
              string value;
            }

            """;

        await new VerifyCS.Test
        {
            TestState =
            {
                Sources =
                {
                    code1,
                    code2,
                    code3,
                    code4,
                },
            },
            FixedState =
            {
                Sources =
                {
                    fixedCode1,
                    fixedCode2,
                    fixedCode3,
                    fixedCode4,
                },
            },
            SolutionTransforms = { s_enableNullableInFixedSolutionFromDisableKeyword },
        }.RunAsync();
    }
}
