// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    internal class CSharpSyntaxFormattingService : AbstractSyntaxFormattingService
    {
        private readonly Lazy<IEnumerable<IFormattingRule>> _lazyExportedRules;

        public CSharpSyntaxFormattingService()
        {
            _lazyExportedRules = new Lazy<IEnumerable<IFormattingRule>>(() => new IFormattingRule[]
            {
                new ElasticTriviaFormattingRule(),
                new EndOfFileTokenFormattingRule(),
                new StructuredTriviaFormattingRule(),
                new IndentBlockFormattingRule(),
                new SuppressFormattingRule(),
                new AnchorIndentationFormattingRule(),
                new QueryExpressionFormattingRule(),
                new TokenBasedFormattingRule(),
                new DefaultOperationProvider(),
            });
        }

        public override IEnumerable<IFormattingRule> GetDefaultFormattingRules()
        {
            var rules = _lazyExportedRules.Value;

            var spaceFormattingRules = new IFormattingRule[]
                {
                    new WrappingFormattingRule(),
                    new SpacingFormattingRule(),
                    new NewLineUserSettingFormattingRule(),
                    new IndentUserSettingsFormattingRule()
                };

            return spaceFormattingRules.Concat(rules).ToImmutableArray();
        }

        protected override IFormattingResult CreateAggregatedFormattingResult(SyntaxNode node, IList<AbstractFormattingResult> results, SimpleIntervalTree<TextSpan> formattingSpans = null)
        {
            return new AggregatedFormattingResult(node, results, formattingSpans);
        }

        protected override AbstractFormattingResult Format(SyntaxNode node, OptionSet optionSet, IEnumerable<IFormattingRule> formattingRules, SyntaxToken token1, SyntaxToken token2, CancellationToken cancellationToken)
        {
            return new CSharpFormatEngine(node, optionSet, formattingRules, token1, token2).Format(cancellationToken);
        }
    }
}
