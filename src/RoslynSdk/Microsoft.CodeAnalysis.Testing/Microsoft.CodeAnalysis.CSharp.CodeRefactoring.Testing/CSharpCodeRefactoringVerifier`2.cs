// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Testing;

namespace Microsoft.CodeAnalysis.CSharp.Testing
{
    public class CSharpCodeRefactoringVerifier<TCodeRefactoring, TVerifier> : CodeRefactoringVerifier<TCodeRefactoring, CSharpCodeRefactoringTest<TCodeRefactoring, TVerifier>, TVerifier>
        where TCodeRefactoring : CodeRefactoringProvider, new()
        where TVerifier : IVerifier, new()
    {
    }
}
