// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeRefactoringService.ErrorCases
{
    internal class ExceptionInCodeActions : CodeRefactoringProvider
    {
        public override Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            throw new Exception($"Exception thrown from ComputeRefactoringsAsync in {nameof(ExceptionInCodeActions)}");
        }
    }
}
