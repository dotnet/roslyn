// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Indentation
{
    internal abstract partial class AbstractIndentationService<TSyntaxRoot> : IIndentationService
        where TSyntaxRoot : SyntaxNode, ICompilationUnitSyntax
    {
        protected abstract AbstractFormattingRule GetSpecializedIndentationFormattingRule(FormattingOptions.IndentStyle indentStyle);

        private IEnumerable<AbstractFormattingRule> GetFormattingRules(Document document, int position, FormattingOptions.IndentStyle indentStyle)
        {
            var formattingRuleFactory = document.Project.Solution.Workspace.Services.GetRequiredService<IHostDependentFormattingRuleFactoryService>();
            var baseIndentationRule = formattingRuleFactory.CreateRule(document, position);

            return new[] { baseIndentationRule, GetSpecializedIndentationFormattingRule(indentStyle) }.Concat(Formatter.GetDefaultFormattingRules(document));
        }

        public IndentationResult GetIndentation(
            SyntacticDocument document,
            int lineNumber,
            FormattingOptions.IndentStyle indentStyle,
            IndentationOptions options,
            CancellationToken cancellationToken)
        {
            var lineToBeIndented = document.Text.Lines[lineNumber];
            var formattingRules = GetFormattingRules(document.Document, lineToBeIndented.Start, indentStyle);
            var indenter = new Indenter(this, document, formattingRules, options, lineToBeIndented, cancellationToken);

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

        /// <summary>
        /// Returns <see langword="true"/> if the language specific <see
        /// cref="ISmartTokenFormatter"/> should be deferred to figure out indentation.  If so, it
        /// will be asked to <see cref="ISmartTokenFormatter.FormatTokenAsync"/> the resultant
        /// <paramref name="token"/> provided by this method.
        /// </summary>
        protected abstract bool ShouldUseTokenIndenter(Indenter indenter, out SyntaxToken token);
        protected abstract ISmartTokenFormatter CreateSmartTokenFormatter(Indenter indenter);

        protected abstract IndentationResult? GetDesiredIndentationWorker(
            Indenter indenter, SyntaxToken? token, SyntaxTrivia? trivia);
    }
}
