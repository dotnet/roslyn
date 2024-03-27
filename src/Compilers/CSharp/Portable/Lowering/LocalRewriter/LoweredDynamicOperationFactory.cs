// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class LoweredDynamicOperationFactory
    {
        private readonly SyntheticBoundNodeFactory _factory;
        private readonly int _methodOrdinal;
        private readonly int _localFunctionOrdinal;
        private NamedTypeSymbol? _currentDynamicCallSiteContainer;
        private int _callSiteIdDispenser;

        internal LoweredDynamicOperationFactory(SyntheticBoundNodeFactory factory, int methodOrdinal, int localFunctionOrdinal = -1)
        {
            Debug.Assert(factory != null);
            _factory = factory;
            _methodOrdinal = methodOrdinal;
            _localFunctionOrdinal = localFunctionOrdinal;
        }

        public int MethodOrdinal => _methodOrdinal;

        // We could read the values of the following enums from metadata instead of hardcoding them here but 
        // - they can never change since existing programs have the values inlined and would be broken if the values changed their meaning,
        // - if any new flags are added to the runtime binder the compiler will change as well to produce them.

        // The only scenario that is not supported by hardcoding the values is when a completely new Framework is created 
        // that redefines these constants and is not supposed to run existing programs.

        /// <summary>
        /// Corresponds to Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags.
        /// </summary>
        [Flags]
        private enum CSharpBinderFlags
        {
            None = 0,
            CheckedContext = 1,
            InvokeSimpleName = 2,
            InvokeSpecialName = 4,
            BinaryOperationLogical = 8,
            ConvertExplicit = 16,
            ConvertArrayIndex = 32,
            ResultIndexed = 64,
            ValueFromCompoundAssignment = 128,
            ResultDiscarded = 256,
        }

        /// <summary>
        /// Corresponds to Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags.
        /// </summary>
        [Flags]
        private enum CSharpArgumentInfoFlags
        {
            None = 0,
            UseCompileTimeType = 1,
            Constant = 2,
            NamedArgument = 4,
            IsRef = 8,
            IsOut = 16,
            IsStaticType = 32,
        }

        internal LoweredDynamicOperation MakeDynamicConversion(
            BoundExpression loweredOperand,
            bool isExplicit,
            bool isArrayIndex,
            bool isChecked,
            TypeSymbol resultType)
        {
            _factory.Syntax = loweredOperand.Syntax;

            CSharpBinderFlags binderFlags = 0;
            Debug.Assert(!isExplicit || !isArrayIndex);

            if (isChecked)
            {
                binderFlags |= CSharpBinderFlags.CheckedContext;
            }
            if (isExplicit)
            {
                binderFlags |= CSharpBinderFlags.ConvertExplicit;
            }
            if (isArrayIndex)
            {
                binderFlags |= CSharpBinderFlags.ConvertArrayIndex;
            }

            var loweredArguments = ImmutableArray.Create(loweredOperand);

            var binderConstruction = MakeBinderConstruction(WellKnownMember.Microsoft_CSharp_RuntimeBinder_Binder__Convert, new[]
            {
                // flags:
                _factory.Literal((int)binderFlags),

                // target type:
                _factory.Typeof(resultType, _factory.WellKnownType(WellKnownType.System_Type)),

                // context:
                _factory.TypeofDynamicOperationContextType()
            });

            return MakeDynamicOperation(binderConstruction, null, RefKind.None, loweredArguments, default(ImmutableArray<RefKind>), null, resultType);
        }

        internal LoweredDynamicOperation MakeDynamicUnaryOperator(
            UnaryOperatorKind operatorKind,
            BoundExpression loweredOperand,
            TypeSymbol resultType)
        {
            Debug.Assert(operatorKind.IsDynamic());

            _factory.Syntax = loweredOperand.Syntax;

            CSharpBinderFlags binderFlags = 0;
            if (operatorKind.IsChecked())
            {
                binderFlags |= CSharpBinderFlags.CheckedContext;
            }

            var loweredArguments = ImmutableArray.Create(loweredOperand);

            MethodSymbol argumentInfoFactory = GetArgumentInfoFactory();
            var binderConstruction = ((object)argumentInfoFactory != null) ? MakeBinderConstruction(WellKnownMember.Microsoft_CSharp_RuntimeBinder_Binder__UnaryOperation, new[]
            {
                // flags:
                _factory.Literal((int)binderFlags),

                // expression type:
                _factory.Literal((int)operatorKind.ToExpressionType()),

                // context:
                _factory.TypeofDynamicOperationContextType(),

                // argument infos:
                MakeCallSiteArgumentInfos(argumentInfoFactory, loweredArguments)
            }) : null;

            return MakeDynamicOperation(binderConstruction, null, RefKind.None, loweredArguments, default(ImmutableArray<RefKind>), null, resultType);
        }

        internal LoweredDynamicOperation MakeDynamicBinaryOperator(
            BinaryOperatorKind operatorKind,
            BoundExpression loweredLeft,
            BoundExpression loweredRight,
            bool isCompoundAssignment,
            TypeSymbol resultType)
        {
            Debug.Assert(operatorKind.IsDynamic());

            _factory.Syntax = loweredLeft.Syntax;

            CSharpBinderFlags binderFlags = 0;
            if (operatorKind.IsChecked())
            {
                binderFlags |= CSharpBinderFlags.CheckedContext;
            }

            if (operatorKind.IsLogical())
            {
                binderFlags |= CSharpBinderFlags.BinaryOperationLogical;
            }

            var loweredArguments = ImmutableArray.Create<BoundExpression>(loweredLeft, loweredRight);

            MethodSymbol argumentInfoFactory = GetArgumentInfoFactory();
            var binderConstruction = ((object)argumentInfoFactory != null) ? MakeBinderConstruction(WellKnownMember.Microsoft_CSharp_RuntimeBinder_Binder__BinaryOperation, new[]
            {
                // flags:
                _factory.Literal((int)binderFlags),

                // expression type:
                _factory.Literal((int)operatorKind.ToExpressionType(isCompoundAssignment)),

                // context:
                _factory.TypeofDynamicOperationContextType(),

                // argument infos:
                MakeCallSiteArgumentInfos(argumentInfoFactory, loweredArguments)
            }) : null;

            return MakeDynamicOperation(binderConstruction, null, RefKind.None, loweredArguments, default(ImmutableArray<RefKind>), null, resultType);
        }

        internal LoweredDynamicOperation MakeDynamicMemberInvocation(
            string name,
            BoundExpression loweredReceiver,
            ImmutableArray<TypeWithAnnotations> typeArgumentsWithAnnotations,
            ImmutableArray<BoundExpression> loweredArguments,
            ImmutableArray<string?> argumentNames,
            ImmutableArray<RefKind> refKinds,
            bool hasImplicitReceiver,
            bool resultDiscarded)
        {
            _factory.Syntax = loweredReceiver.Syntax;
            Debug.Assert(_factory.TopLevelMethod is { });

            CSharpBinderFlags binderFlags = 0;
            if (hasImplicitReceiver && _factory.TopLevelMethod.RequiresInstanceReceiver)
            {
                binderFlags |= CSharpBinderFlags.InvokeSimpleName;
            }

            TypeSymbol resultType;
            if (resultDiscarded)
            {
                binderFlags |= CSharpBinderFlags.ResultDiscarded;
                resultType = _factory.SpecialType(SpecialType.System_Void);
            }
            else
            {
                resultType = AssemblySymbol.DynamicType;
            }

            RefKind receiverRefKind;
            bool receiverIsStaticType;
            if (loweredReceiver.Kind == BoundKind.TypeExpression)
            {
                loweredReceiver = _factory.Typeof(((BoundTypeExpression)loweredReceiver).Type, _factory.WellKnownType(WellKnownType.System_Type));
                receiverRefKind = RefKind.None;
                receiverIsStaticType = true;
            }
            else
            {
                receiverRefKind = GetReceiverRefKind(loweredReceiver);
                receiverIsStaticType = false;
            }

            MethodSymbol argumentInfoFactory = GetArgumentInfoFactory();
            var binderConstruction = ((object)argumentInfoFactory != null) ? MakeBinderConstruction(WellKnownMember.Microsoft_CSharp_RuntimeBinder_Binder__InvokeMember, new[]
            {
                // flags:
                _factory.Literal((int)binderFlags),

                // member name:
                _factory.Literal(name),

                // type arguments:
                typeArgumentsWithAnnotations.IsDefaultOrEmpty ?
                    _factory.Null(_factory.WellKnownArrayType(WellKnownType.System_Type)) :
                    _factory.ArrayOrEmpty(_factory.WellKnownType(WellKnownType.System_Type), _factory.TypeOfs(typeArgumentsWithAnnotations, _factory.WellKnownType(WellKnownType.System_Type))),

                // context:
                _factory.TypeofDynamicOperationContextType(),

                // argument infos:
                MakeCallSiteArgumentInfos(argumentInfoFactory, loweredArguments, argumentNames, refKinds, loweredReceiver, receiverRefKind, receiverIsStaticType)
            }) : null;

            return MakeDynamicOperation(binderConstruction, loweredReceiver, receiverRefKind, loweredArguments, refKinds, null, resultType);
        }

        internal LoweredDynamicOperation MakeDynamicEventAccessorInvocation(
            string accessorName,
            BoundExpression loweredReceiver,
            BoundExpression loweredHandler)
        {
            _factory.Syntax = loweredReceiver.Syntax;

            CSharpBinderFlags binderFlags = CSharpBinderFlags.InvokeSpecialName | CSharpBinderFlags.ResultDiscarded;

            var loweredArguments = ImmutableArray<BoundExpression>.Empty;
            var resultType = AssemblySymbol.DynamicType;

            MethodSymbol argumentInfoFactory = GetArgumentInfoFactory();
            var binderConstruction = ((object)argumentInfoFactory != null) ? MakeBinderConstruction(WellKnownMember.Microsoft_CSharp_RuntimeBinder_Binder__InvokeMember, new[]
            {
                // flags:
                _factory.Literal((int)binderFlags),

                // member name:
                _factory.Literal(accessorName),

                // type arguments:
                _factory.Null(_factory.WellKnownArrayType(WellKnownType.System_Type)),

                // context:
                _factory.TypeofDynamicOperationContextType(),

                // argument infos:
                MakeCallSiteArgumentInfos(argumentInfoFactory, loweredArguments, loweredReceiver: loweredReceiver, loweredRight: loweredHandler)
            }) : null;

            return MakeDynamicOperation(binderConstruction, loweredReceiver, RefKind.None, loweredArguments, default(ImmutableArray<RefKind>), loweredHandler, resultType);
        }

        internal LoweredDynamicOperation MakeDynamicInvocation(
            BoundExpression loweredReceiver,
            ImmutableArray<BoundExpression> loweredArguments,
            ImmutableArray<string?> argumentNames,
            ImmutableArray<RefKind> refKinds,
            bool resultDiscarded)
        {
            _factory.Syntax = loweredReceiver.Syntax;

            TypeSymbol resultType;
            CSharpBinderFlags binderFlags = 0;
            if (resultDiscarded)
            {
                binderFlags |= CSharpBinderFlags.ResultDiscarded;
                resultType = _factory.SpecialType(SpecialType.System_Void);
            }
            else
            {
                resultType = AssemblySymbol.DynamicType;
            }

            MethodSymbol argumentInfoFactory = GetArgumentInfoFactory();
            var binderConstruction = ((object)argumentInfoFactory != null) ? MakeBinderConstruction(WellKnownMember.Microsoft_CSharp_RuntimeBinder_Binder__Invoke, new[]
            {
                // flags:
                _factory.Literal((int)binderFlags),

                // context:
                _factory.TypeofDynamicOperationContextType(),

                // argument infos:
                MakeCallSiteArgumentInfos(argumentInfoFactory, loweredArguments, argumentNames, refKinds, loweredReceiver)
            }) : null;

            return MakeDynamicOperation(binderConstruction, loweredReceiver, RefKind.None, loweredArguments, refKinds, null, resultType);
        }

        internal LoweredDynamicOperation MakeDynamicConstructorInvocation(
            SyntaxNode syntax,
            TypeSymbol type,
            ImmutableArray<BoundExpression> loweredArguments,
            ImmutableArray<string?> argumentNames,
            ImmutableArray<RefKind> refKinds)
        {
            _factory.Syntax = syntax;

            var loweredReceiver = _factory.Typeof(type, _factory.WellKnownType(WellKnownType.System_Type));

            MethodSymbol argumentInfoFactory = GetArgumentInfoFactory();
            var binderConstruction = ((object)argumentInfoFactory != null) ? MakeBinderConstruction(WellKnownMember.Microsoft_CSharp_RuntimeBinder_Binder__InvokeConstructor, new[]
            {
                // flags:
                _factory.Literal(0),

                // context:
                _factory.TypeofDynamicOperationContextType(),

                // argument infos:
                MakeCallSiteArgumentInfos(argumentInfoFactory, loweredArguments, argumentNames, refKinds, loweredReceiver, receiverIsStaticType: true)
            }) : null;

            return MakeDynamicOperation(binderConstruction, loweredReceiver, RefKind.None, loweredArguments, refKinds, null, type);
        }

        internal LoweredDynamicOperation MakeDynamicGetMember(
            BoundExpression loweredReceiver,
            string name,
            bool resultIndexed)
        {
            _factory.Syntax = loweredReceiver.Syntax;

            CSharpBinderFlags binderFlags = 0;
            if (resultIndexed)
            {
                binderFlags |= CSharpBinderFlags.ResultIndexed;
            }

            var loweredArguments = ImmutableArray<BoundExpression>.Empty;
            var resultType = DynamicTypeSymbol.Instance;

            MethodSymbol argumentInfoFactory = GetArgumentInfoFactory();
            var binderConstruction = ((object)argumentInfoFactory != null) ? MakeBinderConstruction(WellKnownMember.Microsoft_CSharp_RuntimeBinder_Binder__GetMember, new[]
            {
                // flags:
                _factory.Literal((int)binderFlags),

                // name:
                _factory.Literal(name),

                // context:
                _factory.TypeofDynamicOperationContextType(),

                // argument infos:
                MakeCallSiteArgumentInfos(argumentInfoFactory, loweredArguments, loweredReceiver: loweredReceiver)
            }) : null;

            return MakeDynamicOperation(binderConstruction, loweredReceiver, RefKind.None, loweredArguments, default(ImmutableArray<RefKind>), null, resultType);
        }

        internal LoweredDynamicOperation MakeDynamicSetMember(
            BoundExpression loweredReceiver,
            string name,
            BoundExpression loweredRight,
            bool isCompoundAssignment = false,
            bool isChecked = false)
        {
            _factory.Syntax = loweredReceiver.Syntax;

            CSharpBinderFlags binderFlags = 0;
            if (isCompoundAssignment)
            {
                binderFlags |= CSharpBinderFlags.ValueFromCompoundAssignment;

                if (isChecked)
                {
                    binderFlags |= CSharpBinderFlags.CheckedContext;
                }
            }

            var loweredArguments = ImmutableArray<BoundExpression>.Empty;

            MethodSymbol argumentInfoFactory = GetArgumentInfoFactory();
            var binderConstruction = ((object)argumentInfoFactory != null) ? MakeBinderConstruction(WellKnownMember.Microsoft_CSharp_RuntimeBinder_Binder__SetMember, new[]
            {
                // flags:
                _factory.Literal((int)binderFlags),

                // name:
                _factory.Literal(name),

                // context:
                _factory.TypeofDynamicOperationContextType(),

                // argument infos:
                MakeCallSiteArgumentInfos(argumentInfoFactory, loweredArguments, loweredReceiver: loweredReceiver, loweredRight: loweredRight)
            }) : null;

            return MakeDynamicOperation(binderConstruction, loweredReceiver, RefKind.None, loweredArguments, default(ImmutableArray<RefKind>), loweredRight, AssemblySymbol.DynamicType);
        }

        internal LoweredDynamicOperation MakeDynamicGetIndex(
            BoundExpression loweredReceiver,
            ImmutableArray<BoundExpression> loweredArguments,
            ImmutableArray<string?> argumentNames,
            ImmutableArray<RefKind> refKinds)
        {
            _factory.Syntax = loweredReceiver.Syntax;

            var resultType = DynamicTypeSymbol.Instance;

            MethodSymbol argumentInfoFactory = GetArgumentInfoFactory();
            var binderConstruction = ((object)argumentInfoFactory != null) ? MakeBinderConstruction(WellKnownMember.Microsoft_CSharp_RuntimeBinder_Binder__GetIndex, new[]
            {
                // flags (unused):
                _factory.Literal((int)CSharpBinderFlags.None),

                // context:
                _factory.TypeofDynamicOperationContextType(),

                // argument infos:
                MakeCallSiteArgumentInfos(argumentInfoFactory, loweredArguments, argumentNames, refKinds, loweredReceiver: loweredReceiver)
            }) : null;

            return MakeDynamicOperation(binderConstruction, loweredReceiver, RefKind.None, loweredArguments, refKinds, null, resultType);
        }

        internal LoweredDynamicOperation MakeDynamicSetIndex(
            BoundExpression loweredReceiver,
            ImmutableArray<BoundExpression> loweredArguments,
            ImmutableArray<string?> argumentNames,
            ImmutableArray<RefKind> refKinds,
            BoundExpression loweredRight,
            bool isCompoundAssignment = false,
            bool isChecked = false)
        {
            CSharpBinderFlags binderFlags = 0;
            if (isCompoundAssignment)
            {
                binderFlags |= CSharpBinderFlags.ValueFromCompoundAssignment;

                if (isChecked)
                {
                    binderFlags |= CSharpBinderFlags.CheckedContext;
                }
            }

            var loweredReceiverRefKind = GetReceiverRefKind(loweredReceiver);
            var resultType = DynamicTypeSymbol.Instance;

            MethodSymbol argumentInfoFactory = GetArgumentInfoFactory();
            var binderConstruction = ((object)argumentInfoFactory != null) ? MakeBinderConstruction(WellKnownMember.Microsoft_CSharp_RuntimeBinder_Binder__SetIndex, new[]
            {
                // flags (unused):
                _factory.Literal((int)binderFlags),

                // context:
                _factory.TypeofDynamicOperationContextType(),

                // argument infos:
                MakeCallSiteArgumentInfos(argumentInfoFactory, loweredArguments, argumentNames, refKinds, loweredReceiver, loweredReceiverRefKind, loweredRight: loweredRight)
            }) : null;

            return MakeDynamicOperation(binderConstruction, loweredReceiver, loweredReceiverRefKind, loweredArguments, refKinds, loweredRight, resultType);
        }

        internal LoweredDynamicOperation MakeDynamicIsEventTest(string name, BoundExpression loweredReceiver)
        {
            _factory.Syntax = loweredReceiver.Syntax;
            var resultType = _factory.SpecialType(SpecialType.System_Boolean);
            var binderConstruction = MakeBinderConstruction(WellKnownMember.Microsoft_CSharp_RuntimeBinder_Binder__IsEvent, new[]
            {
                // flags (unused):
                _factory.Literal((int)0),

                // member name:
                _factory.Literal(name),

                // context:
                _factory.TypeofDynamicOperationContextType()
            });

            return MakeDynamicOperation(binderConstruction, loweredReceiver, RefKind.None, ImmutableArray<BoundExpression>.Empty, default(ImmutableArray<RefKind>), null, resultType);
        }

        private MethodSymbol GetArgumentInfoFactory()
        {
            return _factory.WellKnownMethod(WellKnownMember.Microsoft_CSharp_RuntimeBinder_CSharpArgumentInfo__Create);
        }

        private BoundExpression? MakeBinderConstruction(WellKnownMember factoryMethod, BoundExpression[] args)
        {
            var binderFactory = _factory.WellKnownMember(factoryMethod);
            if (binderFactory is null)
            {
                return null;
            }

            return _factory.Call(null, (MethodSymbol)binderFactory, args.AsImmutableOrNull());
        }

        // If we have a struct calling object, then we need to pass it by ref, provided
        // that it was an Lvalue. For instance,
        //     Struct s = ...; dynamic d = ...;
        //     s.M(d); // becomes Site(ref s, d)
        // however
        //     dynamic d = ...;
        //     GetS().M(d); // becomes Site(GetS(), d) without ref on the target obj arg
        internal RefKind GetReceiverRefKind(BoundExpression loweredReceiver)
        {
            Debug.Assert(loweredReceiver.Type is { });
            if (!loweredReceiver.Type.IsValueType)
            {
                return RefKind.None;
            }

            var hasHome = Binder.HasHome(loweredReceiver,
                Binder.AddressKind.Writeable,
                _factory.CurrentFunction,
                peVerifyCompatEnabled: false,
                stackLocalsOpt: null);
            return hasHome ? RefKind.Ref : RefKind.None;
        }

        internal BoundExpression MakeCallSiteArgumentInfos(
            MethodSymbol argumentInfoFactory,
            ImmutableArray<BoundExpression> loweredArguments,
            ImmutableArray<string?> argumentNames = default(ImmutableArray<string?>),
            ImmutableArray<RefKind> refKinds = default(ImmutableArray<RefKind>),
            BoundExpression? loweredReceiver = null,
            RefKind receiverRefKind = RefKind.None,
            bool receiverIsStaticType = false,
            BoundExpression? loweredRight = null)
        {
            const string? NoName = null;
            Debug.Assert(argumentNames.IsDefaultOrEmpty || loweredArguments.Length == argumentNames.Length);
            Debug.Assert(refKinds.IsDefault || loweredArguments.Length == refKinds.Length);
            Debug.Assert(!receiverIsStaticType || receiverRefKind == RefKind.None);

            var infos = new BoundExpression[(loweredReceiver != null ? 1 : 0) + loweredArguments.Length + (loweredRight != null ? 1 : 0)];
            int j = 0;
            if (loweredReceiver != null)
            {
                infos[j++] = GetArgumentInfo(argumentInfoFactory, loweredReceiver, NoName, receiverRefKind, receiverIsStaticType);
            }

            for (int i = 0; i < loweredArguments.Length; i++)
            {
                infos[j++] = GetArgumentInfo(
                argumentInfoFactory,
                    loweredArguments[i],
                    argumentNames.IsDefaultOrEmpty ? NoName : argumentNames[i],
                    refKinds.IsDefault ? RefKind.None : refKinds[i],
                    isStaticType: false);
            }

            if (loweredRight != null)
            {
                infos[j++] = GetArgumentInfo(argumentInfoFactory, loweredRight, NoName, RefKind.None, isStaticType: false);
            }

            return _factory.ArrayOrEmpty(argumentInfoFactory.ContainingType, infos);
        }

        internal LoweredDynamicOperation MakeDynamicOperation(
            BoundExpression? binderConstruction,
            BoundExpression? loweredReceiver,
            RefKind receiverRefKind,
            ImmutableArray<BoundExpression> loweredArguments,
            ImmutableArray<RefKind> refKinds,
            BoundExpression? loweredRight,
            TypeSymbol resultType)
        {
            Debug.Assert(!loweredArguments.IsDefault);

            // get well-known types and members we need:
            NamedTypeSymbol? delegateTypeOverMethodTypeParameters = GetDelegateType(loweredReceiver, receiverRefKind, loweredArguments, refKinds, loweredRight, resultType);
            NamedTypeSymbol callSiteTypeGeneric = _factory.WellKnownType(WellKnownType.System_Runtime_CompilerServices_CallSite_T);
            MethodSymbol callSiteFactoryGeneric = _factory.WellKnownMethod(WellKnownMember.System_Runtime_CompilerServices_CallSite_T__Create);
            FieldSymbol callSiteTargetFieldGeneric = (FieldSymbol)_factory.WellKnownMember(WellKnownMember.System_Runtime_CompilerServices_CallSite_T__Target);
            MethodSymbol? delegateInvoke;

            if (binderConstruction == null ||
                delegateTypeOverMethodTypeParameters is null ||
                delegateTypeOverMethodTypeParameters.IsErrorType() ||
                (delegateInvoke = delegateTypeOverMethodTypeParameters.DelegateInvokeMethod) is null ||
                callSiteTypeGeneric.IsErrorType() ||
                callSiteFactoryGeneric is null ||
                callSiteTargetFieldGeneric is null)
            {
                // CS1969: One or more types required to compile a dynamic expression cannot be found.
                // Dev11 reports it with source location for each dynamic operation, which results in many error messages.
                // The diagnostic that names the specific missing type or member has already been reported.
                _factory.Diagnostics.Add(ErrorCode.ERR_DynamicRequiredTypesMissing, NoLocation.Singleton);

                return LoweredDynamicOperation.Bad(loweredReceiver, loweredArguments, loweredRight, resultType);
            }

            if (_currentDynamicCallSiteContainer is null)
            {
                _currentDynamicCallSiteContainer = CreateCallSiteContainer(_factory, _methodOrdinal, _localFunctionOrdinal);
            }

            var containerDef = (SynthesizedContainer)_currentDynamicCallSiteContainer.OriginalDefinition;
            var methodToContainerTypeParametersMap = containerDef.TypeMap;

            ImmutableArray<LocalSymbol> temps = MakeTempsForDiscardArguments(ref loweredArguments);

            var callSiteType = callSiteTypeGeneric.Construct(new[] { delegateTypeOverMethodTypeParameters });
            var callSiteFactoryMethod = callSiteFactoryGeneric.AsMember(callSiteType);
            var callSiteTargetField = callSiteTargetFieldGeneric.AsMember(callSiteType);
            var callSiteField = DefineCallSiteStorageSymbol(containerDef, delegateTypeOverMethodTypeParameters, methodToContainerTypeParametersMap);
            var callSiteFieldAccess = _factory.Field(null, callSiteField);
            var callSiteArguments = GetCallSiteArguments(callSiteFieldAccess, loweredReceiver, loweredArguments, loweredRight);

            var nullCallSite = _factory.Null(callSiteField.Type);

            var siteInitialization = _factory.Conditional(
                _factory.ObjectEqual(callSiteFieldAccess, nullCallSite),
                _factory.AssignmentExpression(callSiteFieldAccess, _factory.Call(null, callSiteFactoryMethod, binderConstruction)),
                nullCallSite,
                callSiteField.Type);

            var siteInvocation = _factory.Call(
                _factory.Field(callSiteFieldAccess, callSiteTargetField),
                delegateInvoke,
                callSiteArguments);

            return new LoweredDynamicOperation(_factory, siteInitialization, siteInvocation, resultType, temps);
        }

        /// <summary>
        /// If there are any discards in the arguments, create locals for each, updates the arguments and
        /// returns the symbols that were created.
        /// Returns default if no discards found.
        /// </summary>
        private ImmutableArray<LocalSymbol> MakeTempsForDiscardArguments(ref ImmutableArray<BoundExpression> loweredArguments)
        {
            int discardCount = loweredArguments.Count(a => a.Kind == BoundKind.DiscardExpression);

            if (discardCount == 0)
            {
                return ImmutableArray<LocalSymbol>.Empty;
            }

            ArrayBuilder<LocalSymbol> temporariesBuilder = ArrayBuilder<LocalSymbol>.GetInstance(discardCount);
            loweredArguments = _factory.MakeTempsForDiscardArguments(loweredArguments, temporariesBuilder);
            return temporariesBuilder.ToImmutableAndFree();
        }

        private static NamedTypeSymbol CreateCallSiteContainer(SyntheticBoundNodeFactory factory, int methodOrdinal, int localFunctionOrdinal)
        {
            Debug.Assert(factory.CompilationState.ModuleBuilderOpt is { });
            Debug.Assert(factory.TopLevelMethod is { });
            Debug.Assert(factory.CurrentFunction is { });

            // We don't reuse call-sites during EnC. Each edit creates a new container and sites.
            int generation = factory.CompilationState.ModuleBuilderOpt.CurrentGenerationOrdinal;
            var containerName = GeneratedNames.MakeDynamicCallSiteContainerName(methodOrdinal, localFunctionOrdinal, generation);

            var synthesizedContainer = new DynamicSiteContainer(containerName, factory.TopLevelMethod, factory.CurrentFunction);
            factory.AddNestedType(synthesizedContainer);

            if (!synthesizedContainer.TypeParameters.IsEmpty)
            {
                return synthesizedContainer.Construct(synthesizedContainer.ConstructedFromTypeParameters.Cast<TypeParameterSymbol, TypeSymbol>());
            }

            return synthesizedContainer;
        }

        internal FieldSymbol DefineCallSiteStorageSymbol(NamedTypeSymbol containerDefinition, NamedTypeSymbol delegateTypeOverMethodTypeParameters, TypeMap methodToContainerTypeParametersMap)
        {
            var fieldName = GeneratedNames.MakeDynamicCallSiteFieldName(_callSiteIdDispenser++);
            var delegateTypeOverContainerTypeParameters = methodToContainerTypeParametersMap.SubstituteNamedType(delegateTypeOverMethodTypeParameters);
            var callSiteType = _factory.Compilation.GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_CallSite_T);
            _factory.Diagnostics.ReportUseSite(callSiteType, _factory.Syntax);
            callSiteType = callSiteType.Construct(new[] { delegateTypeOverContainerTypeParameters });
            var field = new SynthesizedFieldSymbol(containerDefinition, callSiteType, fieldName, isPublic: true, isStatic: true);
            _factory.AddField(containerDefinition, field);
            Debug.Assert(_currentDynamicCallSiteContainer is { });
            return _currentDynamicCallSiteContainer.IsGenericType ? field.AsMember(_currentDynamicCallSiteContainer) : field;
        }

        internal NamedTypeSymbol? GetDelegateType(
            BoundExpression? loweredReceiver,
            RefKind receiverRefKind,
            ImmutableArray<BoundExpression> loweredArguments,
            ImmutableArray<RefKind> refKinds,
            BoundExpression? loweredRight,
            TypeSymbol resultType)
        {
            Debug.Assert(refKinds.IsDefaultOrEmpty || refKinds.Length == loweredArguments.Length);

            var callSiteType = _factory.WellKnownType(WellKnownType.System_Runtime_CompilerServices_CallSite);
            if (callSiteType.IsErrorType())
            {
                return null;
            }

            var delegateSignature = MakeCallSiteDelegateSignature(callSiteType, loweredReceiver, loweredArguments, loweredRight, resultType);
            bool returnsVoid = resultType.IsVoidType();
            bool hasByRefs = receiverRefKind != RefKind.None || !refKinds.IsDefaultOrEmpty;

            if (!hasByRefs)
            {
                var wkDelegateType = returnsVoid ?
                    WellKnownTypes.GetWellKnownActionDelegate(invokeArgumentCount: delegateSignature.Length) :
                    WellKnownTypes.GetWellKnownFunctionDelegate(invokeArgumentCount: delegateSignature.Length - 1);

                if (wkDelegateType != WellKnownType.Unknown)
                {
                    var delegateType = _factory.Compilation.GetWellKnownType(wkDelegateType);
                    if (!delegateType.HasUseSiteError)
                    {
                        _factory.Diagnostics.AddDependencies(delegateType);
                        return delegateType.Construct(delegateSignature);
                    }
                }
            }

            RefKindVector byRefs;
            if (hasByRefs)
            {
                byRefs = RefKindVector.Create(1 + (loweredReceiver != null ? 1 : 0) + loweredArguments.Length + (loweredRight != null ? 1 : 0) + (returnsVoid ? 0 : 1));

                int j = 1;
                if (loweredReceiver != null)
                {
                    byRefs[j++] = getRefKind(receiverRefKind);
                }

                if (!refKinds.IsDefault)
                {
                    for (int i = 0; i < refKinds.Length; i++, j++)
                    {
                        byRefs[j] = getRefKind(refKinds[i]);
                    }
                }

                if (!returnsVoid)
                {
                    byRefs[j++] = RefKind.None;
                }
            }
            else
            {
                byRefs = default(RefKindVector);
            }

            int parameterCount = delegateSignature.Length - (returnsVoid ? 0 : 1);
            Debug.Assert(_factory.CompilationState.ModuleBuilderOpt is { });
            int generation = _factory.CompilationState.ModuleBuilderOpt.CurrentGenerationOrdinal;
            var synthesizedType = _factory.Compilation.AnonymousTypeManager.SynthesizeDelegate(parameterCount, byRefs, returnsVoid, generation);
            return synthesizedType.Construct(delegateSignature);

            // The distinction between by-ref kinds is ignored for dynamic call-sites.
            static RefKind getRefKind(RefKind refKind)
            {
                Debug.Assert(refKind != RefKind.RefReadOnlyParameter);
                return refKind == RefKind.None ? RefKind.None : RefKind.Ref;
            }
        }

        private BoundExpression GetArgumentInfo(
            MethodSymbol argumentInfoFactory,
            BoundExpression boundArgument,
            string? name,
            RefKind refKind,
            bool isStaticType)
        {
            CSharpArgumentInfoFlags flags = 0;

            if (isStaticType)
            {
                flags |= CSharpArgumentInfoFlags.IsStaticType;
            }

            if (name != null)
            {
                flags |= CSharpArgumentInfoFlags.NamedArgument;
            }

            Debug.Assert(refKind == RefKind.None || refKind == RefKind.Ref || refKind == RefKind.Out, "unexpected refKind in dynamic");

            // by-ref type doesn't trigger dynamic dispatch and it can't be a null literal => set UseCompileTimeType
            if (refKind == RefKind.Out)
            {
                flags |= CSharpArgumentInfoFlags.IsOut | CSharpArgumentInfoFlags.UseCompileTimeType;
            }
            else if (refKind == RefKind.Ref)
            {
                flags |= CSharpArgumentInfoFlags.IsRef | CSharpArgumentInfoFlags.UseCompileTimeType;
            }

            var argType = boundArgument.Type;

            // Check "literal" constant.

            // What the runtime binder does with this LiteralConstant flag is just to create a constant,
            // which is a compelling enough reason to make sure that on the production end of the binder
            // data, we do the inverse (i.e., use the LiteralConstant flag whenever we encounter a constant
            // argument.

            // And in fact, the bug being fixed with this change is that the compiler will consider constants
            // for numeric and enum conversions even if they are not literals (such as, (1-1) --> enum), but
            // the runtime binder didn't. So we do need to set this flag whenever we see a constant.

            // But the complication is that null values lose their type when they get to the runtime binder,
            // and so we need a way to distinguish a null constant of any given type from the null literal.
            // The design is simple! We use UseCompileTimeType to determine whether we care about the type of
            // a null constant argument, so that the null literal gets "LiteralConstant" whereas every other
            // constant gets "LiteralConstant | UseCompileTimeType". Because obviously UseCompileTimeType is
            // wrong for the null literal.

            // We care, because we want to prevent this from working:
            // 
            //    const C x = null;
            //    class C { public void M(SomeUnrelatedReferenceType x) { } }
            //    ...
            //    dynamic d = new C(); d.M(x); // This will pass a null constant and the type is gone!
            //
            // as well as the alternative where x is a const null of type object.

            if (boundArgument.ConstantValueOpt != null)
            {
                flags |= CSharpArgumentInfoFlags.Constant;
            }

            // Check compile time type.
            // See also DynamicRewriter::GenerateCallingObjectFlags.
            if (argType is { } && !argType.IsDynamic())
            {
                flags |= CSharpArgumentInfoFlags.UseCompileTimeType;
            }

            return _factory.Call(null, argumentInfoFactory, _factory.Literal((int)flags), _factory.Literal(name));
        }

        private static ImmutableArray<BoundExpression> GetCallSiteArguments(BoundExpression callSiteFieldAccess, BoundExpression? receiver, ImmutableArray<BoundExpression> arguments, BoundExpression? right)
        {
            var result = new BoundExpression[1 + (receiver != null ? 1 : 0) + arguments.Length + (right != null ? 1 : 0)];
            int j = 0;

            result[j++] = callSiteFieldAccess;

            if (receiver != null)
            {
                result[j++] = receiver;
            }

            arguments.CopyTo(result, j);
            j += arguments.Length;

            if (right != null)
            {
                result[j++] = right;
            }

            return result.AsImmutableOrNull();
        }

        private TypeSymbol[] MakeCallSiteDelegateSignature(TypeSymbol callSiteType, BoundExpression? receiver, ImmutableArray<BoundExpression> arguments, BoundExpression? right, TypeSymbol resultType)
        {
            var systemObjectType = _factory.SpecialType(SpecialType.System_Object);
            var result = new TypeSymbol[1 + (receiver != null ? 1 : 0) + arguments.Length + (right != null ? 1 : 0) + (resultType.IsVoidType() ? 0 : 1)];
            int j = 0;

            // CallSite:
            result[j++] = callSiteType;

            // receiver:
            if (receiver != null)
            {
                result[j++] = receiver.Type ?? systemObjectType;
            }

            // argument types:
            for (int i = 0; i < arguments.Length; i++)
            {
                result[j++] = arguments[i].Type ?? systemObjectType;
            }

            // right hand side of an assignment:
            if (right != null)
            {
                result[j++] = right.Type ?? systemObjectType;
            }

            // return type:
            if (j < result.Length)
            {
                result[j++] = resultType ?? systemObjectType;
            }

            return result;
        }
    }
}
