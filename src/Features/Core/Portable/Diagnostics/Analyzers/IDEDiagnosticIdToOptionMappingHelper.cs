// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
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
        private static readonly ConcurrentDictionary<string, IOption> s_diagnosticIdToOptionMap = new ConcurrentDictionary<string, IOption>();
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, IOption>> s_diagnosticIdToLanguageSpecificOptionsMap = new ConcurrentDictionary<string, ConcurrentDictionary<string, IOption>>();

        public static bool TryGetMappedOption(string diagnosticId, string language, out IOption option)
            => s_diagnosticIdToOptionMap.TryGetValue(diagnosticId, out option) ||
               (s_diagnosticIdToLanguageSpecificOptionsMap.TryGetValue(language, out var map) &&
                map.TryGetValue(diagnosticId, out option));

        public static void AddOptionMapping(string diagnosticId, IOption option, string languageOpt)
        {
            diagnosticId = diagnosticId ?? throw new ArgumentNullException(nameof(diagnosticId));
            option = option ?? throw new ArgumentNullException(nameof(option));

            var map = languageOpt != null
                ? s_diagnosticIdToLanguageSpecificOptionsMap.GetOrAdd(languageOpt, _ => new ConcurrentDictionary<string, IOption>())
                : s_diagnosticIdToOptionMap;

            // Verify that the option is either being added for the first time, or the existing option is already the same.
            // Latter can happen in tests as we re-instantiate the analyzer for every test, which attempts to add the mapping every time.
            Debug.Assert(!map.TryGetValue(diagnosticId, out var existingOption) || option == existingOption);

            map.TryAdd(diagnosticId, option);
        }
    }
}
