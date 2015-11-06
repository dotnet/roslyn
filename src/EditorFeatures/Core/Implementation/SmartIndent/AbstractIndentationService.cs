// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.SmartIndent
{
    internal abstract partial class AbstractIndentationService : IIndentationService
    {
        protected abstract IFormattingRule GetSpecializedIndentationFormattingRule();

        private IEnumerable<IFormattingRule> GetFormattingRules(Document document, int position)
        {
            var workspace = document.Project.Solution.Workspace;
            var formattingRuleFactory = workspace.Services.GetService<IHostDependentFormattingRuleFactoryService>();
            var baseIndentationRule = formattingRuleFactory.CreateRule(document, position);

            var formattingRules = new[] { baseIndentationRule, this.GetSpecializedIndentationFormattingRule() }.Concat(Formatter.GetDefaultFormattingRules(document));
            return formattingRules;
        }

        public Task<IndentationResult?> GetDesiredIndentationAsync(Document document, int lineNumber, CancellationToken cancellationToken)
        {
            var root = document.GetSyntaxRootAsync(cancellationToken).WaitAndGetResult(cancellationToken);
            var sourceText = document.GetTextAsync(cancellationToken).WaitAndGetResult(cancellationToken);

            var textSnapshot = sourceText.FindCorrespondingEditorTextSnapshot();
            if (textSnapshot == null)
            {
                // text snapshot doesn't exit. return null
                return Task.FromResult<IndentationResult?>(null);
            }

            var lineToBeIndented = textSnapshot.GetLineFromLineNumber(lineNumber);

            var formattingRules = GetFormattingRules(document, lineToBeIndented.Start);
            var optionSet = document.Project.Solution.Workspace.Options;

            // enter on a token case.
            if (ShouldUseSmartTokenFormatterInsteadOfIndenter(formattingRules, root, lineToBeIndented, optionSet, cancellationToken))
            {
                return Task.FromResult<IndentationResult?>(null);
            }

            var indenter = GetIndenter(document, lineToBeIndented, formattingRules, optionSet, cancellationToken);
            return Task.FromResult(indenter.GetDesiredIndentation());
        }

        protected abstract AbstractIndenter GetIndenter(Document document, ITextSnapshotLine lineToBeIndented, IEnumerable<IFormattingRule> formattingRules, OptionSet optionSet, CancellationToken cancellationToken);

        protected abstract bool ShouldUseSmartTokenFormatterInsteadOfIndenter(
            IEnumerable<IFormattingRule> formattingRules, SyntaxNode root, ITextSnapshotLine line, OptionSet optionSet, CancellationToken cancellationToken);
    }
}
