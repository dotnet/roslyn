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
    internal abstract class ExpressionVariableFinder<TFieldOrLocalSymbol> : CSharpSyntaxWalker where TFieldOrLocalSymbol : Symbol
    {
        private ArrayBuilder<TFieldOrLocalSymbol> _localsBuilder;
        private SyntaxNode _nodeToBind;

        protected void FindExpressionVariables(
            ArrayBuilder<TFieldOrLocalSymbol> builder,
            CSharpSyntaxNode node)
        {
            Debug.Assert(node != null);

            ArrayBuilder<TFieldOrLocalSymbol> save = _localsBuilder;
            _localsBuilder = builder;

#if DEBUG
            // These are all of the kinds of nodes we should need to handle in this class.
            // If you add to this list, make sure you handle that node kind with a visitor.
            switch (node.Kind())
            {
                case SyntaxKind.EqualsValueClause:
                case SyntaxKind.ArrowExpressionClause:
                case SyntaxKind.SwitchSection:
                case SyntaxKind.Attribute:
                case SyntaxKind.ThrowStatement:
                case SyntaxKind.ReturnStatement:
                case SyntaxKind.YieldReturnStatement:
                case SyntaxKind.ExpressionStatement:
                case SyntaxKind.WhileStatement:
                case SyntaxKind.DoStatement:
                case SyntaxKind.LockStatement:
                case SyntaxKind.IfStatement:
                case SyntaxKind.SwitchStatement:
                case SyntaxKind.VariableDeclarator:
                    break;
                case SyntaxKind.ArgumentList:
                    Debug.Assert(node.Parent is ConstructorInitializerSyntax);
                    break;
                default:
                    Debug.Assert(node is ExpressionSyntax);
                    break;
            }
#endif

            VisitNodeToBind(node);

            _localsBuilder = save;
        }

        public override void VisitVariableDeclarator(VariableDeclaratorSyntax node)
        {
            if (node.ArgumentList != null)
            {
                foreach (var arg in node.ArgumentList.Arguments)
                {
                    Visit(arg.Expression);
                }
            }

            VisitNodeToBind(node.Initializer);
        }

        private void VisitNodeToBind(CSharpSyntaxNode node)
        {
            var previousNodeToBind = _nodeToBind;
            _nodeToBind = node;
            Visit(node);
            _nodeToBind = previousNodeToBind;
        }

        protected void FindExpressionVariables(
            ArrayBuilder<TFieldOrLocalSymbol> builder,
            SeparatedSyntaxList<ExpressionSyntax> nodes)
        {
            Debug.Assert(nodes.Count > 0);
            ArrayBuilder<TFieldOrLocalSymbol> save = _localsBuilder;
            _localsBuilder = builder;

            foreach (var n in nodes)
            {
                VisitNodeToBind(n);
            }

            _localsBuilder = save;
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
            var variable = MakePatternVariable(node, _nodeToBind);
            if ((object)variable != null)
            {
                _localsBuilder.Add(variable);
            }

            base.VisitDeclarationPattern(node);
        }

        protected abstract TFieldOrLocalSymbol MakePatternVariable(DeclarationPatternSyntax node, SyntaxNode nodeToBind);

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
            var variable = MakeOutVariable(node, argumentListSyntax, _nodeToBind);
            if ((object)variable != null)
            {
                _localsBuilder.Add(variable);
            }
        }

        protected abstract TFieldOrLocalSymbol MakeOutVariable(DeclarationExpressionSyntax node, BaseArgumentListSyntax argumentListSyntax, SyntaxNode nodeToBind);
    }

    internal class ExpressionVariableFinder : ExpressionVariableFinder<LocalSymbol>
    {
        private Binder _scopeBinder;
        private Binder _enclosingBinder;


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

            finder.FindExpressionVariables(builder, node);

            finder._scopeBinder = null;
            finder._enclosingBinder = null;
            s_poolInstance.Free(finder);
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

            finder.FindExpressionVariables(builder, nodes);

            finder._scopeBinder = null;
            finder._enclosingBinder = null;
            s_poolInstance.Free(finder);
        }

        protected override LocalSymbol MakePatternVariable(DeclarationPatternSyntax node, SyntaxNode nodeToBind)
        {
            NamedTypeSymbol container = _scopeBinder.ContainingType;

            if ((object)container != null && container.IsScriptClass &&
                (object)_scopeBinder.LookupDeclaredField(node) != null)
            {
                // This is a field declaration
                return null;
            }

            return SourceLocalSymbol.MakeLocalSymbolWithEnclosingContext(
                            _scopeBinder.ContainingMemberOrLambda,
                            scopeBinder: _scopeBinder,
                            nodeBinder: _enclosingBinder,
                            typeSyntax: node.Type,
                            identifierToken: node.Identifier,
                            kind: LocalDeclarationKind.PatternVariable,
                            nodeToBind: nodeToBind,
                            forbiddenZone: null);
        }
        protected override LocalSymbol MakeOutVariable(DeclarationExpressionSyntax node, BaseArgumentListSyntax argumentListSyntax, SyntaxNode nodeToBind)
        {
            NamedTypeSymbol container = _scopeBinder.ContainingType;

            if ((object)container != null && container.IsScriptClass && 
                (object)_scopeBinder.LookupDeclaredField(node.VariableDesignation()) != null)
            {
                // This is a field declaration
                return null;
            }

            return SourceLocalSymbol.MakeLocalSymbolWithEnclosingContext(
                            containingSymbol: _scopeBinder.ContainingMemberOrLambda,
                            scopeBinder: _scopeBinder,
                            nodeBinder: _enclosingBinder,
                            typeSyntax: node.Type(),
                            identifierToken: node.Identifier(),
                            kind: LocalDeclarationKind.RegularVariable,
                            nodeToBind: nodeToBind,
                            forbiddenZone: argumentListSyntax);
        }

        #region pool
        private static readonly ObjectPool<ExpressionVariableFinder> s_poolInstance = CreatePool();

        public static ObjectPool<ExpressionVariableFinder> CreatePool()
        {
            return new ObjectPool<ExpressionVariableFinder>(() => new ExpressionVariableFinder(), 10);
        }
        #endregion
    }

    internal class ExpressionFieldFinder : ExpressionVariableFinder<Symbol>
    {
        private SourceMemberContainerTypeSymbol _containingType;
        private DeclarationModifiers _modifiers;
        private FieldSymbol _containingFieldOpt;

        internal static void FindExpressionVariables(
            ArrayBuilder<Symbol> builder,
            CSharpSyntaxNode node,
            SourceMemberContainerTypeSymbol containingType,
            DeclarationModifiers modifiers,
            FieldSymbol containingFieldOpt)
        {
            if (node == null)
            {
                return;
            }

            var finder = s_poolInstance.Allocate();
            finder._containingType = containingType;
            finder._modifiers = modifiers;
            finder._containingFieldOpt = containingFieldOpt;

            finder.FindExpressionVariables(builder, node);

            finder._containingType = null;
            finder._modifiers = DeclarationModifiers.None;
            finder._containingFieldOpt = null;
            s_poolInstance.Free(finder);
        }

        protected override Symbol MakePatternVariable(DeclarationPatternSyntax node, SyntaxNode nodeToBind)
        {
            return GlobalExpressionVariable.Create(
                _containingType, _modifiers, node.Type,
                node.Identifier.ValueText, node, node.Identifier.GetLocation(),
                _containingFieldOpt, nodeToBind);
        }

        protected override Symbol MakeOutVariable(DeclarationExpressionSyntax node, BaseArgumentListSyntax argumentListSyntax, SyntaxNode nodeToBind)
        {
            var designation = node.VariableDesignation();
            return GlobalExpressionVariable.Create(
                _containingType, _modifiers, node.Type(),
                designation.Identifier.ValueText, designation, designation.Identifier.GetLocation(),
                _containingFieldOpt, nodeToBind);
        }

        #region pool
        private static readonly ObjectPool<ExpressionFieldFinder> s_poolInstance = CreatePool();

        public static ObjectPool<ExpressionFieldFinder> CreatePool()
        {
            return new ObjectPool<ExpressionFieldFinder>(() => new ExpressionFieldFinder(), 10);
        }
        #endregion
    }
}
