// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CodeRefactorings;

namespace Microsoft.CodeAnalysis.IntroduceVariable
{
    internal class IntroduceVariableResult : IIntroduceVariableResult
    {
        public static readonly IIntroduceVariableResult Failure = new IntroduceVariableResult(null);

        private readonly CodeRefactoring codeRefactoring;

        public IntroduceVariableResult(CodeRefactoring codeRefactoring)
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
