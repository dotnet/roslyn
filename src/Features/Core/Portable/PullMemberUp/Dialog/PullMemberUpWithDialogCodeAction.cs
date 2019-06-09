// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp.Dialog;
using Microsoft.CodeAnalysis.PullMemberUp;

namespace Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp
{
    internal abstract partial class AbstractPullMemberUpRefactoringProvider
    {
        private class PullMemberUpWithDialogCodeAction : CodeActionWithOptions
        {
            /// <summary>
            /// Member which user initially selects. It will be selected initially when the dialog pops up.
            /// </summary>
            private readonly ISymbol _selectedMember;
            private readonly Document _document;
            private readonly IPullMemberUpOptionsService _service;

            public override string Title => FeaturesResources.Pull_members_up_to_base_type;

            public PullMemberUpWithDialogCodeAction(
                Document document,
                ISymbol selectedMember,
                IPullMemberUpOptionsService service)
            {
                _document = document;
                _selectedMember = selectedMember;
                _service = service;
            }

            public override object GetOptions(CancellationToken cancellationToken)
            {
                var pullMemberUpOptionService = _service ?? _document.Project.Solution.Workspace.Services.GetService<IPullMemberUpOptionsService>();
                return pullMemberUpOptionService.GetPullMemberUpOptions(_document, _selectedMember);
            }

            protected async override Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(object options, CancellationToken cancellationToken)
            {
                if (options is PullMembersUpOptions pullMemberUpOptions)
                {
                    var changedSolution = await MembersPuller.PullMembersUpAsync(_document, pullMemberUpOptions, cancellationToken).ConfigureAwait(false);
                    return new[] { new ApplyChangesOperation(changedSolution) };
                }
                else
                {
                    // If user click cancel button, options will be null and hit this branch
                    return Array.Empty<CodeActionOperation>();
                }
            }
        }
    }
}
