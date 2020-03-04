// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.Editor.Options
{
    internal static class EditorCompletionOptions
    {
        public const string FeatureName = "EditorCompletion";

        // Intentionally not persisted
        public static readonly Option<bool> UseSuggestionMode = new Option<bool>(FeatureName, nameof(UseSuggestionMode), defaultValue: false);

        // Default into suggestion mode in the watch/immediate windows but respect the
        // user's preferences if they switch away from it.
        // Intentionally not persisted
        public static readonly Option<bool> UseSuggestionMode_Debugger = new Option<bool>(FeatureName, nameof(UseSuggestionMode_Debugger), defaultValue: true);
    }

    [ExportOptionProvider, Shared]
    internal class EditorCompletionOptionsProvider : IOptionProvider
    {
        [ImportingConstructor]
        public EditorCompletionOptionsProvider()
        {
        }

        public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
            EditorCompletionOptions.UseSuggestionMode,
            EditorCompletionOptions.UseSuggestionMode_Debugger);
    }
}
