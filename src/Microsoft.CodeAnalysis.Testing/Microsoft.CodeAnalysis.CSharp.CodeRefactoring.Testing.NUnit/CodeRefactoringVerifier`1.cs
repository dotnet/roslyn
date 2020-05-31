// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Testing.Verifiers;

namespace Microsoft.CodeAnalysis.CSharp.Testing.NUnit
{
    public class CodeRefactoringVerifier<TCodeRefactoring> : CSharpCodeRefactoringVerifier<TCodeRefactoring, NUnitVerifier>
        where TCodeRefactoring : CodeRefactoringProvider, new()
    {
    }
}
