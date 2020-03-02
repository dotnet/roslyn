// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.  

using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp.Dialog;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PullMemberUp;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.LanguageServices.Implementation.MoveMembers.MainDialog;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.MoveMembers
{
    [ExportWorkspaceService(typeof(IPullMemberUpOptionsService), ServiceLayer.Host), Shared]
    internal class VisualStudioPullMemberUpService : IPullMemberUpOptionsService
    {
        private readonly IGlyphService _glyphService;
        private readonly IWaitIndicator _waitIndicator;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioPullMemberUpService(IGlyphService glyphService, IWaitIndicator waitIndicator)
        {
            _glyphService = glyphService;
            _waitIndicator = waitIndicator;
        }

        public PullMembersUpOptions GetPullMemberUpOptions(Document document, INamedTypeSymbol selectedMember)
        {
            var membersInType = selectedMember.ContainingType.GetMembers().
                WhereAsArray(member => MemberAndDestinationValidator.IsMemberValid(member));
            var memberViewModels = membersInType
                .SelectAsArray(member =>
                    new MoveMembersSymbolViewModel(member, _glyphService)
                    {
                        // The member user selected will be checked at the beginning.
                        IsChecked = SymbolEquivalenceComparer.Instance.Equals(selectedMember, member),
                        MakeAbstract = false,
                        IsMakeAbstractCheckable = !member.IsKind(SymbolKind.Field) && !member.IsAbstract,
                        IsCheckable = true
                    });

            using var cancellationTokenSource = new CancellationTokenSource();
            var baseTypeRootViewModel = BaseTypeTreeNodeViewModel.CreateBaseTypeTree(
                _glyphService,
                document.Project.Solution,
                selectedMember.ContainingType,
                cancellationTokenSource.Token).BaseTypeNodes.CastArray<SymbolViewModel<INamedTypeSymbol>>();

            var memberToDependentsMap = SymbolDependentsBuilder.FindMemberToDependentsMap(membersInType, document.Project, cancellationTokenSource.Token);
            var viewModel = new MoveMembersDialogViewModel(
                _waitIndicator,
                selectedMember,
                memberViewModels,
                memberToDependentsMap,
                document.Project.Language == LanguageNames.CSharp ? ".cs" : ".vb");

            var dialog = new MoveMembersDialog(ServicesVSResources.Pull_Members_Up, ServicesVSResources.Select_destination_and_members_to_pull_up, viewModel);
            var result = dialog.ShowModal();

            // Dialog has finshed its work, cancel finding dependents task.
            cancellationTokenSource.Cancel();
            if (result.GetValueOrDefault())
            {
                return new PullMembersUpOptions(
                    viewModel.SelectedDestination,
                    viewModel.AnalyzeCheckedMembers());
            }
            else
            {
                return null;
            }
        }
    }
}
