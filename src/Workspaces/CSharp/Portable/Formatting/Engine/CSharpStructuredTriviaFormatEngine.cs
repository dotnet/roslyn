// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

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
            return formatter.FormatAsync(cancellationToken).WaitAndGetResult_CanCallOnBackground(cancellationToken);
        }

        private CSharpStructuredTriviaFormatEngine(
            SyntaxTrivia trivia,
            int initialColumn,
            OptionSet optionSet,
            ChainedFormattingRules formattingRules,
            SyntaxToken token1,
            SyntaxToken token2) :
            base(TreeData.Create(trivia, initialColumn),
                 optionSet,
                 formattingRules,
                 token1,
                 token2,
                 TaskExecutor.Synchronous)
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

        protected override NodeOperations CreateNodeOperationTasks(CancellationToken cancellationToken)
        {
            // ignore all node operations for structured trivia since it is not possible for this to have any impact currently.
            return NodeOperations.Empty;
        }

        protected override AbstractFormattingResult CreateFormattingResult(TokenStream tokenStream)
        {
            return new FormattingResult(this.TreeData, tokenStream, this.SpanToFormat, this.TaskExecutor);
        }
    }
}
