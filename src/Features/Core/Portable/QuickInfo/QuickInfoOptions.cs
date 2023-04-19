// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.QuickInfo
{
    internal static class QuickInfoOptions
    {
        public static readonly PerLanguageOption2<bool> ShowRemarksInQuickInfo = new(nameof(QuickInfoOptions), nameof(ShowRemarksInQuickInfo), defaultValue: true,
            storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ShowRemarks"));

        public static readonly Option2<bool> IncludeNavigationHintsInQuickInfo = new(nameof(QuickInfoOptions), nameof(IncludeNavigationHintsInQuickInfo), defaultValue: true,
            storageLocation: new RoamingProfileStorageLocation("TextEditor.Specific.IncludeNavigationHintsInQuickInfo"));
    }
}
