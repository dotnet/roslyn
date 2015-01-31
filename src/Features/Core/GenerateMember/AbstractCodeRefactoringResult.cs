// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CodeRefactorings;

namespace Microsoft.CodeAnalysis.GenerateMember
{
    internal abstract class AbstractCodeRefactoringResult
    {
        private readonly CodeRefactoring codeRefactoring;

        protected AbstractCodeRefactoringResult(CodeRefactoring codeRefactoring)
        {
            this.codeRefactoring = codeRefactoring;
        }

        public bool ContainsChanges
        {
            get
            {
                return this.codeRefactoring != null;
            }
        }

        public CodeRefactoring GetCodeRefactoring(CancellationToken cancellationToken)
        {
            return this.codeRefactoring;
        }
    }
}
