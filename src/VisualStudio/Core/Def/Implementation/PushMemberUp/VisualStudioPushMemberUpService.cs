using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.PushMemberUp;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Language.Intellisense;
using static Microsoft.VisualStudio.LanguageServices.Implementation.ExtractInterface.ExtractInterfaceDialogViewModel;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.PushMemberUp
{
    [ExportWorkspaceService(typeof(IPushMemberUpService), ServiceLayer.Host), Shared]
    internal class VisualStudioPushMemberUpService : IPushMemberUpService
    {
        private readonly IGlyphService _glyphService;

        [ImportingConstructor]
        public VisualStudioPushMemberUpService(IGlyphService glyphService)
        {
            _glyphService = glyphService;
        }

        public PushTargetsResult GetPushTargetAndMembers(
            INamedTypeSymbol selectedNodeOwnerSymbol,
            IEnumerable<ISymbol> members)
        {
            var baseTypeTree = MemberSymbolViewModelGraphNode.CreateInheritanceGraph(selectedNodeOwnerSymbol, _glyphService);
            var viewModel = new PushMemberUpViewModel(members.ToList(), baseTypeTree.Neighbours, _glyphService);
            var dialog = new PushMemberUpDialogxaml(viewModel);
            var result = dialog.ShowModal();
            if (result.GetValueOrDefault())
            {
                return new PushTargetsResult(
                    viewModel.SelectedMembersContainer.
                    Where(memberSymbolView => memberSymbolView.IsChecked).
                    Select(memberSymbolView => memberSymbolView.MemberSymbol), viewModel.SelectedTarget.MemberSymbolViewModel.MemberSymbol as INamedTypeSymbol);
            }
            else
            {
                return PushTargetsResult.CanceledResult;
            }
        }

    }
}
