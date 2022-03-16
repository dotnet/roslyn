// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        /// <inheritdoc cref="FormattingOptions2.NewLine"/>
        // Suppression due to https://github.com/dotnet/roslyn/issues/42614
        public static PerLanguageOption<string> NewLine { get; } = ((PerLanguageOption<string>)FormattingOptions2.NewLine)!;

        // This is also serialized by the Visual Studio-specific LanguageSettingsPersister

        internal static readonly PerLanguageOption2<IndentStyle> SmartIndent2 = new(
            "FormattingOptions", FormattingOptionGroups.IndentationAndSpacing, "SmartIndent", defaultValue: IndentStyle.Smart);

        /// <inheritdoc cref="AutoFormattingOptions.IndentStyle"/>
        // Suppression due to https://github.com/dotnet/roslyn/issues/42614
        public static PerLanguageOption<IndentStyle> SmartIndent { get; } = (PerLanguageOption<IndentStyle>)SmartIndent2!;
    }
}
