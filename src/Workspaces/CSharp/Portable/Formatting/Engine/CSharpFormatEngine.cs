// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    internal class CSharpFormatEngine : AbstractFormatEngine
    {
        public CSharpFormatEngine(
            SyntaxNode node,
            OptionSet optionSet,
            IEnumerable<IFormattingRule> formattingRules,
            SyntaxToken token1,
            SyntaxToken token2) :
            base(TreeData.Create(node),
                 optionSet,
                 formattingRules,
                 token1,
                 token2,
                 TaskExecutor.Concurrent)
        {
        }

        protected override AbstractTriviaDataFactory CreateTriviaFactory()
        {
            return new TriviaDataFactory(this.TreeData, this.OptionSet);
        }

        protected override AbstractFormattingResult CreateFormattingResult(TokenStream tokenStream)
        {
            return new FormattingResult(this.TreeData, tokenStream, this.SpanToFormat, this.TaskExecutor);
        }
    }
}
