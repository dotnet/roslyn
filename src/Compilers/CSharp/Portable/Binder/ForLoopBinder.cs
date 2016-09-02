// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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

            // Deconstruction, Declaration, and Initializers are mutually exclusive.
            if (_syntax.Deconstruction != null)
            {
                CollectLocalsFromDeconstruction(
                    _syntax.Deconstruction.VariableComponent,
                    LocalDeclarationKind.ForInitializerVariable,
                    locals,
                    _syntax);
                ExpressionVariableFinder.FindExpressionVariables(this, locals, _syntax.Deconstruction.Value);
            }
            else if (_syntax.Declaration != null)
            {
                foreach (var vdecl in _syntax.Declaration.Variables)
                {
                    var localSymbol = MakeLocal(_syntax.Declaration, vdecl, LocalDeclarationKind.ForInitializerVariable);
                    locals.Add(localSymbol);

                    // also gather expression-declared variables from the bracketed argument lists and the initializers
                    ExpressionVariableFinder.FindExpressionVariables(this, locals, vdecl);
                }
            }
            else
            {
                ExpressionVariableFinder.FindExpressionVariables(this, locals, _syntax.Initializers);
            }

            ExpressionVariableFinder.FindExpressionVariables(this, locals, node: _syntax.Condition);
            ExpressionVariableFinder.FindExpressionVariables(this, locals, _syntax.Incrementors);
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
            // Deconstruction, Declaration, and Initializers are mutually exclusive.
            if (_syntax.Deconstruction != null)
            {
                var assignment = originalBinder.BindDeconstructionDeclaration(node.Deconstruction, node.Deconstruction.VariableComponent, node.Deconstruction.Value, diagnostics);
                initializer = new BoundLocalDeconstructionDeclaration(node, assignment);
            }
            else if (_syntax.Declaration != null)
            {
                ImmutableArray<BoundLocalDeclaration> unused;
                initializer = originalBinder.BindForOrUsingOrFixedDeclarations(node.Declaration, LocalDeclarationKind.ForInitializerVariable, diagnostics, out unused);
            }
            else
            {
                initializer = originalBinder.BindStatementExpressionList(node.Initializers, diagnostics);
            }

            var condition = (node.Condition != null) ? originalBinder.BindBooleanExpression(node.Condition, diagnostics) : null;
            var increment = originalBinder.BindStatementExpressionList(node.Incrementors, diagnostics);
            var body = originalBinder.BindPossibleEmbeddedStatement(node.Statement, diagnostics);

            Debug.Assert(this.Locals == this.GetDeclaredLocalsForScope(node));
            return new BoundForStatement(node,
                                         this.Locals,
                                         initializer,
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
