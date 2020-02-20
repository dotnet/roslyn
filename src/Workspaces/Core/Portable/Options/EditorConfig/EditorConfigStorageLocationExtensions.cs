// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Options
{
    internal static class EditorConfigStorageLocationExtensions
    {
        public static bool TryGetOption(this IEditorConfigStorageLocation editorConfigStorageLocation, AnalyzerConfigOptions analyzerConfigOptions, Type type, out object value)
        {
            var optionDictionary = analyzerConfigOptions.Keys.ToImmutableDictionary(
                key => key,
                key =>
                {
                    analyzerConfigOptions.TryGetValue(key, out var optionValue);
                    return optionValue;
                });

            return editorConfigStorageLocation.TryGetOption(optionDictionary, type, out value);
        }
    }
}
