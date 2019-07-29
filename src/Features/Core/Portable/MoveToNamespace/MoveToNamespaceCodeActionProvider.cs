// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.MoveToNamespace
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.MoveToNamespace), Shared]
    [ExtensionOrder(After = PredefinedCodeRefactoringProviderNames.SyncNamespace)]
    [ExtensionOrder(After = PredefinedCodeRefactoringProviderNames.MoveTypeToFile)]
    internal class MoveToNamespaceCodeActionProvider : CodeRefactoringProvider
    {
        [ImportingConstructor]
        public MoveToNamespaceCodeActionProvider()
        {
        }

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, textSpan, cancellationToken) = context;
            var moveToNamespaceService = document.GetLanguageService<IMoveToNamespaceService>();
            var actions = await moveToNamespaceService.GetCodeActionsAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
            context.RegisterRefactorings(actions);
        }
    }
}
