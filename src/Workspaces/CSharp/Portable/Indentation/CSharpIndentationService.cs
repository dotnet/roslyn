// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Indentation
{
    [ExportLanguageService(typeof(IIndentationService), LanguageNames.CSharp), Shared]
    internal sealed partial class CSharpIndentationService : AbstractIndentationService<CompilationUnitSyntax>
    {
        public static readonly CSharpIndentationService Instance = new CSharpIndentationService();

        private static readonly AbstractFormattingRule s_instance = new FormattingRule();

        [ImportingConstructor]
        public CSharpIndentationService()
        {
        }

        protected override AbstractFormattingRule GetSpecializedIndentationFormattingRule()
        {
            return s_instance;
        }

        public static bool ShouldUseSmartTokenFormatterInsteadOfIndenter(
            IEnumerable<AbstractFormattingRule> formattingRules,
            CompilationUnitSyntax root,
            TextLine line,
            OptionSet optionSet,
            out SyntaxToken token)
        {
            Contract.ThrowIfNull(formattingRules);
            Contract.ThrowIfNull(root);

            token = default;
            if (!optionSet.GetOption(FormattingOptions.AutoFormattingOnReturn, LanguageNames.CSharp))
            {
                return false;
            }

            if (optionSet.GetOption(FormattingOptions.SmartIndent, LanguageNames.CSharp) != FormattingOptions.IndentStyle.Smart)
            {
                return false;
            }

            var firstNonWhitespacePosition = line.GetFirstNonWhitespacePosition();
            if (!firstNonWhitespacePosition.HasValue)
            {
                return false;
            }

            token = root.FindToken(firstNonWhitespacePosition.Value);
            if (IsInvalidToken(token))
            {
                return false;
            }

            if (token.IsKind(SyntaxKind.None) ||
                token.SpanStart != firstNonWhitespacePosition)
            {
                return false;
            }

            // first see whether there is a line operation for current token
            var previousToken = token.GetPreviousToken(includeZeroWidth: true);

            // only use smart token formatter when we have two visible tokens.
            if (previousToken.Kind() == SyntaxKind.None || previousToken.IsMissing)
            {
                return false;
            }

            var lineOperation = FormattingOperations.GetAdjustNewLinesOperation(formattingRules, previousToken, token, optionSet);
            if (lineOperation == null || lineOperation.Option == AdjustNewLinesOption.ForceLinesIfOnSingleLine)
            {
                // no indentation operation, nothing to do for smart token formatter
                return false;
            }

            // We're pressing enter between two tokens, have the formatter figure out hte appropriate
            // indentation.
            return true;
        }

        private static bool IsInvalidToken(SyntaxToken token)
        {
            // invalid token to be formatted
            return token.IsKind(SyntaxKind.None) ||
                   token.IsKind(SyntaxKind.EndOfDirectiveToken) ||
                   token.IsKind(SyntaxKind.EndOfFileToken);
        }

        private class FormattingRule : AbstractFormattingRule
        {
            public override void AddIndentBlockOperations(List<IndentBlockOperation> list, SyntaxNode node, OptionSet optionSet, in NextIndentBlockOperationAction nextOperation)
            {
                // these nodes should be from syntax tree from ITextSnapshot.
                Debug.Assert(node.SyntaxTree != null);
                Debug.Assert(node.SyntaxTree.GetText() != null);

                nextOperation.Invoke();

                ReplaceCaseIndentationRules(list, node);

                if (node is BaseParameterListSyntax ||
                    node is TypeArgumentListSyntax ||
                    node is TypeParameterListSyntax ||
                    node.IsKind(SyntaxKind.Interpolation))
                {
                    AddIndentBlockOperations(list, node);
                    return;
                }

                if (node is BaseArgumentListSyntax argument &&
                    argument.Parent.Kind() != SyntaxKind.ThisConstructorInitializer &&
                    !IsBracketedArgumentListMissingBrackets(argument as BracketedArgumentListSyntax))
                {
                    AddIndentBlockOperations(list, argument);
                    return;
                }

                // only valid if the user has started to actually type a constructor initializer
                if (node is ConstructorInitializerSyntax { ThisOrBaseKeyword: { IsMissing: false } } constructorInitializer && constructorInitializer.ArgumentList.OpenParenToken.Kind() != SyntaxKind.None)
                {
                    var text = node.SyntaxTree.GetText();

                    // 3 different cases
                    // first case : this or base is the first token on line
                    // second case : colon is the first token on line
                    var colonIsFirstTokenOnLine = !constructorInitializer.ColonToken.IsMissing && constructorInitializer.ColonToken.IsFirstTokenOnLine(text);
                    var thisOrBaseIsFirstTokenOnLine = !constructorInitializer.ThisOrBaseKeyword.IsMissing && constructorInitializer.ThisOrBaseKeyword.IsFirstTokenOnLine(text);

                    if (colonIsFirstTokenOnLine || thisOrBaseIsFirstTokenOnLine)
                    {
                        list.Add(FormattingOperations.CreateRelativeIndentBlockOperation(
                            constructorInitializer.ThisOrBaseKeyword,
                            constructorInitializer.ArgumentList.OpenParenToken.GetNextToken(includeZeroWidth: true),
                            constructorInitializer.ArgumentList.CloseParenToken.GetPreviousToken(includeZeroWidth: true),
                            indentationDelta: 1,
                            option: IndentBlockOption.RelativePosition));
                    }
                    else
                    {
                        // third case : none of them are the first token on the line
                        AddIndentBlockOperations(list, constructorInitializer.ArgumentList);
                    }
                }
            }

            private bool IsBracketedArgumentListMissingBrackets(BracketedArgumentListSyntax node)
            {
                return node != null && node.OpenBracketToken.IsMissing && node.CloseBracketToken.IsMissing;
            }

            private void ReplaceCaseIndentationRules(List<IndentBlockOperation> list, SyntaxNode node)
            {
                if (!(node is SwitchSectionSyntax section) || section.Statements.Count == 0)
                {
                    return;
                }

                var startToken = section.Statements.First().GetFirstToken(includeZeroWidth: true);
                var endToken = section.Statements.Last().GetLastToken(includeZeroWidth: true);

                for (var i = 0; i < list.Count; i++)
                {
                    var operation = list[i];
                    if (operation.StartToken == startToken && operation.EndToken == endToken)
                    {
                        // replace operation
                        list[i] = FormattingOperations.CreateIndentBlockOperation(startToken, endToken, indentationDelta: 1, option: IndentBlockOption.RelativePosition);
                    }
                }
            }

            private static void AddIndentBlockOperations(List<IndentBlockOperation> list, SyntaxNode node)
            {
                // only add indent block operation if the base token is the first token on line
                var text = node.SyntaxTree.GetText();
                var baseToken = node.Parent.GetFirstToken(includeZeroWidth: true);

                list.Add(FormattingOperations.CreateRelativeIndentBlockOperation(
                    baseToken,
                    node.GetFirstToken(includeZeroWidth: true).GetNextToken(includeZeroWidth: true),
                    node.GetLastToken(includeZeroWidth: true).GetPreviousToken(includeZeroWidth: true),
                    indentationDelta: 1,
                    option: IndentBlockOption.RelativeToFirstTokenOnBaseTokenLine));
            }
        }
    }
}
