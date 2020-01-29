// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Text;

#if CODE_STYLE
using OptionSet = Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions;
#else
using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
#endif

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
#if !CODE_STYLE
    [ExportLanguageService(typeof(ISyntaxFormattingService), LanguageNames.CSharp), Shared]
#endif
    internal class CSharpSyntaxFormattingService : AbstractSyntaxFormattingService
    {
        private readonly ImmutableList<AbstractFormattingRule> _rules;

#if !CODE_STYLE
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
#endif
        public CSharpSyntaxFormattingService()
        {
            _rules = ImmutableList.Create<AbstractFormattingRule>(
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
                DefaultOperationProvider.Instance);
        }

        public override IEnumerable<AbstractFormattingRule> GetDefaultFormattingRules()
        {
            return _rules;
        }

        protected override IFormattingResult CreateAggregatedFormattingResult(SyntaxNode node, IList<AbstractFormattingResult> results, SimpleIntervalTree<TextSpan, TextSpanIntervalIntrospector> formattingSpans = null)
        {
            return new AggregatedFormattingResult(node, results, formattingSpans);
        }

        protected override AbstractFormattingResult Format(SyntaxNode node, OptionSet optionSet, IEnumerable<AbstractFormattingRule> formattingRules, SyntaxToken token1, SyntaxToken token2, CancellationToken cancellationToken)
        {
            return new CSharpFormatEngine(node, optionSet, formattingRules, token1, token2).Format(cancellationToken);
        }
    }
}
