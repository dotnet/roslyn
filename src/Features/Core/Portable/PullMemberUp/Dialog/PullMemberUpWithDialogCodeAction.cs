﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.  

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp.Dialog;
using Microsoft.CodeAnalysis.PullMemberUp;
using Roslyn.Utilities;

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
                var pullMemberUpOptionService = _service ?? _document.Project.Solution.Workspace.Services.GetRequiredService<IPullMemberUpOptionsService>();
                return pullMemberUpOptionService.GetPullMemberUpOptions(_document, _selectedMember);
            }

            protected override async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(object options, CancellationToken cancellationToken)
            {
                if (options is PullMembersUpOptions pullMemberUpOptions)
                {
                    var changedSolution = await MembersPuller.PullMembersUpAsync(_document, pullMemberUpOptions, cancellationToken).ConfigureAwait(false);
                    return new[] { new ApplyChangesOperation(changedSolution) };
                }
                else
                {
                    // If user click cancel button, options will be null and hit this branch
                    return SpecializedCollections.EmptyEnumerable<CodeActionOperation>();
                }
            }
        }
    }
}
