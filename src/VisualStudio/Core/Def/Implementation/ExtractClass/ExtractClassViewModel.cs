// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable 

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.VisualStudio.LanguageServices.Implementation.CommonControls;
using Microsoft.VisualStudio.LanguageServices.Implementation.PullMemberUp.MainDialog;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ExtractClass
{
    internal class ExtractClassViewModel
    {
        private readonly INotificationService _notificationService;

        public ExtractClassViewModel(
            IWaitIndicator waitIndicator,
            INotificationService notificationService,
            ImmutableArray<PullMemberUpSymbolViewModel> memberViewModels,
            ImmutableDictionary<ISymbol, Task<ImmutableArray<ISymbol>>> memberToDependentsMap,
            string defaultTypeName,
            string defaultNamespace,
            string languageName,
            string typeParameterSuffix,
            ImmutableArray<string> conflictingNames,
            ISyntaxFactsService syntaxFactsService)
        {
            _notificationService = notificationService;

            MemberSelectionViewModel = new MemberSelectionViewModel(
                waitIndicator,
                memberViewModels,
                memberToDependentsMap,
                destinationTypeKind: TypeKind.Class);

            DestinationViewModel = new NewTypeDestinationSelectionViewModel(
                defaultTypeName,
                languageName,
                defaultNamespace,
                typeParameterSuffix,
                conflictingNames,
                syntaxFactsService);
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
    }
}
