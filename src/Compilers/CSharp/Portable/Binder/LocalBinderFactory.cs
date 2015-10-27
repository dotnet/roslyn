﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

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
    internal sealed class LocalBinderFactory : CSharpSyntaxVisitor
    {
        private readonly SmallDictionary<CSharpSyntaxNode, Binder> _map;
        private bool _sawYield;
        private readonly MethodSymbol _method;
        private Binder _enclosing;

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

        public static SmallDictionary<CSharpSyntaxNode, Binder> BuildMap(MethodSymbol method, CSharpSyntaxNode syntax, Binder enclosing, out bool sawYield)
        {
            var builder = new LocalBinderFactory(method, enclosing);
            builder.Visit(syntax);
            sawYield = builder._sawYield;
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

        private LocalBinderFactory(MethodSymbol method, Binder enclosing)
        {
            Debug.Assert((object)method != null);
            _map = new SmallDictionary<CSharpSyntaxNode, Binder>(ReferenceEqualityComparer.Instance);
            _method = method;
            _enclosing = enclosing;
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
            CSharpSyntaxNode body = node.Body;
            if (body.Kind() == SyntaxKind.Block)
            {
                VisitBlock((BlockSyntax)body);
            }
        }

        public override void VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
        {
            CSharpSyntaxNode body = node.Body;
            if (body.Kind() == SyntaxKind.Block)
            {
                VisitBlock((BlockSyntax)body);
            }
        }

        public override void VisitArrowExpressionClause(ArrowExpressionClauseSyntax node)
        {
            // Do nothing, expressions do not need special binders.
        }

        public override void VisitAnonymousMethodExpression(AnonymousMethodExpressionSyntax node)
        {
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
            Debug.Assert((object)_method == _enclosing.ContainingMemberOrLambda);
            var blockBinder = new BlockBinder(_enclosing, node.Statements);
            AddToMap(node, blockBinder);

            // Visit all the statements inside this block
            foreach (StatementSyntax statement in node.Statements)
            {
                Visit(statement, blockBinder);
            }
        }

        public override void VisitUsingStatement(UsingStatementSyntax node)
        {
            Debug.Assert((object)_method == _enclosing.ContainingMemberOrLambda);
            var usingBinder = new UsingStatementBinder(_enclosing, node);
            AddToMap(node, usingBinder);

            VisitPossibleEmbeddedStatement(node.Statement, usingBinder);
        }

        public override void VisitWhileStatement(WhileStatementSyntax node)
        {
            Debug.Assert((object)_method == _enclosing.ContainingMemberOrLambda);
            var whileBinder = new WhileBinder(_enclosing, node);
            AddToMap(node, whileBinder);

            VisitPossibleEmbeddedStatement(node.Statement, whileBinder);
        }

        public override void VisitDoStatement(DoStatementSyntax node)
        {
            Debug.Assert((object)_method == _enclosing.ContainingMemberOrLambda);
            var whileBinder = new WhileBinder(_enclosing, node);
            AddToMap(node, whileBinder);

            VisitPossibleEmbeddedStatement(node.Statement, whileBinder);
        }

        public override void VisitForStatement(ForStatementSyntax node)
        {
            Debug.Assert((object)_method == _enclosing.ContainingMemberOrLambda);
            var binder = new ForLoopBinder(_enclosing, node);
            AddToMap(node, binder);

            VisitPossibleEmbeddedStatement(node.Statement, binder);
        }

        public override void VisitForEachStatement(ForEachStatementSyntax node)
        {
            Debug.Assert((object)_method == _enclosing.ContainingMemberOrLambda);
            var binder = new ForEachLoopBinder(_enclosing, node);
            AddToMap(node, binder);

            VisitPossibleEmbeddedStatement(node.Statement, binder);
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
            Debug.Assert((object)_method == _enclosing.ContainingMemberOrLambda);
            var binder = new FixedStatementBinder(_enclosing, node);
            AddToMap(node, binder);

            VisitPossibleEmbeddedStatement(node.Statement, binder);
        }

        public override void VisitLockStatement(LockStatementSyntax node)
        {
            var lockBinder = new LockBinder(_enclosing, node);
            AddToMap(node, lockBinder);

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
            Debug.Assert((object)_method == _enclosing.ContainingMemberOrLambda);
            var switchBinder = new SwitchBinder(_enclosing, node);
            AddToMap(node, switchBinder);

            foreach (SwitchSectionSyntax section in node.Sections)
            {
                Visit(section, switchBinder);
            }
        }

        public override void VisitSwitchSection(SwitchSectionSyntax node)
        {
            foreach (StatementSyntax statement in node.Statements)
            {
                Visit(statement, _enclosing);
            }
        }

        public override void VisitIfStatement(IfStatementSyntax node)
        {
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
            Debug.Assert((object)_method == _enclosing.ContainingMemberOrLambda);
            var clauseBinder = new CatchClauseBinder(_enclosing, node);
            AddToMap(node, clauseBinder);
            Visit(node.Block, clauseBinder);

            if (node.Filter != null)
            {
                var filterBinder = clauseBinder.WithAdditionalFlags(BinderFlags.InCatchFilter);
                AddToMap(node.Filter, filterBinder);
                Visit(node.Filter, filterBinder);
            }
        }

        public override void VisitCatchFilterClause(CatchFilterClauseSyntax node)
        {
            // No variables can be defined in a filter atm. 
            // Note - this needs to be updated when declaration expressions are implemented.
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
            _sawYield = true;
            base.VisitYieldStatement(node);
        }

        public override void DefaultVisit(SyntaxNode node)
        {
            // We should only get here for statements that don't introduce new scopes.
            Debug.Assert(node is StatementSyntax || _method.IsSynthesizedLambda());
            base.DefaultVisit(node);
        }

        private void AddToMap(CSharpSyntaxNode node, Binder binder)
        {
            // If this ever breaks, make sure that all callers of
            // CanHaveAssociatedLocalBinder are in sync.
            Debug.Assert(node.CanHaveAssociatedLocalBinder());

            // Cleverness: for some nodes (e.g. lock), we want to specify a binder flag that
            // applies to the embedded statement, but not to the entire node.  Since the
            // embedded statement may or may not have its own binder, we need a way to ensure
            // that the flag is set regardless.  We accomplish this by adding a binder for
            // the embedded statement immediately, and then overwriting it with one constructed
            // in the usual way, if there is such a binder.  That's why we're using update,
            // rather than add, semantics.
            Binder existing;
            Debug.Assert(!_map.TryGetValue(node, out existing) || existing == binder.Next);

            _map[node] = binder;
        }

        private void VisitPossibleEmbeddedStatement(StatementSyntax statement, Binder enclosing)
        {
            if (statement != null)
            {
                switch (statement.Kind())
                {
                    case SyntaxKind.LocalDeclarationStatement:
                    case SyntaxKind.LabeledStatement:
                        // It is an error to have a declaration or a label in an embedded statement,
                        // but we still want to bind it.  We'll pretend that the statement was
                        // inside a block.

                        Debug.Assert((object)_method == enclosing.ContainingMemberOrLambda);
                        var blockBinder = new BlockBinder(enclosing, new SyntaxList<StatementSyntax>(statement));
                        AddToMap(statement, blockBinder);
                        Visit(statement, blockBinder);
                        return;

                    case SyntaxKind.Block:
                    case SyntaxKind.UsingStatement:
                    case SyntaxKind.WhileStatement:
                    case SyntaxKind.DoStatement:
                    case SyntaxKind.ForStatement:
                    case SyntaxKind.ForEachStatement:
                    case SyntaxKind.FixedStatement:
                    case SyntaxKind.LockStatement:
                    case SyntaxKind.SwitchStatement:
                    case SyntaxKind.IfStatement:
                        // These statements always have dedicated binders.
                        break;

                    default:
                        break;
                }
            }

            Visit(statement, enclosing);
        }
    }
}
