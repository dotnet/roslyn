// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal class ExpressionVariableFinder : CSharpSyntaxWalker
    {
        private Binder _scopeBinder;
        private Binder _enclosingBinder;
        private ArrayBuilder<LocalSymbol> _localsBuilder;
        private SyntaxNode _nodeToBind;

        internal static void FindExpressionVariables(
            Binder scopeBinder,
            ArrayBuilder<LocalSymbol> builder,
            CSharpSyntaxNode node,
            Binder enclosingBinderOpt = null)
        {
            if (node == null)
            {
                return;
            }

            var finder = s_poolInstance.Allocate();
            finder._scopeBinder = scopeBinder;
            finder._enclosingBinder = enclosingBinderOpt ?? scopeBinder;
            finder._localsBuilder = builder;

            finder.VisitNodeToBind(node);

            finder._scopeBinder = null;
            finder._enclosingBinder = null;
            finder._localsBuilder = null;
            s_poolInstance.Free(finder);
        }

        private void VisitNodeToBind(CSharpSyntaxNode node)
        {
            var previousNodeToBind = _nodeToBind;
            _nodeToBind = node;
            Visit(node);
            _nodeToBind = previousNodeToBind;
        }

        internal static void FindExpressionVariables(
            Binder binder,
            ArrayBuilder<LocalSymbol> builder,
            SeparatedSyntaxList<ExpressionSyntax> nodes)
        {
            if (nodes.Count == 0)
            {
                return;
            }

            var finder = s_poolInstance.Allocate();
            finder._scopeBinder = binder;
            finder._enclosingBinder = binder;
            finder._localsBuilder = builder;

            foreach (var n in nodes)
            {
                finder._nodeToBind = n;
                finder.VisitNodeToBind(n);
            }

            finder._nodeToBind = null;
            finder._scopeBinder = null;
            finder._enclosingBinder = null;
            finder._localsBuilder = null;
            s_poolInstance.Free(finder);
        }

        public override void VisitEqualsValueClause(EqualsValueClauseSyntax node)
        {
            VisitNodeToBind(node.Value);
        }

        public override void VisitArrowExpressionClause(ArrowExpressionClauseSyntax node)
        {
            VisitNodeToBind(node.Expression);
        }

        public override void VisitSwitchSection(SwitchSectionSyntax node)
        {
            foreach (var label in node.Labels)
            {
                var match = label as CasePatternSwitchLabelSyntax;
                if (match != null)
                {
                    var previousNodeToBind = _nodeToBind;
                    _nodeToBind = match;
                    Visit(match.Pattern);
                    if (match.WhenClause != null)
                    {
                        VisitNodeToBind(match.WhenClause.Condition);
                    }

                    _nodeToBind = previousNodeToBind;
                }
            }
        }

        public override void VisitAttribute(AttributeSyntax node)
        {
            if (node.ArgumentList != null)
            {
            foreach (var argument in node.ArgumentList.Arguments)
            {
                VisitNodeToBind(argument.Expression);
            }
            }
        }

        public override void VisitThrowStatement(ThrowStatementSyntax node)
        {
            VisitNodeToBind(node.Expression);
        }

        public override void VisitReturnStatement(ReturnStatementSyntax node)
        {
            VisitNodeToBind(node.Expression);
        }

        public override void VisitYieldStatement(YieldStatementSyntax node)
        {
            VisitNodeToBind(node.Expression);
        }

        public override void VisitExpressionStatement(ExpressionStatementSyntax node)
        {
            VisitNodeToBind(node.Expression);
        }

        public override void VisitWhileStatement(WhileStatementSyntax node)
        {
            VisitNodeToBind(node.Condition);
        }

        public override void VisitDoStatement(DoStatementSyntax node)
        {
            VisitNodeToBind(node.Condition);
        }

        public override void VisitLockStatement(LockStatementSyntax node)
        {
            VisitNodeToBind(node.Expression);
        }

        public override void VisitIfStatement(IfStatementSyntax node)
        {
            VisitNodeToBind(node.Condition);
        }

        public override void VisitSwitchStatement(SwitchStatementSyntax node)
        {
            VisitNodeToBind(node.Expression);
        }

        public override void VisitDeclarationPattern(DeclarationPatternSyntax node)
        {
            _localsBuilder.Add(SourceLocalSymbol.MakeLocalSymbolWithEnclosingContext(
                _scopeBinder.ContainingMemberOrLambda,
                scopeBinder: _scopeBinder,
                nodeBinder: _enclosingBinder,
                typeSyntax: node.Type,
                identifierToken: node.Identifier,
                kind: LocalDeclarationKind.PatternVariable,
                nodeToBind: _nodeToBind,
                forbiddenZone: null));
            base.VisitDeclarationPattern(node);
        }

        public override void VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node) { }
        public override void VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node) { }
        public override void VisitAnonymousMethodExpression(AnonymousMethodExpressionSyntax node) { }

        public override void VisitQueryExpression(QueryExpressionSyntax node)
        {
            // Variables declared in [in] expressions of top level from clause and 
            // join clauses are in scope 
            VisitNodeToBind(node.FromClause.Expression);
            Visit(node.Body);
        }

        public override void VisitQueryBody(QueryBodySyntax node)
        {
            // Variables declared in [in] expressions of top level from clause and 
            // join clauses are in scope 
            foreach (var clause in node.Clauses)
            {
                if (clause.Kind() == SyntaxKind.JoinClause)
                {
                    VisitNodeToBind(((JoinClauseSyntax)clause).InExpression);
                }
            }

            Visit(node.Continuation);
        }

        public override void VisitBinaryExpression(BinaryExpressionSyntax node)
        {
            // The binary operators (except ??) are left-associative, and expressions of the form
            // a + b + c + d .... are relatively common in machine-generated code. The parser can handle
            // creating a deep-on-the-left syntax tree no problem, and then we promptly blow the stack during
            // semantic analysis. Here we build an explicit stack to handle left recursion.

            var operands = ArrayBuilder<ExpressionSyntax>.GetInstance();
            ExpressionSyntax current = node;
            do
            {
                var binOp = (BinaryExpressionSyntax)current;
                operands.Push(binOp.Right);
                current = binOp.Left;
            }
            while (current is BinaryExpressionSyntax);

            Visit(current);
            while (operands.Count > 0)
            {
                Visit(operands.Pop());
            }

            operands.Free();
        }

        public override void VisitDeclarationExpression(DeclarationExpressionSyntax node)
        {
            var argumentSyntax = (ArgumentSyntax)node?.Parent;
            var argumentListSyntax = (BaseArgumentListSyntax)argumentSyntax?.Parent;
            var invocationSyntax = argumentListSyntax?.Parent;
            _localsBuilder.Add(SourceLocalSymbol.MakeLocalSymbolWithEnclosingContext(
                containingSymbol: _scopeBinder.ContainingMemberOrLambda,
                scopeBinder: _scopeBinder,
                nodeBinder: _enclosingBinder,
                typeSyntax: node.Type(),
                identifierToken: node.Identifier(),
                kind: LocalDeclarationKind.RegularVariable,
                nodeToBind: _nodeToBind,
                forbiddenZone: invocationSyntax));
        }

        #region pool
        private static readonly ObjectPool<ExpressionVariableFinder> s_poolInstance = CreatePool();

        public static ObjectPool<ExpressionVariableFinder> CreatePool()
        {
            return new ObjectPool<ExpressionVariableFinder>(() => new ExpressionVariableFinder(), 10);
        }
        #endregion
    }
}
