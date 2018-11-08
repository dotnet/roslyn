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

        private AnalysisResult MembersInfo { get; set; }

        [ImportingConstructor]
        public VisualStudioPullMemberUpService(IGlyphService glyphService)
        {
            _glyphService = glyphService;
        }

        public PullMemberDialogResult GetPullTargetAndMembers(
            ISymbol selectedOwnerSymbol,
            IEnumerable<ISymbol> members,
            Dictionary<ISymbol, Lazy<List<ISymbol>>> lazyDependentsMap)
        {
            var baseTypeTree = MemberSymbolViewModelGraphNode.CreateInheritanceGraph(selectedOwnerSymbol.ContainingType, _glyphService);
            ViewModel = new PullMemberUpViewModel(members.ToList(), baseTypeTree.Neighbours, selectedOwnerSymbol, _glyphService, lazyDependentsMap, this);
            var dialog = new PullMemberUpDialog(ViewModel);
            if (dialog.ShowModal().GetValueOrDefault())
            {
                return new PullMemberDialogResult(MembersInfo);
            }
            else
            {
                return PullMemberDialogResult.CanceledResult;
            }
        }

        internal AnalysisResult CreateAnaysisResult(PullMemberUpViewModel viewModel)
        {
            var membersInfo = viewModel.SelectedMembersContainer.
                Where(memberSymbolView => memberSymbolView.IsChecked).
                Select(memberSymbolView => (memberSymbolView.MemberSymbol, memberSymbolView.IsAbstract));
            MembersInfo = PullMembersUpAnalysisBuilder.BuildAnalysisResult(
                viewModel.SelectedTarget.MemberSymbolViewModel.MemberSymbol as INamedTypeSymbol,
                membersInfo);
            return MembersInfo;
        }
    }
}
