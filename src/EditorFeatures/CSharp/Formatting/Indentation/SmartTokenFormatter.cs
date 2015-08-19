// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Implementation.Formatting.Indentation;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.Formatting.Indentation
{
    internal class SmartTokenFormatter : ISmartTokenFormatter
    {
        private readonly OptionSet _optionSet;
        private readonly IEnumerable<IFormattingRule> _formattingRules;

        private readonly CompilationUnitSyntax _root;

        public SmartTokenFormatter(
            OptionSet optionSet,
            IEnumerable<IFormattingRule> formattingRules,
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
            if (common.ContainsDiagnostics)
            {
                // if there is errors, do not touch lines
                smartTokenformattingRules = (new NoLineChangeFormattingRule()).Concat(_formattingRules);
            }

            return Formatter.GetFormattedTextChanges(_root, new TextSpan[] { TextSpan.FromBounds(startToken.SpanStart, endToken.Span.End) }, workspace, _optionSet, smartTokenformattingRules, cancellationToken);
        }

        public IList<TextChange> FormatToken(Workspace workspace, SyntaxToken token, CancellationToken cancellationToken)
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
            int adjustedEndPosition = token.Span.End;
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
            var indentStyle = workspace.Options.GetOption(FormattingOptions.SmartIndent, LanguageNames.CSharp);
            if (token.IsKind(SyntaxKind.OpenBraceToken) && token.IsFirstTokenOnLine(token.SyntaxTree.GetText()) && indentStyle != FormattingOptions.IndentStyle.Smart)
            {
                adjustedStartPosition = token.SpanStart;
            }

            return Formatter.GetFormattedTextChanges(_root, new TextSpan[] { TextSpan.FromBounds(adjustedStartPosition, adjustedEndPosition) }, workspace, _optionSet, smartTokenformattingRules, cancellationToken);
        }

        private class NoLineChangeFormattingRule : AbstractFormattingRule
        {
            public override AdjustNewLinesOperation GetAdjustNewLinesOperation(SyntaxToken previousToken, SyntaxToken currentToken, OptionSet optionSet, NextOperation<AdjustNewLinesOperation> nextOperation)
            {
                // no line operation. no line changes what so ever
                var lineOperation = base.GetAdjustNewLinesOperation(previousToken, currentToken, optionSet, nextOperation);
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
            public override void AddSuppressOperations(List<SuppressOperation> list, SyntaxNode node, SyntaxToken lastToken, OptionSet optionSet, NextAction<SuppressOperation> nextOperation)
            {
                // don't suppress anything
            }

            public override AdjustSpacesOperation GetAdjustSpacesOperation(SyntaxToken previousToken, SyntaxToken currentToken, OptionSet optionSet, NextOperation<AdjustSpacesOperation> nextOperation)
            {
                var spaceOperation = base.GetAdjustSpacesOperation(previousToken, currentToken, optionSet, nextOperation);

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
