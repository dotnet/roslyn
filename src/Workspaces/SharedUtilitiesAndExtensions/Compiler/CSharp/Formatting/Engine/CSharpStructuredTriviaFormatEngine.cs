// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.Formatting;

#if CODE_STYLE
using OptionSet = Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions;
#else
using Microsoft.CodeAnalysis.Options;
#endif

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    internal class CSharpStructuredTriviaFormatEngine : AbstractFormatEngine
    {
        public static IFormattingResult Format(
            SyntaxTrivia trivia,
            int initialColumn,
            OptionSet optionSet,
            ChainedFormattingRules formattingRules,
            CancellationToken cancellationToken)
        {
            var root = trivia.GetStructure();
            var formatter = new CSharpStructuredTriviaFormatEngine(trivia, initialColumn, optionSet, formattingRules, root.GetFirstToken(includeZeroWidth: true), root.GetLastToken(includeZeroWidth: true));
            return formatter.Format(cancellationToken);
        }

        private CSharpStructuredTriviaFormatEngine(
            SyntaxTrivia trivia,
            int initialColumn,
            OptionSet optionSet,
            ChainedFormattingRules formattingRules,
            SyntaxToken token1,
            SyntaxToken token2)
            : base(TreeData.Create(trivia, initialColumn),
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

        protected override FormattingContext CreateFormattingContext(TokenStream tokenStream, CancellationToken cancellationToken)
        {
            return new FormattingContext(this, tokenStream, LanguageNames.CSharp);
        }

        protected override NodeOperations CreateNodeOperations(CancellationToken cancellationToken)
        {
            // ignore all node operations for structured trivia since it is not possible for this to have any impact currently.
            return NodeOperations.Empty;
        }

        protected override AbstractFormattingResult CreateFormattingResult(TokenStream tokenStream)
        {
            return new FormattingResult(this.TreeData, tokenStream, this.SpanToFormat);
        }
    }
}
