// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.IntroduceVariable;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.IntroduceVariable
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.IntroduceVariable), Shared]
    internal class IntroduceVariableCodeRefactoringProvider : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var textSpan = context.Span;
            var cancellationToken = context.CancellationToken;

            if (document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
            {
                return;
            }

            var service = document.GetLanguageService<IIntroduceVariableService>();
            var result = await service.IntroduceVariableAsync(document, textSpan, cancellationToken).ConfigureAwait(false);

            if (!result.ContainsChanges)
            {
                return;
            }

            var actions = result.GetCodeRefactoring(cancellationToken).Actions;
            context.RegisterRefactorings(actions);
        }
    }
}
