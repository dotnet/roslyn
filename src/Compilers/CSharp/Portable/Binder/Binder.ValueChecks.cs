// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class RefSafetyAnalysis
    {
        private enum EscapeLevel : uint
        {
            CallingMethod = CallingMethodScope,
            ReturnOnly = ReturnOnlyScope,
        }

        /// <summary>
        /// The destination in a method arguments must match (MAMM) check. This is 
        /// created primarily for ref and out arguments of a ref struct. It also applies
        /// to function pointer this and arglist arguments.
        /// </summary>
        private readonly struct MixableDestination
        {
            internal BoundExpression Argument { get; }

            /// <summary>
            /// In the case this is the argument for a ref / out parameter this will refer
            /// to the corresponding parameter. This will be null in cases like arguments 
            /// passed to an arglist.
            /// </summary>
            internal ParameterSymbol? Parameter { get; }

            /// <summary>
            /// This destination can only be written to by arguments that have an equal or
            /// wider escape level. An destination that is <see cref="EscapeLevel.CallingMethod"/>
            /// can never be written to by an argument that has a level of <see cref="EscapeLevel.ReturnOnly"/>.
            /// </summary>
            internal EscapeLevel EscapeLevel { get; }

            internal MixableDestination(ParameterSymbol parameter, BoundExpression argument)
            {
                Debug.Assert(parameter.RefKind.IsWritableReference() && parameter.Type.IsRefLikeType);
                Debug.Assert(GetParameterValEscapeLevel(parameter).HasValue);
                Argument = argument;
                Parameter = parameter;
                EscapeLevel = GetParameterValEscapeLevel(parameter)!.Value;
            }

            internal MixableDestination(BoundExpression argument, EscapeLevel escapeLevel)
            {
                Argument = argument;
                Parameter = null;
                EscapeLevel = escapeLevel;
            }

            internal bool IsAssignableFrom(EscapeLevel level) => EscapeLevel switch
            {
                EscapeLevel.CallingMethod => level == EscapeLevel.CallingMethod,
                EscapeLevel.ReturnOnly => true,
                _ => throw ExceptionUtilities.UnexpectedValue(EscapeLevel)
            };

            public override string? ToString() => (Parameter, Argument, EscapeLevel).ToString();
        }

        /// <summary>
        /// Represents an argument being analyzed for escape analysis purposes. This represents the
        /// argument as written. For example a `ref x` will only be represented by a single 
        /// <see cref="EscapeArgument"/>.
        /// </summary>
        private readonly struct EscapeArgument
        {
            /// <summary>
            /// This will be null in cases like arglist or a function pointer receiver.
            /// </summary>
            internal ParameterSymbol? Parameter { get; }

            internal BoundExpression Argument { get; }

            internal RefKind RefKind { get; }

            internal EscapeArgument(ParameterSymbol? parameter, BoundExpression argument, RefKind refKind, bool isArgList = false)
            {
                Debug.Assert(!isArgList || parameter is null);
                Argument = argument;
                Parameter = parameter;
                RefKind = refKind;
            }

            public void Deconstruct(out ParameterSymbol? parameter, out BoundExpression argument, out RefKind refKind)
            {
                parameter = Parameter;
                argument = Argument;
                refKind = RefKind;
            }

            public override string? ToString() => Parameter is { } p
                ? p.ToString()
                : Argument.ToString();
        }

        /// <summary>
        /// Represents a value being analyzed for escape analysis purposes. This represents the value 
        /// as it contributes to escape analysis which means arguments can show up multiple times. For
        /// example `ref x` will be represented as both a val and ref escape.
        /// </summary>
        private readonly struct EscapeValue
        {
            /// <summary>
            /// This will be null in cases like arglist or a function pointer receiver.
            /// </summary>
            internal ParameterSymbol? Parameter { get; }

            internal BoundExpression Argument { get; }

            /// <summary>
            /// This is _only_ useful when calculating MAMM as it dictates to what level the value 
            /// escaped to. That allows it to be filtered against the parameters it could possibly
            /// write to.
            /// </summary>
            internal EscapeLevel EscapeLevel { get; }

            internal bool IsRefEscape { get; }

            internal EscapeValue(ParameterSymbol? parameter, BoundExpression argument, EscapeLevel escapeLevel, bool isRefEscape)
            {
                Argument = argument;
                Parameter = parameter;
                EscapeLevel = escapeLevel;
                IsRefEscape = isRefEscape;
            }

            public void Deconstruct(out ParameterSymbol? parameter, out BoundExpression argument, out EscapeLevel escapeLevel, out bool isRefEscape)
            {
                parameter = Parameter;
                argument = Argument;
                escapeLevel = EscapeLevel;
                isRefEscape = IsRefEscape;
            }

            public override string? ToString() => Parameter is { } p
                ? p.ToString()
                : Argument.ToString();
        }

        /// <summary>
        /// For the purpose of escape verification we operate with the depth of local scopes.
        /// The depth is a uint, with smaller number representing shallower/wider scopes.
        /// 0, 1 and 2 are special scopes - 
        /// 0 is the "calling method" scope that is outside of the containing method/lambda. 
        ///   If something can escape to scope 0, it can escape to any scope in a given method through a ref parameter or return.
        /// 1 is the "return-only" scope that is outside of the containing method/lambda. 
        ///   If something can escape to scope 1, it can escape to any scope in a given method or can be returned, but it can't escape through a ref parameter.
        /// 2 is the "current method" scope that is just inside the containing method/lambda. 
        ///   If something can escape to scope 1, it can escape to any scope in a given method, but cannot be returned.
        /// n + 1 corresponds to scopes immediately inside a scope of depth n. 
        ///   Since sibling scopes do not intersect and a value cannot escape from one to another without 
        ///   escaping to a wider scope, we can use simple depth numbering without ambiguity.
        /// </summary>
        private const uint CallingMethodScope = 0;
        private const uint ReturnOnlyScope = 1;
        private const uint CurrentMethodScope = 2;
    }
#nullable disable

    internal partial class Binder
    {
        // Some value kinds are semantically the same and the only distinction is how errors are reported
        // for those purposes we reserve lowest 2 bits
        private const int ValueKindInsignificantBits = 2;
        private const BindValueKind ValueKindSignificantBitsMask = unchecked((BindValueKind)~((1 << ValueKindInsignificantBits) - 1));

        /// <summary>
        /// Expression capabilities and requirements.
        /// </summary>
        [Flags]
        internal enum BindValueKind : ushort
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

            /// <summary>
            /// Expression can be the LHS of a ref-assign operation.
            /// Example:
            ///  ref local, ref parameter, out parameter, ref field
            /// </summary>
            RefAssignable = 8 << ValueKindInsignificantBits,

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
            /// Expression can be the operand of an address-of operation (&amp;).
            /// Same as ReadonlyRef. The difference is just for error reporting.
            /// </summary>
            AddressOf = ReadonlyRef + 1,

            /// <summary>
            /// Expression is the receiver of a fixed buffer field access
            /// Same as ReadonlyRef. The difference is just for error reporting.
            /// </summary>
            FixedReceiver = ReadonlyRef + 2,

            /// <summary>
            /// Expression is passed as a ref or out parameter or assigned to a byref variable.
            /// </summary>
            RefOrOut = RefersToLocation | RValue | Assignable,

            /// <summary>
            /// Expression is returned by an ordinary r/w reference.
            /// Same as RefOrOut. The difference is just for error reporting.
            /// </summary>
            RefReturn = RefOrOut + 1,
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

        private static bool RequiresRefAssignableVariable(BindValueKind kind)
        {
            return (kind & BindValueKind.RefAssignable) != 0;
        }

        private static bool RequiresRefOrOut(BindValueKind kind)
        {
            return (kind & BindValueKind.RefOrOut) == BindValueKind.RefOrOut;
        }

#nullable enable

        private BoundIndexerAccess BindIndexerDefaultArgumentsAndParamsArray(BoundIndexerAccess indexerAccess, BindValueKind valueKind, BindingDiagnosticBag diagnostics)
        {
            var useSetAccessor = valueKind == BindValueKind.Assignable && !indexerAccess.Indexer.ReturnsByRef;
            var accessorForDefaultArguments = useSetAccessor
                ? indexerAccess.Indexer.GetOwnOrInheritedSetMethod()
                : indexerAccess.Indexer.GetOwnOrInheritedGetMethod();
            if (accessorForDefaultArguments is not null)
            {
                var argumentsBuilder = ArrayBuilder<BoundExpression>.GetInstance(accessorForDefaultArguments.ParameterCount);
                argumentsBuilder.AddRange(indexerAccess.Arguments);

                ArrayBuilder<RefKind>? refKindsBuilderOpt;
                if (!indexerAccess.ArgumentRefKindsOpt.IsDefaultOrEmpty)
                {
                    refKindsBuilderOpt = ArrayBuilder<RefKind>.GetInstance(accessorForDefaultArguments.ParameterCount);
                    refKindsBuilderOpt.AddRange(indexerAccess.ArgumentRefKindsOpt);
                }
                else
                {
                    refKindsBuilderOpt = null;
                }
                var argsToParams = indexerAccess.ArgsToParamsOpt;

                // It is possible for the indexer 'value' parameter from metadata to have a default value, but the compiler will not use it.
                // However, we may still use any default values from the preceding parameters.
                var parameters = accessorForDefaultArguments.Parameters;
                if (useSetAccessor)
                {
                    parameters = parameters.RemoveAt(parameters.Length - 1);
                }

                BitVector defaultArguments = default;
                Debug.Assert(parameters.Length == indexerAccess.Indexer.Parameters.Length);

                ImmutableArray<string?> argumentNamesOpt = indexerAccess.ArgumentNamesOpt;

                // If OriginalIndexersOpt is set, there was an overload resolution failure, and we don't want to make guesses about the default
                // arguments that will end up being reflected in the SemanticModel/IOperation
                if (indexerAccess.OriginalIndexersOpt.IsDefault)
                {
                    ArrayBuilder<(string Name, Location Location)?>? namesBuilder = null;

                    if (!argumentNamesOpt.IsDefaultOrEmpty)
                    {
                        namesBuilder = ArrayBuilder<(string Name, Location Location)?>.GetInstance(argumentNamesOpt.Length);
                        foreach (var name in argumentNamesOpt)
                        {
                            if (name is null)
                            {
                                namesBuilder.Add(null);
                            }
                            else
                            {
                                namesBuilder.Add((name, NoLocation.Singleton));
                            }
                        }
                    }

                    BindDefaultArgumentsAndParamsArray(indexerAccess.Syntax, parameters, argumentsBuilder, refKindsBuilderOpt, namesBuilder, ref argsToParams, out defaultArguments, indexerAccess.Expanded, enableCallerInfo: true, diagnostics);

                    if (namesBuilder is object)
                    {
                        argumentNamesOpt = namesBuilder.SelectAsArray(item => item?.Name);
                        namesBuilder.Free();
                    }
                }

                indexerAccess = indexerAccess.Update(
                    indexerAccess.ReceiverOpt,
                    indexerAccess.InitialBindingReceiverIsSubjectToCloning,
                    indexerAccess.Indexer,
                    argumentsBuilder.ToImmutableAndFree(),
                    argumentNamesOpt,
                    refKindsBuilderOpt?.ToImmutableOrNull() ?? default,
                    indexerAccess.Expanded,
                    argsToParams,
                    defaultArguments,
                    indexerAccess.Type);

                refKindsBuilderOpt?.Free();
            }

            return indexerAccess;
        }

#nullable disable

        /// <summary>
        /// Check the expression is of the required lvalue and rvalue specified by valueKind.
        /// The method returns the original expression if the expression is of the required
        /// type. Otherwise, an appropriate error is added to the diagnostics bag and the
        /// method returns a BoundBadExpression node. The method returns the original
        /// expression without generating any error if the expression has errors.
        /// </summary>
        private BoundExpression CheckValue(BoundExpression expr, BindValueKind valueKind, BindingDiagnosticBag diagnostics)
        {
            switch (expr.Kind)
            {
                case BoundKind.PropertyGroup:
                    expr = BindIndexedPropertyAccess((BoundPropertyGroup)expr, mustHaveAllOptionalParameters: false, diagnostics: diagnostics);
                    if (expr is BoundIndexerAccess indexerAccess)
                    {
                        expr = BindIndexerDefaultArgumentsAndParamsArray(indexerAccess, valueKind, diagnostics);
                    }
                    break;

                case BoundKind.Local:
                    Debug.Assert(expr.Syntax.Kind() != SyntaxKind.Argument || valueKind == BindValueKind.RefOrOut);
                    break;

                case BoundKind.OutVariablePendingInference:
                case BoundKind.OutDeconstructVarPendingInference:
                    Debug.Assert(valueKind == BindValueKind.RefOrOut);
                    return expr;

                case BoundKind.DiscardExpression:
                    Debug.Assert(valueKind is (BindValueKind.Assignable or BindValueKind.RefOrOut or BindValueKind.RefAssignable) || diagnostics.DiagnosticBag is null || diagnostics.HasAnyResolvedErrors());
                    return expr;

                case BoundKind.IndexerAccess:
                    expr = BindIndexerDefaultArgumentsAndParamsArray((BoundIndexerAccess)expr, valueKind, diagnostics);
                    break;

                case BoundKind.UnconvertedObjectCreationExpression:
                    if (valueKind == BindValueKind.RValue)
                    {
                        return expr;
                    }
                    break;

                case BoundKind.UnconvertedCollectionExpression:
                    if (valueKind == BindValueKind.RValue)
                    {
                        return expr;
                    }
                    break;

                case BoundKind.PointerIndirectionOperator:
                    if ((valueKind & BindValueKind.RefersToLocation) == BindValueKind.RefersToLocation)
                    {
                        var pointerIndirection = (BoundPointerIndirectionOperator)expr;
                        expr = pointerIndirection.Update(pointerIndirection.Operand, refersToLocation: true, pointerIndirection.Type);
                    }
                    break;

                case BoundKind.PointerElementAccess:
                    if ((valueKind & BindValueKind.RefersToLocation) == BindValueKind.RefersToLocation)
                    {
                        var elementAccess = (BoundPointerElementAccess)expr;
                        expr = elementAccess.Update(elementAccess.Expression, elementAccess.Index, elementAccess.Checked, refersToLocation: true, elementAccess.Type);
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
                CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
                var resolution = this.ResolveMethodGroup(methodGroup, analyzedArguments: null, isMethodGroupConversion: false, useSiteInfo: ref useSiteInfo);
                diagnostics.Add(expr.Syntax, useSiteInfo);
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
                        receiver = otherSymbol.RequiresInstanceReceiver()
                            ? typeOrValue.Data.ValueExpression
                            : null; // no receiver required
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

        internal static bool IsTypeOrValueExpression(BoundExpression expression)
        {
            switch (expression?.Kind)
            {
                case BoundKind.TypeOrValueExpression:
                case BoundKind.QueryClause when ((BoundQueryClause)expression).Value.Kind == BoundKind.TypeOrValueExpression:
                    return true;
                default:
                    return false;
            }
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
        internal bool CheckValueKind(SyntaxNode node, BoundExpression expr, BindValueKind valueKind, bool checkingReceiver, BindingDiagnosticBag diagnostics)
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
                case BoundKind.ImplicitIndexerAccess when ((BoundImplicitIndexerAccess)expr).IndexerOrSliceAccess.Kind == BoundKind.IndexerAccess:
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
            if ((expr.ConstantValueOpt != null) || (expr.Type.GetSpecialTypeSafe() == SpecialType.System_Void))
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

                case BoundKind.UnconvertedAddressOfOperator:
                    var unconvertedAddressOf = (BoundUnconvertedAddressOfOperator)expr;
                    Error(diagnostics, GetMethodGroupOrFunctionPointerLvalueError(valueKind), node, unconvertedAddressOf.Operand.Name, MessageID.IDS_AddressOfMethodGroup.Localize());
                    return false;

                case BoundKind.MethodGroup when valueKind == BindValueKind.AddressOf:
                    // If the addressof operator is used not as an rvalue, that will get flagged when CheckValue
                    // is called on the parent BoundUnconvertedAddressOf node.
                    return true;

                case BoundKind.MethodGroup:
                    // method groups can only be used as RValues except when taking the address of one
                    var methodGroup = (BoundMethodGroup)expr;
                    Error(diagnostics, GetMethodGroupOrFunctionPointerLvalueError(valueKind), node, methodGroup.Name, MessageID.IDS_MethodGroup.Localize());
                    return false;

                case BoundKind.RangeVariable:
                    {
                        // range variables can only be used as RValues
                        var queryref = (BoundRangeVariable)expr;
                        var errorCode = GetRangeLvalueError(valueKind);
                        if (errorCode is ErrorCode.ERR_InvalidAddrOp or ErrorCode.ERR_RefLocalOrParamExpected)
                        {
                            Error(diagnostics, errorCode, node);
                        }
                        else
                        {
                            Error(diagnostics, errorCode, node, queryref.RangeVariableSymbol.Name);
                        }
                        return false;
                    }

                case BoundKind.Conversion:
                    var conversion = (BoundConversion)expr;
                    // conversions are strict RValues, but unboxing has a specific error
                    if (conversion.ConversionKind == ConversionKind.Unboxing)
                    {
                        Error(diagnostics, ErrorCode.ERR_UnboxNotLValue, node);
                        return false;
                    }
                    break;

                // array access is readwrite variable if the indexing expression is not System.Range
                case BoundKind.ArrayAccess:
                    return checkArrayAccessValueKind(node, valueKind, ((BoundArrayAccess)expr).Indices, diagnostics);

                // pointer dereferencing is a readwrite variable
                case BoundKind.PointerIndirectionOperator:
                // The undocumented __refvalue(tr, T) expression results in a variable of type T.
                case BoundKind.RefValueOperator:
                // dynamic expressions are readwrite, and can even be passed by ref (which is implemented via a temp)
                case BoundKind.DynamicMemberAccess:
                case BoundKind.DynamicIndexerAccess:
                case BoundKind.DynamicObjectInitializerMember:
                    {
                        if (RequiresRefAssignableVariable(valueKind))
                        {
                            Error(diagnostics, ErrorCode.ERR_RefLocalOrParamExpected, node);
                            return false;
                        }

                        // These are readwrite variables
                        return true;
                    }

                case BoundKind.PointerElementAccess:
                    {
                        if (RequiresRefAssignableVariable(valueKind))
                        {
                            Error(diagnostics, ErrorCode.ERR_RefLocalOrParamExpected, node);
                            return false;
                        }

                        var receiver = ((BoundPointerElementAccess)expr).Expression;
                        if (receiver is BoundFieldAccess fieldAccess && fieldAccess.FieldSymbol.IsFixedSizeBuffer)
                        {
                            return CheckValueKind(node, fieldAccess.ReceiverOpt, valueKind, checkingReceiver: true, diagnostics);
                        }

                        return true;
                    }

                case BoundKind.Parameter:
                    var parameter = (BoundParameter)expr;
                    return CheckParameterValueKind(node, parameter, valueKind, checkingReceiver, diagnostics);

                case BoundKind.Local:
                    var local = (BoundLocal)expr;
                    return CheckLocalValueKind(node, local, valueKind, checkingReceiver, diagnostics);

                case BoundKind.ThisReference:
                    // `this` is never ref assignable
                    if (RequiresRefAssignableVariable(valueKind))
                    {
                        Error(diagnostics, ErrorCode.ERR_RefLocalOrParamExpected, node);
                        return false;
                    }

                    // We will already have given an error for "this" used outside of a constructor, 
                    // instance method, or instance accessor. Assume that "this" is a variable if it is in a struct.

                    // SPEC: when this is used in a primary-expression within an instance constructor of a struct, 
                    // SPEC: it is classified as a variable. 

                    // SPEC: When this is used in a primary-expression within an instance method or instance accessor
                    // SPEC: of a struct, it is classified as a variable. 

                    // Note: RValueOnly is checked at the beginning of this method. Since we are here we need more than readable.
                    // "this" is readonly in members marked "readonly" and in members of readonly structs, unless we are in a constructor.
                    var isValueType = ((BoundThisReference)expr).Type.IsValueType;
                    if (!isValueType || (RequiresAssignableVariable(valueKind) && (this.ContainingMemberOrLambda as MethodSymbol)?.IsEffectivelyReadOnly == true))
                    {
                        ReportThisLvalueError(node, valueKind, isValueType, isPrimaryConstructorParameter: false, diagnostics);
                        return false;
                    }

                    return true;

                case BoundKind.ImplicitReceiver:
                case BoundKind.ObjectOrCollectionValuePlaceholder:
                    Debug.Assert(!RequiresRefAssignableVariable(valueKind));
                    return true;

                case BoundKind.Call:
                    var call = (BoundCall)expr;
                    return CheckMethodReturnValueKind(call.Method, call.Syntax, node, valueKind, checkingReceiver, diagnostics);

                case BoundKind.FunctionPointerInvocation:
                    return CheckMethodReturnValueKind(((BoundFunctionPointerInvocation)expr).FunctionPointer.Signature,
                        expr.Syntax,
                        node,
                        valueKind,
                        checkingReceiver,
                        diagnostics);

                case BoundKind.ImplicitIndexerAccess:
                    var implicitIndexer = (BoundImplicitIndexerAccess)expr;
                    switch (implicitIndexer.IndexerOrSliceAccess)
                    {
                        case BoundArrayAccess arrayAccess:
                            return checkArrayAccessValueKind(node, valueKind, arrayAccess.Indices, diagnostics);

                        case BoundCall sliceAccess:
                            return CheckMethodReturnValueKind(sliceAccess.Method, sliceAccess.Syntax, node, valueKind, checkingReceiver, diagnostics);

                        default:
                            throw ExceptionUtilities.UnexpectedValue(implicitIndexer.IndexerOrSliceAccess.Kind);
                    }

                case BoundKind.InlineArrayAccess:
                    {
                        var elementAccess = (BoundInlineArrayAccess)expr;

                        if (elementAccess.IsValue || elementAccess.GetItemOrSliceHelper is WellKnownMember.System_Span_T__Slice_Int_Int or WellKnownMember.System_ReadOnlySpan_T__Slice_Int_Int)
                        {
                            // Strict RValue
                            break;
                        }

                        var getItemOrSliceHelper = (MethodSymbol)Compilation.GetWellKnownTypeMember(elementAccess.GetItemOrSliceHelper);

                        if (getItemOrSliceHelper is null)
                        {
                            return true;
                        }

                        getItemOrSliceHelper = getItemOrSliceHelper.AsMember(getItemOrSliceHelper.ContainingType.Construct(ImmutableArray.Create(elementAccess.Expression.Type.TryGetInlineArrayElementField().TypeWithAnnotations)));

                        return CheckMethodReturnValueKind(getItemOrSliceHelper, elementAccess.Syntax, node, valueKind, checkingReceiver, diagnostics);
                    }

                case BoundKind.ImplicitIndexerReceiverPlaceholder:
                    break;

                case BoundKind.DeconstructValuePlaceholder:
                    break;

                case BoundKind.ConditionalOperator:
                    var conditional = (BoundConditionalOperator)expr;

                    // byref conditional defers to its operands
                    if (conditional.IsRef &&
                        (CheckValueKind(conditional.Consequence.Syntax, conditional.Consequence, valueKind, checkingReceiver: false, diagnostics: diagnostics) &
                        CheckValueKind(conditional.Alternative.Syntax, conditional.Alternative, valueKind, checkingReceiver: false, diagnostics: diagnostics)))
                    {
                        return true;
                    }

                    // report standard lvalue error
                    break;

                case BoundKind.FieldAccess:
                    {
                        var fieldAccess = (BoundFieldAccess)expr;
                        return CheckFieldValueKind(node, fieldAccess, valueKind, checkingReceiver, diagnostics);
                    }

                case BoundKind.AssignmentOperator:
                    var assignment = (BoundAssignmentOperator)expr;
                    return CheckSimpleAssignmentValueKind(node, assignment, valueKind, diagnostics);

                case BoundKind.ValuePlaceholder:
                    // Strict RValue
                    break;

                default:
                    Debug.Assert(expr is not BoundValuePlaceholderBase, $"Placeholder kind {expr.Kind} should be explicitly handled");
                    break;
            }

            // At this point we should have covered all the possible cases for anything that is not a strict RValue.
            Error(diagnostics, GetStandardLvalueError(valueKind), node);
            return false;

            bool checkArrayAccessValueKind(SyntaxNode node, BindValueKind valueKind, ImmutableArray<BoundExpression> indices, BindingDiagnosticBag diagnostics)
            {
                if (RequiresRefAssignableVariable(valueKind))
                {
                    Error(diagnostics, ErrorCode.ERR_RefLocalOrParamExpected, node);
                    return false;
                }

                if (indices.Length == 1 &&
                    TypeSymbol.Equals(
                        indices[0].Type,
                        Compilation.GetWellKnownType(WellKnownType.System_Range),
                        TypeCompareKind.ConsiderEverything))
                {
                    // Range indexer is an rvalue
                    Error(diagnostics, GetStandardLvalueError(valueKind), node);
                    return false;
                }
                return true;
            }
        }

        private static void ReportThisLvalueError(SyntaxNode node, BindValueKind valueKind, bool isValueType, bool isPrimaryConstructorParameter, BindingDiagnosticBag diagnostics)
        {
            var errorCode = GetThisLvalueError(valueKind, isValueType, isPrimaryConstructorParameter);
            if (errorCode is ErrorCode.ERR_InvalidAddrOp or ErrorCode.ERR_IncrementLvalueExpected or ErrorCode.ERR_RefReturnThis or ErrorCode.ERR_RefLocalOrParamExpected or ErrorCode.ERR_RefLvalueExpected)
            {
                Error(diagnostics, errorCode, node);
            }
            else
            {
                Error(diagnostics, errorCode, node, node);
            }
        }

        private static bool CheckNotNamespaceOrType(BoundExpression expr, BindingDiagnosticBag diagnostics)
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

        private bool CheckLocalValueKind(SyntaxNode node, BoundLocal local, BindValueKind valueKind, bool checkingReceiver, BindingDiagnosticBag diagnostics)
        {
            if (valueKind == BindValueKind.AddressOf && this.IsInAsyncMethod())
            {
                Error(diagnostics, ErrorCode.WRN_AddressOfInAsync, node);
            }

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

                // IsWritable means the variable is writable. If this is a ref variable, IsWritable
                // does not imply anything about the storage location
                if (localSymbol.RefKind == RefKind.RefReadOnly ||
                    (localSymbol.RefKind == RefKind.None && !localSymbol.IsWritableVariable))
                {
                    ReportReadonlyLocalError(node, localSymbol, valueKind, checkingReceiver, diagnostics);
                    return false;
                }
            }
            else if (RequiresRefAssignableVariable(valueKind))
            {
                if (localSymbol.RefKind == RefKind.None)
                {
                    diagnostics.Add(ErrorCode.ERR_RefLocalOrParamExpected, node.Location);
                    return false;
                }
                else if (!localSymbol.IsWritableVariable)
                {
                    ReportReadonlyLocalError(node, localSymbol, valueKind, checkingReceiver, diagnostics);
                    return false;
                }
            }

            return true;
        }
    }

    internal partial class RefSafetyAnalysis
    {
        private bool CheckLocalRefEscape(SyntaxNode node, BoundLocal local, uint escapeTo, bool checkingReceiver, BindingDiagnosticBag diagnostics)
        {
            LocalSymbol localSymbol = local.LocalSymbol;

            // if local symbol can escape to the same or wider/shallower scope then escapeTo
            // then it is all ok, otherwise it is an error.
            if (GetLocalScopes(localSymbol).RefEscapeScope <= escapeTo)
            {
                return true;
            }

            var inUnsafeRegion = _inUnsafeRegion;
            if (escapeTo is CallingMethodScope or ReturnOnlyScope)
            {
                if (localSymbol.RefKind == RefKind.None)
                {
                    if (checkingReceiver)
                    {
                        Error(diagnostics, inUnsafeRegion ? ErrorCode.WRN_RefReturnLocal2 : ErrorCode.ERR_RefReturnLocal2, local.Syntax, localSymbol);
                    }
                    else
                    {
                        Error(diagnostics, inUnsafeRegion ? ErrorCode.WRN_RefReturnLocal : ErrorCode.ERR_RefReturnLocal, node, localSymbol);
                    }
                    return inUnsafeRegion;
                }

                if (checkingReceiver)
                {
                    Error(diagnostics, inUnsafeRegion ? ErrorCode.WRN_RefReturnNonreturnableLocal2 : ErrorCode.ERR_RefReturnNonreturnableLocal2, local.Syntax, localSymbol);
                }
                else
                {
                    Error(diagnostics, inUnsafeRegion ? ErrorCode.WRN_RefReturnNonreturnableLocal : ErrorCode.ERR_RefReturnNonreturnableLocal, node, localSymbol);
                }
                return inUnsafeRegion;
            }

            Error(diagnostics, inUnsafeRegion ? ErrorCode.WRN_EscapeVariable : ErrorCode.ERR_EscapeVariable, node, localSymbol);
            return inUnsafeRegion;
        }
    }

    internal partial class Binder
    {
        private bool CheckParameterValueKind(SyntaxNode node, BoundParameter parameter, BindValueKind valueKind, bool checkingReceiver, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(!RequiresAssignableVariable(BindValueKind.AddressOf));
            if (valueKind == BindValueKind.AddressOf && this.IsInAsyncMethod())
            {
                Error(diagnostics, ErrorCode.WRN_AddressOfInAsync, node);
            }

            ParameterSymbol parameterSymbol = parameter.ParameterSymbol;

            // all parameters can be passed by ref/out or assigned to
            // except "in" and "ref readonly" parameters, which are readonly
            if (parameterSymbol.RefKind is RefKind.In or RefKind.RefReadOnlyParameter && RequiresAssignableVariable(valueKind))
            {
                ReportReadOnlyError(parameterSymbol, node, valueKind, checkingReceiver, diagnostics);
                return false;
            }
            else if (parameterSymbol.RefKind == RefKind.None && RequiresRefAssignableVariable(valueKind))
            {
                Error(diagnostics, ErrorCode.ERR_RefLocalOrParamExpected, node);
                return false;
            }

            Debug.Assert(parameterSymbol.RefKind != RefKind.None || !RequiresRefAssignableVariable(valueKind));

            // It is an error to capture 'in', 'ref' or 'out' parameters.
            // Skipping them to simplify the logic.
            if (parameterSymbol.RefKind == RefKind.None &&
                parameterSymbol.ContainingSymbol is SynthesizedPrimaryConstructor primaryConstructor &&
                primaryConstructor.GetCapturedParameters().TryGetValue(parameterSymbol, out FieldSymbol backingField))
            {
                Debug.Assert(backingField.RefKind == RefKind.None);
                Debug.Assert(!RequiresRefAssignableVariable(valueKind));

                if (backingField.IsReadOnly)
                {
                    Debug.Assert(backingField.RefKind == RefKind.None);

                    if (RequiresAssignableVariable(valueKind) &&
                        !CanModifyReadonlyField(receiverIsThis: true, backingField))
                    {
                        reportReadOnlyParameterError(parameterSymbol, node, valueKind, checkingReceiver, diagnostics);
                        return false;
                    }
                }

                if (RequiresAssignableVariable(valueKind) && !backingField.ContainingType.IsReferenceType && (this.ContainingMemberOrLambda as MethodSymbol)?.IsEffectivelyReadOnly == true)
                {
                    ReportThisLvalueError(node, valueKind, isValueType: true, isPrimaryConstructorParameter: true, diagnostics);
                    return false;
                }
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

        static void reportReadOnlyParameterError(ParameterSymbol parameterSymbol, SyntaxNode node, BindValueKind valueKind, bool checkingReceiver, BindingDiagnosticBag diagnostics)
        {
            // It's clearer to say that the address can't be taken than to say that the field can't be modified
            // (even though the latter message gives more explanation of why).
            Debug.Assert(valueKind != BindValueKind.AddressOf); // If this assert fails, we probably should report ErrorCode.ERR_InvalidAddrOp

            if (checkingReceiver)
            {
                ErrorCode errorCode;

                if (valueKind == BindValueKind.RefReturn)
                {
                    errorCode = ErrorCode.ERR_RefReturnReadonlyPrimaryConstructorParameter2;
                }
                else if (RequiresRefOrOut(valueKind))
                {
                    errorCode = ErrorCode.ERR_RefReadonlyPrimaryConstructorParameter2;
                }
                else
                {
                    errorCode = ErrorCode.ERR_AssgReadonlyPrimaryConstructorParameter2;
                }

                Error(diagnostics, errorCode, node, parameterSymbol);
            }
            else
            {
                ErrorCode errorCode;

                if (valueKind == BindValueKind.RefReturn)
                {
                    errorCode = ErrorCode.ERR_RefReturnReadonlyPrimaryConstructorParameter;
                }
                else if (RequiresRefOrOut(valueKind))
                {
                    errorCode = ErrorCode.ERR_RefReadonlyPrimaryConstructorParameter;
                }
                else
                {
                    errorCode = ErrorCode.ERR_AssgReadonlyPrimaryConstructorParameter;
                }

                Error(diagnostics, errorCode, node);
            }
        }
    }

    internal partial class RefSafetyAnalysis
    {
        private static EscapeLevel? EscapeLevelFromScope(uint scope) => scope switch
        {
            ReturnOnlyScope => EscapeLevel.ReturnOnly,
            CallingMethodScope => EscapeLevel.CallingMethod,
            _ => null,
        };

        private static uint GetParameterValEscape(ParameterSymbol parameter)
        {
            return parameter switch
            {
                { EffectiveScope: ScopedKind.ScopedValue } => CurrentMethodScope,
                { RefKind: RefKind.Out, UseUpdatedEscapeRules: true } => ReturnOnlyScope,
                _ => CallingMethodScope
            };
        }

        private static EscapeLevel? GetParameterValEscapeLevel(ParameterSymbol parameter) =>
            EscapeLevelFromScope(GetParameterValEscape(parameter));

        private static uint GetParameterRefEscape(ParameterSymbol parameter)
        {
            return parameter switch
            {
                { RefKind: RefKind.None } => CurrentMethodScope,
                { EffectiveScope: ScopedKind.ScopedRef } => CurrentMethodScope,
                { HasUnscopedRefAttribute: true, RefKind: RefKind.Out } => ReturnOnlyScope,
                { HasUnscopedRefAttribute: true, IsThis: false } => CallingMethodScope,
                _ => ReturnOnlyScope
            };
        }

        private static EscapeLevel? GetParameterRefEscapeLevel(ParameterSymbol parameter) =>
            EscapeLevelFromScope(GetParameterRefEscape(parameter));

        private bool CheckParameterValEscape(SyntaxNode node, ParameterSymbol parameter, uint escapeTo, BindingDiagnosticBag diagnostics)
        {
            if (_useUpdatedEscapeRules)
            {
                if (GetParameterValEscape(parameter) > escapeTo)
                {
                    Error(diagnostics, _inUnsafeRegion ? ErrorCode.WRN_EscapeVariable : ErrorCode.ERR_EscapeVariable, node, parameter);
                    return _inUnsafeRegion;
                }
                return true;
            }
            else
            {
                // always returnable
                return true;
            }
        }

        private bool CheckParameterRefEscape(SyntaxNode node, BoundExpression parameter, ParameterSymbol parameterSymbol, uint escapeTo, bool checkingReceiver, BindingDiagnosticBag diagnostics)
        {
            var refSafeToEscape = GetParameterRefEscape(parameterSymbol);
            if (refSafeToEscape > escapeTo)
            {
                var isRefScoped = parameterSymbol.EffectiveScope == ScopedKind.ScopedRef;
                Debug.Assert(parameterSymbol.RefKind == RefKind.None || isRefScoped || refSafeToEscape == ReturnOnlyScope);
                var inUnsafeRegion = _inUnsafeRegion;

                if (parameter is BoundThisReference)
                {
                    Error(diagnostics, inUnsafeRegion ? ErrorCode.WRN_RefReturnStructThis : ErrorCode.ERR_RefReturnStructThis, node);
                    return inUnsafeRegion;
                }

#pragma warning disable format
                var (errorCode, syntax) = (checkingReceiver, isRefScoped, inUnsafeRegion, refSafeToEscape) switch
                {
                    (checkingReceiver: true,  isRefScoped: true,  inUnsafeRegion: false, _)                      => (ErrorCode.ERR_RefReturnScopedParameter2, parameter.Syntax),
                    (checkingReceiver: true,  isRefScoped: true,  inUnsafeRegion: true,  _)                      => (ErrorCode.WRN_RefReturnScopedParameter2, parameter.Syntax),
                    (checkingReceiver: true,  isRefScoped: false, inUnsafeRegion: false, ReturnOnlyScope) => (ErrorCode.ERR_RefReturnOnlyParameter2,   parameter.Syntax),
                    (checkingReceiver: true,  isRefScoped: false, inUnsafeRegion: true,  ReturnOnlyScope) => (ErrorCode.WRN_RefReturnOnlyParameter2,   parameter.Syntax),
                    (checkingReceiver: true,  isRefScoped: false, inUnsafeRegion: false, _)                      => (ErrorCode.ERR_RefReturnParameter2,       parameter.Syntax),
                    (checkingReceiver: true,  isRefScoped: false, inUnsafeRegion: true,  _)                      => (ErrorCode.WRN_RefReturnParameter2,       parameter.Syntax),
                    (checkingReceiver: false, isRefScoped: true,  inUnsafeRegion: false, _)                      => (ErrorCode.ERR_RefReturnScopedParameter,  node),
                    (checkingReceiver: false, isRefScoped: true,  inUnsafeRegion: true,  _)                      => (ErrorCode.WRN_RefReturnScopedParameter,  node),
                    (checkingReceiver: false, isRefScoped: false, inUnsafeRegion: false, ReturnOnlyScope) => (ErrorCode.ERR_RefReturnOnlyParameter,    node),
                    (checkingReceiver: false, isRefScoped: false, inUnsafeRegion: true,  ReturnOnlyScope) => (ErrorCode.WRN_RefReturnOnlyParameter,    node),
                    (checkingReceiver: false, isRefScoped: false, inUnsafeRegion: false, _)                      => (ErrorCode.ERR_RefReturnParameter,        node),
                    (checkingReceiver: false, isRefScoped: false, inUnsafeRegion: true,  _)                      => (ErrorCode.WRN_RefReturnParameter,        node)
                };
#pragma warning restore format
                Error(diagnostics, errorCode, syntax, parameterSymbol.Name);
                return inUnsafeRegion;
            }

            // can ref-escape to any scope otherwise
            return true;
        }
    }

    internal partial class Binder
    {
        private bool CheckFieldValueKind(SyntaxNode node, BoundFieldAccess fieldAccess, BindValueKind valueKind, bool checkingReceiver, BindingDiagnosticBag diagnostics)
        {
            var fieldSymbol = fieldAccess.FieldSymbol;

            if (fieldSymbol.IsReadOnly)
            {
                // A field is writeable unless 
                // (1) it is readonly and we are not in a constructor or field initializer
                // (2) the receiver of the field is of value type and is not a variable or object creation expression.
                // For example, if you have a class C with readonly field f of type S, and
                // S has a mutable field x, then c.f.x is not a variable because c.f is not
                // writable.

                if ((fieldSymbol.RefKind == RefKind.None ? RequiresAssignableVariable(valueKind) : RequiresRefAssignableVariable(valueKind)) &&
                    !CanModifyReadonlyField(fieldAccess.ReceiverOpt is BoundThisReference, fieldSymbol))
                {
                    ReportReadOnlyFieldError(fieldSymbol, node, valueKind, checkingReceiver, diagnostics);
                    return false;
                }
            }

            if (RequiresAssignableVariable(valueKind))
            {
                switch (fieldSymbol.RefKind)
                {
                    case RefKind.None:
                        break;
                    case RefKind.Ref:
                        return true;
                    case RefKind.RefReadOnly:
                        ReportReadOnlyError(fieldSymbol, node, valueKind, checkingReceiver, diagnostics);
                        return false;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(fieldSymbol.RefKind);
                }

                if (fieldSymbol.IsFixedSizeBuffer)
                {
                    Error(diagnostics, GetStandardLvalueError(valueKind), node);
                    return false;
                }
            }

            if (RequiresRefAssignableVariable(valueKind))
            {
                Debug.Assert(!fieldSymbol.IsStatic);
                Debug.Assert(valueKind == BindValueKind.RefAssignable);

                switch (fieldSymbol.RefKind)
                {
                    case RefKind.None:
                        Error(diagnostics, ErrorCode.ERR_RefLocalOrParamExpected, node);
                        return false;
                    case RefKind.Ref:
                    case RefKind.RefReadOnly:
                        return CheckIsValidReceiverForVariable(node, fieldAccess.ReceiverOpt, BindValueKind.Assignable, diagnostics);
                    default:
                        throw ExceptionUtilities.UnexpectedValue(fieldSymbol.RefKind);
                }
            }

            // r/w fields that are static or belong to reference types are writeable and returnable
            if (fieldSymbol.IsStatic || fieldSymbol.ContainingType.IsReferenceType)
            {
                return true;
            }

            // for other fields defer to the receiver.
            return CheckIsValidReceiverForVariable(node, fieldAccess.ReceiverOpt, valueKind, diagnostics);
        }

        private bool CanModifyReadonlyField(bool receiverIsThis, FieldSymbol fieldSymbol)
        {
            // A field is writeable unless 
            // (1) it is readonly and we are not in a constructor or field initializer
            // (2) the receiver of the field is of value type and is not a variable or object creation expression.
            // For example, if you have a class C with readonly field f of type S, and
            // S has a mutable field x, then c.f.x is not a variable because c.f is not
            // writable.

            var fieldIsStatic = fieldSymbol.IsStatic;
            var canModifyReadonly = false;

            Symbol containing = this.ContainingMemberOrLambda;
            if ((object)containing != null &&
                fieldIsStatic == containing.IsStatic &&
                (fieldIsStatic || receiverIsThis) &&
                (Compilation.FeatureStrictEnabled
                    ? TypeSymbol.Equals(fieldSymbol.ContainingType, containing.ContainingType, TypeCompareKind.AllIgnoreOptions)
                    // We duplicate a bug in the native compiler for compatibility in non-strict mode
                    : TypeSymbol.Equals(fieldSymbol.ContainingType.OriginalDefinition, containing.ContainingType.OriginalDefinition, TypeCompareKind.AllIgnoreOptions)))
            {
                if (containing.Kind == SymbolKind.Method)
                {
                    MethodSymbol containingMethod = (MethodSymbol)containing;
                    MethodKind desiredMethodKind = fieldIsStatic ? MethodKind.StaticConstructor : MethodKind.Constructor;
                    canModifyReadonly = (containingMethod.MethodKind == desiredMethodKind) ||
                        isAssignedFromInitOnlySetterOnThis(receiverIsThis);
                }
                else if (containing.Kind == SymbolKind.Field)
                {
                    canModifyReadonly = true;
                }
            }

            return canModifyReadonly;

            bool isAssignedFromInitOnlySetterOnThis(bool receiverIsThis)
            {
                // bad: other.readonlyField = ...
                // bad: base.readonlyField = ...
                if (!receiverIsThis)
                {
                    return false;
                }

                if (!(ContainingMemberOrLambda is MethodSymbol method))
                {
                    return false;
                }

                return method.IsInitOnly;
            }
        }

        private bool CheckSimpleAssignmentValueKind(SyntaxNode node, BoundAssignmentOperator assignment, BindValueKind valueKind, BindingDiagnosticBag diagnostics)
        {
            // Only ref-assigns produce LValues
            if (assignment.IsRef)
            {
                return CheckValueKind(node, assignment.Left, valueKind, checkingReceiver: false, diagnostics);
            }

            Error(diagnostics, GetStandardLvalueError(valueKind), node);
            return false;
        }
    }

    internal partial class RefSafetyAnalysis
    {
        private uint GetFieldRefEscape(BoundFieldAccess fieldAccess, uint scopeOfTheContainingExpression)
        {
            var fieldSymbol = fieldAccess.FieldSymbol;

            // fields that are static or belong to reference types can ref escape anywhere
            if (fieldSymbol.IsStatic || fieldSymbol.ContainingType.IsReferenceType)
            {
                return CallingMethodScope;
            }

            if (_useUpdatedEscapeRules)
            {
                // SPEC: If `F` is a `ref` field its ref-safe-to-escape scope is the safe-to-escape scope of `e`.
                if (fieldSymbol.RefKind != RefKind.None)
                {
                    return GetValEscape(fieldAccess.ReceiverOpt, scopeOfTheContainingExpression);
                }
            }

            // for other fields defer to the receiver.
            return GetRefEscape(fieldAccess.ReceiverOpt, scopeOfTheContainingExpression);
        }

        private bool CheckFieldRefEscape(SyntaxNode node, BoundFieldAccess fieldAccess, uint escapeFrom, uint escapeTo, BindingDiagnosticBag diagnostics)
        {
            var fieldSymbol = fieldAccess.FieldSymbol;
            // fields that are static or belong to reference types can ref escape anywhere
            if (fieldSymbol.IsStatic || fieldSymbol.ContainingType.IsReferenceType)
            {
                return true;
            }

            Debug.Assert(fieldAccess.ReceiverOpt is { });

            if (_useUpdatedEscapeRules)
            {
                // SPEC: If `F` is a `ref` field its ref-safe-to-escape scope is the safe-to-escape scope of `e`.
                if (fieldSymbol.RefKind != RefKind.None)
                {
                    return CheckValEscape(node, fieldAccess.ReceiverOpt, escapeFrom, escapeTo, checkingReceiver: true, diagnostics);
                }
            }

            // for other fields defer to the receiver.
            return CheckRefEscape(node, fieldAccess.ReceiverOpt, escapeFrom, escapeTo, checkingReceiver: true, diagnostics: diagnostics);
        }

        private bool CheckFieldLikeEventRefEscape(SyntaxNode node, BoundEventAccess eventAccess, uint escapeFrom, uint escapeTo, BindingDiagnosticBag diagnostics)
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
    }

    internal partial class Binder
    {
        private bool CheckEventValueKind(BoundEventAccess boundEvent, BindValueKind valueKind, BindingDiagnosticBag diagnostics)
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

                if (ReportUseSite(eventSymbol, diagnostics, eventSyntax))
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
                else if (ReportUseSite(eventSymbol, diagnostics, eventSyntax))
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
                        if (valueKind == BindValueKind.RefOrOut)
                        {
                            Error(diagnostics, ErrorCode.ERR_WinRtEventPassedByRef, eventSyntax);
                        }
                        else
                        {
                            Error(diagnostics, GetStandardLvalueError(valueKind), eventSyntax, eventSymbol);
                        }
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

        private bool CheckIsValidReceiverForVariable(SyntaxNode node, BoundExpression receiver, BindValueKind kind, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(receiver != null);
            return Flags.Includes(BinderFlags.ObjectInitializerMember) && receiver.Kind == BoundKind.ObjectOrCollectionValuePlaceholder ||
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
            return symbol.RequiresInstanceReceiver()
                && symbol.Kind != SymbolKind.Event
                && receiver?.Type?.IsValueType == true;
        }

        protected bool CheckMethodReturnValueKind(
            MethodSymbol methodSymbol,
            SyntaxNode callSyntaxOpt,
            SyntaxNode node,
            BindValueKind valueKind,
            bool checkingReceiver,
            BindingDiagnosticBag diagnostics)
        {
            // A call can only be a variable if it returns by reference. If this is the case,
            // whether or not it is a valid variable depends on whether or not the call is the
            // RHS of a return or an assign by reference:
            // - If call is used in a context demanding ref-returnable reference all of its ref
            //   inputs must be ref-returnable

            if (RequiresVariable(valueKind) && methodSymbol.RefKind == RefKind.None)
            {
                if (checkingReceiver)
                {
                    // Error is associated with expression, not node which may be distinct.
                    Error(diagnostics, ErrorCode.ERR_ReturnNotLValue, callSyntaxOpt, methodSymbol);
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

            if (RequiresRefAssignableVariable(valueKind))
            {
                Error(diagnostics, ErrorCode.ERR_RefLocalOrParamExpected, node);
                return false;
            }

            return true;

        }

        private bool CheckPropertyValueKind(SyntaxNode node, BoundExpression expr, BindValueKind valueKind, bool checkingReceiver, BindingDiagnosticBag diagnostics)
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

                    Debug.Assert(propertySymbol.TypeWithAnnotations.HasType);
                    Error(diagnostics, ErrorCode.ERR_ReturnNotLValue, expr.Syntax, propertySymbol);
                }
                else if (valueKind == BindValueKind.RefOrOut)
                {
                    Error(diagnostics, ErrorCode.ERR_RefProperty, node);
                }
                else
                {
                    Error(diagnostics, GetStandardLvalueError(valueKind), node);
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

                if (setMethod is null)
                {
                    var containing = this.ContainingMemberOrLambda;
                    if (!AccessingAutoPropertyFromConstructor(receiver, propertySymbol, containing)
                        && !isAllowedDespiteReadonly(receiver))
                    {
                        Error(diagnostics, ErrorCode.ERR_AssgReadonlyProp, node, propertySymbol);
                        return false;
                    }
                }
                else
                {
                    if (setMethod.IsInitOnly)
                    {
                        if (!isAllowedInitOnlySet(receiver))
                        {
                            Error(diagnostics, ErrorCode.ERR_AssignmentInitOnly, node, propertySymbol);
                            return false;
                        }

                        if (setMethod.DeclaringCompilation != this.Compilation)
                        {
                            // an error would have already been reported on declaring an init-only setter
                            CheckFeatureAvailability(node, MessageID.IDS_FeatureInitOnlySetters, diagnostics);
                        }
                    }

                    var accessThroughType = this.GetAccessThroughType(receiver);
                    bool failedThroughTypeCheck;
                    CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
                    bool isAccessible = this.IsAccessible(setMethod, accessThroughType, out failedThroughTypeCheck, ref useSiteInfo);
                    diagnostics.Add(node, useSiteInfo);

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

                    var setValueKind = setMethod.IsEffectivelyReadOnly ? BindValueKind.RValue : BindValueKind.Assignable;
                    if (RequiresVariableReceiver(receiver, setMethod) && !CheckIsValidReceiverForVariable(node, receiver, setValueKind, diagnostics))
                    {
                        return false;
                    }

                    if (IsBadBaseAccess(node, receiver, setMethod, diagnostics, propertySymbol) ||
                        reportUseSite(setMethod))
                    {
                        return false;
                    }

                    CheckReceiverAndRuntimeSupportForSymbolAccess(node, receiver, setMethod, diagnostics);
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
                else
                {
                    var accessThroughType = this.GetAccessThroughType(receiver);
                    bool failedThroughTypeCheck;
                    CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics);
                    bool isAccessible = this.IsAccessible(getMethod, accessThroughType, out failedThroughTypeCheck, ref useSiteInfo);
                    diagnostics.Add(node, useSiteInfo);

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

                    CheckImplicitThisCopyInReadOnlyMember(receiver, getMethod, diagnostics);
                    ReportDiagnosticsIfObsolete(diagnostics, getMethod, node, receiver?.Kind == BoundKind.BaseReference);

                    if (IsBadBaseAccess(node, receiver, getMethod, diagnostics, propertySymbol) ||
                        reportUseSite(getMethod))
                    {
                        return false;
                    }

                    CheckReceiverAndRuntimeSupportForSymbolAccess(node, receiver, getMethod, diagnostics);
                }
            }

            if (RequiresRefAssignableVariable(valueKind))
            {
                Error(diagnostics, ErrorCode.ERR_RefLocalOrParamExpected, node);
                return false;
            }

            return true;

            bool reportUseSite(MethodSymbol accessor)
            {
                UseSiteInfo<AssemblySymbol> useSiteInfo = accessor.GetUseSiteInfo();
                if (!object.Equals(useSiteInfo.DiagnosticInfo, propertySymbol.GetUseSiteInfo().DiagnosticInfo))
                {
                    return diagnostics.Add(useSiteInfo, propertySyntax);
                }
                else
                {
                    diagnostics.AddDependencies(useSiteInfo);
                }

                return false;
            }

            static bool isAllowedDespiteReadonly(BoundExpression receiver)
            {
                // ok: anonymousType with { Property = ... }
                if (receiver is BoundObjectOrCollectionValuePlaceholder && receiver.Type.IsAnonymousType)
                {
                    return true;
                }

                return false;
            }

            bool isAllowedInitOnlySet(BoundExpression receiver)
            {
                // ok: new C() { InitOnlyProperty = ... }
                // bad: { ... = { InitOnlyProperty = ... } }
                if (receiver is BoundObjectOrCollectionValuePlaceholder placeholder)
                {
                    return placeholder.IsNewInstance;
                }

                // bad: other.InitOnlyProperty = ...
                if (!(receiver is BoundThisReference || receiver is BoundBaseReference))
                {
                    return false;
                }

                var containingMember = ContainingMemberOrLambda;
                if (!(containingMember is MethodSymbol method))
                {
                    return false;
                }

                if (method.MethodKind == MethodKind.Constructor || method.IsInitOnly)
                {
                    // ok: setting on `this` or `base` from an instance constructor or init-only setter
                    return true;
                }

                return false;
            }
        }

        private bool IsBadBaseAccess(SyntaxNode node, BoundExpression receiverOpt, Symbol member, BindingDiagnosticBag diagnostics,
                                     Symbol propertyOrEventSymbolOpt = null)
        {
            Debug.Assert(member.Kind != SymbolKind.Property);
            Debug.Assert(member.Kind != SymbolKind.Event);

            if (receiverOpt?.Kind == BoundKind.BaseReference && member.IsAbstract)
            {
                Error(diagnostics, ErrorCode.ERR_AbstractBaseCall, node, propertyOrEventSymbolOpt ?? member);
                return true;
            }

            return false;
        }
    }

    internal partial class RefSafetyAnalysis
    {
        internal uint GetInterpolatedStringHandlerConversionEscapeScope(
            BoundExpression expression,
            uint scopeOfTheContainingExpression)
        {
            var data = expression.GetInterpolatedStringHandlerData();
#if DEBUG
            // VisitArgumentsAndGetArgumentPlaceholders() does not visit data.Construction
            // since that expression does not introduce locals or placeholders that are needed
            // by GetValEscape() or CheckValEscape(), so we disable tracking here.
            var previousVisited = _visited;
            _visited = null;
#endif
            uint escapeScope = GetValEscape(data.Construction, scopeOfTheContainingExpression);
#if DEBUG
            _visited = previousVisited;
#endif

            var arguments = ArrayBuilder<BoundExpression>.GetInstance();
            GetInterpolatedStringHandlerArgumentsForEscape(expression, arguments);

            foreach (var argument in arguments)
            {
                uint argEscape = GetValEscape(argument, scopeOfTheContainingExpression);
                escapeScope = Math.Max(escapeScope, argEscape);
            }

            arguments.Free();
            return escapeScope;
        }

#nullable enable

        /// <summary>
        /// Computes the scope to which the given invocation can escape
        /// NOTE: the escape scope for ref and val escapes is the same for invocations except for trivial cases (ordinary type returned by val) 
        ///       where escape is known otherwise. Therefore we do not have two ref/val variants of this.
        ///       
        /// NOTE: we need scopeOfTheContainingExpression as some expressions such as optional <c>in</c> parameters or <c>ref dynamic</c> behave as 
        ///       local variables declared at the scope of the invocation.
        /// </summary>
        private uint GetInvocationEscapeScope(
            Symbol symbol,
            BoundExpression? receiver,
            ThreeState receiverIsSubjectToCloning,
            ImmutableArray<ParameterSymbol> parameters,
            ImmutableArray<BoundExpression> argsOpt,
            ImmutableArray<RefKind> argRefKindsOpt,
            ImmutableArray<int> argsToParamsOpt,
            uint scopeOfTheContainingExpression,
            bool isRefEscape
        )
        {
#if DEBUG
            Debug.Assert(AllParametersConsideredInEscapeAnalysisHaveArguments(argsOpt, parameters, argsToParamsOpt));
#endif

            if (UseUpdatedEscapeRulesForInvocation(symbol))
            {
                return GetInvocationEscapeWithUpdatedRules(symbol, receiver, receiverIsSubjectToCloning, parameters, argsOpt, argRefKindsOpt, argsToParamsOpt, scopeOfTheContainingExpression, isRefEscape);
            }

            // SPEC: (also applies to the CheckInvocationEscape counterpart)
            //
            //            An lvalue resulting from a ref-returning method invocation e1.M(e2, ...) is ref-safe - to - escape the smallest of the following scopes:
            //•	The entire enclosing method
            //•	the ref-safe-to-escape of all ref/out/in argument expressions(excluding the receiver)
            //•	the safe-to - escape of all argument expressions(including the receiver)
            //
            //            An rvalue resulting from a method invocation e1.M(e2, ...) is safe - to - escape from the smallest of the following scopes:
            //•	The entire enclosing method
            //•	the safe-to-escape of all argument expressions(including the receiver)
            //

            if (!symbol.RequiresInstanceReceiver())
            {
                // ignore receiver when symbol is static
                receiver = null;
            }

            //by default it is safe to escape
            uint escapeScope = CallingMethodScope;

            var escapeArguments = ArrayBuilder<EscapeArgument>.GetInstance();
            GetInvocationArgumentsForEscape(
                symbol,
                receiver: null, // receiver handled explicitly below
                receiverIsSubjectToCloning: ThreeState.Unknown,
                parameters,
                argsOpt,
                argRefKindsOpt,
                argsToParamsOpt,
                // ref kinds of varargs are not interesting here. 
                // __refvalue is not ref-returnable, so ref varargs can't come back from a call
                ignoreArglistRefKinds: true,
                mixableArguments: null,
                escapeArguments);

            try
            {
                foreach (var (parameter, argument, effectiveRefKind) in escapeArguments)
                {
                    // ref escape scope is the narrowest of 
                    // - ref escape of all byref arguments
                    // - val escape of all byval arguments  (ref-like values can be unwrapped into refs, so treat val escape of values as possible ref escape of the result)
                    //
                    // val escape scope is the narrowest of 
                    // - val escape of all byval arguments  (refs cannot be wrapped into values, so their ref escape is irrelevant, only use val escapes)

                    var argEscape = effectiveRefKind != RefKind.None && isRefEscape ?
                                        GetRefEscape(argument, scopeOfTheContainingExpression) :
                                        GetValEscape(argument, scopeOfTheContainingExpression);

                    escapeScope = Math.Max(escapeScope, argEscape);

                    if (escapeScope >= scopeOfTheContainingExpression)
                    {
                        // can't get any worse
                        return escapeScope;
                    }
                }
            }
            finally
            {
                escapeArguments.Free();
            }

            // check receiver if ref-like
            if (receiver?.Type?.IsRefLikeType == true)
            {
                escapeScope = Math.Max(escapeScope, GetValEscape(receiver, scopeOfTheContainingExpression));
            }

            return escapeScope;
        }

        private uint GetInvocationEscapeWithUpdatedRules(
            Symbol symbol,
            BoundExpression? receiver,
            ThreeState receiverIsSubjectToCloning,
            ImmutableArray<ParameterSymbol> parameters,
            ImmutableArray<BoundExpression> argsOpt,
            ImmutableArray<RefKind> argRefKindsOpt,
            ImmutableArray<int> argsToParamsOpt,
            uint scopeOfTheContainingExpression,
            bool isRefEscape)
        {
            //by default it is safe to escape
            uint escapeScope = CallingMethodScope;

            var argsAndParamsAll = ArrayBuilder<EscapeValue>.GetInstance();
            GetFilteredInvocationArgumentsForEscapeWithUpdatedRules(
                symbol,
                receiver,
                receiverIsSubjectToCloning,
                parameters,
                argsOpt,
                argRefKindsOpt,
                argsToParamsOpt,
                isRefEscape,
                ignoreArglistRefKinds: true, // https://github.com/dotnet/roslyn/issues/63325: for compatibility with C#10 implementation.
                argsAndParamsAll);

            var returnsRefToRefStruct = ReturnsRefToRefStruct(symbol);
            foreach (var (param, argument, _, isArgumentRefEscape) in argsAndParamsAll)
            {
                // SPEC:
                // If `M()` does return ref-to-ref-struct, the *safe-to-escape* is the same as the *safe-to-escape* of all arguments which are ref-to-ref-struct. It is an error if there are multiple arguments with different *safe-to-escape* because of *method arguments must match*.
                // If `M()` does return ref-to-ref-struct, the *ref-safe-to-escape* is the narrowest *ref-safe-to-escape* contributed by all arguments which are ref-to-ref-struct.
                //
                if (!returnsRefToRefStruct
                    || (param is null or { RefKind: not RefKind.None, Type.IsRefLikeType: true } && isArgumentRefEscape == isRefEscape))
                {
                    uint argEscape = isArgumentRefEscape ?
                        GetRefEscape(argument, scopeOfTheContainingExpression) :
                        GetValEscape(argument, scopeOfTheContainingExpression);

                    escapeScope = Math.Max(escapeScope, argEscape);
                    if (escapeScope >= scopeOfTheContainingExpression)
                    {
                        // can't get any worse
                        break;
                    }
                }
            }
            argsAndParamsAll.Free();

            return escapeScope;
        }

        private static bool ReturnsRefToRefStruct(Symbol symbol)
        {
            var method = symbol switch
            {
                MethodSymbol m => m,
                // We are only getting the method in order to handle a special condition where the method returns by-ref.
                // It is an error for a property to have a setter and return by-ref, so we only bother looking for a getter here.
                PropertySymbol p => p.GetMethod,
                _ => null
            };
            return method is { RefKind: not RefKind.None, ReturnType.IsRefLikeType: true };
        }

        /// <summary>
        /// Validates whether given invocation can allow its results to escape from <paramref name="escapeFrom"/> level to <paramref name="escapeTo"/> level.
        /// The result indicates whether the escape is possible. 
        /// Additionally, the method emits diagnostics (possibly more than one, recursively) that would help identify the cause for the failure.
        /// 
        /// NOTE: we need scopeOfTheContainingExpression as some expressions such as optional <c>in</c> parameters or <c>ref dynamic</c> behave as 
        ///       local variables declared at the scope of the invocation.
        /// </summary>
        private bool CheckInvocationEscape(
            SyntaxNode syntax,
            Symbol symbol,
            BoundExpression? receiver,
            ThreeState receiverIsSubjectToCloning,
            ImmutableArray<ParameterSymbol> parameters,
            ImmutableArray<BoundExpression> argsOpt,
            ImmutableArray<RefKind> argRefKindsOpt,
            ImmutableArray<int> argsToParamsOpt,
            bool checkingReceiver,
            uint escapeFrom,
            uint escapeTo,
            BindingDiagnosticBag diagnostics,
            bool isRefEscape
        )
        {
#if DEBUG
            Debug.Assert(AllParametersConsideredInEscapeAnalysisHaveArguments(argsOpt, parameters, argsToParamsOpt));
#endif

            if (UseUpdatedEscapeRulesForInvocation(symbol))
            {
                return CheckInvocationEscapeWithUpdatedRules(syntax, symbol, receiver, receiverIsSubjectToCloning, parameters, argsOpt, argRefKindsOpt, argsToParamsOpt, checkingReceiver, escapeFrom, escapeTo, diagnostics, isRefEscape);
            }

            // SPEC: 
            //            In a method invocation, the following constraints apply:
            //•	If there is a ref or out argument to a ref struct type (including the receiver), with safe-to-escape E1, then
            //  o no ref or out argument(excluding the receiver and arguments of ref-like types) may have a narrower ref-safe-to-escape than E1; and
            //  o   no argument(including the receiver) may have a narrower safe-to-escape than E1.

            if (!symbol.RequiresInstanceReceiver())
            {
                // ignore receiver when symbol is static
                receiver = null;
            }

            var escapeArguments = ArrayBuilder<EscapeArgument>.GetInstance();
            GetInvocationArgumentsForEscape(
                symbol,
                receiver: null, // receiver handled explicitly below
                receiverIsSubjectToCloning: ThreeState.Unknown,
                parameters,
                argsOpt,
                argRefKindsOpt,
                argsToParamsOpt,
                // ref kinds of varargs are not interesting here. 
                // __refvalue is not ref-returnable, so ref varargs can't come back from a call
                ignoreArglistRefKinds: true,
                mixableArguments: null,
                escapeArguments);

            try
            {
                foreach (var (parameter, argument, effectiveRefKind) in escapeArguments)
                {
                    // ref escape scope is the narrowest of 
                    // - ref escape of all byref arguments
                    // - val escape of all byval arguments  (ref-like values can be unwrapped into refs, so treat val escape of values as possible ref escape of the result)
                    //
                    // val escape scope is the narrowest of 
                    // - val escape of all byval arguments  (refs cannot be wrapped into values, so their ref escape is irrelevant, only use val escapes)

                    var valid = effectiveRefKind != RefKind.None && isRefEscape ?
                                        CheckRefEscape(argument.Syntax, argument, escapeFrom, escapeTo, false, diagnostics) :
                                        CheckValEscape(argument.Syntax, argument, escapeFrom, escapeTo, false, diagnostics);

                    if (!valid)
                    {
                        if (symbol is not SignatureOnlyMethodSymbol)
                        {
                            ReportInvocationEscapeError(syntax, symbol, parameter, checkingReceiver, diagnostics);
                        }

                        return false;
                    }
                }
            }
            finally
            {
                escapeArguments.Free();
            }

            // check receiver if ref-like
            if (receiver?.Type?.IsRefLikeType == true)
            {
                return CheckValEscape(receiver.Syntax, receiver, escapeFrom, escapeTo, false, diagnostics);
            }

            return true;
        }

        private bool CheckInvocationEscapeWithUpdatedRules(
            SyntaxNode syntax,
            Symbol symbol,
            BoundExpression? receiver,
            ThreeState receiverIsSubjectToCloning,
            ImmutableArray<ParameterSymbol> parameters,
            ImmutableArray<BoundExpression> argsOpt,
            ImmutableArray<RefKind> argRefKindsOpt,
            ImmutableArray<int> argsToParamsOpt,
            bool checkingReceiver,
            uint escapeFrom,
            uint escapeTo,
            BindingDiagnosticBag diagnostics,
            bool isRefEscape)
        {
            bool result = true;

            var argsAndParamsAll = ArrayBuilder<EscapeValue>.GetInstance();
            GetFilteredInvocationArgumentsForEscapeWithUpdatedRules(
                symbol,
                receiver,
                receiverIsSubjectToCloning,
                parameters,
                argsOpt,
                argRefKindsOpt,
                argsToParamsOpt,
                isRefEscape,
                ignoreArglistRefKinds: true, // https://github.com/dotnet/roslyn/issues/63325: for compatibility with C#10 implementation.
                argsAndParamsAll);

            var returnsRefToRefStruct = ReturnsRefToRefStruct(symbol);
            foreach (var (param, argument, _, isArgumentRefEscape) in argsAndParamsAll)
            {
                // SPEC:
                // If `M()` does return ref-to-ref-struct, the *safe-to-escape* is the same as the *safe-to-escape* of all arguments which are ref-to-ref-struct. It is an error if there are multiple arguments with different *safe-to-escape* because of *method arguments must match*.
                // If `M()` does return ref-to-ref-struct, the *ref-safe-to-escape* is the narrowest *ref-safe-to-escape* contributed by all arguments which are ref-to-ref-struct.
                //
                if (!returnsRefToRefStruct
                    || (param is null or { RefKind: not RefKind.None, Type.IsRefLikeType: true } && isArgumentRefEscape == isRefEscape))
                {
                    bool valid = isArgumentRefEscape ?
                        CheckRefEscape(argument.Syntax, argument, escapeFrom, escapeTo, false, diagnostics) :
                        CheckValEscape(argument.Syntax, argument, escapeFrom, escapeTo, false, diagnostics);

                    if (!valid)
                    {
                        // For consistency with C#10 implementation, we don't report an additional error
                        // for the receiver. (In both implementations, the call to Check*Escape() above
                        // will have reported a specific escape error for the receiver though.)
                        if ((object)((argument as BoundCapturedReceiverPlaceholder)?.Receiver ?? argument) != receiver && symbol is not SignatureOnlyMethodSymbol)
                        {
                            ReportInvocationEscapeError(syntax, symbol, param, checkingReceiver, diagnostics);
                        }
                        result = false;
                        break;
                    }
                }
            }
            argsAndParamsAll.Free();

            return result;
        }

        /// <summary>
        /// Returns the set of arguments to be considered for escape analysis of a method invocation.
        /// Each argument is returned with the corresponding parameter and ref kind. Arguments are not
        /// filtered - all arguments are included exactly once in the array, and the caller is responsible for
        /// determining which arguments affect escape analysis. This method is used for method invocation
        /// analysis, regardless of whether UseUpdatedEscapeRules is set.
        /// </summary>
        private void GetInvocationArgumentsForEscape(
            Symbol symbol,
            BoundExpression? receiver,
            ThreeState receiverIsSubjectToCloning,
            ImmutableArray<ParameterSymbol> parameters,
            ImmutableArray<BoundExpression> argsOpt,
            ImmutableArray<RefKind> argRefKindsOpt,
            ImmutableArray<int> argsToParamsOpt,
            bool ignoreArglistRefKinds,
            ArrayBuilder<MixableDestination>? mixableArguments,
            ArrayBuilder<EscapeArgument> escapeArguments)
        {
            if (receiver is { })
            {
                var method = symbol switch
                {
                    MethodSymbol m => m,
                    PropertySymbol p => p.GetMethod ?? p.SetMethod,
                    _ => throw ExceptionUtilities.UnexpectedValue(symbol)
                };

                Debug.Assert(receiver.Type is { });
                Debug.Assert(receiverIsSubjectToCloning != ThreeState.Unknown);
                if (receiverIsSubjectToCloning == ThreeState.True)
                {
                    Debug.Assert(receiver is not BoundValuePlaceholderBase && method is not null && receiver.Type?.IsValueType == true);
#if DEBUG
                    AssertVisited(receiver);
#endif
                    // Equivalent to a non-ref local with the underlying receiver as an initializer provided at declaration 
                    receiver = new BoundCapturedReceiverPlaceholder(receiver.Syntax, receiver, _localScopeDepth, receiver.Type).MakeCompilerGenerated();
                }

                var tuple = getReceiver(method, receiver);
                escapeArguments.Add(tuple);

                if (mixableArguments is not null && isMixableParameter(tuple.Parameter))
                {
                    mixableArguments.Add(new MixableDestination(tuple.Parameter, receiver));
                }
            }

            if (!argsOpt.IsDefault)
            {
                for (int argIndex = 0; argIndex < argsOpt.Length; argIndex++)
                {
                    var argument = argsOpt[argIndex];
                    if (argument.Kind == BoundKind.ArgListOperator)
                    {
                        Debug.Assert(argIndex == argsOpt.Length - 1);
                        // unwrap varargs and process as more arguments
                        var argList = (BoundArgListOperator)argument;
                        getArgList(
                            argList.Arguments,
                            ignoreArglistRefKinds ? default : argList.ArgumentRefKindsOpt,
                            mixableArguments,
                            escapeArguments);
                        break;
                    }

                    var parameter = argIndex < parameters.Length ?
                        parameters[argsToParamsOpt.IsDefault ? argIndex : argsToParamsOpt[argIndex]] :
                        null;

                    if (mixableArguments is not null
                        && isMixableParameter(parameter)
                        // assume any expression variable is a valid mixing destination,
                        // since we will infer a legal val-escape for it (if it doesn't already have a narrower one).
                        && isMixableArgument(argument))
                    {
                        mixableArguments.Add(new MixableDestination(parameter, argument));
                    }

                    var refKind = parameter?.RefKind ?? RefKind.None;
                    if (!argRefKindsOpt.IsDefault)
                    {
                        refKind = argRefKindsOpt[argIndex];
                    }
                    if (refKind == RefKind.None &&
                        parameter?.RefKind is RefKind.In or RefKind.RefReadOnlyParameter)
                    {
                        refKind = parameter.RefKind;
                    }

                    escapeArguments.Add(new EscapeArgument(parameter, argument, refKind));
                }
            }

            static bool isMixableParameter([NotNullWhen(true)] ParameterSymbol? parameter) =>
                parameter is not null &&
                parameter.Type.IsRefLikeType &&
                parameter.RefKind.IsWritableReference();

            static bool isMixableArgument(BoundExpression argument)
            {
                if (argument is BoundDeconstructValuePlaceholder { VariableSymbol: not null } or BoundLocal { DeclarationKind: not BoundLocalDeclarationKind.None })
                {
                    return false;
                }
                if (argument.IsDiscardExpression())
                {
                    return false;
                }
                return true;
            }

            static EscapeArgument getReceiver(MethodSymbol? method, BoundExpression receiver)
            {
                if (method is FunctionPointerMethodSymbol)
                {
                    return new EscapeArgument(parameter: null, receiver, RefKind.None);
                }

                var refKind = RefKind.None;
                ParameterSymbol? thisParameter = null;
                if (method is not null &&
                    method.TryGetThisParameter(out thisParameter) &&
                    thisParameter is not null)
                {
                    refKind = thisParameter.RefKind;
                }

                return new EscapeArgument(thisParameter, receiver, refKind);
            }

            static void getArgList(
                ImmutableArray<BoundExpression> argsOpt,
                ImmutableArray<RefKind> argRefKindsOpt,
                ArrayBuilder<MixableDestination>? mixableArguments,
                ArrayBuilder<EscapeArgument> escapeArguments)
            {
                for (int argIndex = 0; argIndex < argsOpt.Length; argIndex++)
                {
                    var argument = argsOpt[argIndex];
                    var refKind = argRefKindsOpt.IsDefault ? RefKind.None : argRefKindsOpt[argIndex];
                    escapeArguments.Add(new EscapeArgument(parameter: null, argument, refKind, isArgList: true));

                    if (refKind == RefKind.Ref && mixableArguments is not null)
                    {
                        mixableArguments.Add(new MixableDestination(argument, EscapeLevel.CallingMethod));
                    }
                }
            }
        }

        /// <summary>
        /// Returns the set of arguments to be considered for escape analysis of a method
        /// invocation. Each argument is returned with the correponding parameter and
        /// whether analysis should consider value or ref escape. Not all method arguments
        /// are included, and some arguments may be included twice - once for value, once for ref.
        /// </summary>
        private void GetFilteredInvocationArgumentsForEscapeWithUpdatedRules(
            Symbol symbol,
            BoundExpression? receiver,
            ThreeState receiverIsSubjectToCloning,
            ImmutableArray<ParameterSymbol> parameters,
            ImmutableArray<BoundExpression> argsOpt,
            ImmutableArray<RefKind> argRefKindsOpt,
            ImmutableArray<int> argsToParamsOpt,
            bool isInvokedWithRef,
            bool ignoreArglistRefKinds,
            ArrayBuilder<EscapeValue> escapeValues)
        {
            // This code is attempting to implement the following portion of the spec. Essentially if we're not 
            // either invoking a method by ref or have a ref struct return then there is no need to consider the 
            // argument escape scopes when calculating the return escape scope.
            //
            // > A value resulting from a method invocation `e1.M(e2, ...)` is *safe-to-escape* from the narrowest of the following scopes:
            // > 1. The *calling method*
            // > 2. When the return is a `ref struct` the *safe-to-escape* contributed by all argument expressions
            // > 3. When the return is a `ref struct` the *ref-safe-to-escape* contributed by all `ref` arguments
            // 
            // The `ref` calling rules can be simplified to:
            // 
            // > A value resulting from a method invocation `ref e1.M(e2, ...)` is *ref-safe-to-escape* the narrowest of the following scopes:
            // > 1. The *calling method*
            // > 2. The *safe-to-escape* contributed by all argument expressions
            // > 3. The *ref-safe-to-escape* contributed by all `ref` arguments

            // If we're not invoking with ref or returning a ref struct then the spec does not consider
            // any arguments hence the filter is always empty.
            if (!isInvokedWithRef && !hasRefLikeReturn(symbol))
            {
                return;
            }

            GetEscapeValuesForUpdatedRules(
                symbol,
                receiver,
                receiverIsSubjectToCloning,
                parameters,
                argsOpt,
                argRefKindsOpt,
                argsToParamsOpt,
                ignoreArglistRefKinds,
                mixableArguments: null,
                escapeValues);

            static bool hasRefLikeReturn(Symbol symbol)
            {
                switch (symbol)
                {
                    case MethodSymbol method:
                        if (method.MethodKind == MethodKind.Constructor)
                        {
                            return method.ContainingType.IsRefLikeType;
                        }

                        return method.ReturnType.IsRefLikeType;
                    case PropertySymbol property:
                        return property.Type.IsRefLikeType;
                    default:
                        return false;
                }
            }
        }

        /// <summary>
        /// Returns the set of <see cref="EscapeValue"/> to an invocation that impact ref analysis. 
        /// This will filter out everything that could never meaningfully contribute to ref analysis. For
        /// example: 
        ///   - For ref arguments it will return an <see cref="EscapeValue"/> for both ref and 
        ///     value escape (if appropriate based on scoped-ness of associated parameters).
        ///   - It will remove value escape for args which correspond to scoped parameters. 
        ///   - It will remove value escape for non-ref struct.
        ///   - It will remove ref escape for args which correspond to scoped refs.
        /// Optionally this will also return all of the <see cref="MixableDestination" /> that 
        /// result from this invocation. That is useful for MAMM analysis.
        /// </summary>
        private void GetEscapeValuesForUpdatedRules(
            Symbol symbol,
            BoundExpression? receiver,
            ThreeState receiverIsSubjectToCloning,
            ImmutableArray<ParameterSymbol> parameters,
            ImmutableArray<BoundExpression> argsOpt,
            ImmutableArray<RefKind> argRefKindsOpt,
            ImmutableArray<int> argsToParamsOpt,
            bool ignoreArglistRefKinds,
            ArrayBuilder<MixableDestination>? mixableArguments,
            ArrayBuilder<EscapeValue> escapeValues)
        {
            if (!symbol.RequiresInstanceReceiver())
            {
                // ignore receiver when symbol is static
                receiver = null;
            }

            var escapeArguments = ArrayBuilder<EscapeArgument>.GetInstance();
            GetInvocationArgumentsForEscape(
                symbol,
                receiver,
                receiverIsSubjectToCloning,
                parameters,
                argsOpt,
                argRefKindsOpt,
                argsToParamsOpt,
                ignoreArglistRefKinds,
                mixableArguments,
                escapeArguments);

            foreach (var (parameter, argument, refKind) in escapeArguments)
            {
                // This means it's part of an __arglist or function pointer receiver. 
                if (parameter is null)
                {
                    if (refKind != RefKind.None)
                    {
                        escapeValues.Add(new EscapeValue(parameter: null, argument, EscapeLevel.ReturnOnly, isRefEscape: true));
                    }

                    if (argument.Type?.IsRefLikeType == true)
                    {
                        escapeValues.Add(new EscapeValue(parameter: null, argument, EscapeLevel.CallingMethod, isRefEscape: false));
                    }

                    continue;
                }

                if (parameter.Type.IsRefLikeType && parameter.RefKind != RefKind.Out && GetParameterValEscapeLevel(parameter) is { } valEscapeLevel)
                {
                    escapeValues.Add(new EscapeValue(parameter, argument, valEscapeLevel, isRefEscape: false));
                }

                // It's important to check values then references. Flipping will change the set of errors 
                // produced by MAMM because of the CheckRefEscape / CheckValEscape calls.
                if (parameter.RefKind != RefKind.None && GetParameterRefEscapeLevel(parameter) is { } refEscapeLevel)
                {
                    escapeValues.Add(new EscapeValue(parameter, argument, refEscapeLevel, isRefEscape: true));
                }
            }

            escapeArguments.Free();
        }

        private static string GetInvocationParameterName(ParameterSymbol? parameter)
        {
            if (parameter is null)
            {
                return "__arglist";
            }
            string parameterName = parameter.Name;
            if (string.IsNullOrEmpty(parameterName))
            {
                parameterName = parameter.Ordinal.ToString();
            }
            return parameterName;
        }

        private static void ReportInvocationEscapeError(
            SyntaxNode syntax,
            Symbol symbol,
            ParameterSymbol? parameter,
            bool checkingReceiver,
            BindingDiagnosticBag diagnostics)
        {
            ErrorCode errorCode = GetStandardCallEscapeError(checkingReceiver);
            string parameterName = GetInvocationParameterName(parameter);
            Error(diagnostics, errorCode, syntax, symbol, parameterName);
        }

        private bool UseUpdatedEscapeRulesForInvocation(Symbol symbol)
        {
            var method = symbol switch
            {
                MethodSymbol m => m,
                PropertySymbol p => p.GetMethod ?? p.SetMethod,
                _ => throw ExceptionUtilities.UnexpectedValue(symbol)
            };
            return method?.UseUpdatedEscapeRules == true;
        }

        private bool ShouldInferDeclarationExpressionValEscape(BoundExpression argument, [NotNullWhen(true)] out SourceLocalSymbol? localSymbol)
        {
            var symbol = argument switch
            {
                BoundDeconstructValuePlaceholder p => p.VariableSymbol,
                BoundLocal { DeclarationKind: not BoundLocalDeclarationKind.None } l => l.LocalSymbol,
                _ => null
            };
            if (symbol is SourceLocalSymbol local &&
                GetLocalScopes(local).ValEscapeScope == CallingMethodScope)
            {
                localSymbol = local;
                return true;
            }
            else
            {
                // No need to infer a val escape for a global variable.
                // These are only used in top-level statements in scripting mode,
                // and since they are class fields, their scope is always CallingMethod.
                Debug.Assert(symbol is null or SourceLocalSymbol or GlobalExpressionVariable);
                localSymbol = null;
                return false;
            }
        }

        /// <summary>
        /// Validates whether the invocation is valid per no-mixing rules.
        /// Returns <see langword="false"/> when it is not valid and produces diagnostics (possibly more than one recursively) that helps to figure the reason.
        /// </summary>
        private bool CheckInvocationArgMixing(
            SyntaxNode syntax,
            Symbol symbol,
            BoundExpression? receiverOpt,
            ThreeState receiverIsSubjectToCloning,
            ImmutableArray<ParameterSymbol> parameters,
            ImmutableArray<BoundExpression> argsOpt,
            ImmutableArray<RefKind> argRefKindsOpt,
            ImmutableArray<int> argsToParamsOpt,
            uint scopeOfTheContainingExpression,
            BindingDiagnosticBag diagnostics)
        {
            if (UseUpdatedEscapeRulesForInvocation(symbol))
            {
                return CheckInvocationArgMixingWithUpdatedRules(syntax, symbol, receiverOpt, receiverIsSubjectToCloning, parameters, argsOpt, argRefKindsOpt, argsToParamsOpt, scopeOfTheContainingExpression, diagnostics);
            }

            // SPEC:
            // In a method invocation, the following constraints apply:
            // - If there is a ref or out argument of a ref struct type (including the receiver), with safe-to-escape E1, then
            // - no argument (including the receiver) may have a narrower safe-to-escape than E1.

            if (!symbol.RequiresInstanceReceiver())
            {
                // ignore receiver when symbol is static
                receiverOpt = null;
            }

            // widest possible escape via writeable ref-like receiver or ref/out argument.
            uint escapeTo = scopeOfTheContainingExpression;

            // collect all writeable ref-like arguments, including receiver
            var receiverType = receiverOpt?.Type;
            if (receiverType?.IsRefLikeType == true && !IsReceiverRefReadOnly(symbol))
            {
                escapeTo = GetValEscape(receiverOpt, scopeOfTheContainingExpression);
            }

            var escapeArguments = ArrayBuilder<EscapeArgument>.GetInstance();
            GetInvocationArgumentsForEscape(
                symbol,
                receiverOpt,
                receiverIsSubjectToCloning,
                parameters,
                argsOpt,
                argRefKindsOpt: default,
                argsToParamsOpt,
                ignoreArglistRefKinds: false,
                mixableArguments: null,
                escapeArguments);

            try
            {
                foreach (var (_, argument, refKind) in escapeArguments)
                {
                    if (ShouldInferDeclarationExpressionValEscape(argument, out _))
                    {
                        // assume any expression variable is a valid mixing destination,
                        // since we will infer a legal val-escape for it (if it doesn't already have a narrower one).
                        continue;
                    }

                    if (refKind.IsWritableReference()
                        && !argument.IsDiscardExpression()
                        && argument.Type?.IsRefLikeType == true)
                    {
                        escapeTo = Math.Min(escapeTo, GetValEscape(argument, scopeOfTheContainingExpression));
                    }
                }

                var hasMixingError = false;

                // track the widest scope that arguments could safely escape to.
                // use this scope as the inferred STE of declaration expressions.
                var inferredDestinationValEscape = CallingMethodScope;
                foreach (var (parameter, argument, _) in escapeArguments)
                {
                    // in the old rules, we assume that refs cannot escape into ref struct variables.
                    // e.g. in `dest = M(ref arg)`, we assume `ref arg` will not escape into `dest`, but `arg` might.
                    inferredDestinationValEscape = Math.Max(inferredDestinationValEscape, GetValEscape(argument, scopeOfTheContainingExpression));
                    if (!hasMixingError && !CheckValEscape(argument.Syntax, argument, scopeOfTheContainingExpression, escapeTo, false, diagnostics))
                    {
                        string parameterName = GetInvocationParameterName(parameter);
                        Error(diagnostics, ErrorCode.ERR_CallArgMixing, syntax, symbol, parameterName);
                        hasMixingError = true;
                    }
                }

                foreach (var (_, argument, _) in escapeArguments)
                {
                    if (ShouldInferDeclarationExpressionValEscape(argument, out var localSymbol))
                    {
                        SetLocalScopes(localSymbol, refEscapeScope: _localScopeDepth, valEscapeScope: inferredDestinationValEscape);
                    }
                }

                return !hasMixingError;
            }
            finally
            {
                escapeArguments.Free();
            }
        }

        private bool CheckInvocationArgMixingWithUpdatedRules(
            SyntaxNode syntax,
            Symbol symbol,
            BoundExpression? receiverOpt,
            ThreeState receiverIsSubjectToCloning,
            ImmutableArray<ParameterSymbol> parameters,
            ImmutableArray<BoundExpression> argsOpt,
            ImmutableArray<RefKind> argRefKindsOpt,
            ImmutableArray<int> argsToParamsOpt,
            uint scopeOfTheContainingExpression,
            BindingDiagnosticBag diagnostics)
        {
            var mixableArguments = ArrayBuilder<MixableDestination>.GetInstance();
            var escapeValues = ArrayBuilder<EscapeValue>.GetInstance();
            GetEscapeValuesForUpdatedRules(
                symbol,
                receiverOpt,
                receiverIsSubjectToCloning,
                parameters,
                argsOpt,
                argRefKindsOpt,
                argsToParamsOpt,
                ignoreArglistRefKinds: false,
                mixableArguments,
                escapeValues);

            var valid = true;
            foreach (var mixableArg in mixableArguments)
            {
                var toArgEscape = GetValEscape(mixableArg.Argument, scopeOfTheContainingExpression);
                foreach (var (fromParameter, fromArg, escapeKind, isRefEscape) in escapeValues)
                {
                    if (mixableArg.Parameter is not null && object.ReferenceEquals(mixableArg.Parameter, fromParameter))
                    {
                        continue;
                    }

                    // This checks to see if the EscapeValue could ever be assigned to this argument based 
                    // on comparing the EscapeLevel of both. If this could never be assigned due to 
                    // this then we don't need to consider it for MAMM analysis.
                    if (!mixableArg.IsAssignableFrom(escapeKind))
                    {
                        continue;
                    }

                    valid = isRefEscape
                        ? CheckRefEscape(fromArg.Syntax, fromArg, scopeOfTheContainingExpression, toArgEscape, checkingReceiver: false, diagnostics)
                        : CheckValEscape(fromArg.Syntax, fromArg, scopeOfTheContainingExpression, toArgEscape, checkingReceiver: false, diagnostics);

                    if (!valid)
                    {
                        string parameterName = GetInvocationParameterName(fromParameter);
                        Error(diagnostics, ErrorCode.ERR_CallArgMixing, syntax, symbol, parameterName);
                        break;
                    }
                }

                if (!valid)
                {
                    break;
                }
            }

            inferDeclarationExpressionValEscape();

            mixableArguments.Free();
            escapeValues.Free();
            return valid;

            void inferDeclarationExpressionValEscape()
            {
                // find the widest scope that arguments could safely escape to.
                // use this scope as the inferred STE of declaration expressions.
                var inferredDestinationValEscape = CallingMethodScope;
                foreach (var (_, fromArg, _, isRefEscape) in escapeValues)
                {
                    inferredDestinationValEscape = Math.Max(inferredDestinationValEscape, isRefEscape
                        ? GetRefEscape(fromArg, scopeOfTheContainingExpression)
                        : GetValEscape(fromArg, scopeOfTheContainingExpression));
                }

                foreach (var argument in argsOpt)
                {
                    if (ShouldInferDeclarationExpressionValEscape(argument, out var localSymbol))
                    {
                        SetLocalScopes(localSymbol, refEscapeScope: _localScopeDepth, valEscapeScope: inferredDestinationValEscape);
                    }
                }
            }
        }

        private static bool IsReceiverRefReadOnly(Symbol methodOrPropertySymbol) => methodOrPropertySymbol switch
        {
            MethodSymbol m => m.IsEffectivelyReadOnly,
            // TODO: val escape checks should be skipped for property accesses when
            // we can determine the only accessors being called are readonly.
            // For now we are pessimistic and check escape if any accessor is non-readonly.
            // Tracking in https://github.com/dotnet/roslyn/issues/35606
            PropertySymbol p => p.GetMethod?.IsEffectivelyReadOnly != false && p.SetMethod?.IsEffectivelyReadOnly != false,
            _ => throw ExceptionUtilities.UnexpectedValue(methodOrPropertySymbol)
        };

#if DEBUG
        private static bool AllParametersConsideredInEscapeAnalysisHaveArguments(
            ImmutableArray<BoundExpression> argsOpt,
            ImmutableArray<ParameterSymbol> parameters,
            ImmutableArray<int> argsToParamsOpt)
        {
            if (parameters.IsDefaultOrEmpty) return true;

            var paramsMatched = BitVector.Create(parameters.Length);
            for (int argIndex = 0; argIndex < argsOpt.Length; argIndex++)
            {
                int paramIndex = argsToParamsOpt.IsDefault ? argIndex : argsToParamsOpt[argIndex];
                paramsMatched[paramIndex] = true;
            }
            for (int paramIndex = 0; paramIndex < parameters.Length; paramIndex++)
            {
                if (!paramsMatched[paramIndex])
                {
                    return false;
                }
            }
            return true;
        }
#endif

#nullable disable

        private static ErrorCode GetStandardCallEscapeError(bool checkingReceiver)
        {
            return checkingReceiver ? ErrorCode.ERR_EscapeCall2 : ErrorCode.ERR_EscapeCall;
        }
    }

    internal partial class Binder
    {
        private static void ReportReadonlyLocalError(SyntaxNode node, LocalSymbol local, BindValueKind kind, bool checkingReceiver, BindingDiagnosticBag diagnostics)
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

        private static ErrorCode GetThisLvalueError(BindValueKind kind, bool isValueType, bool isPrimaryConstructorParameter)
        {
            switch (kind)
            {
                case BindValueKind.CompoundAssignment:
                case BindValueKind.Assignable:
                    return ErrorCode.ERR_AssgReadonlyLocal;

                case BindValueKind.RefOrOut:
                    return ErrorCode.ERR_RefReadonlyLocal;

                case BindValueKind.AddressOf:
                    return ErrorCode.ERR_InvalidAddrOp;

                case BindValueKind.IncrementDecrement:
                    return isValueType ? ErrorCode.ERR_AssgReadonlyLocal : ErrorCode.ERR_IncrementLvalueExpected;

                case BindValueKind.RefReturn:
                case BindValueKind.ReadonlyRef:
                    return isPrimaryConstructorParameter ? ErrorCode.ERR_RefReturnPrimaryConstructorParameter : ErrorCode.ERR_RefReturnThis;

                case BindValueKind.RefAssignable:
                    return ErrorCode.ERR_RefLocalOrParamExpected;
            }

            if (RequiresReferenceToLocation(kind))
            {
                return ErrorCode.ERR_RefLvalueExpected;
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

                case BindValueKind.RefAssignable:
                    return ErrorCode.ERR_RefLocalOrParamExpected;
            }

            if (RequiresReferenceToLocation(kind))
            {
                return ErrorCode.ERR_QueryOutRefRangeVariable;
            }

            throw ExceptionUtilities.UnexpectedValue(kind);
        }

        private static ErrorCode GetMethodGroupOrFunctionPointerLvalueError(BindValueKind valueKind)
        {
            if (RequiresReferenceToLocation(valueKind))
            {
                return ErrorCode.ERR_RefReadonlyLocalCause;
            }

            // Cannot assign to 'W' because it is a 'method group'
            return ErrorCode.ERR_AssgReadonlyLocalCause;
        }

        private static ErrorCode GetStandardLvalueError(BindValueKind kind)
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

                case BindValueKind.RefAssignable:
                    return ErrorCode.ERR_RefLocalOrParamExpected;
            }

            if (RequiresReferenceToLocation(kind))
            {
                return ErrorCode.ERR_RefLvalueExpected;
            }

            throw ExceptionUtilities.UnexpectedValue(kind);
        }
    }

    internal partial class RefSafetyAnalysis
    {
        private static ErrorCode GetStandardRValueRefEscapeError(uint escapeTo)
        {
            if (escapeTo is CallingMethodScope or ReturnOnlyScope)
            {
                return ErrorCode.ERR_RefReturnLvalueExpected;
            }

            return ErrorCode.ERR_EscapeOther;
        }
    }

    internal partial class Binder
    {
        private static void ReportReadOnlyFieldError(FieldSymbol field, SyntaxNode node, BindValueKind kind, bool checkingReceiver, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert((object)field != null);
            Debug.Assert(field.RefKind == RefKind.None ? RequiresAssignableVariable(kind) : RequiresRefAssignableVariable(kind));
            Debug.Assert(field.Type != (object)null);

            // It's clearer to say that the address can't be taken than to say that the field can't be modified
            // (even though the latter message gives more explanation of why).
            Debug.Assert(kind != BindValueKind.AddressOf); // If this assert fails, we probably should report ErrorCode.ERR_InvalidAddrOp

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

        private static void ReportReadOnlyError(Symbol symbol, SyntaxNode node, BindValueKind kind, bool checkingReceiver, BindingDiagnosticBag diagnostics)
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
            Error(diagnostics, ReadOnlyErrors[index], node, symbolKind, new FormattedSymbol(symbol, SymbolDisplayFormat.ShortFormat));
        }
    }

    internal partial class RefSafetyAnalysis
    {
        /// <summary>
        /// Checks whether given expression can escape from the current scope to the <paramref name="escapeTo"/>.
        /// </summary>
        internal void ValidateEscape(BoundExpression expr, uint escapeTo, bool isByRef, BindingDiagnosticBag diagnostics)
        {
            // The result of escape analysis is affected by the expression's type.
            // We can't do escape analysis on expressions which lack a type, such as 'target typed new()', until they are converted.
            Debug.Assert(expr.Type is not null);

            if (isByRef)
            {
                CheckRefEscape(expr.Syntax, expr, _localScopeDepth, escapeTo, checkingReceiver: false, diagnostics: diagnostics);
            }
            else
            {
                CheckValEscape(expr.Syntax, expr, _localScopeDepth, escapeTo, checkingReceiver: false, diagnostics: diagnostics);
            }
        }

        /// <summary>
        /// Computes the widest scope depth to which the given expression can escape by reference.
        /// 
        /// NOTE: in a case if expression cannot be passed by an alias (RValue and similar), the ref-escape is scopeOfTheContainingExpression
        ///       There are few cases where RValues are permitted to be passed by reference which implies that a temporary local proxy is passed instead.
        ///       We reflect such behavior by constraining the escape value to the narrowest scope possible. 
        /// </summary>
        internal uint GetRefEscape(BoundExpression expr, uint scopeOfTheContainingExpression)
        {
#if DEBUG
            AssertVisited(expr);
#endif

            // cannot infer anything from errors
            if (expr.HasAnyErrors)
            {
                return CallingMethodScope;
            }

            // cannot infer anything from Void (broken code)
            if (expr.Type?.GetSpecialTypeSafe() == SpecialType.System_Void)
            {
                return CallingMethodScope;
            }

            // constants/literals cannot ref-escape current scope
            if (expr.ConstantValueOpt != null)
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
                    // array elements and pointer dereferencing are readwrite variables
                    return CallingMethodScope;

                case BoundKind.RefValueOperator:
                    // The undocumented __refvalue(tr, T) expression results in an lvalue of type T.
                    // for compat reasons it is not ref-returnable (since TypedReference is not val-returnable)
                    // it can, however, ref-escape to any other level (since TypedReference can val-escape to any other level)
                    return CurrentMethodScope;

                case BoundKind.DiscardExpression:
                    // same as write-only byval local
                    break;

                case BoundKind.DynamicMemberAccess:
                case BoundKind.DynamicIndexerAccess:
                    // dynamic expressions can be read and written to
                    // can even be passed by reference (which is implemented via a temp)
                    // it is not valid to escape them by reference though, so treat them as RValues here
                    break;

                case BoundKind.Parameter:
                    return GetParameterRefEscape(((BoundParameter)expr).ParameterSymbol);

                case BoundKind.Local:
                    return GetLocalScopes(((BoundLocal)expr).LocalSymbol).RefEscapeScope;

                case BoundKind.CapturedReceiverPlaceholder:
                    // Equivalent to a non-ref local with the underlying receiver as an initializer provided at declaration 
                    return ((BoundCapturedReceiverPlaceholder)expr).LocalScopeDepth;

                case BoundKind.ThisReference:
                    var thisParam = ((MethodSymbol)_symbol).ThisParameter;
                    Debug.Assert(thisParam.Type.Equals(((BoundThisReference)expr).Type, TypeCompareKind.ConsiderEverything));
                    return GetParameterRefEscape(thisParam);

                case BoundKind.ConditionalOperator:
                    var conditional = (BoundConditionalOperator)expr;

                    if (conditional.IsRef)
                    {
                        // ref conditional defers to its operands
                        return Math.Max(GetRefEscape(conditional.Consequence, scopeOfTheContainingExpression),
                                        GetRefEscape(conditional.Alternative, scopeOfTheContainingExpression));
                    }

                    // otherwise it is an RValue
                    break;

                case BoundKind.FieldAccess:
                    return GetFieldRefEscape((BoundFieldAccess)expr, scopeOfTheContainingExpression);

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
                        return CallingMethodScope;
                    }

                    // for other events defer to the receiver.
                    return GetRefEscape(eventAccess.ReceiverOpt, scopeOfTheContainingExpression);

                case BoundKind.Call:
                    {
                        var call = (BoundCall)expr;

                        var methodSymbol = call.Method;
                        if (methodSymbol.RefKind == RefKind.None)
                        {
                            break;
                        }

                        return GetInvocationEscapeScope(
                            call.Method,
                            call.ReceiverOpt,
                            call.InitialBindingReceiverIsSubjectToCloning,
                            methodSymbol.Parameters,
                            call.Arguments,
                            call.ArgumentRefKindsOpt,
                            call.ArgsToParamsOpt,
                            scopeOfTheContainingExpression,
                            isRefEscape: true);
                    }

                case BoundKind.FunctionPointerInvocation:
                    {
                        var ptrInvocation = (BoundFunctionPointerInvocation)expr;

                        var methodSymbol = ptrInvocation.FunctionPointer.Signature;
                        if (methodSymbol.RefKind == RefKind.None)
                        {
                            break;
                        }

                        return GetInvocationEscapeScope(
                            methodSymbol,
                            receiver: null,
                            receiverIsSubjectToCloning: ThreeState.Unknown,
                            methodSymbol.Parameters,
                            ptrInvocation.Arguments,
                            ptrInvocation.ArgumentRefKindsOpt,
                            argsToParamsOpt: default,
                            scopeOfTheContainingExpression,
                            isRefEscape: true);
                    }

                case BoundKind.IndexerAccess:
                    {
                        var indexerAccess = (BoundIndexerAccess)expr;
                        var indexerSymbol = indexerAccess.Indexer;

                        return GetInvocationEscapeScope(
                            indexerSymbol,
                            indexerAccess.ReceiverOpt,
                            indexerAccess.InitialBindingReceiverIsSubjectToCloning,
                            indexerSymbol.Parameters,
                            indexerAccess.Arguments,
                            indexerAccess.ArgumentRefKindsOpt,
                            indexerAccess.ArgsToParamsOpt,
                            scopeOfTheContainingExpression,
                            isRefEscape: true);
                    }

                case BoundKind.ImplicitIndexerAccess:
                    var implicitIndexerAccess = (BoundImplicitIndexerAccess)expr;

                    // Note: the Argument and LengthOrCountAccess use is purely local

                    switch (implicitIndexerAccess.IndexerOrSliceAccess)
                    {
                        case BoundIndexerAccess indexerAccess:
                            var indexerSymbol = indexerAccess.Indexer;

                            return GetInvocationEscapeScope(
                                indexerSymbol,
                                implicitIndexerAccess.Receiver,
                                indexerAccess.InitialBindingReceiverIsSubjectToCloning,
                                indexerSymbol.Parameters,
                                indexerAccess.Arguments,
                                indexerAccess.ArgumentRefKindsOpt,
                                indexerAccess.ArgsToParamsOpt,
                                scopeOfTheContainingExpression,
                                isRefEscape: true);

                        case BoundArrayAccess:
                            // array elements are readwrite variables
                            return CallingMethodScope;

                        case BoundCall call:
                            var methodSymbol = call.Method;
                            if (methodSymbol.RefKind == RefKind.None)
                            {
                                break;
                            }

                            return GetInvocationEscapeScope(
                                call.Method,
                                implicitIndexerAccess.Receiver,
                                call.InitialBindingReceiverIsSubjectToCloning,
                                methodSymbol.Parameters,
                                call.Arguments,
                                call.ArgumentRefKindsOpt,
                                call.ArgsToParamsOpt,
                                scopeOfTheContainingExpression,
                                isRefEscape: true);

                        default:
                            throw ExceptionUtilities.UnexpectedValue(implicitIndexerAccess.IndexerOrSliceAccess.Kind);
                    }
                    break;

                case BoundKind.InlineArrayAccess:
                    {
                        var elementAccess = (BoundInlineArrayAccess)expr;

                        if (elementAccess.GetItemOrSliceHelper is not (WellKnownMember.System_ReadOnlySpan_T__get_Item or WellKnownMember.System_Span_T__get_Item) || elementAccess.IsValue)
                        {
                            Debug.Assert(GetInlineArrayAccessEquivalentSignatureMethod(elementAccess, out _, out _).RefKind == RefKind.None);
                            break;
                        }

                        ImmutableArray<BoundExpression> arguments;
                        ImmutableArray<RefKind> refKinds;
                        SignatureOnlyMethodSymbol equivalentSignatureMethod = GetInlineArrayAccessEquivalentSignatureMethod(elementAccess, out arguments, out refKinds);

                        Debug.Assert(equivalentSignatureMethod.RefKind != RefKind.None);

                        return GetInvocationEscapeScope(
                            equivalentSignatureMethod,
                            receiver: null,
                            receiverIsSubjectToCloning: ThreeState.Unknown,
                            equivalentSignatureMethod.Parameters,
                            arguments,
                            refKinds,
                            argsToParamsOpt: default,
                            scopeOfTheContainingExpression,
                            isRefEscape: true);
                    }

                case BoundKind.PropertyAccess:
                    var propertyAccess = (BoundPropertyAccess)expr;

                    // not passing any arguments/parameters
                    return GetInvocationEscapeScope(
                        propertyAccess.PropertySymbol,
                        propertyAccess.ReceiverOpt,
                        propertyAccess.InitialBindingReceiverIsSubjectToCloning,
                        default,
                        default,
                        default,
                        default,
                        scopeOfTheContainingExpression,
                        isRefEscape: true);

                case BoundKind.AssignmentOperator:
                    var assignment = (BoundAssignmentOperator)expr;

                    if (!assignment.IsRef)
                    {
                        // non-ref assignments are RValues
                        break;
                    }

                    return GetRefEscape(assignment.Left, scopeOfTheContainingExpression);
            }

            // At this point we should have covered all the possible cases for anything that is not a strict RValue.
            return scopeOfTheContainingExpression;
        }

        /// <summary>
        /// A counterpart to the GetRefEscape, which validates if given escape demand can be met by the expression.
        /// The result indicates whether the escape is possible. 
        /// Additionally, the method emits diagnostics (possibly more than one, recursively) that would help identify the cause for the failure.
        /// </summary>
        internal bool CheckRefEscape(SyntaxNode node, BoundExpression expr, uint escapeFrom, uint escapeTo, bool checkingReceiver, BindingDiagnosticBag diagnostics)
        {
#if DEBUG
            AssertVisited(expr);
#endif

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

            // void references cannot escape (error should be reported somewhere)
            if (expr.Type?.GetSpecialTypeSafe() == SpecialType.System_Void)
            {
                return true;
            }

            // references to constants/literals cannot escape higher.
            if (expr.ConstantValueOpt != null)
            {
                Error(diagnostics, GetStandardRValueRefEscapeError(escapeTo), node);
                return false;
            }

            switch (expr.Kind)
            {
                case BoundKind.ArrayAccess:
                case BoundKind.PointerIndirectionOperator:
                case BoundKind.PointerElementAccess:
                    // array elements and pointer dereferencing are readwrite variables
                    return true;

                case BoundKind.RefValueOperator:
                    // The undocumented __refvalue(tr, T) expression results in an lvalue of type T.
                    // for compat reasons it is not ref-returnable (since TypedReference is not val-returnable)
                    if (escapeTo is CallingMethodScope or ReturnOnlyScope)
                    {
                        break;
                    }

                    // it can, however, ref-escape to any other level (since TypedReference can val-escape to any other level)
                    return true;

                case BoundKind.DiscardExpression:
                    // same as write-only byval local
                    break;

                case BoundKind.DynamicMemberAccess:
                case BoundKind.DynamicIndexerAccess:
                    // dynamic expressions can be read and written to
                    // can even be passed by reference (which is implemented via a temp)
                    // it is not valid to escape them by reference though.
                    break;

                case BoundKind.Parameter:
                    var parameter = (BoundParameter)expr;
                    return CheckParameterRefEscape(node, parameter, parameter.ParameterSymbol, escapeTo, checkingReceiver, diagnostics);

                case BoundKind.Local:
                    var local = (BoundLocal)expr;
                    return CheckLocalRefEscape(node, local, escapeTo, checkingReceiver, diagnostics);

                case BoundKind.CapturedReceiverPlaceholder:
                    // Equivalent to a non-ref local with the underlying receiver as an initializer provided at declaration 
                    if (((BoundCapturedReceiverPlaceholder)expr).LocalScopeDepth <= escapeTo)
                    {
                        return true;
                    }
                    break;

                case BoundKind.ThisReference:
                    var thisParam = ((MethodSymbol)_symbol).ThisParameter;
                    Debug.Assert(thisParam.Type.Equals(((BoundThisReference)expr).Type, TypeCompareKind.ConsiderEverything));
                    return CheckParameterRefEscape(node, expr, thisParam, escapeTo, checkingReceiver, diagnostics);

                case BoundKind.ConditionalOperator:
                    var conditional = (BoundConditionalOperator)expr;

                    if (conditional.IsRef)
                    {
                        return CheckRefEscape(conditional.Consequence.Syntax, conditional.Consequence, escapeFrom, escapeTo, checkingReceiver: false, diagnostics: diagnostics) &&
                               CheckRefEscape(conditional.Alternative.Syntax, conditional.Alternative, escapeFrom, escapeTo, checkingReceiver: false, diagnostics: diagnostics);
                    }

                    // report standard lvalue error
                    break;

                case BoundKind.FieldAccess:
                    var fieldAccess = (BoundFieldAccess)expr;
                    return CheckFieldRefEscape(node, fieldAccess, escapeFrom, escapeTo, diagnostics);

                case BoundKind.EventAccess:
                    var eventAccess = (BoundEventAccess)expr;
                    if (!eventAccess.IsUsableAsField)
                    {
                        // not field-like events are RValues
                        break;
                    }

                    return CheckFieldLikeEventRefEscape(node, eventAccess, escapeFrom, escapeTo, diagnostics);

                case BoundKind.Call:
                    {
                        var call = (BoundCall)expr;

                        var methodSymbol = call.Method;
                        if (methodSymbol.RefKind == RefKind.None)
                        {
                            break;
                        }

                        return CheckInvocationEscape(
                            call.Syntax,
                            methodSymbol,
                            call.ReceiverOpt,
                            call.InitialBindingReceiverIsSubjectToCloning,
                            methodSymbol.Parameters,
                            call.Arguments,
                            call.ArgumentRefKindsOpt,
                            call.ArgsToParamsOpt,
                            checkingReceiver,
                            escapeFrom,
                            escapeTo,
                            diagnostics,
                            isRefEscape: true);
                    }

                case BoundKind.IndexerAccess:
                    {
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
                            indexerAccess.InitialBindingReceiverIsSubjectToCloning,
                            indexerSymbol.Parameters,
                            indexerAccess.Arguments,
                            indexerAccess.ArgumentRefKindsOpt,
                            indexerAccess.ArgsToParamsOpt,
                            checkingReceiver,
                            escapeFrom,
                            escapeTo,
                            diagnostics,
                            isRefEscape: true);
                    }

                case BoundKind.ImplicitIndexerAccess:
                    var implicitIndexerAccess = (BoundImplicitIndexerAccess)expr;

                    // Note: the Argument and LengthOrCountAccess use is purely local

                    switch (implicitIndexerAccess.IndexerOrSliceAccess)
                    {
                        case BoundIndexerAccess indexerAccess:
                            var indexerSymbol = indexerAccess.Indexer;

                            if (indexerSymbol.RefKind == RefKind.None)
                            {
                                break;
                            }

                            return CheckInvocationEscape(
                                indexerAccess.Syntax,
                                indexerSymbol,
                                implicitIndexerAccess.Receiver,
                                indexerAccess.InitialBindingReceiverIsSubjectToCloning,
                                indexerSymbol.Parameters,
                                indexerAccess.Arguments,
                                indexerAccess.ArgumentRefKindsOpt,
                                indexerAccess.ArgsToParamsOpt,
                                checkingReceiver,
                                escapeFrom,
                                escapeTo,
                                diagnostics,
                                isRefEscape: true);

                        case BoundArrayAccess:
                            // array elements are readwrite variables
                            return true;

                        case BoundCall call:
                            var methodSymbol = call.Method;
                            if (methodSymbol.RefKind == RefKind.None)
                            {
                                break;
                            }

                            return CheckInvocationEscape(
                                call.Syntax,
                                methodSymbol,
                                implicitIndexerAccess.Receiver,
                                call.InitialBindingReceiverIsSubjectToCloning,
                                methodSymbol.Parameters,
                                call.Arguments,
                                call.ArgumentRefKindsOpt,
                                call.ArgsToParamsOpt,
                                checkingReceiver,
                                escapeFrom,
                                escapeTo,
                                diagnostics,
                                isRefEscape: true);

                        default:
                            throw ExceptionUtilities.UnexpectedValue(implicitIndexerAccess.IndexerOrSliceAccess.Kind);
                    }
                    break;

                case BoundKind.InlineArrayAccess:
                    {
                        var elementAccess = (BoundInlineArrayAccess)expr;

                        if (elementAccess.GetItemOrSliceHelper is not (WellKnownMember.System_ReadOnlySpan_T__get_Item or WellKnownMember.System_Span_T__get_Item) || elementAccess.IsValue)
                        {
                            Debug.Assert(GetInlineArrayAccessEquivalentSignatureMethod(elementAccess, out _, out _).RefKind == RefKind.None);
                            break;
                        }

                        ImmutableArray<BoundExpression> arguments;
                        ImmutableArray<RefKind> refKinds;
                        SignatureOnlyMethodSymbol equivalentSignatureMethod = GetInlineArrayAccessEquivalentSignatureMethod(elementAccess, out arguments, out refKinds);

                        Debug.Assert(equivalentSignatureMethod.RefKind != RefKind.None);

                        return CheckInvocationEscape(
                            elementAccess.Syntax,
                            equivalentSignatureMethod,
                            receiver: null,
                            receiverIsSubjectToCloning: ThreeState.Unknown,
                            equivalentSignatureMethod.Parameters,
                            argsOpt: arguments,
                            argRefKindsOpt: refKinds,
                            argsToParamsOpt: default,
                            checkingReceiver,
                            escapeFrom,
                            escapeTo,
                            diagnostics,
                            isRefEscape: true);
                    }

                case BoundKind.FunctionPointerInvocation:
                    var functionPointerInvocation = (BoundFunctionPointerInvocation)expr;

                    FunctionPointerMethodSymbol signature = functionPointerInvocation.FunctionPointer.Signature;
                    if (signature.RefKind == RefKind.None)
                    {
                        break;
                    }

                    return CheckInvocationEscape(
                        functionPointerInvocation.Syntax,
                        signature,
                        functionPointerInvocation.InvokedExpression,
                        receiverIsSubjectToCloning: ThreeState.False,
                        signature.Parameters,
                        functionPointerInvocation.Arguments,
                        functionPointerInvocation.ArgumentRefKindsOpt,
                        argsToParamsOpt: default,
                        checkingReceiver,
                        escapeFrom,
                        escapeTo,
                        diagnostics,
                        isRefEscape: true);

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
                        propertyAccess.InitialBindingReceiverIsSubjectToCloning,
                        default,
                        default,
                        default,
                        default,
                        checkingReceiver,
                        escapeFrom,
                        escapeTo,
                        diagnostics,
                        isRefEscape: true);

                case BoundKind.AssignmentOperator:
                    var assignment = (BoundAssignmentOperator)expr;

                    // Only ref-assignments can be LValues
                    if (!assignment.IsRef)
                    {
                        break;
                    }

                    return CheckRefEscape(
                        node,
                        assignment.Left,
                        escapeFrom,
                        escapeTo,
                        checkingReceiver: false,
                        diagnostics);

                case BoundKind.Conversion:
                    var conversion = (BoundConversion)expr;
                    if (conversion.Conversion == Conversion.ImplicitThrow)
                    {
                        return CheckRefEscape(node, conversion.Operand, escapeFrom, escapeTo, checkingReceiver, diagnostics);
                    }
                    break;

                case BoundKind.ThrowExpression:
                    return true;
            }

            // At this point we should have covered all the possible cases for anything that is not a strict RValue.
            Error(diagnostics, GetStandardRValueRefEscapeError(escapeTo), node);
            return false;
        }

        internal uint GetBroadestValEscape(BoundTupleExpression expr, uint scopeOfTheContainingExpression)
        {
            uint broadest = scopeOfTheContainingExpression;
            foreach (var element in expr.Arguments)
            {
                uint valEscape;
                if (element is BoundTupleExpression te)
                {
                    valEscape = GetBroadestValEscape(te, scopeOfTheContainingExpression);
                }
                else
                {
                    valEscape = GetValEscape(element, scopeOfTheContainingExpression);
                }

                broadest = Math.Min(broadest, valEscape);
            }

            return broadest;
        }

        /// <summary>
        /// Computes the widest scope depth to which the given expression can escape by value.
        /// 
        /// NOTE: unless the type of expression is ref-like, the result is Binder.ExternalScope since ordinary values can always be returned from methods. 
        /// </summary>
        internal uint GetValEscape(BoundExpression expr, uint scopeOfTheContainingExpression)
        {
#if DEBUG
            AssertVisited(expr);
#endif

            // cannot infer anything from errors
            if (expr.HasAnyErrors)
            {
                return CallingMethodScope;
            }

            // constants/literals cannot refer to local state
            if (expr.ConstantValueOpt != null)
            {
                return CallingMethodScope;
            }

            // to have local-referring values an expression must have a ref-like type
            if (expr.Type?.IsRefLikeType != true)
            {
                return CallingMethodScope;
            }

            // cover case that can refer to local state
            // otherwise default to ExternalScope (ordinary values)
            switch (expr.Kind)
            {
                case BoundKind.ThisReference:
                    var thisParam = ((MethodSymbol)_symbol).ThisParameter;
                    Debug.Assert(thisParam.Type.Equals(((BoundThisReference)expr).Type, TypeCompareKind.ConsiderEverything));
                    return GetParameterValEscape(thisParam);
                case BoundKind.DefaultLiteral:
                case BoundKind.DefaultExpression:
                case BoundKind.Utf8String:
                    // always returnable
                    return CallingMethodScope;

                case BoundKind.Parameter:
                    return GetParameterValEscape(((BoundParameter)expr).ParameterSymbol);

                case BoundKind.FromEndIndexExpression:
                    // We are going to call a constructor that takes an integer and a bool. Cannot leak any references through them.
                    // always returnable
                    return CallingMethodScope;

                case BoundKind.TupleLiteral:
                case BoundKind.ConvertedTupleLiteral:
                    var tupleLiteral = (BoundTupleExpression)expr;
                    return GetTupleValEscape(tupleLiteral.Arguments, scopeOfTheContainingExpression);

                case BoundKind.MakeRefOperator:
                case BoundKind.RefValueOperator:
                    // for compat reasons
                    // NB: it also means can`t assign stackalloc spans to a __refvalue
                    //     we are ok with that.
                    return CallingMethodScope;

                case BoundKind.DiscardExpression:
                    return CallingMethodScope;

                case BoundKind.DeconstructValuePlaceholder:
                case BoundKind.InterpolatedStringArgumentPlaceholder:
                case BoundKind.AwaitableValuePlaceholder:
                    return GetPlaceholderScope((BoundValuePlaceholderBase)expr);

                case BoundKind.Local:
                    return GetLocalScopes(((BoundLocal)expr).LocalSymbol).ValEscapeScope;

                case BoundKind.CapturedReceiverPlaceholder:
                    // Equivalent to a non-ref local with the underlying receiver as an initializer provided at declaration 
                    var placeholder = (BoundCapturedReceiverPlaceholder)expr;
                    return GetValEscape(placeholder.Receiver, placeholder.LocalScopeDepth);

                case BoundKind.StackAllocArrayCreation:
                case BoundKind.ConvertedStackAllocExpression:
                    return CurrentMethodScope;

                case BoundKind.ConditionalOperator:
                    var conditional = (BoundConditionalOperator)expr;

                    var consEscape = GetValEscape(conditional.Consequence, scopeOfTheContainingExpression);

                    if (conditional.IsRef)
                    {
                        // ref conditional defers to one operand. 
                        // the other one is the same or we will be reporting errors anyways.
                        return consEscape;
                    }

                    // val conditional gets narrowest of its operands
                    return Math.Max(consEscape,
                                    GetValEscape(conditional.Alternative, scopeOfTheContainingExpression));

                case BoundKind.NullCoalescingOperator:
                    var coalescingOp = (BoundNullCoalescingOperator)expr;

                    return Math.Max(GetValEscape(coalescingOp.LeftOperand, scopeOfTheContainingExpression),
                                    GetValEscape(coalescingOp.RightOperand, scopeOfTheContainingExpression));

                case BoundKind.FieldAccess:
                    var fieldAccess = (BoundFieldAccess)expr;
                    var fieldSymbol = fieldAccess.FieldSymbol;

                    if (fieldSymbol.IsStatic || !fieldSymbol.ContainingType.IsRefLikeType)
                    {
                        // Already an error state.
                        return CallingMethodScope;
                    }

                    // for ref-like fields defer to the receiver.
                    return GetValEscape(fieldAccess.ReceiverOpt, scopeOfTheContainingExpression);

                case BoundKind.Call:
                    {
                        var call = (BoundCall)expr;

                        return GetInvocationEscapeScope(
                            call.Method,
                            call.ReceiverOpt,
                            call.InitialBindingReceiverIsSubjectToCloning,
                            call.Method.Parameters,
                            call.Arguments,
                            call.ArgumentRefKindsOpt,
                            call.ArgsToParamsOpt,
                            scopeOfTheContainingExpression,
                            isRefEscape: false);
                    }

                case BoundKind.FunctionPointerInvocation:
                    var ptrInvocation = (BoundFunctionPointerInvocation)expr;
                    var ptrSymbol = ptrInvocation.FunctionPointer.Signature;

                    return GetInvocationEscapeScope(
                        ptrSymbol,
                        receiver: null,
                        receiverIsSubjectToCloning: ThreeState.Unknown,
                        ptrSymbol.Parameters,
                        ptrInvocation.Arguments,
                        ptrInvocation.ArgumentRefKindsOpt,
                        argsToParamsOpt: default,
                        scopeOfTheContainingExpression,
                        isRefEscape: false);

                case BoundKind.IndexerAccess:
                    {
                        var indexerAccess = (BoundIndexerAccess)expr;
                        var indexerSymbol = indexerAccess.Indexer;

                        return GetInvocationEscapeScope(
                            indexerSymbol,
                            indexerAccess.ReceiverOpt,
                            indexerAccess.InitialBindingReceiverIsSubjectToCloning,
                            indexerSymbol.Parameters,
                            indexerAccess.Arguments,
                            indexerAccess.ArgumentRefKindsOpt,
                            indexerAccess.ArgsToParamsOpt,
                            scopeOfTheContainingExpression,
                            isRefEscape: false);
                    }

                case BoundKind.ImplicitIndexerAccess:
                    var implicitIndexerAccess = (BoundImplicitIndexerAccess)expr;

                    // Note: the Argument and LengthOrCountAccess use is purely local

                    switch (implicitIndexerAccess.IndexerOrSliceAccess)
                    {
                        case BoundIndexerAccess indexerAccess:
                            var indexerSymbol = indexerAccess.Indexer;

                            return GetInvocationEscapeScope(
                                indexerSymbol,
                                implicitIndexerAccess.Receiver,
                                indexerAccess.InitialBindingReceiverIsSubjectToCloning,
                                indexerSymbol.Parameters,
                                indexerAccess.Arguments,
                                indexerAccess.ArgumentRefKindsOpt,
                                indexerAccess.ArgsToParamsOpt,
                                scopeOfTheContainingExpression,
                                isRefEscape: false);

                        case BoundArrayAccess:
                            // only possible in error cases (if possible at all)
                            return scopeOfTheContainingExpression;

                        case BoundCall call:
                            return GetInvocationEscapeScope(
                                call.Method,
                                implicitIndexerAccess.Receiver,
                                call.InitialBindingReceiverIsSubjectToCloning,
                                call.Method.Parameters,
                                call.Arguments,
                                call.ArgumentRefKindsOpt,
                                call.ArgsToParamsOpt,
                                scopeOfTheContainingExpression,
                                isRefEscape: false);

                        default:
                            throw ExceptionUtilities.UnexpectedValue(implicitIndexerAccess.IndexerOrSliceAccess.Kind);
                    }

                case BoundKind.InlineArrayAccess:
                    {
                        var elementAccess = (BoundInlineArrayAccess)expr;

                        ImmutableArray<BoundExpression> arguments;
                        ImmutableArray<RefKind> refKinds;
                        SignatureOnlyMethodSymbol equivalentSignatureMethod = GetInlineArrayAccessEquivalentSignatureMethod(elementAccess, out arguments, out refKinds);

                        return GetInvocationEscapeScope(
                            equivalentSignatureMethod,
                            receiver: null,
                            receiverIsSubjectToCloning: ThreeState.Unknown,
                            equivalentSignatureMethod.Parameters,
                            arguments,
                            refKinds,
                            argsToParamsOpt: default,
                            scopeOfTheContainingExpression,
                            isRefEscape: false);
                    }

                case BoundKind.PropertyAccess:
                    var propertyAccess = (BoundPropertyAccess)expr;

                    // not passing any arguments/parameters
                    return GetInvocationEscapeScope(
                        propertyAccess.PropertySymbol,
                        propertyAccess.ReceiverOpt,
                        propertyAccess.InitialBindingReceiverIsSubjectToCloning,
                        default,
                        default,
                        default,
                        default,
                        scopeOfTheContainingExpression,
                        isRefEscape: false);

                case BoundKind.ObjectCreationExpression:
                    var objectCreation = (BoundObjectCreationExpression)expr;
                    var constructorSymbol = objectCreation.Constructor;

                    var escape = GetInvocationEscapeScope(
                        constructorSymbol,
                        receiver: null,
                        receiverIsSubjectToCloning: ThreeState.Unknown,
                        constructorSymbol.Parameters,
                        objectCreation.Arguments,
                        objectCreation.ArgumentRefKindsOpt,
                        objectCreation.ArgsToParamsOpt,
                        scopeOfTheContainingExpression,
                        isRefEscape: false);

                    var initializerOpt = objectCreation.InitializerExpressionOpt;
                    if (initializerOpt != null)
                    {
                        escape = Math.Max(escape, GetValEscape(initializerOpt, scopeOfTheContainingExpression));
                    }

                    return escape;

                case BoundKind.WithExpression:
                    var withExpression = (BoundWithExpression)expr;

                    return Math.Max(GetValEscape(withExpression.Receiver, scopeOfTheContainingExpression),
                                    GetValEscape(withExpression.InitializerExpression, scopeOfTheContainingExpression));

                case BoundKind.UnaryOperator:
                    return GetValEscape(((BoundUnaryOperator)expr).Operand, scopeOfTheContainingExpression);

                case BoundKind.Conversion:
                    var conversion = (BoundConversion)expr;
                    Debug.Assert(conversion.ConversionKind != ConversionKind.StackAllocToSpanType, "StackAllocToSpanType unexpected");

                    if (conversion.ConversionKind == ConversionKind.InterpolatedStringHandler)
                    {
                        return GetInterpolatedStringHandlerConversionEscapeScope(conversion.Operand, scopeOfTheContainingExpression);
                    }

                    if (conversion.ConversionKind == ConversionKind.CollectionExpression)
                    {
                        return HasLocalScope((BoundCollectionExpression)conversion.Operand) ?
                            CurrentMethodScope :
                            CallingMethodScope;
                    }

                    if (conversion.Conversion.IsInlineArray)
                    {
                        ImmutableArray<BoundExpression> arguments;
                        ImmutableArray<RefKind> refKinds;
                        SignatureOnlyMethodSymbol equivalentSignatureMethod = GetInlineArrayConversionEquivalentSignatureMethod(conversion, out arguments, out refKinds);

                        return GetInvocationEscapeScope(
                            equivalentSignatureMethod,
                            receiver: null,
                            receiverIsSubjectToCloning: ThreeState.Unknown,
                            equivalentSignatureMethod.Parameters,
                            arguments,
                            refKinds,
                            argsToParamsOpt: default,
                            scopeOfTheContainingExpression,
                            isRefEscape: false);
                    }

                    return GetValEscape(conversion.Operand, scopeOfTheContainingExpression);

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

                case BoundKind.RangeExpression:
                    var range = (BoundRangeExpression)expr;

                    return Math.Max((range.LeftOperandOpt is { } left ? GetValEscape(left, scopeOfTheContainingExpression) : CallingMethodScope),
                                    (range.RightOperandOpt is { } right ? GetValEscape(right, scopeOfTheContainingExpression) : CallingMethodScope));

                case BoundKind.UserDefinedConditionalLogicalOperator:
                    var uo = (BoundUserDefinedConditionalLogicalOperator)expr;

                    return Math.Max(GetValEscape(uo.Left, scopeOfTheContainingExpression),
                                    GetValEscape(uo.Right, scopeOfTheContainingExpression));

                case BoundKind.QueryClause:
                    return GetValEscape(((BoundQueryClause)expr).Value, scopeOfTheContainingExpression);

                case BoundKind.RangeVariable:
                    return GetValEscape(((BoundRangeVariable)expr).Value, scopeOfTheContainingExpression);

                case BoundKind.ObjectInitializerExpression:
                    var initExpr = (BoundObjectInitializerExpression)expr;
                    return GetValEscapeOfObjectInitializer(initExpr, scopeOfTheContainingExpression);

                case BoundKind.CollectionInitializerExpression:
                    var colExpr = (BoundCollectionInitializerExpression)expr;
                    return GetValEscape(colExpr.Initializers, scopeOfTheContainingExpression);

                case BoundKind.CollectionElementInitializer:
                    var colElement = (BoundCollectionElementInitializer)expr;
                    return GetValEscape(colElement.Arguments, scopeOfTheContainingExpression);

                case BoundKind.ObjectInitializerMember:
                    // this node generally makes no sense outside of the context of containing initializer
                    // however binder uses it as a placeholder when binding assignments inside an object initializer
                    // just say it does not escape anywhere, so that we do not get false errors.
                    return scopeOfTheContainingExpression;

                case BoundKind.ImplicitReceiver:
                case BoundKind.ObjectOrCollectionValuePlaceholder:
                    // binder uses this as a placeholder when binding members inside an object initializer
                    // just say it does not escape anywhere, so that we do not get false errors.
                    return scopeOfTheContainingExpression;

                case BoundKind.InterpolatedStringHandlerPlaceholder:
                    // The handler placeholder cannot escape out of the current expression, as it's a compiler-synthesized
                    // location.
                    return scopeOfTheContainingExpression;

                case BoundKind.DisposableValuePlaceholder:
                    // Disposable value placeholder is only ever used to lookup a pattern dispose method
                    // then immediately discarded. The actual expression will be generated during lowering 
                    return scopeOfTheContainingExpression;

                case BoundKind.PointerElementAccess:
                case BoundKind.PointerIndirectionOperator:
                    // Unsafe code will always be allowed to escape.
                    return CallingMethodScope;

                case BoundKind.AsOperator:
                case BoundKind.AwaitExpression:
                case BoundKind.ConditionalAccess:
                case BoundKind.ConditionalReceiver:
                case BoundKind.ArrayAccess:
                    // only possible in error cases (if possible at all)
                    return scopeOfTheContainingExpression;

                case BoundKind.ConvertedSwitchExpression:
                case BoundKind.UnconvertedSwitchExpression:
                    var switchExpr = (BoundSwitchExpression)expr;
                    return GetValEscape(switchExpr.SwitchArms.SelectAsArray(a => a.Value), scopeOfTheContainingExpression);

                default:
                    // in error situations some unexpected nodes could make here
                    // returning "scopeOfTheContainingExpression" seems safer than throwing.
                    // we will still assert to make sure that all nodes are accounted for. 
                    Debug.Assert(false, $"{expr.Kind} expression of {expr.Type} type");
                    return scopeOfTheContainingExpression;
            }
        }

#nullable enable
        private bool HasLocalScope(BoundCollectionExpression expr)
        {
            // A non-empty collection expression with span type may be stored
            // on the stack. In those cases the expression may have local scope.

            if (expr.Type?.IsRefLikeType != true || expr.Elements.Length == 0)
            {
                return false;
            }

            var collectionTypeKind = ConversionsBase.GetCollectionExpressionTypeKind(_compilation, expr.Type, out var elementType);

            switch (collectionTypeKind)
            {
                case CollectionExpressionTypeKind.ReadOnlySpan:
                    Debug.Assert(elementType.Type is { });
                    return !LocalRewriter.ShouldUseRuntimeHelpersCreateSpan(expr, elementType.Type);
                case CollectionExpressionTypeKind.Span:
                    return true;
                case CollectionExpressionTypeKind.CollectionBuilder:
                    // For a ref struct type with a builder method, the scope of the collection
                    // expression is the scope of an invocation of the builder method with the
                    // collection expression as the span argument. That is, `R r = [x, y, z];`
                    // is equivalent to `R r = Builder.Create((ReadOnlySpan<...>)[x, y, z]);`.
                    var constructMethod = expr.CollectionBuilderMethod;
                    if (constructMethod is not { Parameters: [{ RefKind: RefKind.None } parameter] })
                    {
                        // Unexpected construct method. Restrict the collection to local scope.
                        return true;
                    }
                    Debug.Assert(constructMethod.ReturnType.Equals(expr.Type, TypeCompareKind.AllIgnoreOptions));
                    Debug.Assert(parameter.Type.OriginalDefinition.Equals(_compilation.GetWellKnownType(WellKnownType.System_ReadOnlySpan_T), TypeCompareKind.AllIgnoreOptions));
                    if (parameter.EffectiveScope == ScopedKind.ScopedValue)
                    {
                        return false;
                    }
                    if (LocalRewriter.ShouldUseRuntimeHelpersCreateSpan(expr, ((NamedTypeSymbol)parameter.Type).TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[0].Type))
                    {
                        return false;
                    }
                    return true;
                case CollectionExpressionTypeKind.ImplementsIEnumerable:
                    // Error cases. Restrict the collection to local scope.
                    return true;
                default:
                    throw ExceptionUtilities.UnexpectedValue(collectionTypeKind); // ref struct collection type with unexpected type kind
            }
        }
#nullable disable

        private uint GetTupleValEscape(ImmutableArray<BoundExpression> elements, uint scopeOfTheContainingExpression)
        {
            uint narrowestScope = scopeOfTheContainingExpression;
            foreach (var element in elements)
            {
                narrowestScope = Math.Max(narrowestScope, GetValEscape(element, scopeOfTheContainingExpression));
            }

            return narrowestScope;
        }

        private uint GetValEscapeOfObjectInitializer(BoundObjectInitializerExpression initExpr, uint scopeOfTheContainingExpression)
        {
            var result = CallingMethodScope;
            foreach (var expression in initExpr.Initializers)
            {
                if (expression.Kind == BoundKind.AssignmentOperator)
                {
                    var assignment = (BoundAssignmentOperator)expression;
                    var rightValEscape = assignment.IsRef
                        ? GetRefEscape(assignment.Right, scopeOfTheContainingExpression)
                        : GetValEscape(assignment.Right, scopeOfTheContainingExpression);

                    result = Math.Max(result, rightValEscape);

                    var left = (BoundObjectInitializerMember)assignment.Left;
                    result = Math.Max(result, GetValEscape(left.Arguments, scopeOfTheContainingExpression));
                }
                else
                {
                    result = Math.Max(result, GetValEscape(expression, scopeOfTheContainingExpression));
                }
            }

            return result;
        }

        private uint GetValEscape(ImmutableArray<BoundExpression> expressions, uint scopeOfTheContainingExpression)
        {
            var result = CallingMethodScope;
            foreach (var expression in expressions)
            {
                result = Math.Max(result, GetValEscape(expression, scopeOfTheContainingExpression));
            }

            return result;
        }

        /// <summary>
        /// A counterpart to the GetValEscape, which validates if given escape demand can be met by the expression.
        /// The result indicates whether the escape is possible.
        /// Additionally, the method emits diagnostics (possibly more than one, recursively) that would help identify the cause for the failure.
        /// </summary>
        internal bool CheckValEscape(SyntaxNode node, BoundExpression expr, uint escapeFrom, uint escapeTo, bool checkingReceiver, BindingDiagnosticBag diagnostics)
        {
#if DEBUG
            AssertVisited(expr);
#endif

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
            if (expr.ConstantValueOpt != null)
            {
                return true;
            }

            // to have local-referring values an expression must have a ref-like type
            if (expr.Type?.IsRefLikeType != true)
            {
                return true;
            }

            bool inUnsafeRegion = _inUnsafeRegion;

            switch (expr.Kind)
            {
                case BoundKind.ThisReference:
                    var thisParam = ((MethodSymbol)_symbol).ThisParameter;
                    Debug.Assert(thisParam.Type.Equals(((BoundThisReference)expr).Type, TypeCompareKind.ConsiderEverything));
                    return CheckParameterValEscape(node, thisParam, escapeTo, diagnostics);

                case BoundKind.DefaultLiteral:
                case BoundKind.DefaultExpression:
                case BoundKind.Utf8String:
                    // always returnable
                    return true;

                case BoundKind.Parameter:
                    return CheckParameterValEscape(node, ((BoundParameter)expr).ParameterSymbol, escapeTo, diagnostics);

                case BoundKind.TupleLiteral:
                case BoundKind.ConvertedTupleLiteral:
                    var tupleLiteral = (BoundTupleExpression)expr;
                    return CheckTupleValEscape(tupleLiteral.Arguments, escapeFrom, escapeTo, diagnostics);

                case BoundKind.MakeRefOperator:
                case BoundKind.RefValueOperator:
                    // for compat reasons
                    return true;

                case BoundKind.DiscardExpression:
                    // same as uninitialized local
                    return true;

                case BoundKind.DeconstructValuePlaceholder:
                case BoundKind.AwaitableValuePlaceholder:
                case BoundKind.InterpolatedStringArgumentPlaceholder:
                    if (GetPlaceholderScope((BoundValuePlaceholderBase)expr) > escapeTo)
                    {
                        Error(diagnostics, inUnsafeRegion ? ErrorCode.WRN_EscapeVariable : ErrorCode.ERR_EscapeVariable, node, expr.Syntax);
                        return inUnsafeRegion;
                    }
                    return true;

                case BoundKind.Local:
                    var localSymbol = ((BoundLocal)expr).LocalSymbol;
                    if (GetLocalScopes(localSymbol).ValEscapeScope > escapeTo)
                    {
                        Error(diagnostics, inUnsafeRegion ? ErrorCode.WRN_EscapeVariable : ErrorCode.ERR_EscapeVariable, node, localSymbol);
                        return inUnsafeRegion;
                    }
                    return true;

                case BoundKind.CapturedReceiverPlaceholder:
                    // Equivalent to a non-ref local with the underlying receiver as an initializer provided at declaration 
                    BoundExpression underlyingReceiver = ((BoundCapturedReceiverPlaceholder)expr).Receiver;
                    return CheckValEscape(underlyingReceiver.Syntax, underlyingReceiver, escapeFrom, escapeTo, checkingReceiver, diagnostics);

                case BoundKind.StackAllocArrayCreation:
                case BoundKind.ConvertedStackAllocExpression:
                    if (escapeTo < CurrentMethodScope)
                    {
                        Error(diagnostics, inUnsafeRegion ? ErrorCode.WRN_EscapeStackAlloc : ErrorCode.ERR_EscapeStackAlloc, node, expr.Type);
                        return inUnsafeRegion;
                    }
                    return true;

                case BoundKind.UnconvertedConditionalOperator:
                    {
                        var conditional = (BoundUnconvertedConditionalOperator)expr;
                        return
                            CheckValEscape(conditional.Consequence.Syntax, conditional.Consequence, escapeFrom, escapeTo, checkingReceiver: false, diagnostics: diagnostics) &&
                            CheckValEscape(conditional.Alternative.Syntax, conditional.Alternative, escapeFrom, escapeTo, checkingReceiver: false, diagnostics: diagnostics);
                    }

                case BoundKind.ConditionalOperator:
                    {
                        var conditional = (BoundConditionalOperator)expr;

                        var consValid = CheckValEscape(conditional.Consequence.Syntax, conditional.Consequence, escapeFrom, escapeTo, checkingReceiver: false, diagnostics: diagnostics);

                        if (!consValid || conditional.IsRef)
                        {
                            // ref conditional defers to one operand. 
                            // the other one is the same or we will be reporting errors anyways.
                            return consValid;
                        }

                        return CheckValEscape(conditional.Alternative.Syntax, conditional.Alternative, escapeFrom, escapeTo, checkingReceiver: false, diagnostics: diagnostics);
                    }

                case BoundKind.NullCoalescingOperator:
                    var coalescingOp = (BoundNullCoalescingOperator)expr;
                    return CheckValEscape(coalescingOp.LeftOperand.Syntax, coalescingOp.LeftOperand, escapeFrom, escapeTo, checkingReceiver, diagnostics) &&
                            CheckValEscape(coalescingOp.RightOperand.Syntax, coalescingOp.RightOperand, escapeFrom, escapeTo, checkingReceiver, diagnostics);

                case BoundKind.FieldAccess:
                    var fieldAccess = (BoundFieldAccess)expr;
                    var fieldSymbol = fieldAccess.FieldSymbol;

                    if (fieldSymbol.IsStatic || !fieldSymbol.ContainingType.IsRefLikeType)
                    {
                        // Already an error state.
                        return true;
                    }

                    // for ref-like fields defer to the receiver.
                    return CheckValEscape(node, fieldAccess.ReceiverOpt, escapeFrom, escapeTo, true, diagnostics);

                case BoundKind.Call:
                    {
                        var call = (BoundCall)expr;
                        var methodSymbol = call.Method;

                        return CheckInvocationEscape(
                            call.Syntax,
                            methodSymbol,
                            call.ReceiverOpt,
                            call.InitialBindingReceiverIsSubjectToCloning,
                            methodSymbol.Parameters,
                            call.Arguments,
                            call.ArgumentRefKindsOpt,
                            call.ArgsToParamsOpt,
                            checkingReceiver,
                            escapeFrom,
                            escapeTo,
                            diagnostics,
                            isRefEscape: false);
                    }

                case BoundKind.FunctionPointerInvocation:
                    var ptrInvocation = (BoundFunctionPointerInvocation)expr;
                    var ptrSymbol = ptrInvocation.FunctionPointer.Signature;

                    return CheckInvocationEscape(
                        ptrInvocation.Syntax,
                        ptrSymbol,
                        receiver: null,
                        receiverIsSubjectToCloning: ThreeState.Unknown,
                        ptrSymbol.Parameters,
                        ptrInvocation.Arguments,
                        ptrInvocation.ArgumentRefKindsOpt,
                        argsToParamsOpt: default,
                        checkingReceiver,
                        escapeFrom,
                        escapeTo,
                        diagnostics,
                        isRefEscape: false);

                case BoundKind.IndexerAccess:
                    {
                        var indexerAccess = (BoundIndexerAccess)expr;
                        var indexerSymbol = indexerAccess.Indexer;

                        return CheckInvocationEscape(
                            indexerAccess.Syntax,
                            indexerSymbol,
                            indexerAccess.ReceiverOpt,
                            indexerAccess.InitialBindingReceiverIsSubjectToCloning,
                            indexerSymbol.Parameters,
                            indexerAccess.Arguments,
                            indexerAccess.ArgumentRefKindsOpt,
                            indexerAccess.ArgsToParamsOpt,
                            checkingReceiver,
                            escapeFrom,
                            escapeTo,
                            diagnostics,
                            isRefEscape: false);
                    }

                case BoundKind.ImplicitIndexerAccess:
                    var implicitIndexerAccess = (BoundImplicitIndexerAccess)expr;

                    // Note: the Argument and LengthOrCountAccess use is purely local

                    switch (implicitIndexerAccess.IndexerOrSliceAccess)
                    {
                        case BoundIndexerAccess indexerAccess:
                            var indexerSymbol = indexerAccess.Indexer;

                            return CheckInvocationEscape(
                                indexerAccess.Syntax,
                                indexerSymbol,
                                implicitIndexerAccess.Receiver,
                                indexerAccess.InitialBindingReceiverIsSubjectToCloning,
                                indexerSymbol.Parameters,
                                indexerAccess.Arguments,
                                indexerAccess.ArgumentRefKindsOpt,
                                indexerAccess.ArgsToParamsOpt,
                                checkingReceiver,
                                escapeFrom,
                                escapeTo,
                                diagnostics,
                                isRefEscape: false);

                        case BoundArrayAccess:
                            // only possible in error cases (if possible at all)
                            return false;

                        case BoundCall call:
                            var methodSymbol = call.Method;

                            return CheckInvocationEscape(
                                call.Syntax,
                                methodSymbol,
                                implicitIndexerAccess.Receiver,
                                call.InitialBindingReceiverIsSubjectToCloning,
                                methodSymbol.Parameters,
                                call.Arguments,
                                call.ArgumentRefKindsOpt,
                                call.ArgsToParamsOpt,
                                checkingReceiver,
                                escapeFrom,
                                escapeTo,
                                diagnostics,
                                isRefEscape: false);

                        default:
                            throw ExceptionUtilities.UnexpectedValue(implicitIndexerAccess.IndexerOrSliceAccess.Kind);
                    }

                case BoundKind.InlineArrayAccess:
                    {
                        var elementAccess = (BoundInlineArrayAccess)expr;

                        ImmutableArray<BoundExpression> arguments;
                        ImmutableArray<RefKind> refKinds;
                        SignatureOnlyMethodSymbol equivalentSignatureMethod = GetInlineArrayAccessEquivalentSignatureMethod(elementAccess, out arguments, out refKinds);

                        return CheckInvocationEscape(
                            elementAccess.Syntax,
                            equivalentSignatureMethod,
                            receiver: null,
                            receiverIsSubjectToCloning: ThreeState.Unknown,
                            equivalentSignatureMethod.Parameters,
                            argsOpt: arguments,
                            argRefKindsOpt: refKinds,
                            argsToParamsOpt: default,
                            checkingReceiver,
                            escapeFrom,
                            escapeTo,
                            diagnostics,
                            isRefEscape: false);
                    }

                case BoundKind.PropertyAccess:
                    var propertyAccess = (BoundPropertyAccess)expr;

                    // not passing any arguments/parameters
                    return CheckInvocationEscape(
                        propertyAccess.Syntax,
                        propertyAccess.PropertySymbol,
                        propertyAccess.ReceiverOpt,
                        propertyAccess.InitialBindingReceiverIsSubjectToCloning,
                        default,
                        default,
                        default,
                        default,
                        checkingReceiver,
                        escapeFrom,
                        escapeTo,
                        diagnostics,
                        isRefEscape: false);

                case BoundKind.ObjectCreationExpression:
                    {
                        var objectCreation = (BoundObjectCreationExpression)expr;
                        var constructorSymbol = objectCreation.Constructor;

                        var escape = CheckInvocationEscape(
                            objectCreation.Syntax,
                            constructorSymbol,
                            receiver: null,
                            receiverIsSubjectToCloning: ThreeState.Unknown,
                            constructorSymbol.Parameters,
                            objectCreation.Arguments,
                            objectCreation.ArgumentRefKindsOpt,
                            objectCreation.ArgsToParamsOpt,
                            checkingReceiver,
                            escapeFrom,
                            escapeTo,
                            diagnostics,
                            isRefEscape: false);

                        var initializerExpr = objectCreation.InitializerExpressionOpt;
                        if (initializerExpr != null)
                        {
                            escape = escape &&
                                CheckValEscape(
                                    initializerExpr.Syntax,
                                    initializerExpr,
                                    escapeFrom,
                                    escapeTo,
                                    checkingReceiver: false,
                                    diagnostics: diagnostics);
                        }

                        return escape;
                    }

                case BoundKind.WithExpression:
                    {
                        var withExpr = (BoundWithExpression)expr;
                        var escape = CheckValEscape(node, withExpr.Receiver, escapeFrom, escapeTo, checkingReceiver: false, diagnostics);

                        var initializerExpr = withExpr.InitializerExpression;
                        escape = escape && CheckValEscape(initializerExpr.Syntax, initializerExpr, escapeFrom, escapeTo, checkingReceiver: false, diagnostics: diagnostics);

                        return escape;
                    }

                case BoundKind.UnaryOperator:
                    var unary = (BoundUnaryOperator)expr;
                    return CheckValEscape(node, unary.Operand, escapeFrom, escapeTo, checkingReceiver: false, diagnostics: diagnostics);

                case BoundKind.FromEndIndexExpression:
                    // We are going to call a constructor that takes an integer and a bool. Cannot leak any references through them.
                    return true;

                case BoundKind.Conversion:
                    var conversion = (BoundConversion)expr;
                    Debug.Assert(conversion.ConversionKind != ConversionKind.StackAllocToSpanType, "StackAllocToSpanType unexpected");

                    if (conversion.ConversionKind == ConversionKind.InterpolatedStringHandler)
                    {
                        return CheckInterpolatedStringHandlerConversionEscape(conversion.Operand, escapeFrom, escapeTo, diagnostics);
                    }

                    if (conversion.ConversionKind == ConversionKind.CollectionExpression)
                    {
                        if (HasLocalScope((BoundCollectionExpression)conversion.Operand) && escapeTo < CurrentMethodScope)
                        {
                            Error(diagnostics, ErrorCode.ERR_CollectionExpressionEscape, node, expr.Type);
                            return false;
                        }
                        return true;
                    }

                    if (conversion.Conversion.IsInlineArray)
                    {
                        ImmutableArray<BoundExpression> arguments;
                        ImmutableArray<RefKind> refKinds;
                        SignatureOnlyMethodSymbol equivalentSignatureMethod = GetInlineArrayConversionEquivalentSignatureMethod(conversion, out arguments, out refKinds);

                        return CheckInvocationEscape(
                            conversion.Syntax,
                            equivalentSignatureMethod,
                            receiver: null,
                            receiverIsSubjectToCloning: ThreeState.Unknown,
                            equivalentSignatureMethod.Parameters,
                            argsOpt: arguments,
                            argRefKindsOpt: refKinds,
                            argsToParamsOpt: default,
                            checkingReceiver,
                            escapeFrom,
                            escapeTo,
                            diagnostics,
                            isRefEscape: false);
                    }

                    return CheckValEscape(node, conversion.Operand, escapeFrom, escapeTo, checkingReceiver: false, diagnostics: diagnostics);

                case BoundKind.AssignmentOperator:
                    var assignment = (BoundAssignmentOperator)expr;
                    return CheckValEscape(node, assignment.Left, escapeFrom, escapeTo, checkingReceiver: false, diagnostics: diagnostics);

                case BoundKind.IncrementOperator:
                    var increment = (BoundIncrementOperator)expr;
                    return CheckValEscape(node, increment.Operand, escapeFrom, escapeTo, checkingReceiver: false, diagnostics: diagnostics);

                case BoundKind.CompoundAssignmentOperator:
                    var compound = (BoundCompoundAssignmentOperator)expr;

                    return CheckValEscape(compound.Left.Syntax, compound.Left, escapeFrom, escapeTo, checkingReceiver: false, diagnostics: diagnostics) &&
                           CheckValEscape(compound.Right.Syntax, compound.Right, escapeFrom, escapeTo, checkingReceiver: false, diagnostics: diagnostics);

                case BoundKind.BinaryOperator:
                    var binary = (BoundBinaryOperator)expr;

                    if (binary.OperatorKind == BinaryOperatorKind.Utf8Addition)
                    {
                        return true;
                    }

                    return CheckValEscape(binary.Left.Syntax, binary.Left, escapeFrom, escapeTo, checkingReceiver: false, diagnostics: diagnostics) &&
                           CheckValEscape(binary.Right.Syntax, binary.Right, escapeFrom, escapeTo, checkingReceiver: false, diagnostics: diagnostics);

                case BoundKind.RangeExpression:
                    var range = (BoundRangeExpression)expr;

                    if (range.LeftOperandOpt is { } left && !CheckValEscape(left.Syntax, left, escapeFrom, escapeTo, checkingReceiver: false, diagnostics: diagnostics))
                    {
                        return false;
                    }

                    return !(range.RightOperandOpt is { } right && !CheckValEscape(right.Syntax, right, escapeFrom, escapeTo, checkingReceiver: false, diagnostics: diagnostics));

                case BoundKind.UserDefinedConditionalLogicalOperator:
                    var uo = (BoundUserDefinedConditionalLogicalOperator)expr;

                    return CheckValEscape(uo.Left.Syntax, uo.Left, escapeFrom, escapeTo, checkingReceiver: false, diagnostics: diagnostics) &&
                           CheckValEscape(uo.Right.Syntax, uo.Right, escapeFrom, escapeTo, checkingReceiver: false, diagnostics: diagnostics);

                case BoundKind.QueryClause:
                    var clauseValue = ((BoundQueryClause)expr).Value;
                    return CheckValEscape(clauseValue.Syntax, clauseValue, escapeFrom, escapeTo, checkingReceiver: false, diagnostics: diagnostics);

                case BoundKind.RangeVariable:
                    var variableValue = ((BoundRangeVariable)expr).Value;
                    return CheckValEscape(variableValue.Syntax, variableValue, escapeFrom, escapeTo, checkingReceiver: false, diagnostics: diagnostics);

                case BoundKind.ObjectInitializerExpression:
                    var initExpr = (BoundObjectInitializerExpression)expr;
                    return CheckValEscapeOfObjectInitializer(initExpr, escapeFrom, escapeTo, diagnostics);

                // this would be correct implementation for CollectionInitializerExpression 
                // however it is unclear if it is reachable since the initialized type must implement IEnumerable
                case BoundKind.CollectionInitializerExpression:
                    var colExpr = (BoundCollectionInitializerExpression)expr;
                    return CheckValEscape(colExpr.Initializers, escapeFrom, escapeTo, diagnostics);

                // this would be correct implementation for CollectionElementInitializer 
                // however it is unclear if it is reachable since the initialized type must implement IEnumerable
                case BoundKind.CollectionElementInitializer:
                    var colElement = (BoundCollectionElementInitializer)expr;
                    return CheckValEscape(colElement.Arguments, escapeFrom, escapeTo, diagnostics);

                case BoundKind.PointerElementAccess:
                    var accessedExpression = ((BoundPointerElementAccess)expr).Expression;
                    return CheckValEscape(accessedExpression.Syntax, accessedExpression, escapeFrom, escapeTo, checkingReceiver, diagnostics);

                case BoundKind.PointerIndirectionOperator:
                    var operandExpression = ((BoundPointerIndirectionOperator)expr).Operand;
                    return CheckValEscape(operandExpression.Syntax, operandExpression, escapeFrom, escapeTo, checkingReceiver, diagnostics);

                case BoundKind.AsOperator:
                case BoundKind.AwaitExpression:
                case BoundKind.ConditionalAccess:
                case BoundKind.ArrayAccess:
                    // only possible in error cases (if possible at all)
                    return false;

                case BoundKind.UnconvertedSwitchExpression:
                case BoundKind.ConvertedSwitchExpression:
                    foreach (var arm in ((BoundSwitchExpression)expr).SwitchArms)
                    {
                        var result = arm.Value;
                        if (!CheckValEscape(result.Syntax, result, escapeFrom, escapeTo, checkingReceiver: false, diagnostics: diagnostics))
                            return false;
                    }

                    return true;

                default:
                    // in error situations some unexpected nodes could make here
                    // returning "false" seems safer than throwing.
                    // we will still assert to make sure that all nodes are accounted for.
                    Debug.Assert(false, $"{expr.Kind} expression of {expr.Type} type");
                    diagnostics.Add(ErrorCode.ERR_InternalError, node.Location);
                    return false;

                    #region "cannot produce ref-like values"
                    //                case BoundKind.ThrowExpression:
                    //                case BoundKind.ArgListOperator:
                    //                case BoundKind.ArgList:
                    //                case BoundKind.RefTypeOperator:
                    //                case BoundKind.AddressOfOperator:
                    //                case BoundKind.TypeOfOperator:
                    //                case BoundKind.IsOperator:
                    //                case BoundKind.SizeOfOperator:
                    //                case BoundKind.DynamicMemberAccess:
                    //                case BoundKind.DynamicInvocation:
                    //                case BoundKind.NewT:
                    //                case BoundKind.DelegateCreationExpression:
                    //                case BoundKind.ArrayCreation:
                    //                case BoundKind.AnonymousObjectCreationExpression:
                    //                case BoundKind.NameOfOperator:
                    //                case BoundKind.InterpolatedString:
                    //                case BoundKind.StringInsert:
                    //                case BoundKind.DynamicIndexerAccess:
                    //                case BoundKind.Lambda:
                    //                case BoundKind.DynamicObjectCreationExpression:
                    //                case BoundKind.NoPiaObjectCreationExpression:
                    //                case BoundKind.BaseReference:
                    //                case BoundKind.Literal:
                    //                case BoundKind.IsPatternExpression:
                    //                case BoundKind.DeconstructionAssignmentOperator:
                    //                case BoundKind.EventAccess:

                    #endregion

                    #region "not expression that can produce a value"
                    //                case BoundKind.FieldEqualsValue:
                    //                case BoundKind.PropertyEqualsValue:
                    //                case BoundKind.ParameterEqualsValue:
                    //                case BoundKind.NamespaceExpression:
                    //                case BoundKind.TypeExpression:
                    //                case BoundKind.BadStatement:
                    //                case BoundKind.MethodDefIndex:
                    //                case BoundKind.SourceDocumentIndex:
                    //                case BoundKind.ArgList:
                    //                case BoundKind.ArgListOperator:
                    //                case BoundKind.Block:
                    //                case BoundKind.Scope:
                    //                case BoundKind.NoOpStatement:
                    //                case BoundKind.ReturnStatement:
                    //                case BoundKind.YieldReturnStatement:
                    //                case BoundKind.YieldBreakStatement:
                    //                case BoundKind.ThrowStatement:
                    //                case BoundKind.ExpressionStatement:
                    //                case BoundKind.SwitchStatement:
                    //                case BoundKind.SwitchSection:
                    //                case BoundKind.SwitchLabel:
                    //                case BoundKind.BreakStatement:
                    //                case BoundKind.LocalFunctionStatement:
                    //                case BoundKind.ContinueStatement:
                    //                case BoundKind.PatternSwitchStatement:
                    //                case BoundKind.PatternSwitchSection:
                    //                case BoundKind.PatternSwitchLabel:
                    //                case BoundKind.IfStatement:
                    //                case BoundKind.DoStatement:
                    //                case BoundKind.WhileStatement:
                    //                case BoundKind.ForStatement:
                    //                case BoundKind.ForEachStatement:
                    //                case BoundKind.ForEachDeconstructStep:
                    //                case BoundKind.UsingStatement:
                    //                case BoundKind.FixedStatement:
                    //                case BoundKind.LockStatement:
                    //                case BoundKind.TryStatement:
                    //                case BoundKind.CatchBlock:
                    //                case BoundKind.LabelStatement:
                    //                case BoundKind.GotoStatement:
                    //                case BoundKind.LabeledStatement:
                    //                case BoundKind.Label:
                    //                case BoundKind.StatementList:
                    //                case BoundKind.ConditionalGoto:
                    //                case BoundKind.LocalDeclaration:
                    //                case BoundKind.MultipleLocalDeclarations:
                    //                case BoundKind.ArrayInitialization:
                    //                case BoundKind.AnonymousPropertyDeclaration:
                    //                case BoundKind.MethodGroup:
                    //                case BoundKind.PropertyGroup:
                    //                case BoundKind.EventAssignmentOperator:
                    //                case BoundKind.Attribute:
                    //                case BoundKind.FixedLocalCollectionInitializer:
                    //                case BoundKind.DynamicObjectInitializerMember:
                    //                case BoundKind.DynamicCollectionElementInitializer:
                    //                case BoundKind.ImplicitReceiver:
                    //                case BoundKind.FieldInitializer:
                    //                case BoundKind.GlobalStatementInitializer:
                    //                case BoundKind.TypeOrInstanceInitializers:
                    //                case BoundKind.DeclarationPattern:
                    //                case BoundKind.ConstantPattern:
                    //                case BoundKind.WildcardPattern:

                    #endregion

                    #region "not found as an operand in no-error unlowered bound tree"
                    //                case BoundKind.MaximumMethodDefIndex:
                    //                case BoundKind.InstrumentationPayloadRoot:
                    //                case BoundKind.ModuleVersionId:
                    //                case BoundKind.ModuleVersionIdString:
                    //                case BoundKind.Dup:
                    //                case BoundKind.TypeOrValueExpression:
                    //                case BoundKind.BadExpression:
                    //                case BoundKind.ArrayLength:
                    //                case BoundKind.MethodInfo:
                    //                case BoundKind.FieldInfo:
                    //                case BoundKind.SequencePoint:
                    //                case BoundKind.SequencePointExpression:
                    //                case BoundKind.SequencePointWithSpan:
                    //                case BoundKind.StateMachineScope:
                    //                case BoundKind.ConditionalReceiver:
                    //                case BoundKind.ComplexConditionalReceiver:
                    //                case BoundKind.PreviousSubmissionReference:
                    //                case BoundKind.HostObjectMemberReference:
                    //                case BoundKind.UnboundLambda:
                    //                case BoundKind.LoweredConditionalAccess:
                    //                case BoundKind.Sequence:
                    //                case BoundKind.HoistedFieldAccess:
                    //                case BoundKind.OutVariablePendingInference:
                    //                case BoundKind.DeconstructionVariablePendingInference:
                    //                case BoundKind.OutDeconstructVarPendingInference:
                    //                case BoundKind.PseudoVariable:

                    #endregion
            }
        }

        private SignatureOnlyMethodSymbol GetInlineArrayAccessEquivalentSignatureMethod(BoundInlineArrayAccess elementAccess, out ImmutableArray<BoundExpression> arguments, out ImmutableArray<RefKind> refKinds)
        {
            RefKind resultRefKind;
            RefKind parameterRefKind;

            if (elementAccess.GetItemOrSliceHelper is WellKnownMember.System_ReadOnlySpan_T__get_Item or WellKnownMember.System_Span_T__get_Item)
            {
                // inlineArray[index] is equivalent to calling a method with the signature:
                // - ref T GetItem(ref inlineArray), or
                // - ref readonly T GetItem(in inlineArray), or
                // - T GetItem(inlineArray)

                if (elementAccess.IsValue)
                {
                    resultRefKind = RefKind.None;
                    parameterRefKind = RefKind.None;
                }
                else
                {
                    resultRefKind = elementAccess.GetItemOrSliceHelper is WellKnownMember.System_ReadOnlySpan_T__get_Item ? RefKind.In : RefKind.Ref;
                    parameterRefKind = resultRefKind;
                }
            }
            else if (elementAccess.GetItemOrSliceHelper is WellKnownMember.System_ReadOnlySpan_T__Slice_Int_Int or WellKnownMember.System_Span_T__Slice_Int_Int)
            {
                // inlineArray[Range] is equivalent to calling a method with the signature:
                // - Span<T> Slice(ref inlineArray), or
                // - ReadOnlySpan<T> Slice(in inlineArray)
                resultRefKind = RefKind.None;
                parameterRefKind = elementAccess.GetItemOrSliceHelper is WellKnownMember.System_ReadOnlySpan_T__Slice_Int_Int ? RefKind.In : RefKind.Ref;
            }
            else
            {
                throw ExceptionUtilities.Unreachable();
            }

            var equivalentSignatureMethod = new SignatureOnlyMethodSymbol(
                name: "",
                this._symbol.ContainingType,
                MethodKind.Ordinary,
                Cci.CallingConvention.Default,
                ImmutableArray<TypeParameterSymbol>.Empty,
                ImmutableArray.Create<ParameterSymbol>(new SignatureOnlyParameterSymbol(
                                                            TypeWithAnnotations.Create(elementAccess.Expression.Type),
                                                            ImmutableArray<CustomModifier>.Empty,
                                                            isParams: false,
                                                            parameterRefKind
                                                            )),
                resultRefKind,
                isInitOnly: false,
                isStatic: true,
                returnType: TypeWithAnnotations.Create(elementAccess.Type),
                ImmutableArray<CustomModifier>.Empty,
                ImmutableArray<MethodSymbol>.Empty);

            arguments = ImmutableArray.Create(elementAccess.Expression);
            refKinds = ImmutableArray.Create(parameterRefKind);

            return equivalentSignatureMethod;
        }

        private SignatureOnlyMethodSymbol GetInlineArrayConversionEquivalentSignatureMethod(BoundConversion conversion, out ImmutableArray<BoundExpression> arguments, out ImmutableArray<RefKind> refKinds)
        {
            Debug.Assert(conversion.Conversion.IsInlineArray);
            return GetInlineArrayConversionEquivalentSignatureMethod(inlineArray: conversion.Operand, resultType: conversion.Type, out arguments, out refKinds);
        }

        private SignatureOnlyMethodSymbol GetInlineArrayConversionEquivalentSignatureMethod(BoundExpression inlineArray, TypeSymbol resultType, out ImmutableArray<BoundExpression> arguments, out ImmutableArray<RefKind> refKinds)
        {
            // An inline array conversion is equivalent to calling a method with the signature:
            // - Span<T> Convert(ref inlineArray), or
            // - ReadOnlySpan<T> Convert(in inlineArray)

            RefKind parameterRefKind = resultType.OriginalDefinition.Equals(_compilation.GetWellKnownType(WellKnownType.System_ReadOnlySpan_T), TypeCompareKind.AllIgnoreOptions) ? RefKind.In : RefKind.Ref;

            var equivalentSignatureMethod = new SignatureOnlyMethodSymbol(
                name: "",
                _symbol.ContainingType,
                MethodKind.Ordinary,
                Cci.CallingConvention.Default,
                ImmutableArray<TypeParameterSymbol>.Empty,
                ImmutableArray.Create<ParameterSymbol>(new SignatureOnlyParameterSymbol(
                                                            TypeWithAnnotations.Create(inlineArray.Type),
                                                            ImmutableArray<CustomModifier>.Empty,
                                                            isParams: false,
                                                            parameterRefKind
                                                            )),
                RefKind.None,
                isInitOnly: false,
                isStatic: true,
                returnType: TypeWithAnnotations.Create(resultType),
                ImmutableArray<CustomModifier>.Empty,
                ImmutableArray<MethodSymbol>.Empty);

            arguments = ImmutableArray.Create(inlineArray);
            refKinds = ImmutableArray.Create(parameterRefKind);

            return equivalentSignatureMethod;
        }

        private bool CheckTupleValEscape(ImmutableArray<BoundExpression> elements, uint escapeFrom, uint escapeTo, BindingDiagnosticBag diagnostics)
        {
            foreach (var element in elements)
            {
                if (!CheckValEscape(element.Syntax, element, escapeFrom, escapeTo, checkingReceiver: false, diagnostics: diagnostics))
                {
                    return false;
                }
            }

            return true;
        }

        private bool CheckValEscapeOfObjectInitializer(BoundObjectInitializerExpression initExpr, uint escapeFrom, uint escapeTo, BindingDiagnosticBag diagnostics)
        {
            foreach (var expression in initExpr.Initializers)
            {
                if (expression.Kind == BoundKind.AssignmentOperator)
                {
                    var assignment = (BoundAssignmentOperator)expression;
                    bool valid = assignment.IsRef
                        ? CheckRefEscape(expression.Syntax, assignment.Right, escapeFrom, escapeTo, checkingReceiver: false, diagnostics: diagnostics)
                        : CheckValEscape(expression.Syntax, assignment.Right, escapeFrom, escapeTo, checkingReceiver: false, diagnostics: diagnostics);

                    if (!valid)
                    {
                        return false;
                    }

                    var left = (BoundObjectInitializerMember)assignment.Left;
                    if (!CheckValEscape(left.Arguments, escapeFrom, escapeTo, diagnostics: diagnostics))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!CheckValEscape(expression.Syntax, expression, escapeFrom, escapeTo, checkingReceiver: false, diagnostics: diagnostics))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool CheckValEscape(ImmutableArray<BoundExpression> expressions, uint escapeFrom, uint escapeTo, BindingDiagnosticBag diagnostics)
        {
            foreach (var expression in expressions)
            {
                if (!CheckValEscape(expression.Syntax, expression, escapeFrom, escapeTo, checkingReceiver: false, diagnostics: diagnostics))
                {
                    return false;
                }
            }

            return true;
        }

        private bool CheckInterpolatedStringHandlerConversionEscape(BoundExpression expression, uint escapeFrom, uint escapeTo, BindingDiagnosticBag diagnostics)
        {
            var data = expression.GetInterpolatedStringHandlerData();

            // We need to check to see if any values could potentially escape outside the max depth via the handler type.
            // Consider the case where a ref-struct handler saves off the result of one call to AppendFormatted,
            // and then on a subsequent call it either assigns that saved value to another ref struct with a larger
            // escape, or does the opposite. In either case, we need to check.

#if DEBUG
            // VisitArgumentsAndGetArgumentPlaceholders() does not visit data.Construction
            // since that expression does not introduce locals or placeholders that are needed
            // by GetValEscape() or CheckValEscape(), so we disable tracking here.
            var previousVisited = _visited;
            _visited = null;
#endif
            CheckValEscape(expression.Syntax, data.Construction, escapeFrom, escapeTo, checkingReceiver: false, diagnostics);
#if DEBUG
            _visited = previousVisited;
#endif

            var arguments = ArrayBuilder<BoundExpression>.GetInstance();
            GetInterpolatedStringHandlerArgumentsForEscape(expression, arguments);

            bool result = true;
            foreach (var argument in arguments)
            {
                if (!CheckValEscape(argument.Syntax, argument, escapeFrom, escapeTo, checkingReceiver: false, diagnostics))
                {
                    result = false;
                    break;
                }
            }

            arguments.Free();
            return result;
        }

        private void GetInterpolatedStringHandlerArgumentsForEscape(BoundExpression expression, ArrayBuilder<BoundExpression> arguments)
        {
            while (true)
            {
                switch (expression)
                {
                    case BoundBinaryOperator binary:
                        GetInterpolatedStringHandlerArgumentsForEscape(binary.Right, arguments);
                        expression = binary.Left;
                        break;

                    case BoundInterpolatedString interpolatedString:
                        getParts(interpolatedString);
                        return;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(expression.Kind);
                }
            }

            void getParts(BoundInterpolatedString interpolatedString)
            {
                foreach (var part in interpolatedString.Parts)
                {
                    if (part is not BoundCall { Method.Name: BoundInterpolatedString.AppendFormattedMethod } call)
                    {
                        // Dynamic calls cannot have ref struct parameters, and AppendLiteral calls will always have literal
                        // string arguments and do not require us to be concerned with escape
                        continue;
                    }

                    // The interpolation component is always the first argument to the method, and it was not passed by name
                    // so there can be no reordering.

                    // SPEC: For a given argument `a` that is passed to parameter `p`:
                    // SPEC: 1. ...
                    // SPEC: 2. If `p` is `scoped` then `a` does not contribute *safe-to-escape* when considering arguments.
                    if (_useUpdatedEscapeRules &&
                        call.Method.Parameters[0].EffectiveScope == ScopedKind.ScopedValue)
                    {
                        continue;
                    }

                    arguments.Add(call.Arguments[0]);
                }
            }
        }
    }

    internal partial class Binder
    {
        internal enum AddressKind
        {
            // reference may be written to
            Writeable,

            // reference itself will not be written to, but may be used for call, callvirt.
            // for all purposes it is the same as Writeable, except when fetching an address of an array element
            // where it results in a ".readonly" prefix to deal with array covariance.
            Constrained,

            // reference itself will not be written to, nor it will be used to modify fields.
            ReadOnly,

            // same as ReadOnly, but we are not supposed to get a reference to a clone
            // regardless of compat settings.
            ReadOnlyStrict,
        }

        internal static bool IsAnyReadOnly(AddressKind addressKind) => addressKind >= AddressKind.ReadOnly;

        /// <summary>
        /// Checks if expression directly or indirectly represents a value with its own home. In
        /// such cases it is possible to get a reference without loading into a temporary.
        /// </summary>
        internal static bool HasHome(
            BoundExpression expression,
            AddressKind addressKind,
            Symbol containingSymbol,
            bool peVerifyCompatEnabled,
            HashSet<LocalSymbol> stackLocalsOpt)
        {
            Debug.Assert(containingSymbol is object);

            switch (expression.Kind)
            {
                case BoundKind.ArrayAccess:
                    if (addressKind == AddressKind.ReadOnly && !expression.Type.IsValueType && peVerifyCompatEnabled)
                    {
                        // due to array covariance getting a reference may throw ArrayTypeMismatch when element is not a struct, 
                        // passing "readonly." prefix would prevent that, but it is unverifiable, so will make a copy in compat case
                        return false;
                    }

                    return true;

                case BoundKind.PointerIndirectionOperator:
                case BoundKind.RefValueOperator:
                    return true;

                case BoundKind.ThisReference:
                    var type = expression.Type;
                    if (type.IsReferenceType)
                    {
                        Debug.Assert(IsAnyReadOnly(addressKind), "`this` is readonly in classes");
                        return true;
                    }

                    if (!IsAnyReadOnly(addressKind) && containingSymbol is MethodSymbol { ContainingSymbol: NamedTypeSymbol, IsEffectivelyReadOnly: true })
                    {
                        return false;
                    }

                    return true;

                case BoundKind.ThrowExpression:
                    // vacuously this is true, we can take address of throw without temps
                    return true;

                case BoundKind.Parameter:
                    return IsAnyReadOnly(addressKind) ||
                        ((BoundParameter)expression).ParameterSymbol.RefKind is not (RefKind.In or RefKind.RefReadOnlyParameter);

                case BoundKind.Local:
                    // locals have home unless they are byval stack locals or ref-readonly
                    // locals in a mutating call
                    var local = ((BoundLocal)expression).LocalSymbol;
                    return !((CodeGenerator.IsStackLocal(local, stackLocalsOpt) && local.RefKind == RefKind.None) ||
                        (!IsAnyReadOnly(addressKind) && local.RefKind == RefKind.RefReadOnly));

                case BoundKind.Call:
                    var methodRefKind = ((BoundCall)expression).Method.RefKind;
                    return methodRefKind == RefKind.Ref ||
                           (IsAnyReadOnly(addressKind) && methodRefKind == RefKind.RefReadOnly);

                case BoundKind.Dup:
                    //NB: Dup represents locals that do not need IL slot
                    var dupRefKind = ((BoundDup)expression).RefKind;
                    return dupRefKind == RefKind.Ref ||
                        (IsAnyReadOnly(addressKind) && dupRefKind == RefKind.RefReadOnly);

                case BoundKind.FieldAccess:
                    return FieldAccessHasHome((BoundFieldAccess)expression, addressKind, containingSymbol, peVerifyCompatEnabled, stackLocalsOpt);

                case BoundKind.Sequence:
                    return HasHome(((BoundSequence)expression).Value, addressKind, containingSymbol, peVerifyCompatEnabled, stackLocalsOpt);

                case BoundKind.AssignmentOperator:
                    var assignment = (BoundAssignmentOperator)expression;
                    if (!assignment.IsRef)
                    {
                        return false;
                    }
                    var lhsRefKind = assignment.Left.GetRefKind();
                    return lhsRefKind == RefKind.Ref ||
                        (IsAnyReadOnly(addressKind) && lhsRefKind is RefKind.RefReadOnly or RefKind.RefReadOnlyParameter);

                case BoundKind.ComplexConditionalReceiver:
                    Debug.Assert(HasHome(
                        ((BoundComplexConditionalReceiver)expression).ValueTypeReceiver,
                        addressKind,
                        containingSymbol,
                        peVerifyCompatEnabled,
                        stackLocalsOpt));
                    Debug.Assert(HasHome(
                        ((BoundComplexConditionalReceiver)expression).ReferenceTypeReceiver,
                        addressKind,
                        containingSymbol,
                        peVerifyCompatEnabled,
                        stackLocalsOpt));
                    goto case BoundKind.ConditionalReceiver;

                case BoundKind.ConditionalReceiver:
                    //ConditionalReceiver is a noop from Emit point of view. - it represents something that has already been pushed. 
                    //We should never need a temp for it. 
                    return true;

                case BoundKind.ConditionalOperator:
                    var conditional = (BoundConditionalOperator)expression;

                    // only ref conditional may be referenced as a variable
                    if (!conditional.IsRef)
                    {
                        return false;
                    }

                    // branch that has no home will need a temporary
                    // if both have no home, just say whole expression has no home 
                    // so we could just use one temp for the whole thing
                    return HasHome(conditional.Consequence, addressKind, containingSymbol, peVerifyCompatEnabled, stackLocalsOpt)
                        && HasHome(conditional.Alternative, addressKind, containingSymbol, peVerifyCompatEnabled, stackLocalsOpt);

                default:
                    return false;
            }
        }

        /// <summary>
        /// Special HasHome for fields.
        /// A field has a readable home unless the field is a constant.
        /// A ref readonly field doesn't have a writable home.
        /// Other fields have a writable home unless the field is a readonly value
        /// and is used outside of a constructor or init method.
        /// </summary>
        private static bool FieldAccessHasHome(
            BoundFieldAccess fieldAccess,
            AddressKind addressKind,
            Symbol containingSymbol,
            bool peVerifyCompatEnabled,
            HashSet<LocalSymbol> stackLocalsOpt)
        {
            Debug.Assert(containingSymbol is object);

            FieldSymbol field = fieldAccess.FieldSymbol;

            // const fields are literal values with no homes. (ex: decimal.Zero)
            if (field.IsConst)
            {
                return false;
            }

            if (field.RefKind is RefKind.Ref)
            {
                return true;
            }

            // in readonly situations where ref to a copy is not allowed, consider fields as addressable
            if (addressKind == AddressKind.ReadOnlyStrict)
            {
                return true;
            }

            // ReadOnly references can always be taken unless we are in peverify compat mode
            if (addressKind == AddressKind.ReadOnly && !peVerifyCompatEnabled)
            {
                return true;
            }

            // Some field accesses must be values; values do not have homes.
            if (fieldAccess.IsByValue)
            {
                return false;
            }

            if (field.RefKind == RefKind.RefReadOnly)
            {
                return false;
            }

            Debug.Assert(field.RefKind == RefKind.None);

            if (!field.IsReadOnly)
            {
                // in a case if we have a writeable struct field with a receiver that only has a readable home we would need to pass it via a temp.
                // it would be advantageous to make a temp for the field, not for the outer struct, since the field is smaller and we can get to is by fetching references.
                // NOTE: this would not be profitable if we have to satisfy verifier, since for verifiability 
                //       we would not be able to dig for the inner field using references and the outer struct will have to be copied to a temp anyways.
                if (!peVerifyCompatEnabled)
                {
                    Debug.Assert(!IsAnyReadOnly(addressKind));

                    var receiver = fieldAccess.ReceiverOpt;
                    if (receiver?.Type.IsValueType == true)
                    {
                        // Check receiver:
                        // has writeable home -> return true - the whole chain has writeable home (also a more common case)
                        // has readable home -> return false - we need to copy the field
                        // otherwise         -> return true  - the copy will be made at higher level so the leaf field can have writeable home

                        return HasHome(receiver, addressKind, containingSymbol, peVerifyCompatEnabled, stackLocalsOpt)
                            || !HasHome(receiver, AddressKind.ReadOnly, containingSymbol, peVerifyCompatEnabled, stackLocalsOpt);
                    }
                }

                return true;
            }

            // while readonly fields have home it is not valid to refer to it when not constructing.
            if (!TypeSymbol.Equals(field.ContainingType, containingSymbol.ContainingSymbol as NamedTypeSymbol, TypeCompareKind.AllIgnoreOptions))
            {
                return false;
            }

            if (field.IsStatic)
            {
                return containingSymbol is MethodSymbol { MethodKind: MethodKind.StaticConstructor } or FieldSymbol { IsStatic: true };
            }
            else
            {
                return (containingSymbol is MethodSymbol { MethodKind: MethodKind.Constructor } or FieldSymbol { IsStatic: false } or MethodSymbol { IsInitOnly: true }) &&
                    fieldAccess.ReceiverOpt.Kind == BoundKind.ThisReference;
            }
        }
    }
}
