﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options.Style.NamingPreferences
{
    internal interface INamingStylesInfoDialogViewModel
    {
        string ItemName { get; set; }
        bool CanBeDeleted { get; set; }
    }
}
