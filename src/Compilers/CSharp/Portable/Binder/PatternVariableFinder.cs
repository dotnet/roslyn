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
    internal class PatternVariableFinder : CSharpSyntaxWalker
    {
        private Binder _binder;
        private ArrayBuilder<LocalSymbol> _declarationPatterns;

        internal static void FindPatternVariables(
            Binder binder,
            ArrayBuilder<LocalSymbol> builder,
            CSharpSyntaxNode node)
        {
            if (node == null)
            {
                return;
            }

            var finder = s_poolInstance.Allocate();
            finder._binder = binder;
            finder._declarationPatterns = builder;

            finder.Visit(node);

            finder._binder = null;
            finder._declarationPatterns = null;
            s_poolInstance.Free(finder);
        }

        internal static void FindPatternVariables(
            Binder binder,
            ArrayBuilder<LocalSymbol> builder,
            SeparatedSyntaxList<ExpressionSyntax> nodes)
        {
            if (nodes.Count == 0)
            {
                return;
            }

            var finder = s_poolInstance.Allocate();
            finder._binder = binder;
            finder._declarationPatterns = builder;

            foreach (var n in nodes)
            {
                finder.Visit(n);
            }

            finder._binder = null;
            finder._declarationPatterns = null;
            s_poolInstance.Free(finder);
        }

        public override void VisitLetStatement(LetStatementSyntax node)
        {
            Visit(node.Expression);
            Visit(node.WhenClause?.Condition);
        }

        public override void VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
        {
            foreach (var decl in node.Declaration.Variables)
            {
                Visit(decl.Initializer?.Value);
            }
        }

        public override void VisitSwitchSection(SwitchSectionSyntax node)
        {
            foreach (var label in node.Labels)
            {
                var match = label as CasePatternSwitchLabelSyntax;
                if (match != null)
                {
                    Visit(match.Pattern);
                    if (match.WhenClause != null)
                    {
                        Visit(match.WhenClause.Condition);
                    }
                }
            }
        }

        public override void VisitWhileStatement(WhileStatementSyntax node)
        {
            Visit(node.Condition);
        }

        public override void VisitDoStatement(DoStatementSyntax node)
        {
            Visit(node.Condition);
        }

        public override void VisitLockStatement(LockStatementSyntax node)
        {
            Visit(node.Expression);
        }

        public override void VisitDeclarationPattern(DeclarationPatternSyntax node)
        {
            _declarationPatterns.Add(SourceLocalSymbol.MakeLocal(_binder.ContainingMemberOrLambda, _binder, RefKind.None, node.Type, node.Identifier, LocalDeclarationKind.PatternVariable));
            base.VisitDeclarationPattern(node);
        }
        public override void VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node) { }
        public override void VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node) { }
        public override void VisitAnonymousMethodExpression(AnonymousMethodExpressionSyntax node) { }

        public override void VisitQueryExpression(QueryExpressionSyntax node)
        {
            // Variables declared in [in] expressions of top level from clause and 
            // join clauses are in scope 
            Visit(node.FromClause.Expression);
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
                    Visit(((JoinClauseSyntax)clause).InExpression);
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

        public override void VisitMatchExpression(MatchExpressionSyntax node)
        {
            Visit(node.Left);
        }

        #region pool
        private static readonly ObjectPool<PatternVariableFinder> s_poolInstance = CreatePool();

        public static ObjectPool<PatternVariableFinder> CreatePool()
        {
            return new ObjectPool<PatternVariableFinder>(() => new PatternVariableFinder(), 10);
        }
        #endregion
    }
}
