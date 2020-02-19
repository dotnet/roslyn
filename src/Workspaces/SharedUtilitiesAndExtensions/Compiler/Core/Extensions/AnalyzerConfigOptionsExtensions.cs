// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;

#if CODE_STYLE
using Microsoft.CodeAnalysis.Internal.Options;
#else
using Microsoft.CodeAnalysis.Options;
#endif

namespace Microsoft.CodeAnalysis
{
    internal static partial class AnalyzerConfigOptionsExtensions
    {
        public static T GetOption<T>(this AnalyzerConfigOptions optionSet, Option<T> option)
        {
            if (!TryGetEditorConfigOption(optionSet, option, out T value))
            {
                value = option.DefaultValue;
            }

            return value;
        }

        public static T GetOption<T>(this AnalyzerConfigOptions optionSet, PerLanguageOption<T> option, string language)
        {
            if (!TryGetEditorConfigOption(optionSet, option, out T value))
            {
                value = option.DefaultValue;
            }

            return value;
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
