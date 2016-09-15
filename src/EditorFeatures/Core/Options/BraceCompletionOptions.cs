// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.Options
{
    internal static class BraceCompletionOptions
    {
        // This is serialized by the Visual Studio-specific LanguageSettingsSerializer
        [ExportOption]
        public static readonly PerLanguageOption<bool> EnableBraceCompletion = new PerLanguageOption<bool>(nameof(BraceCompletionOptions), nameof(EnableBraceCompletion), defaultValue: true);
    }
}
