// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.GenerateFromMembers.GenerateConstructorFromMembers;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CodeRefactorings.GenerateFromMembers.GenerateConstructorFromMembers
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, LanguageNames.VisualBasic,
        Name = PredefinedCodeRefactoringProviderNames.GenerateConstructorFromMembers), Shared]
    [ExtensionOrder(Before = PredefinedCodeRefactoringProviderNames.AddConstructorParametersFromMembers)]
    internal class GenerateConstructorFromMembersCodeRefactoringProvider : CodeRefactoringProvider
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

            var service = document.GetLanguageService<IGenerateConstructorFromMembersService>();
            var actions = await service.GenerateConstructorFromMembersAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
            if (!actions.IsDefault)
            {
                context.RegisterRefactorings(actions);
            }
        }
    }
}