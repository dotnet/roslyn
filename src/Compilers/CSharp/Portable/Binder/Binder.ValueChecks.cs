// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class Binder
    {
        /// <summary>
        /// constants used to designate escaping scopes.
        /// </summary>
        internal const uint ExternalScope = 0;
        internal const uint TopLevelScope = 1;

        // Some value kinds are semantically the same and the only distinction is how errors are reported
        // for those purposes we reserve lowest 3 bits
        private const int ValueKindInsignificantBits = 3;
        private const BindValueKind ValueKindSignificantBitsMask = unchecked((BindValueKind)~((1 << ValueKindInsignificantBits)-1));

        /// <summary>
        /// Expression capabilities and requirements.
        /// </summary>
        [Flags]
        internal enum BindValueKind : byte
        {
            ///////////////////
            // All expressions can be classified according to the following 4 capabilities:
            //

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
            /// Example: 
            ///   property with a setter
            /// </summary>
            Assignable = 2 << ValueKindInsignificantBits,

            /// <summary>
            /// Expression represents a location. Often referred as a "variable"
            /// Examples:
            ///  local variable, parameter, field
            /// </summary>
            RefersToLocation = 4 << ValueKindInsignificantBits,

            ///////////////////
            // The rest are just combinations of the above.
            //

            /// <summary>
            /// Expression is the RHS of an assignment operation
            /// and may be a method group.
            /// Basically an RValue, but could be treated differently for the purpose of error reporting
            /// </summary>
            RValueOrMethodGroup = RValue + 1,

            /// <summary>
            /// Expression can be an LHS of a compound assignment
            /// operation (such as +=).
            /// </summary>
            CompoundAssignment = RValue | Assignable,

            /// <summary>
            /// Expression can be the operand of an increment or decrement operation.
            /// Same as CompoundAssignment, the distinction is really just for error reporting.
            /// </summary>
            IncrementDecrement = CompoundAssignment + 1,
            
            /// <summary>
            /// Expression is a r/o reference.
            /// </summary>
            ReadonlyRef = RefersToLocation | RValue,

            /// <summary>
            /// Expression is passed as a ref or out parameter or assigned to a byref variable.
            /// </summary>
            RefOrOut = RefersToLocation | RValue | Assignable,

            /// <summary>
            /// Expression can be the operand of an address-of operation (&amp;).
            /// Same as RefOrOut. The difference is just for error reporting.
            /// </summary>
            AddressOf = RefOrOut + 1,

            /// <summary>
            /// Expression is the receiver of a fixed buffer field access
            /// Same as RefOrOut. The difference is just for error reporting.
            /// </summary>
            FixedReceiver = RefOrOut + 2,

            /// <summary>
            /// Expression is returned by an ordinary r/w reference.
            /// Same as RefOrOut. The difference is just for error reporting.
            /// </summary>
            RefReturn = RefOrOut + 3,
        }

        private static bool RequiresRValueOnly(BindValueKind kind)
        {
            return (kind & ValueKindSignificantBitsMask) == BindValueKind.RValue;
        }

        private static bool RequiresAssignmentOnly(BindValueKind kind)
        {
            return (kind & ValueKindSignificantBitsMask) == BindValueKind.Assignable;
        }

        private static bool RequiresVariable(BindValueKind kind)
        {
            return !RequiresRValueOnly(kind);
        }

        private static bool RequiresReferenceToLocation(BindValueKind kind)
        {
            return (kind & BindValueKind.RefersToLocation) != 0;
        }

        private static bool RequiresAssignableVariable(BindValueKind kind)
        {
            return (kind & BindValueKind.Assignable) != 0;
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
                    Debug.Assert(valueKind == BindValueKind.Assignable || valueKind == BindValueKind.RefOrOut || diagnostics.HasAnyResolvedErrors());
                    return expr;

                case BoundKind.IndexerAccess:
                    {
                        // Assigning to an non ref return indexer needs to set 'useSetterForDefaultArgumentGeneration' to true. 
                        // This is for IOperation purpose.
                        var indexerAccess = (BoundIndexerAccess)expr;
                        if (valueKind == BindValueKind.Assignable && !indexerAccess.Indexer.ReturnsByRef)
                        {
                            expr = indexerAccess.Update(indexerAccess.ReceiverOpt,
                               indexerAccess.Indexer,
                               indexerAccess.Arguments,
                               indexerAccess.ArgumentNamesOpt,
                               indexerAccess.ArgumentRefKindsOpt,
                               indexerAccess.Expanded,
                               indexerAccess.ArgsToParamsOpt,
                               indexerAccess.BinderOpt,
                               useSetterForDefaultArgumentGeneration: true,
                               type: indexerAccess.Type);
                        }
                    }
                    break;
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
                    var receiver = methodGroup.ReceiverOpt;
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
                        receiver == null ? ImmutableArray<BoundExpression>.Empty : ImmutableArray.Create(receiver),
                        GetNonMethodMemberType(otherSymbol));
                }
            }

            if (!hasResolutionErrors && CheckValueKind(expr.Syntax, expr, valueKind, checkingReceiver: false, diagnostics: diagnostics) ||
                expr.HasAnyErrors && valueKind == BindValueKind.RValueOrMethodGroup)
            {
                return expr;
            }

            var resultKind = (valueKind == BindValueKind.RValue || valueKind == BindValueKind.RValueOrMethodGroup) ?
                LookupResultKind.NotAValue :
                LookupResultKind.NotAVariable;

            return ToBadExpression(expr, resultKind);
        }

        /// <summary>
        /// The purpose of this method is to determine if the expression satisfies desired capabilities. 
        /// If it is not then this code gives an appropriate error message.
        ///
        /// To determine the appropriate error message we need to know two things:
        ///
        /// (1) What capabilities we need - increment it, assign, return as a readonly reference, . . . ?
        ///
        /// (2) Are we trying to determine if the left hand side of a dot is a variable in order
        ///     to determine if the field or property on the right hand side of a dot is assignable?
        ///     
        /// (3) The syntax of the expression that started the analysis. (for error reporting purposes).
        /// </summary>
        internal bool CheckValueKind(SyntaxNode node, BoundExpression expr, BindValueKind valueKind, bool checkingReceiver, DiagnosticBag diagnostics)
        {
            Debug.Assert(!checkingReceiver || expr.Type.IsValueType || expr.Type.IsTypeParameter());

            if (expr.HasAnyErrors)
            {
                return false;
            }

            switch (expr.Kind)
            {
                // we need to handle properties and event in a special way even in an RValue case because of getters
                case BoundKind.PropertyAccess:
                case BoundKind.IndexerAccess:
                    return CheckPropertyValueKind(node, expr, valueKind, checkingReceiver, diagnostics);

                case BoundKind.EventAccess:
                    return CheckEventValueKind((BoundEventAccess)expr, valueKind, diagnostics);
            }

            // easy out for a very common RValue case.
            if (RequiresRValueOnly(valueKind))
            {
                return CheckNotNamespaceOrType(expr, diagnostics);
            }

            // constants/literals are strictly RValues
            // void is not even an RValue
            if ((expr.ConstantValue != null) || (expr.Type.GetSpecialTypeSafe() == SpecialType.System_Void))
            {
                Error(diagnostics, GetStandardLvalueError(valueKind), node);
                return false;
            }

            switch (expr.Kind)
            {
                case BoundKind.NamespaceExpression:
                    var ns = (BoundNamespaceExpression)expr;
                    Error(diagnostics, ErrorCode.ERR_BadSKknown, node, ns.NamespaceSymbol, MessageID.IDS_SK_NAMESPACE.Localize(), MessageID.IDS_SK_VARIABLE.Localize());
                    return false;

                case BoundKind.TypeExpression:
                    var type = (BoundTypeExpression)expr;
                    Error(diagnostics, ErrorCode.ERR_BadSKknown, node, type.Type, MessageID.IDS_SK_TYPE.Localize(), MessageID.IDS_SK_VARIABLE.Localize());
                    return false;

                case BoundKind.Lambda:
                case BoundKind.UnboundLambda:
                    // lambdas can only be used as RValues
                    Error(diagnostics, GetStandardLvalueError(valueKind), node);
                    return false;

                case BoundKind.MethodGroup:
                    // method groups can only be used as RValues
                    var methodGroup = (BoundMethodGroup)expr;
                    Error(diagnostics, GetMethodGroupLvalueError(valueKind), node, methodGroup.Name, MessageID.IDS_MethodGroup.Localize());
                    return false;

                case BoundKind.RangeVariable:
                    // range variables can only be used as RValues
                    var queryref = (BoundRangeVariable)expr;
                    Error(diagnostics, GetRangeLvalueError(valueKind), node, queryref.RangeVariableSymbol.Name);
                    return false;

                case BoundKind.Conversion:
                    var conversion = (BoundConversion)expr;
                    // conversions are strict RValues, but unboxing has a specific error
                    if (conversion.ConversionKind == ConversionKind.Unboxing)
                    {
                        Error(diagnostics, ErrorCode.ERR_UnboxNotLValue, node);
                        return false;
                    }
                    break;

                case BoundKind.ArrayAccess:
                case BoundKind.PointerIndirectionOperator:
                case BoundKind.PointerElementAccess:
                    // array elements and pointer dereferencing are readwrite varaibles
                    return true;

                case BoundKind.RefValueOperator:
                    // The undocumented __refvalue(tr, T) expression results in a variable of type T.
                    // it is a readwrite variable.
                    return true;

                case BoundKind.DynamicMemberAccess:
                case BoundKind.DynamicIndexerAccess:
                    // dynamic expressions can be read and written to
                    // can even be passed by reference (which is implemented via a temp)
                    return true;

                case BoundKind.Parameter:
                    var parameter = (BoundParameter)expr;
                    return CheckParameterValueKind(node, parameter, valueKind, checkingReceiver, diagnostics);

                case BoundKind.Local:
                    var local = (BoundLocal)expr;
                    return CheckLocalValueKind(node, local, valueKind, checkingReceiver, diagnostics);

                case BoundKind.ThisReference:
                    var thisref = (BoundThisReference)expr;

                    // We will already have given an error for "this" used outside of a constructor, 
                    // instance method, or instance accessor. Assume that "this" is a variable if it is in a struct.

                    // SPEC: when this is used in a primary-expression within an instance constructor of a struct, 
                    // SPEC: it is classified as a variable. 

                    // SPEC: When this is used in a primary-expression within an instance method or instance accessor
                    // SPEC: of a struct, it is classified as a variable. 

                    // Note: RValueOnly is checked at the beginning of this method. Since we are here we need more than readable.
                    //"this" is readonly in members of readonly structs, unless we are in a constructor.
                    if (!thisref.Type.IsValueType ||
                        (thisref.Type.IsReadOnly && (this.ContainingMemberOrLambda as MethodSymbol)?.MethodKind != MethodKind.Constructor))
                    {
                        // CONSIDER: the Dev10 name has angle brackets (i.e. "<this>")
                        Error(diagnostics, GetThisLvalueError(valueKind), node, ThisParameterSymbol.SymbolName);
                        return false;
                    }

                    return true;

                case BoundKind.Call:
                    var call = (BoundCall)expr;
                    return CheckCallValueKind(call, node, valueKind, checkingReceiver, diagnostics);

                case BoundKind.ConditionalOperator:
                    var conditional = (BoundConditionalOperator)expr;

                    // byref conditional defers to its operands
                    if (conditional.IsByRef && 
                        (CheckValueKind(conditional.Consequence.Syntax, conditional.Consequence, valueKind, checkingReceiver: false, diagnostics: diagnostics) &
                        CheckValueKind(conditional.Alternative.Syntax, conditional.Alternative, valueKind, checkingReceiver: false, diagnostics: diagnostics)))
                    {
                        return true;
                    }

                    // reprot standard lvalue error
                    break;
                
                case BoundKind.FieldAccess:
                    var fieldAccess = (BoundFieldAccess)expr;
                    return CheckFieldValueKind(node, fieldAccess, valueKind, checkingReceiver, diagnostics);
            }

            // At this point we should have covered all the possible cases for anything that is not a strict RValue.
            Error(diagnostics, GetStandardLvalueError(valueKind), node);
            return false;
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

        private bool CheckLocalValueKind(SyntaxNode node, BoundLocal local, BindValueKind valueKind, bool checkingReceiver, DiagnosticBag diagnostics)
        {
            // Local constants are never variables. Local variables are sometimes
            // not to be treated as variables, if they are fixed, declared in a using, 
            // or declared in a foreach.

            LocalSymbol localSymbol = local.LocalSymbol;
            if (RequiresAssignableVariable(valueKind))
            {
                if (this.LockedOrDisposedVariables.Contains(localSymbol))
                {
                    diagnostics.Add(ErrorCode.WRN_AssignmentToLockOrDispose, local.Syntax.Location, localSymbol);
                }

                if (!localSymbol.IsWritable)
                { 
                    ReportReadonlyLocalError(node, localSymbol, valueKind, checkingReceiver, diagnostics);
                    return false;
                }
            }

            return true;
        }

        private static bool CheckLocalRefEscape(SyntaxNode node, BoundLocal local, uint escapeTo, bool checkingReceiver, DiagnosticBag diagnostics)
        {
            LocalSymbol localSymbol = local.LocalSymbol;

            if (localSymbol.RefEscapeScope <= escapeTo)
            {
                return true;
            }

            if (escapeTo == Binder.ExternalScope)
            {
                if (localSymbol.RefKind == RefKind.None)
                {
                    if (checkingReceiver)
                    {
                        Error(diagnostics, ErrorCode.ERR_RefReturnLocal2, local.Syntax, localSymbol);
                    }
                    else
                    {
                        Error(diagnostics, ErrorCode.ERR_RefReturnLocal, node, localSymbol);
                    }
                    return false;
                }

                if (checkingReceiver)
                {
                    Error(diagnostics, ErrorCode.ERR_RefReturnNonreturnableLocal2, local.Syntax, localSymbol);
                }
                else
                {
                    Error(diagnostics, ErrorCode.ERR_RefReturnNonreturnableLocal, node, localSymbol);
                }
                return false;
            }

            Error(diagnostics, ErrorCode.ERR_EscapeLocal, node, localSymbol);
            return false;
        }

        private bool CheckParameterValueKind(SyntaxNode node, BoundParameter parameter, BindValueKind valueKind, bool checkingReceiver, DiagnosticBag diagnostics)
        {
            ParameterSymbol parameterSymbol = parameter.ParameterSymbol;
            var paramKind = parameterSymbol.RefKind;

            // all parameters can be passed by ref/out or assigned to
            // except "in" parameters, which are readonly
            if (paramKind == RefKind.RefReadOnly && RequiresAssignableVariable(valueKind))
            {
                ReportReadOnlyError(parameterSymbol, node, valueKind, checkingReceiver, diagnostics);
                return false;
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

        private static bool CheckParameterRefEscape(SyntaxNode node, BoundParameter parameter, uint escapeTo, bool checkingReceiver, DiagnosticBag diagnostics)
        {
            ParameterSymbol parameterSymbol = parameter.ParameterSymbol;
            var paramKind = parameterSymbol.RefKind;

            // byval parameters are not ref-returnable
            if (escapeTo == Binder.ExternalScope && parameterSymbol.RefKind == RefKind.None)
            {
                if (checkingReceiver)
                {
                    Error(diagnostics, ErrorCode.ERR_RefReturnParameter2, parameter.Syntax, parameterSymbol.Name);
                }
                else
                {
                    Error(diagnostics, ErrorCode.ERR_RefReturnParameter, node, parameterSymbol.Name);
                }
                return false;
            }

            // can ref-escape to any scope otherwise
            return true;
        }

        private bool CheckFieldValueKind(SyntaxNode node, BoundFieldAccess fieldAccess, BindValueKind valueKind, bool checkingReceiver, DiagnosticBag diagnostics)
        {
            var fieldSymbol = fieldAccess.FieldSymbol;
            var fieldIsStatic = fieldSymbol.IsStatic;

            if (RequiresAssignableVariable(valueKind))
            {
                // A field is writeable unless 
                // (1) it is readonly and we are not in a constructor or field initializer
                // (2) the receiver of the field is of value type and is not a variable or object creation expression.
                // For example, if you have a class C with readonly field f of type S, and
                // S has a mutable field x, then c.f.x is not a variable because c.f is not
                // writable.

                if (fieldSymbol.IsReadOnly)
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
                        ReportReadOnlyFieldError(fieldSymbol, node, valueKind, checkingReceiver, diagnostics);
                    }
                }

                if (fieldSymbol.IsFixed)
                {
                    Error(diagnostics, GetStandardLvalueError(valueKind), node);
                    return false;
                }
            }

            // r/w fields that are static or belong to reference types are writeable and returnable
            if (fieldIsStatic || fieldSymbol.ContainingType.IsReferenceType)
            {
                return true;
            }

            // for other fields defer to the receiver.
            return CheckIsValidReceiverForVariable(node, fieldAccess.ReceiverOpt, valueKind, diagnostics);
        }

        private static bool CheckFieldRefEscape(SyntaxNode node, BoundFieldAccess fieldAccess, uint escapeFrom, uint escapeTo, bool checkingReceiver, DiagnosticBag diagnostics)
        {
            var fieldSymbol = fieldAccess.FieldSymbol;
            // fields that are static or belong to reference types can ref escape anywhere
            if (fieldSymbol.IsStatic || fieldSymbol.ContainingType.IsReferenceType)
            {
                return true;
            }

            // for other fields defer to the receiver.
            return CheckRefEscape(node, fieldAccess.ReceiverOpt, escapeFrom, escapeTo, checkingReceiver:true, diagnostics: diagnostics);
        }

        private static bool CheckFieldLikeEventRefEscape(SyntaxNode node, BoundEventAccess eventAccess, uint escapeFrom, uint escapeTo, bool checkingReceiver, DiagnosticBag diagnostics)
        {
            var eventSymbol = eventAccess.EventSymbol;

            // field-like events that are static or belong to reference types can ref escape anywhere
            if (eventSymbol.IsStatic || eventSymbol.ContainingType.IsReferenceType)
            {
                return true;
            }

            // for other events defer to the receiver.
            return CheckRefEscape(node, eventAccess.ReceiverOpt, escapeFrom, escapeTo, checkingReceiver: true, diagnostics: diagnostics);
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
                    if (!CheckIsValidReceiverForVariable(eventSyntax, receiver, BindValueKind.Assignable, diagnostics))
                    {
                        return false;
                    }
                }
                else if (RequiresVariable(valueKind))
                {
                    if (eventSymbol.IsWindowsRuntimeEvent && valueKind != BindValueKind.Assignable)
                    {
                        // NOTE: Dev11 reports ERR_RefProperty, as if this were a property access (since that's how it will be lowered).
                        // Roslyn reports a new, more specific, error code.
                        ErrorCode errorCode = valueKind == BindValueKind.RefOrOut ? ErrorCode.ERR_WinRtEventPassedByRef : GetStandardLvalueError(valueKind);
                        Error(diagnostics, errorCode, eventSyntax, eventSymbol);

                        return false;
                    }
                    else if (RequiresVariableReceiver(receiver, eventSymbol.AssociatedField) && // NOTE: using field, not event
                        !CheckIsValidReceiverForVariable(eventSyntax, receiver, valueKind, diagnostics))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        private bool CheckIsValidReceiverForVariable(SyntaxNode node, BoundExpression receiver, BindValueKind kind, DiagnosticBag diagnostics)
        {
            Debug.Assert(receiver != null);
            return Flags.Includes(BinderFlags.ObjectInitializerMember) && receiver.Kind == BoundKind.ImplicitReceiver ||
                CheckValueKind(node, receiver, kind, true, diagnostics);
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

        private bool CheckCallValueKind(BoundCall call, SyntaxNode node, BindValueKind valueKind, bool checkingReceiver, DiagnosticBag diagnostics)
        {
            // A call can only be a variable if it returns by reference. If this is the case,
            // whether or not it is a valid variable depends on whether or not the call is the
            // RHS of a return or an assign by reference:
            // - If call is used in a context demanding ref-returnable reference all of its ref
            //   inputs must be ref-returnable
            var methodSymbol = call.Method;
            var callSyntax = call.Syntax;

            if (RequiresVariable(valueKind) && methodSymbol.RefKind == RefKind.None)
            {
                if (checkingReceiver)
                {
                    // Error is associated with expression, not node which may be distinct.
                    Error(diagnostics, ErrorCode.ERR_ReturnNotLValue, callSyntax, methodSymbol);
                }
                else
                {
                    Error(diagnostics, GetStandardLvalueError(valueKind), node);
                }

                return false;
            }

            if (RequiresAssignableVariable(valueKind) && methodSymbol.RefKind == RefKind.RefReadOnly)
            {
                ReportReadOnlyError(methodSymbol, node, valueKind, checkingReceiver, diagnostics);
                return false;
            }

            return true;
        }

        private bool CheckPropertyValueKind(SyntaxNode node, BoundExpression expr, BindValueKind valueKind, bool checkingReceiver, DiagnosticBag diagnostics)
        {
            // SPEC: If the left operand is a property or indexer access, the property or indexer must
            // SPEC: have a set accessor. If this is not the case, a compile-time error occurs.

            // Addendum: Assignment is also allowed for get-only autoprops in their constructor

            BoundExpression receiver;
            SyntaxNode propertySyntax;
            var propertySymbol = GetPropertySymbol(expr, out receiver, out propertySyntax);

            Debug.Assert((object)propertySymbol != null);
            Debug.Assert(propertySyntax != null);

            if ((RequiresReferenceToLocation(valueKind) || checkingReceiver) &&
                propertySymbol.RefKind == RefKind.None)
            {
                if (checkingReceiver)
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
                    Error(diagnostics, valueKind == BindValueKind.RefOrOut ? ErrorCode.ERR_RefProperty : GetStandardLvalueError(valueKind), node, propertySymbol);
                }

                return false;
            }

            if (RequiresAssignableVariable(valueKind) && propertySymbol.RefKind == RefKind.RefReadOnly)
            {
                ReportReadOnlyError(propertySymbol, node, valueKind, checkingReceiver, diagnostics);
                return false;
            }

            var requiresSet = RequiresAssignableVariable(valueKind) && propertySymbol.RefKind == RefKind.None;
            if (requiresSet)
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

                    if (RequiresVariableReceiver(receiver, setMethod) && !CheckIsValidReceiverForVariable(node, receiver, BindValueKind.Assignable, diagnostics))
                    {
                        return false;
                    }
                }
            }

            var requiresGet = !RequiresAssignmentOnly(valueKind) || propertySymbol.RefKind != RefKind.None;
            if (requiresGet)
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

        private static bool CheckInvocationEscape(
            SyntaxNode syntax,
            Symbol symbol,
            BoundExpression receiverOpt,
            ImmutableArray<ParameterSymbol> parameters,
            ImmutableArray<BoundExpression> args,
            ImmutableArray<RefKind> argRefKinds,
            ImmutableArray<int> argToParamsOpt,
            bool checkingReceiver,
            uint escapeFrom,
            uint escapeTo,
            DiagnosticBag diagnostics)
        {
            ArrayBuilder<bool> inParametersMatchedWithArgs = null;

            // check all arguments that are not passed by value
            if (!args.IsDefault)
            {
                for (var argIndex = 0; argIndex < args.Length; argIndex++)
                {
                    var effectiveRefKind = argRefKinds.IsDefault ? RefKind.None : argRefKinds[argIndex];
                    if (effectiveRefKind == RefKind.None && argIndex < parameters.Length)
                    {
                        var paramIndex = argToParamsOpt.IsDefault ? argIndex : argToParamsOpt[argIndex];
                        if (parameters[paramIndex].RefKind == RefKind.RefReadOnly)
                        {
                            effectiveRefKind = RefKind.RefReadOnly;
                            inParametersMatchedWithArgs = inParametersMatchedWithArgs ?? ArrayBuilder<bool>.GetInstance(parameters.Length, fillWithValue: false);
                            inParametersMatchedWithArgs[paramIndex] = true;
                        }
                    }

                    // if ref is passed, check ref escape, since it is the same or narrower than val escape
                    var argument = args[argIndex];
                    var valid = effectiveRefKind != RefKind.None ?
                                        CheckRefEscape(argument.Syntax, argument, escapeFrom, escapeTo, false, diagnostics) :
                                        CheckValEscape(argument.Syntax, argument, escapeFrom, escapeTo, false, diagnostics);

                    if (!valid)
                    {
                        ErrorCode errorCode = GetStandardCallEscapeError(checkingReceiver);
                        var paramIndex = argToParamsOpt.IsDefault ? argIndex : argToParamsOpt[argIndex];
                        var parameterName = parameters[paramIndex].Name;
                        Error(diagnostics, errorCode, syntax, symbol, parameterName);
                        return false;
                    }
                }
            }

            // handle omitted optional "in" parameters if there are any
            if (!parameters.IsDefault)
            {
                for (int i = 0; i < parameters.Length; i++)
                {
                    var parameter = parameters[i];
                    if (parameter.IsParams)
                    {
                        break;
                    }

                    if (parameter.RefKind == RefKind.RefReadOnly && inParametersMatchedWithArgs?[i] != true)
                    {
                        // unmatched "in" parameter is an RValue 
                        var errorCode = GetStandardCallEscapeError(checkingReceiver);
                        var parameterName = parameters[i].Name;
                        Error(diagnostics, errorCode, syntax, symbol, parameterName);

                        inParametersMatchedWithArgs?.Free();
                        return false;
                    }
                }
            }
            inParametersMatchedWithArgs?.Free();

            // check receiver if ref-like
            if (receiverOpt?.Type?.IsByRefLikeType == true)
            {
                return CheckValEscape(receiverOpt.Syntax, receiverOpt, escapeFrom, escapeTo, false, diagnostics);
            }

            return true;
        }

        private static ErrorCode GetStandardCallEscapeError(bool checkingReceiver)
        {
            return checkingReceiver ? ErrorCode.ERR_EscapeCall2 : ErrorCode.ERR_EscapeCall;
        }

        private static uint GetInvocationEscape(
            BoundExpression receiverOpt,
            ImmutableArray<ParameterSymbol> parameters,
            ImmutableArray<BoundExpression> args,
            ImmutableArray<RefKind> argRefKinds,
            ImmutableArray<int> argToParamsOpt,
            uint scopeOfTheContainingExpression)
        {
            ArrayBuilder<bool> inParametersMatchedWithArgs = null;

            //by default it is safe to escape
            uint escapeScope = Binder.ExternalScope;

            if (!args.IsDefault)
            {
                for (var argIndex = 0; argIndex < args.Length; argIndex++)
                {
                    var effectiveRefKind = argRefKinds.IsDefault ? RefKind.None : argRefKinds[argIndex];
                    if (effectiveRefKind == RefKind.None && argIndex < parameters.Length)
                    {
                        var paramIndex = argToParamsOpt.IsDefault ? argIndex : argToParamsOpt[argIndex];

                        if (parameters[paramIndex].RefKind == RefKind.RefReadOnly)
                        {
                            effectiveRefKind = RefKind.RefReadOnly;
                            inParametersMatchedWithArgs = inParametersMatchedWithArgs ?? ArrayBuilder<bool>.GetInstance(parameters.Length, fillWithValue: false);
                            inParametersMatchedWithArgs[paramIndex] = true;
                        }
                    }

                    // if ref is passed, check ref escape, since it is the same or narrower than val escape
                    var argEscape = effectiveRefKind != RefKind.None ?
                                        GetRefEscape(args[argIndex], scopeOfTheContainingExpression) :
                                        GetValEscape(args[argIndex], scopeOfTheContainingExpression);

                    escapeScope = Math.Max(escapeScope, argEscape);

                    if (escapeScope >= scopeOfTheContainingExpression)
                    {
                        // can't get any worse
                        inParametersMatchedWithArgs?.Free();
                        return escapeScope;
                    }
                }
            }

            // handle omitted optional "in" parameters if there are any
            if (!parameters.IsDefault)
            {
                for(int i = 0; i < parameters.Length; i++)
                {
                    var parameter = parameters[i];
                    if (parameter.IsParams)
                    {
                        break;
                    }

                    if (parameter.RefKind == RefKind.RefReadOnly && inParametersMatchedWithArgs?[i] != true)
                    {
                        // unmatched "in" parameter is an RValue 
                        inParametersMatchedWithArgs?.Free();
                        return scopeOfTheContainingExpression;
                    }
                }
            }

            inParametersMatchedWithArgs?.Free();

            // check receiver if ref-like
            if (receiverOpt?.Type?.IsByRefLikeType == true)
            {
                return GetValEscape(receiverOpt, scopeOfTheContainingExpression);
            }

            return escapeScope;
        }

        private static void ReportReadonlyLocalError(SyntaxNode node, LocalSymbol local, BindValueKind kind, bool checkingReceiver, DiagnosticBag diagnostics)
        {
            Debug.Assert((object)local != null);
            Debug.Assert(kind != BindValueKind.RValue);

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
                Error(diagnostics, GetStandardLvalueError(kind), node);
                return;
            }

            if (kind == BindValueKind.AddressOf)
            {
                Error(diagnostics, ErrorCode.ERR_AddrOnReadOnlyLocal, node);
                return;
            }

            ErrorCode[] ReadOnlyLocalErrors =
            {
                ErrorCode.ERR_RefReadonlyLocalCause,
                ErrorCode.ERR_AssgReadonlyLocalCause,

                ErrorCode.ERR_RefReadonlyLocal2Cause,
                ErrorCode.ERR_AssgReadonlyLocal2Cause
            };

            int index = (checkingReceiver ? 2 : 0) + (RequiresRefOrOut(kind) ? 0 : 1);

            Error(diagnostics, ReadOnlyLocalErrors[index], node, local, cause.Localize());
        }

        static private ErrorCode GetThisLvalueError(BindValueKind kind)
        {
            switch (kind)
            {
                case BindValueKind.CompoundAssignment:
                case BindValueKind.Assignable:
                    return ErrorCode.ERR_AssgReadonlyLocal;

                case BindValueKind.RefOrOut:
                    return ErrorCode.ERR_RefReadonlyLocal;

                case BindValueKind.AddressOf:
                    return ErrorCode.ERR_AddrOnReadOnlyLocal;

                case BindValueKind.IncrementDecrement:
                    return ErrorCode.ERR_IncrementLvalueExpected;

                case BindValueKind.RefReturn:
                case BindValueKind.ReadonlyRef:
                    return ErrorCode.ERR_RefReturnStructThis;
            }

            throw ExceptionUtilities.UnexpectedValue(kind);
        }

        private static ErrorCode GetRangeLvalueError(BindValueKind kind)
        {
            switch (kind)
            {
                case BindValueKind.Assignable:
                case BindValueKind.CompoundAssignment:
                case BindValueKind.IncrementDecrement:
                    return ErrorCode.ERR_QueryRangeVariableReadOnly;

                case BindValueKind.AddressOf:
                    return ErrorCode.ERR_InvalidAddrOp;

                case BindValueKind.RefReturn:
                case BindValueKind.ReadonlyRef:
                    return ErrorCode.ERR_RefReturnRangeVariable;
            }

            if (RequiresReferenceToLocation(kind))
            {
                return ErrorCode.ERR_QueryOutRefRangeVariable;
            }

            throw ExceptionUtilities.UnexpectedValue(kind);
        }

        private static ErrorCode GetMethodGroupLvalueError(BindValueKind valueKind)
        {
            if (valueKind == BindValueKind.AddressOf)
            {
                return ErrorCode.ERR_InvalidAddrOp;
            }

            if (RequiresReferenceToLocation(valueKind))
            {
                return ErrorCode.ERR_RefReadonlyLocalCause;
            }

            // Cannot assign to 'W' because it is a 'method group'
            return ErrorCode.ERR_AssgReadonlyLocalCause;
        }

        static private ErrorCode GetStandardLvalueError(BindValueKind kind)
        {
            switch (kind)
            {
                case BindValueKind.CompoundAssignment:
                case BindValueKind.Assignable:
                    return ErrorCode.ERR_AssgLvalueExpected;

                case BindValueKind.AddressOf:
                    return ErrorCode.ERR_InvalidAddrOp;

                case BindValueKind.IncrementDecrement:
                    return ErrorCode.ERR_IncrementLvalueExpected;

                case BindValueKind.FixedReceiver:
                    return ErrorCode.ERR_FixedNeedsLvalue;

                case BindValueKind.RefReturn:
                case BindValueKind.ReadonlyRef:
                    return ErrorCode.ERR_RefReturnLvalueExpected;
            }

            if (RequiresReferenceToLocation(kind))
            {
                return ErrorCode.ERR_RefLvalueExpected;
            }
            
            throw ExceptionUtilities.UnexpectedValue(kind);
        }

        static private ErrorCode GetStandardRValueRefEscapeError(uint escapeTo)
        {
            if (escapeTo == Binder.ExternalScope)
            { 
                return ErrorCode.ERR_RefReturnLvalueExpected;
            }

            return ErrorCode.ERR_EscapeOther;
        }
        
        private static void ReportReadOnlyFieldError(FieldSymbol field, SyntaxNode node, BindValueKind kind, bool checkingReceiver, DiagnosticBag diagnostics)
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
                ErrorCode.ERR_RefReturnReadonly,
                ErrorCode.ERR_RefReadonly,
                ErrorCode.ERR_AssgReadonly,
                ErrorCode.ERR_RefReturnReadonlyStatic,
                ErrorCode.ERR_RefReadonlyStatic,
                ErrorCode.ERR_AssgReadonlyStatic,
                ErrorCode.ERR_RefReturnReadonly2,
                ErrorCode.ERR_RefReadonly2,
                ErrorCode.ERR_AssgReadonly2,
                ErrorCode.ERR_RefReturnReadonlyStatic2,
                ErrorCode.ERR_RefReadonlyStatic2,
                ErrorCode.ERR_AssgReadonlyStatic2
            };
            int index = (checkingReceiver ? 6 : 0) + (field.IsStatic ? 3 : 0) + (kind == BindValueKind.RefReturn ? 0 : (RequiresRefOrOut(kind) ? 1 : 2));
            if (checkingReceiver)
            {
                Error(diagnostics, ReadOnlyErrors[index], node, field);
            }
            else
            {
                Error(diagnostics, ReadOnlyErrors[index], node);
            }
        }

        private static void ReportReadOnlyError(Symbol symbol, SyntaxNode node, BindValueKind kind, bool checkingReceiver, DiagnosticBag diagnostics)
        {
            Debug.Assert((object)symbol != null);
            Debug.Assert(RequiresAssignableVariable(kind));

            // It's clearer to say that the address can't be taken than to say that the parameter can't be modified
            // (even though the latter message gives more explanation of why).
            if (kind == BindValueKind.AddressOf)
            {
                Error(diagnostics, ErrorCode.ERR_InvalidAddrOp, node);
                return;
            }

            var symbolKind = symbol.Kind.Localize();

            ErrorCode[] ReadOnlyErrors =
            {
                ErrorCode.ERR_RefReturnReadonlyNotField,
                ErrorCode.ERR_RefReadonlyNotField,
                ErrorCode.ERR_AssignReadonlyNotField,
                ErrorCode.ERR_RefReturnReadonlyNotField2,
                ErrorCode.ERR_RefReadonlyNotField2,
                ErrorCode.ERR_AssignReadonlyNotField2,
            };

            int index = (checkingReceiver ? 3 : 0) + (kind == BindValueKind.RefReturn ? 0 : (RequiresRefOrOut(kind) ? 1 : 2));
            Error(diagnostics, ReadOnlyErrors[index], node, symbolKind, symbol);
        }

        internal BoundExpression ValidateEscape(BoundExpression expr, uint escapeTo, bool isByRef, DiagnosticBag diagnostics)
        {
            if (isByRef)
            {
                if (CheckRefEscape(expr.Syntax, expr, this.LocalScopeDepth, escapeTo, checkingReceiver: false, diagnostics: diagnostics))
                {
                    return expr;
                }
            }
            else
            {
                if (CheckValEscape(expr.Syntax, expr, this.LocalScopeDepth, escapeTo, checkingReceiver: false, diagnostics: diagnostics))
                {
                    return expr;
                }
            }

            return ToBadExpression(expr);
        }

        internal static bool CheckRefEscape(SyntaxNode node, BoundExpression expr, uint escapeFrom, uint escapeTo, bool checkingReceiver, DiagnosticBag diagnostics)
        {
            Debug.Assert(!checkingReceiver || expr.Type.IsValueType || expr.Type.IsTypeParameter());

            if (escapeTo >= escapeFrom)
            {
                // escaping to same or narrower scope is ok.
                return true;
            }

            if (expr.HasAnyErrors)
            {
                // already an error
                return true;
            }

            // void references cannot escape (error should be reported somwhere)
            if (expr.Type?.GetSpecialTypeSafe() == SpecialType.System_Void)
            {
                return true;
            }

            // references to constants/literals cannot escape higher.
            if (expr.ConstantValue != null)
            {
                Error(diagnostics, GetStandardRValueRefEscapeError(escapeTo), node);
                return false;
            }

            switch (expr.Kind)
            {
                case BoundKind.ArrayAccess:
                case BoundKind.PointerIndirectionOperator:
                case BoundKind.PointerElementAccess:
                    // array elements and pointer dereferencing are readwrite varaibles
                    return true;

                case BoundKind.RefValueOperator:
                    // The undocumented __refvalue(tr, T) expression results in a variable of type T.
                    // it is a readwrite variable, but could refer to unknown local data
                    break;

                case BoundKind.DynamicMemberAccess:
                case BoundKind.DynamicIndexerAccess:
                    // dynamic expressions can be read and written to
                    // can even be passed by reference (which is implemented via a temp)
                    // it is not valid to escape them by reference though.
                    break;

                case BoundKind.Parameter:
                    var parameter = (BoundParameter)expr;
                    return CheckParameterRefEscape(node, parameter, escapeTo, checkingReceiver, diagnostics);

                case BoundKind.Local:
                    var local = (BoundLocal)expr;
                    return CheckLocalRefEscape(node, local, escapeTo, checkingReceiver, diagnostics);

                case BoundKind.ThisReference:
                    var thisref = (BoundThisReference)expr;

                    // "this" is an RValue, unless in a struct.
                    if (!thisref.Type.IsValueType)
                    {
                        break;
                    }                        

                    //"this" is not returnable by reference in a struct.
                    if (escapeTo == Binder.ExternalScope)
                    {
                        Error(diagnostics, ErrorCode.ERR_RefReturnStructThis, node, ThisParameterSymbol.SymbolName);
                    }

                    // can ref escape to any other level
                    return true;

                case BoundKind.ConditionalOperator:
                    var conditional = (BoundConditionalOperator)expr;

                    // byref conditional defers to its operands
                    if (conditional.IsByRef &&
                        (CheckRefEscape(conditional.Consequence.Syntax, conditional.Consequence, escapeFrom, escapeTo, checkingReceiver: false, diagnostics: diagnostics) &&
                         CheckRefEscape(conditional.Alternative.Syntax, conditional.Alternative, escapeFrom, escapeTo, checkingReceiver: false, diagnostics: diagnostics)))
                    {
                        return true;
                    }

                    // reprot standard lvalue error
                    break;

                case BoundKind.FieldAccess:
                    var fieldAccess = (BoundFieldAccess)expr;
                    return CheckFieldRefEscape(node, fieldAccess, escapeFrom, escapeTo, checkingReceiver, diagnostics);

                case BoundKind.EventAccess:
                    var eventAccess = (BoundEventAccess)expr;
                    if (!eventAccess.IsUsableAsField)
                    {
                        // not field-like events are RValues
                        break;
                    }

                    return CheckFieldLikeEventRefEscape(node, eventAccess, escapeFrom, escapeTo, checkingReceiver, diagnostics);

                case BoundKind.Call:
                    var call = (BoundCall)expr;

                    var methodSymbol = call.Method;
                    if( methodSymbol.RefKind == RefKind.None)
                    {
                        break;
                    }

                    return CheckInvocationEscape(
                        call.Syntax,
                        methodSymbol,
                        call.ReceiverOpt,
                        methodSymbol.Parameters,
                        call.Arguments,
                        call.ArgumentRefKindsOpt,
                        call.ArgsToParamsOpt,
                        checkingReceiver,
                        escapeFrom,
                        escapeTo,
                        diagnostics);

                case BoundKind.IndexerAccess:
                    var indexerAccess = (BoundIndexerAccess)expr;
                    var indexerSymbol = indexerAccess.Indexer;

                    if (indexerSymbol.RefKind == RefKind.None)
                    {
                        break;
                    }

                    return CheckInvocationEscape(
                        indexerAccess.Syntax,
                        indexerSymbol,
                        indexerAccess.ReceiverOpt,
                        indexerSymbol.Parameters,
                        indexerAccess.Arguments,
                        indexerAccess.ArgumentRefKindsOpt,
                        indexerAccess.ArgsToParamsOpt,
                        checkingReceiver,
                        escapeFrom,
                        escapeTo,
                        diagnostics);

                case BoundKind.PropertyAccess:
                    var propertyAccess = (BoundPropertyAccess)expr;
                    var propertySymbol = propertyAccess.PropertySymbol;

                    if (propertySymbol.RefKind == RefKind.None)
                    {
                        break;
                    }

                    // not passing any arguments/parameters
                    return CheckInvocationEscape(
                        propertyAccess.Syntax,
                        propertySymbol,
                        propertyAccess.ReceiverOpt,
                        default,
                        default,
                        default,
                        default,
                        checkingReceiver,
                        escapeFrom,
                        escapeTo,
                        diagnostics);
            }

            // At this point we should have covered all the possible cases for anything that is not a strict RValue.
            Error(diagnostics, GetStandardRValueRefEscapeError(escapeTo), node);
            return false;
        }

        /// <summary>
        /// generally inferred from parts.
        /// RValues get the current level
        /// </summary>
        internal static uint GetRefEscape(BoundExpression expr, uint scopeOfTheContainingExpression)
        {
            // cannot infer anything from errors
            if (expr.HasAnyErrors)
            {
                return Binder.ExternalScope;
            }

            // cannot infer anything from Void (broken code)
            if (expr.Type?.GetSpecialTypeSafe() == SpecialType.System_Void)
            {
                return Binder.ExternalScope;
            }

            // constants/literals cannot ref-escape current scope
            if (expr.ConstantValue != null)
            {
                return scopeOfTheContainingExpression;
            }

            // cover case that cannot refer to local state
            // otherwise default to current scope (RValues, etc)
            switch (expr.Kind)
            {
                case BoundKind.ArrayAccess:
                case BoundKind.PointerIndirectionOperator:
                case BoundKind.PointerElementAccess:
                    // array elements and pointer dereferencing are readwrite varaibles
                    return Binder.ExternalScope;

                case BoundKind.RefValueOperator:
                    // The undocumented __refvalue(tr, T) expression results in a variable of type T.
                    // it is a readwrite variable, but could refer to unknown local data
                    // we will constrain it to the current scope
                    return scopeOfTheContainingExpression;

                case BoundKind.DynamicMemberAccess:
                case BoundKind.DynamicIndexerAccess:
                    // dynamic expressions can be read and written to
                    // can even be passed by reference (which is implemented via a temp)
                    // it is not valid to escape them by reference though, so treat them as RValues here
                    break;

                case BoundKind.Parameter:
                    var parameter = ((BoundParameter)expr).ParameterSymbol;

                    // byval parameters can escape to method's top level.
                    // others can be escape further.
                    return parameter.RefKind == RefKind.None ?
                        Binder.TopLevelScope :
                        Binder.ExternalScope;

                case BoundKind.Local:
                    return ((BoundLocal)expr).LocalSymbol.RefEscapeScope;

                case BoundKind.ThisReference:
                    var thisref = (BoundThisReference)expr;

                    // "this" is an RValue, unless in a struct.
                    if (!thisref.Type.IsValueType)
                    {
                        break;
                    }

                    //"this" is not returnable by reference in a struct.
                    // can ref escape to any other level
                    return Binder.TopLevelScope;

                case BoundKind.ConditionalOperator:
                    var conditional = (BoundConditionalOperator)expr;

                    // byref conditional defers to its operands
                    if (conditional.IsByRef)
                    {
                        return Math.Max(GetRefEscape(conditional.Consequence, scopeOfTheContainingExpression),
                                        GetRefEscape(conditional.Alternative, scopeOfTheContainingExpression));
                    }

                    // report standard lvalue error
                    break;

                case BoundKind.FieldAccess:
                    var fieldAccess = (BoundFieldAccess)expr;
                    var fieldSymbol = fieldAccess.FieldSymbol;

                    // fields that are static or belong to reference types can ref escape anywhere
                    if (fieldSymbol.IsStatic || fieldSymbol.ContainingType.IsReferenceType)
                    {
                        return Binder.ExternalScope;
                    }

                    // for other fields defer to the receiver.
                    return GetRefEscape(fieldAccess.ReceiverOpt, scopeOfTheContainingExpression);

                case BoundKind.EventAccess:
                    var eventAccess = (BoundEventAccess)expr;
                    if (!eventAccess.IsUsableAsField)
                    {
                        // not field-like events are RValues
                        break;
                    }

                    var eventSymbol = eventAccess.EventSymbol;

                    // field-like events that are static or belong to reference types can ref escape anywhere
                    if (eventSymbol.IsStatic || eventSymbol.ContainingType.IsReferenceType)
                    {
                        return Binder.ExternalScope;
                    }

                    // for other events defer to the receiver.
                    return GetRefEscape(eventAccess.ReceiverOpt, scopeOfTheContainingExpression);

                case BoundKind.Call:
                    var call = (BoundCall)expr;

                    var methodSymbol = call.Method;
                    if (methodSymbol.RefKind == RefKind.None)
                    {
                        break;
                    }

                    return GetInvocationEscape(
                        call.ReceiverOpt,
                        methodSymbol.Parameters,
                        call.Arguments,
                        call.ArgumentRefKindsOpt,
                        call.ArgsToParamsOpt,
                        scopeOfTheContainingExpression);

                case BoundKind.IndexerAccess:
                    var indexerAccess = (BoundIndexerAccess)expr;
                    var indexerSymbol = indexerAccess.Indexer;

                    return GetInvocationEscape(
                        indexerAccess.ReceiverOpt,
                        indexerSymbol.Parameters,
                        indexerAccess.Arguments,
                        indexerAccess.ArgumentRefKindsOpt,
                        indexerAccess.ArgsToParamsOpt,
                        scopeOfTheContainingExpression);

                case BoundKind.PropertyAccess:
                    var propertyAccess = (BoundPropertyAccess)expr;
                    var propertySymbol = propertyAccess.PropertySymbol;

                    // not passing any arguments/parameters
                    return GetInvocationEscape(
                        propertyAccess.ReceiverOpt,
                        default,
                        default,
                        default,
                        default,
                        scopeOfTheContainingExpression);
            }

            // At this point we should have covered all the possible cases for anything that is not a strict RValue.
            return scopeOfTheContainingExpression;
        }

        internal static bool CheckValEscape(SyntaxNode node, BoundExpression expr, uint escapeFrom, uint escapeTo, bool checkingReceiver, DiagnosticBag diagnostics)
        {
            Debug.Assert(!checkingReceiver || expr.Type.IsValueType || expr.Type.IsTypeParameter());

            if (escapeTo >= escapeFrom)
            {
                // escaping to same or narrower scope is ok.
                return true;
            }

            // cannot infer anything from errors
            if (expr.HasAnyErrors)
            {
                return true;
            }

            // constants/literals cannot refer to local state
            if (expr.ConstantValue != null)
            {
                return true;
            }

            // to have local-referring values an expression must have a ref-like type
            if (expr.Type?.IsByRefLikeType != true)
            {
                return true;
            }

            // cover case that can refer to local state
            // otherwise default to ExternalScope (ordinary values)
            switch (expr.Kind)
            {
                case BoundKind.DefaultExpression:
                case BoundKind.Parameter:
                    return true;

                case BoundKind.Local:
                    var localSymbol = ((BoundLocal)expr).LocalSymbol;
                    if (localSymbol.ValEscapeScope > escapeTo)
                    {
                        Error(diagnostics, ErrorCode.ERR_EscapeLocal, node, localSymbol);
                    }
                    return true;

                case BoundKind.ConditionalOperator:
                    var conditional = (BoundConditionalOperator)expr;

                    return CheckValEscape(conditional.Consequence.Syntax, conditional.Consequence, escapeFrom, escapeTo, checkingReceiver: false, diagnostics: diagnostics) &&
                           CheckValEscape(conditional.Alternative.Syntax, conditional.Alternative, escapeFrom, escapeTo, checkingReceiver: false, diagnostics: diagnostics);

                case BoundKind.FieldAccess:
                    var fieldAccess = (BoundFieldAccess)expr;
                    var fieldSymbol = fieldAccess.FieldSymbol;

                    Debug.Assert(!fieldSymbol.IsStatic && fieldSymbol.ContainingType.IsByRefLikeType);

                    // for ref-like fields defer to the receiver.
                    return CheckValEscape(node, fieldAccess.ReceiverOpt, escapeFrom, escapeTo, true, diagnostics);

                case BoundKind.Call:
                    var call = (BoundCall)expr;
                    var methodSymbol = call.Method;

                    return CheckInvocationEscape(
                        call.Syntax,
                        methodSymbol,
                        call.ReceiverOpt,
                        methodSymbol.Parameters,
                        call.Arguments,
                        call.ArgumentRefKindsOpt,
                        call.ArgsToParamsOpt,
                        checkingReceiver,
                        escapeFrom,
                        escapeTo,
                        diagnostics);

                case BoundKind.IndexerAccess:
                    var indexerAccess = (BoundIndexerAccess)expr;
                    var indexerSymbol = indexerAccess.Indexer;

                    return CheckInvocationEscape(
                        indexerAccess.Syntax,
                        indexerSymbol,
                        indexerAccess.ReceiverOpt,
                        indexerSymbol.Parameters,
                        indexerAccess.Arguments,
                        indexerAccess.ArgumentRefKindsOpt,
                        indexerAccess.ArgsToParamsOpt,
                        checkingReceiver,
                        escapeFrom,
                        escapeTo,
                        diagnostics);

                case BoundKind.PropertyAccess:
                    var propertyAccess = (BoundPropertyAccess)expr;
                    var propertySymbol = propertyAccess.PropertySymbol;

                    // not passing any arguments/parameters
                    return CheckInvocationEscape(
                        propertyAccess.Syntax,
                        propertySymbol,
                        propertyAccess.ReceiverOpt,
                        default,
                        default,
                        default,
                        default,
                        checkingReceiver,
                        escapeFrom,
                        escapeTo,
                        diagnostics);

                case BoundKind.ObjectCreationExpression:
                    var objectCreation = (BoundObjectCreationExpression)expr;
                    var constructorSymbol = objectCreation.Constructor;

                    var escape = CheckInvocationEscape(
                        objectCreation.Syntax,
                        constructorSymbol,
                        null,
                        constructorSymbol.Parameters,
                        objectCreation.Arguments,
                        objectCreation.ArgumentRefKindsOpt,
                        objectCreation.ArgsToParamsOpt,
                        checkingReceiver,
                        escapeFrom,
                        escapeTo,
                        diagnostics);

                    //PROTOTYPE(span):  handle object initializers. (no neeed to worry about collection init, the obj must be IEnumerable)
                    return escape;

                //PROTOTYPE(span): add tests for the following cases

                case BoundKind.UnaryOperator:
                    var unary = (BoundUnaryOperator)expr;
                    return CheckValEscape(node, unary.Operand, escapeFrom, escapeTo, checkingReceiver: false, diagnostics: diagnostics);

                case BoundKind.Conversion:
                    var conversion = (BoundConversion)expr;
                    return CheckValEscape(node, conversion.Operand, escapeFrom, escapeTo, checkingReceiver: false, diagnostics: diagnostics);

                case BoundKind.AssignmentOperator:
                    var assignment = (BoundAssignmentOperator)expr;
                    return CheckValEscape(node, assignment.Right, escapeFrom, escapeTo, checkingReceiver: false, diagnostics: diagnostics);

                case BoundKind.IncrementOperator:
                    var increment = (BoundIncrementOperator)expr;
                    return CheckValEscape(node, increment.Operand, escapeFrom, escapeTo, checkingReceiver: false, diagnostics: diagnostics);

                case BoundKind.CompoundAssignmentOperator:
                    var compound = (BoundCompoundAssignmentOperator)expr;

                    return CheckValEscape(compound.Left.Syntax, compound.Left, escapeFrom, escapeTo, checkingReceiver: false, diagnostics: diagnostics) &&
                           CheckValEscape(compound.Right.Syntax, compound.Right, escapeFrom, escapeTo, checkingReceiver: false, diagnostics: diagnostics);

                case BoundKind.BinaryOperator:
                    var binary = (BoundBinaryOperator)expr;

                    return CheckValEscape(binary.Left.Syntax, binary.Left, escapeFrom, escapeTo, checkingReceiver: false, diagnostics: diagnostics) &&
                           CheckValEscape(binary.Right.Syntax, binary.Right, escapeFrom, escapeTo, checkingReceiver: false, diagnostics: diagnostics);

                    //TODO: VS __refvalue. It is not possible to __makeref(span), but it is allowed to do __refvalue(tr, Span), perhaps it shoudl not be allowed, looks like a bug.

                    //PROTOTYPE(span): handle remaining expressions with operands - ...
                    //      Note that requirement that the result is ref-like makes many cases, like array access or tuple literals not applicable.
            }

            // At this point we should have covered all the possible cases for anything that may return its operands.
            return true;
        }

        internal static uint GetValEscape(BoundExpression expr, uint scopeOfTheContainingExpression)
        {
            // cannot infer anything from errors
            if (expr.HasAnyErrors)
            {
                return Binder.ExternalScope;
            }

            // constants/literals cannot refer to local state
            if (expr.ConstantValue != null)
            {
                return Binder.ExternalScope;
            }

            // to have local-referring values an expression must have a ref-like type
            if (expr.Type?.IsByRefLikeType != true)
            {
                return Binder.ExternalScope;
            }

            // cover case that can refer to local state
            // otherwise default to ExternalScope (ordinary values)
            switch (expr.Kind)
            {
                case BoundKind.DefaultExpression:
                case BoundKind.Parameter:
                    return Binder.ExternalScope;

                case BoundKind.Local:
                    return ((BoundLocal)expr).LocalSymbol.ValEscapeScope;

                case BoundKind.ConditionalOperator:
                    var conditional = (BoundConditionalOperator)expr;

                    // conditional defers to its operands
                    return Math.Max(GetValEscape(conditional.Consequence, scopeOfTheContainingExpression),
                                    GetValEscape(conditional.Alternative, scopeOfTheContainingExpression));

                case BoundKind.FieldAccess:
                    var fieldAccess = (BoundFieldAccess)expr;
                    var fieldSymbol = fieldAccess.FieldSymbol;

                    Debug.Assert(!fieldSymbol.IsStatic && fieldSymbol.ContainingType.IsByRefLikeType);

                    // for ref-like fields defer to the receiver.
                    return GetRefEscape(fieldAccess.ReceiverOpt, scopeOfTheContainingExpression);

                case BoundKind.Call:
                    var call = (BoundCall)expr;
                    var methodSymbol = call.Method;

                    return GetInvocationEscape(
                        call.ReceiverOpt,
                        methodSymbol.Parameters,
                        call.Arguments,
                        call.ArgumentRefKindsOpt,
                        call.ArgsToParamsOpt,
                        scopeOfTheContainingExpression);

                case BoundKind.IndexerAccess:
                    var indexerAccess = (BoundIndexerAccess)expr;
                    var indexerSymbol = indexerAccess.Indexer;

                    return GetInvocationEscape(
                        indexerAccess.ReceiverOpt,
                        indexerSymbol.Parameters,
                        indexerAccess.Arguments,
                        indexerAccess.ArgumentRefKindsOpt,
                        indexerAccess.ArgsToParamsOpt,
                        scopeOfTheContainingExpression);

                case BoundKind.PropertyAccess:
                    var propertyAccess = (BoundPropertyAccess)expr;
                    var propertySymbol = propertyAccess.PropertySymbol;

                    // not passing any arguments/parameters
                    return GetInvocationEscape(
                        propertyAccess.ReceiverOpt,
                        default,
                        default,
                        default,
                        default,
                        scopeOfTheContainingExpression);

                case BoundKind.ObjectCreationExpression:
                    var objectCreation = (BoundObjectCreationExpression)expr;
                    var constructorSymbol = objectCreation.Constructor;

                    var escape = GetInvocationEscape(
                        null,
                        constructorSymbol.Parameters,
                        objectCreation.Arguments,
                        objectCreation.ArgumentRefKindsOpt,
                        objectCreation.ArgsToParamsOpt,
                        scopeOfTheContainingExpression);

                    //PROTOTYPE(span): handle object initializers. (no neeed to worry about collection init, the obj must be IEnumerable)
                    return escape;

                //PROTOTYPE(span): add tests for the following cases

                case BoundKind.UnaryOperator:
                    return GetValEscape(((BoundUnaryOperator)expr).Operand, scopeOfTheContainingExpression);

                case BoundKind.Conversion:
                    return GetValEscape(((BoundConversion)expr).Operand, scopeOfTheContainingExpression);

                case BoundKind.AssignmentOperator:
                    return GetValEscape(((BoundAssignmentOperator)expr).Right, scopeOfTheContainingExpression);

                case BoundKind.IncrementOperator:
                    return GetValEscape(((BoundIncrementOperator)expr).Operand, scopeOfTheContainingExpression);

                case BoundKind.CompoundAssignmentOperator:
                    var compound = (BoundCompoundAssignmentOperator)expr;

                    return Math.Max(GetValEscape(compound.Left, scopeOfTheContainingExpression),
                                    GetValEscape(compound.Right, scopeOfTheContainingExpression));

                case BoundKind.BinaryOperator:
                    var binary = (BoundBinaryOperator)expr;

                    return Math.Max(GetValEscape(binary.Left, scopeOfTheContainingExpression),
                                    GetValEscape(binary.Right, scopeOfTheContainingExpression));

                    //TODO: VS __refvalue. It is not possible to __makeref(span), but it is allowed to do __refvalue(tr, Span), perhaps it shoudl not be allowed, looks like a bug.

                    //PROTOTYPE(span): handle remaining expressions with operands - ...
                    //      Note that requirement that the result is ref-like makes many cases, like array access or tuple literals not applicable.
            }

            // At this point we should have covered all the possible cases for anything that may return its operands.
            return Binder.ExternalScope;
        }

    }
}
