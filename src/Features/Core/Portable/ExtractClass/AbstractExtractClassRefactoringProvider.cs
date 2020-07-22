// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable 

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.PullMemberUp;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ExtractClass
{
    internal abstract class AbstractExtractClassRefactoringProvider : CodeRefactoringProvider
    {
        public AbstractExtractClassRefactoringProvider()
        {
        }

        public AbstractExtractClassRefactoringProvider(IExtractClassOptionsService service)
        {
            _optionsService = service;
        }

        private readonly IExtractClassOptionsService? _optionsService;

        protected abstract Task<SyntaxNode?> GetSelectedNodeAsync(CodeRefactoringContext context);
        protected abstract Task<SyntaxNode?> GetSelectedClassDeclarationAsync(CodeRefactoringContext context);

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var optionsService = _optionsService ?? context.Document.Project.Solution.Workspace.Services.GetService<IExtractClassOptionsService>();
            if (optionsService is null)
            {
                return;
            }

            // If we register the action on a class node, no need to find selected members. Just allow
            // the action to be invoked with the dialog 
            if (await TryRegisterClassActionAsync(context, optionsService).ConfigureAwait(false))
            {
                return;
            }

            await RegisterMemberActionAsync(context, optionsService).ConfigureAwait(false);
        }

        private async Task RegisterMemberActionAsync(CodeRefactoringContext context, IExtractClassOptionsService optionsService)
        {
            var selectedMemberNode = await GetSelectedNodeAsync(context).ConfigureAwait(false);
            if (selectedMemberNode is null)
            {
                return;
            }

            var (document, span, cancellationToken) = context;
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var selectedMember = semanticModel.GetDeclaredSymbol(selectedMemberNode);
            if (selectedMember is null || selectedMember.ContainingType is null)
            {
                return;
            }

            // Use same logic as pull members up for determining if a selected member
            // is valid to be moved into a base
            if (!MemberAndDestinationValidator.IsMemberValid(selectedMember))
            {
                return;
            }

            var containingType = selectedMember.ContainingType;

            // Can't extract to a new type if there's already a base. Maybe
            // in the future we could inject a new type inbetween base and
            // current
            if (containingType.BaseType?.SpecialType != SpecialType.System_Object)
            {
                return;
            }

            context.RegisterRefactoring(new ExtractClassWithDialogCodeAction(document, span, optionsService, selectedMember.ContainingType, selectedMember), selectedMemberNode.Span);
        }

        private async Task<bool> TryRegisterClassActionAsync(CodeRefactoringContext context, IExtractClassOptionsService optionsService)
        {
            var selectedClassNode = await GetSelectedClassDeclarationAsync(context).ConfigureAwait(false);
            if (selectedClassNode is null)
            {
                return false;
            }

            var (document, span, cancellationToken) = context;

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var originalSymbol = semanticModel.GetDeclaredSymbol(selectedClassNode, cancellationToken);

            if (originalSymbol is INamedTypeSymbol originalType)
            {
                context.RegisterRefactoring(new ExtractClassWithDialogCodeAction(document, span, optionsService, originalType));
                return true;
            }

            return false;
        }
    }
}
