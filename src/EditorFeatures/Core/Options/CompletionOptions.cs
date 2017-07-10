// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.Options
{
    internal static class EditorCompletionOptions
    {
        public const string FeatureName = "EditorCompletion";

        // Intentionally not persisted
        [ExportOption]
        public static readonly Option<bool> UseSuggestionMode = new Option<bool>(FeatureName, nameof(UseSuggestionMode), defaultValue: false);

        // Default into suggestion mode in the watch/immediate windows but respect the
        // user's preferences if they switch away from it.
        // Intentionally not persisted
        [ExportOption]
        public static readonly Option<bool> UseSuggestionMode_Debugger = new Option<bool>(FeatureName, nameof(UseSuggestionMode_Debugger), defaultValue: true);
    }
}
