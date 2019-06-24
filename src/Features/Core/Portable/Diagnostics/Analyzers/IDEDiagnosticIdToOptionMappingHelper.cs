// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Helper type to map <see cref="IDEDiagnosticIds"/> to an unique editorconfig code style option, if any,
    /// such that diagnostic's severity can be configured in .editorconfig with an entry such as:
    ///     "%option_name% = %option_value%:%severity%
    /// </summary>
    internal static class IDEDiagnosticIdToOptionMappingHelper
    {
        private static readonly ConcurrentDictionary<string, ImmutableHashSet<IOption>> s_diagnosticIdToOptionMap = new ConcurrentDictionary<string, ImmutableHashSet<IOption>>();
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ImmutableHashSet<IOption>>> s_diagnosticIdToLanguageSpecificOptionsMap = new ConcurrentDictionary<string, ConcurrentDictionary<string, ImmutableHashSet<IOption>>>();

        public static bool TryGetMappedOptions(string diagnosticId, string language, out ImmutableHashSet<IOption> options)
            => s_diagnosticIdToOptionMap.TryGetValue(diagnosticId, out options) ||
               (s_diagnosticIdToLanguageSpecificOptionsMap.TryGetValue(language, out var map) &&
                map.TryGetValue(diagnosticId, out options));

        public static void AddOptionMapping(string diagnosticId, ImmutableHashSet<IPerLanguageOption> perLanguageOptions)
        {
            diagnosticId = diagnosticId ?? throw new ArgumentNullException(nameof(diagnosticId));
            perLanguageOptions = perLanguageOptions ?? throw new ArgumentNullException(nameof(perLanguageOptions));

            var options = perLanguageOptions.Cast<IOption>().ToImmutableHashSet();
            AddOptionMapping(s_diagnosticIdToOptionMap, diagnosticId, options);
        }
        public static void AddOptionMapping(string diagnosticId, ImmutableHashSet<ILanguageSpecificOption> languageSpecificOptions, string language)
        {
            diagnosticId = diagnosticId ?? throw new ArgumentNullException(nameof(diagnosticId));
            languageSpecificOptions = languageSpecificOptions ?? throw new ArgumentNullException(nameof(languageSpecificOptions));
            language = language ?? throw new ArgumentNullException(nameof(language));

            var map = s_diagnosticIdToLanguageSpecificOptionsMap.GetOrAdd(language, _ => new ConcurrentDictionary<string, ImmutableHashSet<IOption>>());
            var options = languageSpecificOptions.Cast<IOption>().ToImmutableHashSet();
            AddOptionMapping(map, diagnosticId, options);
        }

        private static void AddOptionMapping(ConcurrentDictionary<string, ImmutableHashSet<IOption>> map, string diagnosticId, ImmutableHashSet<IOption> options)
        {
            // Verify that the option is either being added for the first time, or the existing option is already the same.
            // Latter can happen in tests as we re-instantiate the analyzer for every test, which attempts to add the mapping every time.
            Debug.Assert(!map.TryGetValue(diagnosticId, out var existingOptions) || options.SetEquals(existingOptions));

            map.TryAdd(diagnosticId, options);
        }
    }
}
