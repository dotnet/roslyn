// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class RefSafetyAnalysis
    {
        private enum EscapeLevel
        {
            CallingMethod,
            ReturnOnly
        }

        /// <summary>
        /// Encapsulates a symbol used in ref safety analysis. For properties and indexers this
        /// captures the accessor(s) on it that were used. The particular accessor used is 
        /// important as it can impact ref safety analysis.
        /// </summary>
        private readonly struct MethodInfo
        {
            internal Symbol Symbol { get; }

            /// <summary>
            /// This is the primary <see cref="MethodSymbol" /> used in ref safety analysis.
            /// </summary>
            /// <remarks>
            /// This will be null in error scenarios. For example when an indexer with only a set
            /// method is used in a get scenario. That will lead to a non-null <see cref="MethodInfo.Symbol"/>
            /// but a null value here.
            /// </remarks>
            internal MethodSymbol? Method { get; }

            /// <summary>
            /// In the case of a compound operation on non-ref return property or indexer 
            /// <see cref="Method"/> will represent the `get` accessor and this will 
            /// represent the `set` accessor. 
            /// </summary>
            internal MethodSymbol? SetMethod { get; }

            internal bool UseUpdatedEscapeRules => Method?.UseUpdatedEscapeRules == true;
            internal bool ReturnsRefToRefStruct =>
                Method is { RefKind: not RefKind.None, ReturnType: { } returnType } &&
                returnType.IsRefLikeOrAllowsRefLikeType();

            private MethodInfo(Symbol symbol, MethodSymbol? method, MethodSymbol? setMethod)
            {
                Symbol = symbol;
                Method = method;
                SetMethod = setMethod;
            }

            internal static MethodInfo Create(MethodSymbol method)
            {
                return new MethodInfo(method, method, null);
            }

            internal static MethodInfo Create(PropertySymbol property)
            {
                return new MethodInfo(
                    property,
                    property.GetOwnOrInheritedGetMethod() ?? property.GetOwnOrInheritedSetMethod(),
                    null);
            }

            internal static MethodInfo Create(PropertySymbol property, AccessorKind accessorKind) =>
                accessorKind switch
                {
                    AccessorKind.Get => new MethodInfo(property, property.GetOwnOrInheritedGetMethod(), setMethod: null),
                    AccessorKind.Set => new MethodInfo(property, property.GetOwnOrInheritedSetMethod(), setMethod: null),
                    AccessorKind.Both => new MethodInfo(property, property.GetOwnOrInheritedGetMethod(), property.GetOwnOrInheritedSetMethod()),
                    _ => throw ExceptionUtilities.UnexpectedValue(accessorKind),
                };

            internal static MethodInfo Create(BoundIndexerAccess expr) =>
                Create(expr.Indexer, expr.AccessorKind);

            internal MethodInfo ReplaceWithExtensionImplementation(out bool wasError)
            {
                var method = replace(Method);
                var setMethod = replace(SetMethod);
                Symbol symbol = ReferenceEquals(Symbol, Method) && method is not null ? method : Symbol;

                Debug.Assert(SetMethod?.IsExtensionBlockMember() != true);
                wasError = (Method is not null && method is null) || (SetMethod is not null && setMethod is null);

                return new MethodInfo(symbol, method, setMethod);

                static MethodSymbol? replace(MethodSymbol? method)
                {
                    if (method is null)
                    {
                        return null;
                    }

                    if (method.OriginalDefinition.TryGetCorrespondingExtensionImplementationMethod() is MethodSymbol implementationMethod)
                    {
                        return implementationMethod.AsMember(method.ContainingSymbol.ContainingType).
                            ConstructIfGeneric(method.ContainingType.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics.Concat(method.TypeArgumentsWithAnnotations));
                    }

                    // Valid scenarios shouldn't get to here. These are the cases we know can get to here. If this assert triggers for
                    // an invalid scenario, it can simply be added. If it triggers for a valid scenario, the above code should be updated to handle it.
                    Debug.Assert(method is ErrorMethodSymbol or { HasUnsupportedMetadata: true });
                    return null;
                }
            }

            public override string? ToString() => Method?.ToString();
        }

        private struct MethodInvocationInfo
        {
            public MethodInfo MethodInfo;
            public ImmutableArray<ParameterSymbol> Parameters;
            public BoundExpression? Receiver;
            public ThreeState ReceiverIsSubjectToCloning;
            public ImmutableArray<BoundExpression> ArgsOpt;
            public ImmutableArray<RefKind> ArgumentRefKindsOpt;
            public ImmutableArray<int> ArgsToParamsOpt;
            public bool HasAnyErrors;

            public static MethodInvocationInfo FromCall(BoundCall call, BoundExpression? substitutedReceiver = null)
                => new MethodInvocationInfo
                {
                    MethodInfo = MethodInfo.Create(call.Method),
                    Parameters = call.Method.Parameters,
                    Receiver = substitutedReceiver ?? call.ReceiverOpt,
                    ReceiverIsSubjectToCloning = call.InitialBindingReceiverIsSubjectToCloning,
                    ArgsOpt = call.Arguments,
                    ArgumentRefKindsOpt = call.ArgumentRefKindsOpt,
                    ArgsToParamsOpt = call.ArgsToParamsOpt,
                    HasAnyErrors = call.HasAnyErrors
                };

            public static MethodInvocationInfo FromCallParts(MethodSymbol method, BoundExpression receiver, ImmutableArray<BoundExpression> args, ThreeState receiverIsSubjectToCloning)
                => new MethodInvocationInfo
                {
                    MethodInfo = MethodInfo.Create(method),
                    Parameters = method.Parameters,
                    Receiver = receiver,
                    ReceiverIsSubjectToCloning = receiverIsSubjectToCloning,
                    ArgsOpt = args,
                    ArgumentRefKindsOpt = default,
                    ArgsToParamsOpt = default,
                    HasAnyErrors = false
                };

            public static MethodInvocationInfo FromFunctionPointerInvocation(BoundFunctionPointerInvocation ptrInvocation)
            {
                var methodSymbol = ptrInvocation.FunctionPointer.Signature;
                return new MethodInvocationInfo
                {
                    MethodInfo = MethodInfo.Create(methodSymbol),
                    Receiver = null,
                    ReceiverIsSubjectToCloning = ThreeState.Unknown,
                    Parameters = methodSymbol.Parameters,
                    ArgsOpt = ptrInvocation.Arguments,
                    ArgumentRefKindsOpt = ptrInvocation.ArgumentRefKindsOpt,
                    ArgsToParamsOpt = default,
                    HasAnyErrors = ptrInvocation.HasAnyErrors
                };
            }

            public static MethodInvocationInfo FromIndexerAccess(BoundIndexerAccess indexerAccess, BoundExpression? substitutedReceiver = null)
                => new MethodInvocationInfo
                {
                    MethodInfo = MethodInfo.Create(indexerAccess),
                    Receiver = substitutedReceiver ?? indexerAccess.ReceiverOpt,
                    ReceiverIsSubjectToCloning = indexerAccess.InitialBindingReceiverIsSubjectToCloning,
                    Parameters = indexerAccess.Indexer.Parameters,
                    ArgsOpt = indexerAccess.Arguments,
                    ArgumentRefKindsOpt = indexerAccess.ArgumentRefKindsOpt,
                    ArgsToParamsOpt = indexerAccess.ArgsToParamsOpt,
                    HasAnyErrors = indexerAccess.HasAnyErrors
                };

            public static MethodInvocationInfo FromObjectCreation(BoundObjectCreationExpressionBase objectCreation)
            {
                Debug.Assert(objectCreation.Constructor is not null);
                return new MethodInvocationInfo
                {
                    MethodInfo = MethodInfo.Create(objectCreation.Constructor),
                    Receiver = null,
                    ReceiverIsSubjectToCloning = ThreeState.Unknown,
                    Parameters = objectCreation.Constructor.Parameters,
                    ArgsOpt = objectCreation.Arguments,
                    ArgumentRefKindsOpt = objectCreation.ArgumentRefKindsOpt,
                    ArgsToParamsOpt = objectCreation.ArgsToParamsOpt,
                    HasAnyErrors = objectCreation.HasAnyErrors
                };
            }

            public static MethodInvocationInfo FromUnaryOperator(BoundUnaryOperator unaryOperator)
            {
                Debug.Assert(unaryOperator.MethodOpt is not null);
                return new MethodInvocationInfo
                {
                    MethodInfo = MethodInfo.Create(unaryOperator.MethodOpt),
                    Receiver = null,
                    ReceiverIsSubjectToCloning = ThreeState.Unknown,
                    Parameters = unaryOperator.MethodOpt.Parameters,
                    ArgsOpt = [unaryOperator.Operand],
                    ArgumentRefKindsOpt = default,
                    ArgsToParamsOpt = default,
                    HasAnyErrors = unaryOperator.HasAnyErrors
                };
            }

            public static MethodInvocationInfo FromBinaryOperator(BoundBinaryOperator binaryOperator)
            {
                var binaryOperatorMethod = binaryOperator.BinaryOperatorMethod;
                Debug.Assert(binaryOperatorMethod is not null);
                return new MethodInvocationInfo
                {
                    MethodInfo = MethodInfo.Create(binaryOperatorMethod),
                    Receiver = null,
                    ReceiverIsSubjectToCloning = ThreeState.Unknown,
                    Parameters = binaryOperatorMethod.Parameters,
                    ArgsOpt = [binaryOperator.Left, binaryOperator.Right],
                    ArgumentRefKindsOpt = default,
                    ArgsToParamsOpt = default,
                    HasAnyErrors = binaryOperator.HasAnyErrors
                };
            }

            public static MethodInvocationInfo FromUserDefinedConditionalLogicalOperator(BoundUserDefinedConditionalLogicalOperator logicalOperator)
                => new MethodInvocationInfo
                {
                    MethodInfo = MethodInfo.Create(logicalOperator.LogicalOperator),
                    Receiver = null,
                    ReceiverIsSubjectToCloning = ThreeState.Unknown,
                    Parameters = logicalOperator.LogicalOperator.Parameters,
                    ArgsOpt = [logicalOperator.Left, logicalOperator.Right],
                    ArgumentRefKindsOpt = default,
                    ArgsToParamsOpt = default,
                    HasAnyErrors = logicalOperator.HasAnyErrors
                };

            public static MethodInvocationInfo FromUserDefinedConversion(MethodSymbol operatorMethod, BoundExpression operand, bool hasAnyErrors)
                => new MethodInvocationInfo
                {
                    MethodInfo = MethodInfo.Create(operatorMethod),
                    Receiver = null,
                    ReceiverIsSubjectToCloning = ThreeState.Unknown,
                    Parameters = operatorMethod.Parameters,
                    ArgsOpt = [operand],
                    ArgumentRefKindsOpt = default,
                    ArgsToParamsOpt = default,
                    HasAnyErrors = hasAnyErrors
                };

            public static MethodInvocationInfo FromInlineArrayConversion(SignatureOnlyMethodSymbol equivalentSignatureMethod, ImmutableArray<BoundExpression> arguments, ImmutableArray<RefKind> refKinds, bool hasAnyErrors)
                => new MethodInvocationInfo
                {
                    MethodInfo = MethodInfo.Create(equivalentSignatureMethod),
                    Receiver = null,
                    ReceiverIsSubjectToCloning = ThreeState.Unknown,
                    Parameters = equivalentSignatureMethod.Parameters,
                    ArgsOpt = arguments,
                    ArgumentRefKindsOpt = refKinds,
                    ArgsToParamsOpt = default,
                    HasAnyErrors = hasAnyErrors
                };

            public static MethodInvocationInfo FromIncrementOperator(BoundIncrementOperator incrementOperator)
            {
                Debug.Assert(incrementOperator.MethodOpt is not null);
                return new MethodInvocationInfo
                {
                    MethodInfo = MethodInfo.Create(incrementOperator.MethodOpt),
                    Receiver = null,
                    ReceiverIsSubjectToCloning = ThreeState.Unknown,
                    Parameters = incrementOperator.MethodOpt.Parameters,
                    ArgsOpt = [incrementOperator.Operand],
                    ArgumentRefKindsOpt = default,
                    ArgsToParamsOpt = default,
                    HasAnyErrors = incrementOperator.HasAnyErrors
                };
            }

            public static MethodInvocationInfo FromCompoundAssignmentOperator(BoundCompoundAssignmentOperator compoundOperator)
            {
                var method = compoundOperator.Operator.Method;
                Debug.Assert(method is not null);
                return new MethodInvocationInfo
                {
                    MethodInfo = MethodInfo.Create(method),
                    Receiver = method.IsStatic ? null : compoundOperator.Left,
                    ReceiverIsSubjectToCloning = method.IsStatic ? ThreeState.Unknown : ThreeState.False,
                    Parameters = method.Parameters,
                    ArgsOpt = method.IsStatic ? [compoundOperator.Left, compoundOperator.Right] : [compoundOperator.Right],
                    ArgumentRefKindsOpt = default,
                    ArgsToParamsOpt = default,
                    HasAnyErrors = compoundOperator.HasAnyErrors
                };
            }

            public static MethodInvocationInfo FromInlineArrayAccess(SignatureOnlyMethodSymbol equivalentSignatureMethod, ImmutableArray<BoundExpression> arguments, ImmutableArray<RefKind> refKinds, bool hasAnyErrors)
                => new MethodInvocationInfo
                {
                    MethodInfo = MethodInfo.Create(equivalentSignatureMethod),
                    Receiver = null,
                    ReceiverIsSubjectToCloning = ThreeState.Unknown,
                    Parameters = equivalentSignatureMethod.Parameters,
                    ArgsOpt = arguments,
                    ArgumentRefKindsOpt = refKinds,
                    ArgsToParamsOpt = default,
                    HasAnyErrors = hasAnyErrors
                };

            public static MethodInvocationInfo FromProperty(BoundPropertyAccess propertyAccess)
                => new MethodInvocationInfo
                {
                    MethodInfo = MethodInfo.Create(propertyAccess.PropertySymbol),
                    Receiver = propertyAccess.ReceiverOpt,
                    ReceiverIsSubjectToCloning = propertyAccess.InitialBindingReceiverIsSubjectToCloning,
                    HasAnyErrors = propertyAccess.HasAnyErrors,
                };

            public static MethodInvocationInfo FromCollectionElementInitializer(BoundCollectionElementInitializer colElement)
                => new MethodInvocationInfo
                {
                    MethodInfo = MethodInfo.Create(colElement.AddMethod),
                    Parameters = colElement.AddMethod.Parameters,
                    Receiver = colElement.ImplicitReceiverOpt,
                    ArgsOpt = colElement.Arguments,
                    ArgsToParamsOpt = colElement.ArgsToParamsOpt,
                };
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
                Debug.Assert(parameter.RefKind.IsWritableReference() && parameter.Type.IsRefLikeOrAllowsRefLikeType());
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

        private static AccessorKind GetIndexerAccessorKind(BoundIndexerAccess indexerAccess, BindValueKind valueKind)
        {
            if (indexerAccess.Indexer.RefKind != RefKind.None)
            {
                return AccessorKind.Get;
            }

            return GetAccessorKind(valueKind);
        }

        private static AccessorKind GetAccessorKind(BindValueKind valueKind)
        {
            var coreValueKind = valueKind & ValueKindSignificantBitsMask;
            return coreValueKind switch
            {
                BindValueKind.CompoundAssignment => AccessorKind.Both,
                BindValueKind.Assignable => AccessorKind.Set,
                _ => AccessorKind.Get,
            };
        }

        private BoundIndexerAccess BindIndexerDefaultArgumentsAndParamsCollection(BoundIndexerAccess indexerAccess, BindValueKind valueKind, BindingDiagnosticBag diagnostics)
        {
            var coreValueKind = valueKind & ValueKindSignificantBitsMask;
            AccessorKind accessorKind = GetIndexerAccessorKind(indexerAccess, valueKind);
            var useSetAccessor = coreValueKind == BindValueKind.Assignable && indexerAccess.Indexer.RefKind != RefKind.Ref;
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

                    // Tracked by https://github.com/dotnet/roslyn/issues/78829 : caller info on extension parameter of an extension indexer will need the receiver/argument to be passed
                    Debug.Assert(!indexerAccess.Indexer.IsExtensionBlockMember());
                    BindDefaultArguments(indexerAccess.Syntax, parameters, extensionReceiver: null, argumentsBuilder, refKindsBuilderOpt, namesBuilder, ref argsToParams, out defaultArguments, indexerAccess.Expanded, enableCallerInfo: true, diagnostics: diagnostics);

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
                    accessorKind,
                    argsToParams,
                    defaultArguments,
                    indexerAccess.Type);

                refKindsBuilderOpt?.Free();

                return indexerAccess;
            }

            return indexerAccess.Update(accessorKind);
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
                    {
                        expr = BindIndexedPropertyAccess((BoundPropertyGroup)expr, mustHaveAllOptionalParameters: false, diagnostics: diagnostics);
                        if (expr is BoundIndexerAccess indexerAccess)
                        {
                            expr = BindIndexerDefaultArgumentsAndParamsCollection(indexerAccess, valueKind, diagnostics);
                        }
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

                case BoundKind.PropertyAccess:
                    if (!InAttributeArgument)
                    {
                        // If the property has a synthesized backing field, record the accessor kind of the property
                        // access for determining whether the property access can use the backing field directly.
                        var propertyAccess = (BoundPropertyAccess)expr;
                        if (HasSynthesizedBackingField(propertyAccess.PropertySymbol, out _))
                        {
                            expr = propertyAccess.Update(
                                propertyAccess.ReceiverOpt,
                                propertyAccess.InitialBindingReceiverIsSubjectToCloning,
                                propertyAccess.PropertySymbol,
                                autoPropertyAccessorKind: GetAccessorKind(valueKind),
                                propertyAccess.ResultKind,
                                propertyAccess.Type);
                        }
                    }
#if DEBUG
                    expr.WasPropertyBackingFieldAccessChecked = true;
#endif
                    break;

                case BoundKind.IndexerAccess:
                    expr = BindIndexerDefaultArgumentsAndParamsCollection((BoundIndexerAccess)expr, valueKind, diagnostics);
                    break;

                case BoundKind.ImplicitIndexerAccess:
                    {
                        var implicitIndexer = (BoundImplicitIndexerAccess)expr;
                        if (implicitIndexer.IndexerOrSliceAccess is BoundIndexerAccess indexerAccess)
                        {
                            var kind = GetIndexerAccessorKind(indexerAccess, valueKind);
                            expr = implicitIndexer.Update(
                                implicitIndexer.Receiver,
                                implicitIndexer.Argument,
                                implicitIndexer.LengthOrCountAccess,
                                implicitIndexer.ReceiverPlaceholder,
                                indexerAccess.Update(kind),
                                implicitIndexer.ArgumentPlaceholders,
                                implicitIndexer.Type);
                        }
                    }
                    break;

                case BoundKind.UnconvertedObjectCreationExpression:
                case BoundKind.UnconvertedCollectionExpression:
                case BoundKind.TupleLiteral:
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
                var resolution = this.ResolveMethodGroup(methodGroup, analyzedArguments: null, useSiteInfo: ref useSiteInfo, options: OverloadResolution.Options.None, acceptOnlyMethods: true);
                Debug.Assert(!resolution.IsNonMethodExtensionMember(out _));
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
                    if (RequiresRefAssignableVariable(valueKind))
                    {
                        Error(diagnostics, ErrorCode.ERR_RefLocalOrParamExpected, node);
                        return false;
                    }

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
                    // Cannot ref-assign to a ref assignment.
                    if (RequiresRefAssignableVariable(valueKind))
                    {
                        Error(diagnostics, ErrorCode.ERR_RefLocalOrParamExpected, node);
                        return false;
                    }

                    var assignment = (BoundAssignmentOperator)expr;
                    return CheckSimpleAssignmentValueKind(node, assignment, valueKind, diagnostics);

                case BoundKind.ValuePlaceholder:
                    // Strict RValue
                    break;

                default:
                    RoslynDebug.Assert(expr is not BoundValuePlaceholderBase, $"Placeholder kind {expr.Kind} should be explicitly handled");
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

        private void CheckAddressOfInAsyncOrIteratorMethod(SyntaxNode node, BindValueKind valueKind, BindingDiagnosticBag diagnostics)
        {
            if (valueKind == BindValueKind.AddressOf)
            {
                if (this.IsInAsyncMethod())
                {
                    Error(diagnostics, ErrorCode.WRN_AddressOfInAsync, node);
                }
                else if (this.IsDirectlyInIterator && Compilation.IsFeatureEnabled(MessageID.IDS_FeatureRefUnsafeInIteratorAsync))
                {
                    Error(diagnostics, ErrorCode.ERR_AddressOfInIterator, node);
                }
            }
        }

        private bool CheckLocalValueKind(SyntaxNode node, BoundLocal local, BindValueKind valueKind, bool checkingReceiver, BindingDiagnosticBag diagnostics)
        {
            CheckAddressOfInAsyncOrIteratorMethod(node, valueKind, diagnostics);

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
        private bool CheckLocalRefEscape(SyntaxNode node, BoundLocal local, SafeContext escapeTo, bool checkingReceiver, BindingDiagnosticBag diagnostics)
        {
            LocalSymbol localSymbol = local.LocalSymbol;

            // if local symbol can escape to the same or wider/shallower scope then escapeTo
            // then it is all ok, otherwise it is an error.
            if (GetLocalScopes(localSymbol).RefEscapeScope.IsConvertibleTo(escapeTo))
            {
                return true;
            }

            var inUnsafeRegion = _inUnsafeRegion;
            if (escapeTo.IsReturnable)
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

            CheckAddressOfInAsyncOrIteratorMethod(node, valueKind, diagnostics);

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
        private static EscapeLevel? EscapeLevelFromScope(SafeContext lifetime) => lifetime switch
        {
            { IsReturnOnly: true } => EscapeLevel.ReturnOnly,
            { IsCallingMethod: true } => EscapeLevel.CallingMethod,
            _ => null,
        };

        private static SafeContext GetParameterValEscape(ParameterSymbol parameter)
        {
            return parameter switch
            {
                { EffectiveScope: ScopedKind.ScopedValue } => SafeContext.CurrentMethod,
                { RefKind: RefKind.Out, UseUpdatedEscapeRules: true } => SafeContext.ReturnOnly,
                _ => SafeContext.CallingMethod
            };
        }

        private static EscapeLevel? GetParameterValEscapeLevel(ParameterSymbol parameter) =>
            EscapeLevelFromScope(GetParameterValEscape(parameter));

        private static SafeContext GetParameterRefEscape(ParameterSymbol parameter)
        {
            return parameter switch
            {
                { RefKind: RefKind.None } => SafeContext.CurrentMethod,
                { EffectiveScope: ScopedKind.ScopedRef } => SafeContext.CurrentMethod,
                { HasUnscopedRefAttribute: true, UseUpdatedEscapeRules: true, RefKind: RefKind.Out } => SafeContext.ReturnOnly,
                { HasUnscopedRefAttribute: true, UseUpdatedEscapeRules: true, IsThis: false } => SafeContext.CallingMethod,
                _ => SafeContext.ReturnOnly
            };
        }

        private static EscapeLevel? GetParameterRefEscapeLevel(ParameterSymbol parameter) =>
            EscapeLevelFromScope(GetParameterRefEscape(parameter));

        private bool CheckParameterValEscape(SyntaxNode node, ParameterSymbol parameter, SafeContext escapeTo, BindingDiagnosticBag diagnostics)
        {
            if (_useUpdatedEscapeRules)
            {
                if (!GetParameterValEscape(parameter).IsConvertibleTo(escapeTo))
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

        private bool CheckParameterRefEscape(SyntaxNode node, BoundExpression parameter, ParameterSymbol parameterSymbol, SafeContext escapeTo, bool checkingReceiver, BindingDiagnosticBag diagnostics)
        {
            var refSafeToEscape = GetParameterRefEscape(parameterSymbol);
            if (!refSafeToEscape.IsConvertibleTo(escapeTo))
            {
                var isRefScoped = parameterSymbol.EffectiveScope == ScopedKind.ScopedRef;
                Debug.Assert(parameterSymbol.RefKind == RefKind.None || isRefScoped || refSafeToEscape.IsReturnOnly);
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
                    (checkingReceiver: true,  isRefScoped: false, inUnsafeRegion: false, { IsReturnOnly: true }) => (ErrorCode.ERR_RefReturnOnlyParameter2,   parameter.Syntax),
                    (checkingReceiver: true,  isRefScoped: false, inUnsafeRegion: true,  { IsReturnOnly: true }) => (ErrorCode.WRN_RefReturnOnlyParameter2,   parameter.Syntax),
                    (checkingReceiver: true,  isRefScoped: false, inUnsafeRegion: false, _)                      => (ErrorCode.ERR_RefReturnParameter2,       parameter.Syntax),
                    (checkingReceiver: true,  isRefScoped: false, inUnsafeRegion: true,  _)                      => (ErrorCode.WRN_RefReturnParameter2,       parameter.Syntax),
                    (checkingReceiver: false, isRefScoped: true,  inUnsafeRegion: false, _)                      => (ErrorCode.ERR_RefReturnScopedParameter,  node),
                    (checkingReceiver: false, isRefScoped: true,  inUnsafeRegion: true,  _)                      => (ErrorCode.WRN_RefReturnScopedParameter,  node),
                    (checkingReceiver: false, isRefScoped: false, inUnsafeRegion: false, { IsReturnOnly: true }) => (ErrorCode.ERR_RefReturnOnlyParameter,    node),
                    (checkingReceiver: false, isRefScoped: false, inUnsafeRegion: true,  { IsReturnOnly: true }) => (ErrorCode.WRN_RefReturnOnlyParameter,    node),
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
                Debug.Assert(valueKind == BindValueKind.RefAssignable);

                switch (fieldSymbol.RefKind)
                {
                    case RefKind.None:
                        Error(diagnostics, ErrorCode.ERR_RefLocalOrParamExpected, node);
                        return false;
                    case RefKind.Ref:
                    case RefKind.RefReadOnly:
                        if (fieldSymbol.IsStatic)
                        {
                            Debug.Assert(fieldAccess.ReceiverOpt is null or BoundTypeExpression);
                            break;
                        }
                        else
                        {
                            Debug.Assert(fieldAccess.ReceiverOpt is not null);
                            return CheckIsValidReceiverForVariable(node, fieldAccess.ReceiverOpt, BindValueKind.Assignable, diagnostics);
                        }
                    default:
                        throw ExceptionUtilities.UnexpectedValue(fieldSymbol.RefKind);
                }
            }

            if (RequiresReferenceToLocation(valueKind))
            {
                switch (fieldSymbol.RefKind)
                {
                    case RefKind.None:
                        break;
                    case RefKind.Ref:
                    case RefKind.RefReadOnly:
                        // ref readonly access to a ref (readonly) field is fine regardless of the receiver
                        return true;
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
        private SafeContext GetFieldRefEscape(BoundFieldAccess fieldAccess)
        {
            var fieldSymbol = fieldAccess.FieldSymbol;

            // fields that are static or belong to reference types can ref escape anywhere
            if (fieldSymbol.IsStatic || fieldSymbol.ContainingType.IsReferenceType)
            {
                return SafeContext.CallingMethod;
            }

            if (_useUpdatedEscapeRules)
            {
                // SPEC: If `F` is a `ref` field its ref-safe-to-escape scope is the safe-to-escape scope of `e`.
                if (fieldSymbol.RefKind != RefKind.None)
                {
                    return GetValEscape(fieldAccess.ReceiverOpt);
                }
            }

            // for other fields defer to the receiver.
            return GetRefEscape(fieldAccess.ReceiverOpt);
        }

        private bool CheckFieldRefEscape(SyntaxNode node, BoundFieldAccess fieldAccess, SafeContext escapeTo, BindingDiagnosticBag diagnostics)
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
                    return CheckValEscape(node, fieldAccess.ReceiverOpt, escapeTo, checkingReceiver: true, diagnostics);
                }
            }

            // for other fields defer to the receiver.
            return CheckRefEscape(node, fieldAccess.ReceiverOpt, escapeTo, checkingReceiver: true, diagnostics: diagnostics);
        }

        private bool CheckFieldLikeEventRefEscape(SyntaxNode node, BoundEventAccess eventAccess, SafeContext escapeTo, BindingDiagnosticBag diagnostics)
        {
            var eventSymbol = eventAccess.EventSymbol;

            // field-like events that are static or belong to reference types can ref escape anywhere
            if (eventSymbol.IsStatic || eventSymbol.ContainingType.IsReferenceType)
            {
                return true;
            }

            // for other events defer to the receiver.
            return CheckRefEscape(node, eventAccess.ReceiverOpt, escapeTo, checkingReceiver: true, diagnostics: diagnostics);
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
                    if (!AccessingAutoPropertyFromConstructor(receiver, propertySymbol, containing, AccessorKind.Set)
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
        internal SafeContext GetInterpolatedStringHandlerConversionEscapeScope(
            BoundExpression expression)
        {
            var data = expression.GetInterpolatedStringHandlerData();
#if DEBUG
            // VisitArgumentsAndGetArgumentPlaceholders() does not visit data.Construction
            // since that expression does not introduce locals or placeholders that are needed
            // by GetValEscape() or CheckValEscape(), so we disable tracking here.
            var previousVisited = _visited;
            _visited = null;
#endif
            SafeContext escapeScope = GetValEscape(data.Construction);
#if DEBUG
            _visited = previousVisited;
#endif

            // Narrow the scope for implicit calls which allow the receiver to capture refs from the arguments.
            escapeScope = escapeScope.Intersect(GetValEscapeOfInterpolatedStringHandlerCalls(expression));

            return escapeScope;
        }

#nullable enable

        /// <summary>
        /// Computes the scope to which the given invocation can escape
        /// NOTE: the escape scope for ref and val escapes is the same for invocations except for trivial cases (ordinary type returned by val) 
        ///       where escape is known otherwise. Therefore we do not have two ref/val variants of this.
        ///       
        /// NOTE: we need localScopeDepth as some expressions such as optional <c>in</c> parameters or <c>ref dynamic</c> behave as 
        ///       local variables declared at the scope of the invocation.
        /// </summary>
        private SafeContext GetInvocationEscapeScope(
            in MethodInvocationInfo methodInvocationInfo,
            bool isRefEscape
        )
        {
#if DEBUG
            Debug.Assert(AllParametersConsideredInEscapeAnalysisHaveArguments(in methodInvocationInfo));
#endif

            var localMethodInvocationInfo = ReplaceWithExtensionImplementationIfNeeded(in methodInvocationInfo);

            if (methodInvocationInfo.MethodInfo.UseUpdatedEscapeRules)
            {
                return GetInvocationEscapeWithUpdatedRules(in localMethodInvocationInfo, isRefEscape);
            }

            return getInvocationEscapeWithOldRules(in localMethodInvocationInfo, isRefEscape);

            SafeContext getInvocationEscapeWithOldRules(ref readonly MethodInvocationInfo methodInvocationInfo, bool isRefEscape)
            {
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

                SafeContext escapeScope = SafeContext.CallingMethod;
                var escapeValues = ArrayBuilder<EscapeValue>.GetInstance();
                GetEscapeValuesForOldRules(
                    in methodInvocationInfo,
                    // ref kinds of varargs are not interesting here. 
                    // __refvalue is not ref-returnable, so ref varargs can't come back from a call
                    ignoreArglistRefKinds: true,
                    mixableArguments: null,
                    escapeValues);

                try
                {
                    foreach (var (parameter, argument, _, argumentIsRefEscape) in escapeValues)
                    {
                        // ref escape scope is the narrowest of 
                        // - ref escape of all byref arguments
                        // - val escape of all byval arguments  (ref-like values can be unwrapped into refs, so treat val escape of values as possible ref escape of the result)
                        //
                        // val escape scope is the narrowest of 
                        // - val escape of all byval arguments  (refs cannot be wrapped into values, so their ref escape is irrelevant, only use val escapes)
                        SafeContext argumentEscape = (isRefEscape, argumentIsRefEscape) switch
                        {
                            (true, true) => GetRefEscape(argument),
                            (false, false) => GetValEscape(argument),
                            _ => escapeScope
                        };

                        escapeScope = escapeScope.Intersect(argumentEscape);
                        if (_localScopeDepth.IsConvertibleTo(escapeScope))
                        {
                            // can't get any worse
                            return escapeScope;
                        }
                    }
                }
                finally
                {
                    escapeValues.Free();
                }

                // check receiver if ref-like
                if (methodInvocationInfo.MethodInfo.Method?.RequiresInstanceReceiver == true && methodInvocationInfo.Receiver?.Type?.IsRefLikeOrAllowsRefLikeType() == true)
                {
                    escapeScope = escapeScope.Intersect(GetValEscape(methodInvocationInfo.Receiver));
                }

                return escapeScope;
            }
        }

        private SafeContext GetInvocationEscapeWithUpdatedRules(
            ref readonly MethodInvocationInfo methodInvocationInfo,
            bool isRefEscape)
        {
            //by default it is safe to escape
            SafeContext escapeScope = SafeContext.CallingMethod;

            var argsAndParamsAll = ArrayBuilder<EscapeValue>.GetInstance();
            GetFilteredInvocationArgumentsForEscapeWithUpdatedRules(
                in methodInvocationInfo,
                isRefEscape,
                ignoreArglistRefKinds: true, // https://github.com/dotnet/roslyn/issues/63325: for compatibility with C#10 implementation.
                argsAndParamsAll);

            var returnsRefToRefStruct = methodInvocationInfo.MethodInfo.ReturnsRefToRefStruct;
            foreach (var (param, argument, _, isArgumentRefEscape) in argsAndParamsAll)
            {
                // SPEC:
                // If `M()` does return ref-to-ref-struct, the *safe-to-escape* is the same as the *safe-to-escape* of all arguments which are ref-to-ref-struct. It is an error if there are multiple arguments with different *safe-to-escape* because of *method arguments must match*.
                // If `M()` does return ref-to-ref-struct, the *ref-safe-to-escape* is the narrowest *ref-safe-to-escape* contributed by all arguments which are ref-to-ref-struct.
                //
                if (!returnsRefToRefStruct
                    || ((param is null ||
                         (param is { RefKind: not RefKind.None, Type: { } type } && type.IsRefLikeOrAllowsRefLikeType())) &&
                        isArgumentRefEscape == isRefEscape))
                {
                    SafeContext argEscape = isArgumentRefEscape ?
                        GetRefEscape(argument) :
                        GetValEscape(argument);

                    escapeScope = escapeScope.Intersect(argEscape);
                    if (_localScopeDepth.IsConvertibleTo(escapeScope))
                    {
                        // can't get any worse
                        break;
                    }
                }
            }
            argsAndParamsAll.Free();

            return escapeScope;
        }

        private SafeContext GetInvocationEscapeToReceiver(
            in MethodInvocationInfo methodInvocationInfo)
        {
            // By default it is safe to escape.
            SafeContext escapeScope = SafeContext.CallingMethod;

            var escapeValues = ArrayBuilder<EscapeValue>.GetInstance();
            GetFilteredInvocationArgumentsForEscapeToReceiver(in methodInvocationInfo, escapeValues);

            foreach (var (_, argument, _, isArgumentRefEscape) in escapeValues)
            {
                SafeContext argEscape = isArgumentRefEscape
                    ? GetRefEscape(argument)
                    : GetValEscape(argument);

                escapeScope = escapeScope.Intersect(argEscape);
                if (_localScopeDepth.IsConvertibleTo(escapeScope))
                {
                    // Can't get any worse.
                    break;
                }
            }

            escapeValues.Free();

            return escapeScope;
        }

        private static MethodInvocationInfo ReplaceWithExtensionImplementationIfNeeded(ref readonly MethodInvocationInfo methodInvocationInfo)
        {
            Symbol? symbol = methodInvocationInfo.MethodInfo.Symbol;
            if (symbol?.IsExtensionBlockMember() != true || symbol.IsStatic)
            {
                return methodInvocationInfo;
            }

            MethodInfo replacedMethodInfo = methodInvocationInfo.MethodInfo.ReplaceWithExtensionImplementation(out bool wasError);
            if (wasError)
            {
                return methodInvocationInfo;
            }

            var result = methodInvocationInfo with { MethodInfo = replacedMethodInfo };
            var extensionParameter = symbol.ContainingType.ExtensionParameter;
            Debug.Assert(extensionParameter is not null);
            result.Parameters = methodInvocationInfo.Parameters.IsDefault ? [extensionParameter] : [extensionParameter, .. methodInvocationInfo.Parameters];

            if (methodInvocationInfo.Receiver is not null)
            {
                result.ArgsOpt = methodInvocationInfo.ArgsOpt.IsDefault ? [methodInvocationInfo.Receiver] : [methodInvocationInfo.Receiver, .. methodInvocationInfo.ArgsOpt];
                result.Receiver = null;
            }
            else
            {
                Debug.Assert(methodInvocationInfo.HasAnyErrors, "Got a null receiver for a non-static extension method without errors?");
            }

            if (!methodInvocationInfo.ArgumentRefKindsOpt.IsDefault)
            {
                result.ArgumentRefKindsOpt = [RefKind.None, .. methodInvocationInfo.ArgumentRefKindsOpt];
            }

            if (!methodInvocationInfo.ArgsToParamsOpt.IsDefault)
            {
                var argsToParamsBuilder = ArrayBuilder<int>.GetInstance(methodInvocationInfo.ArgsToParamsOpt.Length + 1);
                argsToParamsBuilder.Add(0);
                for (int i = 0; i < methodInvocationInfo.ArgsToParamsOpt.Length; i++)
                {
                    argsToParamsBuilder.Add(methodInvocationInfo.ArgsToParamsOpt[i] + 1);
                }

                result.ArgsToParamsOpt = argsToParamsBuilder.ToImmutableAndFree();
            }

            return result;
        }

        /// <summary>
        /// Validates whether given invocation can allow its results to escape to <paramref name="escapeTo"/> level.
        /// The result indicates whether the escape is possible. 
        /// Additionally, the method emits diagnostics (possibly more than one, recursively) that would help identify the cause for the failure.
        /// 
        /// NOTE: we need localScopeDepth as some expressions such as optional <c>in</c> parameters or <c>ref dynamic</c> behave as 
        ///       local variables declared at the scope of the invocation.
        /// </summary>
        private bool CheckInvocationEscape(
            SyntaxNode syntax,
            in MethodInvocationInfo methodInvocationInfo,
            bool checkingReceiver,
            SafeContext escapeTo,
            BindingDiagnosticBag diagnostics,
            bool isRefEscape
        )
        {
#if DEBUG
            Debug.Assert(AllParametersConsideredInEscapeAnalysisHaveArguments(in methodInvocationInfo));
#endif

            var localMethodInvocationInfo = ReplaceWithExtensionImplementationIfNeeded(in methodInvocationInfo);

            if (methodInvocationInfo.MethodInfo.UseUpdatedEscapeRules)
            {
                return CheckInvocationEscapeWithUpdatedRules(syntax, in localMethodInvocationInfo, checkingReceiver, escapeTo, diagnostics, isRefEscape, symbolForReporting: methodInvocationInfo.MethodInfo.Symbol);
            }

            return checkInvocationEscapeWithOldRules(syntax, in localMethodInvocationInfo, checkingReceiver, escapeTo, diagnostics, isRefEscape, symbolForReporting: methodInvocationInfo.MethodInfo.Symbol);

            bool checkInvocationEscapeWithOldRules(SyntaxNode syntax, ref readonly MethodInvocationInfo methodInvocationInfo,
                bool checkingReceiver, SafeContext escapeTo,
                BindingDiagnosticBag diagnostics, bool isRefEscape, Symbol symbolForReporting)
            {
                // SPEC: 
                //            In a method invocation, the following constraints apply:
                //•	If there is a ref or out argument to a ref struct type (including the receiver), with safe-to-escape E1, then
                //  o no ref or out argument(excluding the receiver and arguments of ref-like types) may have a narrower ref-safe-to-escape than E1; and
                //  o   no argument(including the receiver) may have a narrower safe-to-escape than E1.

                var receiverlessMethodInvocationInfo = methodInvocationInfo with
                {
                    Receiver = null,
                    ReceiverIsSubjectToCloning = ThreeState.Unknown
                };

                var symbol = methodInvocationInfo.MethodInfo.Symbol;

                var escapeArguments = ArrayBuilder<EscapeArgument>.GetInstance();
                GetInvocationArgumentsForEscape(
                    // receiver handled explicitly below
                    in receiverlessMethodInvocationInfo,
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
                                            CheckRefEscape(argument.Syntax, argument, escapeTo, false, diagnostics) :
                                            CheckValEscape(argument.Syntax, argument, escapeTo, false, diagnostics);

                        if (!valid)
                        {
                            if (symbol is not SignatureOnlyMethodSymbol)
                            {
                                ReportInvocationEscapeError(syntax, methodInvocationInfo.MethodInfo.Symbol, parameter, checkingReceiver, diagnostics);
                            }

                            return false;
                        }
                    }
                }
                finally
                {
                    escapeArguments.Free();
                }

                // ignore receiver when symbol is static
                if (symbol.RequiresInstanceReceiver())
                {
                    // check receiver if ref-like
                    var receiver = methodInvocationInfo.Receiver;
                    if (receiver?.Type?.IsRefLikeOrAllowsRefLikeType() == true)
                    {
                        return CheckValEscape(receiver.Syntax, receiver, escapeTo, false, diagnostics);
                    }
                }

                return true;
            }
        }

        private bool CheckInvocationEscapeWithUpdatedRules(
            SyntaxNode syntax,
            ref readonly MethodInvocationInfo methodInvocationInfo,
            bool checkingReceiver,
            SafeContext escapeTo,
            BindingDiagnosticBag diagnostics,
            bool isRefEscape,
            Symbol symbolForReporting)
        {
            bool result = true;

            var argsAndParamsAll = ArrayBuilder<EscapeValue>.GetInstance();
            GetFilteredInvocationArgumentsForEscapeWithUpdatedRules(
                in methodInvocationInfo,
                isRefEscape,
                ignoreArglistRefKinds: true, // https://github.com/dotnet/roslyn/issues/63325: for compatibility with C#10 implementation.
                argsAndParamsAll);

            var returnsRefToRefStruct = methodInvocationInfo.MethodInfo.ReturnsRefToRefStruct;
            foreach (var (param, argument, _, isArgumentRefEscape) in argsAndParamsAll)
            {
                // SPEC:
                // If `M()` does return ref-to-ref-struct, the *safe-to-escape* is the same as the *safe-to-escape* of all arguments which are ref-to-ref-struct. It is an error if there are multiple arguments with different *safe-to-escape* because of *method arguments must match*.
                // If `M()` does return ref-to-ref-struct, the *ref-safe-to-escape* is the narrowest *ref-safe-to-escape* contributed by all arguments which are ref-to-ref-struct.
                //
                if (!returnsRefToRefStruct
                    || ((param is null ||
                         (param is { RefKind: not RefKind.None, Type: { } type } && type.IsRefLikeOrAllowsRefLikeType())) &&
                        isArgumentRefEscape == isRefEscape))
                {
                    bool valid = isArgumentRefEscape ?
                        CheckRefEscape(argument.Syntax, argument, escapeTo, false, diagnostics) :
                        CheckValEscape(argument.Syntax, argument, escapeTo, false, diagnostics);

                    if (!valid)
                    {
                        // For consistency with C#10 implementation, we don't report an additional error
                        // for the receiver. (In both implementations, the call to Check*Escape() above
                        // will have reported a specific escape error for the receiver though.)
                        if ((object)((argument as BoundCapturedReceiverPlaceholder)?.Receiver ?? argument) != methodInvocationInfo.Receiver && methodInvocationInfo.MethodInfo.Symbol is not SignatureOnlyMethodSymbol)
                        {
                            ReportInvocationEscapeError(syntax, symbolForReporting, param, checkingReceiver, diagnostics);
                        }
                        result = false;
                        break;
                    }
                }
            }
            argsAndParamsAll.Free();

            return result;
        }

        private bool CheckInvocationEscapeToReceiver(
            SyntaxNode syntax,
            in MethodInvocationInfo methodInvocationInfo,
            bool checkingReceiver,
            SafeContext escapeTo,
            BindingDiagnosticBag diagnostics)
        {
            bool result = true;

            var escapeValues = ArrayBuilder<EscapeValue>.GetInstance();
            GetFilteredInvocationArgumentsForEscapeToReceiver(in methodInvocationInfo, escapeValues);

            foreach (var (parameter, argument, _, isArgumentRefEscape) in escapeValues)
            {
                bool valid = isArgumentRefEscape
                    ? CheckRefEscape(argument.Syntax, argument, escapeTo, checkingReceiver: false, diagnostics)
                    : CheckValEscape(argument.Syntax, argument, escapeTo, checkingReceiver: false, diagnostics);

                if (!valid)
                {
                    ReportInvocationEscapeError(syntax, methodInvocationInfo.MethodInfo.Symbol, parameter, checkingReceiver, diagnostics);
                    result = false;
                    break;
                }
            }

            escapeValues.Free();

            return result;
        }

        /// <summary>
        /// Returns the set of arguments to be considered for escape analysis of a method invocation. This
        /// set potentially includes the receiver of the method call. Each argument is returned (only once)
        /// with the corresponding parameter and ref kind.
        /// 
        /// No filtering like removing non-reflike types is done by this method. It is the responsibility of
        /// the caller to determine which arguments impact escape analysis.
        /// </summary>
        private void GetInvocationArgumentsForEscape(
            ref readonly MethodInvocationInfo methodInvocationInfo,
            bool ignoreArglistRefKinds,
            ArrayBuilder<MixableDestination>? mixableArguments,
            ArrayBuilder<EscapeArgument> escapeArguments)
        {
            var receiver = methodInvocationInfo.Receiver;
            if (receiver is { })
            {
                Debug.Assert(receiver.Type is { });
                Debug.Assert(methodInvocationInfo.ReceiverIsSubjectToCloning != ThreeState.Unknown);
                var method = methodInvocationInfo.MethodInfo.Method;
                if (methodInvocationInfo.ReceiverIsSubjectToCloning == ThreeState.True)
                {
                    Debug.Assert(receiver is not BoundValuePlaceholderBase && method is not null && receiver.Type?.IsReferenceType == false);
#if DEBUG
                    AssertVisited(receiver);
#endif
                    // Equivalent to a non-ref local with the underlying receiver as an initializer provided at declaration 
                    receiver = new BoundCapturedReceiverPlaceholder(receiver.Syntax, receiver, _localScopeDepth, receiver.Type).MakeCompilerGenerated();
                }

                var tuple = getReceiver(methodInvocationInfo.MethodInfo, receiver);
                escapeArguments.Add(tuple);

                if (mixableArguments is not null && isMixableParameter(tuple.Parameter))
                {
                    mixableArguments.Add(new MixableDestination(tuple.Parameter, receiver));
                }
            }

            var argsOpt = methodInvocationInfo.ArgsOpt;
            if (!argsOpt.IsDefault)
            {
                var parameters = methodInvocationInfo.Parameters;
                var argsToParamsOpt = methodInvocationInfo.ArgsToParamsOpt;
                var argRefKindsOpt = methodInvocationInfo.ArgumentRefKindsOpt;
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
                parameter.Type.IsRefLikeOrAllowsRefLikeType() &&
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

            static EscapeArgument getReceiver(in MethodInfo methodInfo, BoundExpression receiver)
            {
                // When there is compound usage the receiver is used once but both the get and 
                // set methods are invoked. This will prefer an accessor that has a writable 
                // `this` as it's more dangerous from a ref safety standpoint. 
                if (methodInfo.Method is not null && methodInfo.SetMethod is not null)
                {
                    var getArgument = getReceiverCore(methodInfo.Method, receiver);
                    if (getArgument.RefKind == RefKind.Ref)
                    {
                        return getArgument;
                    }

                    var setArgument = getReceiverCore(methodInfo.SetMethod, receiver);
                    if (setArgument.RefKind == RefKind.Ref)
                    {
                        return setArgument;
                    }

                    Debug.Assert(!getArgument.RefKind.IsWritableReference());
                    return getArgument;
                }

                return getReceiverCore(methodInfo.Method, receiver);
            }

            static EscapeArgument getReceiverCore(MethodSymbol? method, BoundExpression receiver)
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
                    if (receiver.Type is TypeParameterSymbol typeParameter)
                    {
                        // Pretend that the type of the parameter is the type parameter
                        thisParameter = new TypeParameterThisParameterSymbol(thisParameter, typeParameter);
                    }

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
        /// invocation. Each argument is returned with the corresponding parameter and
        /// whether analysis should consider value or ref escape. Not all method arguments
        /// are included, and some arguments may be included twice - once for value, once for ref.
        /// </summary>
        private void GetFilteredInvocationArgumentsForEscapeWithUpdatedRules(
            ref readonly MethodInvocationInfo methodInvocationInfo,
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
            if (!isInvokedWithRef && !hasRefLikeReturn(methodInvocationInfo.MethodInfo.Symbol))
            {
                return;
            }

            GetEscapeValuesForUpdatedRules(
                in methodInvocationInfo,
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

                        return method.ReturnType.IsRefLikeOrAllowsRefLikeType();
                    case PropertySymbol property:
                        return property.Type.IsRefLikeOrAllowsRefLikeType();
                    default:
                        return false;
                }
            }
        }

        private void GetFilteredInvocationArgumentsForEscapeToReceiver(
            ref readonly MethodInvocationInfo methodInvocationInfo,
            ArrayBuilder<EscapeValue> escapeValues)
        {
            var localMethodInvocationInfo = ReplaceWithExtensionImplementationIfNeeded(in methodInvocationInfo);
            var methodInfo = localMethodInvocationInfo.MethodInfo;

            // If the receiver is not a ref to a ref struct, it cannot capture anything.
            ParameterSymbol? extensionReceiver = null;
            if (methodInfo.Symbol.RequiresInstanceReceiver())
            {
                // We have an instance method receiver.
                if (!hasRefToRefStructThis(methodInfo.Method) && !hasRefToRefStructThis(methodInfo.SetMethod))
                {
                    return;
                }
            }
            else
            {
                // We have a classic extension method receiver.
                Debug.Assert(methodInfo.Method?.IsExtensionMethod != false);

                if (localMethodInvocationInfo.Parameters is [var extReceiver, ..])
                {
                    extensionReceiver = extReceiver;
                    if (!isRefToRefStruct(extensionReceiver))
                    {
                        return;
                    }
                }
            }

            var unfilteredEscapeValues = ArrayBuilder<EscapeValue>.GetInstance();
            GetEscapeValues(
                // We do not need the receiver in `escapeValues`.
                localMethodInvocationInfo with { Receiver = null },
                ignoreArglistRefKinds: true,
                mixableArguments: null,
                unfilteredEscapeValues);

            foreach (var (parameter, argument, escapeLevel, isArgumentRefEscape) in unfilteredEscapeValues)
            {
                // Skip if this is the extension method receiver.
                if (extensionReceiver is not null && parameter == extensionReceiver)
                {
                    continue;
                }

                // We did not pass the instance method receiver to GetEscapeValues so we cannot encounter it here.
                Debug.Assert(parameter?.IsThis != true);

                // Skip if the parameter cannot escape from the method to the receiver.
                if (escapeLevel != EscapeLevel.CallingMethod)
                {
                    Debug.Assert(escapeLevel == EscapeLevel.ReturnOnly);
                    continue;
                }

                escapeValues.Add(new EscapeValue(parameter, argument, escapeLevel, isArgumentRefEscape));
            }

            unfilteredEscapeValues.Free();

            static bool hasRefToRefStructThis(MethodSymbol? method)
            {
                return method?.TryGetThisParameter(out var thisParameter) == true && thisParameter is not null &&
                    isRefToRefStruct(thisParameter);
            }

            static bool isRefToRefStruct(ParameterSymbol parameter)
            {
                return parameter.RefKind == RefKind.Ref &&
                    parameter.Type.IsRefLikeOrAllowsRefLikeType();
            }
        }

        /// <summary>
        /// Returns the set of <see cref="EscapeValue"/> to an invocation that impact ref analysis. 
        /// This will filter out everything that could never meaningfully contribute to ref analysis.
        /// </summary>
        private void GetEscapeValues(
            in MethodInvocationInfo methodInvocationInfo,
            bool ignoreArglistRefKinds,
            ArrayBuilder<MixableDestination>? mixableArguments,
            ArrayBuilder<EscapeValue> escapeValues)
        {
            if (methodInvocationInfo.MethodInfo.UseUpdatedEscapeRules)
            {
                GetEscapeValuesForUpdatedRules(
                    in methodInvocationInfo,
                    ignoreArglistRefKinds,
                    mixableArguments,
                    escapeValues);
            }
            else
            {
                GetEscapeValuesForOldRules(
                    in methodInvocationInfo,
                    ignoreArglistRefKinds,
                    mixableArguments,
                    escapeValues);
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
            ref readonly MethodInvocationInfo methodInvocationInfo,
            bool ignoreArglistRefKinds,
            ArrayBuilder<MixableDestination>? mixableArguments,
            ArrayBuilder<EscapeValue> escapeValues)
        {
            var receiverlessInvocationInfo = methodInvocationInfo;
            if (!methodInvocationInfo.MethodInfo.Symbol.RequiresInstanceReceiver())
            {
                // ignore receiver when symbol is static
                receiverlessInvocationInfo = methodInvocationInfo with { Receiver = null, ReceiverIsSubjectToCloning = ThreeState.Unknown };
            }

            var escapeArguments = ArrayBuilder<EscapeArgument>.GetInstance();
            GetInvocationArgumentsForEscape(
                in receiverlessInvocationInfo,
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

                    if (argument.Type?.IsRefLikeOrAllowsRefLikeType() == true)
                    {
                        escapeValues.Add(new EscapeValue(parameter: null, argument, EscapeLevel.CallingMethod, isRefEscape: false));
                    }

                    continue;
                }

                if (parameter.Type.IsRefLikeOrAllowsRefLikeType() && parameter.RefKind != RefKind.Out && GetParameterValEscapeLevel(parameter) is { } valEscapeLevel)
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

        /// <summary>
        /// Returns the set of <see cref="EscapeValue"/> to an invocation that impact ref analysis. 
        /// This will filter out everything that could never meaningfully contribute to ref analysis. For
        /// example: 
        ///   - For ref arguments it will return an <see cref="EscapeValue"/> for both ref and 
        ///     value escape.
        ///   - It will remove value escape for non-ref struct.
        ///   - It will remove ref escape for args which correspond to any refs as old rules couldn't 
        ///     escape refs
        /// Note: this does not consider scoped-ness as it was not present in old rules
        /// </summary>
        private void GetEscapeValuesForOldRules(
            ref readonly MethodInvocationInfo methodInvocationInfo,
            bool ignoreArglistRefKinds,
            ArrayBuilder<MixableDestination>? mixableArguments,
            ArrayBuilder<EscapeValue> escapeValues)
        {
            var adjustedMethodInvocationInfo = methodInvocationInfo;
            if (!methodInvocationInfo.MethodInfo.Symbol.RequiresInstanceReceiver())
            {
                // ignore receiver when symbol is static
                adjustedMethodInvocationInfo = adjustedMethodInvocationInfo with { Receiver = null, ReceiverIsSubjectToCloning = ThreeState.Unknown };
            }

            var escapeArguments = ArrayBuilder<EscapeArgument>.GetInstance();
            GetInvocationArgumentsForEscape(
                in adjustedMethodInvocationInfo,
                ignoreArglistRefKinds,
                mixableArguments,
                escapeArguments);

            foreach (var (parameter, argument, refKind) in escapeArguments)
            {
                // This means it's part of an __arglist or function pointer receiver. 
                if (parameter is null)
                {
                    if (argument.Type?.IsRefLikeOrAllowsRefLikeType() == true)
                    {
                        escapeValues.Add(new EscapeValue(parameter: null, argument, EscapeLevel.CallingMethod, isRefEscape: false));
                    }

                    continue;
                }

                if (parameter.Type.IsRefLikeOrAllowsRefLikeType())
                {
                    escapeValues.Add(new EscapeValue(parameter, argument, EscapeLevel.CallingMethod, isRefEscape: false));
                }

                // https://github.com/dotnet/csharpstandard/blob/0ad29bf615b18ae463d92ef64f557eeb007b76f1/standard/variables.md#9723-parameter-ref-safe-context
                // For a parameter `p`:
                // - If `p` is a reference or input parameter, its ref-safe-context is the caller-context. If `p` is an input parameter, it can’t be returned as a writable `ref` but can be returned as `ref readonly`.
                // - If `p` is an output parameter, its ref-safe-context is the caller-context.
                // - Otherwise, if `p` is the `this` parameter of a struct type, its ref-safe-context is the function-member.
                // - Otherwise, the parameter is a value parameter, and its ref-safe-context is the function-member.
                if (parameter.RefKind != RefKind.None && !parameter.IsThis)
                {
                    escapeValues.Add(new EscapeValue(parameter, argument, EscapeLevel.CallingMethod, isRefEscape: true));
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

        private bool ShouldInferDeclarationExpressionValEscape(BoundExpression argument, [NotNullWhen(true)] out SourceLocalSymbol? localSymbol)
        {
            var symbol = argument switch
            {
                BoundDeconstructValuePlaceholder p => p.VariableSymbol,
                BoundLocal { DeclarationKind: not BoundLocalDeclarationKind.None } l => l.LocalSymbol,
                _ => null
            };
            if (symbol is SourceLocalSymbol local &&
                GetLocalScopes(local).ValEscapeScope.IsCallingMethod)
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
            ref readonly MethodInvocationInfo methodInvocationInfo,
            Symbol symbolForReporting,
            BindingDiagnosticBag diagnostics)
        {
            if (methodInvocationInfo.MethodInfo.UseUpdatedEscapeRules)
            {
                return CheckInvocationArgMixingWithUpdatedRules(syntax, in methodInvocationInfo, diagnostics, symbolForReporting);
            }

            return checkInvocationArgMixingWithOldRules(syntax, in methodInvocationInfo, diagnostics, symbolForReporting);

            bool checkInvocationArgMixingWithOldRules(SyntaxNode syntax, ref readonly MethodInvocationInfo methodInvocationInfo, BindingDiagnosticBag diagnostics, Symbol symbolForReporting)
            {
                // SPEC:
                // In a method invocation, the following constraints apply:
                // - If there is a ref or out argument of a ref struct type (including the receiver), with safe-to-escape E1, then
                // - no argument (including the receiver) may have a narrower safe-to-escape than E1.

                var symbol = methodInvocationInfo.MethodInfo.Symbol;
                var receiverlessInvocationInfo = methodInvocationInfo;
                if (!symbol.RequiresInstanceReceiver())
                {
                    // ignore receiver when symbol is static
                    receiverlessInvocationInfo = methodInvocationInfo with { Receiver = null, ReceiverIsSubjectToCloning = ThreeState.Unknown };
                }

                // widest possible escape via writeable ref-like receiver or ref/out argument.
                SafeContext escapeTo = _localScopeDepth;

                // collect all writeable ref-like arguments, including receiver
                var escapeArguments = ArrayBuilder<EscapeArgument>.GetInstance();
                GetInvocationArgumentsForEscape(
                    in receiverlessInvocationInfo,
                    ignoreArglistRefKinds: false,
                    mixableArguments: null,
                    escapeArguments);

                try
                {
                    foreach (var (_, argument, refKind) in escapeArguments)
                    {
                        if (ShouldInferDeclarationExpressionValEscape(argument, out _))
                        {
                            // Any variable from a declaration expression is a valid mixing destination as we 
                            // infer a legal value escape for it. It does not contribute input as it's declared
                            // at this point (functions like an `out` in the new escape rules)
                            continue;
                        }

                        if (refKind.IsWritableReference()
                            && !argument.IsDiscardExpression()
                            && argument.Type?.IsRefLikeOrAllowsRefLikeType() == true)
                        {
                            escapeTo = escapeTo.Union(GetValEscape(argument));
                        }
                    }

                    var hasMixingError = false;

                    // track the widest scope that arguments could safely escape to.
                    // use this scope as the inferred STE of declaration expressions.
                    var inferredDestinationValEscape = SafeContext.CallingMethod;
                    foreach (var (parameter, argument, _) in escapeArguments)
                    {
                        // in the old rules, we assume that refs cannot escape into ref struct variables.
                        // e.g. in `dest = M(ref arg)`, we assume `ref arg` will not escape into `dest`, but `arg` might.
                        inferredDestinationValEscape = inferredDestinationValEscape.Intersect(GetValEscape(argument));
                        if (!hasMixingError && !CheckValEscape(argument.Syntax, argument, escapeTo, false, diagnostics))
                        {
                            string parameterName = GetInvocationParameterName(parameter);
                            Error(diagnostics, ErrorCode.ERR_CallArgMixing, syntax, symbolForReporting, parameterName);
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
        }

        private bool CheckInvocationArgMixingWithUpdatedRules(
            SyntaxNode syntax,
            ref readonly MethodInvocationInfo methodInvocationInfo,
            BindingDiagnosticBag diagnostics,
            Symbol symbolForReporting)
        {
            var mixableArguments = ArrayBuilder<MixableDestination>.GetInstance();
            var escapeValues = ArrayBuilder<EscapeValue>.GetInstance();
            GetEscapeValuesForUpdatedRules(
                in methodInvocationInfo,
                ignoreArglistRefKinds: false,
                mixableArguments,
                escapeValues);

            var valid = true;
            foreach (var mixableArg in mixableArguments)
            {
                var toArgEscape = GetValEscape(mixableArg.Argument);
                foreach (var (fromParameter, fromArg, escapeKind, isRefEscape) in escapeValues)
                {
                    // This checks to see if the EscapeValue could ever be assigned to this argument based 
                    // on comparing the EscapeLevel of both. If this could never be assigned due to 
                    // this then we don't need to consider it for MAMM analysis.
                    if (!mixableArg.IsAssignableFrom(escapeKind))
                    {
                        continue;
                    }

                    valid = isRefEscape
                        ? CheckRefEscape(fromArg.Syntax, fromArg, toArgEscape, checkingReceiver: false, diagnostics)
                        : CheckValEscape(fromArg.Syntax, fromArg, toArgEscape, checkingReceiver: false, diagnostics);

                    if (!valid)
                    {
                        string parameterName = GetInvocationParameterName(fromParameter);
                        Error(diagnostics, ErrorCode.ERR_CallArgMixing, syntax, symbolForReporting, parameterName);
                        break;
                    }
                }

                if (!valid)
                {
                    break;
                }
            }

            inferDeclarationExpressionValEscape(methodInvocationInfo.ArgsOpt, escapeValues);

            mixableArguments.Free();
            escapeValues.Free();
            return valid;

            void inferDeclarationExpressionValEscape(ImmutableArray<BoundExpression> argsOpt, ArrayBuilder<EscapeValue> escapeValues)
            {
                // find the widest scope that arguments could safely escape to.
                // use this scope as the inferred STE of declaration expressions.
                var inferredDestinationValEscape = SafeContext.CallingMethod;
                foreach (var (_, fromArg, _, isRefEscape) in escapeValues)
                {
                    inferredDestinationValEscape = inferredDestinationValEscape.Intersect(isRefEscape
                        ? GetRefEscape(fromArg)
                        : GetValEscape(fromArg));
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

#if DEBUG
        private static bool AllParametersConsideredInEscapeAnalysisHaveArguments(ref readonly MethodInvocationInfo methodInvocationInfo)
        {
            var parameters = methodInvocationInfo.Parameters;
            var argsOpt = methodInvocationInfo.ArgsOpt;
            var argsToParamsOpt = methodInvocationInfo.ArgsToParamsOpt;
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

        private static ErrorCode GetStandardCallEscapeError(bool checkingReceiver)
        {
            return checkingReceiver ? ErrorCode.ERR_EscapeCall2 : ErrorCode.ERR_EscapeCall;
        }

        private sealed class TypeParameterThisParameterSymbol : ThisParameterSymbolBase
        {
            private readonly TypeParameterSymbol _type;
            private readonly ParameterSymbol _underlyingParameter;

            internal TypeParameterThisParameterSymbol(ParameterSymbol underlyingParameter, TypeParameterSymbol type)
            {
                Debug.Assert(underlyingParameter.IsThis);
                Debug.Assert(underlyingParameter.RefKind != RefKind.Out); // Shouldn't get here for a constructor
                Debug.Assert(underlyingParameter.ContainingSymbol is MethodSymbol);

                _underlyingParameter = underlyingParameter;
                _type = type;
            }

            public override TypeWithAnnotations TypeWithAnnotations
                => TypeWithAnnotations.Create(_type, NullableAnnotation.NotAnnotated);

            public override RefKind RefKind
            {
                get
                {
                    if (_underlyingParameter.RefKind is not RefKind.None and var underlyingRefKind)
                    {
                        return underlyingRefKind;
                    }

                    if (!_underlyingParameter.ContainingType.IsInterface || _type.IsReferenceType)
                    {
                        return RefKind.None;
                    }

                    // Receiver of an interface method could possibly be a structure.
                    // Let's treat it as by ref parameter for the purpose of ref safety analysis.
                    return RefKind.Ref;
                }
            }

            public override ImmutableArray<Location> Locations
            {
                get { return _underlyingParameter.Locations; }
            }

            public override Symbol ContainingSymbol
            {
                get { return _underlyingParameter.ContainingSymbol; }
            }

            internal override ScopedKind DeclaredScope => throw ExceptionUtilities.Unreachable();

            internal override ScopedKind EffectiveScope
            {
                get
                {
                    if (HasUnscopedRefAttribute && UseUpdatedEscapeRules)
                    {
                        return ScopedKind.None;
                    }

                    if (!_underlyingParameter.ContainingType.IsInterface || _type.IsReferenceType)
                    {
                        return ScopedKind.None;
                    }

                    // Receiver of an interface method could possibly be a structure.
                    // Let's treat it as scoped ref by ref parameter for the purpose of ref safety analysis.
                    return ScopedKind.ScopedRef;
                }
            }

            internal override bool HasUnscopedRefAttribute
                => _underlyingParameter.HasUnscopedRefAttribute;

            internal sealed override bool UseUpdatedEscapeRules
                => _underlyingParameter.UseUpdatedEscapeRules;
        }

#nullable disable
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
        private static ErrorCode GetStandardRValueRefEscapeError(SafeContext escapeTo)
        {
            if (escapeTo.IsReturnable)
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
        internal void ValidateEscape(BoundExpression expr, SafeContext escapeTo, bool isByRef, BindingDiagnosticBag diagnostics)
        {
            // The result of escape analysis is affected by the expression's type.
            // We can't do escape analysis on expressions which lack a type, such as 'target typed new()', until they are converted.
            Debug.Assert(expr.Type is not null);

            if (isByRef)
            {
                CheckRefEscape(expr.Syntax, expr, escapeTo, checkingReceiver: false, diagnostics: diagnostics);
            }
            else
            {
                CheckValEscape(expr.Syntax, expr, escapeTo, checkingReceiver: false, diagnostics: diagnostics);
            }
        }

        /// <summary>
        /// Computes the widest scope depth to which the given expression can escape by reference.
        /// 
        /// NOTE: in a case if expression cannot be passed by an alias (RValue and similar), the ref-escape is localScopeDepth
        ///       There are few cases where RValues are permitted to be passed by reference which implies that a temporary local proxy is passed instead.
        ///       We reflect such behavior by constraining the escape value to the narrowest scope possible. 
        /// </summary>
        internal SafeContext GetRefEscape(BoundExpression expr)
        {
#if DEBUG
            AssertVisited(expr);
#endif

            // cannot infer anything from errors
            if (expr.HasAnyErrors)
            {
                return SafeContext.CallingMethod;
            }

            // cannot infer anything from Void (broken code)
            if (expr.Type?.GetSpecialTypeSafe() == SpecialType.System_Void)
            {
                return SafeContext.CallingMethod;
            }

            // constants/literals cannot ref-escape current scope
            if (expr.ConstantValueOpt != null)
            {
                return _localScopeDepth;
            }

            // cover case that cannot refer to local state
            // otherwise default to current scope (RValues, etc)
            switch (expr.Kind)
            {
                case BoundKind.ArrayAccess:
                case BoundKind.PointerIndirectionOperator:
                case BoundKind.PointerElementAccess:
                    // array elements and pointer dereferencing are readwrite variables
                    return SafeContext.CallingMethod;

                case BoundKind.RefValueOperator:
                    // The undocumented __refvalue(tr, T) expression results in an lvalue of type T.
                    // for compat reasons it is not ref-returnable (since TypedReference is not val-returnable)
                    // it can, however, ref-escape to any other level (since TypedReference can val-escape to any other level)
                    return SafeContext.CurrentMethod;

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
                        return GetRefEscape(conditional.Consequence)
                            .Intersect(GetRefEscape(conditional.Alternative));
                    }

                    // otherwise it is an RValue
                    break;

                case BoundKind.FieldAccess:
                    return GetFieldRefEscape((BoundFieldAccess)expr);

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
                        return SafeContext.CallingMethod;
                    }

                    // for other events defer to the receiver.
                    return GetRefEscape(eventAccess.ReceiverOpt);

                case BoundKind.Call:
                    {
                        var call = (BoundCall)expr;

                        if (call.IsErroneousNode)
                        {
                            return SafeContext.CallingMethod;
                        }

                        var methodSymbol = call.Method;
                        if (methodSymbol.RefKind == RefKind.None)
                        {
                            break;
                        }

                        return GetInvocationEscapeScope(
                            MethodInvocationInfo.FromCall(call),
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
                            MethodInvocationInfo.FromFunctionPointerInvocation(ptrInvocation),
                            isRefEscape: true);
                    }

                case BoundKind.IndexerAccess:
                    {
                        var indexerAccess = (BoundIndexerAccess)expr;
                        var indexerSymbol = indexerAccess.Indexer;

                        return GetInvocationEscapeScope(
                            MethodInvocationInfo.FromIndexerAccess(indexerAccess),
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
                                MethodInvocationInfo.FromIndexerAccess(indexerAccess, implicitIndexerAccess.Receiver),
                                isRefEscape: true);

                        case BoundArrayAccess:
                            // array elements are readwrite variables
                            return SafeContext.CallingMethod;

                        case BoundCall call:
                            if (call.IsErroneousNode)
                            {
                                return SafeContext.CallingMethod;
                            }

                            var methodSymbol = call.Method;
                            if (methodSymbol.RefKind == RefKind.None)
                            {
                                break;
                            }

                            return GetInvocationEscapeScope(
                                MethodInvocationInfo.FromCall(call, implicitIndexerAccess.Receiver),
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
                            MethodInvocationInfo.FromInlineArrayAccess(equivalentSignatureMethod, arguments, refKinds, elementAccess.HasAnyErrors),
                            isRefEscape: true);
                    }

                case BoundKind.PropertyAccess:
                    var propertyAccess = (BoundPropertyAccess)expr;

                    // not passing any arguments/parameters
                    return GetInvocationEscapeScope(
                        MethodInvocationInfo.FromProperty(propertyAccess),
                        isRefEscape: true);

                case BoundKind.AssignmentOperator:
                    var assignment = (BoundAssignmentOperator)expr;

                    if (!assignment.IsRef)
                    {
                        // non-ref assignments are RValues
                        break;
                    }

                    // The result of a ref assignment is its right-hand side.
                    return GetRefEscape(assignment.Right);

                case BoundKind.Conversion:
                    Debug.Assert(expr is BoundConversion conversion &&
                        (!conversion.Conversion.IsUserDefined ||
                        conversion.Conversion.Method.HasUnsupportedMetadata ||
                        conversion.Conversion.Method.RefKind == RefKind.None));
                    break;

                case BoundKind.UnaryOperator:
                    Debug.Assert(expr is BoundUnaryOperator unaryOperator &&
                        (unaryOperator.MethodOpt is not { } unaryMethod ||
                        unaryMethod.HasUnsupportedMetadata ||
                        unaryMethod.RefKind == RefKind.None));
                    break;

                case BoundKind.BinaryOperator:
                    Debug.Assert(expr is BoundBinaryOperator binaryOperator &&
                        (binaryOperator.BinaryOperatorMethod is not { } binaryMethod ||
                        binaryMethod.HasUnsupportedMetadata ||
                        binaryMethod.RefKind == RefKind.None));
                    break;

                case BoundKind.UserDefinedConditionalLogicalOperator:
                    Debug.Assert(expr is BoundUserDefinedConditionalLogicalOperator logicalOperator &&
                        (logicalOperator.LogicalOperator.HasUnsupportedMetadata ||
                        logicalOperator.LogicalOperator.RefKind == RefKind.None));
                    break;

                case BoundKind.CompoundAssignmentOperator:
                    Debug.Assert(expr is BoundCompoundAssignmentOperator compoundAssignmentOperator &&
                        (compoundAssignmentOperator.Operator.Method is not { } compoundMethod ||
                        compoundMethod.HasUnsupportedMetadata ||
                        compoundMethod.RefKind == RefKind.None));
                    break;

                case BoundKind.IncrementOperator:
                    Debug.Assert(expr is BoundIncrementOperator incrementOperator &&
                        (incrementOperator.MethodOpt is not { } incrementMethod ||
                        incrementMethod.HasUnsupportedMetadata ||
                        incrementMethod.RefKind == RefKind.None));
                    break;
            }

            // At this point we should have covered all the possible cases for anything that is not a strict RValue.
            return _localScopeDepth;
        }

        /// <summary>
        /// A counterpart to the GetRefEscape, which validates if given escape demand can be met by the expression.
        /// The result indicates whether the escape is possible. 
        /// Additionally, the method emits diagnostics (possibly more than one, recursively) that would help identify the cause for the failure.
        /// </summary>
        internal bool CheckRefEscape(SyntaxNode node, BoundExpression expr, SafeContext escapeTo, bool checkingReceiver, BindingDiagnosticBag diagnostics)
        {
#if DEBUG
            AssertVisited(expr);
#endif

            Debug.Assert(!checkingReceiver || expr.Type.IsValueType || expr.Type.IsTypeParameter());

            if (_localScopeDepth.IsConvertibleTo(escapeTo))
            {
                // 'expr' will never have a *ref-safe-context* narrower than '_localScopeDepth'.
                // So, its *ref-safe-context* is definitely convertible to 'escapeTo'.
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
                    if (escapeTo.IsReturnable)
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
                    if (((BoundCapturedReceiverPlaceholder)expr).LocalScopeDepth.IsConvertibleTo(escapeTo))
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
                        return CheckRefEscape(conditional.Consequence.Syntax, conditional.Consequence, escapeTo, checkingReceiver: false, diagnostics: diagnostics) &&
                               CheckRefEscape(conditional.Alternative.Syntax, conditional.Alternative, escapeTo, checkingReceiver: false, diagnostics: diagnostics);
                    }

                    // report standard lvalue error
                    break;

                case BoundKind.FieldAccess:
                    var fieldAccess = (BoundFieldAccess)expr;
                    return CheckFieldRefEscape(node, fieldAccess, escapeTo, diagnostics);

                case BoundKind.EventAccess:
                    var eventAccess = (BoundEventAccess)expr;
                    if (!eventAccess.IsUsableAsField)
                    {
                        // not field-like events are RValues
                        break;
                    }

                    return CheckFieldLikeEventRefEscape(node, eventAccess, escapeTo, diagnostics);

                case BoundKind.Call:
                    {
                        var call = (BoundCall)expr;

                        if (call.IsErroneousNode)
                        {
                            return true;
                        }

                        var methodSymbol = call.Method;
                        if (methodSymbol.RefKind == RefKind.None)
                        {
                            break;
                        }

                        return CheckInvocationEscape(
                            call.Syntax,
                            MethodInvocationInfo.FromCall(call),
                            checkingReceiver,
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
                            MethodInvocationInfo.FromIndexerAccess(indexerAccess),
                            checkingReceiver,
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
                                MethodInvocationInfo.FromIndexerAccess(indexerAccess, implicitIndexerAccess.Receiver),
                                checkingReceiver,
                                escapeTo,
                                diagnostics,
                                isRefEscape: true);

                        case BoundArrayAccess:
                            // array elements are readwrite variables
                            return true;

                        case BoundCall call:
                            if (call.IsErroneousNode)
                            {
                                return true;
                            }

                            var methodSymbol = call.Method;
                            if (methodSymbol.RefKind == RefKind.None)
                            {
                                break;
                            }

                            return CheckInvocationEscape(
                                call.Syntax,
                                MethodInvocationInfo.FromCall(call, implicitIndexerAccess.Receiver),
                                checkingReceiver,
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
                            MethodInvocationInfo.FromInlineArrayAccess(equivalentSignatureMethod, arguments, refKinds, elementAccess.HasAnyErrors),
                            checkingReceiver,
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
                        MethodInvocationInfo.FromFunctionPointerInvocation(functionPointerInvocation),
                        checkingReceiver,
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
                        MethodInvocationInfo.FromProperty(propertyAccess),
                        checkingReceiver,
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

                    // The result of a ref assignment is its right-hand side.
                    return CheckRefEscape(
                        node,
                        assignment.Right,
                        escapeTo,
                        checkingReceiver: false,
                        diagnostics);

                case BoundKind.Conversion:
                    var conversion = (BoundConversion)expr;
                    if (conversion.Conversion == Conversion.ImplicitThrow)
                    {
                        return CheckRefEscape(node, conversion.Operand, escapeTo, checkingReceiver, diagnostics);
                    }

                    Debug.Assert(!conversion.Conversion.IsUserDefined ||
                        conversion.Conversion.Method.HasUnsupportedMetadata ||
                        conversion.Conversion.Method.RefKind == RefKind.None);
                    break;

                case BoundKind.UnaryOperator:
                    Debug.Assert(expr is BoundUnaryOperator unaryOperator &&
                        (unaryOperator.MethodOpt is not { } unaryMethod ||
                        unaryMethod.HasUnsupportedMetadata ||
                        unaryMethod.RefKind == RefKind.None));
                    break;

                case BoundKind.BinaryOperator:
                    Debug.Assert(expr is BoundBinaryOperator binaryOperator &&
                        (binaryOperator.BinaryOperatorMethod is not { } binaryMethod ||
                        binaryMethod.HasUnsupportedMetadata ||
                        binaryMethod.RefKind == RefKind.None));
                    break;

                case BoundKind.UserDefinedConditionalLogicalOperator:
                    Debug.Assert(expr is BoundUserDefinedConditionalLogicalOperator logicalOperator &&
                        (logicalOperator.LogicalOperator.HasUnsupportedMetadata ||
                        logicalOperator.LogicalOperator.RefKind == RefKind.None));
                    break;

                case BoundKind.CompoundAssignmentOperator:
                    Debug.Assert(expr is BoundCompoundAssignmentOperator compoundAssignmentOperator &&
                        (compoundAssignmentOperator.Operator.Method is not { } compoundMethod ||
                        compoundMethod.HasUnsupportedMetadata ||
                        compoundMethod.RefKind == RefKind.None));
                    break;

                case BoundKind.IncrementOperator:
                    Debug.Assert(expr is BoundIncrementOperator incrementOperator &&
                        (incrementOperator.MethodOpt is not { } incrementMethod ||
                        incrementMethod.HasUnsupportedMetadata ||
                        incrementMethod.RefKind == RefKind.None));
                    break;

                case BoundKind.ThrowExpression:
                    return true;
            }

            // At this point we should have covered all the possible cases for anything that is not a strict RValue.
            Error(diagnostics, GetStandardRValueRefEscapeError(escapeTo), node);
            return false;
        }

        internal SafeContext GetBroadestValEscape(BoundTupleExpression expr)
        {
            SafeContext broadest = _localScopeDepth;
            foreach (var element in expr.Arguments)
            {
                SafeContext valEscape;
                if (element is BoundTupleExpression te)
                {
                    valEscape = GetBroadestValEscape(te);
                }
                else
                {
                    valEscape = GetValEscape(element);
                }

                broadest = broadest.Union(valEscape);
            }

            return broadest;
        }

        /// <summary>
        /// Computes the widest scope depth to which the given expression can escape by value.
        /// 
        /// NOTE: unless the type of expression is ref-like, the result is Binder.ExternalScope since ordinary values can always be returned from methods. 
        /// </summary>
        internal SafeContext GetValEscape(BoundExpression expr)
        {
#if DEBUG
            AssertVisited(expr);
#endif

            // cannot infer anything from errors
            if (expr.HasAnyErrors)
            {
                return SafeContext.CallingMethod;
            }

            // constants/literals cannot refer to local state
            if (expr.ConstantValueOpt != null)
            {
                return SafeContext.CallingMethod;
            }

            // to have local-referring values an expression must have a ref-like type
            if (expr.Type?.IsRefLikeOrAllowsRefLikeType() != true)
            {
                return SafeContext.CallingMethod;
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
                    return SafeContext.CallingMethod;

                case BoundKind.Parameter:
                    return GetParameterValEscape(((BoundParameter)expr).ParameterSymbol);

                case BoundKind.FromEndIndexExpression:
                    // We are going to call a constructor that takes an integer and a bool. Cannot leak any references through them.
                    // always returnable
                    return SafeContext.CallingMethod;

                case BoundKind.TupleLiteral:
                case BoundKind.ConvertedTupleLiteral:
                    var tupleLiteral = (BoundTupleExpression)expr;
                    return GetTupleValEscape(tupleLiteral.Arguments);

                case BoundKind.MakeRefOperator:
                case BoundKind.RefValueOperator:
                    // for compat reasons
                    // NB: it also means can't assign stackalloc spans to a __refvalue
                    //     we are ok with that.
                    return SafeContext.CallingMethod;

                case BoundKind.DiscardExpression:
                    return SafeContext.CallingMethod;

                case BoundKind.DeconstructValuePlaceholder:
                case BoundKind.InterpolatedStringArgumentPlaceholder:
                case BoundKind.AwaitableValuePlaceholder:
                    return GetPlaceholderScope((BoundValuePlaceholderBase)expr);

                case BoundKind.Local:
                    return GetLocalScopes(((BoundLocal)expr).LocalSymbol).ValEscapeScope;

                case BoundKind.CapturedReceiverPlaceholder:
                    // Equivalent to a non-ref local with the underlying receiver as an initializer provided at declaration 
                    var placeholder = (BoundCapturedReceiverPlaceholder)expr;
                    Debug.Assert(placeholder.LocalScopeDepth == _localScopeDepth);
                    return GetValEscape(placeholder.Receiver);

                case BoundKind.StackAllocArrayCreation:
                case BoundKind.ConvertedStackAllocExpression:
                    return SafeContext.CurrentMethod;

                case BoundKind.ConditionalOperator:
                    var conditional = (BoundConditionalOperator)expr;

                    var consEscape = GetValEscape(conditional.Consequence);

                    if (conditional.IsRef)
                    {
                        // ref conditional defers to one operand. 
                        // the other one is the same or we will be reporting errors anyways.
                        return consEscape;
                    }

                    // val conditional gets narrowest of its operands
                    return consEscape.Intersect(GetValEscape(conditional.Alternative));

                case BoundKind.NullCoalescingOperator:
                    var coalescingOp = (BoundNullCoalescingOperator)expr;

                    return GetValEscape(coalescingOp.LeftOperand)
                        .Intersect(GetValEscape(coalescingOp.RightOperand));

                case BoundKind.FieldAccess:
                    var fieldAccess = (BoundFieldAccess)expr;
                    var fieldSymbol = fieldAccess.FieldSymbol;

                    if (fieldSymbol.IsStatic || !fieldSymbol.ContainingType.IsRefLikeType)
                    {
                        // Already an error state.
                        return SafeContext.CallingMethod;
                    }

                    // for ref-like fields defer to the receiver.
                    return GetValEscape(fieldAccess.ReceiverOpt);

                case BoundKind.Call:
                    {
                        var call = (BoundCall)expr;

                        if (call.IsErroneousNode)
                        {
                            return SafeContext.CallingMethod;
                        }

                        return GetInvocationEscapeScope(
                            MethodInvocationInfo.FromCall(call),
                            isRefEscape: false);
                    }

                case BoundKind.FunctionPointerInvocation:
                    var ptrInvocation = (BoundFunctionPointerInvocation)expr;
                    var ptrSymbol = ptrInvocation.FunctionPointer.Signature;

                    return GetInvocationEscapeScope(
                        MethodInvocationInfo.FromFunctionPointerInvocation(ptrInvocation),
                        isRefEscape: false);

                case BoundKind.IndexerAccess:
                    {
                        var indexerAccess = (BoundIndexerAccess)expr;
                        var indexerSymbol = indexerAccess.Indexer;

                        return GetInvocationEscapeScope(
                            MethodInvocationInfo.FromIndexerAccess(indexerAccess),
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
                                MethodInvocationInfo.FromIndexerAccess(indexerAccess, implicitIndexerAccess.Receiver),
                                isRefEscape: false);

                        case BoundArrayAccess:
                            // only possible in error cases (if possible at all)
                            return _localScopeDepth;

                        case BoundCall call:
                            if (call.IsErroneousNode)
                            {
                                return SafeContext.CallingMethod;
                            }

                            return GetInvocationEscapeScope(
                                MethodInvocationInfo.FromCall(call, implicitIndexerAccess.Receiver),
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
                            MethodInvocationInfo.FromInlineArrayAccess(equivalentSignatureMethod, arguments, refKinds, elementAccess.HasAnyErrors),
                            isRefEscape: false);
                    }

                case BoundKind.PropertyAccess:
                    var propertyAccess = (BoundPropertyAccess)expr;

                    // not passing any arguments/parameters
                    return GetInvocationEscapeScope(
                        MethodInvocationInfo.FromProperty(propertyAccess),
                        isRefEscape: false);

                case BoundKind.ObjectCreationExpression:
                    {
                        var objectCreation = (BoundObjectCreationExpression)expr;
                        var constructorSymbol = objectCreation.Constructor;

                        var escape = GetInvocationEscapeScope(
                            MethodInvocationInfo.FromObjectCreation(objectCreation),
                            isRefEscape: false);

                        var initializerOpt = objectCreation.InitializerExpressionOpt;
                        if (initializerOpt != null)
                        {
                            escape = escape.Intersect(GetValEscape(initializerOpt));
                        }

                        return escape;
                    }

                case BoundKind.NewT:
                    {
                        var newT = (BoundNewT)expr;
                        // By default it is safe to escape
                        var escape = SafeContext.CallingMethod;

                        var initializerOpt = newT.InitializerExpressionOpt;
                        if (initializerOpt != null)
                        {
                            escape = escape.Intersect(GetValEscape(initializerOpt));
                        }

                        return escape;
                    }

                case BoundKind.WithExpression:
                    var withExpression = (BoundWithExpression)expr;

                    return GetValEscape(withExpression.Receiver)
                        .Intersect(GetValEscape(withExpression.InitializerExpression));

                case BoundKind.UnaryOperator:
                    var unaryOperator = (BoundUnaryOperator)expr;
                    if (unaryOperator.MethodOpt is { } unaryMethod)
                    {
                        return GetInvocationEscapeScope(
                            MethodInvocationInfo.FromUnaryOperator(unaryOperator),
                            isRefEscape: false);
                    }

                    return GetValEscape(unaryOperator.Operand);

                case BoundKind.Conversion:
                    var conversion = (BoundConversion)expr;
                    Debug.Assert(conversion.ConversionKind != ConversionKind.StackAllocToSpanType, "StackAllocToSpanType unexpected");

                    if (conversion.ConversionKind == ConversionKind.InterpolatedStringHandler)
                    {
                        return GetInterpolatedStringHandlerConversionEscapeScope(conversion.Operand);
                    }

                    if (conversion.ConversionKind == ConversionKind.CollectionExpression)
                    {
                        return HasLocalScope((BoundCollectionExpression)conversion.Operand) ?
                            _localScopeDepth :
                            SafeContext.CallingMethod;
                    }

                    if (conversion.Conversion.IsInlineArray)
                    {
                        ImmutableArray<BoundExpression> arguments;
                        ImmutableArray<RefKind> refKinds;
                        SignatureOnlyMethodSymbol equivalentSignatureMethod = GetInlineArrayConversionEquivalentSignatureMethod(conversion, out arguments, out refKinds);

                        return GetInvocationEscapeScope(
                            MethodInvocationInfo.FromInlineArrayConversion(equivalentSignatureMethod, arguments, refKinds, conversion.HasAnyErrors),
                            isRefEscape: false);
                    }

                    if (conversion.Conversion.IsUserDefined)
                    {
                        var operatorMethod = conversion.Conversion.Method;
                        Debug.Assert(operatorMethod is not null);

                        return GetInvocationEscapeScope(
                            MethodInvocationInfo.FromUserDefinedConversion(operatorMethod, conversion.Operand, conversion.HasAnyErrors),
                            isRefEscape: false);
                    }

                    return GetValEscape(conversion.Operand);

                case BoundKind.AssignmentOperator:
                    // The result of an assignment is its right-hand side.
                    return GetValEscape(((BoundAssignmentOperator)expr).Right);

                case BoundKind.NullCoalescingAssignmentOperator:
                    var nullCoalescingAssignment = (BoundNullCoalescingAssignmentOperator)expr;
                    return GetValEscape(nullCoalescingAssignment.LeftOperand)
                        .Intersect(GetValEscape(nullCoalescingAssignment.RightOperand));

                case BoundKind.IncrementOperator:
                    var increment = (BoundIncrementOperator)expr;
                    if (increment.MethodOpt is { IsStatic: true } incrementMethod)
                    {
                        Debug.Assert(increment.OperatorKind.IsUserDefined());

                        var prefix = increment.OperatorKind.Operator() is UnaryOperatorKind.PrefixIncrement or UnaryOperatorKind.PrefixDecrement;
                        Debug.Assert(prefix || increment.OperatorKind.Operator() is UnaryOperatorKind.PostfixIncrement or UnaryOperatorKind.PostfixDecrement);

                        // Prefix increment can be analyzed like the underlying method call since that's what it returns.
                        // Postfix increment is better analyzed as only the operand since that's what it returns.
                        if (prefix)
                        {
                            return GetInvocationEscapeScope(
                                MethodInvocationInfo.FromIncrementOperator(increment),
                                isRefEscape: false);
                        }
                    }

                    return GetValEscape(increment.Operand);

                case BoundKind.CompoundAssignmentOperator:
                    var compound = (BoundCompoundAssignmentOperator)expr;

                    // https://github.com/dotnet/roslyn/issues/78198 It looks like we don't have a single test demonstrating significance of the code below.

                    if (compound.Operator.Method is { } compoundMethod)
                    {
                        if (compoundMethod.IsStatic)
                        {
                            return GetInvocationEscapeScope(
                                MethodInvocationInfo.FromCompoundAssignmentOperator(compound),
                                isRefEscape: false);
                        }
                        else
                        {
                            return GetValEscape(compound.Left);
                        }
                    }

                    return GetValEscape(compound.Left)
                        .Intersect(GetValEscape(compound.Right));

                case BoundKind.BinaryOperator:
                    var binary = (BoundBinaryOperator)expr;

                    if (binary.BinaryOperatorMethod is { } binaryMethod)
                    {
                        return GetInvocationEscapeScope(
                            MethodInvocationInfo.FromBinaryOperator(binary),
                            isRefEscape: false);
                    }

                    return GetValEscape(binary.Left)
                        .Intersect(GetValEscape(binary.Right));

                case BoundKind.RangeExpression:
                    var range = (BoundRangeExpression)expr;

                    return (range.LeftOperandOpt is { } left ? GetValEscape(left) : SafeContext.CallingMethod)
                        .Intersect(range.RightOperandOpt is { } right ? GetValEscape(right) : SafeContext.CallingMethod);

                case BoundKind.UserDefinedConditionalLogicalOperator:
                    var uo = (BoundUserDefinedConditionalLogicalOperator)expr;

                    return GetInvocationEscapeScope(
                        MethodInvocationInfo.FromUserDefinedConditionalLogicalOperator(uo),
                        isRefEscape: false);

                case BoundKind.QueryClause:
                    return GetValEscape(((BoundQueryClause)expr).Value);

                case BoundKind.RangeVariable:
                    return GetValEscape(((BoundRangeVariable)expr).Value);

                case BoundKind.ObjectInitializerExpression:
                    var initExpr = (BoundObjectInitializerExpression)expr;
                    return GetValEscapeOfObjectInitializer(initExpr);

                case BoundKind.CollectionInitializerExpression:
                    var colExpr = (BoundCollectionInitializerExpression)expr;
                    return GetValEscapeOfCollectionInitializer(colExpr);

                case BoundKind.ObjectInitializerMember:
                    // this node generally makes no sense outside of the context of containing initializer
                    // however binder uses it as a placeholder when binding assignments inside an object initializer
                    // just say it does not escape anywhere, so that we do not get false errors.
                    return _localScopeDepth;

                case BoundKind.ImplicitReceiver:
                case BoundKind.ObjectOrCollectionValuePlaceholder:
                    // binder uses this as a placeholder when binding members inside an object initializer
                    // just say it does not escape anywhere, so that we do not get false errors.
                    return _localScopeDepth;

                case BoundKind.InterpolatedStringHandlerPlaceholder:
                    // The handler placeholder cannot escape out of the current expression, as it's a compiler-synthesized
                    // location.
                    return _localScopeDepth;

                case BoundKind.DisposableValuePlaceholder:
                    // Disposable value placeholder is only ever used to lookup a pattern dispose method
                    // then immediately discarded. The actual expression will be generated during lowering 
                    return _localScopeDepth;

                case BoundKind.PointerElementAccess:
                case BoundKind.PointerIndirectionOperator:
                    // Unsafe code will always be allowed to escape.
                    return SafeContext.CallingMethod;

                case BoundKind.AsOperator:
                case BoundKind.AwaitExpression:
                case BoundKind.ConditionalAccess:
                case BoundKind.ConditionalReceiver:
                case BoundKind.ArrayAccess:
                    // only possible in error cases (if possible at all)
                    return _localScopeDepth;

                case BoundKind.ArgList:
                    // Only possible in error scenarios in runtime async (arglist operators are disallowed in runtime async methods)
                    return _localScopeDepth;

                case BoundKind.ConvertedSwitchExpression:
                case BoundKind.UnconvertedSwitchExpression:
                    var switchExpr = (BoundSwitchExpression)expr;
                    return GetValEscape(switchExpr.SwitchArms.SelectAsArray(a => a.Value));

                default:
                    // in error situations some unexpected nodes could make here
                    // returning "localScopeDepth" seems safer than throwing.
                    // we will still assert to make sure that all nodes are accounted for. 
                    RoslynDebug.Assert(false, $"{expr.Kind} expression of {expr.Type} type");
                    return _localScopeDepth;
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

        private SafeContext GetTupleValEscape(ImmutableArray<BoundExpression> elements)
        {
            SafeContext narrowestScope = _localScopeDepth;
            foreach (var element in elements)
            {
                narrowestScope = narrowestScope.Intersect(GetValEscape(element));
            }

            return narrowestScope;
        }

        private SafeContext GetValEscapeOfCollectionInitializer(BoundCollectionInitializerExpression colExpr)
        {
            var result = SafeContext.CallingMethod;
            foreach (var expr in colExpr.Initializers)
            {
                result = result.Intersect(expr is BoundCollectionElementInitializer colElement
                    ? GetInvocationEscapeToReceiver(
                        MethodInvocationInfo.FromCollectionElementInitializer(colElement))
                    : GetValEscape(expr));
            }
            return result;
        }

        /// <summary>
        /// The escape value of an object initializer is calculated by looking at all of the
        /// expressions that can be stored into the implicit receiver. That means arguments
        /// passed to an indexer for example only matter if they can escape into the receiver
        /// as a stored field.
        /// </summary>
        private SafeContext GetValEscapeOfObjectInitializer(BoundObjectInitializerExpression initExpr)
        {
            var result = SafeContext.CallingMethod;
            foreach (var expr in initExpr.Initializers)
            {
                var exprResult = GetValEscapeOfObjectMemberInitializer(expr);
                result = result.Intersect(exprResult);
            }

            return result;
        }

        private SafeContext GetValEscapeOfObjectMemberInitializer(BoundExpression expr)
        {
            SafeContext result;
            if (expr.Kind == BoundKind.AssignmentOperator)
            {
                var assignment = (BoundAssignmentOperator)expr;
                var rightEscape = assignment.IsRef
                    ? GetRefEscape(assignment.Right)
                    : GetValEscape(assignment.Right);

                if (assignment.Left is BoundObjectInitializerMember left)
                {
                    result = left.MemberSymbol switch
                    {
                        PropertySymbol { IsIndexer: true } indexer => getIndexerEscape(indexer, left, rightEscape),
                        PropertySymbol property => getPropertyEscape(property, rightEscape),
                        _ => rightEscape
                    };
                }
                else
                {
                    result = rightEscape;
                }
            }
            else
            {
                result = GetValEscape(expr);
            }

            return result;

            SafeContext getIndexerEscape(
                PropertySymbol indexer,
                BoundObjectInitializerMember expr,
                SafeContext rightEscapeScope)
            {
                Debug.Assert(expr.AccessorKind != AccessorKind.Unknown);
                var methodInfo = MethodInfo.Create(indexer, expr.AccessorKind);
                if (methodInfo.Method is null)
                {
                    return SafeContext.CallingMethod;
                }

                // If the indexer is readonly then none of the arguments can contribute to 
                // the receiver escape
                if (methodInfo.Method.IsEffectivelyReadOnly)
                {
                    return SafeContext.CallingMethod;
                }

                var escapeValues = ArrayBuilder<EscapeValue>.GetInstance();
                GetEscapeValues(
                    new MethodInvocationInfo
                    {
                        MethodInfo = methodInfo,
                        // This is calculating the actual receiver scope
                        Receiver = null,
                        ReceiverIsSubjectToCloning = ThreeState.Unknown,
                        Parameters = methodInfo.Method.Parameters,
                        ArgsOpt = expr.Arguments,
                        ArgumentRefKindsOpt = expr.ArgumentRefKindsOpt,
                        ArgsToParamsOpt = expr.ArgsToParamsOpt,
                        HasAnyErrors = expr.HasAnyErrors,
                    },
                    ignoreArglistRefKinds: true,
                    mixableArguments: null,
                    escapeValues);

                SafeContext receiverEscapeScope = SafeContext.CallingMethod;
                foreach (var escapeValue in escapeValues)
                {
                    // This is a call to an indexer so the ref escape scope can only impact the escape value if it
                    // can be assigned to `this`. Return Only can't do this.
                    if (escapeValue.IsRefEscape && escapeValue.EscapeLevel != EscapeLevel.CallingMethod)
                    {
                        continue;
                    }

                    SafeContext escapeScope = escapeValue.IsRefEscape
                        ? GetRefEscape(escapeValue.Argument)
                        : GetValEscape(escapeValue.Argument);
                    receiverEscapeScope = escapeScope.Intersect(receiverEscapeScope);
                }

                escapeValues.Free();
                return receiverEscapeScope.Intersect(rightEscapeScope);
            }

            SafeContext getPropertyEscape(
                PropertySymbol property,
                SafeContext rightEscapeScope)
            {
                var accessorKind = property.RefKind == RefKind.None ? AccessorKind.Set : AccessorKind.Get;
                var methodInfo = MethodInfo.Create(property, accessorKind);
                if (methodInfo.Method is null || methodInfo.Method.IsEffectivelyReadOnly)
                {
                    return SafeContext.CallingMethod;
                }

                return rightEscapeScope;
            }
        }

#nullable disable

        private SafeContext GetValEscape(ImmutableArray<BoundExpression> expressions)
        {
            var result = SafeContext.CallingMethod;
            foreach (var expression in expressions)
            {
                result = result.Intersect(GetValEscape(expression));
            }

            return result;
        }

        /// <summary>
        /// A counterpart to the GetValEscape, which validates if given escape demand can be met by the expression.
        /// The result indicates whether the escape is possible.
        /// Additionally, the method emits diagnostics (possibly more than one, recursively) that would help identify the cause for the failure.
        /// </summary>
        internal bool CheckValEscape(SyntaxNode node, BoundExpression expr, SafeContext escapeTo, bool checkingReceiver, BindingDiagnosticBag diagnostics)
        {
#if DEBUG
            AssertVisited(expr);
#endif

            Debug.Assert(!checkingReceiver || expr.Type.IsValueType || expr.Type.IsTypeParameter());

            if (_localScopeDepth.IsConvertibleTo(escapeTo))
            {
                // 'expr' will never have a *safe-context* narrower than '_localScopeDepth'.
                // So, its *safe-context* is definitely convertible to 'escapeTo'.
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
            if (expr.Type?.IsRefLikeOrAllowsRefLikeType() != true)
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
                    return CheckTupleValEscape(tupleLiteral.Arguments, escapeTo, diagnostics);

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
                    if (!GetPlaceholderScope((BoundValuePlaceholderBase)expr).IsConvertibleTo(escapeTo))
                    {
                        Error(diagnostics, inUnsafeRegion ? ErrorCode.WRN_EscapeVariable : ErrorCode.ERR_EscapeVariable, node, expr.Syntax);
                        return inUnsafeRegion;
                    }
                    return true;

                case BoundKind.Local:
                    var localSymbol = ((BoundLocal)expr).LocalSymbol;
                    if (!GetLocalScopes(localSymbol).ValEscapeScope.IsConvertibleTo(escapeTo))
                    {
                        Error(diagnostics, inUnsafeRegion ? ErrorCode.WRN_EscapeVariable : ErrorCode.ERR_EscapeVariable, node, localSymbol);
                        return inUnsafeRegion;
                    }
                    return true;

                case BoundKind.CapturedReceiverPlaceholder:
                    // Equivalent to a non-ref local with the underlying receiver as an initializer provided at declaration 
                    BoundExpression underlyingReceiver = ((BoundCapturedReceiverPlaceholder)expr).Receiver;
                    return CheckValEscape(underlyingReceiver.Syntax, underlyingReceiver, escapeTo, checkingReceiver, diagnostics);

                case BoundKind.StackAllocArrayCreation:
                case BoundKind.ConvertedStackAllocExpression:
                    if (!SafeContext.CurrentMethod.IsConvertibleTo(escapeTo))
                    {
                        Error(diagnostics, inUnsafeRegion ? ErrorCode.WRN_EscapeStackAlloc : ErrorCode.ERR_EscapeStackAlloc, node, expr.Type);
                        return inUnsafeRegion;
                    }
                    return true;

                case BoundKind.UnconvertedConditionalOperator:
                    {
                        var conditional = (BoundUnconvertedConditionalOperator)expr;
                        return
                            CheckValEscape(conditional.Consequence.Syntax, conditional.Consequence, escapeTo, checkingReceiver: false, diagnostics: diagnostics) &&
                            CheckValEscape(conditional.Alternative.Syntax, conditional.Alternative, escapeTo, checkingReceiver: false, diagnostics: diagnostics);
                    }

                case BoundKind.ConditionalOperator:
                    {
                        var conditional = (BoundConditionalOperator)expr;

                        var consValid = CheckValEscape(conditional.Consequence.Syntax, conditional.Consequence, escapeTo, checkingReceiver: false, diagnostics: diagnostics);

                        if (!consValid || conditional.IsRef)
                        {
                            // ref conditional defers to one operand. 
                            // the other one is the same or we will be reporting errors anyways.
                            return consValid;
                        }

                        return CheckValEscape(conditional.Alternative.Syntax, conditional.Alternative, escapeTo, checkingReceiver: false, diagnostics: diagnostics);
                    }

                case BoundKind.NullCoalescingOperator:
                    var coalescingOp = (BoundNullCoalescingOperator)expr;
                    return CheckValEscape(coalescingOp.LeftOperand.Syntax, coalescingOp.LeftOperand, escapeTo, checkingReceiver, diagnostics) &&
                            CheckValEscape(coalescingOp.RightOperand.Syntax, coalescingOp.RightOperand, escapeTo, checkingReceiver, diagnostics);

                case BoundKind.FieldAccess:
                    var fieldAccess = (BoundFieldAccess)expr;
                    var fieldSymbol = fieldAccess.FieldSymbol;

                    if (fieldSymbol.IsStatic || !fieldSymbol.ContainingType.IsRefLikeType)
                    {
                        // Already an error state.
                        return true;
                    }

                    // for ref-like fields defer to the receiver.
                    return CheckValEscape(node, fieldAccess.ReceiverOpt, escapeTo, true, diagnostics);

                case BoundKind.Call:
                    {
                        var call = (BoundCall)expr;
                        if (call.IsErroneousNode)
                        {
                            return true;
                        }

                        var methodSymbol = call.Method;

                        return CheckInvocationEscape(
                            call.Syntax,
                            MethodInvocationInfo.FromCall(call),
                            checkingReceiver,
                            escapeTo,
                            diagnostics,
                            isRefEscape: false);
                    }

                case BoundKind.FunctionPointerInvocation:
                    var ptrInvocation = (BoundFunctionPointerInvocation)expr;
                    var ptrSymbol = ptrInvocation.FunctionPointer.Signature;

                    return CheckInvocationEscape(
                        ptrInvocation.Syntax,
                        MethodInvocationInfo.FromFunctionPointerInvocation(ptrInvocation),
                        checkingReceiver,
                        escapeTo,
                        diagnostics,
                        isRefEscape: false);

                case BoundKind.IndexerAccess:
                    {
                        var indexerAccess = (BoundIndexerAccess)expr;
                        var indexerSymbol = indexerAccess.Indexer;

                        return CheckInvocationEscape(
                            indexerAccess.Syntax,
                            MethodInvocationInfo.FromIndexerAccess(indexerAccess),
                            checkingReceiver,
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
                                MethodInvocationInfo.FromIndexerAccess(indexerAccess, implicitIndexerAccess.Receiver),
                                checkingReceiver,
                                escapeTo,
                                diagnostics,
                                isRefEscape: false);

                        case BoundArrayAccess:
                            // only possible in error cases (if possible at all)
                            return false;

                        case BoundCall call:
                            if (call.IsErroneousNode)
                            {
                                return true;
                            }

                            var methodSymbol = call.Method;

                            return CheckInvocationEscape(
                                call.Syntax,
                                MethodInvocationInfo.FromCall(call, implicitIndexerAccess.Receiver),
                                checkingReceiver,
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
                            MethodInvocationInfo.FromInlineArrayAccess(equivalentSignatureMethod, arguments, refKinds, elementAccess.HasAnyErrors),
                            checkingReceiver,
                            escapeTo,
                            diagnostics,
                            isRefEscape: false);
                    }

                case BoundKind.PropertyAccess:
                    var propertyAccess = (BoundPropertyAccess)expr;

                    // not passing any arguments/parameters
                    return CheckInvocationEscape(
                        propertyAccess.Syntax,
                        MethodInvocationInfo.FromProperty(propertyAccess),
                        checkingReceiver,
                        escapeTo,
                        diagnostics,
                        isRefEscape: false);

                case BoundKind.ObjectCreationExpression:
                    {
                        var objectCreation = (BoundObjectCreationExpression)expr;
                        var constructorSymbol = objectCreation.Constructor;

                        var escape = CheckInvocationEscape(
                            objectCreation.Syntax,
                            MethodInvocationInfo.FromObjectCreation(objectCreation),
                            checkingReceiver,
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
                                    escapeTo,
                                    checkingReceiver: false,
                                    diagnostics: diagnostics);
                        }

                        return escape;
                    }

                case BoundKind.NewT:
                    {
                        var newT = (BoundNewT)expr;
                        var escape = true;

                        var initializerExpr = newT.InitializerExpressionOpt;
                        if (initializerExpr != null)
                        {
                            escape = escape &&
                                CheckValEscape(
                                    initializerExpr.Syntax,
                                    initializerExpr,
                                    escapeTo,
                                    checkingReceiver: false,
                                    diagnostics: diagnostics);
                        }

                        return escape;
                    }

                case BoundKind.WithExpression:
                    {
                        var withExpr = (BoundWithExpression)expr;
                        var escape = CheckValEscape(node, withExpr.Receiver, escapeTo, checkingReceiver: false, diagnostics);

                        var initializerExpr = withExpr.InitializerExpression;
                        escape = escape && CheckValEscape(initializerExpr.Syntax, initializerExpr, escapeTo, checkingReceiver: false, diagnostics: diagnostics);

                        return escape;
                    }

                case BoundKind.UnaryOperator:
                    var unary = (BoundUnaryOperator)expr;
                    if (unary.MethodOpt is { } unaryMethod)
                    {
                        return CheckInvocationEscape(
                            unary.Syntax,
                            MethodInvocationInfo.FromUnaryOperator(unary),
                            checkingReceiver,
                            escapeTo,
                            diagnostics,
                            isRefEscape: false);
                    }

                    return CheckValEscape(node, unary.Operand, escapeTo, checkingReceiver: false, diagnostics: diagnostics);

                case BoundKind.FromEndIndexExpression:
                    // We are going to call a constructor that takes an integer and a bool. Cannot leak any references through them.
                    return true;

                case BoundKind.Conversion:
                    var conversion = (BoundConversion)expr;
                    Debug.Assert(conversion.ConversionKind != ConversionKind.StackAllocToSpanType, "StackAllocToSpanType unexpected");

                    if (conversion.ConversionKind == ConversionKind.InterpolatedStringHandler)
                    {
                        return CheckInterpolatedStringHandlerConversionEscape(conversion.Operand, escapeTo, diagnostics);
                    }

                    if (conversion.ConversionKind == ConversionKind.CollectionExpression)
                    {
                        if (HasLocalScope((BoundCollectionExpression)conversion.Operand) && !_localScopeDepth.IsConvertibleTo(escapeTo))
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
                            MethodInvocationInfo.FromInlineArrayConversion(equivalentSignatureMethod, arguments, refKinds, conversion.HasAnyErrors),
                            checkingReceiver,
                            escapeTo,
                            diagnostics,
                            isRefEscape: false);
                    }

                    if (conversion.Conversion.IsUserDefined)
                    {
                        var operatorMethod = conversion.Conversion.Method;
                        Debug.Assert(operatorMethod is not null);

                        return CheckInvocationEscape(
                            conversion.Syntax,
                            MethodInvocationInfo.FromUserDefinedConversion(operatorMethod, conversion.Operand, conversion.HasAnyErrors),
                            checkingReceiver,
                            escapeTo,
                            diagnostics,
                            isRefEscape: false);
                    }

                    return CheckValEscape(node, conversion.Operand, escapeTo, checkingReceiver: false, diagnostics: diagnostics);

                case BoundKind.AssignmentOperator:
                    var assignment = (BoundAssignmentOperator)expr;
                    // The result of an assignment is its right-hand side.
                    return CheckValEscape(node, assignment.Right, escapeTo, checkingReceiver: false, diagnostics: diagnostics);

                case BoundKind.NullCoalescingAssignmentOperator:
                    var nullCoalescingAssignment = (BoundNullCoalescingAssignmentOperator)expr;
                    return CheckValEscape(node, nullCoalescingAssignment.LeftOperand, escapeTo, checkingReceiver: false, diagnostics: diagnostics) &&
                        CheckValEscape(node, nullCoalescingAssignment.RightOperand, escapeTo, checkingReceiver: false, diagnostics: diagnostics);

                case BoundKind.IncrementOperator:
                    var increment = (BoundIncrementOperator)expr;
                    if (increment.MethodOpt is { IsStatic: true } incrementMethod)
                    {
                        Debug.Assert(increment.OperatorKind.IsUserDefined());

                        var prefix = increment.OperatorKind.Operator() is UnaryOperatorKind.PrefixIncrement or UnaryOperatorKind.PrefixDecrement;
                        Debug.Assert(prefix || increment.OperatorKind.Operator() is UnaryOperatorKind.PostfixIncrement or UnaryOperatorKind.PostfixDecrement);

                        // Prefix increment can be analyzed like the underlying method call since that's what it returns.
                        // Postfix increment is better analyzed as only the operand since that's what it returns.
                        if (prefix)
                        {
                            return CheckInvocationEscape(
                                increment.Syntax,
                                MethodInvocationInfo.FromIncrementOperator(increment),
                                checkingReceiver,
                                escapeTo,
                                diagnostics,
                                isRefEscape: false);
                        }
                    }

                    return CheckValEscape(node, increment.Operand, escapeTo, checkingReceiver: false, diagnostics: diagnostics);

                case BoundKind.CompoundAssignmentOperator:
                    var compound = (BoundCompoundAssignmentOperator)expr;

                    if (compound.Operator.Method is { } compoundMethod)
                    {
                        if (compoundMethod.IsStatic)
                        {
                            return CheckInvocationEscape(
                                compound.Syntax,
                                MethodInvocationInfo.FromCompoundAssignmentOperator(compound),
                                checkingReceiver,
                                escapeTo,
                                diagnostics,
                                isRefEscape: false);
                        }
                        else
                        {
                            return CheckValEscape(compound.Left.Syntax, compound.Left, escapeTo, checkingReceiver: false, diagnostics: diagnostics);
                        }
                    }

                    return CheckValEscape(compound.Left.Syntax, compound.Left, escapeTo, checkingReceiver: false, diagnostics: diagnostics) &&
                           CheckValEscape(compound.Right.Syntax, compound.Right, escapeTo, checkingReceiver: false, diagnostics: diagnostics);

                case BoundKind.BinaryOperator:
                    var binary = (BoundBinaryOperator)expr;

                    if (binary.OperatorKind == BinaryOperatorKind.Utf8Addition)
                    {
                        return true;
                    }

                    if (binary.BinaryOperatorMethod is { } binaryMethod)
                    {
                        return CheckInvocationEscape(
                            binary.Syntax,
                            MethodInvocationInfo.FromBinaryOperator(binary),
                            checkingReceiver,
                            escapeTo,
                            diagnostics,
                            isRefEscape: false);
                    }

                    return CheckValEscape(binary.Left.Syntax, binary.Left, escapeTo, checkingReceiver: false, diagnostics: diagnostics) &&
                           CheckValEscape(binary.Right.Syntax, binary.Right, escapeTo, checkingReceiver: false, diagnostics: diagnostics);

                case BoundKind.RangeExpression:
                    var range = (BoundRangeExpression)expr;

                    if (range.LeftOperandOpt is { } left && !CheckValEscape(left.Syntax, left, escapeTo, checkingReceiver: false, diagnostics: diagnostics))
                    {
                        return false;
                    }

                    return !(range.RightOperandOpt is { } right && !CheckValEscape(right.Syntax, right, escapeTo, checkingReceiver: false, diagnostics: diagnostics));

                case BoundKind.UserDefinedConditionalLogicalOperator:
                    var uo = (BoundUserDefinedConditionalLogicalOperator)expr;

                    return CheckInvocationEscape(
                        uo.Syntax,
                        MethodInvocationInfo.FromUserDefinedConditionalLogicalOperator(uo),
                        checkingReceiver,
                        escapeTo,
                        diagnostics,
                        isRefEscape: false);

                case BoundKind.QueryClause:
                    var clauseValue = ((BoundQueryClause)expr).Value;
                    return CheckValEscape(clauseValue.Syntax, clauseValue, escapeTo, checkingReceiver: false, diagnostics: diagnostics);

                case BoundKind.RangeVariable:
                    var variableValue = ((BoundRangeVariable)expr).Value;
                    return CheckValEscape(variableValue.Syntax, variableValue, escapeTo, checkingReceiver: false, diagnostics: diagnostics);

                case BoundKind.ObjectInitializerExpression:
                    var initExpr = (BoundObjectInitializerExpression)expr;
                    return CheckValEscapeOfObjectInitializer(initExpr, escapeTo, diagnostics);

                case BoundKind.CollectionInitializerExpression:
                    var colExpr = (BoundCollectionInitializerExpression)expr;
                    return CheckValEscapeOfCollectionInitializer(colExpr, escapeTo, diagnostics);

                case BoundKind.PointerElementAccess:
                    var accessedExpression = ((BoundPointerElementAccess)expr).Expression;
                    return CheckValEscape(accessedExpression.Syntax, accessedExpression, escapeTo, checkingReceiver, diagnostics);

                case BoundKind.PointerIndirectionOperator:
                    var operandExpression = ((BoundPointerIndirectionOperator)expr).Operand;
                    return CheckValEscape(operandExpression.Syntax, operandExpression, escapeTo, checkingReceiver, diagnostics);

                case BoundKind.AsOperator:
                case BoundKind.AwaitExpression:
                case BoundKind.ConditionalAccess:
                case BoundKind.ConditionalReceiver:
                case BoundKind.ArrayAccess:
                    // only possible in error cases (if possible at all)
                    return false;

                case BoundKind.UnconvertedSwitchExpression:
                case BoundKind.ConvertedSwitchExpression:
                    foreach (var arm in ((BoundSwitchExpression)expr).SwitchArms)
                    {
                        var result = arm.Value;
                        if (!CheckValEscape(result.Syntax, result, escapeTo, checkingReceiver: false, diagnostics: diagnostics))
                            return false;
                    }

                    return true;

                default:
                    // in error situations some unexpected nodes could make here
                    // returning "false" seems safer than throwing.
                    // we will still assert to make sure that all nodes are accounted for.
                    RoslynDebug.Assert(false, $"{expr.Kind} expression of {expr.Type} type");
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
                                                            isParamsArray: false,
                                                            isParamsCollection: false,
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
                                                            isParamsArray: false,
                                                            isParamsCollection: false,
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

        private bool CheckTupleValEscape(ImmutableArray<BoundExpression> elements, SafeContext escapeTo, BindingDiagnosticBag diagnostics)
        {
            foreach (var element in elements)
            {
                if (!CheckValEscape(element.Syntax, element, escapeTo, checkingReceiver: false, diagnostics: diagnostics))
                {
                    return false;
                }
            }

            return true;
        }

#nullable enable

        private bool CheckValEscapeOfCollectionInitializer(BoundCollectionInitializerExpression colExpr, SafeContext escapeTo, BindingDiagnosticBag diagnostics)
        {
            foreach (var expr in colExpr.Initializers)
            {
                if (expr is BoundCollectionElementInitializer colElement)
                {
                    if (!CheckInvocationEscapeToReceiver(
                            colElement.Syntax,
                            MethodInvocationInfo.FromCollectionElementInitializer(colElement),
                            checkingReceiver: false,
                            escapeTo,
                            diagnostics))
                    {
                        return false;
                    }
                }
                else if (!CheckValEscape(expr.Syntax, expr, escapeTo, checkingReceiver: false, diagnostics))
                {
                    return false;
                }
            }

            return true;
        }

        private bool CheckValEscapeOfObjectInitializer(BoundObjectInitializerExpression initExpr, SafeContext escapeTo, BindingDiagnosticBag diagnostics)
        {
            foreach (var expr in initExpr.Initializers)
            {
                if (!GetValEscapeOfObjectMemberInitializer(expr).IsConvertibleTo(escapeTo))
                {
                    Error(diagnostics, _inUnsafeRegion ? ErrorCode.WRN_EscapeVariable : ErrorCode.ERR_EscapeVariable, initExpr.Syntax, expr.Syntax);
                    return false;
                }
            }

            return true;
        }

#nullable disable

        private bool CheckInterpolatedStringHandlerConversionEscape(BoundExpression expression, SafeContext escapeTo, BindingDiagnosticBag diagnostics)
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
            bool result = CheckValEscape(expression.Syntax, data.Construction, escapeTo, checkingReceiver: false, diagnostics);
#if DEBUG
            _visited = previousVisited;
#endif

            // Narrow the scope for implicit calls which allow the receiver to capture refs from the arguments.
            result = result && CheckValEscapeOfInterpolatedStringHandlerCalls(expression, escapeTo, diagnostics);

            return result;
        }

        private SafeContext GetValEscapeOfInterpolatedStringHandlerCalls(BoundExpression expression)
        {
            SafeContext scope = SafeContext.CallingMethod;

            while (true)
            {
                switch (expression)
                {
                    case BoundBinaryOperator binary:
                        scope = scope.Intersect(GetValEscapeOfInterpolatedStringHandlerCalls(binary.Right));
                        expression = binary.Left;
                        break;

                    case BoundInterpolatedString interpolatedString:
                        return scope.Intersect(getPartsScope(interpolatedString));

                    default:
                        throw ExceptionUtilities.UnexpectedValue(expression.Kind);
                }
            }

            SafeContext getPartsScope(BoundInterpolatedString interpolatedString)
            {
                SafeContext scope = SafeContext.CallingMethod;

                foreach (var part in interpolatedString.Parts)
                {
                    if (part is not BoundCall { IsErroneousNode: false } call)
                    {
                        // Dynamic calls cannot have ref struct parameters.
                        continue;
                    }

                    scope = scope.Intersect(GetInvocationEscapeToReceiver(
                        MethodInvocationInfo.FromCall(call)));
                }

                return scope;
            }
        }

        private bool CheckValEscapeOfInterpolatedStringHandlerCalls(BoundExpression expression, SafeContext escapeTo, BindingDiagnosticBag diagnostics)
        {
            while (true)
            {
                switch (expression)
                {
                    case BoundBinaryOperator binary:
                        if (!CheckValEscapeOfInterpolatedStringHandlerCalls(binary.Right, escapeTo, diagnostics))
                        {
                            return false;
                        }

                        expression = binary.Left;
                        break;

                    case BoundInterpolatedString interpolatedString:
                        return checkParts(interpolatedString, escapeTo, diagnostics);

                    default:
                        throw ExceptionUtilities.UnexpectedValue(expression.Kind);
                }
            }

            bool checkParts(BoundInterpolatedString interpolatedString, SafeContext escapeTo, BindingDiagnosticBag diagnostics)
            {
                foreach (var part in interpolatedString.Parts)
                {
                    if (part is not BoundCall { IsErroneousNode: false } call)
                    {
                        // Dynamic calls cannot have ref struct parameters.
                        continue;
                    }

                    if (!CheckInvocationEscapeToReceiver(
                            call.Syntax,
                            MethodInvocationInfo.FromCall(call),
                            checkingReceiver: false,
                            escapeTo,
                            diagnostics))
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}
