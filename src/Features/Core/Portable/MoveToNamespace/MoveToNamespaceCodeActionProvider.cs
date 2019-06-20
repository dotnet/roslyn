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
            var moveToNamespaceService = context.Document.GetLanguageService<IMoveToNamespaceService>();
            var actions = await moveToNamespaceService.GetCodeActionsAsync(context.Document, context.Span, context.CancellationToken).ConfigureAwait(false);
            context.RegisterRefactorings(actions);
        }
    }
}
