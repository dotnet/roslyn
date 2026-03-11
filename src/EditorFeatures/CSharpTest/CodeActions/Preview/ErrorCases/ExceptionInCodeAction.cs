// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings.ErrorCases;

internal sealed class ExceptionInCodeAction : CodeRefactoringProvider
{
    public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
    {
        context.RegisterRefactoring(new ExceptionCodeAction(), context.Span);
    }

    internal sealed class ExceptionCodeAction : CodeAction
    {
        public override string Title
        {
            get
            {
                throw new Exception($"Exception thrown from get_Title in {nameof(ExceptionCodeAction)}");
            }
        }

        public override string EquivalenceKey
        {
            get
            {
                throw new Exception($"Exception thrown from get_EquivalenceKey in {nameof(ExceptionCodeAction)}");
            }
        }

        protected override Task<IEnumerable<CodeActionOperation>> ComputePreviewOperationsAsync(CancellationToken cancellationToken)
            => throw new Exception($"Exception thrown from ComputePreviewOperationsAsync in {nameof(ExceptionCodeAction)}");

        protected override Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(CancellationToken cancellationToken)
            => throw new Exception($"Exception thrown from ComputeOperationsAsync in {nameof(ExceptionCodeAction)}");

        protected override Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            => throw new Exception($"Exception thrown from GetChangedDocumentAsync in {nameof(ExceptionCodeAction)}");

        protected override Task<Solution> GetChangedSolutionAsync(IProgress<CodeAnalysisProgress> progress, CancellationToken cancellationToken)
            => throw new Exception($"Exception thrown from GetChangedSolutionAsync in {nameof(ExceptionCodeAction)}");

        protected override Task<Document> PostProcessChangesAsync(Document document, CancellationToken cancellationToken)
            => throw new Exception($"Exception thrown from PostProcessChangesAsync in {nameof(ExceptionCodeAction)}");

        public override int GetHashCode()
            => throw new Exception($"Exception thrown from GetHashCode in {nameof(ExceptionCodeAction)}");

        public override bool Equals(object obj)
            => throw new Exception($"Exception thrown from Equals in {nameof(ExceptionCodeAction)}");
    }
}
