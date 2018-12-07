// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.MoveToNamespace
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.MoveToNamespace), Shared]
    internal class MoveToNamespaceCodeActionProvider : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var service = context.Document.GetLanguageService<AbstractMoveToNamespaceService>();
            var actions = await service.GetCodeActionsAsync(context.Document, context.Span, context.CancellationToken).ConfigureAwait(false);
            context.RegisterRefactorings(actions);
        }
    }
}
