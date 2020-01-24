// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature
{
    internal class AddParameterDialogViewModel : AbstractNotifyPropertyChanged
    {
        private readonly INotificationService? _notificationService;

        public readonly Document Document;
        public readonly int InsertPosition;

        public AddParameterDialogViewModel(Document document, int insertPosition)
        {
            _notificationService = document.Project.Solution.Workspace.Services.GetService<INotificationService>();
            Document = document;
            InsertPosition = insertPosition;
            ParameterName = string.Empty;
            CallSiteValue = string.Empty;
            TypeName = string.Empty;
        }

        public string ParameterName { get; set; }

        public string CallSiteValue { get; set; }

        public string TypeName { get; set; }

        internal bool TrySubmit(Document document)
        {
            if (string.IsNullOrEmpty(ParameterName) || string.IsNullOrEmpty(TypeName))
            {
                SendFailureNotification(ServicesVSResources.A_type_and_name_must_be_provided);
                return false;
            }

            if (!IsParameterTypeValid(TypeName, document))
            {
                SendFailureNotification(ServicesVSResources.Parameter_type_contains_invalid_characters);
                return false;
            }

            if (!IsParameterNameValid(ParameterName, document))
            {
                SendFailureNotification(ServicesVSResources.Parameter_name_contains_invalid_characters);
                return false;
            }

            return true;
        }

        private void SendFailureNotification(string message)
        {
            _notificationService?.SendNotification(message, severity: NotificationSeverity.Information);
        }

        private bool IsParameterTypeValid(string typeName, Document document)
        {
            var languageService = document.GetRequiredLanguageService<IChangeSignatureViewModelFactoryService>();
            return languageService.IsTypeNameValid(typeName);
        }

        private bool IsParameterNameValid(string identifierName, Document document)
        {
            var languageService = document.GetRequiredLanguageService<ISyntaxFactsService>();
            return languageService.IsValidIdentifier(identifierName);
        }
    }
}
