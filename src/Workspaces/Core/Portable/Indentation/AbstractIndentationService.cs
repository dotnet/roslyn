// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Indentation
{
    internal abstract partial class AbstractIndentationService<TSyntaxRoot> : IIndentationService
        where TSyntaxRoot : SyntaxNode, ICompilationUnitSyntax
    {
        protected abstract AbstractFormattingRule GetSpecializedIndentationFormattingRule();

        private IEnumerable<AbstractFormattingRule> GetFormattingRules(Document document, int position)
        {
            var workspace = document.Project.Solution.Workspace;
            var formattingRuleFactory = workspace.Services.GetService<IHostDependentFormattingRuleFactoryService>();
            var baseIndentationRule = formattingRuleFactory.CreateRule(document, position);

            var formattingRules = new[] { baseIndentationRule, this.GetSpecializedIndentationFormattingRule() }.Concat(Formatter.GetDefaultFormattingRules(document));
            return formattingRules;
        }

        public IndentationResult GetIndentation(
            Document document, int lineNumber,
            FormattingOptions.IndentStyle indentStyle, CancellationToken cancellationToken)
        {
            var indenter = GetIndenter(document, lineNumber, cancellationToken);

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

            return indenter.GetDesiredIndentation(indentStyle);
        }

        private Indenter GetIndenter(Document document, int lineNumber, CancellationToken cancellationToken)
        {
            var documentOptions = document.GetOptionsAsync(cancellationToken).WaitAndGetResult_CanCallOnBackground(cancellationToken);
            var syntacticDoc = SyntacticDocument.CreateAsync(document, cancellationToken).WaitAndGetResult_CanCallOnBackground(cancellationToken);

            var sourceText = syntacticDoc.Root.SyntaxTree.GetText(cancellationToken);
            var lineToBeIndented = sourceText.Lines[lineNumber];

            var formattingRules = GetFormattingRules(document, lineToBeIndented.Start);

            return new Indenter(this, syntacticDoc, formattingRules, documentOptions, lineToBeIndented, cancellationToken);
        }

        /// <summary>
        /// Returns <see langword="true"/> if the language specific <see
        /// cref="ISmartTokenFormatter"/> should be deferred to figure out indentation.  If so, it
        /// will be asked to <see cref="ISmartTokenFormatter.FormatTokenAsync"/> the resultant
        /// <paramref name="token"/> provided by this method.
        /// </summary>
        protected abstract bool ShouldUseTokenIndenter(Indenter indenter, out SyntaxToken token);
        protected abstract ISmartTokenFormatter CreateSmartTokenFormatter(Indenter indenter);

        protected abstract IndentationResult GetDesiredIndentationWorker(
            Indenter indenter, SyntaxToken token, TextLine previousLine, int lastNonWhitespacePosition);
    }
}
