// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ExtractClass;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Options;
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
internal class VisualStudioExtractClassOptionsService : IExtractClassOptionsService
{
    private readonly IThreadingContext _threadingContext;
    private readonly IGlyphService _glyphService;
    private readonly IUIThreadOperationExecutor _uiThreadOperationExecutor;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public VisualStudioExtractClassOptionsService(
        IThreadingContext threadingContext,
        IGlyphService glyphService,
        IUIThreadOperationExecutor uiThreadOperationExecutor)
    {
        _threadingContext = threadingContext;
        _glyphService = glyphService;
        _uiThreadOperationExecutor = uiThreadOperationExecutor;
    }

    public async Task<ExtractClassOptions?> GetExtractClassOptionsAsync(Document document, INamedTypeSymbol selectedType, ImmutableArray<ISymbol> selectedMembers, CancellationToken cancellationToken)
    {
        var notificationService = document.Project.Solution.Services.GetRequiredService<INotificationService>();

        var membersInType = selectedType.GetMembers().
           WhereAsArray(MemberAndDestinationValidator.IsMemberValid);

        var memberViewModels = membersInType
            .SelectAsArray(member =>
                new MemberSymbolViewModel(member, _glyphService)
                {
                    // The member(s) user selected will be checked at the beginning.
                    IsChecked = selectedMembers.Any(predicate: SymbolEquivalenceComparer.Instance.Equals, arg: member),
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

        var formattingOptions = await document.GetSyntaxFormattingOptionsAsync(cancellationToken).ConfigureAwait(false);
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
            conflictingTypeNames.ToImmutableArray(),
            document.GetRequiredLanguageService<ISyntaxFactsService>());

        await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
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
