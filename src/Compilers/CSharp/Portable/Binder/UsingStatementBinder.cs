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
            MethodSymbol disposeMethod = null;
            Conversion iDisposableConversion = Conversion.NoConversion;
            TypeSymbol iDisposable = this.Compilation.GetSpecialType(SpecialType.System_IDisposable); // no need for diagnostics, so use the Compilation version
            Debug.Assert((object)iDisposable != null);

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
                        disposeMethod = TryFindDisposePatternMethod(expressionType, diagnostics);
                    }
                    if (disposeMethod is null)
                    {
                        if (expressionType is null || !expressionType.IsErrorType())
                        {
                            Error(diagnostics, ErrorCode.ERR_NoConvToIDisp, expressionSyntax, expressionOpt.Display);
                        } 
                        hasErrors = true;
                    }       
                }
            }
            else
            {

                ImmutableArray<BoundLocalDeclaration> declarations;
                originalBinder.BindForOrUsingOrFixedDeclarations(declarationSyntax, LocalDeclarationKind.UsingVariable, diagnostics, out declarations);

                Debug.Assert(!declarations.IsEmpty);

                declarationsOpt = new BoundMultipleLocalDeclarations(declarationSyntax, declarations);

                TypeSymbol declType = declarations[0].DeclaredType.Type;

                if (declType.IsDynamic())
                {
                    iDisposableConversion = Conversion.ImplicitDynamic;
                }
                else
                {
                    HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                    iDisposableConversion = originalBinder.Conversions.ClassifyImplicitConversionFromType(declType, iDisposable, ref useSiteDiagnostics);
                    diagnostics.Add(declarationSyntax, useSiteDiagnostics);

                    if (!iDisposableConversion.IsImplicit)
                    {
                        disposeMethod = TryFindDisposePatternMethod(declType, diagnostics);
                        if (disposeMethod is null)
                        {
                            if (!declType.IsErrorType())
                            {
                                Error(diagnostics, ErrorCode.ERR_NoConvToIDisp, declarationSyntax, declType);
                            }
                            hasErrors = true;
                        }
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
                disposeMethod,
                hasErrors);
        }

        /// <summary>
        /// Checks for a Dispose method on exprType in the case that there is no explicit
        /// IDisposable conversion.
        /// </summary>
        /// <param name="exprType">Type of the expression over which to iterate</param>
        /// <param name="diagnostics">Populated with warnings if there are near misses</param>
        /// <returns>True if a matching method is found with correct return type.</returns>
        private MethodSymbol TryFindDisposePatternMethod(TypeSymbol exprType, DiagnosticBag diagnostics)
        {
            LookupResult lookupResult = LookupResult.GetInstance();
            SyntaxNode syntax = _syntax.Expression != null ? (SyntaxNode)_syntax.Expression : (SyntaxNode)_syntax.Declaration;
            MethodSymbol disposeMethod = FindPatternMethod(exprType, WellKnownMemberNames.DisposeMethodName, lookupResult, syntax, warningsOnly: true, diagnostics, _syntax.SyntaxTree, MessageID.IDS_Disposable);
            lookupResult.Free();

            if (disposeMethod?.ReturnsVoid == false)
            {
                diagnostics.Add(ErrorCode.WRN_PatternBadSignature, syntax.Location, exprType, MessageID.IDS_Disposable.Localize(), disposeMethod);
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
