// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp.Dialog;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Language.Intellisense;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.PullMemberUp
{
    [ExportWorkspaceService(typeof(IPullMemberUpDialogService), ServiceLayer.Host), Shared]
    internal class VisualStudioPullMemberUpService : IPullMemberUpDialogService
    {
        private readonly IGlyphService _glyphService;

        private PullMemberUpViewModel ViewModel { get; set; }

        [ImportingConstructor]
        public VisualStudioPullMemberUpService(IGlyphService glyphService)
        {
            _glyphService = glyphService;
        }

        public bool CreateWarningDialog(AnalysisResult analysisResult)
        {
            var warningMessages = new List<string>();


            var viewModel = new PullMemberUpWarningViewModel(warningMessages);
            var dialog = new PullMemberUpDialogWarningxaml(viewModel);
            var result = dialog.ShowModal();
            if (result.GetValueOrDefault())
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public PullMemberDialogResult GetPullTargetAndMembers(
            ISymbol selectedOwnerSymbol,
            IEnumerable<ISymbol> members,
            Dictionary<ISymbol, Lazy<List<ISymbol>>> lazyDependentsMap)
        {
            if (ViewModel != null)
            {
                var restoredDialog = new PullMemberUpDialogxaml(ViewModel);
                return CreateResult(ViewModel, restoredDialog.ShowModal());
            }
            else
            {
                var baseTypeTree = MemberSymbolViewModelGraphNode.CreateInheritanceGraph(selectedOwnerSymbol.ContainingType, _glyphService);
                ViewModel = new PullMemberUpViewModel(members.ToList(), baseTypeTree.Neighbours, selectedOwnerSymbol, _glyphService, lazyDependentsMap);
                var dialog = new PullMemberUpDialogxaml(ViewModel);
                return CreateResult(ViewModel, dialog.ShowModal());
            }
        }

        private PullMemberDialogResult CreateResult(PullMemberUpViewModel viewModel, bool? showModal)
        {
            if (showModal.GetValueOrDefault())
            {
                return new PullMemberDialogResult(
                    ViewModel.SelectedMembersContainer.
                    Where(memberSymbolView => memberSymbolView.IsChecked).
                    Select(memberSymbolView => (memberSymbolView.MemberSymbol, memberSymbolView.IsAbstract)),
                    ViewModel.SelectedTarget.MemberSymbolViewModel.MemberSymbol as INamedTypeSymbol);
            }
            else
            {
                return PullMemberDialogResult.CanceledResult;
            }
        }
    }
}
