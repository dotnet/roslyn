// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CodeRefactorings;

namespace Microsoft.CodeAnalysis.GenerateFromMembers
{
    internal abstract class AbstractCodeRefactoringResult
    {
        private readonly CodeRefactoring _codeRefactoring;

        protected AbstractCodeRefactoringResult(CodeRefactoring codeRefactoring)
        {
            _codeRefactoring = codeRefactoring;
        }

        public bool ContainsChanges
        {
            get
            {
                return _codeRefactoring != null;
            }
        }

        public CodeRefactoring GetCodeRefactoring(CancellationToken cancellationToken)
        {
            return _codeRefactoring;
        }
    }
}
