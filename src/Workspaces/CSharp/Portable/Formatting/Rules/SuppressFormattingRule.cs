// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    internal class SuppressFormattingRule : BaseFormattingRule
    {
        internal const string Name = "CSharp Suppress Formatting Rule";

        public override void AddSuppressOperations(List<SuppressOperation> list, SyntaxNode node, OptionSet optionSet, in NextSuppressOperationAction nextOperation)
        {
            nextOperation.Invoke();

            AddFormatSuppressOperations(list, node);

            AddInitializerSuppressOperations(list, node);

            AddBraceSuppressOperations(list, node);

            AddStatementExceptBlockSuppressOperations(list, node);

            AddSpecificNodesSuppressOperations(list, node);
        }

        private void AddSpecificNodesSuppressOperations(List<SuppressOperation> list, SyntaxNode node)
        {
            if (node is IfStatementSyntax ifStatementNode)
            {
                AddSuppressWrappingIfOnSingleLineOperation(list, ifStatementNode.IfKeyword, ifStatementNode.Statement.GetLastToken(includeZeroWidth: true));

                if (ifStatementNode.Else != null)
                {
                    AddSuppressWrappingIfOnSingleLineOperation(list, ifStatementNode.Else.ElseKeyword, ifStatementNode.Else.Statement.GetLastToken(includeZeroWidth: true));
                }

                return;
            }

            // ex: `e is Type ( /* positional */ )`
            if (node.IsKind(SyntaxKindEx.RecursivePattern))
            {
#if !CODE_STYLE
                var positional = ((RecursivePatternSyntax)node).PositionalPatternClause;
                var property = ((RecursivePatternSyntax)node).PropertyPatternClause;
#else
                var positional = node.ChildNodes().SingleOrDefault(child => child.IsKind(SyntaxKindEx.PositionalPatternClause));
                var property = node.ChildNodes().SingleOrDefault(child => child.IsKind(SyntaxKindEx.PropertyPatternClause));
#endif
                if (positional != null)
                {
#if !CODE_STYLE
                    var openParenToken = positional.OpenParenToken;
                    var closeParenToken = positional.CloseParenToken;
#else
                    var openParenToken = positional.ChildTokens().SingleOrDefault(token => token.IsKind(SyntaxKind.OpenParenToken));
                    var closeParenToken = positional.ChildTokens().SingleOrDefault(token => token.IsKind(SyntaxKind.CloseParenToken));
#endif
                    // Formatting should refrain from inserting new lines, unless the user already split across multiple lines
                    AddSuppressWrappingIfOnSingleLineOperation(list, openParenToken, closeParenToken);
                    if (property != null)
                    {
                        AddSuppressWrappingIfOnSingleLineOperation(list, openParenToken, property.GetLastToken());
                    }
                }

                // ex: `Property: <pattern>` inside a recursive pattern, such as `e is { Property: <pattern>, ... }`
                else if (property != null)
                {
#if !CODE_STYLE
                    var openBraceToken = property.OpenBraceToken;
                    var closeBraceToken = property.CloseBraceToken;
#else
                    var openBraceToken = property.ChildTokens().SingleOrDefault(token => token.IsKind(SyntaxKind.OpenBraceToken));
                    var closeBraceToken = property.ChildTokens().SingleOrDefault(token => token.IsKind(SyntaxKind.CloseBraceToken));
#endif
                    // Formatting should refrain from inserting new lines, unless the user already split across multiple lines
                    AddSuppressWrappingIfOnSingleLineOperation(list, openBraceToken, closeBraceToken);
                }

                return;
            }

            // ex: `<pattern>: expression` inside a switch expression, such as `e switch { <pattern>: expression, ... }`
            if (node.IsKind(SyntaxKindEx.SwitchExpressionArm))
            {
                // Formatting should refrain from inserting new lines, unless the user already split across multiple lines
                AddSuppressWrappingIfOnSingleLineOperation(list, node.GetFirstToken(), node.GetLastToken());
                return;
            }

            // ex: `e switch { <pattern>: expression, ... }`
            if (node.IsKind(SyntaxKindEx.SwitchExpression))
            {
                // Formatting should refrain from inserting new lines, unless the user already split across multiple lines
                AddSuppressWrappingIfOnSingleLineOperation(list, node.GetFirstToken(), node.GetLastToken());
                return;
            }

            // ex: `case <pattern>:` inside a switch statement
            if (node is CasePatternSwitchLabelSyntax casePattern)
            {
                // Formatting should refrain from inserting new lines, unless the user already split across multiple lines
                AddSuppressWrappingIfOnSingleLineOperation(list, casePattern.GetFirstToken(), casePattern.GetLastToken());
                return;
            }

            // ex: `expression is <pattern>`
            if (node is IsPatternExpressionSyntax isPattern)
            {
                // Formatting should refrain from inserting new lines, unless the user already split across multiple lines
                AddSuppressWrappingIfOnSingleLineOperation(list, isPattern.GetFirstToken(), isPattern.GetLastToken());

                if (isPattern.Pattern.IsKind(SyntaxKindEx.RecursivePattern))
                {
                    // ex:
                    // ```
                    // _ = expr is (1, 2) { }$$
                    // M();
                    // ```
                    // or:
                    // ```
                    // _ = expr is { }$$
                    // M();
                    // ```
#if !CODE_STYLE
                    var propertyPatternClause = ((RecursivePatternSyntax)isPattern.Pattern).PropertyPatternClause;
#else
                    var propertyPatternClause = isPattern.Pattern.ChildNodes().SingleOrDefault(child => child.IsKind(SyntaxKindEx.PropertyPatternClause));
#endif
                    if (propertyPatternClause != null)
                    {
                        AddSuppressWrappingIfOnSingleLineOperation(list, isPattern.IsKeyword, propertyPatternClause.GetLastToken());
                    }
                }

                return;
            }

            if (node is ConstructorInitializerSyntax constructorInitializerNode)
            {
                var constructorDeclarationNode = constructorInitializerNode.Parent as ConstructorDeclarationSyntax;
                if (constructorDeclarationNode?.Body != null)
                {
                    AddSuppressWrappingIfOnSingleLineOperation(list, constructorInitializerNode.ColonToken, constructorDeclarationNode.Body.CloseBraceToken);
                }

                return;
            }

            if (node is DoStatementSyntax whileStatementNode)
            {
                AddSuppressWrappingIfOnSingleLineOperation(list, whileStatementNode.GetFirstToken(includeZeroWidth: true), whileStatementNode.Statement.GetLastToken(includeZeroWidth: true));
                return;
            }

            if (node is MemberDeclarationSyntax memberDeclNode)
            {
                // Attempt to keep the part of a member that follows the attributes on a single
                // line if that's how it's currently written.
                var tokens = memberDeclNode.GetFirstAndLastMemberDeclarationTokensAfterAttributes();
                AddSuppressWrappingIfOnSingleLineOperation(list, tokens.Item1, tokens.Item2);

                // Also, If the member is on single line with its attributes on it, then keep 
                // it on a single line.  This is for code like the following:
                //
                //      [Import] public int Field1;
                //      [Import] public int Field2;
                var attributes = memberDeclNode.GetAttributes();
                var endToken = node.GetLastToken(includeZeroWidth: true);
                for (var i = 0; i < attributes.Count; ++i)
                {
                    AddSuppressWrappingIfOnSingleLineOperation(list,
                        attributes[i].GetFirstToken(includeZeroWidth: true),
                        endToken);
                }

                var propertyDeclNode = node as PropertyDeclarationSyntax;
                if (propertyDeclNode?.Initializer != null && propertyDeclNode?.AccessorList != null)
                {
                    AddSuppressWrappingIfOnSingleLineOperation(list, tokens.Item1, propertyDeclNode.AccessorList.GetLastToken());
                }

                return;
            }

            if (node is AccessorDeclarationSyntax accessorDeclNode)
            {
                AddSuppressWrappingIfOnSingleLineOperation(list, accessorDeclNode.Keyword, accessorDeclNode.GetLastToken(includeZeroWidth: true));
                return;
            }

            if (node is SwitchSectionSyntax switchSection)
            {
                if (switchSection.Labels.Count < 2)
                {
                    AddSuppressWrappingIfOnSingleLineOperation(list, switchSection.GetFirstToken(includeZeroWidth: true), switchSection.GetLastToken(includeZeroWidth: true));
                    return;
                }
                else
                {
                    // Add Separate suppression for each Label and for the last label, add the <> 
                    for (var i = 0; i < switchSection.Labels.Count - 1; ++i)
                    {
                        if (switchSection.Labels[i] != null)
                        {
                            AddSuppressWrappingIfOnSingleLineOperation(list, switchSection.Labels[i].GetFirstToken(includeZeroWidth: true), switchSection.Labels[i].GetLastToken(includeZeroWidth: true));
                        }
                    }

                    // For the last label add the rest of the statements of the switch
                    if (switchSection.Labels[switchSection.Labels.Count - 1] != null)
                    {
                        AddSuppressWrappingIfOnSingleLineOperation(list, switchSection.Labels[switchSection.Labels.Count - 1].GetFirstToken(includeZeroWidth: true), switchSection.GetLastToken(includeZeroWidth: true));
                    }

                    return;
                }
            }

            if (node is AnonymousFunctionExpressionSyntax ||
                node is LocalFunctionStatementSyntax)
            {
                AddSuppressWrappingIfOnSingleLineOperation(list,
                    node.GetFirstToken(includeZeroWidth: true),
                    node.GetLastToken(includeZeroWidth: true),
                    SuppressOption.IgnoreElasticWrapping);
                return;
            }

            if (node is ParameterSyntax parameterNode)
            {
                if (parameterNode.AttributeLists.Count != 0)
                {
                    var anchorToken = parameterNode.AttributeLists.First().OpenBracketToken;
                    AddSuppressAllOperationIfOnMultipleLine(list, anchorToken, parameterNode.GetLastToken());
                }
            }

            if (node is TryStatementSyntax tryStatement)
            {
                // Add a suppression operation if the try keyword and the block are in the same line
                if (!tryStatement.TryKeyword.IsMissing && tryStatement.Block != null && !tryStatement.Block.CloseBraceToken.IsMissing)
                {
                    AddSuppressWrappingIfOnSingleLineOperation(list, tryStatement.TryKeyword, tryStatement.Block.CloseBraceToken);
                }
            }

            if (node is CatchClauseSyntax catchClause)
            {
                // Add a suppression operation if the catch keyword and the corresponding block are in the same line
                if (!catchClause.CatchKeyword.IsMissing && catchClause.Block != null && !catchClause.Block.CloseBraceToken.IsMissing)
                {
                    AddSuppressWrappingIfOnSingleLineOperation(list, catchClause.CatchKeyword, catchClause.Block.CloseBraceToken);
                }
            }

            if (node is FinallyClauseSyntax finallyClause)
            {
                // Add a suppression operation if the finally keyword and the corresponding block are in the same line
                if (!finallyClause.FinallyKeyword.IsMissing && finallyClause.Block != null && !finallyClause.Block.CloseBraceToken.IsMissing)
                {
                    AddSuppressWrappingIfOnSingleLineOperation(list, finallyClause.FinallyKeyword, finallyClause.Block.CloseBraceToken);
                }
            }

            if (node is InterpolatedStringExpressionSyntax interpolatedStringExpression)
            {
                AddSuppressWrappingIfOnSingleLineOperation(list, interpolatedStringExpression.StringStartToken, interpolatedStringExpression.StringEndToken);
            }
        }

        private void AddStatementExceptBlockSuppressOperations(List<SuppressOperation> list, SyntaxNode node)
        {
            if (!(node is StatementSyntax statementNode) || statementNode.Kind() == SyntaxKind.Block)
            {
                return;
            }

            var firstToken = statementNode.GetFirstToken(includeZeroWidth: true);
            var lastToken = statementNode.GetLastToken(includeZeroWidth: true);

            AddSuppressWrappingIfOnSingleLineOperation(list, firstToken, lastToken);
        }

        private void AddFormatSuppressOperations(List<SuppressOperation> list, SyntaxNode node)
        {
            if (!node.ContainsDirectives)
            {
                return;
            }

            foreach (var child in node.ChildNodesAndTokens())
            {
                if (!child.IsToken)
                {
                    continue;
                }

                ProcessTriviaList(list, child.AsToken().LeadingTrivia);
                ProcessTriviaList(list, child.AsToken().TrailingTrivia);
            }

            return;

            // Local functions
            static void ProcessTriviaList(List<SuppressOperation> list, SyntaxTriviaList triviaList)
            {
                foreach (var trivia in triviaList)
                {
                    ProcessTrivia(list, trivia);
                }
            }

            static void ProcessTrivia(List<SuppressOperation> list, SyntaxTrivia trivia)
            {
                if (!(trivia.HasStructure))
                {
                    return;
                }

                ProcessStructuredTrivia(list, trivia.GetStructure());
            }

            static void ProcessStructuredTrivia(List<SuppressOperation> list, SyntaxNode structure)
            {
                if (!(structure is PragmaWarningDirectiveTriviaSyntax pragmaWarningDirectiveTrivia))
                {
                    return;
                }

                if (!IsFormatDirective(pragmaWarningDirectiveTrivia, SyntaxKind.DisableKeyword))
                {
                    return;
                }

                var startToken = pragmaWarningDirectiveTrivia.GetLastToken(includeZeroWidth: true, includeDirectives: true);

                var endDirective = pragmaWarningDirectiveTrivia.GetNextDirective(trivia => IsFormatDirective(trivia, SyntaxKind.RestoreKeyword));
                var endToken = endDirective is null
                    ? ((CompilationUnitSyntax)structure.SyntaxTree.GetRoot(CancellationToken.None)).EndOfFileToken
                    : endDirective.GetFirstToken(includeDirectives: true);

                Debug.Assert(!startToken.IsKind(SyntaxKind.None) && !endToken.IsKind(SyntaxKind.None));
                var textSpan = TextSpan.FromBounds(startToken.Span.End, endToken.SpanStart);
                list.Add(new SuppressOperation(startToken, endToken, textSpan, SuppressOption.DisableFormatting));
            }
        }

        private static bool IsFormatDirective(DirectiveTriviaSyntax trivia, SyntaxKind disableOrRestoreKeyword)
        {
            if (!(trivia is PragmaWarningDirectiveTriviaSyntax pragmaWarningDirectiveTrivia))
            {
                return false;
            }

            if (!pragmaWarningDirectiveTrivia.DisableOrRestoreKeyword.IsKind(disableOrRestoreKeyword))
            {
                return false;
            }

            foreach (var errorCode in pragmaWarningDirectiveTrivia.ErrorCodes)
            {
                if (!(errorCode is IdentifierNameSyntax identifierName))
                {
                    continue;
                }

                if (identifierName.Identifier.ValueText.Equals(FormattingDiagnosticIds.FormatDocumentControlDiagnosticId)
                    || identifierName.Identifier.ValueText.Equals(FormattingDiagnosticIds.FormattingDiagnosticId))
                {
                    return true;
                }
            }

            return false;
        }

        private void AddInitializerSuppressOperations(List<SuppressOperation> list, SyntaxNode node)
        {
            // array or collection initializer case
            if (node.IsInitializerForArrayOrCollectionCreationExpression())
            {
                var arrayOrCollectionInitializer = node as InitializerExpressionSyntax;
                AddSuppressAllOperationIfOnMultipleLine(list, arrayOrCollectionInitializer.OpenBraceToken.GetPreviousToken(includeZeroWidth: true), arrayOrCollectionInitializer.CloseBraceToken);
                return;
            }

            var initializer = GetInitializerNode(node);
            if (initializer != null)
            {
                AddInitializerSuppressOperations(list, initializer.Parent, initializer.Expressions);
                return;
            }

            if (node is AnonymousObjectCreationExpressionSyntax anonymousCreationNode)
            {
                AddInitializerSuppressOperations(list, anonymousCreationNode, anonymousCreationNode.Initializers);
                return;
            }
        }

        private void AddInitializerSuppressOperations(List<SuppressOperation> list, SyntaxNode parent, IEnumerable<SyntaxNode> items)
        {
            // make creation node itself to not break into multiple line, if it is on same line
            AddSuppressWrappingIfOnSingleLineOperation(list, parent.GetFirstToken(includeZeroWidth: true), parent.GetLastToken(includeZeroWidth: true));

            // make each initializer expression to not break into multiple line if it is on same line
            foreach (var item in items)
            {
                var firstToken = item.GetFirstToken(includeZeroWidth: true);
                var lastToken = item.GetLastToken(includeZeroWidth: true);

                if (!firstToken.Equals(lastToken))
                {
                    AddSuppressWrappingIfOnSingleLineOperation(list, firstToken, lastToken);
                }
            }
        }

        private InitializerExpressionSyntax GetInitializerNode(SyntaxNode node)
            => node switch
            {
                ObjectCreationExpressionSyntax objectCreationNode => objectCreationNode.Initializer,
                ArrayCreationExpressionSyntax arrayCreationNode => arrayCreationNode.Initializer,
                ImplicitArrayCreationExpressionSyntax implicitArrayNode => implicitArrayNode.Initializer,
                _ => null,
            };
    }
}
