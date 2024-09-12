// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.MoveStaticMembers;
using Microsoft.CodeAnalysis.PullMemberUp;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.LanguageServices.Implementation.PullMemberUp;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.MoveStaticMembers;

[ExportWorkspaceService(typeof(IMoveStaticMembersOptionsService), ServiceLayer.Host), Shared]
internal class VisualStudioMoveStaticMembersOptionsService : IMoveStaticMembersOptionsService
{
    private readonly IGlyphService _glyphService;
    private readonly IUIThreadOperationExecutor _uiThreadOperationExecutor;

    private const int HistorySize = 3;

    public readonly LinkedList<INamedTypeSymbol> History = new();

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public VisualStudioMoveStaticMembersOptionsService(
        IGlyphService glyphService,
        IUIThreadOperationExecutor uiThreadOperationExecutor)
    {
        _glyphService = glyphService;
        _uiThreadOperationExecutor = uiThreadOperationExecutor;
    }

    public MoveStaticMembersOptions GetMoveMembersToTypeOptions(Document document, INamedTypeSymbol selectedType, ImmutableArray<ISymbol> selectedNodeSymbols)
    {
        var viewModel = GetViewModel(document, selectedType, selectedNodeSymbols, History, _glyphService, _uiThreadOperationExecutor);

        var dialog = new MoveStaticMembersDialog(viewModel);

        var result = dialog.ShowModal();

        if (result.GetValueOrDefault())
        {
            UpdateHistory(viewModel);
            return GenerateOptions(document.Project.Language, viewModel, result.GetValueOrDefault());
        }

        return MoveStaticMembersOptions.Cancelled;
    }

    // internal for testing purposes
    internal static MoveStaticMembersOptions GenerateOptions(string language, MoveStaticMembersDialogViewModel viewModel, bool dialogResult)
    {
        if (dialogResult)
        {
            // if the destination name contains extra namespaces, we want the last one as that is the real type name
            var typeName = viewModel.DestinationName.TypeName.Split('.').Last();
            var newFileName = Path.ChangeExtension(typeName, language == LanguageNames.CSharp ? ".cs" : ".vb");
            var selectedMembers = viewModel.MemberSelectionViewModel.CheckedMembers.SelectAsArray(vm => vm.Symbol);

            if (viewModel.DestinationName.IsNew)
            {
                return new MoveStaticMembersOptions(
                    newFileName,
                    viewModel.DestinationName.TypeName,
                    selectedMembers);
            }

            RoslynDebug.AssertNotNull(viewModel.DestinationName.NamedType);

            return new MoveStaticMembersOptions(
                viewModel.DestinationName.NamedType,
                selectedMembers);
        }

        return MoveStaticMembersOptions.Cancelled;
    }

    // internal for testing purposes, get the view model
    internal static MoveStaticMembersDialogViewModel GetViewModel(
        Document document,
        INamedTypeSymbol selectedType,
        ImmutableArray<ISymbol> selectedNodeSymbols,
        LinkedList<INamedTypeSymbol> history,
        IGlyphService? glyphService,
        IUIThreadOperationExecutor uiThreadOperationExecutor)
    {
        var membersInType = selectedType.GetMembers().
           WhereAsArray(member => MemberAndDestinationValidator.IsMemberValid(member) && member.IsStatic);

        var memberViewModels = membersInType
            .SelectAsArray(member =>
                new SymbolViewModel<ISymbol>(member, glyphService)
                {
                    // The member(s) user selected will be checked at the beginning.
                    IsChecked = selectedNodeSymbols.Any(SymbolEquivalenceComparer.Instance.Equals, member),
                });

        using var cancellationTokenSource = new CancellationTokenSource();
        var memberToDependentsMap = SymbolDependentsBuilder.FindMemberToDependentsMap(membersInType, document.Project, cancellationTokenSource.Token);

        var existingTypes = selectedType.ContainingNamespace.GetAllTypes(cancellationTokenSource.Token).ToImmutableArray();
        var existingTypeNames = existingTypes.SelectAsArray(t => t.ToDisplayString());
        var candidateName = selectedType.Name + "Helpers";
        var defaultTypeName = NameGenerator.GenerateUniqueName(candidateName, name => !existingTypeNames.Contains(name));

        var containingNamespaceDisplay = selectedType.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : selectedType.ContainingNamespace.ToDisplayString();

        var selectMembersViewModel = new StaticMemberSelectionViewModel(
            uiThreadOperationExecutor,
            memberViewModels,
            memberToDependentsMap);

        var availableTypeNames = MakeTypeNameItems(
            selectedType.ContainingNamespace,
            selectedType,
            document,
            history,
            cancellationTokenSource.Token);

        return new MoveStaticMembersDialogViewModel(
            selectMembersViewModel,
            defaultTypeName,
            availableTypeNames,
            containingNamespaceDisplay,
            document.GetRequiredLanguageService<ISyntaxFactsService>());
    }

    private void UpdateHistory(MoveStaticMembersDialogViewModel viewModel)
    {
        if (viewModel.DestinationName.IsNew || viewModel.DestinationName.IsFromHistory)
        {
            // if we create a new destination or already have it in the history,
            // we don't need to update our history list.
            return;
        }

        History.AddFirst(viewModel.DestinationName.NamedType!);
        if (History.Count > HistorySize)
        {
            History.RemoveLast();
        }
    }

    private static string GetFile(Location loc) => loc.SourceTree!.FilePath;

    /// <summary>
    /// Construct all the type names declared in the project,
    /// </summary>
    private static ImmutableArray<TypeNameItem> MakeTypeNameItems(
        INamespaceSymbol currentNamespace,
        INamedTypeSymbol currentType,
        Document currentDocument,
        LinkedList<INamedTypeSymbol> history,
        CancellationToken cancellationToken)
    {
        return currentNamespace.GetAllTypes(cancellationToken)
            // only take symbols that are the same kind of type (class, module)
            // and remove non-static types only when the current type is static
            .Where(t => t.TypeKind == currentType.TypeKind && (t.IsStaticType() || !currentType.IsStaticType()))
            .SelectMany(t =>
            {
                // for partially declared classes, we may want multiple entries for a single type.
                // filter to those actually in a real file, and that is not our current location.
                return t.Locations
                    .Where(l => l.IsInSource &&
                        (currentType.Name != t.Name || GetFile(l) != currentDocument.FilePath))
                    .Select(l => new TypeNameItem(
                        history.Contains(t),
                        GetFile(l),
                        t));
            })
        .ToImmutableArrayOrEmpty()
        .Sort(comparison: TypeNameItem.CompareTo);
    }
}
