// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Structure
{
    internal class BlockSyntaxStructureProvider : AbstractSyntaxNodeStructureProvider<BlockSyntax>
    {
        protected override void CollectBlockSpans(
            BlockSyntax node, ArrayBuilder<BlockSpan> spans, CancellationToken cancellationToken)
        {
            var parentKind = node.Parent.Kind();

            // For most types of statements, just consider the block 'attached' to the 
            // parent node.  That means we'll show the parent node header when doing 
            // things like hovering over the indent guide.
            //
            // This also works nicely as the close brace for these constructs will always
            // align with the start of these statements.
            if (node.Parent is StatementSyntax ||
                parentKind == SyntaxKind.CatchClause ||
                parentKind == SyntaxKind.FinallyClause ||
                parentKind == SyntaxKind.ElseClause)
            {
                spans.Add(new BlockSpan(
                    isCollapsible: node.IsParentKind(SyntaxKind.LocalFunctionStatement),
                    textSpan: GetTextSpan(node),
                    hintSpan: node.Parent.Span,
                    type: GetType(node.Parent)));
            }

            // Switch sections are somewhat special.  Say you have the following:
            //
            //      case 0:
            //          {
            //          
            //          }
            //
            // We don't want to consider the block parented by teh case, because 
            // that would cause us to draw the following:
            // 
            //      case 0:
            //      |   {
            //      |   
            //      |   }
            //
            // Which would obviously be wonky.  So in this case, we just use the
            // spanof the block alone, without consideration for the case clause.
            if (parentKind == SyntaxKind.SwitchSection)
            {
                spans.Add(new BlockSpan(
                    isCollapsible: node.IsParentKind(SyntaxKind.LocalFunctionStatement),
                    textSpan: node.Span,
                    hintSpan: node.Parent.Span,
                    type: BlockTypes.Case));
            }
        }

        private TextSpan GetTextSpan(BlockSyntax node)
        {
            var previousToken = node.GetFirstToken().GetPreviousToken();
            if (previousToken.IsKind(SyntaxKind.None))
            {
                return node.Span;
            }

            return TextSpan.FromBounds(previousToken.Span.End, node.Span.End);
        }

        private string GetType(SyntaxNode parent)
        {
            switch (parent.Kind())
            {
                case SyntaxKind.TryStatement: return BlockTypes.TryCatchFinally;
                case SyntaxKind.CatchClause: return BlockTypes.TryCatchFinally;
                case SyntaxKind.FinallyClause: return BlockTypes.TryCatchFinally;

                case SyntaxKind.LockStatement: return BlockTypes.Lock;
                case SyntaxKind.UsingStatement: return BlockTypes.Using;

                case SyntaxKind.ForStatement: return BlockTypes.Loop;
                case SyntaxKind.ForEachStatement: return BlockTypes.Loop;
                case SyntaxKind.ForEachComponentStatement: return BlockTypes.Loop;
                case SyntaxKind.WhileStatement: return BlockTypes.Loop;
                case SyntaxKind.DoStatement: return BlockTypes.Loop;

                case SyntaxKind.IfStatement: return BlockTypes.Conditional;
                case SyntaxKind.ElseClause: return BlockTypes.Conditional;

                case SyntaxKind.Block: return BlockTypes.Standalone;

                case SyntaxKind.LocalFunctionStatement: return BlockTypes.LocalFunction;

                default: return BlockTypes.Other;
            }
        }
    }
}