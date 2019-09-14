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

                // gather expression-declared variables from invalid array dimensions. eg. using(int[x is var y] z = new int[0])
                declarationSyntax.Type.VisitRankSpecifiers((rankSpecifier, args) =>
                {
                    foreach (var size in rankSpecifier.Sizes)
                    {
                        if (size.Kind() != SyntaxKind.OmittedArraySizeExpression)
                        {
                            ExpressionVariableFinder.FindExpressionVariables(args.binder, args.locals, size);
                        }
                    }
                }, (binder: this, locals: locals));

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

            var boundUsingStatement = BindUsingStatementOrDeclarationFromParts((CSharpSyntaxNode)expressionSyntax ?? declarationSyntax, _syntax.UsingKeyword, _syntax.AwaitKeyword, originalBinder, this, diagnostics);
            Debug.Assert(boundUsingStatement is BoundUsingStatement);
            return boundUsingStatement;
        }

        internal static BoundStatement BindUsingStatementOrDeclarationFromParts(SyntaxNode syntax, SyntaxToken usingKeyword, SyntaxToken awaitKeyword, Binder originalBinder, UsingStatementBinder usingBinderOpt, DiagnosticBag diagnostics)
        {
            bool isUsingDeclaration = syntax.Kind() == SyntaxKind.LocalDeclarationStatement;
            bool isExpression = !isUsingDeclaration && syntax.Kind() != SyntaxKind.VariableDeclaration;
            bool hasAwait = awaitKeyword != default;

            Debug.Assert(isUsingDeclaration || usingBinderOpt != null);

            TypeSymbol disposableInterface = getDisposableInterface(hasAwait);

            Debug.Assert((object)disposableInterface != null);
            bool hasErrors = ReportUseSiteDiagnostics(disposableInterface, diagnostics, hasAwait ? awaitKeyword : usingKeyword);

            Conversion iDisposableConversion = Conversion.NoConversion;
            ImmutableArray<BoundLocalDeclaration> declarationsOpt = default;
            BoundMultipleLocalDeclarations multipleDeclarationsOpt = null;
            BoundExpression expressionOpt = null;
            BoundAwaitableInfo awaitOpt = null;
            TypeSymbol declarationTypeOpt = null;
            MethodSymbol disposeMethodOpt = null;
            TypeSymbol awaitableTypeOpt = null;

            if (isExpression)
            {
                expressionOpt = usingBinderOpt.BindTargetExpression(diagnostics, originalBinder);
                hasErrors |= !populateDisposableConversionOrDisposeMethod(fromExpression: true);
            }
            else
            {
                VariableDeclarationSyntax declarationSyntax = isUsingDeclaration ? ((LocalDeclarationStatementSyntax)syntax).Declaration : (VariableDeclarationSyntax)syntax;
                originalBinder.BindForOrUsingOrFixedDeclarations(declarationSyntax, LocalDeclarationKind.UsingVariable, diagnostics, out declarationsOpt);

                Debug.Assert(!declarationsOpt.IsEmpty);
                multipleDeclarationsOpt = new BoundMultipleLocalDeclarations(declarationSyntax, declarationsOpt);
                declarationTypeOpt = declarationsOpt[0].DeclaredTypeOpt.Type;

                if (declarationTypeOpt.IsDynamic())
                {
                    iDisposableConversion = Conversion.ImplicitDynamic;
                }
                else
                {
                    hasErrors |= !populateDisposableConversionOrDisposeMethod(fromExpression: false);
                }
            }

            if (hasAwait)
            {
                BoundAwaitableValuePlaceholder placeholderOpt;
                if (awaitableTypeOpt is null)
                {
                    placeholderOpt = null;
                }
                else
                {
                    hasErrors |= ReportUseSiteDiagnostics(awaitableTypeOpt, diagnostics, awaitKeyword);
                    placeholderOpt = new BoundAwaitableValuePlaceholder(syntax, awaitableTypeOpt).MakeCompilerGenerated();
                }

                // even if we don't have a proper value to await, we'll still report bad usages of `await`
                awaitOpt = originalBinder.BindAwaitInfo(placeholderOpt, syntax, awaitKeyword.GetLocation(), diagnostics, ref hasErrors);
            }

            // This is not awesome, but its factored. 
            // In the future it might be better to have a separate shared type that we add the info to, and have the callers create the appropriate bound nodes from it
            if (isUsingDeclaration)
            {
                return new BoundUsingLocalDeclarations(syntax, disposeMethodOpt, iDisposableConversion, awaitOpt, declarationsOpt, hasErrors);
            }
            else
            {
                BoundStatement boundBody = originalBinder.BindPossibleEmbeddedStatement(usingBinderOpt._syntax.Statement, diagnostics);

                return new BoundUsingStatement(
                    usingBinderOpt._syntax,
                    usingBinderOpt.Locals,
                    multipleDeclarationsOpt,
                    expressionOpt,
                    iDisposableConversion,
                    boundBody,
                    awaitOpt,
                    disposeMethodOpt,
                    hasErrors);
            }

            // initializes iDisposableConversion, awaitableTypeOpt and disposeMethodOpt
            bool populateDisposableConversionOrDisposeMethod(bool fromExpression)
            {
                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                iDisposableConversion = classifyConversion(fromExpression, disposableInterface, ref useSiteDiagnostics);

                diagnostics.Add(syntax, useSiteDiagnostics);

                if (iDisposableConversion.IsImplicit)
                {
                    if (hasAwait)
                    {
                        awaitableTypeOpt = originalBinder.Compilation.GetWellKnownType(WellKnownType.System_Threading_Tasks_ValueTask);
                    }
                    return true;
                }

                TypeSymbol type = fromExpression ? expressionOpt.Type : declarationTypeOpt;

                // If this is a ref struct, or we're in a valid asynchronous using, try binding via pattern.
                // We won't need to try and bind a second time if it fails, as async dispose can't be pattern based (ref structs are not allowed in async methods)
                if (type is object && (type.IsRefLikeType || hasAwait))
                {
                    BoundExpression receiver = fromExpression
                                               ? expressionOpt
                                               : new BoundLocal(syntax, declarationsOpt[0].LocalSymbol, null, type) { WasCompilerGenerated = true };

                    disposeMethodOpt = originalBinder.TryFindDisposePatternMethod(receiver, syntax, hasAwait, diagnostics);
                    if (disposeMethodOpt is object)
                    {
                        if (hasAwait)
                        {
                            awaitableTypeOpt = disposeMethodOpt.ReturnType;
                        }
                        return true;
                    }
                }

                if (type is null || !type.IsErrorType())
                {
                    // Retry with a different assumption about whether the `using` is async
                    TypeSymbol alternateInterface = getDisposableInterface(!hasAwait);
                    HashSet<DiagnosticInfo> ignored = null;
                    Conversion alternateConversion = classifyConversion(fromExpression, alternateInterface, ref ignored);

                    bool wrongAsync = alternateConversion.IsImplicit;
                    ErrorCode errorCode = wrongAsync
                        ? (hasAwait ? ErrorCode.ERR_NoConvToIAsyncDispWrongAsync : ErrorCode.ERR_NoConvToIDispWrongAsync)
                        : (hasAwait ? ErrorCode.ERR_NoConvToIAsyncDisp : ErrorCode.ERR_NoConvToIDisp);

                    Error(diagnostics, errorCode, syntax, declarationTypeOpt ?? expressionOpt.Display);
                }

                return false;
            }

            Conversion classifyConversion(bool fromExpression, TypeSymbol targetInterface, ref HashSet<DiagnosticInfo> diag)
            {
                return fromExpression ?
                    originalBinder.Conversions.ClassifyImplicitConversionFromExpression(expressionOpt, targetInterface, ref diag) :
                    originalBinder.Conversions.ClassifyImplicitConversionFromType(declarationTypeOpt, targetInterface, ref diag);
            }

            TypeSymbol getDisposableInterface(bool isAsync)
            {
                return isAsync
                    ? originalBinder.Compilation.GetWellKnownType(WellKnownType.System_IAsyncDisposable)
                    : originalBinder.Compilation.GetSpecialType(SpecialType.System_IDisposable);
            }
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
