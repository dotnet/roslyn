// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.ExtractMethod
{
    internal static class ExtractMethodPresentationOptionsStorage
    {
        // Deprecated.  Never exposed in the UI to users in any way.  Kept around to still support automation
        // getting/setting our storage values from the user's profile.
        public static readonly PerLanguageOption2<bool> AllowBestEffort = new("dotnet_allow_best_effort_when_extracting_method", defaultValue: true);
    }
}
