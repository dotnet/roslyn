// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.ExtractMethod
{
    internal static class ExtractMethodOptions
    {
        public static readonly PerLanguageOption2<bool> AllowBestEffort = new PerLanguageOption2<bool>(nameof(ExtractMethodOptions), nameof(AllowBestEffort), defaultValue: true,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.Allow Best Effort"));

        public static readonly PerLanguageOption2<bool> DontPutOutOrRefOnStruct = new PerLanguageOption2<bool>(nameof(ExtractMethodOptions), nameof(DontPutOutOrRefOnStruct), defaultValue: true,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.Don't Put Out Or Ref On Strcut")); // NOTE: the spelling error is what we've shipped and thus should not change
    }
}
