// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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

        protected override ImmutableArray<LocalSymbol> BuildLocals()
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
                    locals.Add(MakeLocal(declarationSyntax, declarator, LocalDeclarationKind.UsingVariable, allowScoped: true));

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

        internal override BoundStatement BindUsingStatementParts(BindingDiagnosticBag diagnostics, Binder originalBinder)
        {
            ExpressionSyntax expressionSyntax = TargetExpressionSyntax;
            VariableDeclarationSyntax declarationSyntax = _syntax.Declaration;
            bool hasAwait = _syntax.AwaitKeyword.Kind() != default;

            Debug.Assert((expressionSyntax == null) ^ (declarationSyntax == null)); // Can't have both or neither.

            var boundUsingStatement = BindUsingStatementOrDeclarationFromParts((CSharpSyntaxNode)expressionSyntax ?? declarationSyntax, _syntax.UsingKeyword, _syntax.AwaitKeyword, originalBinder, this, diagnostics);
            Debug.Assert(boundUsingStatement is BoundUsingStatement);
            return boundUsingStatement;
        }

#nullable enable
        internal static BoundStatement BindUsingStatementOrDeclarationFromParts(SyntaxNode syntax, SyntaxToken usingKeyword, SyntaxToken awaitKeyword, Binder originalBinder, UsingStatementBinder? usingBinderOpt, BindingDiagnosticBag diagnostics)
        {
            bool isUsingDeclaration = syntax.Kind() == SyntaxKind.LocalDeclarationStatement;
            bool isExpression = !isUsingDeclaration && syntax.Kind() != SyntaxKind.VariableDeclaration;
            bool hasAwait = awaitKeyword != default;

            if (isUsingDeclaration)
            {
                CheckFeatureAvailability(usingKeyword, MessageID.IDS_FeatureUsingDeclarations, diagnostics);
            }
            else if (hasAwait)
            {
                CheckFeatureAvailability(awaitKeyword, MessageID.IDS_FeatureAsyncUsing, diagnostics);
            }

            Debug.Assert(isUsingDeclaration || usingBinderOpt != null);

            bool hasErrors = false;
            ImmutableArray<BoundLocalDeclaration> declarationsOpt = default;
            BoundMultipleLocalDeclarations? multipleDeclarationsOpt = null;
            BoundExpression? expressionOpt = null;
            TypeSymbol? declarationTypeOpt = null;
            MethodArgumentInfo? patternDisposeInfo;
            TypeSymbol? awaitableTypeOpt;

            if (isExpression)
            {
                expressionOpt = usingBinderOpt!.BindTargetExpression(diagnostics, originalBinder);
                hasErrors |= !bindDisposable(fromExpression: true, out patternDisposeInfo, out awaitableTypeOpt);
                Debug.Assert(expressionOpt is not null);
                if (expressionOpt.Type is not null)
                {
                    CheckRestrictedTypeInAsyncMethod(originalBinder.ContainingMemberOrLambda, expressionOpt.Type, diagnostics, expressionOpt.Syntax, forUsingExpression: true);
                }
            }
            else
            {
                VariableDeclarationSyntax declarationSyntax = isUsingDeclaration ? ((LocalDeclarationStatementSyntax)syntax).Declaration : (VariableDeclarationSyntax)syntax;
                originalBinder.BindForOrUsingOrFixedDeclarations(declarationSyntax, LocalDeclarationKind.UsingVariable, diagnostics, out declarationsOpt);

                Debug.Assert(!declarationsOpt.IsEmpty && declarationsOpt[0].DeclaredTypeOpt != null);
                multipleDeclarationsOpt = new BoundMultipleLocalDeclarations(declarationSyntax, declarationsOpt);
                declarationTypeOpt = declarationsOpt[0].DeclaredTypeOpt!.Type;

                if (declarationTypeOpt.IsDynamic())
                {
                    patternDisposeInfo = null;
                    awaitableTypeOpt = null;
                }
                else
                {
                    hasErrors |= !bindDisposable(fromExpression: false, out patternDisposeInfo, out awaitableTypeOpt);
                }
            }

            BoundAwaitableInfo? awaitOpt = null;
            if (hasAwait)
            {
                // even if we don't have a proper value to await, we'll still report bad usages of `await`
                originalBinder.ReportBadAwaitDiagnostics(awaitKeyword, diagnostics, ref hasErrors);

                if (awaitableTypeOpt is null)
                {
                    awaitOpt = new BoundAwaitableInfo(syntax, awaitableInstancePlaceholder: null, isDynamic: true, getAwaiter: null, isCompleted: null, getResult: null) { WasCompilerGenerated = true };
                }
                else
                {
                    hasErrors |= ReportUseSite(awaitableTypeOpt, diagnostics, awaitKeyword);
                    var placeholder = new BoundAwaitableValuePlaceholder(syntax, awaitableTypeOpt).MakeCompilerGenerated();
                    awaitOpt = originalBinder.BindAwaitInfo(placeholder, syntax, diagnostics, ref hasErrors);
                }
            }

            // This is not awesome, but its factored. 
            // In the future it might be better to have a separate shared type that we add the info to, and have the callers create the appropriate bound nodes from it
            if (isUsingDeclaration)
            {
                return new BoundUsingLocalDeclarations(syntax, patternDisposeInfo, awaitOpt, declarationsOpt, hasErrors);
            }
            else
            {
                BoundStatement boundBody = originalBinder.BindPossibleEmbeddedStatement(usingBinderOpt!._syntax.Statement, diagnostics);

                return new BoundUsingStatement(
                    usingBinderOpt._syntax,
                    usingBinderOpt.Locals,
                    multipleDeclarationsOpt,
                    expressionOpt,
                    boundBody,
                    awaitOpt,
                    patternDisposeInfo,
                    hasErrors);
            }

            bool bindDisposable(bool fromExpression, out MethodArgumentInfo? patternDisposeInfo, out TypeSymbol? awaitableType)
            {
                TypeSymbol disposableInterface = getDisposableInterface(hasAwait);
                Debug.Assert((object)disposableInterface != null);

                CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = originalBinder.GetNewCompoundUseSiteInfo(diagnostics);
                Conversion iDisposableConversion = classifyConversion(fromExpression, disposableInterface, ref useSiteInfo);
                patternDisposeInfo = null;
                awaitableType = null;

                diagnostics.Add(syntax, useSiteInfo);

                if (iDisposableConversion.IsImplicit)
                {
                    if (hasAwait)
                    {
                        awaitableType = originalBinder.Compilation.GetWellKnownType(WellKnownType.System_Threading_Tasks_ValueTask);
                    }

                    return !ReportUseSite(disposableInterface, diagnostics, hasAwait ? awaitKeyword : usingKeyword);
                }

                Debug.Assert(!fromExpression || expressionOpt != null);
                TypeSymbol? type = fromExpression ? expressionOpt!.Type : declarationTypeOpt;

                // If this is a ref struct, or we're in a valid asynchronous using, try binding via pattern.
                // We won't need to try and bind a second time if it fails, as async dispose can't be pattern based (ref structs are not allowed in async methods)
                if (type is object && (type.IsRefLikeType || hasAwait))
                {
                    BoundExpression? receiver = fromExpression
                                               ? expressionOpt
                                               : new BoundLocal(syntax, declarationsOpt[0].LocalSymbol, null, type) { WasCompilerGenerated = true };

                    BindingDiagnosticBag patternDiagnostics = originalBinder.Compilation.IsFeatureEnabled(MessageID.IDS_FeatureDisposalPattern)
                                                       ? diagnostics
                                                       : BindingDiagnosticBag.Discarded;
                    MethodSymbol disposeMethod = originalBinder.TryFindDisposePatternMethod(receiver, syntax, hasAwait, patternDiagnostics);
                    if (disposeMethod is object)
                    {
                        MessageID.IDS_FeatureDisposalPattern.CheckFeatureAvailability(diagnostics, originalBinder.Compilation, syntax.Location);

                        var argumentsBuilder = ArrayBuilder<BoundExpression>.GetInstance(disposeMethod.ParameterCount);
                        ImmutableArray<int> argsToParams = default;
                        bool expanded = disposeMethod.HasParamsParameter();
                        originalBinder.BindDefaultArgumentsAndParamsArray(
                            // If this is a using statement, then we want to use the whole `using (expr) { }` as the argument location. These arguments
                            // will be represented in the IOperation tree and the "correct" node for them, given that they are an implicit invocation
                            // at the end of the using statement, is on the whole using statement, not on the current expression.
                            usingBinderOpt?._syntax ?? syntax,
                            disposeMethod.Parameters,
                            argumentsBuilder,
                            argumentRefKindsBuilder: null,
                            namesBuilder: null,
                            ref argsToParams,
                            out BitVector defaultArguments,
                            expanded,
                            enableCallerInfo: true,
                            patternDiagnostics);

                        Debug.Assert(argsToParams.IsDefault);
                        patternDisposeInfo = new MethodArgumentInfo(disposeMethod, argumentsBuilder.ToImmutableAndFree(), defaultArguments, expanded);
                        if (hasAwait)
                        {
                            awaitableType = disposeMethod.ReturnType;
                        }
                        return true;
                    }
                }

                if (type is null || !type.IsErrorType())
                {
                    // Retry with a different assumption about whether the `using` is async
                    TypeSymbol alternateInterface = getDisposableInterface(!hasAwait);
                    var discardedUseSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
                    Conversion alternateConversion = classifyConversion(fromExpression, alternateInterface, ref discardedUseSiteInfo);

                    bool wrongAsync = alternateConversion.IsImplicit;
                    ErrorCode errorCode = wrongAsync
                        ? (hasAwait ? ErrorCode.ERR_NoConvToIAsyncDispWrongAsync : ErrorCode.ERR_NoConvToIDispWrongAsync)
                        : (hasAwait ? ErrorCode.ERR_NoConvToIAsyncDisp : ErrorCode.ERR_NoConvToIDisp);

                    Error(diagnostics, errorCode, syntax, declarationTypeOpt ?? expressionOpt!.Display);
                }

                return false;
            }

            Conversion classifyConversion(bool fromExpression, TypeSymbol targetInterface, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
            {
                var conversions = originalBinder.Conversions;
                if (fromExpression)
                {
                    Debug.Assert(expressionOpt is { });
                    var result = conversions.ClassifyImplicitConversionFromExpression(expressionOpt, targetInterface, ref useSiteInfo);

                    Debug.Assert(expressionOpt.Type?.IsDynamic() != true || result.Kind == ConversionKind.ImplicitDynamic);
                    return result;
                }
                else
                {
                    Debug.Assert(declarationTypeOpt is { });
                    var result = conversions.ClassifyImplicitConversionFromType(declarationTypeOpt, targetInterface, ref useSiteInfo);

                    Debug.Assert(!declarationTypeOpt.IsDynamic() || result.Kind == ConversionKind.ImplicitDynamic);
                    return result;
                }
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

            throw ExceptionUtilities.Unreachable();
        }

        internal override ImmutableArray<LocalFunctionSymbol> GetDeclaredLocalFunctionsForScope(CSharpSyntaxNode scopeDesignator)
        {
            throw ExceptionUtilities.Unreachable();
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
