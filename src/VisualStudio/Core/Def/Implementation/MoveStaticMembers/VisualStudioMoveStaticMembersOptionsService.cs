// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

        private const int HistorySize = 3;

        public readonly LinkedList<(string, string)> History = new();

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

            var existingTypeNames = MakeTypeNameItems(
                selectedType.ContainingNamespace,
                selectedType,
                document,
                cancellationTokenSource.Token);

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

        private static string GetFile(Location loc) => PathUtilities.GetFileName(loc.SourceTree!.FilePath);

        /// <summary>
        /// Construct all the type names declared in the project, 
        /// </summary>
        private ImmutableArray<TypeNameItem> MakeTypeNameItems(
            INamespaceSymbol currentNamespace,
            INamedTypeSymbol currentType,
            Document currentDocument,
            CancellationToken cancellationToken)
        {
            // find the top-level namespace
            while (!currentNamespace.IsGlobalNamespace)
            {
                currentNamespace = currentNamespace.ContainingNamespace;
            }

            return currentNamespace.GetAllTypes(cancellationToken).SelectMany(t =>
            {
                var fullName = GetFullyQualifiedNamespace(t);

                // for partially declared classes, we may want multiple entries for a single type.
                // filter to those actually in a real file, and that is not our current location.
                return t.Locations
                    .Where(l => l.IsInSource &&
                        (currentType.Name != t.Name || GetFile(l) != currentDocument.Name))
                    .Select(l => new TypeNameItem(
                        History.Contains((fullName, currentDocument.Name)),
                        GetFile(l),
                        fullName));
            })
            .ToImmutableArrayOrEmpty()
            .Sort(comparison: TypeNameItem.CompareTo);
        }

        private static string GetFullyQualifiedNamespace(INamedTypeSymbol type)
        {
            // construct fully qualified type name
            var displayName = type.Name;
            var containingNamespace = type.ContainingNamespace;
            while (!containingNamespace.IsGlobalNamespace)
            {
                displayName = containingNamespace.Name + "." + displayName;
                containingNamespace = containingNamespace.ContainingNamespace;
            }
            return displayName;
        }
    }
}
