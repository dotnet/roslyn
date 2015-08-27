// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeRefactorings;

namespace Microsoft.CodeAnalysis.GenerateFromMembers.AddConstructorParameters
{
    internal class AddConstructorParametersResult : AbstractCodeRefactoringResult, IAddConstructorParametersResult
    {
        public static readonly IAddConstructorParametersResult Failure = new AddConstructorParametersResult(null);

        public AddConstructorParametersResult(CodeRefactoring codeRefactoring)
            : base(codeRefactoring)
        {
        }
    }
}
