// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.VisualStudio.LanguageServices.Implementation.CommonControls;
using Microsoft.VisualStudio.LanguageServices.Utilities;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ExtractClass;

internal sealed class ExtractClassViewModel
{
    private readonly INotificationService _notificationService;
    private readonly INamedTypeSymbol _selectedType;

    public ExtractClassViewModel(
        IUIThreadOperationExecutor uiThreadOperationExecutor,
        INotificationService notificationService,
        INamedTypeSymbol selectedType,
        ImmutableArray<MemberSymbolViewModel> memberViewModels,
        ImmutableDictionary<ISymbol, Task<ImmutableArray<ISymbol>>> memberToDependentsMap,
        string defaultTypeName,
        string defaultNamespace,
        string languageName,
        string typeParameterSuffix,
        ImmutableArray<string> conflictingNames,
        ISyntaxFactsService syntaxFactsService,
        bool canAddDocument)
    {
        _notificationService = notificationService;
        _selectedType = selectedType;

        MemberSelectionViewModel = new MemberSelectionViewModel(
            uiThreadOperationExecutor,
            memberViewModels,
            memberToDependentsMap,
            destinationTypeKind: TypeKind.Class);

        DestinationViewModel = new NewTypeDestinationSelectionViewModel(
            defaultTypeName,
            languageName,
            defaultNamespace,
            typeParameterSuffix,
            conflictingNames,
            syntaxFactsService,
            canAddDocument);
    }

    internal bool TrySubmit()
    {
        if (!DestinationViewModel.TrySubmit(out var message))
        {
            SendFailureNotification(message);
            return false;
        }

        return true;
    }

    private void SendFailureNotification(string message)
        => _notificationService.SendNotification(message, severity: NotificationSeverity.Information);

    public MemberSelectionViewModel MemberSelectionViewModel { get; }
    public NewTypeDestinationSelectionViewModel DestinationViewModel { get; }
    public string Title => _selectedType.IsRecord
        ? ServicesVSResources.Extract_Base_Record
        : ServicesVSResources.Extract_Base_Class;
}
