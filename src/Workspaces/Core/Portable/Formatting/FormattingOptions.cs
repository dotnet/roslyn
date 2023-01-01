// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable RS0030 // Do not used banned APIs: PerLanguageOption<T>

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Formatting
{
    /// <inheritdoc cref="FormattingOptions2"/>
    public static partial class FormattingOptions
    {
        /// <inheritdoc cref="FormattingOptions2.UseTabs"/>
        public static PerLanguageOption<bool> UseTabs { get; } = FormattingOptions2.UseTabs.ToPublicOption();

        /// <inheritdoc cref="FormattingOptions2.TabSize"/>
        public static PerLanguageOption<int> TabSize { get; } = FormattingOptions2.TabSize.ToPublicOption();

        /// <inheritdoc cref="FormattingOptions2.IndentationSize"/>
        public static PerLanguageOption<int> IndentationSize { get; } = FormattingOptions2.IndentationSize.ToPublicOption();

        /// <inheritdoc cref="FormattingOptions2.NewLine"/>
        public static PerLanguageOption<string> NewLine { get; } = FormattingOptions2.NewLine.ToPublicOption();

        /// <inheritdoc cref="FormattingOptions2.IndentStyle"/>
        public static PerLanguageOption<IndentStyle> SmartIndent { get; } = (PerLanguageOption<IndentStyle>)FormattingOptions2.SmartIndent.PublicOption!;
    }
}
