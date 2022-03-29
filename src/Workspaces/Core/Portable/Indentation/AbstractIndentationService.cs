// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Indentation
{
    internal abstract partial class AbstractIndentationService<TSyntaxRoot> : IIndentationService
        where TSyntaxRoot : SyntaxNode, ICompilationUnitSyntax
    {
        protected abstract ISyntaxFacts SyntaxFacts { get; }
        protected abstract IHeaderFacts HeaderFacts { get; }

        protected abstract AbstractFormattingRule GetSpecializedIndentationFormattingRule(FormattingOptions2.IndentStyle indentStyle);

        /// <summary>
        /// Returns <see langword="true"/> if the language specific <see
        /// cref="ISmartTokenFormatter"/> should be deferred to figure out indentation.  If so, it
        /// will be asked to <see cref="ISmartTokenFormatter.FormatTokenAsync"/> the resultant
        /// <paramref name="token"/> provided by this method.
        /// </summary>
        protected abstract bool ShouldUseTokenIndenter(Indenter indenter, out SyntaxToken token);
        protected abstract ISmartTokenFormatter CreateSmartTokenFormatter(Document document, TSyntaxRoot root, TextLine lineToBeIndented, IndentationOptions options);

        protected abstract IndentationResult? GetDesiredIndentationWorker(
            Indenter indenter, SyntaxToken? token, SyntaxTrivia? trivia);

        private ImmutableArray<AbstractFormattingRule> GetFormattingRules(Document document, int position, FormattingOptions2.IndentStyle indentStyle)
        {
            var workspace = document.Project.Solution.Workspace;
            var formattingRuleFactory = workspace.Services.GetRequiredService<IHostDependentFormattingRuleFactoryService>();
            var baseIndentationRule = formattingRuleFactory.CreateRule(document, position);

            return ImmutableArray.Create(
                baseIndentationRule,
                this.GetSpecializedIndentationFormattingRule(indentStyle)).AddRange(
                Formatter.GetDefaultFormattingRules(document));
        }

        public IndentationResult GetIndentation(
            Document document, int lineNumber,
            FormattingOptions.IndentStyle indentStyle, CancellationToken cancellationToken)
        {
            var indenter = GetIndenter(document, lineNumber, (FormattingOptions2.IndentStyle)indentStyle, cancellationToken);

            if (indentStyle == FormattingOptions.IndentStyle.None)
            {
                // If there is no indent style, then do nothing.
                return new IndentationResult(basePosition: 0, offset: 0);
            }

            if (indentStyle == FormattingOptions.IndentStyle.Smart &&
                indenter.TryGetSmartTokenIndentation(out var indentationResult))
            {
                return indentationResult;
            }

            // If the indenter can't produce a valid result, just default to 0 as our indentation.
            return indenter.GetDesiredIndentation(indentStyle) ?? default;
        }

        private Indenter GetIndenter(Document document, int lineNumber, FormattingOptions2.IndentStyle indentStyle, CancellationToken cancellationToken)
        {
            var options = IndentationOptions.FromDocumentAsync(document, cancellationToken).WaitAndGetResult_CanCallOnBackground(cancellationToken);
            var tree = document.GetRequiredSyntaxTreeSynchronously(cancellationToken);

            var sourceText = tree.GetText(cancellationToken);
            var lineToBeIndented = sourceText.Lines[lineNumber];

            var formattingRules = GetFormattingRules(document, lineToBeIndented.Start, indentStyle);

            var smartTokenFormatter = CreateSmartTokenFormatter(
                document, (TSyntaxRoot)tree.GetRoot(cancellationToken), lineToBeIndented, options);
            return new Indenter(this, tree, formattingRules, options, lineToBeIndented, smartTokenFormatter, cancellationToken);
        }
    }
}
