// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
using System.Collections.Generic;

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

            Debug.Assert((expressionSyntax == null) ^ (declarationSyntax == null)); // Can't have both or neither.

            bool hasErrors = false;
            BoundMultipleLocalDeclarations declarationsOpt = null;
            BoundExpression expressionOpt = null;
            Conversion iDisposableConversion = Conversion.NoConversion;
            TypeSymbol iDisposable = this.Compilation.GetSpecialType(SpecialType.System_IDisposable); // no need for diagnostics, so use the Compilation version
            Debug.Assert((object)iDisposable != null);

            if (expressionSyntax != null)
            {
                expressionOpt = this.BindTargetExpression(diagnostics, originalBinder);

                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                TypeSymbol expressionType = expressionOpt.Type;
                MethodSymbol disposeMethod = expressionType == null ? null : TryFindDisposePattern(expressionOpt.Type, diagnostics);
                iDisposableConversion = originalBinder.Conversions.ClassifyImplicitConversionFromExpression(expressionOpt, iDisposable, ref useSiteDiagnostics);
                diagnostics.Add(expressionSyntax, useSiteDiagnostics);

                if (!iDisposableConversion.IsImplicit)
                {
                    if ((object)disposeMethod != null)
                    {
                        if (!disposeMethod.ReturnsVoid)
                        {
                            Error(diagnostics, ErrorCode.WRN_PatternBadSignature, expressionSyntax, expressionType);
                        }
                        BoundStatement boundBody2 = originalBinder.BindPossibleEmbeddedStatement(_syntax.Statement, diagnostics);

                        Debug.Assert(GetDeclaredLocalsForScope(_syntax) == this.Locals);
                        return new BoundUsingStatement(
                            _syntax,
                            this.Locals,
                            declarationsOpt,
                            expressionOpt,
                            Conversion.Identity,
                            boundBody2,
                            disposeMethod,
                            hasErrors
                            );
                    }
                    else if ((object)expressionType == null || !expressionType.IsErrorType())
                    {
                        Error(diagnostics, ErrorCode.ERR_NoConvToIDisp, expressionSyntax, expressionOpt.Display);
                    }
                    hasErrors = true;
                }
            }
            else
            {
                ImmutableArray<BoundLocalDeclaration> declarations;
                originalBinder.BindForOrUsingOrFixedDeclarations(declarationSyntax, LocalDeclarationKind.UsingVariable, diagnostics, out declarations);

                Debug.Assert(!declarations.IsEmpty);

                declarationsOpt = new BoundMultipleLocalDeclarations(declarationSyntax, declarations);

                TypeSymbol declType = declarations[0].DeclaredType.Type;

                MethodSymbol disposeMethod = TryFindDisposePattern(declType, diagnostics);

                if (declType.IsDynamic())
                {
                    iDisposableConversion = Conversion.ImplicitDynamic;
                }
                else if ((object)disposeMethod != null)
                {
                    BoundStatement boundBody2 = originalBinder.BindPossibleEmbeddedStatement(_syntax.Statement, diagnostics);

                    Debug.Assert(GetDeclaredLocalsForScope(_syntax) == this.Locals);
                    return new BoundUsingStatement(
                        _syntax,
                        this.Locals,
                        declarationsOpt,
                        expressionOpt,
                        Conversion.Identity,
                        boundBody2,
                        disposeMethod,
                        hasErrors
                        );
                }
                else
                {
                    HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                    iDisposableConversion = originalBinder.Conversions.ClassifyImplicitConversionFromType(declType, iDisposable, ref useSiteDiagnostics);
                    diagnostics.Add(declarationSyntax, useSiteDiagnostics);

                    if (!iDisposableConversion.IsImplicit)
                    {
                        if (!declType.IsErrorType())
                        {
                            Error(diagnostics, ErrorCode.ERR_NoConvToIDisp, declarationSyntax, declType);
                        }

                        hasErrors = true;
                    }
                }
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
                disposeMethodOpt: null, // This ensures that a pattern-matched Dispose statement is not used.
                hasErrors);
        }

        /// <summary>
        /// Checks for a Dispose method on exprType. Failing to satisfy the pattern is not an error -
        /// it just means we have to check for an interface instead.
        /// </summary>
        /// <param name="exprType">Type of the expression over which to iterate</param>
        /// <param name="diagnostics">Populated with warnings if there are near misses</param>
        /// <returns>True if a matching method is found (still need to verify return type).</returns>
        private MethodSymbol TryFindDisposePattern(TypeSymbol exprType, DiagnosticBag diagnostics)
        {
            LookupResult lookupResult = LookupResult.GetInstance();
            SyntaxNode exp = _syntax.Expression != null ? (SyntaxNode) _syntax.Expression : (SyntaxNode) _syntax.Declaration;
            MethodSymbol disposeMethod = FindPatternMethod(exprType, WellKnownMemberNames.DisposeMethodName, lookupResult, exp, warningsOnly: true, diagnostics: diagnostics, _syntax.SyntaxTree);
            lookupResult.Free();

            if (!((object)disposeMethod is null) && !disposeMethod.ReturnsVoid)
            {
                Error(diagnostics, ErrorCode.WRN_PatternBadSignature, _syntax);
                disposeMethod = null;
            }

            return disposeMethod;
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
