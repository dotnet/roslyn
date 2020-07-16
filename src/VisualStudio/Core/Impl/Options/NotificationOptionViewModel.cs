// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    /// <summary>
    /// Represents a view model for <see cref="NotificationOption"/>
    /// </summary>
    internal class NotificationOptionViewModel
    {
        public NotificationOptionViewModel(NotificationOption notification, ImageMoniker moniker)
        {
            Notification = notification;
            Name = notification.Name;
            Moniker = moniker;
        }

        public ImageMoniker Moniker { get; }

        public string Name { get; }

        public NotificationOption Notification { get; }
    }
}
