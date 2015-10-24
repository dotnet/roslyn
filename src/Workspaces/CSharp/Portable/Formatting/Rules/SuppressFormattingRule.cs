// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    [ExportFormattingRule(Name, LanguageNames.CSharp), Shared]
    [ExtensionOrder(After = IndentBlockFormattingRule.Name)]
    internal class SuppressFormattingRule : BaseFormattingRule
    {
        internal const string Name = "CSharp Suppress Formatting Rule";

        public override void AddSuppressOperations(List<SuppressOperation> list, SyntaxNode node, SyntaxToken lastToken, OptionSet optionSet, NextAction<SuppressOperation> nextOperation)
        {
            nextOperation.Invoke(list);

            AddInitializerSuppressOperations(list, node);

            AddBraceSuppressOperations(list, node, lastToken);

            AddStatementExceptBlockSuppressOperations(list, node);

            AddSpecificNodesSuppressOperations(list, node);
        }

        private void AddSpecificNodesSuppressOperations(List<SuppressOperation> list, SyntaxNode node)
        {
            var ifStatementNode = node as IfStatementSyntax;
            if (ifStatementNode != null)
            {
                AddSuppressWrappingIfOnSingleLineOperation(list, ifStatementNode.IfKeyword, ifStatementNode.Statement.GetLastToken(includeZeroWidth: true));

                if (ifStatementNode.Else != null)
                {
                    AddSuppressWrappingIfOnSingleLineOperation(list, ifStatementNode.Else.ElseKeyword, ifStatementNode.Else.Statement.GetLastToken(includeZeroWidth: true));
                }

                return;
            }

            var whileStatementNode = node as DoStatementSyntax;
            if (whileStatementNode != null)
            {
                AddSuppressWrappingIfOnSingleLineOperation(list, whileStatementNode.GetFirstToken(includeZeroWidth: true), whileStatementNode.Statement.GetLastToken(includeZeroWidth: true));
                return;
            }

            var memberDeclNode = node as MemberDeclarationSyntax;
            if (memberDeclNode != null)
            {
                var tokens = memberDeclNode.GetFirstAndLastMemberDeclarationTokensAfterAttributes();
                AddSuppressWrappingIfOnSingleLineOperation(list, tokens.Item1, tokens.Item2);
                var propertyDeclNode = node as PropertyDeclarationSyntax;
                if (propertyDeclNode?.Initializer != null && propertyDeclNode?.AccessorList != null)
                {
                    AddSuppressWrappingIfOnSingleLineOperation(list, tokens.Item1, propertyDeclNode.AccessorList.GetLastToken());
                }
                return;
            }

            var accessorDeclNode = node as AccessorDeclarationSyntax;
            if (accessorDeclNode != null)
            {
                AddSuppressWrappingIfOnSingleLineOperation(list, accessorDeclNode.Keyword, accessorDeclNode.GetLastToken(includeZeroWidth: true));
                return;
            }

            var switchSection = node as SwitchSectionSyntax;
            if (switchSection != null)
            {
                if (switchSection.Labels.Count < 2)
                {
                    AddSuppressWrappingIfOnSingleLineOperation(list, switchSection.GetFirstToken(includeZeroWidth: true), switchSection.GetLastToken(includeZeroWidth: true));
                    return;
                }
                else
                {
                    // Add Separate suppression for each Label and for the last label, add the <> 
                    for (int i = 0; i < switchSection.Labels.Count - 1; ++i)
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

            var anonymousMethod = node as AnonymousMethodExpressionSyntax;
            if (anonymousMethod != null)
            {
                AddSuppressWrappingIfOnSingleLineOperation(list, anonymousMethod.DelegateKeyword, anonymousMethod.GetLastToken(includeZeroWidth: true));
                return;
            }

            var parameterNode = node as ParameterSyntax;
            if (parameterNode != null)
            {
                if (parameterNode.AttributeLists.Count != 0)
                {
                    var anchorToken = parameterNode.AttributeLists.First().OpenBracketToken;
                    AddSuppressAllOperationIfOnMultipleLine(list, anchorToken, parameterNode.GetLastToken());
                }
            }

            var tryStatement = node as TryStatementSyntax;
            if (tryStatement != null)
            {
                // Add a suppression operation if the try keyword and the block are in the same line
                if (!tryStatement.TryKeyword.IsMissing && tryStatement.Block != null && !tryStatement.Block.CloseBraceToken.IsMissing)
                {
                    AddSuppressWrappingIfOnSingleLineOperation(list, tryStatement.TryKeyword, tryStatement.Block.CloseBraceToken);
                }
            }

            var catchClause = node as CatchClauseSyntax;
            if (catchClause != null)
            {
                // Add a suppression operation if the catch keyword and the corresponding block are in the same line
                if (!catchClause.CatchKeyword.IsMissing && catchClause.Block != null && !catchClause.Block.CloseBraceToken.IsMissing)
                {
                    AddSuppressWrappingIfOnSingleLineOperation(list, catchClause.CatchKeyword, catchClause.Block.CloseBraceToken);
                }
            }

            var finallyClause = node as FinallyClauseSyntax;
            if (finallyClause != null)
            {
                // Add a suppression operation if the finally keyword and the corresponding block are in the same line
                if (!finallyClause.FinallyKeyword.IsMissing && finallyClause.Block != null && !finallyClause.Block.CloseBraceToken.IsMissing)
                {
                    AddSuppressWrappingIfOnSingleLineOperation(list, finallyClause.FinallyKeyword, finallyClause.Block.CloseBraceToken);
                }
            }
        }

        private void AddStatementExceptBlockSuppressOperations(List<SuppressOperation> list, SyntaxNode node)
        {
            var statementNode = node as StatementSyntax;
            if (statementNode == null || statementNode.Kind() == SyntaxKind.Block)
            {
                return;
            }

            var firstToken = statementNode.GetFirstToken(includeZeroWidth: true);
            var lastToken = statementNode.GetLastToken(includeZeroWidth: true);

            AddSuppressWrappingIfOnSingleLineOperation(list, firstToken, lastToken);
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

            var anonymousCreationNode = node as AnonymousObjectCreationExpressionSyntax;
            if (anonymousCreationNode != null)
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
        {
            var objectCreationNode = node as ObjectCreationExpressionSyntax;
            if (objectCreationNode != null)
            {
                return objectCreationNode.Initializer;
            }

            var arrayCreationNode = node as ArrayCreationExpressionSyntax;
            if (arrayCreationNode != null)
            {
                return arrayCreationNode.Initializer;
            }

            var implicitArrayNode = node as ImplicitArrayCreationExpressionSyntax;
            if (implicitArrayNode != null)
            {
                return implicitArrayNode.Initializer;
            }

            return null;
        }
    }
}
