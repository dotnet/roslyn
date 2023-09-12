// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Structure;

internal sealed class BlockSyntaxStructureProvider : AbstractSyntaxNodeStructureProvider<BlockSyntax>
{
    protected override void CollectBlockSpans(
        SyntaxToken previousToken,
        BlockSyntax node,
        ref TemporaryArray<BlockSpan> spans,
        BlockStructureOptions options,
        CancellationToken cancellationToken)
    {
        var parent = node.GetRequiredParent();
        var parentKind = parent.Kind();

        // For most types of statements, just consider the block 'attached' to the parent node.  That means we'll
        // show the parent node header when doing things like hovering over the indent guide.
        //
        // This also works nicely as the close brace for these constructs will always align with the start of these
        // statements.

        // If we're on the topmost if-statement in a chain if/else-if/else-if/else, then all the else-if and
        // final-else blocks as subheadings as well.  This is so that sticky scroll knows what to show when we're
        // nested within one of the deeper elses.
        if (parent is IfStatementSyntax { Parent: not ElseClauseSyntax } ifStatement)
        {
            using var subHeadings = TemporaryArray<(TextSpan textSpan, TextSpan hintSpan, string type)>.Empty;

            for (var currentElse = ifStatement.Else; currentElse != null;)
            {
                StatementSyntax elseStatement;
                if (currentElse.Statement is IfStatementSyntax nextIfStatement)
                {
                    elseStatement = nextIfStatement.Statement;
                    currentElse = nextIfStatement.Else;
                }
                else
                {
                    elseStatement = currentElse.Statement;
                    currentElse = null;
                }

                if (elseStatement is BlockSyntax { IsMissing: false } elseBlock)
                    subHeadings.Add((GetTextSpan(elseBlock), GetHintSpan(elseBlock), BlockTypes.Conditional));
            }

            spans.Add(new BlockSpan(
                BlockTypes.Conditional,
                isCollapsible: true,
                GetTextSpan(node),
                GetHintSpan(node),
                subHeadings.ToImmutableAndClear(),
                autoCollapse: false));
        }
        else if (parent is TryStatementSyntax tryStatement)
        {
            using var subHeadings = TemporaryArray<(TextSpan textSpan, TextSpan hintSpan, string type)>.Empty;

            foreach (var catchClause in tryStatement.Catches)
            {
                if (!catchClause.Block.IsMissing)
                    subHeadings.Add((GetTextSpan(catchClause.Block), GetHintSpan(catchClause.Block), BlockTypes.Statement));
            }

            if (tryStatement.Finally?.Block is { IsMissing: false } finallyBlock)
                subHeadings.Add((GetTextSpan(finallyBlock), GetHintSpan(finallyBlock), BlockTypes.Statement));

            spans.Add(new BlockSpan(
                BlockTypes.Statement,
                isCollapsible: true,
                GetTextSpan(node),
                GetHintSpan(node),
                subHeadings.ToImmutableAndClear(),
                autoCollapse: false));
        }
        else if (parentKind == SyntaxKind.ElseClause || IsNonBlockStatement(parent))
        {
            var autoCollapse = false;

            // Treat a local function at the top level as if it was a definition.  Similarly, if the user has asked
            // to always collapse local functions, then respect that option.
            if (parentKind == SyntaxKind.LocalFunctionStatement)
            {
                autoCollapse =
                    options.CollapseLocalFunctionsWhenCollapsingToDefinitions ||
                    parent.IsParentKind(SyntaxKind.GlobalStatement);
            }

            var type = GetType(parent);
            if (type != null)
            {
                spans.Add(new BlockSpan(
                    isCollapsible: true,
                    textSpan: GetTextSpan(node),
                    hintSpan: GetHintSpan(node),
                    type: type,
                    autoCollapse: autoCollapse));
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
        if (parentKind is SyntaxKind.Block or SyntaxKind.SwitchSection)
        {
            var type = GetType(parent);
            if (type != null)
            {
                spans.Add(new BlockSpan(
                    isCollapsible: true,
                    textSpan: node.Span,
                    hintSpan: node.Span,
                    type: type));
            }
        }
    }

    private static bool IsNonBlockStatement(SyntaxNode node)
        => node is StatementSyntax(kind: not SyntaxKind.Block);

    private static TextSpan GetHintSpan(BlockSyntax node)
    {
        var parent = node.GetRequiredParent();
        if (parent.IsKind(SyntaxKind.IfStatement) && parent.IsParentKind(SyntaxKind.ElseClause))
        {
            parent = parent.GetRequiredParent();
        }

        var start = parent.Span.Start;
        var end = GetEnd(node);
        return TextSpan.FromBounds(start, end);
    }

    private static TextSpan GetTextSpan(BlockSyntax node)
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
            return node.GetRequiredParent().Span.End;
        }
    }

    private static string? GetType(SyntaxNode? parent)
        => (parent?.Kind()) switch
        {
            SyntaxKind.ForStatement => BlockTypes.Loop,
            SyntaxKind.ForEachStatement => BlockTypes.Loop,
            SyntaxKind.ForEachVariableStatement => BlockTypes.Loop,
            SyntaxKind.WhileStatement => BlockTypes.Loop,
            SyntaxKind.DoStatement => BlockTypes.Loop,
            SyntaxKind.TryStatement => BlockTypes.Statement,
            SyntaxKind.CatchClause => BlockTypes.Statement,
            SyntaxKind.FinallyClause => BlockTypes.Statement,
            SyntaxKind.UnsafeStatement => BlockTypes.Statement,
            SyntaxKind.FixedStatement => BlockTypes.Statement,
            SyntaxKind.LockStatement => BlockTypes.Statement,
            SyntaxKind.UsingStatement => BlockTypes.Statement,
            SyntaxKind.IfStatement => BlockTypes.Conditional,
            SyntaxKind.ElseClause => BlockTypes.Conditional,
            SyntaxKind.SwitchSection => BlockTypes.Conditional,
            SyntaxKind.Block => BlockTypes.Statement,
            SyntaxKind.LocalFunctionStatement => BlockTypes.Statement,
            _ => null,
        };
}
