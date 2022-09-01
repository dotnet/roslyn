// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.ExtractMethod;

// TODO: move to LSP layer
internal static class ExtractMethodOptionsStorage
{
    public static ExtractMethodOptions GetExtractMethodOptions(this IGlobalOptionService globalOptions, string language)
        => new(
            DontPutOutOrRefOnStruct: globalOptions.GetOption(DontPutOutOrRefOnStruct, language));

    public static readonly PerLanguageOption2<bool> DontPutOutOrRefOnStruct = new(
        "ExtractMethodOptions", "DontPutOutOrRefOnStruct", ExtractMethodOptions.Default.DontPutOutOrRefOnStruct,
        storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.Don't Put Out Or Ref On Strcut")); // NOTE: the spelling error is what we've shipped and thus should not change
}
