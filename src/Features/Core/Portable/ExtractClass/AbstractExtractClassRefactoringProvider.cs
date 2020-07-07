// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable 

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

        protected abstract Task<SyntaxNode> GetSelectedNodeAsync(CodeRefactoringContext context);

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var optionsService = _optionsService ?? context.Document.Project.Solution.Workspace.Services.GetService<IExtractClassOptionsService>();
            if (optionsService is null)
            {
                return;
            }

            // Currently support to pull field, method, event, property and indexer up,
            // constructor, operator and finalizer are excluded.
            var (document, span, cancellationToken) = context;

            var selectedMemberNode = await GetSelectedNodeAsync(context).ConfigureAwait(false);
            if (selectedMemberNode is null)
            {
                return;
            }

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

            context.RegisterRefactoring(new ExtractClassWithDialogCodeAction(document, selectedMember, span, optionsService), selectedMemberNode.Span);
        }
    }
}
