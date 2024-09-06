// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Testing;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
{
    public static partial class CSharpCodeRefactoringVerifier<TCodeRefactoring>
        where TCodeRefactoring : CodeRefactoringProvider, new()
    {
        /// <inheritdoc cref="CodeRefactoringVerifier{TCodeRefactoring, TTest, TVerifier}.VerifyRefactoringAsync(string, string)"/>
        public static Task VerifyRefactoringAsync(
            [StringSyntax("C#-Test")] string source)
        {
            return VerifyRefactoringAsync(source, DiagnosticResult.EmptyDiagnosticResults, source);
        }

        /// <inheritdoc cref="CodeRefactoringVerifier{TCodeRefactoring, TTest, TVerifier}.VerifyRefactoringAsync(string, string)"/>
        public static Task VerifyRefactoringAsync(
            [StringSyntax("C#-Test")] string source, [StringSyntax("C#-Test")] string fixedSource)
        {
            return VerifyRefactoringAsync(source, DiagnosticResult.EmptyDiagnosticResults, fixedSource);
        }

        /// <inheritdoc cref="CodeRefactoringVerifier{TCodeRefactoring, TTest, TVerifier}.VerifyRefactoringAsync(string, DiagnosticResult, string)"/>
        public static Task VerifyRefactoringAsync([StringSyntax("C#-Test")] string source, DiagnosticResult expected, [StringSyntax("C#-Test")] string fixedSource)
        {
            return VerifyRefactoringAsync(source, [expected], fixedSource);
        }

        /// <inheritdoc cref="CodeRefactoringVerifier{TCodeRefactoring, TTest, TVerifier}.VerifyRefactoringAsync(string, DiagnosticResult[], string)"/>
        public static Task VerifyRefactoringAsync([StringSyntax("C#-Test")] string source, DiagnosticResult[] expected, [StringSyntax("C#-Test")] string fixedSource)
        {
            var test = new Test
            {
                TestCode = source,
                FixedCode = fixedSource
            };

            test.ExpectedDiagnostics.AddRange(expected);
            return test.RunAsync(CancellationToken.None);
        }
    }
}
