﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;
using System;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// The LocalBinderFactory is used to build up the map of all Binders within a method body, and the associated
    /// CSharpSyntaxNode. To do so it traverses all the statements, handling blocks and other
    /// statements that create scopes. For efficiency reasons, it does not traverse into
    /// expressions. This means that blocks within lambdas and queries are not created. 
    /// Blocks within lambdas are bound by their own LocalBinderFactory when they are 
    /// analyzed.
    ///
    /// For reasons of lifetime management, this type is distinct from the BinderFactory 
    /// which also creates a map from CSharpSyntaxNode to Binder. That type owns it's binders
    /// and that type's lifetime is that of the compilation. Therefore we do not store
    /// binders local to method bodies in that type's cache. 
    /// </summary>
    internal sealed class LocalBinderFactory : CSharpSyntaxWalker
    {
        private readonly SmallDictionary<SyntaxNode, Binder> _map;
        private bool _sawYield;
        private readonly ArrayBuilder<SyntaxNode> _methodsWithYields;
        private Symbol _containingMemberOrLambda;
        private Binder _enclosing;
        private readonly SyntaxNode _root;

        private void Visit(CSharpSyntaxNode syntax, Binder enclosing)
        {
            if (_enclosing == enclosing)
            {
                this.Visit(syntax);
            }
            else
            {
                Binder oldEnclosing = _enclosing;
                _enclosing = enclosing;
                this.Visit(syntax);
                _enclosing = oldEnclosing;
            }
        }

        // methodsWithYields will contain all function-declaration-like CSharpSyntaxNodes with yield statements contained within them.
        // Currently the types of these are restricted to only be whatever the syntax parameter is, plus any LocalFunctionStatementSyntax contained within it.
        // This may change if the language is extended to allow iterator lambdas, in which case the lambda would also be returned.
        // (lambdas currently throw a diagnostic in WithLambdaParametersBinder.GetIteratorElementType when a yield is used within them)
        public static SmallDictionary<SyntaxNode, Binder> BuildMap(
            Symbol containingMemberOrLambda, 
            SyntaxNode syntax, 
            Binder enclosing, 
            ArrayBuilder<SyntaxNode> methodsWithYields,
            Func<Binder, SyntaxNode, Binder> rootBinderAdjusterOpt = null)
        {
            var builder = new LocalBinderFactory(containingMemberOrLambda, syntax, enclosing, methodsWithYields);

            StatementSyntax statement;
            var expressionSyntax = syntax as ExpressionSyntax;
            if (expressionSyntax != null)
            {
                enclosing = new ExpressionVariableBinder(syntax, enclosing);

                if ((object)rootBinderAdjusterOpt != null)
                {
                    enclosing = rootBinderAdjusterOpt(enclosing, syntax);
                }

                builder.AddToMap(syntax, enclosing);
                builder.Visit(expressionSyntax, enclosing);
            }
            else if (syntax.Kind() != SyntaxKind.Block && (statement = syntax as StatementSyntax) != null)
            {
                CSharpSyntaxNode embeddedScopeDesignator;
                enclosing = builder.GetBinderForPossibleEmbeddedStatement(statement, enclosing, out embeddedScopeDesignator);

                if ((object)rootBinderAdjusterOpt != null)
                {
                    enclosing = rootBinderAdjusterOpt(enclosing, embeddedScopeDesignator);
                }

                if (embeddedScopeDesignator != null)
                {
                    builder.AddToMap(embeddedScopeDesignator, enclosing);
                }

                builder.Visit(statement, enclosing);
            }
            else
            {
                if ((object)rootBinderAdjusterOpt != null)
                {
                    enclosing = rootBinderAdjusterOpt(enclosing, null);
                }

                builder.Visit((CSharpSyntaxNode)syntax, enclosing);
            }

            // the other place this is possible is in a local function
            if (builder._sawYield)
                methodsWithYields.Add(syntax);
            return builder._map;
        }

        public override void VisitCompilationUnit(CompilationUnitSyntax node)
        {
            foreach (MemberDeclarationSyntax member in node.Members)
            {
                if (member.Kind() == SyntaxKind.GlobalStatement)
                {
                    Visit(member);
                }
            }
        }

        private LocalBinderFactory(Symbol containingMemberOrLambda, SyntaxNode root, Binder enclosing, ArrayBuilder<SyntaxNode> methodsWithYields)
        {
            Debug.Assert((object)containingMemberOrLambda != null);
            Debug.Assert(containingMemberOrLambda.Kind != SymbolKind.Local && containingMemberOrLambda.Kind != SymbolKind.RangeVariable && containingMemberOrLambda.Kind != SymbolKind.Parameter);

            _map = new SmallDictionary<SyntaxNode, Binder>(ReferenceEqualityComparer.Instance);
            _containingMemberOrLambda = containingMemberOrLambda;
            _enclosing = enclosing;
            _methodsWithYields = methodsWithYields;
            _root = root;
        }

        #region Starting points - these nodes contain statements

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            VisitBlock(node.Body);
        }

        public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            VisitBlock(node.Body);
        }

        public override void VisitDestructorDeclaration(DestructorDeclarationSyntax node)
        {
            VisitBlock(node.Body);
        }

        public override void VisitAccessorDeclaration(AccessorDeclarationSyntax node)
        {
            VisitBlock(node.Body);
        }

        public override void VisitConversionOperatorDeclaration(ConversionOperatorDeclarationSyntax node)
        {
            VisitBlock(node.Body);
        }

        public override void VisitOperatorDeclaration(OperatorDeclarationSyntax node)
        {
            VisitBlock(node.Body);
        }

        public override void VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node)
        {
            VisitLambdaExpression(node);
        }

        private void VisitLambdaExpression(LambdaExpressionSyntax node)
        {
            // Do not descend into a lambda unless it is a root node
            if (_root != node)
            {
                return;
            }

            CSharpSyntaxNode body = node.Body;
            if (body.Kind() == SyntaxKind.Block)
            {
                VisitBlock((BlockSyntax)body);
            }
            else
            {
                var binder = new ExpressionVariableBinder(body, _enclosing);
                AddToMap(body, binder);
                Visit(body, binder);
            }
        }

        public override void VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
        {
            VisitLambdaExpression(node);
        }

        public override void VisitLocalFunctionStatement(LocalFunctionStatementSyntax node)
        {
            var body = (CSharpSyntaxNode)node.Body ?? node.ExpressionBody;
            LocalFunctionSymbol match = null;
            // Don't use LookupLocalFunction because it recurses up the tree, as it
            // should be defined in the directly enclosing block (see note below)

            Binder possibleScopeBinder = _enclosing;
            while (possibleScopeBinder != null && !possibleScopeBinder.IsLocalFunctionsScopeBinder)
            {
                possibleScopeBinder = possibleScopeBinder.Next;
            }

            if (possibleScopeBinder != null)
            {
                foreach (var candidate in possibleScopeBinder.LocalFunctions)
                {
                    if (candidate.Locations[0] == node.Identifier.GetLocation())
                    {
                        match = candidate;
                    }
                }
            }

            bool oldSawYield = _sawYield;
            _sawYield = false;

            if (match != null)
            {
                var oldMethod = _containingMemberOrLambda;
                _containingMemberOrLambda = match;

                if (body != null)
                {
                    Binder binder = match.IsGenericMethod
                        ? new WithMethodTypeParametersBinder(match, _enclosing)
                        : _enclosing;

                    Visit(body, new InMethodBinder(match, binder));
                }

                _containingMemberOrLambda = oldMethod;
            }
            else
            {
                // The enclosing block should have found this node and created a LocalFunctionMethodSymbol
                // The code that does so is in LocalScopeBinder.BuildLocalFunctions

                if (body != null)
                {
                    // do our best to attempt to bind
                    Visit(body);
                }
            }

            if (_sawYield)
            {
                _methodsWithYields.Add(body);
            }
            _sawYield = oldSawYield;
        }

        public override void VisitArrowExpressionClause(ArrowExpressionClauseSyntax node)
        {
            var arrowBinder = new ExpressionVariableBinder(node, _enclosing);
            AddToMap(node, arrowBinder);
            Visit(node.Expression, arrowBinder);
        }

        public override void VisitEqualsValueClause(EqualsValueClauseSyntax node)
        {
            var valueBinder = new ExpressionVariableBinder(node, _enclosing);
            AddToMap(node, valueBinder);
            Visit(node.Value, valueBinder);
        }

        public override void VisitAttribute(AttributeSyntax node)
        {
            var attrBinder = new ExpressionVariableBinder(node, _enclosing);
            AddToMap(node, attrBinder);

            if (node.ArgumentList?.Arguments.Count > 0)
            {
                foreach (var argument in node.ArgumentList.Arguments)
                {
                    Visit(argument.Expression, attrBinder);
                }
            }
        }

        public override void VisitArgumentList(ArgumentListSyntax node)
        {
            if (_root == node)
            {
                // We are supposed to get here only for constructor initializers
                Debug.Assert(node.Parent is ConstructorInitializerSyntax);
                var argBinder = new ExpressionVariableBinder(node, _enclosing);
                AddToMap(node, argBinder);

                foreach (var arg in node.Arguments)
                {
                    Visit(arg.Expression, argBinder);
                }
            }
            else
            {
                base.VisitArgumentList(node);
            }
        }

        public override void VisitAnonymousMethodExpression(AnonymousMethodExpressionSyntax node)
        {
            // Do not descend into a lambda unless it is a root node
            if (_root != node)
            {
                return;
            }

            VisitBlock(node.Block);
        }

        public override void VisitGlobalStatement(GlobalStatementSyntax node)
        {
            Visit(node.Statement);
        }

        #endregion

        // Top-level block has an enclosing that is not a BinderContext. All others must (so that variables can be declared).
        public override void VisitBlock(BlockSyntax node)
        {
            Debug.Assert((object)_containingMemberOrLambda == _enclosing.ContainingMemberOrLambda);
            var blockBinder = new BlockBinder(_enclosing, node);
            AddToMap(node, blockBinder);

            // Visit all the statements inside this block
            foreach (StatementSyntax statement in node.Statements)
            {
                Visit(statement, blockBinder);
            }
        }

        public override void VisitUsingStatement(UsingStatementSyntax node)
        {
            Debug.Assert((object)_containingMemberOrLambda == _enclosing.ContainingMemberOrLambda);
            var usingBinder = new UsingStatementBinder(_enclosing, node);
            AddToMap(node, usingBinder);

            ExpressionSyntax expressionSyntax = node.Expression;
            VariableDeclarationSyntax declarationSyntax = node.Declaration;

            Debug.Assert((expressionSyntax == null) ^ (declarationSyntax == null)); // Can't have both or neither.

            if (expressionSyntax != null)
            {
                Visit(expressionSyntax, usingBinder);
            }
            else
            {
                foreach (VariableDeclaratorSyntax declarator in declarationSyntax.Variables)
                {
                    Visit(declarator, usingBinder);
                }
            }

            VisitPossibleEmbeddedStatement(node.Statement, usingBinder);
        }

        public override void VisitWhileStatement(WhileStatementSyntax node)
        {
            Debug.Assert((object)_containingMemberOrLambda == _enclosing.ContainingMemberOrLambda);
            var whileBinder = new WhileBinder(_enclosing, node);
            AddToMap(node, whileBinder);

            Visit(node.Condition, whileBinder);
            VisitPossibleEmbeddedStatement(node.Statement, whileBinder);
        }

        public override void VisitDoStatement(DoStatementSyntax node)
        {
            Debug.Assert((object)_containingMemberOrLambda == _enclosing.ContainingMemberOrLambda);
            var whileBinder = new WhileBinder(_enclosing, node);
            AddToMap(node, whileBinder);

            Visit(node.Condition, whileBinder);
            VisitPossibleEmbeddedStatement(node.Statement, whileBinder);
        }

        public override void VisitForStatement(ForStatementSyntax node)
        {
            Debug.Assert((object)_containingMemberOrLambda == _enclosing.ContainingMemberOrLambda);
            var binder = new ForLoopBinder(_enclosing, node);
            AddToMap(node, binder);

            var declaration = node.Declaration;
            if (declaration != null)
            {
                foreach (var variable in declaration.Variables)
                {
                    Visit(variable, binder);
                }
            }
            else
            {
                foreach (var initializer in node.Initializers)
                {
                    Visit(initializer, binder);
                } 
            }

            if (node.Condition != null)
            {
                Visit(node.Condition, binder);
            }

            foreach (var incrementor in node.Incrementors)
            {
                Visit(incrementor, binder);
            }

            VisitPossibleEmbeddedStatement(node.Statement, binder);
        }

        private void VisitCommonForEachStatement(CommonForEachStatementSyntax node)
        {
            Debug.Assert((object)_containingMemberOrLambda == _enclosing.ContainingMemberOrLambda);
            var patternBinder = new ExpressionVariableBinder(node.Expression, _enclosing);

            AddToMap(node.Expression, patternBinder);
            Visit(node.Expression, patternBinder);

            var binder = new ForEachLoopBinder(patternBinder, node);
            AddToMap(node, binder);

            VisitPossibleEmbeddedStatement(node.Statement, binder);
        }

        public override void VisitForEachStatement(ForEachStatementSyntax node)
        {
            VisitCommonForEachStatement(node);
        }

        public override void VisitForEachVariableStatement(ForEachVariableStatementSyntax node)
        {
            VisitCommonForEachStatement(node);
        }

        public override void VisitCheckedStatement(CheckedStatementSyntax node)
        {
            var binder = _enclosing.WithCheckedOrUncheckedRegion(@checked: node.Kind() == SyntaxKind.CheckedStatement);
            AddToMap(node, binder);

            Visit(node.Block, binder);
        }

        public override void VisitUnsafeStatement(UnsafeStatementSyntax node)
        {
            var binder = _enclosing.WithAdditionalFlags(BinderFlags.UnsafeRegion);
            AddToMap(node, binder);

            Visit(node.Block, binder); // This will create the block binder for the block.
        }

        public override void VisitFixedStatement(FixedStatementSyntax node)
        {
            Debug.Assert((object)_containingMemberOrLambda == _enclosing.ContainingMemberOrLambda);
            var binder = new FixedStatementBinder(_enclosing, node);
            AddToMap(node, binder);

            if (node.Declaration != null)
            {
                foreach (VariableDeclaratorSyntax declarator in node.Declaration.Variables)
                {
                    Visit(declarator, binder);
                }
            }

            VisitPossibleEmbeddedStatement(node.Statement, binder);
        }

        public override void VisitLockStatement(LockStatementSyntax node)
        {
            var lockBinder = new LockBinder(_enclosing, node);
            AddToMap(node, lockBinder);

            Visit(node.Expression, lockBinder);

            StatementSyntax statement = node.Statement;
            var statementBinder = lockBinder.WithAdditionalFlags(BinderFlags.InLockBody);
            if (statementBinder != lockBinder)
            {
                AddToMap(statement, statementBinder);
            }

            VisitPossibleEmbeddedStatement(statement, statementBinder);
        }

        public override void VisitSwitchStatement(SwitchStatementSyntax node)
        {
            Debug.Assert((object)_containingMemberOrLambda == _enclosing.ContainingMemberOrLambda);
            AddToMap(node.Expression, _enclosing);
            Visit(node.Expression, _enclosing);

            var switchBinder = SwitchBinder.Create(_enclosing, node);
            AddToMap(node, switchBinder);

            foreach (SwitchSectionSyntax section in node.Sections)
            {
                Visit(section, switchBinder);
            }
        }

        public override void VisitSwitchSection(SwitchSectionSyntax node)
        {
            var patternBinder = new ExpressionVariableBinder(node, _enclosing);
            AddToMap(node, patternBinder);

            foreach (var label in node.Labels)
            {
                switch (label.Kind())
                {
                    case SyntaxKind.CasePatternSwitchLabel:
                        {
                            var switchLabel = (CasePatternSwitchLabelSyntax)label;
                            Visit(switchLabel.Pattern, patternBinder);
                            if (switchLabel.WhenClause != null)
                            {
                                Visit(switchLabel.WhenClause.Condition, patternBinder);
                            }
                            break;
                        }
                    case SyntaxKind.CaseSwitchLabel:
                        {
                            var switchLabel = (CaseSwitchLabelSyntax)label;
                            Visit(switchLabel.Value, patternBinder);
                            break;
                        }
                }
            }

            foreach (StatementSyntax statement in node.Statements)
            {
                Visit(statement, patternBinder);
            }
        }

        public override void VisitIfStatement(IfStatementSyntax node)
        {
            Visit(node.Condition, _enclosing);
            VisitPossibleEmbeddedStatement(node.Statement, _enclosing);
            Visit(node.Else, _enclosing);
        }

        public override void VisitElseClause(ElseClauseSyntax node)
        {
            VisitPossibleEmbeddedStatement(node.Statement, _enclosing);
        }

        public override void VisitLabeledStatement(LabeledStatementSyntax node)
        {
            Visit(node.Statement, _enclosing);
        }

        public override void VisitTryStatement(TryStatementSyntax node)
        {
            if (node.Catches.Any())
            {
                // NOTE: We're going to cheat a bit - we know that the block is definitely going 
                // to get a map entry, so we don't need to worry about the WithAdditionalFlags
                // binder being dropped.  That is, there's no point in adding the WithAdditionalFlags
                // binder to the map ourselves and having VisitBlock unconditionally overwrite it.
                Visit(node.Block, _enclosing.WithAdditionalFlags(BinderFlags.InTryBlockOfTryCatch));
            }
            else
            {
                Visit(node.Block, _enclosing);
            }

            foreach (var c in node.Catches)
            {
                Visit(c, _enclosing);
            }

            if (node.Finally != null)
            {
                Visit(node.Finally, _enclosing);
            }
        }

        public override void VisitCatchClause(CatchClauseSyntax node)
        {
            Debug.Assert((object)_containingMemberOrLambda == _enclosing.ContainingMemberOrLambda);
            var clauseBinder = new CatchClauseBinder(_enclosing, node);
            AddToMap(node, clauseBinder);

            if (node.Filter != null)
            {
                var filterBinder = clauseBinder.WithAdditionalFlags(BinderFlags.InCatchFilter);
                AddToMap(node.Filter, filterBinder);
                Visit(node.Filter, filterBinder);
            }

            Visit(node.Block, clauseBinder);
        }

        public override void VisitCatchFilterClause(CatchFilterClauseSyntax node)
        {
            Visit(node.FilterExpression);
        }

        public override void VisitFinallyClause(FinallyClauseSyntax node)
        {
            // NOTE: We're going to cheat a bit - we know that the block is definitely going 
            // to get a map entry, so we don't need to worry about the WithAdditionalFlags
            // binder being dropped.  That is, there's no point in adding the WithAdditionalFlags
            // binder to the map ourselves and having VisitBlock unconditionally overwrite it.

            // If this finally block is nested inside a catch block, we need to use a distinct
            // binder flag so that we can detect the nesting order for error CS074: A throw
            // statement with no arguments is not allowed in a finally clause that is nested inside
            // the nearest enclosing catch clause.

            var additionalFlags = BinderFlags.InFinallyBlock;
            if (_enclosing.Flags.Includes(BinderFlags.InCatchBlock))
            {
                additionalFlags |= BinderFlags.InNestedFinallyBlock;
            }

            Visit(node.Block, _enclosing.WithAdditionalFlags(additionalFlags));

            Binder finallyBinder;
            Debug.Assert(_map.TryGetValue(node.Block, out finallyBinder) && finallyBinder.Flags.Includes(BinderFlags.InFinallyBlock));
        }

        public override void VisitYieldStatement(YieldStatementSyntax node)
        {
            if (node.Expression != null)
            {
                Visit(node.Expression, _enclosing);
            }

            _sawYield = true;
        }

        public override void VisitExpressionStatement(ExpressionStatementSyntax node)
        {
            Visit(node.Expression, _enclosing);
        }

        public override void VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
        {
            foreach (var decl in node.Declaration.Variables)
            {
                Visit(decl);
            }
        }

        public override void VisitVariableDeclarator(VariableDeclaratorSyntax node)
        {
            Visit(node.ArgumentList);
            Visit(node.Initializer?.Value);
        }

        public override void VisitReturnStatement(ReturnStatementSyntax node)
        {
            if (node.Expression != null)
            {
                Visit(node.Expression, _enclosing);
            }
        }

        public override void VisitThrowStatement(ThrowStatementSyntax node)
        {
            if (node.Expression != null)
            {
                Visit(node.Expression, _enclosing);
            }
        }

        public override void VisitBinaryExpression(BinaryExpressionSyntax node)
        {
            // The binary operators (except ??) are left-associative, and expressions of the form
            // a + b + c + d .... are relatively common in machine-generated code. The parser can handle
            // creating a deep-on-the-left syntax tree no problem, and then we promptly blow the stack.

            // For the purpose of creating binders, the order, in which we visit expressions, is not 
            // significant. 
            while (true)
            {
                Visit(node.Right);
                var binOp = node.Left as BinaryExpressionSyntax;
                if (binOp == null)
                {
                    Visit(node.Left);
                    break;
                }

                node = binOp;
            }
        }

        public override void DefaultVisit(SyntaxNode node)
        {
            // We should only get here for statements that don't introduce new scopes.
            // Given pattern variables, they must have no subexpressions either.
            // It is fine to get here for non-statements.
            base.DefaultVisit(node);
        }

        private void AddToMap(SyntaxNode node, Binder binder)
        {
            // If this ever breaks, make sure that all callers of
            // CanHaveAssociatedLocalBinder are in sync.
            Debug.Assert(node.CanHaveAssociatedLocalBinder() || 
                (node == _root && node is ExpressionSyntax));

            // Cleverness: for some nodes (e.g. lock), we want to specify a binder flag that
            // applies to the embedded statement, but not to the entire node.  Since the
            // embedded statement may or may not have its own binder, we need a way to ensure
            // that the flag is set regardless.  We accomplish this by adding a binder for
            // the embedded statement immediately, and then overwriting it with one constructed
            // in the usual way, if there is such a binder.  That's why we're using update,
            // rather than add, semantics.
            Binder existing;
            // Note that a lock statement has two outer binders (a second one for pattern variable scope)
            Debug.Assert(!_map.TryGetValue(node, out existing) || existing == binder || existing == binder.Next || existing == binder.Next?.Next);

            _map[node] = binder;
        }

        /// <summary>
        /// Some statements by default do not introduce its own scope for locals. 
        /// For example: Expression Statement, Return Statement, etc. However, 
        /// when a statement like that is an embedded statement (like IfStatementSyntax.Statement), 
        /// then it should introduce a scope for locals declared within it. 
        /// Here we are detecting such statements and creating a binder that should own the scope.
        /// </summary>
        private Binder GetBinderForPossibleEmbeddedStatement(StatementSyntax statement, Binder enclosing, out CSharpSyntaxNode embeddedScopeDesignator)
        {
            switch (statement.Kind())
            {
                case SyntaxKind.LocalDeclarationStatement:
                case SyntaxKind.LabeledStatement:
                case SyntaxKind.LocalFunctionStatement:
                // It is an error to have a declaration or a label in an embedded statement,
                // but we still want to bind it.  

                case SyntaxKind.ExpressionStatement:
                case SyntaxKind.WhileStatement:
                case SyntaxKind.DoStatement:
                case SyntaxKind.LockStatement:
                case SyntaxKind.IfStatement:
                case SyntaxKind.YieldReturnStatement:
                case SyntaxKind.ReturnStatement:
                case SyntaxKind.ThrowStatement:
                    Debug.Assert((object)_containingMemberOrLambda == enclosing.ContainingMemberOrLambda);
                    embeddedScopeDesignator = statement;
                    return new EmbeddedStatementBinder(enclosing, statement);

                case SyntaxKind.SwitchStatement:
                    Debug.Assert((object)_containingMemberOrLambda == enclosing.ContainingMemberOrLambda);
                    var switchStatement = (SwitchStatementSyntax)statement;
                    embeddedScopeDesignator = switchStatement.Expression;
                    return new ExpressionVariableBinder(switchStatement.Expression, enclosing);

                default:
                    embeddedScopeDesignator = null;
                    return enclosing;
            }
        }

        private void VisitPossibleEmbeddedStatement(StatementSyntax statement, Binder enclosing)
        {
            if (statement != null)
            {
                CSharpSyntaxNode embeddedScopeDesignator;
                // Some statements by default do not introduce its own scope for locals. 
                // For example: Expression Statement, Return Statement, etc. However, 
                // when a statement like that is an embedded statement (like IfStatementSyntax.Statement), 
                // then it should introduce a scope for locals declared within it. Here we are detecting 
                // such statements and creating a binder that should own the scope.
                enclosing = GetBinderForPossibleEmbeddedStatement(statement, enclosing, out embeddedScopeDesignator);

                if (embeddedScopeDesignator != null)
                {
                    AddToMap(embeddedScopeDesignator, enclosing);
                }

                Visit(statement, enclosing);
            }
        }

        public override void VisitQueryExpression(QueryExpressionSyntax node)
        {
            Visit(node.FromClause.Expression);
            Visit(node.Body);
        }

        public override void VisitQueryBody(QueryBodySyntax node)
        {
            foreach (var clause in node.Clauses)
            {
                if (clause.Kind() == SyntaxKind.JoinClause)
                {
                    Visit(((JoinClauseSyntax)clause).InExpression);
                }
            }

            Visit(node.Continuation);
        }
    }
}
