﻿// Licensed to the .NET Foundation under one or more agreements.
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
    internal sealed class CSharpSyntaxWrappingOptions(
        CSharpSyntaxFormattingOptions formattingOptions,
        int wrappingColumn,
        OperatorPlacementWhenWrappingPreference operatorPlacement,
        bool newLinesForBracesInObjectCollectionArrayInitializers) : SyntaxWrappingOptions(formattingOptions, wrappingColumn, operatorPlacement)
    {
        public readonly bool NewLinesForBracesInObjectCollectionArrayInitializers = newLinesForBracesInObjectCollectionArrayInitializers;
    }

    internal static class CSharpSyntaxWrappingOptionsProviders
    {
        public static CSharpSyntaxWrappingOptions GetCSharpSyntaxWrappingOptions(this IOptionsReader options, CodeActionOptions fallbackOptions)
        {
            var newLineBeforeOpenBraceDefault = ((CSharpSyntaxFormattingOptions)fallbackOptions.CleanupOptions.FormattingOptions).NewLines.ToNewLineBeforeOpenBracePlacement();

            return new(
                new CSharpSyntaxFormattingOptions(options, (CSharpSyntaxFormattingOptions)fallbackOptions.CleanupOptions.FormattingOptions),
                operatorPlacement: options.GetOption(CodeStyleOptions2.OperatorPlacementWhenWrapping, fallbackOptions.CodeStyleOptions.OperatorPlacementWhenWrapping),
                wrappingColumn: fallbackOptions.WrappingColumn,
                newLinesForBracesInObjectCollectionArrayInitializers: options.GetOption(CSharpFormattingOptions2.NewLineBeforeOpenBrace, newLineBeforeOpenBraceDefault).HasFlag(NewLineBeforeOpenBracePlacement.ObjectCollectionArrayInitializers));
        }
    }
}
