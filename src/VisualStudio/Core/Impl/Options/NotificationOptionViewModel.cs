// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
