// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    internal static class VisualStudioNavigationOptions
    {
        public static readonly PerLanguageOption2<bool> NavigateToObjectBrowser = new(nameof(VisualStudioNavigationOptions), nameof(NavigateToObjectBrowser), defaultValue: false,
            storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.NavigateToObjectBrowser"));
    }
}
