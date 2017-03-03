// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class Binder
    {
        private const int ValueKindInsignificantBits = 3;
        private const BindValueKind ValueKindSignificantBitsMask = unchecked((BindValueKind)~7);

        /// <summary>
        /// Expression requirements.
        /// </summary>
        [Flags]
        internal enum BindValueKind : byte
        {
            /// <summary>
            /// Expression can be an RHS of an assignment operation.
            /// </summary>
            /// <remarks>
            /// The following are rvalues: values, variables, null literals, properties
            /// and indexers with getters, events. 
            /// 
            /// The following are not rvalues:
            /// namespaces, types, method groups, anonymous functions.
            /// </remarks>
            RValue = 1 << ValueKindInsignificantBits,

            /// <summary>
            /// Expression can be the LHS of a simple assignment operation.
            /// </summary>
            Assignment = 2 << ValueKindInsignificantBits,

            /// <summary>
            /// Expression represents a location. 
            /// Examples:
            ///  array index, local variable, parameter, field
            /// </summary>
            RefersToLocation = 4 << ValueKindInsignificantBits,

            /// <summary>
            /// Expression does not refer to data in the local frame
            /// As long as there are no types that can embed references, 
            /// only byref variables can have this bit.
            /// </summary>
            ReturnableReference = 8 << ValueKindInsignificantBits,

            // ====================================
            // ====================================
            
            /// <summary>
            /// Expression is the RHS of an assignment operation
            /// and may be a method group.
            /// Basically an RValue, but could be treated differently for the purpose of error reporting
            /// </summary>
            RValueOrMethodGroup = RValue + 1,

            /// <summary>
            /// Expression can be the LHS of a compound assignment
            /// operation (such as +=).
            /// </summary>
            CompoundAssignment = RValue | Assignment,

            /// <summary>
            /// Expression can be the operand of an increment
            /// or decrement operation.
            /// Same as CompoundAssignment, the distinction is really just for error reporting.
            /// </summary>
            IncrementDecrement = CompoundAssignment + 1,

            /// <summary>
            /// Expression is passed as a ref or out parameter or assigned to a byref variable.
            /// </summary>
            RefOrOut = RefersToLocation | RValue | Assignment,

            /// <summary>
            /// Expression can be the operand of an address-of operation (&amp;).
            /// Same as RefOrOut. The difference is just dor error reporting.
            /// </summary>
            AddressOf = RefOrOut + 1,

            /// <summary>
            /// Expression is the receiver of a fixed buffer field access
            /// Same as RefOrOut. The difference is just dor error reporting.
            /// </summary>
            FixedReceiver = RefOrOut + 2,

            /// <summary>
            /// Expression is returned by a r/o reference.
            /// </summary>
            RefReadonlyReturn = RefersToLocation | ReturnableReference | RValue,

            /// <summary>
            /// Expression is returned by an ordinary r/w reference.
            /// </summary>
            RefReturn = RefersToLocation | ReturnableReference | RValue | Assignment,
        }

        private static bool RequiresRValueOnly(BindValueKind kind)
        {
            return (kind & ValueKindSignificantBitsMask) == BindValueKind.RValue;
        }

        private static bool RequiresVariable(BindValueKind kind)
        {
            switch (kind)
            {
                case BindValueKind.RValue:
                case BindValueKind.RValueOrMethodGroup:
                    return false;

                case BindValueKind.CompoundAssignment:
                case BindValueKind.IncrementDecrement:
                case BindValueKind.RefOrOut:
                case BindValueKind.AddressOf:
                case BindValueKind.Assignment:
                case BindValueKind.ReturnableReference:
                case BindValueKind.RefReturn:
                case BindValueKind.RefersToLocation:
                    return true;

                default:
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }

        private static bool RequiresReferenceToLocation(BindValueKind kind)
        {
            return (kind & BindValueKind.RefersToLocation) != 0;
        }

        private static bool RequiresReturnableReference(BindValueKind kind)
        {
            return (kind & BindValueKind.ReturnableReference) != 0;
        }

        private static bool RequiresAssignableVariable(BindValueKind kind)
        {
            return (kind & BindValueKind.Assignment) != 0;
        }

        private static bool RequiresRefOrOut(BindValueKind kind)
        {
            return (kind & BindValueKind.RefOrOut) == BindValueKind.RefOrOut;
        }

        /// <summary>
        /// Check the expression is of the required lvalue and rvalue specified by valueKind.
        /// The method returns the original expression if the expression is of the required
        /// type. Otherwise, an appropriate error is added to the diagnostics bag and the
        /// method returns a BoundBadExpression node. The method returns the original
        /// expression without generating any error if the expression has errors.
        /// </summary>
        private BoundExpression CheckValue(BoundExpression expr, BindValueKind valueKind, DiagnosticBag diagnostics)
        {
            switch (expr.Kind)
            {
                case BoundKind.PropertyGroup:
                    expr = BindIndexedPropertyAccess((BoundPropertyGroup)expr, mustHaveAllOptionalParameters: false, diagnostics: diagnostics);
                    break;

                case BoundKind.Local:
                    Debug.Assert(expr.Syntax.Kind() != SyntaxKind.Argument || valueKind == BindValueKind.RefOrOut);
                    break;

                case BoundKind.OutVariablePendingInference:
                case BoundKind.OutDeconstructVarPendingInference:
                    Debug.Assert(valueKind == BindValueKind.RefOrOut);
                    return expr;

                case BoundKind.DiscardExpression:
                    Debug.Assert(valueKind == BindValueKind.Assignment || valueKind == BindValueKind.RefOrOut);
                    return expr;
            }

            bool hasResolutionErrors = false;

            // If this a MethodGroup where an rvalue is not expected or where the caller will not explicitly handle
            // (and resolve) MethodGroups (in short, cases where valueKind != BindValueKind.RValueOrMethodGroup),
            // resolve the MethodGroup here to generate the appropriate errors, otherwise resolution errors (such as
            // "member is inaccessible") will be dropped.
            if (expr.Kind == BoundKind.MethodGroup && valueKind != BindValueKind.RValueOrMethodGroup)
            {
                var methodGroup = (BoundMethodGroup)expr;
                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                var resolution = this.ResolveMethodGroup(methodGroup, analyzedArguments: null, isMethodGroupConversion: false, useSiteDiagnostics: ref useSiteDiagnostics);
                diagnostics.Add(expr.Syntax, useSiteDiagnostics);
                Symbol otherSymbol = null;
                bool resolvedToMethodGroup = resolution.MethodGroup != null;
                if (!expr.HasAnyErrors) diagnostics.AddRange(resolution.Diagnostics); // Suppress cascading.
                hasResolutionErrors = resolution.HasAnyErrors;
                if (hasResolutionErrors)
                {
                    otherSymbol = resolution.OtherSymbol;
                }
                resolution.Free();

                // It's possible the method group is not a method group at all, but simply a
                // delayed lookup that resolved to a non-method member (perhaps an inaccessible
                // field or property), or nothing at all. In those cases, the member should not be exposed as a
                // method group, not even within a BoundBadExpression. Instead, the
                // BoundBadExpression simply refers to the receiver and the resolved symbol (if any).
                if (!resolvedToMethodGroup)
                {
                    Debug.Assert(methodGroup.ResultKind != LookupResultKind.Viable);
                    BoundNode receiver = methodGroup.ReceiverOpt;
                    if ((object)otherSymbol != null && receiver?.Kind == BoundKind.TypeOrValueExpression)
                    {
                        // Since we're not accessing a method, this can't be a Color Color case, so TypeOrValueExpression should not have been used.
                        // CAVEAT: otherSymbol could be invalid in some way (e.g. inaccessible), in which case we would have fallen back on a
                        // method group lookup (to allow for extension methods), which would have required a TypeOrValueExpression.
                        Debug.Assert(methodGroup.LookupError != null);

                        // Since we have a concrete member in hand, we can resolve the receiver.
                        var typeOrValue = (BoundTypeOrValueExpression)receiver;
                        receiver = otherSymbol.IsStatic
                            ? null // no receiver required
                            : typeOrValue.Data.ValueExpression;
                    }
                    return new BoundBadExpression(
                        expr.Syntax,
                        methodGroup.ResultKind,
                        (object)otherSymbol == null ? ImmutableArray<Symbol>.Empty : ImmutableArray.Create(otherSymbol),
                        receiver == null ? ImmutableArray<BoundNode>.Empty : ImmutableArray.Create(receiver),
                        GetNonMethodMemberType(otherSymbol));
                }
            }

            if (!hasResolutionErrors && CheckValueKind(expr, valueKind, diagnostics) ||
                expr.HasAnyErrors && valueKind == BindValueKind.RValueOrMethodGroup)
            {
                return expr;
            }

            var resultKind = (valueKind == BindValueKind.RValue || valueKind == BindValueKind.RValueOrMethodGroup) ?
                LookupResultKind.NotAValue :
                LookupResultKind.NotAVariable;

            return ToBadExpression(expr, resultKind);
        }

        internal bool CheckValueKind(BoundExpression expr, BindValueKind valueKind, DiagnosticBag diagnostics)
        {
            if (expr.HasAnyErrors)
            {
                return false;
            }

            switch (expr.Kind)
            {
                case BoundKind.PropertyAccess:
                case BoundKind.IndexerAccess:
                    return CheckPropertyValueKind(expr, valueKind, diagnostics);
                case BoundKind.EventAccess:
                    return CheckEventValueKind((BoundEventAccess)expr, valueKind, diagnostics);
                case BoundKind.DynamicMemberAccess:
                case BoundKind.DynamicIndexerAccess:
                    return true;
                default:
                    {
                        if (RequiresVariable(valueKind))
                        {
                            if (!CheckIsVariable(expr.Syntax, expr, valueKind, false, diagnostics))
                            {
                                return false;
                            }
                        }

                        if (RequiresRValueOnly(valueKind))
                        {
                            if (!CheckNotNamespaceOrType(expr, diagnostics))
                            {
                                return false;
                            }
                        }

                        return true;
                    }
            }
        }

        /// <summary>
        /// The purpose of this method is to determine if the expression is classified by the 
        /// specification as a *variable*. If it is not then this code gives an appropriate error message.
        ///
        /// To determine the appropriate error message we need to know two things:
        ///
        /// (1) why do we want to know if this is a variable? Because we are trying to assign it,
        ///     increment it, or pass it by reference?
        ///
        /// (2) Are we trying to determine if the left hand side of a dot is a variable in order
        ///     to determine if the field or property on the right hand side of a dot is assignable?
        /// </summary>
        private bool CheckIsVariable(SyntaxNode node, BoundExpression expr, BindValueKind kind, bool checkingReceiver, DiagnosticBag diagnostics)
        {
            Debug.Assert(expr != null);
            Debug.Assert(!checkingReceiver || expr.Type.IsValueType || expr.Type.IsTypeParameter());

            // Every expression is classified as one of:
            // 1. a namespace
            // 2. a type
            // 3. an anonymous function
            // 4. a literal
            // 5. an event access
            // 6. a call to a void-returning method
            // 7. a method group
            // 8. a property access
            // 9. an indexer access
            // 10. a variable
            // 11. a value

            // We wish to give an error and return false for all of those except case 10.

            // case 0: We've already reported an error:

            if (expr.HasAnyErrors)
            {
                return false;
            }

            // Case 1: a namespace:
            var ns = expr as BoundNamespaceExpression;
            if (ns != null)
            {
                Error(diagnostics, ErrorCode.ERR_BadSKknown, node, ns.NamespaceSymbol, MessageID.IDS_SK_NAMESPACE.Localize(), MessageID.IDS_SK_VARIABLE.Localize());
                return false;
            }

            // Case 2: a type:
            var type = expr as BoundTypeExpression;
            if (type != null)
            {
                Error(diagnostics, ErrorCode.ERR_BadSKknown, node, type.Type, MessageID.IDS_SK_TYPE.Localize(), MessageID.IDS_SK_VARIABLE.Localize());
                return false;
            }

            // Cases 3, 4, 6:
            if ((expr.Kind == BoundKind.Lambda) ||
                (expr.Kind == BoundKind.UnboundLambda) ||
                (expr.ConstantValue != null) ||
                (expr.Type.GetSpecialTypeSafe() == SpecialType.System_Void))
            {
                Error(diagnostics, GetStandardLvalueError(kind), node);
                return false;
            }

            // Case 5: field-like events are variables

            var eventAccess = expr as BoundEventAccess;
            if (eventAccess != null)
            {
                EventSymbol eventSymbol = eventAccess.EventSymbol;
                if (!eventAccess.IsUsableAsField)
                {
                    Error(diagnostics, GetBadEventUsageDiagnosticInfo(eventSymbol), node);
                    return false;
                }
                else if (eventSymbol.IsWindowsRuntimeEvent)
                {
                    switch (kind)
                    {
                        case BindValueKind.RValue:
                        case BindValueKind.RValueOrMethodGroup:
                            Debug.Assert(false, "Why call CheckIsVariable if you want an RValue?");
                            goto case BindValueKind.Assignment;
                        case BindValueKind.Assignment:
                        case BindValueKind.CompoundAssignment:
                            return true;
                    }

                    // NOTE: Dev11 reports ERR_RefProperty, as if this were a property access (since that's how it will be lowered).
                    // Roslyn reports a new, more specific, error code.
                    Error(diagnostics, kind == BindValueKind.RefOrOut ? ErrorCode.ERR_WinRtEventPassedByRef : GetStandardLvalueError(kind), node, eventSymbol);
                    return false;
                }
                else
                {
                    return true;
                }
            }

            // Case 7: method group gets a nicer error message depending on whether this is M(out F) or F = x.

            var methodGroup = expr as BoundMethodGroup;
            if (methodGroup != null)
            {
                ErrorCode errorCode;
                switch (kind)
                {
                    case BindValueKind.RefOrOut:
                    case BindValueKind.RefReturn:
                        errorCode = ErrorCode.ERR_RefReadonlyLocalCause;
                        break;
                    case BindValueKind.AddressOf:
                        errorCode = ErrorCode.ERR_InvalidAddrOp;
                        break;
                    default:
                        errorCode = ErrorCode.ERR_AssgReadonlyLocalCause;
                        break;
                }
                Error(diagnostics, errorCode, node, methodGroup.Name, MessageID.IDS_MethodGroup.Localize());
                return false;
            }

            // Cases 8 and 9: Properties and indexer accesses are variables iff they return by reference
            //                or the receiver is also a variable. Otherwise, they get special error messages.

            BoundExpression receiver;
            SyntaxNode propertySyntax;
            var propertySymbol = GetPropertySymbol(expr, out receiver, out propertySyntax);
            if ((object)propertySymbol != null)
            {
                if (propertySymbol.RefKind != RefKind.None)
                {
                    return true;
                }
                else if (checkingReceiver)
                {
                    // Error is associated with expression, not node which may be distinct.
                    // This error is reported for all values types. That is a breaking
                    // change from Dev10 which reports this error for struct types only,
                    // not for type parameters constrained to "struct".

                    Debug.Assert((object)propertySymbol.Type != null);
                    Error(diagnostics, ErrorCode.ERR_ReturnNotLValue, expr.Syntax, propertySymbol);
                }
                else
                {
                    Error(diagnostics, kind == BindValueKind.RefOrOut ? ErrorCode.ERR_RefProperty : GetStandardLvalueError(kind), node, propertySymbol);
                }

                return false;
            }

            // That then leaves variables and values. There are several things that look like variables that nevertheless are
            // to be treated as values.

            // The undocumented __refvalue(tr, T) expression results in a variable of type T.
            var refvalue = expr as BoundRefValueOperator;
            if (refvalue != null && !RequiresReturnableReference(kind))
            {
                return true;
            }

            // All parameters are variables 
            // However, only ref and out parameters may be used as writeable or returnable references .
            var parameter = expr as BoundParameter;
            if (parameter != null)
            {
                ParameterSymbol parameterSymbol = parameter.ParameterSymbol;
                var paramKind = parameterSymbol.RefKind;

                // byval parameters are not ref-returnable
                if (RequiresReturnableReference(kind) && parameterSymbol.RefKind == RefKind.None)
                {
                    if (checkingReceiver)
                    {
                        Error(diagnostics, ErrorCode.ERR_RefReturnParameter2, expr.Syntax, parameterSymbol.Name);
                    }
                    else
                    {
                        Error(diagnostics, ErrorCode.ERR_RefReturnParameter, node, parameterSymbol.Name);
                    }
                    return false;
                }

                // all parameters can be passed by ref/out or assigned to
                // except "in" parameters, which are readonly
                if (paramKind == RefKind.RefReadOnly && RequiresAssignableVariable(kind))
                {
                    ReportReadOnlyError(parameterSymbol, node, kind, checkingReceiver, diagnostics);
                }

                if (this.LockedOrDisposedVariables.Contains(parameterSymbol))
                {
                    // Consider: It would be more conventional to pass "symbol" rather than "symbol.Name".
                    // The issue is that the error SymbolDisplayFormat doesn't display parameter
                    // names - only their types - which works great in signatures, but not at all
                    // at the top level.
                    diagnostics.Add(ErrorCode.WRN_AssignmentToLockOrDispose, parameter.Syntax.Location, parameterSymbol.Name);
                }

                return true;
            }


            if (expr is BoundArrayAccess  // Array accesses are always variables
                || expr is BoundPointerIndirectionOperator // Pointer dereferences are always variables
                || expr is BoundPointerElementAccess) // Pointer element access is just sugar for pointer dereference
            {
                return true;
            }

            // Local constants are never variables. Local variables are sometimes
            // not to be treated as variables, if they are fixed, declared in a using, 
            // or declared in a foreach.

            // UNDONE: give good errors for range variables and transparent identifiers

            var local = expr as BoundLocal;
            if (local != null)
            {
                LocalSymbol localSymbol = local.LocalSymbol;
                if (RequiresReturnableReference(kind))
                {
                    if (localSymbol.RefKind == RefKind.None)
                    {
                        if (checkingReceiver)
                        {
                            Error(diagnostics, ErrorCode.ERR_RefReturnLocal2, expr.Syntax, localSymbol);
                        }
                        else
                        {
                            Error(diagnostics, ErrorCode.ERR_RefReturnLocal, node, localSymbol);
                        }

                        return false;
                    }

                    if (!localSymbol.IsReturnable)
                    {
                        if (checkingReceiver)
                        {
                            Error(diagnostics, ErrorCode.ERR_RefReturnNonreturnableLocal2, expr.Syntax, localSymbol);
                        }
                        else
                        {
                            Error(diagnostics, ErrorCode.ERR_RefReturnNonreturnableLocal, node, localSymbol);
                        }
                        return false;
                    }
                }

                //PROTOTYPE(readonlyRefs): should this be triggered only by writeable?
                if (this.LockedOrDisposedVariables.Contains(localSymbol))
                {
                    diagnostics.Add(ErrorCode.WRN_AssignmentToLockOrDispose, local.Syntax.Location, localSymbol);
                }

                return CheckLocalVariable(node, localSymbol, kind, checkingReceiver, diagnostics);
            }

            // SPEC: when this is used in a primary-expression within an instance constructor of a struct, 
            // SPEC: it is classified as a variable. 

            // SPEC: When this is used in a primary-expression within an instance method or instance accessor
            // SPEC: of a struct, it is classified as a variable. 

            var thisref = expr as BoundThisReference;
            if (thisref != null)
            {
                // We will already have given an error for "this" used outside of a constructor, 
                // instance method, or instance accessor. Assume that "this" is a variable if it is in a struct.
                if (!thisref.Type.IsValueType || RequiresReturnableReference(kind))
                {
                    // CONSIDER: the Dev10 name has angle brackets (i.e. "<this>")
                    Error(diagnostics, GetThisLvalueError(kind), node, ThisParameterSymbol.SymbolName);
                    return false;
                }
                return true;
            }

            var queryref = expr as BoundRangeVariable;
            if (queryref != null)
            {
                Error(diagnostics, GetRangeLvalueError(kind), node, queryref.RangeVariableSymbol.Name);
                return false;
            }

            // A field is a variable unless 
            // (1) it is readonly and we are not in a constructor or field initializer
            // (2) the receiver of the field is of value type and is not a variable or object creation expression.
            // For example, if you have a class C with readonly field f of type S, and
            // S has a mutable field x, then c.f.x is not a variable because c.f is not
            // writable.

            var fieldAccess = expr as BoundFieldAccess;
            if (fieldAccess != null)
            {
                // NOTE: only the expression part of a field initializer is bound, not the assignment.
                // As a result, it is okay to see that fields are not variables unless they are in
                // constructors.

                var fieldSymbol = fieldAccess.FieldSymbol;
                var fieldIsStatic = fieldSymbol.IsStatic;

                if (fieldSymbol.IsReadOnly & RequiresAssignableVariable(kind))
                {
                    var canModifyReadonly = false;

                    Symbol containing = this.ContainingMemberOrLambda;
                    if ((object)containing != null &&
                        fieldIsStatic == containing.IsStatic &&
                        (fieldIsStatic || fieldAccess.ReceiverOpt.Kind == BoundKind.ThisReference) &&
                        (Compilation.FeatureStrictEnabled
                            ? fieldSymbol.ContainingType == containing.ContainingType
                            // We duplicate a bug in the native compiler for compatibility in non-strict mode
                            : fieldSymbol.ContainingType.OriginalDefinition == containing.ContainingType.OriginalDefinition))
                    {
                        if (containing.Kind == SymbolKind.Method)
                        {
                            MethodSymbol containingMethod = (MethodSymbol)containing;
                            MethodKind desiredMethodKind = fieldIsStatic ? MethodKind.StaticConstructor : MethodKind.Constructor;
                            canModifyReadonly = containingMethod.MethodKind == desiredMethodKind;
                        }
                        else if (containing.Kind == SymbolKind.Field)
                        {
                            canModifyReadonly = true;
                        }
                    }

                    if (!canModifyReadonly)
                    {
                        ReportReadOnlyError(fieldSymbol, node, kind, checkingReceiver, diagnostics);
                    }
                }

                if (fieldSymbol.IsFixed)
                {
                    Error(diagnostics, GetStandardLvalueError(kind), node);
                    return false;
                }

                if (fieldSymbol.ContainingType.IsValueType &&
                    !fieldIsStatic &&
                    !CheckIsValidReceiverForVariable(node, fieldAccess.ReceiverOpt, kind, diagnostics))
                {
                    return false;
                }

                return true;
            }

            var call = expr as BoundCall;
            if (call != null)
            {
                return CheckIsCallVariable(call, node, kind, checkingReceiver, diagnostics);
            }

            var assign = expr as BoundAssignmentOperator;
            if (assign != null && assign.RefKind != RefKind.None)
            {
                return true;
            }

            // At this point we should have covered all the possible cases for variables.

            if ((expr as BoundConversion)?.ConversionKind == ConversionKind.Unboxing)
            {
                Error(diagnostics, ErrorCode.ERR_UnboxNotLValue, node);
                return false;
            }

            Error(diagnostics, GetStandardLvalueError(kind), node);
            return false;
        }

        private bool CheckIsValidReceiverForVariable(SyntaxNode node, BoundExpression receiver, BindValueKind kind, DiagnosticBag diagnostics)
        {
            Debug.Assert(receiver != null);
            return Flags.Includes(BinderFlags.ObjectInitializerMember) && receiver.Kind == BoundKind.ImplicitReceiver ||
                CheckIsVariable(node, receiver, kind, true, diagnostics);
        }

        private static bool CheckNotNamespaceOrType(BoundExpression expr, DiagnosticBag diagnostics)
        {
            switch (expr.Kind)
            {
                case BoundKind.NamespaceExpression:
                    Error(diagnostics, ErrorCode.ERR_BadSKknown, expr.Syntax, ((BoundNamespaceExpression)expr).NamespaceSymbol, MessageID.IDS_SK_NAMESPACE.Localize(), MessageID.IDS_SK_VARIABLE.Localize());
                    return false;
                case BoundKind.TypeExpression:
                    Error(diagnostics, ErrorCode.ERR_BadSKunknown, expr.Syntax, expr.Type, MessageID.IDS_SK_TYPE.Localize());
                    return false;
                default:
                    return true;
            }
        }

        /// <summary>
        /// SPEC: When a property or indexer declared in a struct-type is the target of an 
        /// SPEC: assignment, the instance expression associated with the property or indexer 
        /// SPEC: access must be classified as a variable. If the instance expression is 
        /// SPEC: classified as a value, a compile-time error occurs. Because of 7.6.4, 
        /// SPEC: the same rule also applies to fields.
        /// </summary>
        /// <remarks>
        /// NOTE: The spec fails to impose the restriction that the event receiver must be classified
        /// as a variable (unlike for properties - 7.17.1).  This seems like a bug, but we have
        /// production code that won't build with the restriction in place (see DevDiv #15674).
        /// </remarks>
        private static bool RequiresVariableReceiver(BoundExpression receiver, Symbol symbol)
        {
            return !symbol.IsStatic
                && symbol.Kind != SymbolKind.Event
                && receiver?.Type?.IsValueType == true;
        }

        private bool CheckPropertyValueKind(BoundExpression expr, BindValueKind valueKind, DiagnosticBag diagnostics)
        {
            // SPEC: If the left operand is a property or indexer access, the property or indexer must
            // SPEC: have a set accessor. If this is not the case, a compile-time error occurs.

            // Addendum: Assignment is also allowed for get-only autoprops in their constructor

            BoundExpression receiver;
            SyntaxNode propertySyntax;
            var propertySymbol = GetPropertySymbol(expr, out receiver, out propertySyntax);

            Debug.Assert((object)propertySymbol != null);
            Debug.Assert(propertySyntax != null);

            var node = expr.Syntax;

            if (RequiresReferenceToLocation(valueKind) && propertySymbol.RefKind == RefKind.None)
            {
                Error(diagnostics, valueKind == BindValueKind.RefOrOut ? ErrorCode.ERR_RefProperty : GetStandardLvalueError(valueKind), node, propertySymbol);
                return false;
            }

            if (RequiresVariable(valueKind) && propertySymbol.RefKind == RefKind.None)
            {
                var setMethod = propertySymbol.GetOwnOrInheritedSetMethod();

                if ((object)setMethod == null)
                {
                    var containing = this.ContainingMemberOrLambda;
                    if (!AccessingAutoPropertyFromConstructor(receiver, propertySymbol, containing))
                    {
                        Error(diagnostics, ErrorCode.ERR_AssgReadonlyProp, node, propertySymbol);
                        return false;
                    }
                }
                else if (receiver?.Kind == BoundKind.BaseReference && setMethod.IsAbstract)
                {
                    Error(diagnostics, ErrorCode.ERR_AbstractBaseCall, node, propertySymbol);
                    return false;
                }
                else if (!object.Equals(setMethod.GetUseSiteDiagnostic(), propertySymbol.GetUseSiteDiagnostic()) && ReportUseSiteDiagnostics(setMethod, diagnostics, propertySyntax))
                {
                    return false;
                }
                else
                {
                    var accessThroughType = this.GetAccessThroughType(receiver);
                    bool failedThroughTypeCheck;
                    HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                    bool isAccessible = this.IsAccessible(setMethod, accessThroughType, out failedThroughTypeCheck, ref useSiteDiagnostics);
                    diagnostics.Add(node, useSiteDiagnostics);

                    if (!isAccessible)
                    {
                        if (failedThroughTypeCheck)
                        {
                            Error(diagnostics, ErrorCode.ERR_BadProtectedAccess, node, propertySymbol, accessThroughType, this.ContainingType);
                        }
                        else
                        {
                            Error(diagnostics, ErrorCode.ERR_InaccessibleSetter, node, propertySymbol);
                        }
                        return false;
                    }

                    ReportDiagnosticsIfObsolete(diagnostics, setMethod, node, receiver?.Kind == BoundKind.BaseReference);

                    if (RequiresVariableReceiver(receiver, setMethod) && !CheckIsValidReceiverForVariable(node, receiver, BindValueKind.Assignment, diagnostics))
                    {
                        return false;
                    }
                }
            }

            var valueSet = valueKind == BindValueKind.Assignment && propertySymbol.RefKind == RefKind.None;

            if (!valueSet)
            {
                var getMethod = propertySymbol.GetOwnOrInheritedGetMethod();

                if ((object)getMethod == null)
                {
                    Error(diagnostics, ErrorCode.ERR_PropertyLacksGet, node, propertySymbol);
                    return false;
                }
                else if (receiver?.Kind == BoundKind.BaseReference && getMethod.IsAbstract)
                {
                    Error(diagnostics, ErrorCode.ERR_AbstractBaseCall, node, propertySymbol);
                    return false;
                }
                else if (!object.Equals(getMethod.GetUseSiteDiagnostic(), propertySymbol.GetUseSiteDiagnostic()) && ReportUseSiteDiagnostics(getMethod, diagnostics, propertySyntax))
                {
                    return false;
                }
                else
                {
                    var accessThroughType = this.GetAccessThroughType(receiver);
                    bool failedThroughTypeCheck;
                    HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                    bool isAccessible = this.IsAccessible(getMethod, accessThroughType, out failedThroughTypeCheck, ref useSiteDiagnostics);
                    diagnostics.Add(node, useSiteDiagnostics);

                    if (!isAccessible)
                    {
                        if (failedThroughTypeCheck)
                        {
                            Error(diagnostics, ErrorCode.ERR_BadProtectedAccess, node, propertySymbol, accessThroughType, this.ContainingType);
                        }
                        else
                        {
                            Error(diagnostics, ErrorCode.ERR_InaccessibleGetter, node, propertySymbol);
                        }
                        return false;
                    }

                    ReportDiagnosticsIfObsolete(diagnostics, getMethod, node, receiver?.Kind == BoundKind.BaseReference);
                }
            }

            return true;
        }

        private bool CheckEventValueKind(BoundEventAccess boundEvent, BindValueKind valueKind, DiagnosticBag diagnostics)
        {
            // Compound assignment (actually "event assignment") is allowed "everywhere", subject to the restrictions of
            // accessibility, use site errors, and receiver variable-ness (for structs).
            // Other operations are allowed only for field-like events and only where the backing field is accessible
            // (i.e. in the declaring type) - subject to use site errors and receiver variable-ness.

            BoundExpression receiver = boundEvent.ReceiverOpt;
            SyntaxNode eventSyntax = GetEventName(boundEvent); //does not include receiver
            EventSymbol eventSymbol = boundEvent.EventSymbol;

            if (valueKind == BindValueKind.CompoundAssignment)
            {
                // NOTE: accessibility has already been checked by lookup.
                // NOTE: availability of well-known members is checked in BindEventAssignment because
                // we don't have the context to determine whether addition or subtraction is being performed.

                if (receiver?.Kind == BoundKind.BaseReference && eventSymbol.IsAbstract)
                {
                    Error(diagnostics, ErrorCode.ERR_AbstractBaseCall, boundEvent.Syntax, eventSymbol);
                    return false;
                }
                else if (ReportUseSiteDiagnostics(eventSymbol, diagnostics, eventSyntax))
                {
                    // NOTE: BindEventAssignment checks use site errors on the specific accessor 
                    // (since we don't know which is being used).
                    return false;
                }

                Debug.Assert(!RequiresVariableReceiver(receiver, eventSymbol));
                return true;
            }
            else
            {
                if (!boundEvent.IsUsableAsField)
                {
                    // Dev10 reports this in addition to ERR_BadAccess, but we won't even reach this point if the event isn't accessible (caught by lookup).
                    Error(diagnostics, GetBadEventUsageDiagnosticInfo(eventSymbol), eventSyntax);
                    return false;
                }
                else if (ReportUseSiteDiagnostics(eventSymbol, diagnostics, eventSyntax))
                {
                    if (RequiresReturnableReference(valueKind) && !CheckIsValidReceiverForVariable(eventSyntax, receiver, valueKind, diagnostics))
                    {
                        return false;
                    }
                    else if (!CheckIsValidReceiverForVariable(eventSyntax, receiver, BindValueKind.Assignment, diagnostics))
                    {
                        return false;
                    }
                }
                else if (RequiresVariable(valueKind))
                {
                    if (eventSymbol.IsWindowsRuntimeEvent && valueKind != BindValueKind.Assignment)
                    {
                        // NOTE: Dev11 reports ERR_RefProperty, as if this were a property access (since that's how it will be lowered).
                        // Roslyn reports a new, more specific, error code.
                        ErrorCode errorCode = valueKind == BindValueKind.RefOrOut ? ErrorCode.ERR_WinRtEventPassedByRef : GetStandardLvalueError(valueKind);
                        Error(diagnostics, errorCode, eventSyntax, eventSymbol);

                        return false;
                    }
                    else if (RequiresVariableReceiver(receiver, eventSymbol.AssociatedField) && // NOTE: using field, not event
                        !CheckIsValidReceiverForVariable(eventSyntax, receiver, BindValueKind.Assignment, diagnostics))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        static private ErrorCode GetThisLvalueError(BindValueKind kind)
        {
            switch (kind)
            {
                default:
                    throw ExceptionUtilities.UnexpectedValue(kind);
                case BindValueKind.CompoundAssignment:
                case BindValueKind.Assignment:
                    return ErrorCode.ERR_AssgReadonlyLocal;
                case BindValueKind.RefOrOut:
                    return ErrorCode.ERR_RefReadonlyLocal;
                case BindValueKind.AddressOf:
                    return ErrorCode.ERR_AddrOnReadOnlyLocal;
                case BindValueKind.IncrementDecrement:
                    return ErrorCode.ERR_IncrementLvalueExpected;
                case BindValueKind.RefReturn:
                case BindValueKind.ReturnableReference:
                    return ErrorCode.ERR_RefReturnStructThis;
            }
        }

        private static ErrorCode GetRangeLvalueError(BindValueKind kind)
        {
            switch (kind)
            {
                case BindValueKind.Assignment:
                case BindValueKind.CompoundAssignment:
                case BindValueKind.IncrementDecrement:
                    return ErrorCode.ERR_QueryRangeVariableReadOnly;
                case BindValueKind.RefOrOut:
                    return ErrorCode.ERR_QueryOutRefRangeVariable;
                case BindValueKind.AddressOf:
                    return ErrorCode.ERR_InvalidAddrOp;
                case BindValueKind.RefReturn:
                case BindValueKind.ReturnableReference:
                    return ErrorCode.ERR_RefReturnRangeVariable;
                default:
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }

        // Check to see if a local symbol is to be treated as a variable. Returns true if yes, reports an
        // error and returns false if no.
        private static bool CheckLocalVariable(SyntaxNode tree, LocalSymbol local, BindValueKind kind, bool checkingReceiver, DiagnosticBag diagnostics)
        {
            Debug.Assert((object)local != null);
            Debug.Assert(kind != BindValueKind.RValue);

            if (local.IsWritable)
            {
                return true;
            }

            MessageID cause;
            if (local.IsForEach)
            {
                cause = MessageID.IDS_FOREACHLOCAL;
            }
            else if (local.IsUsing)
            {
                cause = MessageID.IDS_USINGLOCAL;
            }
            else if (local.IsFixed)
            {
                cause = MessageID.IDS_FIXEDLOCAL;
            }
            else
            {
                Error(diagnostics, GetStandardLvalueError(kind), tree);
                return false;
            }

            if (kind == BindValueKind.AddressOf)
            {
                Error(diagnostics, ErrorCode.ERR_AddrOnReadOnlyLocal, tree);
                return false;
            }

            ErrorCode[] ReadOnlyLocalErrors =
            {
                ErrorCode.ERR_RefReadonlyLocalCause,
                // impossible since readonly locals are never byref, but would be a reasonable error otherwise
                ErrorCode.ERR_RefReadonlyLocalCause,
                ErrorCode.ERR_AssgReadonlyLocalCause,

                ErrorCode.ERR_RefReadonlyLocal2Cause,
                // impossible since readonly locals are never byref, but would be a reasonable error otherwise
                ErrorCode.ERR_RefReadonlyLocal2Cause,
                ErrorCode.ERR_AssgReadonlyLocal2Cause
            };

            int index = (checkingReceiver ? 3 : 0) + (RequiresRefOrOut(kind) ? 0 : (kind == BindValueKind.RefReturn ? 1 : 2));

            Error(diagnostics, ReadOnlyLocalErrors[index], tree, local, cause.Localize());

            return false;
        }

        private bool CheckIsCallVariable(BoundCall call, SyntaxNode node, BindValueKind kind, bool checkingReceiver, DiagnosticBag diagnostics)
        {
            // A call can only be a variable if it returns by reference. If this is the case,
            // whether or not it is a valid variable depends on whether or not the call is the
            // RHS of a return or an assign by reference:
            // - If call is used in a context demanding ref-returnable reference all of its ref
            //   inputs must be ref-returnable

            var methodSymbol = call.Method;
            if (methodSymbol.RefKind != RefKind.None)
            {
                if (RequiresReturnableReference(kind))
                {
                    var args = call.Arguments;
                    var argRefKinds = call.ArgumentRefKindsOpt;
                    if (!argRefKinds.IsDefault)
                    {
                        for (var i = 0; i < args.Length; i++)
                        {
                            if (argRefKinds[i] != RefKind.None && !CheckIsVariable(args[i].Syntax, args[i], kind, false, diagnostics))
                            {
                                var errorCode = checkingReceiver ? ErrorCode.ERR_RefReturnCall2 : ErrorCode.ERR_RefReturnCall;
                                var parameterIndex = call.ArgsToParamsOpt.IsDefault ? i : call.ArgsToParamsOpt[i];
                                var parameterName = methodSymbol.Parameters[parameterIndex].Name;
                                Error(diagnostics, errorCode, call.Syntax, methodSymbol, parameterName);
                                return false;
                            }
                        }
                    }
                }

                return true;
            }

            if (checkingReceiver)
            {
                // Error is associated with expression, not node which may be distinct.
                Error(diagnostics, ErrorCode.ERR_ReturnNotLValue, call.Syntax, methodSymbol);
            }
            else
            {
                Error(diagnostics, GetStandardLvalueError(kind), node);
            }

            return false;
        }

        static private ErrorCode GetStandardLvalueError(BindValueKind kind)
        {
            switch (kind)
            {
                case BindValueKind.CompoundAssignment:
                case BindValueKind.Assignment:
                    return ErrorCode.ERR_AssgLvalueExpected;

                case BindValueKind.RefOrOut:
                    return ErrorCode.ERR_RefLvalueExpected;

                case BindValueKind.AddressOf:
                    return ErrorCode.ERR_InvalidAddrOp;

                case BindValueKind.IncrementDecrement:
                    return ErrorCode.ERR_IncrementLvalueExpected;

                case BindValueKind.FixedReceiver:
                    return ErrorCode.ERR_FixedNeedsLvalue;

                case BindValueKind.RefReturn:
                case BindValueKind.ReturnableReference:
                    return ErrorCode.ERR_RefReturnLvalueExpected;

                default:
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }

        private static void ReportReadOnlyError(FieldSymbol field, SyntaxNode node, BindValueKind kind, bool checkingReceiver, DiagnosticBag diagnostics)
        {
            Debug.Assert((object)field != null);
            Debug.Assert(RequiresAssignableVariable(kind));
            Debug.Assert((object)field.Type != null);

            // It's clearer to say that the address can't be taken than to say that the field can't be modified
            // (even though the latter message gives more explanation of why).
            if (kind == BindValueKind.AddressOf)
            {
                Error(diagnostics, ErrorCode.ERR_InvalidAddrOp, node);
                return;
            }

            ErrorCode[] ReadOnlyErrors =
            {
                ErrorCode.ERR_RefReadonly,
                ErrorCode.ERR_RefReturnReadonly,
                ErrorCode.ERR_AssgReadonly,
                ErrorCode.ERR_RefReadonlyStatic,
                ErrorCode.ERR_RefReturnReadonlyStatic,
                ErrorCode.ERR_AssgReadonlyStatic,
                ErrorCode.ERR_RefReadonly2,
                ErrorCode.ERR_RefReturnReadonly2,
                ErrorCode.ERR_AssgReadonly2,
                ErrorCode.ERR_RefReadonlyStatic2,
                ErrorCode.ERR_RefReturnReadonlyStatic2,
                ErrorCode.ERR_AssgReadonlyStatic2
            };
            int index = (checkingReceiver ? 6 : 0) + (field.IsStatic ? 3 : 0) + (RequiresRefOrOut(kind) ? 0 : (RequiresReturnableReference(kind) ? 1 : 2));
            if (checkingReceiver)
            {
                Error(diagnostics, ReadOnlyErrors[index], node, field);
            }
            else
            {
                Error(diagnostics, ReadOnlyErrors[index], node);
            }
        }

        private static void ReportReadOnlyError(ParameterSymbol parameter, SyntaxNode node, BindValueKind kind, bool checkingReceiver, DiagnosticBag diagnostics)
        {
            Debug.Assert((object)parameter != null);
            Debug.Assert(RequiresAssignableVariable(kind));
            Debug.Assert((object)parameter.Type != null);

            // It's clearer to say that the address can't be taken than to say that the field can't be modified
            // (even though the latter message gives more explanation of why).
            if (kind == BindValueKind.AddressOf)
            {
                Error(diagnostics, ErrorCode.ERR_InvalidAddrOp, node);
                return;
            }

            ErrorCode[] ReadOnlyErrors =
            {
                ErrorCode.ERR_RefReadonlyParam,
                ErrorCode.ERR_RefReturnReadonlyParam,
                ErrorCode.ERR_AssignReadonlyParam,
                ErrorCode.ERR_RefReadonlyParam2,
                ErrorCode.ERR_RefReturnReadonlyParam2,
                ErrorCode.ERR_AssignReadonlyParam2,
            };

            int index = (checkingReceiver ? 3 : 0) + (RequiresRefOrOut(kind) ? 0 : (kind == BindValueKind.RefReturn ? 1 : 2));
            if (checkingReceiver)
            {
                Error(diagnostics, ReadOnlyErrors[index], node, parameter);
            }
            else
            {
                Error(diagnostics, ReadOnlyErrors[index], node);
            }
        }

    }
}
