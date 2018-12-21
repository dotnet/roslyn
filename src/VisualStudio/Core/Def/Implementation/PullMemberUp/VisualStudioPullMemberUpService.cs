// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp.Dialog;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PullMemberUp;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.LanguageServices.Implementation.PullMemberUp.MainDialog;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.PullMemberUp
{
    [ExportWorkspaceService(typeof(IPullMemberUpOptionsService), ServiceLayer.Host), Shared]
    internal class VisualStudioPullMemberUpService : IPullMemberUpOptionsService
    {
        private readonly IGlyphService _glyphService;
        private readonly IWaitIndicator _waitIndicator;

        [ImportingConstructor]
        public VisualStudioPullMemberUpService(IGlyphService glyphService, IWaitIndicator waitIndicator)
        {
            _glyphService = glyphService;
            _waitIndicator = waitIndicator;
        }

        public PullMembersUpAnalysisResult GetPullMemberUpOptions(Document document, ISymbol selectedMember)
        {
            var membersInType = selectedMember.ContainingType.GetMembers().
                WhereAsArray(member => MemberAndDestinationValidator.IsMemberValid(member));
            var memberViewModels = membersInType.
                SelectAsArray(member => 
                    new PullMemberUpSymbolViewModel(_glyphService, member)
                    {
                        // The member user selected will be checked at the begining.
                        IsChecked = SymbolEquivalenceComparer.Instance.Equals(selectedMember, member),
                        MakeAbstract = false,
                        IsCheckable = true
                    });

            using (var cts = new CancellationTokenSource())
            {
                var baseTypeRootViewModel = BaseTypeTreeNodeViewModel.CreateBaseTypeTree(
                    _glyphService,
                    document.Project.Solution,
                    selectedMember.ContainingType,
                    cts.Token).BaseTypeNodes;
                var dependentsBuilder = new SymbolDependentsBuilder(document, membersInType);
                var dependentsMap = dependentsBuilder.CreateDependentsMap(cts.Token);

                // Finding the dependents of all members will be expensive, so start an new background thread calculates it.
                var dependentsTask = Task.Run(async () =>
                {
                    foreach (var dependents in dependentsMap.Values)
                    {
                        await dependents.ConfigureAwait(false);
                    }
                }, cts.Token);

                var viewModel = new PullMemberUpDialogViewModel(_waitIndicator,  memberViewModels, baseTypeRootViewModel, dependentsMap);
                var dialog = new PullMemberUpDialog(viewModel);
                var result = dialog.ShowModal();

                // Dialog has finshed its work, cancel finding dependents task.
                cts.Cancel();

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
