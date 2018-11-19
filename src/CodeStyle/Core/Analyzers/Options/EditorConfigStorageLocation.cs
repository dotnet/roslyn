// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Options
{
    internal static class EditorConfigStorageLocation
    {
        public static EditorConfigStorageLocation<bool> ForBoolOption(string keyName)
            => new EditorConfigStorageLocation<bool>(keyName, s_parseBool, s_getBoolEditorConfigStringForValue);

        public static EditorConfigStorageLocation<int> ForInt32Option(string keyName)
            => new EditorConfigStorageLocation<int>(keyName, s_parseInt32, s_getInt32EditorConfigStringForValue);

        private static readonly Func<string, Optional<bool>> s_parseBool = ParseBool;
        private static Optional<bool> ParseBool(string str)
            => bool.TryParse(str, out var result) ? result : new Optional<bool>();
        private static readonly Func<bool, string> s_getBoolEditorConfigStringForValue = GetBoolEditorConfigStringForValue;
        private static string GetBoolEditorConfigStringForValue(bool value) => value.ToString().ToLowerInvariant();

        private static readonly Func<string, Optional<int>> s_parseInt32 = ParseInt32;
        private static Optional<int> ParseInt32(string str)
            => int.TryParse(str, out var result) ? result : new Optional<int>();
        private static readonly Func<int, string> s_getInt32EditorConfigStringForValue = GetInt32EditorConfigStringForValue;
        private static string GetInt32EditorConfigStringForValue(int value) => value.ToString().ToLowerInvariant();
    }
}
