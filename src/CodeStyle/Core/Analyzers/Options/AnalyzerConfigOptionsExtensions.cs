// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    internal static class AnalyzerConfigOptionsExtensions
    {
        public static T GetOption<T>(this AnalyzerConfigOptions analyzerConfigOptions, PerLanguageOption<T> option, string language)
        {
            foreach (var storageLocation in option.StorageLocations)
            {
                if (!(storageLocation is EditorConfigStorageLocation<T> editorConfigStorageLocation))
                {
                    continue;
                }

                if (!analyzerConfigOptions.TryGetValue(editorConfigStorageLocation.KeyName, out var stringValue))
                {
                    continue;
                }

                if (editorConfigStorageLocation.TryGetOption(
                    underlyingOption: null,
                    allRawConventions: new Dictionary<string, object> { { editorConfigStorageLocation.KeyName, stringValue } },
                    typeof(T),
                    out var value))
                {
                    return (T)value;
                }
            }

            return option.DefaultValue;
        }

        public static T GetOption<T>(this AnalyzerConfigOptions analyzerConfigOptions, Option<T> option)
        {
            foreach (var storageLocation in option.StorageLocations)
            {
                if (!(storageLocation is EditorConfigStorageLocation<T> editorConfigStorageLocation))
                {
                    continue;
                }

                if (!analyzerConfigOptions.TryGetValue(editorConfigStorageLocation.KeyName, out var stringValue))
                {
                    continue;
                }

                if (editorConfigStorageLocation.TryGetOption(
                    underlyingOption: null,
                    allRawConventions: new Dictionary<string, object> { { editorConfigStorageLocation.KeyName, stringValue } },
                    typeof(T),
                    out var value))
                {
                    return (T)value;
                }
            }

            return option.DefaultValue;
        }
    }
}
