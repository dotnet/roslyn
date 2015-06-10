// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Formatting
{
    public static class FormattingOptions
    {
        internal const string TabFeatureName = "Tab";
        internal const string InternalTabFeatureName = "InternalTab";
        internal const string FormattingFeatureName = "Formatting";

        public static PerLanguageOption<bool> UseTabs { get; } = new PerLanguageOption<bool>(TabFeatureName, "UseTab", defaultValue: false);

        public static PerLanguageOption<int> TabSize { get; } = new PerLanguageOption<int>(TabFeatureName, "TabSize", defaultValue: 4);

        public static PerLanguageOption<int> IndentationSize { get; } = new PerLanguageOption<int>(TabFeatureName, "IndentationSize", defaultValue: 4);

        public static PerLanguageOption<IndentStyle> SmartIndent { get; } = new PerLanguageOption<IndentStyle>(TabFeatureName, "SmartIndent", defaultValue: IndentStyle.Smart);

        public static PerLanguageOption<string> NewLine { get; } = new PerLanguageOption<string>(FormattingFeatureName, "NewLine", defaultValue: "\r\n");

        internal static PerLanguageOption<bool> DebugMode { get; } = new PerLanguageOption<bool>(FormattingFeatureName, "DebugMode", defaultValue: false);

        internal static Option<bool> AllowDisjointSpanMerging { get; } = new Option<bool>(FormattingFeatureName, "ShouldUseFormattingSpanCollapse", defaultValue: false);

        public enum IndentStyle
        {
            None = 0,
            Block = 1,
            Smart = 2
        }
    }
}
