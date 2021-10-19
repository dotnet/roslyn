// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.CSharpCodeRefactoringVerifier<
    Microsoft.CodeAnalysis.CSharp.CodeRefactorings.EnableNullable.EnableNullableCodeRefactoringProvider>;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions.EnableNullable
{
    public class EnableNullableTests
    {
        private static readonly Func<Solution, ProjectId, Solution> s_enableNullableInFixedSolution =
            (solution, projectId) =>
            {
                var project = solution.GetRequiredProject(projectId);
                var document = project.Documents.First();

                // Only the input solution contains '#nullable enable'
                if (!document.GetTextSynchronously(CancellationToken.None).ToString().Contains("#nullable enable"))
                {
                    var compilationOptions = (CSharpCompilationOptions)solution.GetRequiredProject(projectId).CompilationOptions!;
                    solution = solution.WithProjectCompilationOptions(projectId, compilationOptions.WithNullableContextOptions(NullableContextOptions.Enable));
                }

                return solution;
            };

        [Fact]
        public async Task EnabledOnNullableEnable()
        {
            var code1 = @"
#nullable enable$$

class Example
{
  string? value;
}
";
            var code2 = @"
class Example2
{
  string value;
}
";
            var code3 = @"
class Example3
{
#nullable enable
  string? value;
#nullable restore
}
";
            var code4 = @"
#nullable disable

class Example4
{
  string value;
}
";

            var fixedCode1 = @"

class Example
{
  string? value;
}
";
            var fixedCode2 = @"
#nullable disable

class Example2
{
  string value;
}
";
            var fixedCode3 = @"
#nullable disable

class Example3
{
#nullable restore
  string? value;
#nullable disable
}
";
            var fixedCode4 = @"
#nullable disable

class Example4
{
  string value;
}
";

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
            var code1 = @"
#nullable enable$$

class Example
{
  string? value;
}
";
            var code2 = @"// File header line 1
// File header line 2

class Example2
{
  string value;
}
";
            var code3 = @"#region File Header
// File header line 1
// File header line 2
#endregion

class Example3
{
  string value;
}
";

            var fixedCode1 = @"

class Example
{
  string? value;
}
";
            var fixedCode2 = @"// File header line 1
// File header line 2

#nullable disable

class Example2
{
  string value;
}
";
            var fixedCode3 = @"#region File Header
// File header line 1
// File header line 2
#endregion

#nullable disable

class Example3
{
  string value;
}
";

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
        public async Task OmitLeadingRestore()
        {
            var code1 = @"
#nullable enable$$

class Example
{
  string? value;
}
";
            var code2 = @"
#nullable enable

class Example2
{
  string? value;
}
";
            var code3 = @"
#nullable enable warnings

class Example3
{
  string value;
}
";
            var code4 = @"
#nullable enable annotations

class Example4
{
  string? value;
}
";

            var fixedCode1 = @"

class Example
{
  string? value;
}
";
            var fixedCode2 = @"

class Example2
{
  string? value;
}
";
            var fixedCode3 = @"
#nullable disable

#nullable restore warnings

class Example3
{
  string value;
}
";
            var fixedCode4 = @"
#nullable disable

#nullable restore annotations

class Example4
{
  string? value;
}
";

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

        [Theory]
        [InlineData(NullableContextOptions.Annotations)]
        [InlineData(NullableContextOptions.Warnings)]
        [InlineData(NullableContextOptions.Enable)]
        public async Task DisabledIfSetInProject(NullableContextOptions nullableContextOptions)
        {
            var code = @"
#nullable enable$$
";

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
                SolutionTransforms =
                {
                    (solution, projectId) =>
                    {
                        var compilationOptions = (CSharpCompilationOptions)solution.GetRequiredProject(projectId).CompilationOptions!;
                        return solution.WithProjectCompilationOptions(projectId, compilationOptions.WithNullableContextOptions(nullableContextOptions));
                    },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task DisabledOnNullableDisable()
        {
            var code = @"
#nullable disable$$
";

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
            }.RunAsync();
        }
    }
}
