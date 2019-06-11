// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;

namespace Microsoft.CodeAnalysis.MoveToNamespace
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.MoveToNamespace), Shared]
    [ExtensionOrder(After = PredefinedCodeRefactoringProviderNames.SyncNamespace)]
    [ExtensionOrder(After = PredefinedCodeRefactoringProviderNames.MoveTypeToFile)]
    internal class MoveToNamespaceCodeActionProvider : CodeRefactoringProvider
    {
        private readonly IMoveToNamespaceService _moveToNamespaceService;

        [ImportingConstructor]
        public MoveToNamespaceCodeActionProvider(IMoveToNamespaceService moveToNamespaceService)
        {
            _moveToNamespaceService = moveToNamespaceService;
        }

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var actions = await _moveToNamespaceService.GetCodeActionsAsync(context.Document, context.Span, context.CancellationToken).ConfigureAwait(false);
            context.RegisterRefactorings(actions);
        }
    }
}
