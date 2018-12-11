// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class UsingStatementBinder : LockOrUsingBinder
    {
        private readonly UsingStatementSyntax _syntax;

        public UsingStatementBinder(Binder enclosing, UsingStatementSyntax syntax)
            : base(enclosing)
        {
            _syntax = syntax;
        }

        override protected ImmutableArray<LocalSymbol> BuildLocals()
        {
            ExpressionSyntax expressionSyntax = TargetExpressionSyntax;
            VariableDeclarationSyntax declarationSyntax = _syntax.Declaration;

            Debug.Assert((expressionSyntax == null) ^ (declarationSyntax == null)); // Can't have both or neither.

            if (expressionSyntax != null)
            {
                var locals = ArrayBuilder<LocalSymbol>.GetInstance();
                ExpressionVariableFinder.FindExpressionVariables(this, locals, expressionSyntax);
                return locals.ToImmutableAndFree();
            }
            else
            {
                var locals = ArrayBuilder<LocalSymbol>.GetInstance(declarationSyntax.Variables.Count);
                foreach (VariableDeclaratorSyntax declarator in declarationSyntax.Variables)
                {
                    locals.Add(MakeLocal(declarationSyntax, declarator, LocalDeclarationKind.UsingVariable));

                    // also gather expression-declared variables from the bracketed argument lists and the initializers
                    ExpressionVariableFinder.FindExpressionVariables(this, locals, declarator);
                }

                return locals.ToImmutableAndFree();
            }
        }

        protected override ExpressionSyntax TargetExpressionSyntax
        {
            get
            {
                return _syntax.Expression;
            }
        }

        internal override BoundStatement BindUsingStatementParts(DiagnosticBag diagnostics, Binder originalBinder)
        {
            ExpressionSyntax expressionSyntax = TargetExpressionSyntax;
            VariableDeclarationSyntax declarationSyntax = _syntax.Declaration;
            bool hasAwait = _syntax.AwaitKeyword.Kind() != default;

            Debug.Assert((expressionSyntax == null) ^ (declarationSyntax == null)); // Can't have both or neither.

            TypeSymbol iDisposable = hasAwait
                ? this.Compilation.GetWellKnownType(WellKnownType.System_IAsyncDisposable)
                : this.Compilation.GetSpecialType(SpecialType.System_IDisposable);

            Debug.Assert((object)iDisposable != null);
            bool hasErrors = ReportUseSiteDiagnostics(iDisposable, diagnostics, hasAwait ? _syntax.AwaitKeyword : _syntax.UsingKeyword);

            Conversion iDisposableConversion = Conversion.NoConversion;
            BoundMultipleLocalDeclarations declarationsOpt = null;
            BoundExpression expressionOpt = null;
            MethodSymbol disposeMethod = null;
            AwaitableInfo awaitOpt = null;

            if (expressionSyntax != null)
            {
                expressionOpt = this.BindTargetExpression(diagnostics, originalBinder);

                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                iDisposableConversion = originalBinder.Conversions.ClassifyImplicitConversionFromExpression(expressionOpt, iDisposable, ref useSiteDiagnostics);
                diagnostics.Add(expressionSyntax, useSiteDiagnostics);

                TypeSymbol expressionType = expressionOpt.Type;

                if (!iDisposableConversion.IsImplicit)
                {
                    if (!(expressionType is null))
                    {
                        disposeMethod = TryFindDisposePatternMethod(expressionOpt, expressionSyntax, hasAwait, diagnostics);
                    }
                    if (disposeMethod is null)
                    {
                        if (expressionType is null || !expressionType.IsErrorType())
                        {
                            Error(diagnostics, hasAwait ? ErrorCode.ERR_NoConvToIAsyncDisp : ErrorCode.ERR_NoConvToIDisp, expressionSyntax, expressionOpt.Display);
                        }
                        hasErrors = true;
                    }
                }
            }
            else
            {
                var declarations = BindUsingVariableDeclaration(originalBinder, diagnostics, hasErrors, declarationSyntax, declarationSyntax, hasAwait, out iDisposableConversion, out disposeMethod);
                declarationsOpt = new BoundMultipleLocalDeclarations(declarationSyntax, declarations, hasErrors);
            }

            if (hasAwait)
            {
                var resource = (SyntaxNode)expressionSyntax ?? declarationSyntax;
                awaitOpt = BindAsyncDisposeAwaiter(resource, _syntax.AwaitKeyword, disposeMethod, diagnostics, ref hasErrors);
            }

            BoundStatement boundBody = originalBinder.BindPossibleEmbeddedStatement(_syntax.Statement, diagnostics);

            Debug.Assert(GetDeclaredLocalsForScope(_syntax) == this.Locals);
            return new BoundUsingStatement(
                _syntax,
                this.Locals,
                declarationsOpt,
                expressionOpt,
                iDisposableConversion,
                boundBody,
                awaitOpt,
                disposeMethod,
                hasErrors);
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
