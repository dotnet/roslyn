// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis
{
    internal static class AnalyzerConfigOptionsExtensions
    {
        public static bool TryGetOption<T>(this AnalyzerConfigOptions analyzerConfigOptions, Option<T> option, out T value)
        {
            return TryGetOption(analyzerConfigOptions, (IOption)option, out value);
        }

        public static bool TryGetOption<T>(this AnalyzerConfigOptions analyzerConfigOptions, PerLanguageOption<T> option, out T value)
        {
            return TryGetOption(analyzerConfigOptions, option, out value);
        }

        public static bool TryGetOption<T>(this AnalyzerConfigOptions analyzerConfigOptions, IOption option, out T value)
        {
            foreach (var storageLocation in option.StorageLocations)
            {
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

            throw new ArgumentException("AnalyzerConfigOptions can only be queried with Options that use .editorconfig as a storage location.", nameof(option));
        }
    }
}
