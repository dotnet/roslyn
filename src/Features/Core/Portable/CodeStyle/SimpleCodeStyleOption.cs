// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CodeStyle
{
    internal class SimpleCodeStyleOption
    {
        public static readonly SimpleCodeStyleOption Default = new SimpleCodeStyleOption(false, NotificationOption.None);

        public SimpleCodeStyleOption(bool isChecked, NotificationOption notification)
        {
            IsChecked = isChecked;
            Notification = notification;
        }

        public bool IsChecked { get; set; }

        public NotificationOption Notification { get; set; }
    }
}
