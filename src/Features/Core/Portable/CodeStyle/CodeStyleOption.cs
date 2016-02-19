// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CodeStyle
{
    // TODO: make this a generic class with a T and Notification.
    // If we're using a checkbox, T is bool.
    internal class CodeStyleOption
    {
        public static readonly CodeStyleOption Default = new CodeStyleOption(false, NotificationOption.None);

        public CodeStyleOption(bool isChecked, NotificationOption notification)
        {
            IsChecked = isChecked;
            Notification = notification;
        }

        public bool IsChecked { get; set; }

        public NotificationOption Notification { get; set; }
    }
}
