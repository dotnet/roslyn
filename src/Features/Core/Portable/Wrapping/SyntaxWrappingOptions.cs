// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.CodeAnalysis.Wrapping;

internal abstract class SyntaxWrappingOptions
{
    public readonly SyntaxFormattingOptions FormattingOptions;
    public readonly OperatorPlacementWhenWrappingPreference OperatorPlacement;

    protected SyntaxWrappingOptions(
        SyntaxFormattingOptions formattingOptions,
        OperatorPlacementWhenWrappingPreference operatorPlacement)
    {
        FormattingOptions = formattingOptions;
        OperatorPlacement = operatorPlacement;
    }

    public bool UseTabs => FormattingOptions.UseTabs;
    public int TabSize => FormattingOptions.TabSize;
    public string NewLine => FormattingOptions.NewLine;
    public int WrappingColumn => FormattingOptions.WrappingColumn;
}
