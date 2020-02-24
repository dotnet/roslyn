// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Testing.Verifiers;

namespace Microsoft.CodeAnalysis.CSharp.Testing.MSTest
{
    public class CodeRefactoringVerifier<TCodeRefactoring> : CSharpCodeRefactoringVerifier<TCodeRefactoring, MSTestVerifier>
        where TCodeRefactoring : CodeRefactoringProvider, new()
    {
    }
}
