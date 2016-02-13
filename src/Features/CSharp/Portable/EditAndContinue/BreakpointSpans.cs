// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue
{
    internal static class BreakpointSpans
    {
        public static bool TryGetBreakpointSpan(SyntaxTree tree, int position, CancellationToken cancellationToken, out TextSpan breakpointSpan)
        {
            var source = tree.GetText(cancellationToken);

            // If the line is entirely whitespace, then don't set any breakpoint there.
            var line = source.Lines.GetLineFromPosition(position);
            if (IsBlank(line))
            {
                breakpointSpan = default(TextSpan);
                return false;
            }

            // If the user is asking for breakpoint in an inactive region, then just create a line
            // breakpoint there.
            if (tree.IsInInactiveRegion(position, cancellationToken))
            {
                breakpointSpan = default(TextSpan);
                return true;
            }

            var root = tree.GetRoot(cancellationToken);
            return TryGetClosestBreakpointSpan(root, position, out breakpointSpan);
        }

        private static bool IsBlank(TextLine line)
        {
            var text = line.ToString();

            for (int i = 0; i < text.Length; i++)
            {
                if (!SyntaxFacts.IsWhitespace(text[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Given a syntax token determines a text span delimited by the closest applicable sequence points 
        /// encompassing the token.
        /// </summary>
        /// <remarks>
        /// If the span exists it is possible to place a breakpoint at the given position.
        /// </remarks>
        public static bool TryGetClosestBreakpointSpan(SyntaxNode root, int position, out TextSpan span)
        {
            SyntaxNode node = root.FindToken(position).Parent;
            while (node != null)
            {
                TextSpan? breakpointSpan = TryCreateSpanForNode(node, position);
                if (breakpointSpan.HasValue)
                {
                    span = breakpointSpan.Value;
                    return span != default(TextSpan);
                }

                node = node.Parent;
            }

            span = default(TextSpan);
            return false;
        }

        private static TextSpan CreateSpan(SyntaxToken startToken, SyntaxToken endToken)
        {
            return TextSpan.FromBounds(startToken.SpanStart, endToken.Span.End);
        }

        private static TextSpan CreateSpan(SyntaxNode node)
        {
            return TextSpan.FromBounds(node.SpanStart, node.Span.End);
        }

        private static TextSpan CreateSpan(SyntaxNode node, SyntaxToken token)
        {
            return TextSpan.FromBounds(node.SpanStart, token.Span.End);
        }

        private static TextSpan CreateSpan(SyntaxToken token)
        {
            return TextSpan.FromBounds(token.SpanStart, token.Span.End);
        }

        private static TextSpan CreateSpan(SyntaxTokenList startOpt, SyntaxNodeOrToken startFallbackOpt, SyntaxNodeOrToken endOpt)
        {
            Debug.Assert(startFallbackOpt != default(SyntaxNodeOrToken) || endOpt != default(SyntaxNodeOrToken));

            int startPos;
            if (startOpt.Count > 0)
            {
                startPos = startOpt.First().SpanStart;
            }
            else if (startFallbackOpt != default(SyntaxNodeOrToken))
            {
                startPos = startFallbackOpt.SpanStart;
            }
            else
            {
                startPos = endOpt.SpanStart;
            }

            int endPos;
            if (endOpt != default(SyntaxNodeOrToken))
            {
                endPos = GetEndPosition(endOpt);
            }
            else
            {
                endPos = GetEndPosition(startFallbackOpt);
            }

            return TextSpan.FromBounds(startPos, endPos);
        }

        private static int GetEndPosition(SyntaxNodeOrToken nodeOrToken)
        {
            if (nodeOrToken.IsToken)
            {
                return nodeOrToken.Span.End;
            }
            else
            {
                return nodeOrToken.AsNode().GetLastToken().Span.End;
            }
        }

        private static TextSpan? TryCreateSpanForNode(SyntaxNode node, int position)
        {
            if (node == null)
            {
                return null;
            }

            switch (node.Kind())
            {
                case SyntaxKind.MethodDeclaration:
                    var methodDeclaration = (MethodDeclarationSyntax)node;
                    return (methodDeclaration.Body != null) ? CreateSpanForBlock(methodDeclaration.Body, position) : methodDeclaration.ExpressionBody?.Expression.Span;

                case SyntaxKind.OperatorDeclaration:
                    var operatorDeclaration = (OperatorDeclarationSyntax)node;
                    return (operatorDeclaration.Body != null) ? CreateSpanForBlock(operatorDeclaration.Body, position) : operatorDeclaration.ExpressionBody?.Expression.Span;

                case SyntaxKind.ConversionOperatorDeclaration:
                    var conversionDeclaration = (ConversionOperatorDeclarationSyntax)node;
                    return (conversionDeclaration.Body != null) ? CreateSpanForBlock(conversionDeclaration.Body, position) : conversionDeclaration.ExpressionBody?.Expression.Span;

                case SyntaxKind.DestructorDeclaration:
                    return TryCreateSpanForNode(((DestructorDeclarationSyntax)node).Body, position);

                case SyntaxKind.ConstructorDeclaration:
                    return CreateSpanForConstructorDeclaration((ConstructorDeclarationSyntax)node);

                case SyntaxKind.VariableDeclarator:
                    // handled by the parent node
                    return null;

                case SyntaxKind.VariableDeclaration:
                    return TryCreateSpanForVariableDeclaration((VariableDeclarationSyntax)node, position);

                case SyntaxKind.EventFieldDeclaration:
                case SyntaxKind.FieldDeclaration:
                    return TryCreateSpanForFieldDeclaration((BaseFieldDeclarationSyntax)node, position);

                case SyntaxKind.ElseClause:
                    return TryCreateSpanForNode(((ElseClauseSyntax)node).Statement, position);

                case SyntaxKind.CatchFilterClause:
                    return CreateSpan(node);

                case SyntaxKind.CatchClause:
                    return CreateSpanForCatchClause((CatchClauseSyntax)node, position);

                case SyntaxKind.FinallyClause:
                    return TryCreateSpanForNode(((FinallyClauseSyntax)node).Block, position);

                case SyntaxKind.CaseSwitchLabel:
                case SyntaxKind.DefaultSwitchLabel:
                    return TryCreateSpanForSwitchLabel((SwitchLabelSyntax)node, position);

                case SyntaxKind.GetAccessorDeclaration:
                case SyntaxKind.SetAccessorDeclaration:
                    var body = ((AccessorDeclarationSyntax)node).Body;
                    if (body == null)
                    {
                        return CreateSpan(node);
                    }

                    return TryCreateSpanForNode(body, position);

                case SyntaxKind.AddAccessorDeclaration:
                case SyntaxKind.RemoveAccessorDeclaration:
                case SyntaxKind.UnknownAccessorDeclaration:
                    return TryCreateSpanForNode(((AccessorDeclarationSyntax)node).Body, position);

                case SyntaxKind.PropertyDeclaration:
                    var property = (PropertyDeclarationSyntax)node;

                    // Note that expression body, initializer and accessors are mutually exclusive.

                    // int P => [|expr|]
                    if (property.ExpressionBody != null)
                    {
                        return property.ExpressionBody.Expression.Span;
                    }

                    // int P { get; set; } = [|expr|]
                    if (property.Initializer != null && position >= property.Initializer.FullSpan.Start)
                    {
                        return property.Initializer.Value.Span;
                    }

                    // int P { get [|{|] ... } set { ... } }
                    // int P { [|get;|] [|set;|] }
                    return CreateSpanForAccessors(property.AccessorList.Accessors, position);

                case SyntaxKind.IndexerDeclaration:
                    // int this[args] => [|expr|]
                    var indexer = (IndexerDeclarationSyntax)node;
                    if (indexer.ExpressionBody != null)
                    {
                        return indexer.ExpressionBody.Expression.Span;
                    }

                    // int this[args] { get [|{|] ... } set { ... } }
                    return CreateSpanForAccessors(indexer.AccessorList.Accessors, position);

                case SyntaxKind.EventDeclaration:
                    var evnt = (EventDeclarationSyntax)node;
                    return evnt.AccessorList.Accessors.Select(a => TryCreateSpanForNode(a, position)).FirstOrDefault();

                case SyntaxKind.BaseConstructorInitializer:
                case SyntaxKind.ThisConstructorInitializer:
                    return CreateSpanForConstructorInitializer((ConstructorInitializerSyntax)node);

                // Query clauses:
                // 
                // Used when the user's initial location is on a query keyword itself (as
                // opposed to inside an expression inside the query clause).  It places the bp on the
                // appropriate child expression in the clause.

                case SyntaxKind.FromClause:
                    var fromClause = (FromClauseSyntax)node;
                    return TryCreateSpanForNode(fromClause.Expression, position);

                case SyntaxKind.JoinClause:
                    var joinClause = (JoinClauseSyntax)node;
                    return TryCreateSpanForNode(joinClause.LeftExpression, position);

                case SyntaxKind.LetClause:
                    var letClause = (LetClauseSyntax)node;
                    return TryCreateSpanForNode(letClause.Expression, position);

                case SyntaxKind.WhereClause:
                    var whereClause = (WhereClauseSyntax)node;
                    return TryCreateSpanForNode(whereClause.Condition, position);

                case SyntaxKind.OrderByClause:
                    var orderByClause = (OrderByClauseSyntax)node;
                    return orderByClause.Orderings.Count > 0
                            ? TryCreateSpanForNode(orderByClause.Orderings.First().Expression, position)
                            : null;

                case SyntaxKind.SelectClause:
                    var selectClause = (SelectClauseSyntax)node;
                    return TryCreateSpanForNode(selectClause.Expression, position);

                case SyntaxKind.GroupClause:
                    var groupClause = (GroupClauseSyntax)node;
                    return TryCreateSpanForNode(groupClause.GroupExpression, position);

                default:
                    var expression = node as ExpressionSyntax;
                    if (expression != null)
                    {
                        return IsBreakableExpression(expression) ? CreateSpan(expression) : default(TextSpan?);
                    }

                    var statement = node as StatementSyntax;
                    if (statement != null)
                    {
                        return TryCreateSpanForStatement(statement, position);
                    }

                    return null;
            }
        }

        private static TextSpan CreateSpanForConstructorDeclaration(ConstructorDeclarationSyntax constructorSyntax)
        {
            if (constructorSyntax.Initializer != null)
            {
                return CreateSpanForConstructorInitializer(constructorSyntax.Initializer);
            }

            // static ctor doesn't have a default initializer:
            if (constructorSyntax.Modifiers.Any(SyntaxKind.StaticKeyword))
            {
                return CreateSpan(constructorSyntax.Body.OpenBraceToken);
            }

            return CreateSpan(constructorSyntax.Modifiers, constructorSyntax.Identifier, constructorSyntax.ParameterList.CloseParenToken);
        }

        private static TextSpan CreateSpanForConstructorInitializer(ConstructorInitializerSyntax constructorInitializer)
        {
            return CreateSpan(constructorInitializer.ThisOrBaseKeyword, constructorInitializer.ArgumentList.CloseParenToken);
        }

        private static TextSpan? TryCreateSpanForFieldDeclaration(BaseFieldDeclarationSyntax fieldDeclaration, int position)
        {
            return TryCreateSpanForVariableDeclaration(fieldDeclaration.Declaration, fieldDeclaration.Modifiers, fieldDeclaration.SemicolonToken, position);
        }

        private static TextSpan? TryCreateSpanForSwitchLabel(SwitchLabelSyntax switchLabel, int position)
        {
            var switchSection = switchLabel.Parent as SwitchSectionSyntax;
            if (switchSection == null || switchSection.Statements.Count == 0)
            {
                return null;
            }

            return TryCreateSpanForNode(switchSection.Statements[0], position);
        }

        private static TextSpan CreateSpanForBlock(BlockSyntax block, int position)
        {
            // If the user was on the close curly of the block, then set the breakpoint
            // there.  Otherwise, set it on the open curly.
            if (position >= block.OpenBraceToken.FullSpan.End)
            {
                return CreateSpan(block.CloseBraceToken);
            }
            else
            {
                return CreateSpan(block.OpenBraceToken);
            }
        }

        private static TextSpan? TryCreateSpanForStatement(StatementSyntax statement, int position)
        {
            if (statement == null)
            {
                return null;
            }

            switch (statement.Kind())
            {
                case SyntaxKind.Block:
                    return CreateSpanForBlock((BlockSyntax)statement, position);

                case SyntaxKind.LocalDeclarationStatement:
                    // If the declaration has multiple variables then just set the breakpoint on the first
                    // variable declarator.  Otherwise, set the breakpoint over this entire
                    // statement.
                    var declarationStatement = (LocalDeclarationStatementSyntax)statement;
                    return TryCreateSpanForVariableDeclaration(declarationStatement.Declaration, declarationStatement.Modifiers,
                        declarationStatement.SemicolonToken, position);

                case SyntaxKind.LabeledStatement:
                    // Create the breakpoint on the actual statement we are labeling:
                    var labeledStatement = (LabeledStatementSyntax)statement;
                    return TryCreateSpanForStatement(labeledStatement.Statement, position);

                case SyntaxKind.WhileStatement:
                    // Note: if the user was in the body of the while, then we would have hit its
                    // nested statement on the way up.  This means we must be on the "while(expr)"
                    // part.  Rather than putting a bp on the entire statement, just put it on the
                    // top portion.
                    var whileStatement = (WhileStatementSyntax)statement;
                    return CreateSpan(whileStatement, whileStatement.CloseParenToken);

                case SyntaxKind.DoStatement:
                    // Note: if the user was in the body of the while, then we would have hit its nested
                    // statement on the way up.  This means we're either in the "while(expr)" portion or
                    // the "do" portion. 
                    var doStatement = (DoStatementSyntax)statement;
                    if (position < doStatement.Statement.Span.Start)
                    {
                        return TryCreateSpanForStatement(doStatement.Statement, position);
                    }
                    else
                    {
                        return CreateSpan(doStatement.WhileKeyword,
                            LastNotMissing(doStatement.CloseParenToken, doStatement.SemicolonToken));
                    }

                case SyntaxKind.ForStatement:
                    // Note: if the user was in the body of the for, then we would have hit its nested
                    // statement on the way up.  If they were in the condition or the incrementors, then
                    // we would have those on the way up as well (in TryCreateBreakpointSpanForExpression or
                    // CreateBreakpointSpanForVariableDeclarator). So the user must be on the 'for'
                    // itself. in that case, set the bp on the variable declaration or initializers
                    var forStatement = (ForStatementSyntax)statement;
                    if (forStatement.Declaration != null)
                    {
                        // for (int i = 0; ...
                        var firstVariable = forStatement.Declaration.Variables.FirstOrDefault();
                        return CreateSpan(default(SyntaxTokenList), forStatement.Declaration.Type, firstVariable);
                    }
                    else if (forStatement.Initializers.Count > 0)
                    {
                        // for (i = 0; ...
                        return CreateSpan(forStatement.Initializers[0]);
                    }
                    else if (forStatement.Condition != null)
                    {
                        // for (; i > 0; ...)
                        return CreateSpan(forStatement.Condition);
                    }
                    else if (forStatement.Incrementors.Count > 0)
                    {
                        // for (;;...)
                        return CreateSpan(forStatement.Incrementors[0]);
                    }
                    else
                    {
                        // for (;;)
                        //
                        // In this case, just set the bp on the contained statement.
                        return TryCreateSpanForStatement(forStatement.Statement, position);
                    }

                case SyntaxKind.ForEachStatement:
                    // Note: if the user was in the body of the foreach, then we would have hit its
                    // nested statement on the way up.  If they were in the expression then we would
                    // have hit that on the way up as well. In "foreach(var f in expr)" we allow a
                    // bp on "foreach", "var f" and "in".
                    var forEachStatement = (ForEachStatementSyntax)statement;
                    if (position < forEachStatement.OpenParenToken.Span.End || position > forEachStatement.CloseParenToken.SpanStart)
                    {
                        return CreateSpan(forEachStatement.ForEachKeyword);
                    }
                    else if (position < forEachStatement.InKeyword.FullSpan.Start)
                    {
                        return CreateSpan(forEachStatement.Type, forEachStatement.Identifier);
                    }
                    else if (position < forEachStatement.Expression.FullSpan.Start)
                    {
                        return CreateSpan(forEachStatement.InKeyword);
                    }
                    else
                    {
                        return CreateSpan(forEachStatement.Expression);
                    }

                case SyntaxKind.UsingStatement:
                    var usingStatement = (UsingStatementSyntax)statement;
                    if (usingStatement.Declaration != null)
                    {
                        return TryCreateSpanForNode(usingStatement.Declaration, position);
                    }
                    else
                    {
                        return CreateSpan(usingStatement, usingStatement.CloseParenToken);
                    }

                case SyntaxKind.FixedStatement:
                    var fixedStatement = (FixedStatementSyntax)statement;
                    return TryCreateSpanForNode(fixedStatement.Declaration, position);

                case SyntaxKind.CheckedStatement:
                case SyntaxKind.UncheckedStatement:
                    var checkedStatement = (CheckedStatementSyntax)statement;
                    return TryCreateSpanForStatement(checkedStatement.Block, position);

                case SyntaxKind.UnsafeStatement:
                    var unsafeStatement = (UnsafeStatementSyntax)statement;
                    return TryCreateSpanForStatement(unsafeStatement.Block, position);

                case SyntaxKind.LockStatement:
                    // Note: if the user was in the body of the 'lock', then we would have hit its
                    // nested statement on the way up.  This means we must be on the "lock(expr)" part.
                    // Rather than putting a bp on the entire statement, just put it on the top portion.
                    var lockStatement = (LockStatementSyntax)statement;
                    return CreateSpan(lockStatement, lockStatement.CloseParenToken);

                case SyntaxKind.IfStatement:
                    // Note: if the user was in the body of the 'if' or the 'else', then we would have
                    // hit its nested statement on the way up.  This means we must be on the "if(expr)"
                    // part. Rather than putting a bp on the entire statement, just put it on the top
                    // portion.
                    var ifStatement = (IfStatementSyntax)statement;
                    return CreateSpan(ifStatement, ifStatement.CloseParenToken);

                case SyntaxKind.SwitchStatement:
                    // Note: Any nested statements in the switch will already have been hit on the
                    // way up.  Similarly, hitting a 'case' label will already have been taken care
                    // of.  So in this case, we just set the bp on the "switch(expr)" itself.
                    var switchStatement = (SwitchStatementSyntax)statement;
                    return CreateSpan(switchStatement, switchStatement.CloseParenToken);

                case SyntaxKind.TryStatement:
                    // Note: if the user was in the body of the 'try', then we would have hit its nested
                    // statement on the way up.  This means we must be on the "try" part.  In this case,
                    // just set the BP on the start of the block.  Note: if they were in a catch or
                    // finally section, then that will already have been taken care of above.
                    var tryStatement = (TryStatementSyntax)statement;
                    return TryCreateSpanForStatement(tryStatement.Block, position);

                // All these cases are handled by just putting a breakpoint over the entire
                // statement
                case SyntaxKind.GotoStatement:
                case SyntaxKind.GotoCaseStatement:
                case SyntaxKind.GotoDefaultStatement:
                case SyntaxKind.BreakStatement:
                case SyntaxKind.ContinueStatement:
                case SyntaxKind.ReturnStatement:
                case SyntaxKind.YieldReturnStatement:
                case SyntaxKind.YieldBreakStatement:
                case SyntaxKind.ThrowStatement:
                case SyntaxKind.ExpressionStatement:
                case SyntaxKind.EmptyStatement:
                default:
                    // Fallback case.  If it was none of the above types of statements, then we make a span
                    // over the entire statement.  Note: this is not a very desirable thing to do (as
                    // statements can often span multiple lines.  So, when possible, we should try to do
                    // better.
                    return CreateSpan(statement);
            }
        }

        private static SyntaxToken LastNotMissing(SyntaxToken token1, SyntaxToken token2)
        {
            return token2.IsMissing ? token1 : token2;
        }

        private static TextSpan? TryCreateSpanForVariableDeclaration(VariableDeclarationSyntax declaration, int position)
        {
            switch (declaration.Parent.Kind())
            {
                case SyntaxKind.LocalDeclarationStatement:
                case SyntaxKind.EventFieldDeclaration:
                case SyntaxKind.FieldDeclaration:
                    // parent node will handle:
                    return null;
            }

            return TryCreateSpanForVariableDeclaration(declaration, default(SyntaxTokenList), default(SyntaxToken), position);
        }

        private static TextSpan? TryCreateSpanForVariableDeclaration(
            VariableDeclarationSyntax variableDeclaration,
            SyntaxTokenList modifiersOpt,
            SyntaxToken semicolonOpt,
            int position)
        {
            if (variableDeclaration.Variables.Count == 0)
            {
                return null;
            }

            if (modifiersOpt.Any(SyntaxKind.ConstKeyword))
            {
                // no sequence points are emitted for const fields/locals
                return default(TextSpan);
            }

            if (variableDeclaration.Variables.Count == 1)
            {
                if (variableDeclaration.Variables[0].Initializer == null)
                {
                    return default(TextSpan);
                }

                return CreateSpan(modifiersOpt, variableDeclaration, semicolonOpt);
            }

            if (semicolonOpt != default(SyntaxToken) && position > semicolonOpt.SpanStart)
            {
                position = variableDeclaration.SpanStart;
            }

            var declarator = FindClosestDeclaratorWithInitializer(variableDeclaration.Variables, position);
            if (declarator == null)
            {
                return default(TextSpan);
            }

            if (declarator == variableDeclaration.Variables[0])
            {
                return CreateSpan(modifiersOpt, variableDeclaration.Type, variableDeclaration.Variables[0]);
            }

            return CreateSpan(declarator);
        }

        private static VariableDeclaratorSyntax FindClosestDeclaratorWithInitializer(SeparatedSyntaxList<VariableDeclaratorSyntax> declarators, int position)
        {
            var d = GetItemIndexByPosition(declarators, position);
            var i = 0;
            while (true)
            {
                var left = d - i;
                var right = d + i;
                if (left < 0 && right >= declarators.Count)
                {
                    return null;
                }

                if (left >= 0 && declarators[left].Initializer != null)
                {
                    return declarators[left];
                }

                if (right < declarators.Count && declarators[right].Initializer != null)
                {
                    return declarators[right];
                }

                i += 1;
            }
        }

        private static int GetItemIndexByPosition<TNode>(SeparatedSyntaxList<TNode> list, int position)
            where TNode : SyntaxNode
        {
            for (var i = list.SeparatorCount - 1; i >= 0; i--)
            {
                if (position > list.GetSeparator(i).SpanStart)
                {
                    return i + 1;
                }
            }

            return 0;
        }

        private static SyntaxTokenList GetModifiers(VariableDeclarationSyntax declaration)
        {
            BaseFieldDeclarationSyntax fieldDeclaration;
            LocalDeclarationStatementSyntax localDeclaration;
            if ((fieldDeclaration = declaration.Parent as BaseFieldDeclarationSyntax) != null)
            {
                return fieldDeclaration.Modifiers;
            }

            if ((localDeclaration = declaration.Parent as LocalDeclarationStatementSyntax) != null)
            {
                return localDeclaration.Modifiers;
            }

            return default(SyntaxTokenList);
        }

        private static TextSpan CreateSpanForCatchClause(CatchClauseSyntax catchClause, int position)
        {
            if (catchClause.Filter != null)
            {
                return CreateSpan(catchClause.Filter);
            }
            else if (catchClause.Declaration != null)
            {
                return CreateSpan(catchClause.CatchKeyword, catchClause.Declaration.CloseParenToken);
            }
            else
            {
                return CreateSpan(catchClause.CatchKeyword);
            }
        }

        /// <summary>
        /// There are a few places where we allow breakpoints on expressions. 
        ///
        /// 1) When the expression is the body of a lambda/method/operator/property/indexer.
        /// 2) The expression is a breakable expression inside a query expression.
        /// 3) The expression is in a for statement initializer, condition or incrementor.
        /// 4) The expression is a foreach initializer.
        /// </summary>
        private static bool IsBreakableExpression(ExpressionSyntax expression)
        {
            if (expression == null || expression.Parent == null)
            {
                return false;
            }

            var parent = expression.Parent;
            switch (parent.Kind())
            {
                case SyntaxKind.ArrowExpressionClause:
                    Debug.Assert(((ArrowExpressionClauseSyntax)parent).Expression == expression);
                    return true;

                case SyntaxKind.ForStatement:
                    var forStatement = (ForStatementSyntax)parent;
                    return
                        forStatement.Initializers.Contains(expression) ||
                        forStatement.Condition == expression ||
                        forStatement.Incrementors.Contains(expression);

                case SyntaxKind.ForEachStatement:
                    var forEachStatement = (ForEachStatementSyntax)parent;
                    return forEachStatement.Expression == expression;

                default:
                    return LambdaUtilities.IsLambdaBodyStatementOrExpression(expression);
            }
        }

        private static TextSpan? CreateSpanForAccessors(SyntaxList<AccessorDeclarationSyntax> accessors, int position)
        {
            for (var i = 0; i < accessors.Count; i++)
            {
                if (position <= accessors[i].FullSpan.End || i == accessors.Count - 1)
                {
                    return TryCreateSpanForNode(accessors[i], position);
                }
            }

            return null;
        }
    }
}
