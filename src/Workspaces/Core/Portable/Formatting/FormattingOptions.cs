// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Formatting
{
    public static class FormattingOptions
    {
        internal const string TabFeatureName = "Tab";
        internal const string InternalTabFeatureName = "InternalTab";
        internal const string FormattingFeatureName = "Formatting";

        public static readonly PerLanguageOption<bool> UseTabs = new PerLanguageOption<bool>(TabFeatureName, "UseTab", defaultValue: false);

        public static readonly PerLanguageOption<int> TabSize = new PerLanguageOption<int>(TabFeatureName, "TabSize", defaultValue: 4);

        public static readonly PerLanguageOption<int> IndentationSize = new PerLanguageOption<int>(TabFeatureName, "IndentationSize", defaultValue: 4);

        public static readonly PerLanguageOption<IndentStyle> SmartIndent = new PerLanguageOption<IndentStyle>(TabFeatureName, "SmartIndent", defaultValue: IndentStyle.Smart);

        public static readonly PerLanguageOption<string> NewLine = new PerLanguageOption<string>(FormattingFeatureName, "NewLine", defaultValue: "\r\n");

        internal static readonly PerLanguageOption<bool> DebugMode = new PerLanguageOption<bool>(FormattingFeatureName, "DebugMode", defaultValue: false);

        internal static readonly Option<bool> AllowDisjointSpanMerging = new Option<bool>(FormattingFeatureName, "ShouldUseFormattingSpanCollapse", defaultValue: false);

        public enum IndentStyle
        {
            None = 0,
            Block = 1,
            Smart = 2
        }
    }
}
