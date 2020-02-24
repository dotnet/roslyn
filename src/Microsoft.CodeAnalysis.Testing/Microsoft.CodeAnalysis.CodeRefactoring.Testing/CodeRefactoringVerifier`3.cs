// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    }
}
