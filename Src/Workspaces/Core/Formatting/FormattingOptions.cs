// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Formatting
{
    public static class FormattingOptions
    {
        internal const string TabFeatureName = "Tab";
        internal const string InternalTabFeatureName = "InternalTab";

        // All Languages
#if MEF
        [ExportOption]
#endif
        public static readonly PerLanguageOption<bool> UseTabs = new PerLanguageOption<bool>(TabFeatureName, "UseTab", defaultValue: false);

#if MEF
        [ExportOption]
#endif
        public static readonly PerLanguageOption<int> TabSize = new PerLanguageOption<int>(TabFeatureName, "TabSize", defaultValue: 4);

#if MEF
        [ExportOption]
#endif
        public static readonly PerLanguageOption<int> IndentationSize = new PerLanguageOption<int>(TabFeatureName, "IndentationSize", defaultValue: 4);

#if MEF
        [ExportOption]
#endif
        public static readonly PerLanguageOption<IndentStyle> SmartIndent = new PerLanguageOption<IndentStyle>(TabFeatureName, "SmartIndent", defaultValue: IndentStyle.Smart);

#if MEF
        [ExportOption]
#endif
        public static readonly PerLanguageOption<bool> UseTabOnlyForIndentation = new PerLanguageOption<bool>(InternalTabFeatureName, "UseTabOnlyForIndentation", defaultValue: false);

#if MEF
        [ExportOption]
#endif
        public static readonly PerLanguageOption<string> NewLine = new PerLanguageOption<string>(TabFeatureName, "NewLine", defaultValue: "\r\n");

#if MEF
        [ExportOption]
#endif
        internal static readonly PerLanguageOption<bool> DebugMode = new PerLanguageOption<bool>(InternalTabFeatureName, "DebugMode", defaultValue: false);

        public enum IndentStyle
        {
            None,
            Block,
            Smart
        }
    }
}
