// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.QuickInfo
{
    internal static class QuickInfoOptions
    {
        public static readonly PerLanguageOption2<bool> ShowRemarksInQuickInfo = new PerLanguageOption2<bool>(nameof(QuickInfoOptions), nameof(ShowRemarksInQuickInfo), defaultValue: true,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ShowRemarks"));
    }
}
