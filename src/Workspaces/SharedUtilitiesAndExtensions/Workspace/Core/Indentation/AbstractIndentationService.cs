// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Indentation;

internal abstract partial class AbstractIndentationService<TSyntaxRoot>
    : AbstractIndentation<TSyntaxRoot>, IIndentationService
    where TSyntaxRoot : SyntaxNode, ICompilationUnitSyntax
{
    public IndentationResult GetIndentation(
        ParsedDocument document, int lineNumber, IndentationOptions options, CancellationToken cancellationToken)
    {
        var indentStyle = options.IndentStyle;

        if (indentStyle == FormattingOptions2.IndentStyle.None)
        {
            // If there is no indent style, then do nothing.
            return new IndentationResult(basePosition: 0, offset: 0);
        }

        var indenter = GetIndenter(document, lineNumber, options, cancellationToken);

        if (indentStyle == FormattingOptions2.IndentStyle.Smart &&
            indenter.TryGetSmartTokenIndentation(out var indentationResult))
        {
            return indentationResult;
        }

        // If the indenter can't produce a valid result, just default to 0 as our indentation.
        return indenter.GetDesiredIndentation(indentStyle) ?? default;
    }

    private Indenter GetIndenter(ParsedDocument document, int lineNumber, IndentationOptions options, CancellationToken cancellationToken)
    {
        var syntaxFormatting = this.SyntaxFormatting;

        var lineToBeIndented = document.Text.Lines[lineNumber];

#if CODE_STYLE
        var baseIndentationRule = NoOpFormattingRule.Instance;
#else
        var formattingRuleFactory = document.SolutionServices.GetRequiredService<IHostDependentFormattingRuleFactoryService>();
        var baseIndentationRule = formattingRuleFactory.CreateRule(document, lineToBeIndented.Start);
#endif

        var formattingRules = ImmutableArray.Create(
            baseIndentationRule,
            this.GetSpecializedIndentationFormattingRule(options.IndentStyle)).AddRange(
            syntaxFormatting.GetDefaultFormattingRules());

        var smartTokenFormatter = CreateSmartTokenFormatter(
            (TSyntaxRoot)document.Root, document.Text, lineToBeIndented, options, baseIndentationRule);
        return new Indenter(this, document.SyntaxTree, document.Text, formattingRules, options, lineToBeIndented, smartTokenFormatter, cancellationToken);
    }
}
