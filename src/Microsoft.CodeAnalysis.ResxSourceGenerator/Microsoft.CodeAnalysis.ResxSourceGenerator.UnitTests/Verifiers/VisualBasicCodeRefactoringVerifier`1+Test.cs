// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Microsoft.CodeAnalysis.VisualBasic.Testing;

namespace Microsoft.CodeAnalysis.ResxSourceGenerator.Test
{
    public static partial class VisualBasicCodeRefactoringVerifier<TCodeRefactoring>
        where TCodeRefactoring : CodeRefactoringProvider, new()
    {
        public class Test : VisualBasicCodeRefactoringTest<TCodeRefactoring, XUnitVerifier>
        {
        }
    }
}
