// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

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
        internal class PullMemberUpWithDialogCodeAction : CodeActionWithOptions
        {
            private readonly ISymbol _selectedMember;
            private readonly Document _document;
            private readonly IPullMemberUpOptionsService _service;

            public override string Title => string.Format(FeaturesResources.More_option_to_pull_0_up, _selectedMember.ToDisplayString());

            internal PullMemberUpWithDialogCodeAction(
                Document document,
                ISymbol selectedMember,
                AbstractPullMemberUpRefactoringProvider provider)
            {
                _document = document;
                _selectedMember = selectedMember;
                _service = provider._service;
            }

            public override object GetOptions(CancellationToken cancellationToken)
            {
                var pullMemberUpOptionService = _service ?? _document.Project.Solution.Workspace.Services.GetService<IPullMemberUpOptionsService>();
                return pullMemberUpOptionService.GetPullMemberUpOptions(_document, _selectedMember);
            }
            
            protected async override Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(object options, CancellationToken cancellationToken)
            {
                if (options is PullMembersUpAnalysisResult result)
                {
                    var changedSolution = await MembersPuller.Instance.PullMembersUpAsync(_document, result, cancellationToken).ConfigureAwait(false);
                    return new CodeActionOperation[1] { new ApplyChangesOperation(changedSolution) };
                }
                else
                {
                    return new CodeActionOperation[0];
                }
            }
        }
    }
}
