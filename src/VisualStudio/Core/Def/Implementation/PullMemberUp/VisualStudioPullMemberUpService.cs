// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp.Dialog;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PullMemberUp;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.LanguageServices.Implementation.PullMemberUp.MainDialog;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.PullMemberUp
{
    [ExportWorkspaceService(typeof(IPullMemberUpOptionsService), ServiceLayer.Host), Shared]
    internal class VisualStudioPullMemberUpService : IPullMemberUpOptionsService
    {
        private readonly IGlyphService _glyphService;

        [ImportingConstructor]
        public VisualStudioPullMemberUpService(IGlyphService glyphService)
        {
            _glyphService = glyphService;
        }

        public PullMembersUpAnalysisResult GetPullMemberUpAnalysisResultFromDialogBox(ISymbol selectedMember, Document document)
        {
            var baseTypeRootViewModel = BaseTypeTreeNodeViewModel.CreateBaseTypeTree(selectedMember.ContainingType, _glyphService);
            var membersInType = selectedMember.ContainingType.GetMembers().
                WhereAsArray(member => MemberAndDestinationValidator.IsMemeberValid(member));
            var memberViewModels = membersInType.SelectAsArray(member => new PullUpMemberSymbolViewModel(member, _glyphService)
                {
                    // The member user selected will be checked at the begining.
                    IsChecked = member.Equals(selectedMember),
                    MakeAbstract = false,
                    IsMakeAbstractCheckable = member.Kind != SymbolKind.Field && !member.IsAbstract,
                    IsCheckable = true
                });

            var dependentsMap = SymbolDependentsBuilder.CreateDependentsMap(document, membersInType);
            using (var cts = new CancellationTokenSource())
            {
                // Finding the dependents of all members will be expensive, so start an new background thread calculates it.
                var dependentsTask = Task.Run(async () =>
                {
                    foreach (var asyncLazy in dependentsMap.Values)
                    {
                        await asyncLazy.GetValueAsync(cts.Token).ConfigureAwait(false);
                    }
                });

                var viewModel = new PullMemberUpViewModel(baseTypeRootViewModel.BaseTypeNodes, memberViewModels, dependentsMap, cts);
                var dialog = new PullMemberUpDialog(viewModel);
                var result = dialog.ShowModal();

                // Dialog UI has finshed its work, if the finding dependents task still not finished, cancel it.
                if (!dependentsTask.IsCompleted)
                {
                    cts.Cancel();
                }

                if (result.GetValueOrDefault())
                {
                    return dialog.ViewModel.CreateAnaysisResult();
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
