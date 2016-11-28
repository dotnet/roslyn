// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeRefactorings;

namespace Microsoft.CodeAnalysis.GenerateFromMembers.GenerateConstructorFromMembers
{
    internal class GenerateConstructorFromMembersResult : AbstractCodeRefactoringResult, IGenerateConstructorFromMembersResult
    {
        public static readonly IGenerateConstructorFromMembersResult Failure = new GenerateConstructorFromMembersResult(null);

        public GenerateConstructorFromMembersResult(CodeRefactoring codeRefactoring)
            : base(codeRefactoring)
        {
        }
    }
}
