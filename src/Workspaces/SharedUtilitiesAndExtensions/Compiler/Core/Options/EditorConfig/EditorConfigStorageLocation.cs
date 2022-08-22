// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.EditorConfigSettings;

namespace Microsoft.CodeAnalysis.Options
{
    internal static class EditorConfigStorageLocation
    {
        public static EditorConfigStorageLocation<CodeStyleOption2<bool>> ForBoolCodeStyleOption(EditorConfigData<bool> editorConfigData, CodeStyleOption2<bool> defaultValue)
            => new(editorConfigData.GetSettingName(), str => ParseBoolCodeStyleOption(str, defaultValue, editorConfigData), value => GetBoolCodeStyleOptionEditorConfigStringForValue(value, defaultValue, editorConfigData));

        public static EditorConfigStorageLocation<CodeStyleOption2<string>> ForStringCodeStyleOption(EditorConfigData<string> editorConfigData, CodeStyleOption2<string> defaultValue)
            => new(editorConfigData.GetSettingName(), str => ParseStringCodeStyleOption(str, defaultValue), value => GetStringCodeStyleOptionEditorConfigStringForValue(value, defaultValue));

        private static Optional<CodeStyleOption2<bool>> ParseBoolCodeStyleOption(string str, CodeStyleOption2<bool> defaultValue, EditorConfigData<bool> editorConfigData)
            => CodeStyleHelpers.TryParseBoolEditorConfigCodeStyleOption(str, defaultValue, out var result, editorConfigData) ? result : new Optional<CodeStyleOption2<bool>>();
        private static string GetBoolCodeStyleOptionEditorConfigStringForValue(CodeStyleOption2<bool> value, CodeStyleOption2<bool> defaultValue, EditorConfigData<bool> editorConfigData)
            => $"{editorConfigData.GetEditorConfigStringFromValue(value.Value)}{CodeStyleHelpers.GetEditorConfigStringNotificationPart(value, defaultValue)}";

        private static Optional<CodeStyleOption2<string>> ParseStringCodeStyleOption(string str, CodeStyleOption2<string> defaultValue)
            => CodeStyleHelpers.TryParseStringEditorConfigCodeStyleOption(str, defaultValue, out var result) ? result : new Optional<CodeStyleOption2<string>>();
        private static string GetStringCodeStyleOptionEditorConfigStringForValue(CodeStyleOption2<string> value, CodeStyleOption2<string> defaultValue)
            => $"{value.Value.ToLowerInvariant()}{CodeStyleHelpers.GetEditorConfigStringNotificationPart(value, defaultValue)}";
    }
}
