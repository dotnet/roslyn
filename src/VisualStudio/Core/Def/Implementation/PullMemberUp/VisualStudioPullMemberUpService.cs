using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.PullMemberUp;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Language.Intellisense;
using static Microsoft.VisualStudio.LanguageServices.Implementation.ExtractInterface.ExtractInterfaceDialogViewModel;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.PullMemberUp
{
    [ExportWorkspaceService(typeof(IPullMemberUpService), ServiceLayer.Host), Shared]
    internal class VisualStudioPullMemberUpService : IPullMemberUpService
    {
        private readonly IGlyphService _glyphService;

        private PullMemberUpViewModel ViewModel { get; set; }

        [ImportingConstructor]
        public VisualStudioPullMemberUpService(IGlyphService glyphService)
        {
            _glyphService = glyphService;
        }

        public bool CreateWarningDialog(List<string> warningMessages)
        {
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

        public PullTargetsResult RestoreSelectionDialog()
        {
            var dialog = new PullMemberUpDialogxaml(ViewModel);
            return CreateResult(ViewModel, dialog.ShowModal());
        }

        public PullTargetsResult GetPullTargetAndMembers(
            ISymbol selectedOwnerSymbol,
            IEnumerable<ISymbol> members,
            Dictionary<ISymbol, Lazy<List<ISymbol>>> lazyDependentsMap)
        {
            var baseTypeTree = MemberSymbolViewModelGraphNode.CreateInheritanceGraph(selectedOwnerSymbol.ContainingType, _glyphService);
            ViewModel = new PullMemberUpViewModel(members.ToList(), baseTypeTree.Neighbours, selectedOwnerSymbol, _glyphService, lazyDependentsMap);
            var dialog = new PullMemberUpDialogxaml(ViewModel);
            return CreateResult(ViewModel, dialog.ShowModal());
        }

        private PullTargetsResult CreateResult(PullMemberUpViewModel viewModel, bool? showModal)
        {
            if (showModal.GetValueOrDefault())
            {
                return new PullTargetsResult(
                    ViewModel.SelectedMembersContainer.
                    Where(memberSymbolView => memberSymbolView.IsChecked).
                    Select(memberSymbolView => (memberSymbolView.MemberSymbol, memberSymbolView.IsAbstract)),
                    ViewModel.SelectedTarget.MemberSymbolViewModel.MemberSymbol as INamedTypeSymbol);
            }
            else
            {
                return PullTargetsResult.CanceledResult;
            }
        }
    }
}
