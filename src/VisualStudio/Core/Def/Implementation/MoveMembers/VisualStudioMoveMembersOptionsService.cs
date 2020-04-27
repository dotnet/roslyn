using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MoveMembers;
using Microsoft.VisualStudio.LanguageServices.Implementation.MoveMembers.MainDialog;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.MoveMembers
{
    [Export(typeof(IMoveMembersOptionService)), Shared]
    internal class VisualStudioMoveMembersOptionsService : IMoveMembersOptionService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioMoveMembersOptionsService()
        {
        }

        public MoveMembersOptions GetMoveMembersOptions(Document document, MoveMembersAnalysisResult analysis, MoveMembersEntryPoint entryPoint)
        {
            var viewModel = new MoveMembersDialogViewModel(
                waitIndicator: null!,
                analysis.SelectedType,
                analysis.ValidMembersInType.SelectAsArray(m => new MoveMembersSymbolViewModel(m, null)),
                SymbolDependentsBuilder.FindMemberToDependentsMap(analysis.ValidMembersInType, document.Project, CancellationToken.None),
                document.Project.Language == LanguageNames.CSharp ? ".cs" : ".vb",
                suggestInterface: analysis.SelectedType.TypeKind == TypeKind.Interface,
                destinations: analysis.DestinationAnalysisResults.SelectAsArray(d => new SymbolViewModel<INamedTypeSymbol>(d.Destination, null)));

            var dialog = new MoveMembersDialog("", "", viewModel);
            var result = dialog.ShowModal();

            if (result == true)
            {
                return new MoveMembersOptions(
                    viewModel.SelectedDestination,
                    viewModel.GetCheckedMembers().SelectAsArray(m => new MemberAnalysisResult(m.member, changeDestinationTypeToAbstract: m.makeAbstract)),
                    !viewModel.MovingToExistingType,
                    analysis.SelectedNode,
                    viewModel.OriginalTypeSymbol);
            }
            else
            {
                return MoveMembersOptions.Cancelled;
            }
        }
    }
}
