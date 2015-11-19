// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.GenerateMember.GenerateDefaultConstructors;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.GenerateDefaultConstructors
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.GenerateDefaultConstructors), Shared]
    internal class GenerateDefaultConstructorsCodeRefactoringProvider : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var textSpan = context.Span;
            var cancellationToken = context.CancellationToken;

            // TODO: https://github.com/dotnet/roslyn/issues/5778
            // Not supported in REPL for now.
            if (document.Project.IsSubmission)
            {
                return;
            }

            if (document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
            {
                return;
            }

            var service = document.GetLanguageService<IGenerateDefaultConstructorsService>();
            var result = await service.GenerateDefaultConstructorsAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
            if (!result.ContainsChanges)
            {
                return;
            }

            var actions = result.GetCodeRefactoring(cancellationToken).Actions;
            context.RegisterRefactorings(actions);
        }
    }
}
