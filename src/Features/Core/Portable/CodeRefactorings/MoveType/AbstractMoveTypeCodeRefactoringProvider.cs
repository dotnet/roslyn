// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.GeneratedCodeRecognition;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CodeRefactorings.MoveType
{
    internal abstract partial class AbstractMoveTypeCodeRefactoringProvider : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var textSpan = context.Span;
            var cancellationToken = context.CancellationToken;

            if (!textSpan.IsEmpty)
            {
                return;
            }

            var workspace = document.Project.Solution.Workspace;
            if (workspace.Kind == WorkspaceKind.MiscellaneousFiles)
            {
                return;
            }

            var generatedCodeRecognitionService = workspace.Services.GetService<IGeneratedCodeRecognitionService>();
            if (generatedCodeRecognitionService.IsGeneratedCode(document))
            {
                return;
            }

            var service = document.GetLanguageService<IMoveTypeService>();
            var refactoring = await service.GetRefactoringAsync(document, textSpan, cancellationToken).ConfigureAwait(false);

            if (refactoring != null)
            {
                context.RegisterRefactorings(refactoring.Actions);
            }
        }
    }
}
