// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PullMemberUp;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ExtractClass
{
    internal abstract class AbstractExtractClassRefactoringProvider : CodeRefactoringProvider
    {
        private readonly IExtractClassOptionsService? _optionsService;

        public AbstractExtractClassRefactoringProvider(IExtractClassOptionsService? service)
        {
            _optionsService = service;
        }

        protected abstract Task<SyntaxNode?> GetSelectedNodeAsync(CodeRefactoringContext context);
        protected abstract Task<SyntaxNode?> GetSelectedClassDeclarationAsync(CodeRefactoringContext context);

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            // For simplicity if we can't add a document the don't supply this refactoring. Not checking this results in known
            // cases that won't work because the refactoring may try to add a document. There's non-trivial
            // work to support a user interaction that makes sense for those cases. 
            // See: https://github.com/dotnet/roslyn/issues/50868
            if (!context.Document.Project.Solution.Workspace.CanApplyChange(ApplyChangesKind.AddDocument))
            {
                return;
            }

            var optionsService = _optionsService ?? context.Document.Project.Solution.Workspace.Services.GetService<IExtractClassOptionsService>();
            if (optionsService is null)
            {
                return;
            }

            // If we register the action on a class node, no need to find selected members. Just allow
            // the action to be invoked with the dialog and no selected members
            var action = await TryGetClassActionAsync(context, optionsService).ConfigureAwait(false)
                ?? await TryGetMemberActionAsync(context, optionsService).ConfigureAwait(false);

            if (action != null)
            {
                context.RegisterRefactoring(action, action.Span);
            }
        }

        private async Task<ExtractClassWithDialogCodeAction?> TryGetMemberActionAsync(CodeRefactoringContext context, IExtractClassOptionsService optionsService)
        {
            var selectedMemberNode = await GetSelectedNodeAsync(context).ConfigureAwait(false);
            if (selectedMemberNode is null)
            {
                return null;
            }

            var (document, span, cancellationToken) = context;
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var selectedMember = semanticModel.GetDeclaredSymbol(selectedMemberNode, cancellationToken);
            if (selectedMember is null || selectedMember.ContainingType is null)
            {
                return null;
            }

            // Use same logic as pull members up for determining if a selected member
            // is valid to be moved into a base
            if (!MemberAndDestinationValidator.IsMemberValid(selectedMember))
            {
                return null;
            }

            var containingType = selectedMember.ContainingType;

            // Can't extract to a new type if there's already a base. Maybe
            // in the future we could inject a new type inbetween base and
            // current
            if (containingType.BaseType?.SpecialType != SpecialType.System_Object)
            {
                return null;
            }

            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var containingTypeDeclarationNode = selectedMemberNode.FirstAncestorOrSelf<SyntaxNode>(syntaxFacts.IsTypeDeclaration);

            return new ExtractClassWithDialogCodeAction(document, span, optionsService, containingType, containingTypeDeclarationNode!, context.Options, selectedMember);
        }

        private async Task<ExtractClassWithDialogCodeAction?> TryGetClassActionAsync(CodeRefactoringContext context, IExtractClassOptionsService optionsService)
        {
            var selectedClassNode = await GetSelectedClassDeclarationAsync(context).ConfigureAwait(false);
            if (selectedClassNode is null)
            {
                return null;
            }

            var (document, span, cancellationToken) = context;

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var originalType = semanticModel.GetDeclaredSymbol(selectedClassNode, cancellationToken) as INamedTypeSymbol;

            if (originalType is null)
            {
                return null;
            }

            return new ExtractClassWithDialogCodeAction(document, span, optionsService, originalType, selectedClassNode, context.Options);
        }
    }
}
