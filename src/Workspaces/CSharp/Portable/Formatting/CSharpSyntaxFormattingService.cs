// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    [ExportLanguageService(typeof(ISyntaxFormattingService), LanguageNames.CSharp), Shared]
    internal class CSharpSyntaxFormattingService : AbstractSyntaxFormattingService
    {
        private readonly ImmutableList<IFormattingRule> _rules;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpSyntaxFormattingService()
        {
            _rules = ImmutableList.Create<IFormattingRule>(
                new WrappingFormattingRule(),
                new SpacingFormattingRule(),
                new NewLineUserSettingFormattingRule(),
                new IndentUserSettingsFormattingRule(),
                new ElasticTriviaFormattingRule(),
                new EndOfFileTokenFormattingRule(),
                new StructuredTriviaFormattingRule(),
                new IndentBlockFormattingRule(),
                new SuppressFormattingRule(),
                new AnchorIndentationFormattingRule(),
                new QueryExpressionFormattingRule(),
                new TokenBasedFormattingRule(),
                new DefaultOperationProvider());
        }

        public override IEnumerable<IFormattingRule> GetDefaultFormattingRules()
        {
            return _rules;
        }

        protected override IFormattingResult CreateAggregatedFormattingResult(SyntaxNode node, IList<AbstractFormattingResult> results, SimpleIntervalTree<TextSpan> formattingSpans = null)
        {
            return new AggregatedFormattingResult(node, results, formattingSpans);
        }

        protected override Task<AbstractFormattingResult> FormatAsync(SyntaxNode node, OptionSet optionSet, IEnumerable<IFormattingRule> formattingRules, SyntaxToken token1, SyntaxToken token2, CancellationToken cancellationToken)
        {
            return Task.FromResult(new CSharpFormatEngine(node, optionSet, formattingRules, token1, token2).Format(cancellationToken));
        }
    }
}
