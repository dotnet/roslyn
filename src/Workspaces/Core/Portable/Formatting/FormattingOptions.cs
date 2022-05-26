// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Formatting
{
    /// <inheritdoc cref="FormattingOptions2"/>
    public static partial class FormattingOptions
    {
        /// <inheritdoc cref="FormattingOptions2.UseTabs"/>
        public static PerLanguageOption<bool> UseTabs { get; } = (PerLanguageOption<bool>)FormattingOptions2.UseTabs;

        /// <inheritdoc cref="FormattingOptions2.TabSize"/>
        public static PerLanguageOption<int> TabSize { get; } = (PerLanguageOption<int>)FormattingOptions2.TabSize;

        /// <inheritdoc cref="FormattingOptions2.IndentationSize"/>
        public static PerLanguageOption<int> IndentationSize { get; } = (PerLanguageOption<int>)FormattingOptions2.IndentationSize;

        /// <inheritdoc cref="FormattingOptions2.NewLine"/>
        public static PerLanguageOption<string> NewLine { get; } = (PerLanguageOption<string>)FormattingOptions2.NewLine;

        /// <inheritdoc cref="FormattingOptions2.IndentStyle"/>
        public static PerLanguageOption<IndentStyle> SmartIndent { get; } = new(
            FormattingOptions2.SmartIndent.OptionDefinition,
            FormattingOptions2.SmartIndent.StorageLocations.As<OptionStorageLocation>());
    }
}
