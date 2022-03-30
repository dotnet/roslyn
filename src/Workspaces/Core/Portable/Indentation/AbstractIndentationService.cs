// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Indentation
{
    internal abstract partial class AbstractIndentationService<TSyntaxRoot>
        : AbstractIndentation<TSyntaxRoot>, IIndentationService
        where TSyntaxRoot : SyntaxNode, ICompilationUnitSyntax
    {
        public IndentationResult GetIndentation(
            Document document, int lineNumber,
            FormattingOptions.IndentStyle indentStyle, CancellationToken cancellationToken)
        {
            if (indentStyle == FormattingOptions.IndentStyle.None)
            {
                // If there is no indent style, then do nothing.
                return new IndentationResult(basePosition: 0, offset: 0);
            }

            var indenter = GetIndenter(document, lineNumber, (FormattingOptions2.IndentStyle)indentStyle, cancellationToken);

            if (indentStyle == FormattingOptions.IndentStyle.Smart &&
                indenter.TryGetSmartTokenIndentation(out var indentationResult))
            {
                return indentationResult;
            }

            // If the indenter can't produce a valid result, just default to 0 as our indentation.
            return indenter.GetDesiredIndentation((FormattingOptions2.IndentStyle)indentStyle) ?? default;
        }

        private Indenter GetIndenter(Document document, int lineNumber, FormattingOptions2.IndentStyle indentStyle, CancellationToken cancellationToken)
        {
            var options = IndentationOptions.FromDocumentAsync(document, cancellationToken).WaitAndGetResult_CanCallOnBackground(cancellationToken);
            var tree = document.GetRequiredSyntaxTreeSynchronously(cancellationToken);

            var sourceText = tree.GetText(cancellationToken);
            var lineToBeIndented = sourceText.Lines[lineNumber];

            var workspace = document.Project.Solution.Workspace;
            var formattingRuleFactory = workspace.Services.GetRequiredService<IHostDependentFormattingRuleFactoryService>();
            var baseIndentationRule = formattingRuleFactory.CreateRule(document, lineToBeIndented.Start);

            var formattingRules = ImmutableArray.Create(
                baseIndentationRule,
                this.GetSpecializedIndentationFormattingRule(indentStyle)).AddRange(
                Formatter.GetDefaultFormattingRules(document));

            var smartTokenFormatter = CreateSmartTokenFormatter(
                (TSyntaxRoot)tree.GetRoot(cancellationToken), lineToBeIndented, options, baseIndentationRule);
            return new Indenter(this, tree, formattingRules, options, lineToBeIndented, smartTokenFormatter, cancellationToken);
        }
    }
}
