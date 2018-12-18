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

        public PullMembersUpAnalysisResult GetPullMemberUpOptions(ISymbol selectedMember, Document document)
        {
            var membersInType = selectedMember.ContainingType.GetMembers().
                WhereAsArray(member => MemberAndDestinationValidator.IsMemeberValid(member));
            var memberViewModels = membersInType.SelectAsArray(member => new PullMemberUpSymbolViewModel(selectedMember, member, _glyphService));

            using (var cts = new CancellationTokenSource())
            {
                var baseTypeRootViewModel = BaseTypeTreeNodeViewModel.CreateBaseTypeTree(selectedMember.ContainingType, document.Project.Solution, _glyphService, cts.Token);
                var dependentsBuilder = new SymbolDependentsBuilder(membersInType, document);
                var dependentsMap = dependentsBuilder.CreateDependentsMap(cts.Token);

                // Finding the dependents of all members will be expensive, so start an new background thread calculates it.
                var dependentsTask = Task.Run(async () =>
                {
                    foreach (var dependents in dependentsMap.Values)
                    {
                        await dependents.ConfigureAwait(false);
                    }
                }, cts.Token);

                var viewModel = new PullMemberUpViewModel(baseTypeRootViewModel.BaseTypeNodes, memberViewModels, dependentsMap, _waitIndicator, cts.Token);
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
