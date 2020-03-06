// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;

namespace Microsoft.CodeAnalysis.Testing
{
    /// <summary>
    /// A default verifier for code refactorings.
    /// </summary>
    /// <typeparam name="TCodeRefactoring">The <see cref="CodeRefactoringProvider"/> to test.</typeparam>
    /// <typeparam name="TTest">The test implementation to use.</typeparam>
    /// <typeparam name="TVerifier">The type of verifier to use.</typeparam>
    public class CodeRefactoringVerifier<TCodeRefactoring, TTest, TVerifier>
           where TCodeRefactoring : CodeRefactoringProvider, new()
           where TTest : CodeRefactoringTest<TVerifier>, new()
           where TVerifier : IVerifier, new()
    {
        /// <summary>
        /// Verifies the application of the code refactoring produces the expected result.
        /// </summary>
        /// <param name="source">The source text to test. Any diagnostics are defined in markup.</param>
        /// <param name="fixedSource">The expected fixed source text. Any remaining diagnostics are defined in markup.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static Task VerifyRefactoringAsync(string source, string fixedSource)
            => VerifyRefactoringAsync(source, DiagnosticResult.EmptyDiagnosticResults, fixedSource);

        /// <summary>
        /// Verifies the application of the code refactoring produces the expected result.
        /// </summary>
        /// <param name="source">The source text to test, which may include markup syntax.</param>
        /// <param name="expected">The expected diagnostic. This diagnostic is in addition to any diagnostics defined in
        /// markup.</param>
        /// <param name="fixedSource">The expected fixed source text. Any remaining diagnostics are defined in markup.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static Task VerifyRefactoringAsync(string source, DiagnosticResult expected, string fixedSource)
            => VerifyRefactoringAsync(source, new[] { expected }, fixedSource);

        /// <summary>
        /// Verifies the application of the code refactoring produces the expected result.
        /// </summary>
        /// <param name="source">The source text to test, which may include markup syntax.</param>
        /// <param name="expected">The expected diagnostics. These diagnostics are in addition to any diagnostics
        /// defined in markup.</param>
        /// <param name="fixedSource">The expected fixed source text. Any remaining diagnostics are defined in markup.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static Task VerifyRefactoringAsync(string source, DiagnosticResult[] expected, string fixedSource)
        {
            var test = new TTest
            {
                TestCode = source,
                FixedCode = fixedSource,
            };

            test.ExpectedDiagnostics.AddRange(expected);
            return test.RunAsync(CancellationToken.None);
        }
    }
}
