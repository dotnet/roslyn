// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.ExtractMethod
{
    internal static class ExtractMethodOptions
    {
        public static readonly PerLanguageOption<bool> AllowBestEffort = new PerLanguageOption<bool>(nameof(ExtractMethodOptions), nameof(AllowBestEffort), defaultValue: true,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.Allow Best Effort"));

        public static readonly PerLanguageOption<bool> DontPutOutOrRefOnStruct = new PerLanguageOption<bool>(nameof(ExtractMethodOptions), nameof(DontPutOutOrRefOnStruct), defaultValue: true,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.Don't Put Out Or Ref On Strcut")); // NOTE: the spelling error is what we've shipped and thus should not change

        public static readonly PerLanguageOption<bool> AllowMovingDeclaration = new PerLanguageOption<bool>(nameof(ExtractMethodOptions), nameof(AllowMovingDeclaration), defaultValue: false,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.Allow Moving Declaration"));
    }
}
