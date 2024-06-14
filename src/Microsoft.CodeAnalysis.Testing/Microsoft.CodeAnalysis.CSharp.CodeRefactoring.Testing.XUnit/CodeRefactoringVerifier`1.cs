// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;

namespace Microsoft.CodeAnalysis.CSharp.Testing.XUnit
{
    [Obsolete(ObsoleteMessages.FrameworkPackages)]
    public class CodeRefactoringVerifier<TCodeRefactoring> : CSharpCodeRefactoringVerifier<TCodeRefactoring, XUnitVerifier>
        where TCodeRefactoring : CodeRefactoringProvider, new()
    {
    }
}
