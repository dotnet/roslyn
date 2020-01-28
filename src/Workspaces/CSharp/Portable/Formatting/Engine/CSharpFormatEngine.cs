// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;

#if CODE_STYLE
using OptionSet = Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions;
#else
using Microsoft.CodeAnalysis.Options;
#endif

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    internal class CSharpFormatEngine : AbstractFormatEngine
    {
        public CSharpFormatEngine(
            SyntaxNode node,
            OptionSet optionSet,
            IEnumerable<AbstractFormattingRule> formattingRules,
            SyntaxToken token1,
            SyntaxToken token2)
            : base(TreeData.Create(node),
                 optionSet,
                 formattingRules,
                 token1,
                 token2)
        {
        }

        protected override AbstractTriviaDataFactory CreateTriviaFactory()
        {
            return new TriviaDataFactory(this.TreeData, this.OptionSet);
        }

        protected override AbstractFormattingResult CreateFormattingResult(TokenStream tokenStream)
        {
            return new FormattingResult(this.TreeData, tokenStream, this.SpanToFormat);
        }
    }
}
