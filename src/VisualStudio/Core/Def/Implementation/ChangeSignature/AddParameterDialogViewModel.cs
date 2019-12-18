// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Notification;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature
{
    internal class AddParameterDialogViewModel : AbstractNotifyPropertyChanged
    {
        private readonly INotificationService _notificationService;

        public AddParameterDialogViewModel(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public string ParameterName { get; set; }

        public string CallsiteValue { get; set; }

        public string TypeName { get; set; }

        internal bool TrySubmit()
        {
            if (string.IsNullOrEmpty(ParameterName) || string.IsNullOrEmpty(TypeName))
            {
                SendFailureNotification("A type and name must be provided.");
                return false;
            }

            return true;
        }

        private void SendFailureNotification(string message)
        {
            _notificationService.SendNotification(message, severity: NotificationSeverity.Information);
        }
    }
}
