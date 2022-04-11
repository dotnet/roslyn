// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Indentation
{
    internal class CSharpSmartTokenFormatter : ISmartTokenFormatter
    {
        private readonly IndentationOptions _options;
        private readonly ImmutableArray<AbstractFormattingRule> _formattingRules;

        private readonly CompilationUnitSyntax _root;

        public CSharpSmartTokenFormatter(
            IndentationOptions options,
            ImmutableArray<AbstractFormattingRule> formattingRules,
            CompilationUnitSyntax root)
        {
            Contract.ThrowIfNull(root);

            _options = options;
            _formattingRules = formattingRules;

            _root = root;
        }

        public IList<TextChange> FormatRange(
            SyntaxToken startToken, SyntaxToken endToken, CancellationToken cancellationToken)
        {
            Contract.ThrowIfTrue(startToken.Kind() is SyntaxKind.None or SyntaxKind.EndOfFileToken);
            Contract.ThrowIfTrue(endToken.Kind() is SyntaxKind.None or SyntaxKind.EndOfFileToken);

            var smartTokenformattingRules = _formattingRules;
            var common = startToken.GetCommonRoot(endToken);
            RoslynDebug.AssertNotNull(common);

            // if there are errors, do not touch lines
            // Exception 1: In the case of try-catch-finally block, a try block without a catch/finally block is considered incomplete
            //            but we would like to apply line operation in a completed try block even if there is no catch/finally block
            // Exception 2: Similar behavior for do-while
            if (common.ContainsDiagnostics && !CloseBraceOfTryOrDoBlock(endToken))
            {
                smartTokenformattingRules = ImmutableArray<AbstractFormattingRule>.Empty.Add(
                    new NoLineChangeFormattingRule()).AddRange(_formattingRules);
            }

            var formatter = CSharpSyntaxFormatting.Instance;
            var result = formatter.GetFormattingResult(
                _root, new[] { TextSpan.FromBounds(startToken.SpanStart, endToken.Span.End) }, _options.FormattingOptions, smartTokenformattingRules, cancellationToken);
            return result.GetTextChanges(cancellationToken);
        }

        private static bool CloseBraceOfTryOrDoBlock(SyntaxToken endToken)
        {
            return endToken.IsKind(SyntaxKind.CloseBraceToken) &&
                endToken.Parent.IsKind(SyntaxKind.Block) &&
                (endToken.Parent.IsParentKind(SyntaxKind.TryStatement) || endToken.Parent.IsParentKind(SyntaxKind.DoStatement));
        }

        public async Task<IList<TextChange>> FormatTokenAsync(
            SyntaxToken token, CancellationToken cancellationToken)
        {
            Contract.ThrowIfTrue(token.Kind() is SyntaxKind.None or SyntaxKind.EndOfFileToken);

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

            var smartTokenformattingRules = new SmartTokenFormattingRule().Concat(_formattingRules);
            var adjustedStartPosition = previousToken.SpanStart;
            if (token.IsKind(SyntaxKind.OpenBraceToken) &&
                _options.IndentStyle != FormattingOptions2.IndentStyle.Smart)
            {
                RoslynDebug.AssertNotNull(token.SyntaxTree);
                var text = await token.SyntaxTree.GetTextAsync(cancellationToken).ConfigureAwait(false);
                if (token.IsFirstTokenOnLine(text))
                {
                    adjustedStartPosition = token.SpanStart;
                }
            }

            var formatter = CSharpSyntaxFormatting.Instance;
            var result = formatter.GetFormattingResult(
                _root, new[] { TextSpan.FromBounds(adjustedStartPosition, adjustedEndPosition) }, _options.FormattingOptions, smartTokenformattingRules, cancellationToken);
            return result.GetTextChanges(cancellationToken);
        }

        private class NoLineChangeFormattingRule : AbstractFormattingRule
        {
            public override AdjustNewLinesOperation? GetAdjustNewLinesOperation(in SyntaxToken previousToken, in SyntaxToken currentToken, in NextGetAdjustNewLinesOperation nextOperation)
            {
                // no line operation. no line changes what so ever
                var lineOperation = base.GetAdjustNewLinesOperation(in previousToken, in currentToken, in nextOperation);
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
            public override void AddSuppressOperations(List<SuppressOperation> list, SyntaxNode node, in NextSuppressOperationAction nextOperation)
            {
                // don't suppress anything
            }

            public override AdjustSpacesOperation? GetAdjustSpacesOperation(in SyntaxToken previousToken, in SyntaxToken currentToken, in NextGetAdjustSpacesOperation nextOperation)
            {
                var spaceOperation = base.GetAdjustSpacesOperation(in previousToken, in currentToken, in nextOperation);

                // if there is force space operation, convert it to ForceSpaceIfSingleLine operation.
                // (force space basically means remove all line breaks)
                if (spaceOperation != null && spaceOperation.Option == AdjustSpacesOption.ForceSpaces)
                {
                    return FormattingOperations.CreateAdjustSpacesOperation(spaceOperation.Space, AdjustSpacesOption.ForceSpacesIfOnSingleLine);
                }

                return spaceOperation;
            }
        }
    }
}
