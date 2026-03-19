// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue;

internal static class BreakpointSpans
{
    public static bool TryGetBreakpointSpan(SyntaxTree tree, int position, CancellationToken cancellationToken, out TextSpan breakpointSpan)
    {
        var source = tree.GetText(cancellationToken);

        // If the line is entirely whitespace, then don't set any breakpoint there.
        var line = source.Lines.GetLineFromPosition(position);
        if (IsBlank(line))
        {
            breakpointSpan = default;
            return false;
        }

        // If the user is asking for breakpoint in an inactive region, then just create a line
        // breakpoint there.
        if (tree.IsInInactiveRegion(position, cancellationToken))
        {
            breakpointSpan = default;
            return true;
        }

        var root = tree.GetRoot(cancellationToken);
        return TryGetClosestBreakpointSpan(root, position, minLength: 0, out breakpointSpan);
    }

    private static bool IsBlank(TextLine line)
    {
        var text = line.ToString();

        for (var i = 0; i < text.Length; i++)
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
    /// <param name="minLength">
    /// In case there are multiple breakpoint spans starting at the given <paramref name="position"/>,
    /// <paramref name="minLength"/> can be used to disambiguate between them. 
    /// The inner-most available span whose length is at least <paramref name="minLength"/> is returned.
    /// </param>
    /// <remarks>
    /// If the span exists it is possible to place a breakpoint at the given position.
    /// </remarks>
    public static bool TryGetClosestBreakpointSpan(SyntaxNode root, int position, int minLength, out TextSpan span)
    {
        var node = root.FindToken(position).Parent;
        var candidate = (TextSpan?)null;

        while (node != null)
        {
            var breakpointSpan = TryCreateSpanForNode(node, position);
            if (breakpointSpan.HasValue)
            {
                span = breakpointSpan.Value;
                if (span == default)
                {
                    break;
                }

                // the new breakpoint span doesn't alight with the previously found breakpoint span, return the previous one:
                if (candidate.HasValue && breakpointSpan.Value.Start != candidate.Value.Start)
                {
                    span = candidate.Value;
                    return true;
                }

                // The span length meets the requirement:
                if (breakpointSpan.Value.Length >= minLength)
                {
                    span = breakpointSpan.Value;
                    return true;
                }

                candidate = breakpointSpan;
            }

            node = node.Parent;
        }

        span = candidate.GetValueOrDefault();
        return candidate.HasValue;
    }

    private static TextSpan CreateSpan(SyntaxToken startToken, SyntaxToken endToken)
        => TextSpan.FromBounds(startToken.SpanStart, endToken.Span.End);

    private static TextSpan CreateSpan(SyntaxNode node)
        => CreateSpan(node.GetFirstToken(), node.GetLastToken());

    private static TextSpan CreateSpan(SyntaxNode node, SyntaxToken token)
        => TextSpan.FromBounds(node.SpanStart, token.Span.End);

    private static TextSpan CreateSpan(SyntaxToken token)
        => TextSpan.FromBounds(token.SpanStart, token.Span.End);

    private static TextSpan CreateSpan(SyntaxTokenList startOpt, SyntaxNodeOrToken startFallbackOpt, SyntaxNodeOrToken endOpt)
    {
        Debug.Assert(startFallbackOpt != default || endOpt != default);

        int startPos;
        if (startOpt.Count > 0)
        {
            startPos = startOpt.First().SpanStart;
        }
        else if (startFallbackOpt != default)
        {
            startPos = startFallbackOpt.SpanStart;
        }
        else
        {
            startPos = endOpt.SpanStart;
        }

        int endPos;
        if (endOpt != default)
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
            return nodeOrToken.AsNode()!.GetLastToken().Span.End;
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
            case SyntaxKind.OperatorDeclaration:
            case SyntaxKind.ConversionOperatorDeclaration:
            case SyntaxKind.DestructorDeclaration:
                var methodDeclaration = (BaseMethodDeclarationSyntax)node;
                return (methodDeclaration.Body != null) ? CreateSpanForBlock(methodDeclaration.Body, position) : methodDeclaration.ExpressionBody?.Expression.Span;

            case SyntaxKind.ConstructorDeclaration:
                return CreateSpanForConstructorDeclaration((ConstructorDeclarationSyntax)node, position);

            case SyntaxKind.RecordDeclaration:
            case SyntaxKind.RecordStructDeclaration:
            case SyntaxKind.StructDeclaration:
            case SyntaxKind.ClassDeclaration:
                var typeDeclaration = (TypeDeclarationSyntax)node;
                if (typeDeclaration.ParameterList != null)
                {
                    // after brace or semicolon
                    // class C<T>(...) {$$ ... }
                    // class C<T>(...) ;$$
                    if (position > LastNotMissing(typeDeclaration.SemicolonToken, typeDeclaration.OpenBraceToken).SpanStart)
                    {
                        return null;
                    }

                    // on or after explicit base initializer:
                    //   C<T>(...) :$$ [|B(...)|], I
                    //   C<T>(...) : [|B(...)|], I where ... $$
                    var baseInitializer = (PrimaryConstructorBaseTypeSyntax?)typeDeclaration.BaseList?.Types.FirstOrDefault(t => t.IsKind(SyntaxKind.PrimaryConstructorBaseType));
                    if (baseInitializer != null && position > typeDeclaration.BaseList!.ColonToken.SpanStart)
                    {
                        return CreateSpanForExplicitPrimaryConstructorInitializer(baseInitializer);
                    }

                    // record properties and copy constructor
                    if (position >= typeDeclaration.Identifier.SpanStart && node is RecordDeclarationSyntax recordDeclaration)
                    {
                        // on identifier:
                        // record $$C<T>(...) : B(...);
                        // record C<T>$$(...) : B(...);
                        if (position <= typeDeclaration.ParameterList.SpanStart)
                        {
                            // copy-constructor: [|C<T>|]
                            return CreateSpanForCopyConstructor(recordDeclaration);
                        }

                        // on parameter:
                        // record C<T>(..., $$ int p, ...) : B(...);
                        if (position < typeDeclaration.ParameterList.CloseParenToken.Span.End)
                        {
                            var parameter = GetParameter(position, typeDeclaration.ParameterList.Parameters);
                            if (parameter != null)
                            {
                                // [A][|int p|] = default
                                return CreateSpanForRecordParameter(parameter);
                            }

                            static ParameterSyntax? GetParameter(int position, SeparatedSyntaxList<ParameterSyntax> parameters)
                            {
                                if (parameters.Count == 0)
                                {
                                    return null;
                                }

                                for (var i = 0; i < parameters.SeparatorCount; i++)
                                {
                                    var separator = parameters.GetSeparator(i);
                                    if (position <= separator.SpanStart)
                                    {
                                        return parameters[i];
                                    }
                                }

                                return parameters.Last();
                            }
                        }
                    }

                    // explicit base initializer
                    //   C<T>(...) : [|B(...)|]
                    // implicit base initializer
                    //   [|C<T>(...)|]
                    return (baseInitializer != null)
                        ? CreateSpanForExplicitPrimaryConstructorInitializer(baseInitializer)
                        : CreateSpanForImplicitPrimaryConstructorInitializer(typeDeclaration);
                }

                return null;

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
                return CreateSpanForCatchClause((CatchClauseSyntax)node);

            case SyntaxKind.FinallyClause:
                return TryCreateSpanForNode(((FinallyClauseSyntax)node).Block, position);

            case SyntaxKind.CaseSwitchLabel:
            case SyntaxKind.DefaultSwitchLabel:
                return TryCreateSpanForSwitchLabel((SwitchLabelSyntax)node, position);

            case SyntaxKind.CasePatternSwitchLabel:
                var caseClause = (CasePatternSwitchLabelSyntax)node;
                return caseClause.WhenClause == null
                    ? TryCreateSpanForSwitchLabel((SwitchLabelSyntax)node, position)
                    : CreateSpan(caseClause.WhenClause);

            case SyntaxKind.SwitchExpressionArm:
                var switchArm = (SwitchExpressionArmSyntax)node;
                return createSpanForSwitchArm(switchArm);

                TextSpan createSpanForSwitchArm(SwitchExpressionArmSyntax switchArm)
                    => CreateSpan((position <= switchArm.WhenClause?.FullSpan.End == true) ? switchArm.WhenClause : switchArm.Expression);

            case SyntaxKind.SwitchExpression when
                        node is SwitchExpressionSyntax switchExpression &&
                        switchExpression.Arms.Count > 0 &&
                        position >= switchExpression.OpenBraceToken.Span.End &&
                        position <= switchExpression.CloseBraceToken.Span.Start:
                // This can occur if the cursor is on a separator. Find the nearest switch arm.
                switchArm = switchExpression.Arms.LastOrDefault(arm => position >= arm.FullSpan.Start) ?? switchExpression.Arms.First();
                return createSpanForSwitchArm(switchArm);

            case SyntaxKind.WhenClause:
                return CreateSpan(node);

            case SyntaxKind.GetAccessorDeclaration:
            case SyntaxKind.SetAccessorDeclaration:
            case SyntaxKind.InitAccessorDeclaration:
            case SyntaxKind.AddAccessorDeclaration:
            case SyntaxKind.RemoveAccessorDeclaration:
            case SyntaxKind.UnknownAccessorDeclaration:
                var accessor = (AccessorDeclarationSyntax)node;
                if (accessor.ExpressionBody != null)
                {
                    return CreateSpan(accessor.ExpressionBody.Expression);
                }
                else if (accessor.Body != null)
                {
                    return TryCreateSpanForNode(accessor.Body, position);
                }
                else
                {
                    return CreateSpanForAutoPropertyAccessor(accessor);
                }

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

                // properties without expression body have accessor list:
                Contract.ThrowIfNull(property.AccessorList);

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

                // indexers without expression body have accessor list:
                Contract.ThrowIfNull(indexer.AccessorList);

                // int this[args] { get [|{|] ... } set { ... } }
                return CreateSpanForAccessors(indexer.AccessorList.Accessors, position);

            case SyntaxKind.EventDeclaration:
                // event Action P { add [|{|] ... } remove { ... } }
                // event Action P { [|add;|] [|remove;|] }
                var @event = (EventDeclarationSyntax)node;
                return @event.AccessorList != null ? CreateSpanForAccessors(@event.AccessorList.Accessors, position) : null;

            case SyntaxKind.BaseConstructorInitializer:
            case SyntaxKind.ThisConstructorInitializer:
                return CreateSpanForExplicitConstructorInitializer((ConstructorInitializerSyntax)node);

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

            case SyntaxKind.LocalFunctionStatement:
                var localFunction = (LocalFunctionStatementSyntax)node;
                return (localFunction.Body != null)
                    ? TryCreateSpanForNode(localFunction.Body, position)
                    : TryCreateSpanForNode(localFunction.ExpressionBody!.Expression, position);

            default:
                if (node is ExpressionSyntax expression)
                {
                    return IsBreakableExpression(expression) ? CreateSpan(expression) : null;
                }

                if (node is StatementSyntax statement)
                {
                    return TryCreateSpanForStatement(statement, position);
                }

                return null;
        }
    }

    internal static TextSpan? CreateSpanForConstructorDeclaration(ConstructorDeclarationSyntax constructorSyntax, int position)
    {
        if (constructorSyntax.ExpressionBody != null &&
            position > constructorSyntax.ExpressionBody.ArrowToken.Span.Start)
        {
            return constructorSyntax.ExpressionBody.Expression.Span;
        }

        if (constructorSyntax.Initializer != null)
        {
            return CreateSpanForExplicitConstructorInitializer(constructorSyntax.Initializer);
        }

        // static ctor doesn't have a default initializer:
        if (constructorSyntax.Modifiers.Any(SyntaxKind.StaticKeyword))
        {
            if (constructorSyntax.ExpressionBody != null)
            {
                return constructorSyntax.ExpressionBody.Expression.Span;
            }

            if (constructorSyntax.Body != null)
            {
                return CreateSpan(constructorSyntax.Body.OpenBraceToken);
            }

            return null;
        }

        return CreateSpanForImplicitConstructorInitializer(constructorSyntax);
    }

    internal static TextSpan CreateSpanForImplicitConstructorInitializer(ConstructorDeclarationSyntax constructor)
        => CreateSpan(constructor.Modifiers, constructor.Identifier, constructor.ParameterList.CloseParenToken);

    internal static IEnumerable<SyntaxToken> GetActiveTokensForImplicitConstructorInitializer(ConstructorDeclarationSyntax constructor, Func<SyntaxNode, IEnumerable<SyntaxToken>> getDescendantTokens)
        => constructor.Modifiers.Concat([constructor.Identifier]).Concat(getDescendantTokens(constructor.ParameterList));

    internal static TextSpan CreateSpanForExplicitConstructorInitializer(ConstructorInitializerSyntax constructorInitializer)
        => CreateSpan(constructorInitializer.ThisOrBaseKeyword, constructorInitializer.ArgumentList.CloseParenToken);

    internal static IEnumerable<SyntaxToken> GetActiveTokensForExplicitConstructorInitializer(ConstructorInitializerSyntax constructorInitializer, Func<SyntaxNode, IEnumerable<SyntaxToken>> getDescendantTokens)
        => [constructorInitializer.ThisOrBaseKeyword, .. getDescendantTokens(constructorInitializer.ArgumentList)];

    internal static TextSpan CreateSpanForImplicitPrimaryConstructorInitializer(TypeDeclarationSyntax typeDeclaration)
    {
        Debug.Assert(typeDeclaration.ParameterList != null);
        return TextSpan.FromBounds(typeDeclaration.Identifier.SpanStart, typeDeclaration.ParameterList.Span.End);
    }

    internal static IEnumerable<SyntaxToken> GetActiveTokensForImplicitPrimaryConstructorInitializer(TypeDeclarationSyntax typeDeclaration, Func<SyntaxNode, IEnumerable<SyntaxToken>> getDescendantTokens)
    {
        Debug.Assert(typeDeclaration.ParameterList != null);

        yield return typeDeclaration.Identifier;

        if (typeDeclaration.TypeParameterList != null)
        {
            foreach (var token in getDescendantTokens(typeDeclaration.TypeParameterList))
                yield return token;
        }

        foreach (var token in getDescendantTokens(typeDeclaration.ParameterList))
            yield return token;
    }

    internal static TextSpan CreateSpanForExplicitPrimaryConstructorInitializer(PrimaryConstructorBaseTypeSyntax baseTypeSyntax)
        => baseTypeSyntax.Span;

    internal static IEnumerable<SyntaxToken> GetActiveTokensForExplicitPrimaryConstructorInitializer(PrimaryConstructorBaseTypeSyntax baseTypeSyntax, Func<SyntaxNode, IEnumerable<SyntaxToken>> getDescendantTokens)
        => getDescendantTokens(baseTypeSyntax);

    internal static TextSpan CreateSpanForCopyConstructor(RecordDeclarationSyntax recordDeclaration)
        => CreateSpan(
            recordDeclaration.Identifier,
            LastNotMissing(recordDeclaration.Identifier, recordDeclaration.TypeParameterList?.GreaterThanToken ?? default));

    internal static IEnumerable<SyntaxToken> GetActiveTokensForCopyConstructor(RecordDeclarationSyntax recordDeclaration, Func<SyntaxNode, IEnumerable<SyntaxToken>> getDescendantTokens)
    {
        yield return recordDeclaration.Identifier;

        if (recordDeclaration.TypeParameterList != null)
        {
            foreach (var token in getDescendantTokens(recordDeclaration.TypeParameterList))
                yield return token;
        }
    }

    internal static TextSpan CreateSpanForRecordParameter(ParameterSyntax parameter)
        => CreateSpan(parameter.Modifiers, parameter.Type, parameter.Identifier);

    internal static IEnumerable<SyntaxToken> GetActiveTokensForRecordParameter(ParameterSyntax parameter, Func<SyntaxNode, IEnumerable<SyntaxToken>> getDescendantTokens)
    {
        foreach (var modifier in parameter.Modifiers)
            yield return modifier;

        if (parameter.Type != null)
        {
            foreach (var token in getDescendantTokens(parameter.Type))
                yield return token;
        }

        yield return parameter.Identifier;
    }

    internal static TextSpan CreateSpanForAutoPropertyAccessor(AccessorDeclarationSyntax accessor)
        => accessor.Span;

    internal static IEnumerable<SyntaxToken> GetActiveTokensForAutoPropertyAccessor(AccessorDeclarationSyntax accessor, Func<SyntaxNode, IEnumerable<SyntaxToken>> getDescendantTokens)
        => getDescendantTokens(accessor);

    private static TextSpan? TryCreateSpanForFieldDeclaration(BaseFieldDeclarationSyntax fieldDeclaration, int position)
        => TryCreateSpanForVariableDeclaration(fieldDeclaration.Declaration, fieldDeclaration.Modifiers, fieldDeclaration.SemicolonToken, position);

    private static TextSpan? TryCreateSpanForSwitchLabel(SwitchLabelSyntax switchLabel, int position)
    {
        if (switchLabel.Parent is not SwitchSectionSyntax switchSection || switchSection.Statements.Count == 0)
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
                    declarationStatement.SemicolonToken, position, startNodeOpt: declarationStatement);

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
                    return CreateSpan(default, forStatement.Declaration.Type, firstVariable);
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
            case SyntaxKind.ForEachVariableStatement:
                // Note: if the user was in the body of the foreach, then we would have hit its
                // nested statement on the way up.  If they were in the expression then we would
                // have hit that on the way up as well. In "foreach(var f in expr)" we allow a
                // bp on "foreach", "var f" and "in".
                var forEachStatement = (CommonForEachStatementSyntax)statement;
                if (position < forEachStatement.OpenParenToken.Span.End || position > forEachStatement.CloseParenToken.SpanStart)
                {
                    return CreateSpan(forEachStatement.ForEachKeyword);
                }
                else if (position < forEachStatement.InKeyword.FullSpan.Start)
                {
                    if (forEachStatement.Kind() == SyntaxKind.ForEachStatement)
                    {
                        var simpleForEachStatement = (ForEachStatementSyntax)statement;
                        return CreateSpan(simpleForEachStatement.Type, simpleForEachStatement.Identifier);
                    }
                    else
                    {
                        return ((ForEachVariableStatementSyntax)statement).Variable.Span;
                    }
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
                return CreateSpan(switchStatement, (switchStatement.CloseParenToken != default) ? switchStatement.CloseParenToken : switchStatement.Expression.GetLastToken());

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
        => token2.IsKind(SyntaxKind.None) || token2.IsMissing ? token1 : token2;

    private static TextSpan? TryCreateSpanForVariableDeclaration(VariableDeclarationSyntax declaration, int position)
        => declaration.Parent!.Kind() switch
        {
            // parent node will handle:
            SyntaxKind.LocalDeclarationStatement or SyntaxKind.EventFieldDeclaration or SyntaxKind.FieldDeclaration => null,

            _ => TryCreateSpanForVariableDeclaration(declaration, modifiersOpt: default, semicolonOpt: default, position, startNodeOpt: null),
        };

    private static TextSpan? TryCreateSpanForVariableDeclaration(
        VariableDeclarationSyntax variableDeclaration,
        SyntaxTokenList modifiersOpt,
        SyntaxToken semicolonOpt,
        int position,
        SyntaxNode? startNodeOpt = null)
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

            // If we have a start node (e.g., LocalDeclarationStatementSyntax with 'using' or 'await'),
            // use it as the start to include those keywords in the span
            if (startNodeOpt != null)
            {
                return CreateSpan(
                    startOpt: default,
                    startFallbackOpt: startNodeOpt,
                    endOpt: semicolonOpt != default ? semicolonOpt : (SyntaxNodeOrToken)variableDeclaration);
            }

            return CreateSpan(modifiersOpt, variableDeclaration, semicolonOpt);
        }

        if (semicolonOpt != default && position > semicolonOpt.SpanStart)
        {
            position = variableDeclaration.SpanStart;
        }

        var variableDeclarator = FindClosestDeclaratorWithInitializer(variableDeclaration.Variables, position);
        if (variableDeclarator == null)
        {
            return default(TextSpan);
        }

        if (variableDeclarator == variableDeclaration.Variables[0])
        {
            // If we have a start node (e.g., LocalDeclarationStatementSyntax with 'using' or 'await'),
            // use it as the start to include those keywords in the span
            if (startNodeOpt != null)
            {
                return CreateSpan(
                    startOpt: default,
                    startFallbackOpt: startNodeOpt,
                    endOpt: variableDeclarator);
            }

            return CreateSpan(modifiersOpt, variableDeclaration, variableDeclarator);
        }

        return CreateSpan(variableDeclarator);
    }

    internal static TextSpan CreateSpanForVariableDeclarator(
        VariableDeclaratorSyntax variableDeclarator,
        SyntaxTokenList modifiers,
        SyntaxToken semicolon)
    {
        if (variableDeclarator.Initializer == null || modifiers.Any(SyntaxKind.ConstKeyword))
        {
            return default;
        }

        var variableDeclaration = (VariableDeclarationSyntax)variableDeclarator.Parent!;
        if (variableDeclaration.Variables.Count == 1)
        {
            return CreateSpan(modifiers, variableDeclaration, semicolon);
        }

        if (variableDeclarator == variableDeclaration.Variables[0])
        {
            return CreateSpan(modifiers, variableDeclaration, variableDeclarator);
        }

        return CreateSpan(variableDeclarator);
    }

    internal static IEnumerable<SyntaxToken> GetActiveTokensForVariableDeclarator(
        VariableDeclaratorSyntax variableDeclarator, SyntaxTokenList modifiers, SyntaxToken semicolon, Func<SyntaxNode, IEnumerable<SyntaxToken>> getDescendantTokens)
    {
        if (variableDeclarator.Initializer == null || modifiers.Any(SyntaxKind.ConstKeyword))
            return [];

        // [|int F = 1;|]
        var variableDeclaration = (VariableDeclarationSyntax)variableDeclarator.Parent!;
        if (variableDeclaration.Variables.Count == 1)
        {
            return modifiers.Concat(getDescendantTokens(variableDeclaration)).Concat(semicolon);
        }

        // [|int F = 1|], G = 2;
        if (variableDeclarator == variableDeclaration.Variables[0])
        {
            return modifiers.Concat(getDescendantTokens(variableDeclaration.Type)).Concat(getDescendantTokens(variableDeclarator));
        }

        // int F = 1, [|G = 2|];
        return getDescendantTokens(variableDeclarator);
    }

    private static VariableDeclaratorSyntax? FindClosestDeclaratorWithInitializer(SeparatedSyntaxList<VariableDeclaratorSyntax> declarators, int position)
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

    private static TextSpan CreateSpanForCatchClause(CatchClauseSyntax catchClause)
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
    /// 5) The expression is the value of an arm of a switch expression
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

            case SyntaxKind.SwitchExpressionArm:
                Debug.Assert(((SwitchExpressionArmSyntax)parent).Expression == expression);
                return true;

            case SyntaxKind.ForStatement:
                var forStatement = (ForStatementSyntax)parent;
                return
                    forStatement.Initializers.Contains(expression) ||
                    forStatement.Condition == expression ||
                    forStatement.Incrementors.Contains(expression);

            case SyntaxKind.ForEachStatement:
            case SyntaxKind.ForEachVariableStatement:
                var forEachStatement = (CommonForEachStatementSyntax)parent;
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
