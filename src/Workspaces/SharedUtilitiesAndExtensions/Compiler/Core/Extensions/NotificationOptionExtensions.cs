// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CodeStyle;

internal static class NotificationOptionExtensions
{
    extension(NotificationOption2 notificationOption)
    {
        public string ToEditorConfigString()
        => notificationOption.Severity.ToEditorConfigString();
    }
}
