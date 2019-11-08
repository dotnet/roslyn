// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Indentation
{
    internal class CSharpSmartTokenFormatter : ISmartTokenFormatter
    {
        private readonly OptionSet _optionSet;
        private readonly IEnumerable<AbstractFormattingRule> _formattingRules;

        private readonly CompilationUnitSyntax _root;

        public CSharpSmartTokenFormatter(
            OptionSet optionSet,
            IEnumerable<AbstractFormattingRule> formattingRules,
            CompilationUnitSyntax root)
        {
            Contract.ThrowIfNull(optionSet);
            Contract.ThrowIfNull(formattingRules);
            Contract.ThrowIfNull(root);

            _optionSet = optionSet;
            _formattingRules = formattingRules;

            _root = root;
        }

        public IList<TextChange> FormatRange(
            Workspace workspace, SyntaxToken startToken, SyntaxToken endToken, CancellationToken cancellationToken)
        {
            Contract.ThrowIfTrue(startToken.Kind() == SyntaxKind.None || startToken.Kind() == SyntaxKind.EndOfFileToken);
            Contract.ThrowIfTrue(endToken.Kind() == SyntaxKind.None || endToken.Kind() == SyntaxKind.EndOfFileToken);

            var smartTokenformattingRules = _formattingRules;
            var common = startToken.GetCommonRoot(endToken);

            // if there are errors, do not touch lines
            // Exception 1: In the case of try-catch-finally block, a try block without a catch/finally block is considered incomplete
            //            but we would like to apply line operation in a completed try block even if there is no catch/finally block
            // Exception 2: Similar behavior for do-while
            if (common.ContainsDiagnostics && !CloseBraceOfTryOrDoBlock(endToken))
            {
                smartTokenformattingRules = (new NoLineChangeFormattingRule()).Concat(_formattingRules);
            }

            return Formatter.GetFormattedTextChanges(_root, new TextSpan[] { TextSpan.FromBounds(startToken.SpanStart, endToken.Span.End) }, workspace, _optionSet, smartTokenformattingRules, cancellationToken);
        }

        private bool CloseBraceOfTryOrDoBlock(SyntaxToken endToken)
        {
            return endToken.IsKind(SyntaxKind.CloseBraceToken) &&
                endToken.Parent.IsKind(SyntaxKind.Block) &&
                (endToken.Parent.IsParentKind(SyntaxKind.TryStatement) || endToken.Parent.IsParentKind(SyntaxKind.DoStatement));
        }

        public async Task<IList<TextChange>> FormatTokenAsync(
            Workspace workspace, SyntaxToken token, CancellationToken cancellationToken)
        {
            Contract.ThrowIfTrue(token.Kind() == SyntaxKind.None || token.Kind() == SyntaxKind.EndOfFileToken);

            // get previous token
            var previousToken = token.GetPreviousToken(includeZeroWidth: true);
            if (previousToken.Kind() == SyntaxKind.None)
            {
                // no previous token. nothing to format
                return SpecializedCollections.EmptyList<TextChange>();
            }

            // This is a heuristic to prevent brace completion from breaking user expectation/muscle memory in common scenarios (see Devdiv:823958).
            // Formatter uses FindToken on the position, which returns token to left, if there is nothing to the right and returns token to the right
            // if there exists one. If the shape is "{|}", we're including '}' in the formatting range. Avoid doing that to improve verbatim typing
            // in the following special scenarios.  
            var adjustedEndPosition = token.Span.End;
            if (token.IsKind(SyntaxKind.OpenBraceToken) &&
                (token.Parent.IsInitializerForArrayOrCollectionCreationExpression() ||
                    token.Parent is AnonymousObjectCreationExpressionSyntax))
            {
                var nextToken = token.GetNextToken(includeZeroWidth: true);
                if (nextToken.IsKind(SyntaxKind.CloseBraceToken))
                {
                    // Format upto '{' and exclude '}'
                    adjustedEndPosition = token.SpanStart;
                }
            }

            var smartTokenformattingRules = (new SmartTokenFormattingRule()).Concat(_formattingRules);
            var adjustedStartPosition = previousToken.SpanStart;
            var indentStyle = _optionSet.GetOption(FormattingOptions.SmartIndent, LanguageNames.CSharp);
            if (token.IsKind(SyntaxKind.OpenBraceToken) &&
                indentStyle != FormattingOptions.IndentStyle.Smart)
            {
                var text = await token.SyntaxTree.GetTextAsync(cancellationToken).ConfigureAwait(false);
                if (token.IsFirstTokenOnLine(text))
                {
                    adjustedStartPosition = token.SpanStart;
                }
            }

            return Formatter.GetFormattedTextChanges(_root,
                new TextSpan[] { TextSpan.FromBounds(adjustedStartPosition, adjustedEndPosition) },
                workspace, _optionSet, smartTokenformattingRules, cancellationToken);
        }

        private class NoLineChangeFormattingRule : AbstractFormattingRule
        {
            public override AdjustNewLinesOperation GetAdjustNewLinesOperation(SyntaxToken previousToken, SyntaxToken currentToken, OptionSet optionSet, in NextGetAdjustNewLinesOperation nextOperation)
            {
                // no line operation. no line changes what so ever
                var lineOperation = base.GetAdjustNewLinesOperation(previousToken, currentToken, optionSet, in nextOperation);
                if (lineOperation != null)
                {
                    // ignore force if same line option
                    if (lineOperation.Option == AdjustNewLinesOption.ForceLinesIfOnSingleLine)
                    {
                        return null;
                    }

                    // basically means don't ever put new line if there isn't already one, but do
                    // indentation.
                    return FormattingOperations.CreateAdjustNewLinesOperation(line: 0, option: AdjustNewLinesOption.PreserveLines);
                }

                return null;
            }
        }

        private class SmartTokenFormattingRule : NoLineChangeFormattingRule
        {
            public override void AddSuppressOperations(List<SuppressOperation> list, SyntaxNode node, OptionSet optionSet, in NextSuppressOperationAction nextOperation)
            {
                // don't suppress anything
            }

            public override AdjustSpacesOperation GetAdjustSpacesOperation(SyntaxToken previousToken, SyntaxToken currentToken, OptionSet optionSet, in NextGetAdjustSpacesOperation nextOperation)
            {
                var spaceOperation = base.GetAdjustSpacesOperation(previousToken, currentToken, optionSet, in nextOperation);

                // if there is force space operation, convert it to ForceSpaceIfSingleLine operation.
                // (force space basically means remove all line breaks)
                if (spaceOperation is { Option: AdjustSpacesOption.ForceSpaces })
                {
                    return FormattingOperations.CreateAdjustSpacesOperation(spaceOperation.Space, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
                }

                return spaceOperation;
            }
        }
    }
}
