﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CodeStyle
{
    internal static class NotificationOptionExtensions
    {
        public static string ToEditorConfigString(this NotificationOption notificationOption)
        {
            if (notificationOption == NotificationOption.Silent)
            {
                return nameof(NotificationOption.Silent).ToLowerInvariant();
            }
            else
            {
                return notificationOption.ToString().ToLowerInvariant();
            }
        }
    }
}
