// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Wrapping;

namespace Microsoft.CodeAnalysis.CSharp.Wrapping
{
    internal sealed class CSharpSyntaxWrappingOptions : SyntaxWrappingOptions
    {
        public readonly bool NewLinesForBracesInObjectCollectionArrayInitializers;

        public CSharpSyntaxWrappingOptions(
            CSharpSyntaxFormattingOptions formattingOptions,
            int wrappingColumn,
            OperatorPlacementWhenWrappingPreference operatorPlacement,
            bool newLinesForBracesInObjectCollectionArrayInitializers)
            : base(formattingOptions, wrappingColumn, operatorPlacement)
        {
            NewLinesForBracesInObjectCollectionArrayInitializers = newLinesForBracesInObjectCollectionArrayInitializers;
        }
    }

    internal static class CSharpSyntaxWrappingOptionsProviders
    {
        public static CSharpSyntaxWrappingOptions GetCSharpSyntaxWrappingOptions(this IOptionsReader options, CodeActionOptions fallbackOptions)
        {
            var newLineBeforeOpenBraceDefault = ((CSharpSyntaxFormattingOptions)fallbackOptions.CleanupOptions.FormattingOptions).NewLines.ToNewLineBeforeOpenBracePlacement();

            return new(
                options.GetCSharpSyntaxFormattingOptions((CSharpSyntaxFormattingOptions)fallbackOptions.CleanupOptions.FormattingOptions),
                operatorPlacement: options.GetOption(CodeStyleOptions2.OperatorPlacementWhenWrapping, fallbackOptions.CodeStyleOptions.Common.OperatorPlacementWhenWrapping),
                wrappingColumn: fallbackOptions.WrappingColumn,
                newLinesForBracesInObjectCollectionArrayInitializers: options.GetOption(CSharpFormattingOptions2.NewLineBeforeOpenBrace, newLineBeforeOpenBraceDefault).HasFlag(NewLineBeforeOpenBracePlacement.ObjectCollectionArrayInitializers));
        }
    }
}
