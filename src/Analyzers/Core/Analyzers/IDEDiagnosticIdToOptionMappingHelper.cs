// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
        private static readonly ConcurrentDictionary<string, ImmutableHashSet<IOption2>> s_diagnosticIdToOptionMap = new();
        private static readonly ConcurrentDictionary<string, PerLanguageOption2<bool>> s_diagnosticIdToFadingOptionMap = new();

        public static bool TryGetMappedOptions(string diagnosticId, [NotNullWhen(true)] out ImmutableHashSet<IOption2>? options)
            => s_diagnosticIdToOptionMap.TryGetValue(diagnosticId, out options);

        public static bool TryGetMappedFadingOption(string diagnosticId, [NotNullWhen(true)] out PerLanguageOption2<bool>? fadingOption)
            => s_diagnosticIdToFadingOptionMap.TryGetValue(diagnosticId, out fadingOption);

        public static bool IsKnownIDEDiagnosticId(string diagnosticId)
            => s_diagnosticIdToOptionMap.ContainsKey(diagnosticId);

        public static void AddOptionMapping(string diagnosticId, ImmutableHashSet<IOption2> options)
        {
            diagnosticId = diagnosticId ?? throw new ArgumentNullException(nameof(diagnosticId));
            options = options ?? throw new ArgumentNullException(nameof(options));

            // Verify that the option is either being added for the first time, or the existing option is already the same.
            // Latter can happen in tests as we re-instantiate the analyzer for every test, which attempts to add the mapping every time.
            Debug.Assert(!s_diagnosticIdToOptionMap.TryGetValue(diagnosticId, out var existingOptions) || options.SetEquals(existingOptions));

            s_diagnosticIdToOptionMap.TryAdd(diagnosticId, options);
        }

        public static void AddFadingOptionMapping(string diagnosticId, PerLanguageOption2<bool> fadingOption)
        {
            diagnosticId = diagnosticId ?? throw new ArgumentNullException(nameof(diagnosticId));
            fadingOption = fadingOption ?? throw new ArgumentNullException(nameof(fadingOption));

            // Verify that the option is either being added for the first time, or the existing option is already the same.
            // Latter can happen in tests as we re-instantiate the analyzer for every test, which attempts to add the mapping every time.
            Debug.Assert(!s_diagnosticIdToFadingOptionMap.TryGetValue(diagnosticId, out var existingOption) || existingOption.Equals(fadingOption));

            s_diagnosticIdToFadingOptionMap.TryAdd(diagnosticId, fadingOption);
        }
    }
}
