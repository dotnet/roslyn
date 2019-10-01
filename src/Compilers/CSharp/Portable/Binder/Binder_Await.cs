// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// This portion of the binder converts an AwaitExpressionSyntax into a BoundExpression
    /// </summary>
    internal partial class Binder
    {
        private BoundExpression BindAwait(AwaitExpressionSyntax node, DiagnosticBag diagnostics)
        {
            BoundExpression expression = BindRValueWithoutTargetType(node.Expression, diagnostics);

            return BindAwait(expression, node, diagnostics);
        }

        private BoundAwaitExpression BindAwait(BoundExpression expression, SyntaxNode node, DiagnosticBag diagnostics)
        {
            bool hasErrors = false;
            var placeholder = new BoundAwaitableValuePlaceholder(expression.Syntax, expression.Type);
            var info = BindAwaitInfo(expression, placeholder, node, node.Location, diagnostics, ref hasErrors);

            // Spec 7.7.7.2:
            // The expression await t is classified the same way as the expression (t).GetAwaiter().GetResult(). Thus,
            // if the return type of GetResult is void, the await-expression is classified as nothing. If it has a
            // non-void return type T, the await-expression is classified as a value of type T.
            TypeSymbol awaitExpressionType = info.GetResult?.ReturnType ?? (hasErrors ? CreateErrorType() : Compilation.DynamicType);

            return new BoundAwaitExpression(node, expression, info, awaitExpressionType, hasErrors);
        }

        internal BoundAwaitableInfo BindAwaitInfo(BoundExpression expressionOpt, BoundAwaitableValuePlaceholder placeholder, SyntaxNode node, Location location, DiagnosticBag diagnostics, ref bool hasErrors)
        {
            hasErrors |= ReportBadAwaitWithoutAsync(location, diagnostics);
            hasErrors |= ReportBadAwaitContext(node, location, diagnostics);

            BoundExpression getAwaiter = null;
            PropertySymbol isCompleted = null;
            MethodSymbol getResult = null;
            bool hasGetAwaitableErrors = false;
            if (expressionOpt != null)
            {
                hasGetAwaitableErrors = !GetAwaitableExpressionInfo(expressionOpt, placeholder, out getAwaiter, out isCompleted, out getResult, out _, node, diagnostics);
                hasErrors |= hasGetAwaitableErrors;
            }

            return new BoundAwaitableInfo(node, placeholder, getAwaiter, isCompleted, getResult, hasGetAwaitableErrors);
        }

        /// <summary>
        /// Return true iff an await with this subexpression would be legal where the expression appears.
        /// </summary>
        private bool CouldBeAwaited(BoundExpression expression)
        {
            // If the expression doesn't have a type, just bail out now. Also,
            // the dynamic type is always awaitable in an async method and
            // could generate a lot of noise if we warned on it. Finally, we only want
            // to warn on method calls, not other kinds of expressions.

            if (expression.Kind != BoundKind.Call)
            {
                return false;
            }

            var type = expression.Type;
            if (((object)type == null) ||
                type.IsDynamic() ||
                (type.IsVoidType()))
            {
                return false;
            }

            var call = (BoundCall)expression;

            // First check if the target method is async.
            if ((object)call.Method != null && call.Method.IsAsync)
            {
                return true;
            }

            // Then check if the method call returns a WinRT async type.
            if (ImplementsWinRTAsyncInterface(call.Type))
            {
                return true;
            }

            // Finally, if we're in an async method, and the expression could be awaited, report that it is instead discarded.
            var containingMethod = this.ContainingMemberOrLambda as MethodSymbol;
            if ((object)containingMethod == null || !containingMethod.IsAsync)
            {
                return false;
            }

            if (ContextForbidsAwait)
            {
                return false;
            }

            var fakeDiagnostics = DiagnosticBag.GetInstance();
            var boundAwait = BindAwait(expression, expression.Syntax, fakeDiagnostics);
            fakeDiagnostics.Free();
            return !boundAwait.HasAnyErrors;
        }

        /// <summary>
        /// Assuming we are in an async method, return true if we're in a context where await would be illegal.
        /// Specifically, return true if we're in a lock or catch filter.
        /// </summary>
        private bool ContextForbidsAwait
        {
            get
            {
                return this.Flags.Includes(BinderFlags.InCatchFilter) ||
                    this.Flags.Includes(BinderFlags.InLockBody);
            }
        }

        /// <summary>
        /// Reports an error if the await expression did not occur in an async context.
        /// </summary>
        /// <returns>True if the expression contains errors.</returns>
        private bool ReportBadAwaitWithoutAsync(Location location, DiagnosticBag diagnostics)
        {
            DiagnosticInfo info = null;
            var containingMemberOrLambda = this.ContainingMemberOrLambda;
            if ((object)containingMemberOrLambda != null)
            {
                switch (containingMemberOrLambda.Kind)
                {
                    case SymbolKind.Field:
                        if (containingMemberOrLambda.ContainingType.IsScriptClass)
                        {
                            if (((FieldSymbol)containingMemberOrLambda).IsStatic)
                            {
                                info = new CSDiagnosticInfo(ErrorCode.ERR_BadAwaitInStaticVariableInitializer);
                            }
                            else
                            {
                                return false;
                            }
                        }
                        break;
                    case SymbolKind.Method:
                        var method = (MethodSymbol)containingMemberOrLambda;
                        if (method.IsAsync)
                        {
                            return false;
                        }
                        if (method.MethodKind == MethodKind.AnonymousFunction)
                        {
                            info = method.IsImplicitlyDeclared ?
                                // The await expression occurred in a query expression:
                                new CSDiagnosticInfo(ErrorCode.ERR_BadAwaitInQuery) :
                                new CSDiagnosticInfo(ErrorCode.ERR_BadAwaitWithoutAsyncLambda, ((LambdaSymbol)method).MessageID.Localize());
                        }
                        else
                        {
                            info = method.ReturnsVoid ?
                                new CSDiagnosticInfo(ErrorCode.ERR_BadAwaitWithoutVoidAsyncMethod) :
                                new CSDiagnosticInfo(ErrorCode.ERR_BadAwaitWithoutAsyncMethod, method.ReturnType);
                        }
                        break;
                }
            }
            if (info == null)
            {
                info = new CSDiagnosticInfo(ErrorCode.ERR_BadAwaitWithoutAsync);
            }
            Error(diagnostics, info, location);
            return true;
        }

        /// <summary>
        /// Report diagnostics if the await expression occurs in a context where it is not allowed.
        /// </summary>
        /// <returns>True if errors were found.</returns>
        private bool ReportBadAwaitContext(SyntaxNode node, Location location, DiagnosticBag diagnostics)
        {
            if (this.InUnsafeRegion && !this.Flags.Includes(BinderFlags.AllowAwaitInUnsafeContext))
            {
                Error(diagnostics, ErrorCode.ERR_AwaitInUnsafeContext, location);
                return true;
            }
            else if (this.Flags.Includes(BinderFlags.InLockBody))
            {
                Error(diagnostics, ErrorCode.ERR_BadAwaitInLock, location);
                return true;
            }
            else if (this.Flags.Includes(BinderFlags.InCatchFilter))
            {
                Error(diagnostics, ErrorCode.ERR_BadAwaitInCatchFilter, location);
                return true;
            }
            else if (this.Flags.Includes(BinderFlags.InFinallyBlock) &&
                (node.SyntaxTree as CSharpSyntaxTree)?.Options?.IsFeatureEnabled(MessageID.IDS_AwaitInCatchAndFinally) == false)
            {
                Error(diagnostics, ErrorCode.ERR_BadAwaitInFinally, location);
                return true;
            }
            else if (this.Flags.Includes(BinderFlags.InCatchBlock) &&
                (node.SyntaxTree as CSharpSyntaxTree)?.Options?.IsFeatureEnabled(MessageID.IDS_AwaitInCatchAndFinally) == false)
            {
                Error(diagnostics, ErrorCode.ERR_BadAwaitInCatch, location);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Finds and validates the required members of an awaitable expression, as described in spec 7.7.7.1.
        /// </summary>
        /// <returns>True if the expression is awaitable; false otherwise.</returns>
        internal bool GetAwaitableExpressionInfo(
            BoundExpression expression,
            out BoundExpression getAwaiterGetResultCall,
            SyntaxNode node,
            DiagnosticBag diagnostics)
        {
            return GetAwaitableExpressionInfo(expression, expression, out _, out _, out _, out getAwaiterGetResultCall, node, diagnostics);
        }

        private bool GetAwaitableExpressionInfo(
            BoundExpression expression,
            BoundExpression getAwaiterArgument,
            out BoundExpression getAwaiter,
            out PropertySymbol isCompleted,
            out MethodSymbol getResult,
            out BoundExpression getAwaiterGetResultCall,
            SyntaxNode node,
            DiagnosticBag diagnostics)
        {
            Debug.Assert(TypeSymbol.Equals(expression.Type, getAwaiterArgument.Type, TypeCompareKind.ConsiderEverything));

            getAwaiter = null;
            isCompleted = null;
            getResult = null;
            getAwaiterGetResultCall = null;

            if (!ValidateAwaitedExpression(expression, node, diagnostics))
            {
                return false;
            }

            if (expression.HasDynamicType())
            {
                return true;
            }

            if (!GetGetAwaiterMethod(getAwaiterArgument, node, diagnostics, out getAwaiter))
            {
                return false;
            }

            TypeSymbol awaiterType = getAwaiter.Type;
            return GetIsCompletedProperty(awaiterType, node, expression.Type, diagnostics, out isCompleted)
                && AwaiterImplementsINotifyCompletion(awaiterType, node, diagnostics)
                && GetGetResultMethod(getAwaiter, node, expression.Type, diagnostics, out getResult, out getAwaiterGetResultCall);
        }

        /// <summary>
        /// Validates the awaited expression, returning true if no errors are found.
        /// </summary>
        private static bool ValidateAwaitedExpression(BoundExpression expression, SyntaxNode node, DiagnosticBag diagnostics)
        {
            if (expression.HasAnyErrors)
            {
                // The appropriate diagnostics have already been reported.
                return false;
            }

            if ((object)expression.Type == null)
            {
                Error(diagnostics, ErrorCode.ERR_BadAwaitArgIntrinsic, node, expression.Display);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Finds the GetAwaiter method of an awaitable expression.
        /// </summary>
        /// <remarks>
        /// Spec 7.7.7.1:
        /// An awaitable expression t has an accessible instance or extension method called GetAwaiter with no
        /// parameters and no type parameters, and a return type A that meets the additional requirements for an
        /// Awaiter.
        /// NOTE: this is an error in the spec.  An extension method of the form
        /// Awaiter&lt;T&gt; GetAwaiter&lt;T&gt;(this Task&lt;T&gt;) may be used.
        /// </remarks>
        private bool GetGetAwaiterMethod(BoundExpression expression, SyntaxNode node, DiagnosticBag diagnostics, out BoundExpression getAwaiterCall)
        {
            if (expression.Type.IsVoidType())
            {
                Error(diagnostics, ErrorCode.ERR_BadAwaitArgVoidCall, node);
                getAwaiterCall = null;
                return false;
            }

            getAwaiterCall = MakeInvocationExpression(node, expression, WellKnownMemberNames.GetAwaiter, ImmutableArray<BoundExpression>.Empty, diagnostics);
            if (getAwaiterCall.HasAnyErrors) // && !expression.HasAnyErrors?
            {
                getAwaiterCall = null;
                return false;
            }

            if (getAwaiterCall.Kind != BoundKind.Call)
            {
                Error(diagnostics, ErrorCode.ERR_BadAwaitArg, node, expression.Type);
                getAwaiterCall = null;
                return false;
            }

            var getAwaiterMethod = ((BoundCall)getAwaiterCall).Method;
            if (getAwaiterMethod is ErrorMethodSymbol ||
                HasOptionalOrVariableParameters(getAwaiterMethod) || // We might have been able to resolve a GetAwaiter overload with optional parameters, so check for that here
                getAwaiterMethod.ReturnsVoid) // If GetAwaiter returns void, don't bother checking that it returns an Awaiter.
            {
                Error(diagnostics, ErrorCode.ERR_BadAwaitArg, node, expression.Type);
                getAwaiterCall = null;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Finds the IsCompleted property of an Awaiter type.
        /// </summary>
        /// <remarks>
        /// Spec 7.7.7.1:
        /// An Awaiter A has an accessible, readable instance property IsCompleted of type bool.
        /// </remarks>
        private bool GetIsCompletedProperty(TypeSymbol awaiterType, SyntaxNode node, TypeSymbol awaitedExpressionType, DiagnosticBag diagnostics, out PropertySymbol isCompletedProperty)
        {
            var receiver = new BoundLiteral(node, ConstantValue.Null, awaiterType);
            var name = WellKnownMemberNames.IsCompleted;
            var qualified = BindInstanceMemberAccess(node, node, receiver, name, 0, default(SeparatedSyntaxList<TypeSyntax>), default(ImmutableArray<TypeWithAnnotations>), invoked: false, indexed: false, diagnostics);
            if (qualified.HasAnyErrors)
            {
                isCompletedProperty = null;
                return false;
            }

            if (qualified.Kind != BoundKind.PropertyAccess)
            {
                Error(diagnostics, ErrorCode.ERR_NoSuchMember, node, awaiterType, WellKnownMemberNames.IsCompleted);
                isCompletedProperty = null;
                return false;
            }

            isCompletedProperty = ((BoundPropertyAccess)qualified).PropertySymbol;
            if (isCompletedProperty.IsWriteOnly)
            {
                Error(diagnostics, ErrorCode.ERR_PropertyLacksGet, node, isCompletedProperty);
                isCompletedProperty = null;
                return false;
            }

            if (isCompletedProperty.Type.SpecialType != SpecialType.System_Boolean)
            {
                Error(diagnostics, ErrorCode.ERR_BadAwaiterPattern, node, awaiterType, awaitedExpressionType);
                isCompletedProperty = null;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks that the Awaiter implements System.Runtime.CompilerServices.INotifyCompletion.
        /// </summary>
        /// <remarks>
        /// Spec 7.7.7.1:
        /// An Awaiter A implements the interface System.Runtime.CompilerServices.INotifyCompletion.
        /// </remarks>
        private bool AwaiterImplementsINotifyCompletion(TypeSymbol awaiterType, SyntaxNode node, DiagnosticBag diagnostics)
        {
            var INotifyCompletion = GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_INotifyCompletion, diagnostics, node);
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;

            var conversion = this.Conversions.ClassifyImplicitConversionFromType(awaiterType, INotifyCompletion, ref useSiteDiagnostics);
            if (!conversion.IsImplicit)
            {
                diagnostics.Add(node, useSiteDiagnostics);
                Error(diagnostics, ErrorCode.ERR_DoesntImplementAwaitInterface, node, awaiterType, INotifyCompletion);
                return false;
            }

            Debug.Assert(conversion.IsValid);
            return true;
        }

        /// <summary>
        /// Finds the GetResult method of an Awaiter type.
        /// </summary>
        /// <remarks>
        /// Spec 7.7.7.1:
        /// An Awaiter A has an accessible instance method GetResult with no parameters and no type parameters.
        /// </remarks>
        private bool GetGetResultMethod(BoundExpression awaiterExpression, SyntaxNode node, TypeSymbol awaitedExpressionType, DiagnosticBag diagnostics, out MethodSymbol getResultMethod, out BoundExpression getAwaiterGetResultCall)
        {
            var awaiterType = awaiterExpression.Type;
            getAwaiterGetResultCall = MakeInvocationExpression(node, awaiterExpression, WellKnownMemberNames.GetResult, ImmutableArray<BoundExpression>.Empty, diagnostics);
            if (getAwaiterGetResultCall.HasAnyErrors)
            {
                getResultMethod = null;
                getAwaiterGetResultCall = null;
                return false;
            }

            if (getAwaiterGetResultCall.Kind != BoundKind.Call)
            {
                Error(diagnostics, ErrorCode.ERR_NoSuchMember, node, awaiterType, WellKnownMemberNames.GetResult);
                getResultMethod = null;
                getAwaiterGetResultCall = null;
                return false;
            }

            getResultMethod = ((BoundCall)getAwaiterGetResultCall).Method;
            if (getResultMethod.IsExtensionMethod)
            {
                Error(diagnostics, ErrorCode.ERR_NoSuchMember, node, awaiterType, WellKnownMemberNames.GetResult);
                getResultMethod = null;
                getAwaiterGetResultCall = null;
                return false;
            }

            if (HasOptionalOrVariableParameters(getResultMethod) || getResultMethod.IsConditional)
            {
                Error(diagnostics, ErrorCode.ERR_BadAwaiterPattern, node, awaiterType, awaitedExpressionType);
                getResultMethod = null;
                getAwaiterGetResultCall = null;
                return false;
            }

            // The lack of a GetResult method will be reported by ValidateGetResult().
            return true;
        }

        private static bool HasOptionalOrVariableParameters(MethodSymbol method)
        {
            Debug.Assert(method != null);

            if (method.ParameterCount != 0)
            {
                var parameter = method.Parameters[method.ParameterCount - 1];
                return parameter.IsOptional || parameter.IsParams;
            }

            return false;
        }
    }
}
