// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Wrapping;

namespace Microsoft.CodeAnalysis.CSharp.Wrapping
{
    internal sealed class CSharpSyntaxWrappingOptions : SyntaxWrappingOptions
    {
        public readonly bool NewLinesForBracesInObjectCollectionArrayInitializers;

        public CSharpSyntaxWrappingOptions(
            bool useTabs,
            int tabSize,
            string newLine,
            int wrappingColumn,
            OperatorPlacementWhenWrappingPreference operatorPlacement,
            bool newLinesForBracesInObjectCollectionArrayInitializers)
            : base(useTabs, tabSize, newLine, wrappingColumn, operatorPlacement)
        {
            NewLinesForBracesInObjectCollectionArrayInitializers = newLinesForBracesInObjectCollectionArrayInitializers;
        }

        public static CSharpSyntaxWrappingOptions Create(AnalyzerConfigOptions options, CodeActionOptions ideOptions)
            => new(
                useTabs: options.GetOption(FormattingOptions2.UseTabs),
                tabSize: options.GetOption(FormattingOptions2.TabSize),
                newLine: options.GetOption(FormattingOptions2.NewLine),
                operatorPlacement: options.GetOption(CodeStyleOptions2.OperatorPlacementWhenWrapping),
                wrappingColumn: ideOptions.WrappingColumn,
                newLinesForBracesInObjectCollectionArrayInitializers: options.GetOption(CSharpFormattingOptions2.NewLinesForBracesInObjectCollectionArrayInitializers));
    }
}
