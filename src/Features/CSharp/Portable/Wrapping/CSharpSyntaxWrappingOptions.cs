// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Wrapping;

namespace Microsoft.CodeAnalysis.CSharp.Wrapping;

internal sealed class CSharpSyntaxWrappingOptions(
    CSharpSyntaxFormattingOptions formattingOptions,
    OperatorPlacementWhenWrappingPreference operatorPlacement,
    bool newLinesForBracesInObjectCollectionArrayInitializers) : SyntaxWrappingOptions(formattingOptions, operatorPlacement)
{
    public readonly bool NewLinesForBracesInObjectCollectionArrayInitializers = newLinesForBracesInObjectCollectionArrayInitializers;
}

internal static class CSharpSyntaxWrappingOptionsProviders
{
    public static CSharpSyntaxWrappingOptions GetCSharpSyntaxWrappingOptions(this IOptionsReader options)
        => new(
            new CSharpSyntaxFormattingOptions(options),
            operatorPlacement: options.GetOption(CodeStyleOptions2.OperatorPlacementWhenWrapping),
            newLinesForBracesInObjectCollectionArrayInitializers: options.GetOption(CSharpFormattingOptions2.NewLineBeforeOpenBrace).HasFlag(NewLineBeforeOpenBracePlacement.ObjectCollectionArrayInitializers));
}
