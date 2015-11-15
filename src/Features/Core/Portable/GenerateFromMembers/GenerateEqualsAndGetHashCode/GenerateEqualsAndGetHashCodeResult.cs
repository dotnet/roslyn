// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeRefactorings;

namespace Microsoft.CodeAnalysis.GenerateFromMembers.GenerateEqualsAndGetHashCode
{
    internal class GenerateEqualsAndGetHashCodeResult : AbstractCodeRefactoringResult, IGenerateEqualsAndGetHashCodeResult
    {
        public static readonly IGenerateEqualsAndGetHashCodeResult Failure = new GenerateEqualsAndGetHashCodeResult(null);

        public GenerateEqualsAndGetHashCodeResult(CodeRefactoring refactoring)
            : base(refactoring)
        {
        }
    }
}
