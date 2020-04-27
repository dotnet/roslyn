// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.  

#nullable enable

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.MoveMembers;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp
{
    internal partial class PullMemberUpRefactoringProvider
    {
        private class PullMemberUpWithDialogCodeAction : CodeActionWithOptions
        {
            private readonly MoveMembersAnalysisResult _analysisResult;
            private readonly Document _document;
            private readonly IMoveMembersOptionService? _service;

            public override string Title => FeaturesResources.Pull_members_up_to_base_type;

            public PullMemberUpWithDialogCodeAction(
                Document document,
                MoveMembersAnalysisResult analysisResult,
                IMoveMembersOptionService? service)
            {
                _document = document;
                _analysisResult = analysisResult;
                _service = service;
            }

            public override object GetOptions(CancellationToken cancellationToken)
            {
                var moveMembersOptionService = _service ?? _document.Project.Solution.Workspace.Services.GetRequiredService<IMoveMembersOptionService>();
                return moveMembersOptionService.GetMoveMembersOptions(_document, _analysisResult, MoveMembersEntryPoint.PullMembersUp)
                    ?? MoveMembersOptions.Cancelled;
            }

            protected async override Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(object options, CancellationToken cancellationToken)
            {
                if (options is MoveMembersOptions pullMemberUpOptions)
                {
                    var moveMembersService = _document.GetRequiredLanguageService<AbstractMoveMembersService>();
                    var result = await moveMembersService.MoveMembersAsync(_document, pullMemberUpOptions, cancellationToken).ConfigureAwait(false);

                    Debug.Assert(result.Success);

                    return new[] { new ApplyChangesOperation(result.Solution!) };
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
