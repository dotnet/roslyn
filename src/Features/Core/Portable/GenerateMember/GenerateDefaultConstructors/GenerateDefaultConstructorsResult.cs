// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeRefactorings;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateDefaultConstructors
{
    internal class GenerateDefaultConstructorsResult : AbstractCodeRefactoringResult, IGenerateDefaultConstructorsResult
    {
        public static readonly IGenerateDefaultConstructorsResult Failure = new GenerateDefaultConstructorsResult(null);

        internal GenerateDefaultConstructorsResult(CodeRefactoring codeRefactoring)
            : base(codeRefactoring)
        {
        }
    }
}
