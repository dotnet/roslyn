// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    internal class CSharpFormatEngine : AbstractFormatEngine
    {
        public CSharpFormatEngine(
            SyntaxNode node,
            AnalyzerConfigOptions options,
            IEnumerable<AbstractFormattingRule> formattingRules,
            SyntaxToken token1,
            SyntaxToken token2)
            : base(TreeData.Create(node),
                 options,
                 formattingRules,
                 token1,
                 token2)
        {
        }

        internal override ISyntaxFacts SyntaxFacts => CSharpSyntaxFacts.Instance;

        protected override AbstractTriviaDataFactory CreateTriviaFactory()
            => new TriviaDataFactory(this.TreeData, this.Options);

        protected override AbstractFormattingResult CreateFormattingResult(TokenStream tokenStream)
            => new FormattingResult(this.TreeData, tokenStream, this.SpanToFormat);
    }
}
