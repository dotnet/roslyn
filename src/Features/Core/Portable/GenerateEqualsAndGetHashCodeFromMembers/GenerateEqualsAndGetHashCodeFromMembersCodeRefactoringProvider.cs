// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.GenerateEqualsAndGetHashCodeFromMembers
{
    // [ExportCodeRefactoringProvider(LanguageNames.CSharp, LanguageNames.VisualBasic, PredefinedCodeRefactoringProviderNames.GenerateEqualsAndGetHashCodeFromMembers)]
    [ExtensionOrder(After = PredefinedCodeRefactoringProviderNames.GenerateConstructorFromMembers,
                    Before = PredefinedCodeRefactoringProviderNames.AddConstructorParametersFromMembers)]
    internal class GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider : CodeRefactoringProvider
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

            var service = document.GetLanguageService<IGenerateEqualsAndGetHashCodeFromMembersService>();
            var actions = await service.GenerateEqualsAndGetHashCodeFromMembersAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
            if (!actions.IsDefault)
            {
                context.RegisterRefactorings(actions);
            }
        }
    }
}