// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

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
