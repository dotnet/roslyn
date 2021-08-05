// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.MoveStaticMembers;
using Microsoft.CodeAnalysis.PullMemberUp;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.LanguageServices.Implementation.PullMemberUp;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.MoveStaticMembers
{
    [ExportWorkspaceService(typeof(IMoveStaticMembersOptionsService), ServiceLayer.Host), Shared]
    internal class VisualStudioMoveStaticMembersOptionsService : IMoveStaticMembersOptionsService
    {
        private readonly IGlyphService _glyphService;
        private readonly IUIThreadOperationExecutor _uiThreadOperationExecutor;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioMoveStaticMembersOptionsService(
            IGlyphService glyphService,
            IUIThreadOperationExecutor uiThreadOperationExecutor)
        {
            _glyphService = glyphService;
            _uiThreadOperationExecutor = uiThreadOperationExecutor;
        }

        public MoveStaticMembersOptions GetMoveMembersToTypeOptions(Document document, INamedTypeSymbol selectedType, ISymbol? selectedNodeSymbol)
        {
            var membersInType = selectedType.GetMembers().
               WhereAsArray(member => MemberAndDestinationValidator.IsMemberValid(member) && member.IsStatic);

            var memberViewModels = membersInType
                .SelectAsArray(member =>
                    new SymbolViewModel<ISymbol>(member, _glyphService)
                    {
                        // The member user selected will be checked at the beginning.
                        IsChecked = SymbolEquivalenceComparer.Instance.Equals(selectedNodeSymbol, member),
                    });

            using var cancellationTokenSource = new CancellationTokenSource();
            var memberToDependentsMap = SymbolDependentsBuilder.FindMemberToDependentsMap(membersInType, document.Project, cancellationTokenSource.Token);

            var existingTypeNames = selectedType.ContainingNamespace.GetAllTypes(cancellationTokenSource.Token).SelectAsArray(t => t.Name);
            var candidateName = selectedType.Name + "Helpers";
            var defaultTypeName = NameGenerator.GenerateUniqueName(candidateName, name => !existingTypeNames.Contains(name));

            var containingNamespaceDisplay = selectedType.ContainingNamespace.IsGlobalNamespace
                ? string.Empty
                : selectedType.ContainingNamespace.ToDisplayString();

            var generatedNameTypeParameterSuffix = ExtractTypeHelpers.GetTypeParameterSuffix(document, selectedType, membersInType);

            var selectMembersViewModel = new StaticMemberSelectionViewModel(
                _uiThreadOperationExecutor,
                memberViewModels,
                memberToDependentsMap);

            var viewModel = new MoveStaticMembersDialogViewModel(selectMembersViewModel,
                defaultTypeName,
                existingTypeNames,
                document.GetRequiredLanguageService<ISyntaxFactsService>());

            var dialog = new MoveStaticMembersDialog(viewModel);

            var result = dialog.ShowModal();

            if (result.GetValueOrDefault())
            {
                return new MoveStaticMembersOptions(
                    // TODO: generate unique file name based off of existing folder documents
                    viewModel.DestinationName + (document.Project.Language == LanguageNames.CSharp ? ".cs" : ".vb"),
                    string.Join(".", containingNamespaceDisplay, viewModel.DestinationName),
                    selectMembersViewModel.CheckedMembers.SelectAsArray(vm => vm.Symbol));
            }

            return MoveStaticMembersOptions.Cancelled;
        }
    }
}
