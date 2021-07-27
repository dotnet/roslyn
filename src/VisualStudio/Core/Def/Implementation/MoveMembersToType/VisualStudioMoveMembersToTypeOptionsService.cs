// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.MoveMembersToType;
using Microsoft.CodeAnalysis.PullMemberUp;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.LanguageServices.Implementation.PullMemberUp;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.MoveMembersToType
{
    [ExportWorkspaceService(typeof(IMoveMembersToTypeOptionsService), ServiceLayer.Host), Shared]
    internal class VisualStudioMoveMembersToTypeOptionsService : IMoveMembersToTypeOptionsService
    {
        private readonly IGlyphService _glyphService;
        private readonly IUIThreadOperationExecutor _uiThreadOperationExecutor;

        private const int HistorySize = 3;

        public readonly LinkedList<TypeNameItem> History = new();

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioMoveMembersToTypeOptionsService(
            IGlyphService glyphService,
            IUIThreadOperationExecutor uiThreadOperationExecutor)
        {
            _glyphService = glyphService;
            _uiThreadOperationExecutor = uiThreadOperationExecutor;
        }

        public MoveMembersToTypeOptions GetMoveMembersToTypeOptions(Document document, INamedTypeSymbol selectedType, ISymbol? selectedNodeSymbol)
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

            var existingTypeNames = selectedType.ContainingNamespace.GetAllTypes(cancellationTokenSource.Token)
                .SelectMany(t =>
                {
                    // for partially declared classes, we may want multiple entries for a single type.
                    // filter to those actually in a real file, and that are not already in our History list.
                    return t.Locations
                        .Where(l => l.IsInSource &&
                            !History.Any(h => GetFile(l) == h.DeclarationFile && t.Name == h.TypeName))
                        .Select(l => new TypeNameItem(false, GetFile(l), t.Name));
                })
                .ToImmutableArrayOrEmpty();
            var candidateName = selectedType.Name + "Helpers";
            var defaultTypeName = NameGenerator.GenerateUniqueName(candidateName, name => !existingTypeNames.Any(t => t.TypeName == name));

            var containingNamespaceDisplay = selectedType.ContainingNamespace.IsGlobalNamespace
                ? string.Empty
                : selectedType.ContainingNamespace.ToDisplayString();

            var generatedNameTypeParameterSuffix = ExtractTypeHelpers.GetTypeParameterSuffix(document, selectedType, membersInType);

            var selectMembersViewModel = new StaticMemberSelectionViewModel(
                _uiThreadOperationExecutor,
                memberViewModels,
                memberToDependentsMap);

            var viewModel = new MoveMembersToTypeDialogViewModel(selectMembersViewModel,
                defaultTypeName,
                existingTypeNames,
                document.GetRequiredLanguageService<ISyntaxFactsService>(),
                History.ToImmutableArrayOrEmpty());

            var dialog = new MoveMembersToTypeDialog(viewModel);

            var result = dialog.ShowModal();

            if (result.GetValueOrDefault())
            {
                // TODO: Get Options
                return null;
            }

            return null;
        }

        private static string GetFile(Location loc) => PathUtilities.GetFileName(loc.SourceTree!.FilePath);
    }
}
