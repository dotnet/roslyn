// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Notification;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.Notification;

internal enum OmniSharpNotificationSeverity
{
    Information = NotificationSeverity.Information,
    Warning = NotificationSeverity.Warning,
    Error = NotificationSeverity.Error
}
