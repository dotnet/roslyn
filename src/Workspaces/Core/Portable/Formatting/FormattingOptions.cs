// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Formatting
{
    public static partial class FormattingOptions
    {
        public static PerLanguageOption<bool> UseTabs { get; } = FormattingOptions2.UseTabs;

        // This is also serialized by the Visual Studio-specific LanguageSettingsPersister
        public static PerLanguageOption<int> TabSize { get; } = FormattingOptions2.TabSize;

        // This is also serialized by the Visual Studio-specific LanguageSettingsPersister
        public static PerLanguageOption<int> IndentationSize { get; } = FormattingOptions2.IndentationSize;

        // This is also serialized by the Visual Studio-specific LanguageSettingsPersister
        public static PerLanguageOption<IndentStyle> SmartIndent { get; } = FormattingOptions2.SmartIndent;

        public static PerLanguageOption<string> NewLine { get; } = FormattingOptions2.NewLine;

        internal static Option<bool> InsertFinalNewLine { get; } = FormattingOptions2.InsertFinalNewLine;

        internal static Option<int> PreferredWrappingColumn { get; } = FormattingOptions2.PreferredWrappingColumn;

        internal static Option<bool> AllowDisjointSpanMerging { get; } = FormattingOptions2.AllowDisjointSpanMerging;

        internal static readonly PerLanguageOption<bool> AutoFormattingOnReturn = FormattingOptions2.AutoFormattingOnReturn;
    }
}
