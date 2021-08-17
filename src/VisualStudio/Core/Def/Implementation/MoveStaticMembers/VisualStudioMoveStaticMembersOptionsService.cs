// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.IO;
using System.Linq;
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
            var viewModel = GetViewModel(document, selectedType, selectedNodeSymbol, _glyphService, _uiThreadOperationExecutor);

            var dialog = new MoveStaticMembersDialog(viewModel);

            var result = dialog.ShowModal();

            return GenerateOptions(document.Project.Language, viewModel, result.GetValueOrDefault());
        }

        // internal for testing purposes
        internal static MoveStaticMembersOptions GenerateOptions(string language, MoveStaticMembersDialogViewModel viewModel, bool dialogResult)
        {
            if (dialogResult)
            {
                // if the destination name contains extra namespaces, we want the last one as that is the real type name
                var typeName = viewModel.DestinationName.Split('.').Last();
                var newFileName = Path.ChangeExtension(typeName, language == LanguageNames.CSharp ? ".cs" : ".vb");
                return new MoveStaticMembersOptions(
                    newFileName,
                    viewModel.PrependedNamespace + viewModel.DestinationName,
                    viewModel.MemberSelectionViewModel.CheckedMembers.SelectAsArray(vm => vm.Symbol));
            }

            return MoveStaticMembersOptions.Cancelled;
        }

        // internal for testing purposes, get the view model
        internal static MoveStaticMembersDialogViewModel GetViewModel(
            Document document,
            INamedTypeSymbol selectedType,
            ISymbol? selectedNodeSymbol,
            IGlyphService? glyphService,
            IUIThreadOperationExecutor uiThreadOperationExecutor)
        {
            var membersInType = selectedType.GetMembers().
               WhereAsArray(member => MemberAndDestinationValidator.IsMemberValid(member) && member.IsStatic);

            var memberViewModels = membersInType
                .SelectAsArray(member =>
                    new SymbolViewModel<ISymbol>(member, glyphService)
                    {
                        // The member user selected will be checked at the beginning.
                        IsChecked = SymbolEquivalenceComparer.Instance.Equals(selectedNodeSymbol, member),
                    });

            using var cancellationTokenSource = new CancellationTokenSource();
            var memberToDependentsMap = SymbolDependentsBuilder.FindMemberToDependentsMap(membersInType, document.Project, cancellationTokenSource.Token);

            var existingTypeNames = selectedType.ContainingNamespace.GetAllTypes(cancellationTokenSource.Token).SelectAsArray(t => t.ToDisplayString());
            var candidateName = selectedType.Name + "Helpers";
            var defaultTypeName = NameGenerator.GenerateUniqueName(candidateName, name => !existingTypeNames.Contains(name));

            var containingNamespaceDisplay = selectedType.ContainingNamespace.IsGlobalNamespace
                ? string.Empty
                : selectedType.ContainingNamespace.ToDisplayString();

            var selectMembersViewModel = new StaticMemberSelectionViewModel(
                uiThreadOperationExecutor,
                memberViewModels,
                memberToDependentsMap);

            return new MoveStaticMembersDialogViewModel(selectMembersViewModel,
                defaultTypeName,
                existingTypeNames,
                containingNamespaceDisplay,
                document.GetRequiredLanguageService<ISyntaxFactsService>());
        }
    }
}
