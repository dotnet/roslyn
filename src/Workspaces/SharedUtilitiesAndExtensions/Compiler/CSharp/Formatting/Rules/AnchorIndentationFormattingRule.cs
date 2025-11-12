// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting.Rules;

namespace Microsoft.CodeAnalysis.CSharp.Formatting;

internal sealed class AnchorIndentationFormattingRule : BaseFormattingRule
{
    internal const string Name = "CSharp Anchor Indentation Formatting Rule";

    public override void AddAnchorIndentationOperations(List<AnchorIndentationOperation> list, SyntaxNode node, in NextAnchorIndentationOperationAction nextOperation)
    {
        nextOperation.Invoke();

        if (node.Kind() is SyntaxKind.SimpleLambdaExpression or SyntaxKind.ParenthesizedLambdaExpression)
        {
            AddAnchorIndentationOperation(list, node);
            return;
        }

        if (node.IsKind(SyntaxKind.AnonymousMethodExpression))
        {
            AddAnchorIndentationOperation(list, node);
            return;
        }

        if (node is BlockSyntax block)
        {
            // if it is not nested block, then its anchor will be first token that this block is
            // associated with. otherwise, "{" of block is the anchor token its children would follow
            if (block.Parent is null or BlockSyntax)
            {
                AddAnchorIndentationOperation(list, block);
                return;
            }
            else
            {
                AddAnchorIndentationOperation(list,
                    block.Parent.GetFirstToken(includeZeroWidth: true),
                    block.GetLastToken(includeZeroWidth: true));
                return;
            }
        }

        switch (node)
        {
            case StatementSyntax statement:
                // Don't add anchor indentation for if statements that are on a separate line from their else keyword.
                // The if statement should be indented like any other embedded statement in that case.
                if (statement is IfStatementSyntax ifStatement && ifStatement.Parent is ElseClauseSyntax elseClause)
                {
                    var elseKeyword = elseClause.ElseKeyword;
                    var ifKeyword = ifStatement.IfKeyword;
                    
                    // Check if there's a newline between the else and if tokens
                    var hasNewLine = elseKeyword.TrailingTrivia.Any(SyntaxKind.EndOfLineTrivia) ||
                                     ifKeyword.LeadingTrivia.Any(SyntaxKind.EndOfLineTrivia);
                    
                    if (hasNewLine)
                    {
                        // Skip adding anchor operation - let the indent operation from IndentBlockFormattingRule apply
                        return;
                    }
                }
                
                AddAnchorIndentationOperation(list, statement);
                return;
            case UsingDirectiveSyntax usingNode:
                AddAnchorIndentationOperation(list, usingNode);
                return;
            case NamespaceDeclarationSyntax namespaceNode:
                AddAnchorIndentationOperation(list, namespaceNode);
                return;
            case BaseTypeDeclarationSyntax typeNode:
                AddAnchorIndentationOperation(list, typeNode);
                return;
            case MemberDeclarationSyntax memberDeclNode:
                AddAnchorIndentationOperation(list, memberDeclNode);
                return;
            case AccessorDeclarationSyntax accessorDeclNode:
                AddAnchorIndentationOperation(list, accessorDeclNode);
                return;
            case SwitchExpressionArmSyntax switchExpressionArm:
                // The expression in a switch expression arm should be anchored to the beginning of the arm
                // ```
                // e switch
                // {
                // pattern:
                //         expression,
                // ```
                // We will keep the relative position of `expression` relative to `pattern:`. It will format to:
                // ```
                // e switch
                // {
                //     pattern:
                //             expression,
                // ```
                AddAnchorIndentationOperation(list, switchExpressionArm);
                return;
        }
    }

    private static void AddAnchorIndentationOperation(List<AnchorIndentationOperation> list, SyntaxNode node)
        => AddAnchorIndentationOperation(list, node.GetFirstToken(includeZeroWidth: true), node.GetLastToken(includeZeroWidth: true));
}
