// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class ForLoopBinder : LoopBinder
    {
        private readonly ForStatementSyntax _syntax;

        public ForLoopBinder(Binder enclosing, ForStatementSyntax syntax)
            : base(enclosing)
        {
            Debug.Assert(syntax != null);
            _syntax = syntax;
        }

        override protected ImmutableArray<LocalSymbol> BuildLocals()
        {
            var locals = ArrayBuilder<LocalSymbol>.GetInstance();

            // Declaration and Initializers are mutually exclusive.
            if (_syntax.Declaration != null)
            {
                foreach (var vdecl in _syntax.Declaration.Variables)
                {
                    var localSymbol = MakeLocal(_syntax.Declaration, vdecl, LocalDeclarationKind.RegularVariable);
                    locals.Add(localSymbol);

                    // also gather expression-declared variables from the bracketed argument lists and the initializers
                    ExpressionVariableFinder.FindExpressionVariables(this, locals, vdecl);
                }
            }
            else
            {
                ExpressionVariableFinder.FindExpressionVariables(this, locals, _syntax.Initializers);
            }

            return locals.ToImmutableAndFree();
        }

        internal override BoundForStatement BindForParts(DiagnosticBag diagnostics, Binder originalBinder)
        {
            BoundForStatement result = BindForParts(_syntax, originalBinder, diagnostics);
            return result;
        }

        private BoundForStatement BindForParts(ForStatementSyntax node, Binder originalBinder, DiagnosticBag diagnostics)
        {
            BoundStatement initializer;
            // Declaration and Initializers are mutually exclusive.
            if (_syntax.Declaration != null)
            {
                ImmutableArray<BoundLocalDeclaration> unused;
                initializer = originalBinder.BindForOrUsingOrFixedDeclarations(node.Declaration, LocalDeclarationKind.RegularVariable, diagnostics, out unused);
            }
            else
            {
                initializer = originalBinder.BindStatementExpressionList(node.Initializers, diagnostics);
            }

            BoundExpression condition = null;
            var innerLocals = ImmutableArray<LocalSymbol>.Empty;
            ExpressionSyntax conditionSyntax = node.Condition;
            if (conditionSyntax != null)
            {
                originalBinder = originalBinder.GetBinder(conditionSyntax);
                condition = originalBinder.BindBooleanExpression(conditionSyntax, diagnostics);
                innerLocals = originalBinder.GetDeclaredLocalsForScope(conditionSyntax);
            }

            BoundStatement increment = null;
            SeparatedSyntaxList<ExpressionSyntax> incrementors = node.Incrementors;
            if (incrementors.Count > 0)
            {
                var scopeDesignator = incrementors.First();
                var incrementBinder = originalBinder.GetBinder(scopeDesignator);
                increment = incrementBinder.BindStatementExpressionList(incrementors, diagnostics);
                Debug.Assert(increment.Kind != BoundKind.StatementList || ((BoundStatementList)increment).Statements.Length > 1);

                var locals = incrementBinder.GetDeclaredLocalsForScope(scopeDesignator);
                if (!locals.IsEmpty)
                {
                    if (increment.Kind == BoundKind.StatementList)
                    {
                        increment = new BoundBlock(scopeDesignator, locals, ((BoundStatementList)increment).Statements)
                                            { WasCompilerGenerated = true };
                    }
                    else
                    {
                        increment = new BoundBlock(increment.Syntax, locals, ImmutableArray.Create(increment))
                                            { WasCompilerGenerated = true };
                    }
                }
            }

            var body = originalBinder.BindPossibleEmbeddedStatement(node.Statement, diagnostics);

            Debug.Assert(this.Locals == this.GetDeclaredLocalsForScope(node));
            return new BoundForStatement(node,
                                         this.Locals,
                                         initializer,
                                         innerLocals,
                                         condition,
                                         increment,
                                         body,
                                         this.BreakLabel,
                                         this.ContinueLabel);
        }

        internal override ImmutableArray<LocalSymbol> GetDeclaredLocalsForScope(SyntaxNode scopeDesignator)
        {
            if (_syntax == scopeDesignator)
            {
                return this.Locals;
            }

            throw ExceptionUtilities.Unreachable;
        }

        internal override ImmutableArray<LocalFunctionSymbol> GetDeclaredLocalFunctionsForScope(CSharpSyntaxNode scopeDesignator)
        {
            throw ExceptionUtilities.Unreachable;
        }

        internal override SyntaxNode ScopeDesignator
        {
            get
            {
                return _syntax;
            }
        }
    }
}
