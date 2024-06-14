// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Testing;

namespace Microsoft.CodeAnalysis.CSharp.Testing.NUnit
{
    [Obsolete(ObsoleteMessages.FrameworkPackages)]
    public static class CodeRefactoringVerifier
    {
        public static CodeRefactoringVerifier<TCodeRefactoring> Create<TCodeRefactoring>()
            where TCodeRefactoring : CodeRefactoringProvider, new()
        {
            return new CodeRefactoringVerifier<TCodeRefactoring>();
        }
    }
}
