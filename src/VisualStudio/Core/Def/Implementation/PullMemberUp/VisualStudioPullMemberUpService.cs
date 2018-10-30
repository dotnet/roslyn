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
    [ExportWorkspaceService(typeof(IPullMemberUpOptionsService), ServiceLayer.Host), Shared]
    internal class VisualStudioPullMemberUpService : IPullMemberUpOptionsService
    {
        private readonly IGlyphService _glyphService;

        private PullMemberUpViewModel ViewModel { get; set; }

        [ImportingConstructor]
        public VisualStudioPullMemberUpService(IGlyphService glyphService)
        {
            _glyphService = glyphService;
        }

        private List<string> GenerateMessage(AnalysisResult analysisResult)
        {
            var warningMessages = new List<string>();
            foreach (var result in analysisResult.MembersAnalysisResults)
            {
                if (result.ChangeOriginToPublic)
                {
                    warningMessages.Add(string.Format(ServicesVSResources.Change_Member_To_Public, result.Member.Name, analysisResult.Target));
                }

                if (result.ChangeOriginToNonStatic)
                {
                    warningMessages.Add(string.Format(ServicesVSResources.Change_Member_To_NonStatic, result.Member.Name, analysisResult.Target));
                }
            }
            if (analysisResult.ChangeTargetAbstract)
            {
                warningMessages.Add(string.Format(ServicesVSResources.Change_Target_To_Abstract, analysisResult.Target.Name));
            }
            return warningMessages;
        }

        public bool CreateWarningDialog(AnalysisResult analysisResult)
        {
            var viewModel = new PullMemberUpWarningViewModel(GenerateMessage(analysisResult));
            var dialog = new PullMemberUpDialogWarning(viewModel);
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
                var restoredDialog = new PullMemberUpDialog(ViewModel);
                return CreateResult(ViewModel, restoredDialog.ShowModal());
            }
            else
            {
                var baseTypeTree = MemberSymbolViewModelGraphNode.CreateInheritanceGraph(selectedOwnerSymbol.ContainingType, _glyphService);
                ViewModel = new PullMemberUpViewModel(members.ToList(), baseTypeTree.Neighbours, selectedOwnerSymbol, _glyphService, lazyDependentsMap);
                var dialog = new PullMemberUpDialog(ViewModel);
                return CreateResult(ViewModel, dialog.ShowModal());
            }
        }

        public void ResetSession()
        {
            ViewModel = null;
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
