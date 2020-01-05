// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Structure
{
    internal class BlockSyntaxStructureProvider : AbstractSyntaxNodeStructureProvider<BlockSyntax>
    {
        protected override void CollectBlockSpans(
            BlockSyntax node,
            ArrayBuilder<BlockSpan> spans,
            OptionSet options,
            CancellationToken cancellationToken)
        {
            var parentKind = node.Parent.Kind();

            // For most types of statements, just consider the block 'attached' to the 
            // parent node.  That means we'll show the parent node header when doing 
            // things like hovering over the indent guide.
            //
            // This also works nicely as the close brace for these constructs will always
            // align with the start of these statements.
            if (IsNonBlockStatement(node.Parent) ||
                parentKind == SyntaxKind.ElseClause)
            {
                var type = GetType(node.Parent);
                if (type != null)
                {
                    spans.Add(new BlockSpan(
                        isCollapsible: true,
                        textSpan: GetTextSpan(node),
                        hintSpan: GetHintSpan(node),
                        type: type));
                }
            }

            // Nested blocks aren't attached to anything.  Just collapse them as is.
            // Switch sections are also special.  Say you have the following:
            //
            //      case 0:
            //          {
            //          
            //          }
            //
            // We don't want to consider the block parented by the case, because 
            // that would cause us to draw the following:
            // 
            //      case 0:
            //      |   {
            //      |   
            //      |   }
            //
            // Which would obviously be wonky.  So in this case, we just use the
            // spanof the block alone, without consideration for the case clause.
            if (parentKind == SyntaxKind.Block || parentKind == SyntaxKind.SwitchSection)
            {
                var type = GetType(node.Parent);

                spans.Add(new BlockSpan(
                    isCollapsible: true,
                    textSpan: node.Span,
                    hintSpan: node.Span,
                    type: type));
            }
        }

        private static bool IsNonBlockStatement(SyntaxNode node)
        {
            return node is StatementSyntax && !node.IsKind(SyntaxKind.Block);
        }

        private TextSpan GetHintSpan(BlockSyntax node)
        {
            var start = node.Parent.Span.Start;
            var end = GetEnd(node);
            return TextSpan.FromBounds(start, end);
        }

        private TextSpan GetTextSpan(BlockSyntax node)
        {
            var previousToken = node.GetFirstToken().GetPreviousToken();
            if (previousToken.IsKind(SyntaxKind.None))
            {
                return node.Span;
            }

            return TextSpan.FromBounds(previousToken.Span.End, GetEnd(node));
        }

        private static int GetEnd(BlockSyntax node)
        {
            if (node.Parent.IsKind(SyntaxKind.IfStatement))
            {
                // For an if-statement, just collapse up to the end of the block.
                // We don't want collapse the whole statement just for the 'true'
                // portion.  Also, while outlining might be ok, the Indent-Guide
                // would look very strange for nodes like:
                //
                //      if (goo)
                //      {
                //      }
                //      else
                //          return a ||
                //                 b;
                return node.Span.End;
            }
            else
            {
                // For all other constructs, we collapse up to the end of the parent
                // construct.
                return node.Parent.Span.End;
            }
        }

        private string GetType(SyntaxNode parent)
        {
            switch (parent.Kind())
            {
                case SyntaxKind.ForStatement: return BlockTypes.Loop;
                case SyntaxKind.ForEachStatement: return BlockTypes.Loop;
                case SyntaxKind.ForEachVariableStatement: return BlockTypes.Loop;
                case SyntaxKind.WhileStatement: return BlockTypes.Loop;
                case SyntaxKind.DoStatement: return BlockTypes.Loop;

                case SyntaxKind.TryStatement: return BlockTypes.Statement;
                case SyntaxKind.CatchClause: return BlockTypes.Statement;
                case SyntaxKind.FinallyClause: return BlockTypes.Statement;

                case SyntaxKind.UnsafeStatement: return BlockTypes.Statement;
                case SyntaxKind.FixedStatement: return BlockTypes.Statement;
                case SyntaxKind.LockStatement: return BlockTypes.Statement;
                case SyntaxKind.UsingStatement: return BlockTypes.Statement;

                case SyntaxKind.IfStatement: return BlockTypes.Conditional;
                case SyntaxKind.ElseClause: return BlockTypes.Conditional;
                case SyntaxKind.SwitchSection: return BlockTypes.Conditional;

                case SyntaxKind.Block: return BlockTypes.Statement;

                case SyntaxKind.LocalFunctionStatement: return BlockTypes.Statement;
            }

            return null;
        }
    }
}
