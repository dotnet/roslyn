// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MoveMembers
{
    internal abstract class AbstractMoveMembersCodeAction : CodeActionWithOptions
    {
        private readonly MoveMembersAnalysisResult _analysisResult;
        private readonly Document _document;

        protected AbstractMoveMembersCodeAction(Document document, MoveMembersAnalysisResult analysisResult)
        {
            _document = document;
            _analysisResult = analysisResult;
        }

        public override object GetOptions(CancellationToken cancellationToken)
        {
            var moveMembersOptionService = _document.Project.Solution.Workspace.Services.GetRequiredService<IMoveMembersOptionService>();

            return moveMembersOptionService.GetMoveMembersOptions(
                _document,
                _analysisResult,
                MoveMembersEntryPoint.ExtractInterface)
                ?? MoveMembersOptions.Cancelled;
        }

        protected override async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(object options, CancellationToken cancellationToken)
        {
            var operations = SpecializedCollections.EmptyEnumerable<CodeActionOperation>();

            if (options is MoveMembersOptions moveMembersOptions && !moveMembersOptions.IsCancelled)
            {
                var moveMembersService = _document.GetRequiredLanguageService<AbstractMoveMembersService>();
                var result = await moveMembersService
                        .MoveMembersAsync(_document, moveMembersOptions, cancellationToken).ConfigureAwait(false);

                if (result.Success)
                {
                    operations = new CodeActionOperation[]
                    {
                        new ApplyChangesOperation(result.Solution!),
                        new DocumentNavigationOperation(result.NavigationDocumentId!, position: 0)
                    };
                }
            }

            return operations;
        }
    }
}
