// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeRefactorings;

namespace Microsoft.CodeAnalysis.CSharp.Testing.XUnit
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
