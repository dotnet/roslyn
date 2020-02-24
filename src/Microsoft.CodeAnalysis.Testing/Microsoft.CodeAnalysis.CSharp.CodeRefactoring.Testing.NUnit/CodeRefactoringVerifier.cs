// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeRefactorings;

namespace Microsoft.CodeAnalysis.CSharp.Testing.NUnit
{
    public static class CodeRefactoringVerifier
    {
        public static CodeRefactoringVerifier<TCodeRefactoring> Create<TCodeRefactoring>()
            where TCodeRefactoring : CodeRefactoringProvider, new()
        {
            return new CodeRefactoringVerifier<TCodeRefactoring>();
        }
    }
}
