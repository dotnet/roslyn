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

        public static bool TryGetMappedOption(string diagnosticId, out IOption option)
            => s_diagnosticIdToOptionMap.TryGetValue(diagnosticId, out option);

        public static void AddOptionMapping(string diagnosticId, IOption option)
        {
            diagnosticId = diagnosticId ?? throw new ArgumentNullException(nameof(diagnosticId));
            option = option ?? throw new ArgumentNullException(nameof(option));

            Debug.Assert(!s_diagnosticIdToOptionMap.TryGetValue(diagnosticId, out var existingOption) || option == existingOption);
            s_diagnosticIdToOptionMap.TryAdd(diagnosticId, option);
        }
    }
}
