// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.GenerateFromMembers.GenerateConstructor;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.GenerateFromMembers.GenerateConstructor
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.GenerateConstructorFromMembers), Shared]
    [ExtensionOrder(Before = PredefinedCodeRefactoringProviderNames.AddConstructorParametersFromMembers)]
    internal class GenerateConstructorCodeRefactoringProvider : CodeRefactoringProvider
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

            var service = document.GetLanguageService<IGenerateConstructorService>();
            var result = await service.GenerateConstructorAsync(document, textSpan, cancellationToken).ConfigureAwait(false);

            if (!result.ContainsChanges)
            {
                return;
            }

            var actions = result.GetCodeRefactoring(cancellationToken).Actions;
            context.RegisterRefactorings(actions);
        }
    }
}
