// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Diagnostics;
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

        public static CSharpSyntaxWrappingOptions Create(AnalyzerConfigOptions options, CodeActionOptions ideOptions)
            => new(
                CSharpSyntaxFormattingOptions.Create(options, (CSharpSyntaxFormattingOptions?)ideOptions.CleanupOptions?.FormattingOptions),
                operatorPlacement: options.GetOption(CodeStyleOptions2.OperatorPlacementWhenWrapping),
                wrappingColumn: ideOptions.WrappingColumn,
                newLinesForBracesInObjectCollectionArrayInitializers: options.GetOption(CSharpFormattingOptions2.NewLinesForBracesInObjectCollectionArrayInitializers));
    }
}
