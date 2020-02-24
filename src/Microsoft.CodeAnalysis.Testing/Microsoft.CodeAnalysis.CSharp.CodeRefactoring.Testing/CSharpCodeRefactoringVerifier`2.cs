// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
