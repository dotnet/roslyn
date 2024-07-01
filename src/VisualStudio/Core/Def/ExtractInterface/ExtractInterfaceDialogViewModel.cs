// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices.CommonControls;
using Microsoft.VisualStudio.LanguageServices.Implementation.CommonControls;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.VisualStudio.LanguageServices.Utilities;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ExtractInterface;

internal class ExtractInterfaceDialogViewModel : AbstractNotifyPropertyChanged
{
    private readonly INotificationService _notificationService;

    internal ExtractInterfaceDialogViewModel(
        ISyntaxFactsService syntaxFactsService,
        IUIThreadOperationExecutor uiThreadOperationExecutor,
        INotificationService notificationService,
        string defaultInterfaceName,
        List<string> conflictingTypeNames,
        ImmutableArray<LanguageServices.Utilities.MemberSymbolViewModel> memberViewModels,
        string defaultNamespace,
        string generatedNameTypeParameterSuffix,
        string languageName,
        IGlobalOptionService globalOptionService)
    {
        _notificationService = notificationService;

        MemberSelectionViewModel = new MemberSelectionViewModel(
            uiThreadOperationExecutor,
            memberViewModels,
            dependentsMap: null,
            destinationTypeKind: TypeKind.Interface,
            showDependentsButton: false,
            showPublicButton: false);

        DestinationViewModel = new NewTypeDestinationSelectionViewModel(
            defaultInterfaceName,
            languageName,
            defaultNamespace,
            generatedNameTypeParameterSuffix,
            conflictingTypeNames.ToImmutableArray(),
            syntaxFactsService,
            new PersistNewTypeDestinationValueSource(
                globalOptionService, NewTypeDestinationOptionStorage.ExtractInterfaceDestination, languageName));
    }

    internal bool TrySubmit()
    {
        if (!DestinationViewModel.TrySubmit(out var message))
        {
            SendFailureNotification(message);
            return false;
        }

        if (!MemberContainers.Any(c => c.IsChecked))
        {
            SendFailureNotification(ServicesVSResources.You_must_select_at_least_one_member);
            return false;
        }

        // TODO: Deal with filename already existing

        return true;
    }

    private void SendFailureNotification(string message)
        => _notificationService.SendNotification(message, severity: NotificationSeverity.Information);

    public ImmutableArray<MemberSymbolViewModel> MemberContainers => MemberSelectionViewModel.Members;

    public NewTypeDestinationSelectionViewModel DestinationViewModel { get; internal set; }

    public MemberSelectionViewModel MemberSelectionViewModel { get; }
}
