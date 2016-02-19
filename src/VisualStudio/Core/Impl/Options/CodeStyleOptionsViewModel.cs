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
    internal class CodeStyleOptionsViewModel
    {
        public CodeStyleOptionsViewModel(NotificationOption notification, ImageMoniker moniker, bool isChecked = false)
        {
            Notification = notification;
            Name = notification.Name;
            Moniker = moniker;
            Checked = isChecked;
        }

        public ImageMoniker Moniker { get; }

        public string Name { get; }

        public NotificationOption Notification { get; }

        // TODO: rename to IsChecked if it doesn't muck up WPF binding
        // Or even remove this class?
        public bool Checked { get; }
    }
}
