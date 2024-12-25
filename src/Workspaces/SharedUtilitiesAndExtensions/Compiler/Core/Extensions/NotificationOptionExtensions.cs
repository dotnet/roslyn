// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CodeStyle;

internal static class NotificationOptionExtensions
{
    public static string ToEditorConfigString(this NotificationOption2 notificationOption)
        => notificationOption.Severity.ToEditorConfigString();
}
