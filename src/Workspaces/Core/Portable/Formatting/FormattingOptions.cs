// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Formatting
{
    public static partial class FormattingOptions
    {
        public static PerLanguageOption<bool> UseTabs { get; } = (PerLanguageOption<bool>)FormattingOptions2.UseTabs;

        // This is also serialized by the Visual Studio-specific LanguageSettingsPersister
        public static PerLanguageOption<int> TabSize { get; } = (PerLanguageOption<int>)FormattingOptions2.TabSize;

        // This is also serialized by the Visual Studio-specific LanguageSettingsPersister
        public static PerLanguageOption<int> IndentationSize { get; } = (PerLanguageOption<int>)FormattingOptions2.IndentationSize;

        // This is also serialized by the Visual Studio-specific LanguageSettingsPersister
        public static PerLanguageOption<IndentStyle> SmartIndent { get; } = (PerLanguageOption<IndentStyle>)FormattingOptions2.SmartIndent;

        public static PerLanguageOption<string> NewLine { get; } = (PerLanguageOption<string>)FormattingOptions2.NewLine;

        internal static Option<bool> InsertFinalNewLine { get; } = (Option<bool>)FormattingOptions2.InsertFinalNewLine;

        internal static Option<int> PreferredWrappingColumn { get; } = (Option<int>)FormattingOptions2.PreferredWrappingColumn;

        internal static Option<bool> AllowDisjointSpanMerging { get; } = (Option<bool>)FormattingOptions2.AllowDisjointSpanMerging;

        internal static readonly PerLanguageOption<bool> AutoFormattingOnReturn = (PerLanguageOption<bool>)FormattingOptions2.AutoFormattingOnReturn;
    }
}
