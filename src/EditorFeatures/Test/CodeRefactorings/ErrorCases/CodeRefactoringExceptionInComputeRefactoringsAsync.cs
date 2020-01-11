// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeRefactoringService.ErrorCases
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = "Test")]
    [Shared]
    [PartNotDiscoverable]
    internal class ExceptionInComputeRefactoringsAsync : CodeRefactoringProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ExceptionInComputeRefactoringsAsync()
        {
        }

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            await Task.Yield();
            throw new Exception($"Exception thrown from ComputeRefactoringsAsync in {nameof(ExceptionInCodeActions)}");
        }
    }
}
