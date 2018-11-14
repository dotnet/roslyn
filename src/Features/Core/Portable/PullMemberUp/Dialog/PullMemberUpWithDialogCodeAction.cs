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
        private class PullMemberUpWithDialogCodeAction : CodeActionWithOptions
        {
            private readonly ISymbol _selectedNodeSymbol;

            private readonly Document _contextDocument;

            private readonly IPullMemberUpOptionsService _service;

            private readonly SemanticModel _semanticModel;

            public override string Title => FeaturesResources.DotDotDot;

            internal PullMemberUpWithDialogCodeAction(
                Document document,
                SemanticModel semanticModel,
                ISymbol selectedNodeSymbol,
                AbstractPullMemberUpRefactoringProvider provider)
            {
                _contextDocument = document;
                _semanticModel = semanticModel;
                _selectedNodeSymbol = selectedNodeSymbol;
                _service = provider._pullMemberUpOptionsService;
            }

            public override object GetOptions(CancellationToken cancellationToken)
            {
                var pullMemberUpService = _service ?? _contextDocument.Project.Solution.Workspace.Services.GetService<IPullMemberUpOptionsService>();
                return pullMemberUpService.GetPullTargetAndMembers(_semanticModel, _selectedNodeSymbol);
            }
            
            protected async override Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(object options, CancellationToken cancellationToken)
            {
                if (options is PullMemberDialogResult result && !result.IsCanceled)
                {
                    var generator = new CodeActionAndSolutionGenerator();
                    var changedSolution = await generator.GetSolutionAsync(
                        result.PullMembersAnalysisResult,
                        _contextDocument, cancellationToken).ConfigureAwait(false);
                    var operation = new ApplyChangesOperation(changedSolution);
                    return new CodeActionOperation[] { operation };
                }
                else
                {
                    return new CodeActionOperation[0];
                }
            }
        }
    }
}
