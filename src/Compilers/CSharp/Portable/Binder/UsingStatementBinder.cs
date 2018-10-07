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
            TypeSymbol iDisposable = getDisposableInterface(hasAwait);

            Debug.Assert((object)iDisposable != null);
            bool hasErrors = ReportUseSiteDiagnostics(iDisposable, diagnostics, hasAwait ? _syntax.AwaitKeyword : _syntax.UsingKeyword);

            Conversion iDisposableConversion = Conversion.NoConversion;
            BoundMultipleLocalDeclarations declarationsOpt = null;
            BoundExpression expressionOpt = null;
            AwaitableInfo awaitOpt = null;
            TypeSymbol declarationTypeOpt = null;

            if (expressionSyntax != null)
            {
                expressionOpt = this.BindTargetExpression(diagnostics, originalBinder);
                hasErrors |= initConversionConsideringAlternate(iDisposable, diagnostics, fromExpression: true);
            }
            else
            {
                ImmutableArray<BoundLocalDeclaration> declarations;
                originalBinder.BindForOrUsingOrFixedDeclarations(declarationSyntax, LocalDeclarationKind.UsingVariable, diagnostics, out declarations);

                Debug.Assert(!declarations.IsEmpty);
                declarationsOpt = new BoundMultipleLocalDeclarations(declarationSyntax, declarations);
                declarationTypeOpt = declarations[0].DeclaredType.Type;

                if (declarationTypeOpt.IsDynamic())
                {
                    iDisposableConversion = Conversion.ImplicitDynamic;
                }
                else
                {
                    hasErrors |= initConversionConsideringAlternate(iDisposable, diagnostics, fromExpression: false);
                }
            }

            if (hasAwait)
            {
                TypeSymbol taskType = this.Compilation.GetWellKnownType(WellKnownType.System_Threading_Tasks_ValueTask);
                hasErrors |= ReportUseSiteDiagnostics(taskType, diagnostics, _syntax.AwaitKeyword);

                var resource = (SyntaxNode)expressionSyntax ?? declarationSyntax;
                BoundExpression placeholder = new BoundAwaitableValuePlaceholder(resource, taskType).MakeCompilerGenerated();
                awaitOpt = BindAwaitInfo(placeholder, resource, _syntax.AwaitKeyword.GetLocation(), diagnostics, ref hasErrors);
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
                hasErrors);

            // returns true for error
            bool initConversionConsideringAlternate(TypeSymbol disposableInterface, DiagnosticBag bag, bool fromExpression)
            {
                DisposableConversion conversionResult = getConversion(disposableInterface, bag, fromExpression, out iDisposableConversion);

                switch (conversionResult)
                {
                    case DisposableConversion.CascadingError:
                        return true;
                    case DisposableConversion.FailedNotReported:
                        // Retry with a different assumption about whether the `using` is async
                        TypeSymbol alternateInterface = getDisposableInterface(!hasAwait);
                        var alternateConversionResult = getConversion(alternateInterface, bag: null, fromExpression: expressionSyntax != null, out _);

                        bool wrongAsync = (alternateConversionResult == DisposableConversion.Succeeded);
                        ErrorCode errorCode = wrongAsync
                            ? (hasAwait ? ErrorCode.ERR_NoConvToIAsyncDispWrongAsync : ErrorCode.ERR_NoConvToIDispWrongAsync)
                            : (hasAwait ? ErrorCode.ERR_NoConvToIAsyncDisp : ErrorCode.ERR_NoConvToIDisp);

                        Error(diagnostics, errorCode, (CSharpSyntaxNode)declarationSyntax ?? expressionSyntax, declarationTypeOpt ?? expressionOpt.Display);
                        return true;
                    default:
                        return false;
                }
            }

            DisposableConversion getConversion(TypeSymbol disposableInterface, DiagnosticBag bag, bool fromExpression, out Conversion conversion)
            {
                HashSet<DiagnosticInfo> useSiteDiagnostics = null;

                conversion = fromExpression ?
                    originalBinder.Conversions.ClassifyImplicitConversionFromExpression(expressionOpt, disposableInterface, ref useSiteDiagnostics) :
                    originalBinder.Conversions.ClassifyImplicitConversionFromType(declarationTypeOpt, disposableInterface, ref useSiteDiagnostics);

                bag?.Add(fromExpression ? (CSharpSyntaxNode)expressionSyntax : declarationSyntax, useSiteDiagnostics);

                if (!conversion.IsImplicit)
                {
                    TypeSymbol type = fromExpression ? expressionOpt.Type : declarationTypeOpt;
                    if (type is null || !type.IsErrorType())
                    {
                        return DisposableConversion.FailedNotReported;
                    }
                    return DisposableConversion.CascadingError;
                }

                return DisposableConversion.Succeeded;
            }

            TypeSymbol getDisposableInterface(bool isAsync)
            {
                return isAsync
                    ? this.Compilation.GetWellKnownType(WellKnownType.System_IAsyncDisposable)
                    : this.Compilation.GetSpecialType(SpecialType.System_IDisposable);
            }
        }

        private enum DisposableConversion
        {
            Succeeded,
            FailedNotReported,
            CascadingError
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
