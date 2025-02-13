// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ExtractClass;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.PullMemberUp;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.LanguageServices.Implementation.PullMemberUp;
using Microsoft.VisualStudio.LanguageServices.Utilities;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ExtractClass;

[ExportWorkspaceService(typeof(IExtractClassOptionsService), ServiceLayer.Host), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class VisualStudioExtractClassOptionsService(
    IThreadingContext threadingContext,
    IGlyphService glyphService,
    IUIThreadOperationExecutor uiThreadOperationExecutor) : IExtractClassOptionsService
{
    private readonly IThreadingContext _threadingContext = threadingContext;
    private readonly IGlyphService _glyphService = glyphService;
    private readonly IUIThreadOperationExecutor _uiThreadOperationExecutor = uiThreadOperationExecutor;

    public ExtractClassOptions? GetExtractClassOptions(
        Document document,
        INamedTypeSymbol selectedType,
        ImmutableArray<ISymbol> selectedMembers,
        SyntaxFormattingOptions formattingOptions,
        CancellationToken cancellationToken)
    {
        _threadingContext.ThrowIfNotOnUIThread();

        var solution = document.Project.Solution;
        var canAddDocument = solution.CanApplyChange(ApplyChangesKind.AddDocument);
        var notificationService = solution.Services.GetRequiredService<INotificationService>();

        var membersInType = selectedType.GetMembers().
           WhereAsArray(MemberAndDestinationValidator.IsMemberValid);

        var memberViewModels = membersInType
            .SelectAsArray(member =>
                new MemberSymbolViewModel(member, _glyphService)
                {
                    // The member(s) user selected will be checked at the beginning.
                    IsChecked = selectedMembers.Any(SymbolEquivalenceComparer.Instance.Equals, member),
                    MakeAbstract = false,
                    IsMakeAbstractCheckable = !member.IsKind(SymbolKind.Field) && !member.IsAbstract,
                    IsCheckable = true
                });

        var memberToDependentsMap = SymbolDependentsBuilder.FindMemberToDependentsMap(membersInType, document.Project, cancellationToken);

        var conflictingTypeNames = selectedType.ContainingNamespace.GetAllTypes(cancellationToken).Select(t => t.Name);
        var candidateName = selectedType.Name + "Base";
        var defaultTypeName = NameGenerator.GenerateUniqueName(candidateName, name => !conflictingTypeNames.Contains(name));

        var containingNamespaceDisplay = selectedType.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : selectedType.ContainingNamespace.ToDisplayString();

        var generatedNameTypeParameterSuffix = ExtractTypeHelpers.GetTypeParameterSuffix(document, formattingOptions, selectedType, membersInType, cancellationToken);

        var viewModel = new ExtractClassViewModel(
            _uiThreadOperationExecutor,
            notificationService,
            selectedType,
            memberViewModels,
            memberToDependentsMap,
            defaultTypeName,
            containingNamespaceDisplay,
            document.Project.Language,
            generatedNameTypeParameterSuffix,
            [.. conflictingTypeNames],
            document.GetRequiredLanguageService<ISyntaxFactsService>(),
            canAddDocument);

        var dialog = new ExtractClassDialog(viewModel);

        var result = dialog.ShowModal();

        if (result.GetValueOrDefault())
        {
            return new ExtractClassOptions(
                viewModel.DestinationViewModel.FileName,
                viewModel.DestinationViewModel.TypeName,
                viewModel.DestinationViewModel.Destination == CommonControls.NewTypeDestination.CurrentFile,
                viewModel.MemberSelectionViewModel.CheckedMembers.SelectAsArray(m => new ExtractClassMemberAnalysisResult(m.Symbol, m.MakeAbstract)));
        }

        return null;
    }
}
