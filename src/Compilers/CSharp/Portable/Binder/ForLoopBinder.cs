﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

            var declaration = _syntax.Declaration;
            if (declaration != null)
            {
                if (!declaration.IsDeconstructionDeclaration)
                {
                    var refKind = _syntax.RefKeyword.Kind().GetRefKind();

                    foreach (var variable in declaration.Variables)
                    {
                        var localSymbol = MakeLocal(refKind,
                                                    declaration,
                                                    variable,
                                                    LocalDeclarationKind.ForInitializerVariable);
                        locals.Add(localSymbol);

                        if (variable.Initializer != null)
                        {
                            PatternVariableFinder.FindPatternVariables(this, locals, variable.Initializer.Value);
                        }
                    }
                }
                else
                {
                    CollectLocalsFromDeconstruction(declaration, declaration.Type, LocalDeclarationKind.ForInitializerVariable, locals);
                }
            }
            else
            {
                PatternVariableFinder.FindPatternVariables(this, locals, _syntax.Initializers);
            }

            if (_syntax.Condition != null)
            {
                PatternVariableFinder.FindPatternVariables(this, locals, node: _syntax.Condition);
            }

            PatternVariableFinder.FindPatternVariables(this, locals, _syntax.Incrementors);

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
            if (node.Declaration != null)
            {
                Debug.Assert(node.Initializers.Count == 0);
                if (node.Declaration.IsDeconstructionDeclaration)
                {
                    initializer = originalBinder.BindDeconstructionDeclaration(node.Declaration, node.Declaration, diagnostics);
                }
                else
                {
                    ImmutableArray<BoundLocalDeclaration> unused;
                    initializer = originalBinder.BindForOrUsingOrFixedDeclarations(node.Declaration, LocalDeclarationKind.ForInitializerVariable, diagnostics, out unused);
                }
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

        internal override ImmutableArray<LocalSymbol> GetDeclaredLocalsForScope(CSharpSyntaxNode scopeDesignator)
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
