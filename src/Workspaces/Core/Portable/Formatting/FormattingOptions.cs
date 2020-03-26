// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Formatting
{
    /// <inheritdoc cref="FormattingOptions2"/>
    public static partial class FormattingOptions
    {
        /// <inheritdoc cref="FormattingOptions2.UseTabs"/>
        // Suppression due to https://github.com/dotnet/roslyn/issues/42614
        public static PerLanguageOption<bool> UseTabs { get; } = ((PerLanguageOption<bool>)FormattingOptions2.UseTabs)!;

        /// <inheritdoc cref="FormattingOptions2.TabSize"/>
        // Suppression due to https://github.com/dotnet/roslyn/issues/42614
        public static PerLanguageOption<int> TabSize { get; } = ((PerLanguageOption<int>)FormattingOptions2.TabSize)!;

        /// <inheritdoc cref="FormattingOptions2.IndentationSize"/>
        // Suppression due to https://github.com/dotnet/roslyn/issues/42614
        public static PerLanguageOption<int> IndentationSize { get; } = ((PerLanguageOption<int>)FormattingOptions2.IndentationSize)!;

        /// <inheritdoc cref="FormattingOptions2.SmartIndent"/>
        // Suppression due to https://github.com/dotnet/roslyn/issues/42614
        public static PerLanguageOption<IndentStyle> SmartIndent { get; } = ((PerLanguageOption<IndentStyle>)FormattingOptions2.SmartIndent)!;

        /// <inheritdoc cref="FormattingOptions2.NewLine"/>
        // Suppression due to https://github.com/dotnet/roslyn/issues/42614
        public static PerLanguageOption<string> NewLine { get; } = ((PerLanguageOption<string>)FormattingOptions2.NewLine)!;

        /// <inheritdoc cref="FormattingOptions2.InsertFinalNewLine"/>
        // Suppression due to https://github.com/dotnet/roslyn/issues/42614
        internal static Option<bool> InsertFinalNewLine { get; } = ((Option<bool>)FormattingOptions2.InsertFinalNewLine)!;

        /// <inheritdoc cref="FormattingOptions2.PreferredWrappingColumn"/>
        // Suppression due to https://github.com/dotnet/roslyn/issues/42614
        internal static Option<int> PreferredWrappingColumn { get; } = ((Option<int>)FormattingOptions2.PreferredWrappingColumn)!;

        /// <inheritdoc cref="FormattingOptions2.AllowDisjointSpanMerging"/>
        // Suppression due to https://github.com/dotnet/roslyn/issues/42614
        internal static Option<bool> AllowDisjointSpanMerging { get; } = ((Option<bool>)FormattingOptions2.AllowDisjointSpanMerging)!;

        /// <inheritdoc cref="FormattingOptions2.AutoFormattingOnReturn"/>
        // Suppression due to https://github.com/dotnet/roslyn/issues/42614
        internal static readonly PerLanguageOption<bool> AutoFormattingOnReturn = ((PerLanguageOption<bool>)FormattingOptions2.AutoFormattingOnReturn)!;
    }
}
