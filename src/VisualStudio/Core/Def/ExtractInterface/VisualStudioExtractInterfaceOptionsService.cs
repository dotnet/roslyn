// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ExtractInterface;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.LanguageServices.Implementation.CommonControls;
using Microsoft.VisualStudio.LanguageServices.Utilities;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ExtractInterface;

[ExportWorkspaceService(typeof(IExtractInterfaceOptionsService), ServiceLayer.Host), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class VisualStudioExtractInterfaceOptionsService(
    IGlyphService glyphService,
    IThreadingContext threadingContext,
    IUIThreadOperationExecutor uiThreadOperationExecutor) : IExtractInterfaceOptionsService
{
    private readonly IGlyphService _glyphService = glyphService;
    private readonly IThreadingContext _threadingContext = threadingContext;
    private readonly IUIThreadOperationExecutor _uiThreadOperationExecutor = uiThreadOperationExecutor;

    public ExtractInterfaceOptionsResult GetExtractInterfaceOptions(
        Document document,
        ImmutableArray<ISymbol> extractableMembers,
        string defaultInterfaceName,
        ImmutableArray<string> allTypeNames,
        string defaultNamespace,
        string generatedNameTypeParameterSuffix)
    {
        _threadingContext.ThrowIfNotOnUIThread();
        var solution = document.Project.Solution;
        var canAddDocument = solution.CanApplyChange(ApplyChangesKind.AddDocument);
        var notificationService = solution.Services.GetRequiredService<INotificationService>();

        var memberViewModels = extractableMembers
            .SelectAsArray(member =>
                new MemberSymbolViewModel(member, _glyphService)
                {
                    IsChecked = true,
                    MakeAbstract = false,
                    IsMakeAbstractCheckable = false,
                    IsCheckable = true
                });

        var viewModel = new ExtractInterfaceDialogViewModel(
            document.GetRequiredLanguageService<ISyntaxFactsService>(),
            _uiThreadOperationExecutor,
            notificationService,
            defaultInterfaceName,
            allTypeNames,
            memberViewModels,
            defaultNamespace,
            generatedNameTypeParameterSuffix,
            document.Project.Language,
            canAddDocument);

        var dialog = new ExtractInterfaceDialog(viewModel);
        var result = dialog.ShowModal();

        if (result.HasValue && result.Value)
        {
            var includedMembers = viewModel.MemberContainers.SelectAsArray(c => c.IsChecked, c => c.Symbol);

            return new ExtractInterfaceOptionsResult(
                isCancelled: false,
                includedMembers: includedMembers.AsImmutable(),
                interfaceName: viewModel.DestinationViewModel.TypeName.Trim(),
                fileName: viewModel.DestinationViewModel.FileName.Trim(),
                location: GetLocation(viewModel.DestinationViewModel.Destination));
        }
        else
        {
            return ExtractInterfaceOptionsResult.Cancelled;
        }
    }

    private static ExtractInterfaceOptionsResult.ExtractLocation GetLocation(NewTypeDestination destination)
        => destination switch
        {
            NewTypeDestination.CurrentFile => ExtractInterfaceOptionsResult.ExtractLocation.SameFile,
            NewTypeDestination.NewFile => ExtractInterfaceOptionsResult.ExtractLocation.NewFile,
            _ => throw ExceptionUtilities.UnexpectedValue(destination),
        };
}
