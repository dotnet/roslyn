// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeRefactorings;

namespace Microsoft.CodeAnalysis.GenerateFromMembers.GenerateConstructor
{
    internal class GenerateConstructorResult : AbstractCodeRefactoringResult, IGenerateConstructorResult
    {
        public static readonly IGenerateConstructorResult Failure = new GenerateConstructorResult(null);

        public GenerateConstructorResult(CodeRefactoring codeRefactoring)
            : base(codeRefactoring)
        {
        }
    }
}
