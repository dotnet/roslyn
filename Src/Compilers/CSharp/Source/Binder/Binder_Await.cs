// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private BoundExpression BindAwait(PrefixUnaryExpressionSyntax node, DiagnosticBag diagnostics)
        {
            BoundExpression expression = BindValue(node.Operand, diagnostics, BindValueKind.RValue);

            return BindAwait(expression, node, diagnostics);
        }

        private BoundExpression BindAwait(BoundExpression expression, CSharpSyntaxNode node, DiagnosticBag diagnostics)
        {
            MethodSymbol getAwaiter = null;
            PropertySymbol isCompleted = null;
            MethodSymbol getResult = null;

            bool hasErrors = false;

            hasErrors =
                ReportBadAwaitWithoutAsync(node, diagnostics) |
                ReportBadAwaitContext(node, diagnostics) |
                GetAwaitableExpressionInfo(expression, ref getAwaiter, ref isCompleted, ref getResult, node, diagnostics);

            // Spec 7.7.7.2:
            // The expression await t is classified the same way as the expression (t).GetAwaiter().GetResult(). Thus,
            // if the return type of GetResult is void, the await-expression is classified as nothing. If it has a
            // non-void return type T, the await-expression is classified as a value of type T.
            TypeSymbol awaitExpressionType = hasErrors ? CreateErrorType()
                : (object)getResult != null ? getResult.ReturnType
                : Compilation.DynamicType;

            return new BoundAwaitExpression(node, expression, getAwaiter, isCompleted, getResult, awaitExpressionType, hasErrors);
        }

        /// <summary>
        /// Return true iff an await with this subexpression would be legal where the expression appears.
        /// </summary>
        bool CouldBeAwaited(BoundExpression expression)
        {
            var containingMethod = this.ContainingMemberOrLambda as MethodSymbol;
            if ((object)containingMethod == null || !containingMethod.IsAsync) return false;
            if (ContextForbidsAwait) return false;
            var fakeDiagnostics = DiagnosticBag.GetInstance();
            var boundAwait = BindAwait(expression, expression.Syntax, fakeDiagnostics);
            fakeDiagnostics.Free();
            return !boundAwait.HasAnyErrors;
        }

        /// <summary>
        /// Assuming we are in an async method, return true if we're in a context where await would be illegal.
        /// Specifically, return true if we're in a lock, catch, or finally.
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
        private bool ReportBadAwaitWithoutAsync(CSharpSyntaxNode node, DiagnosticBag diagnostics)
        {
            if ((object)this.ContainingMemberOrLambda == null || this.ContainingMemberOrLambda.Kind != SymbolKind.Method)
            {
                Error(diagnostics, ErrorCode.ERR_BadAwaitWithoutAsync, node);
                return true;
            }

            var method = (MethodSymbol)this.ContainingMemberOrLambda;
            if (!method.IsAsync)
            {
                if (method.MethodKind == MethodKind.AnonymousFunction)
                {
                    if (method.IsImplicitlyDeclared)
                    {
                        // The await expression occurred in a query expression:
                        Error(diagnostics, ErrorCode.ERR_BadAwaitInQuery, node);
                    }
                    else
                    {
                        var lambda = (LambdaSymbol)method;
                        Error(diagnostics, ErrorCode.ERR_BadAwaitWithoutAsyncLambda, node, lambda.MessageID.Localize());
                    }
                }
                else
                {
                    if (method.ReturnsVoid)
                    {
                        Error(diagnostics, ErrorCode.ERR_BadAwaitWithoutVoidAsyncMethod, node);
                    }
                    else
                    {
                        Error(diagnostics, ErrorCode.ERR_BadAwaitWithoutAsyncMethod, node, method.ReturnType);
                    }
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Report diagnostics if the await expression occurs in an unsafe context.
        /// Errors for await in lock statement, finally block, or catch clause are detected
        /// and reported in the warnings pass.
        /// </summary>
        /// <returns>True if errors were found.</returns>
        private bool ReportBadAwaitContext(CSharpSyntaxNode node, DiagnosticBag diagnostics)
        {
            bool hasErrors = false;

            if (this.InUnsafeRegion)
            {
                Error(diagnostics, ErrorCode.ERR_AwaitInUnsafeContext, node);
                hasErrors = true;
            }
            else if (this.Flags.Includes(BinderFlags.InLockBody))
            {
                Error(diagnostics, ErrorCode.ERR_BadAwaitInLock, node);
                hasErrors = true;
            }
            else if (this.Flags.Includes(BinderFlags.InCatchFilter))
            {
                Error(diagnostics, ErrorCode.ERR_BadAwaitInCatchFilter, node);
                hasErrors = true;
            }

            return hasErrors;
        }

        /// <summary>
        /// Finds and validates the required members of an awaitable expression, as described in spec 7.7.7.1. Returns
        /// true if the expression is awaitable.
        /// </summary>
        /// <returns>True if the expression contains errors.</returns>
        private bool GetAwaitableExpressionInfo(
            BoundExpression expression,
            ref MethodSymbol getAwaiter,
            ref PropertySymbol isCompleted,
            ref MethodSymbol getResult,
            CSharpSyntaxNode node,
            DiagnosticBag diagnostics)
        {
            if (!ValidateAwaitedExpression(expression, node, diagnostics))
            {
                return true;
            }

            if (expression.HasDynamicType())
            {
                return false;
            }

            if (!GetGetAwaiterMethod(expression, node, diagnostics, out getAwaiter) ||
                !ValidateGetAwaiter(getAwaiter, node, diagnostics, expression))
            {
                return true;
            }

            TypeSymbol awaiterType = getAwaiter.ReturnType;

            if (!GetIsCompletedProperty(awaiterType, node, diagnostics, out isCompleted) ||
                !ValidateIsCompleted(isCompleted, node, diagnostics, awaiterType, expression.Type))
            {
                return true;
            }

            if (!AwaiterImplementsINotifyCompletion(awaiterType, node, diagnostics))
            {
                return true;
            }

            if (!GetGetResultMethod(awaiterType, node, diagnostics, out getResult) ||
                !ValidateGetResult(getResult, node, diagnostics, awaiterType, expression.Type))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Validates the awaited expression, returning true if no errors are found.
        /// </summary>
        private static bool ValidateAwaitedExpression(BoundExpression expression, CSharpSyntaxNode node, DiagnosticBag diagnostics)
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
        private bool GetGetAwaiterMethod(BoundExpression expression, CSharpSyntaxNode node, DiagnosticBag diagnostics, out MethodSymbol getAwaiterMethod)
        {
            if (expression.Type.SpecialType == SpecialType.System_Void)
            {
                Error(diagnostics, ErrorCode.ERR_BadAwaitArgVoidCall, node);
                getAwaiterMethod = null;
                return false;
            }

            var getAwaiterCall = MakeInvocationExpression(node, expression, WellKnownMemberNames.GetAwaiter, ImmutableArray<BoundExpression>.Empty, diagnostics);
            var call = getAwaiterCall as BoundCall;
            if (call == null || call.HasAnyErrors && !expression.HasAnyErrors || call.Method is ErrorMethodSymbol)
            {
                getAwaiterMethod = null;
                return false;
            }
            else
            {
                getAwaiterMethod = call.Method;
                return true;
            }
        }

        /// <summary>
        /// Validates the GetAwaiter method, returning true if no errors are found.
        /// </summary>
        private static bool ValidateGetAwaiter(MethodSymbol getAwaiter, CSharpSyntaxNode node, DiagnosticBag diagnostics, BoundExpression expression)
        {
            if ((object)getAwaiter == null ||
                HasOptionalOrVariableParameters(getAwaiter) || // We might have been able to resolve a GetAwaiter overload with optional parameters, so check for that here
                getAwaiter.ReturnsVoid) // If GetAwaiter returns void, don't bother checking that it returns an Awaiter.
            {
                Error(diagnostics, ErrorCode.ERR_BadAwaitArg, node, expression.Type);
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
        private bool GetIsCompletedProperty(TypeSymbol type, CSharpSyntaxNode node, DiagnosticBag diagnostics, out PropertySymbol isCompletedProperty)
        {
            isCompletedProperty = null;
            var receiver = new BoundLiteral(node, ConstantValue.Null, type);
            var name = WellKnownMemberNames.IsCompleted;
            var qualified = BindInstanceMemberAccess(node, node, receiver, name, 0, default(SeparatedSyntaxList<TypeSyntax>), default(ImmutableArray<TypeSymbol>), false, diagnostics);
            if (qualified.Kind == BoundKind.PropertyAccess)
            {
                var property = (BoundPropertyAccess)qualified;
                isCompletedProperty = property.PropertySymbol;
            }

            return !qualified.HasAnyErrors;
        }

        /// <summary>
        /// Validate the IsCompleted property, returning true is no errors are found.
        /// </summary>
        private static bool ValidateIsCompleted(
            PropertySymbol isCompleted,
            CSharpSyntaxNode node,
            DiagnosticBag diagnostics,
            TypeSymbol awaiterType,
            TypeSymbol awaitedExpressionType)
        {
            if ((object)isCompleted == null)
            {
                Error(diagnostics, ErrorCode.ERR_NoSuchMember, node, awaiterType, WellKnownMemberNames.IsCompleted);
                return false;
            }

            Debug.Assert(!isCompleted.IsStatic); // would have been rejected by GetIsCompletedProperty
            if (isCompleted.IsWriteOnly)
            {
                Error(diagnostics, ErrorCode.ERR_PropertyLacksGet, node, isCompleted);
                return false;
            }

            if (isCompleted.Type.SpecialType != SpecialType.System_Boolean)
            {
                Error(diagnostics, ErrorCode.ERR_BadAwaiterPattern, node, awaiterType, awaitedExpressionType);
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
        private bool AwaiterImplementsINotifyCompletion(TypeSymbol awaiterType, CSharpSyntaxNode node, DiagnosticBag diagnostics)
        {
            var INotifyCompletion = GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_INotifyCompletion, diagnostics, node);
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;

            var conversion = this.Conversions.ClassifyImplicitConversion(awaiterType, INotifyCompletion, ref useSiteDiagnostics);
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
        private bool GetGetResultMethod(TypeSymbol type, CSharpSyntaxNode node, DiagnosticBag diagnostics, out MethodSymbol getResultMethod)
        {
            var receiver = new BoundLiteral(node, ConstantValue.Null, type);
            var getResultCall = MakeInvocationExpression(node, receiver, WellKnownMemberNames.GetResult, ImmutableArray<BoundExpression>.Empty, diagnostics);
            getResultMethod = (getResultCall.Kind != BoundKind.Call) ? null : ((BoundCall)getResultCall).Method;
            return !getResultCall.HasAnyErrors;
        }

        /// <summary>
        /// Validate the GetResult method, returning true if there are no errors.
        /// </summary>
        private static bool ValidateGetResult(MethodSymbol getResult, CSharpSyntaxNode node, DiagnosticBag diagnostics, TypeSymbol awaiterType, TypeSymbol awaitedExpressionType)
        {
            if ((object)getResult == null || getResult.IsExtensionMethod)
            {
                Error(diagnostics, ErrorCode.ERR_NoSuchMember, node, awaiterType, WellKnownMemberNames.GetResult);
                return false;
            }

            if (HasOptionalOrVariableParameters(getResult) || getResult.IsConditional)
            {
                Error(diagnostics, ErrorCode.ERR_BadAwaiterPattern, node, awaiterType, awaitedExpressionType);
                return false;
            }

            return true;
        }

        private static bool HasOptionalOrVariableParameters(MethodSymbol method)
        {
            if (method.ParameterCount != 0)
            {
                var parameter = method.Parameters[method.ParameterCount - 1];
                return parameter.IsOptional || parameter.IsParams;
            }

            return false;
        }
    }
}