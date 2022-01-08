// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Options
{
    internal static class EditorConfigStorageLocationExtensions
    {
        public static bool TryGetOption(this IEditorConfigStorageLocation editorConfigStorageLocation, AnalyzerConfigOptions analyzerConfigOptions, Type type, out object? value)
        {
            // This is a workaround until we have an API for enumeratings AnalyzerConfigOptions. See https://github.com/dotnet/roslyn/issues/41840
            if (analyzerConfigOptions.GetType().FullName == typeof(DictionaryAnalyzerConfigOptions).FullName)
            {
                var optionsField = analyzerConfigOptions.GetType().GetField(nameof(DictionaryAnalyzerConfigOptions.Options), BindingFlags.NonPublic | BindingFlags.Instance);
                Contract.ThrowIfNull(optionsField);

                var options = optionsField.GetValue(analyzerConfigOptions);
                Contract.ThrowIfNull(options);

                return editorConfigStorageLocation.TryGetOption((ImmutableDictionary<string, string?>)options, type, out value);
            }

            value = null;
            return false;
        }
    }
}
