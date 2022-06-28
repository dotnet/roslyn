// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.LanguageServices.Implementation.CommonControls;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ExtractInterface
{
    internal class ExtractInterfaceDialogViewModel : AbstractNotifyPropertyChanged
    {
        private readonly INotificationService _notificationService;

        internal ExtractInterfaceDialogViewModel(
            ISyntaxFactsService syntaxFactsService,
            IGlyphService glyphService,
            INotificationService notificationService,
            string defaultInterfaceName,
            List<ISymbol> extractableMembers,
            List<string> conflictingTypeNames,
            string defaultNamespace,
            string generatedNameTypeParameterSuffix,
            string languageName)
        {
            _notificationService = notificationService;

            DestinationViewModel = new NewTypeDestinationSelectionViewModel(
                defaultInterfaceName,
                languageName,
                defaultNamespace,
                generatedNameTypeParameterSuffix,
                conflictingTypeNames.ToImmutableArray(),
                syntaxFactsService);

            MemberContainers = extractableMembers.Select(m => new MemberSymbolViewModel(m, glyphService)).OrderBy(s => s.SymbolName).ToList();
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

        internal void DeselectAll()
        {
            foreach (var memberContainer in MemberContainers)
            {
                memberContainer.IsChecked = false;
            }
        }

        internal void SelectAll()
        {
            foreach (var memberContainer in MemberContainers)
            {
                memberContainer.IsChecked = true;
            }
        }

        public List<MemberSymbolViewModel> MemberContainers { get; set; }

        public NewTypeDestinationSelectionViewModel DestinationViewModel { get; internal set; }

        internal class MemberSymbolViewModel : SymbolViewModel<ISymbol>
        {
            public MemberSymbolViewModel(ISymbol symbol, IGlyphService glyphService) : base(symbol, glyphService)
            {
            }
        }
    }
}
