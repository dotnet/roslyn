// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Options
{
    internal static class EditorConfigStorageLocationExtensions
    {
        public static bool TryGetOption(this IEditorConfigStorageLocation editorConfigStorageLocation, AnalyzerConfigOptions analyzerConfigOptions, Type type, out object value)
        {
            // This is a workaround until we have an API for enumeratings AnalyzerConfigOptions. See https://github.com/dotnet/roslyn/issues/41840
            var backingField = analyzerConfigOptions.GetType().GetField("_backing", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var backing = backingField?.GetValue(analyzerConfigOptions);

            if (backing is IReadOnlyDictionary<string, string> backingDictionary)
            {
                return editorConfigStorageLocation.TryGetOption(backingDictionary, type, out value);
            }

            value = null;
            return false;
        }
    }
}
