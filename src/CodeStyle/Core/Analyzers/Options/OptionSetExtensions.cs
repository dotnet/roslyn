// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;
using OptionSet = Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions;

namespace Microsoft.CodeAnalysis
{
    internal static class OptionSetExtensions
    {
        public static T GetOption<T>(this OptionSet optionSet, Option<T> option)
        {
            if (!TryGetEditorConfigOption(optionSet, option, out T value))
            {
                value = option.DefaultValue;
            }

            return value;
        }

        public static T GetOption<T>(this OptionSet optionSet, PerLanguageOption<T> option, string language)
        {
            if (!TryGetEditorConfigOption(optionSet, option, out T value))
            {
                value = option.DefaultValue;
            }

            return value;
        }

        private static bool TryGetEditorConfigOption<T>(this OptionSet optionSet, IOption option, out T value)
        {
            foreach (var storageLocation in option.StorageLocations)
            {
                if (!(storageLocation is EditorConfigStorageLocation<T> editorConfigStorageLocation))
                {
                    continue;
                }

                if (!optionSet.TryGetValue(editorConfigStorageLocation.KeyName, out var stringValue))
                {
                    continue;
                }

                if (editorConfigStorageLocation.TryGetOption(stringValue, typeof(T), out value))
                {
                    return true;
                }
            }

            value = default;
            return false;
        }
    }
}
