// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis
{
    internal static class AnalyzerConfigOptionsExtensions
    {
        public static T GetOption<T>(this AnalyzerConfigOptions analyzerConfigOptions, Option<T> option)
        {
            if (!TryGetEditorConfigOption(analyzerConfigOptions, option, out var value))
            {
                Debug.Fail("Failed to find a .editorconfig key for the option.");
                value = option.DefaultValue;
            }

            return value;
        }

        public static T GetOption<T>(this AnalyzerConfigOptions analyzerConfigOptions, PerLanguageOption<T> option)
        {
            if (!TryGetEditorConfigOption(analyzerConfigOptions, option, out var value))
            {
                Debug.Fail("Failed to find a .editorconfig key for the option.");
                value = option.DefaultValue;
            }

            return value;
        }

        public static bool TryGetEditorConfigOption<T>(this AnalyzerConfigOptions analyzerConfigOptions, Option<T> option, out T value)
        {
            return TryGetEditorConfigOption(analyzerConfigOptions, (IOption)option, out value);
        }

        public static bool TryGetEditorConfigOption<T>(this AnalyzerConfigOptions analyzerConfigOptions, PerLanguageOption<T> option, out T value)
        {
            return TryGetEditorConfigOption(analyzerConfigOptions, (IOption)option, out value);
        }

        public static bool TryGetEditorConfigOption<T>(this AnalyzerConfigOptions analyzerConfigOptions, IOption option, out T value)
        {
            foreach (var storageLocation in option.StorageLocations)
            {
                // This code path will avoid allocating a Dictionary wrapper since we can get direct access to the KeyName.
                if (storageLocation is EditorConfigStorageLocation<T> editorConfigStorageLocation &&
                    analyzerConfigOptions.TryGetValue(editorConfigStorageLocation.KeyName, out var stringValue) &&
                    editorConfigStorageLocation.TryGetOption(stringValue, typeof(T), out value))
                {
                    return true;
                }

                if (storageLocation is IEditorConfigStorageLocation configStorageLocation &&
                   configStorageLocation.TryGetOption(analyzerConfigOptions, option.Type, out var objectValue))
                {
                    value = (T)objectValue;
                    return true;
                }
            }

            value = default;
            return false;
        }
    }
}
