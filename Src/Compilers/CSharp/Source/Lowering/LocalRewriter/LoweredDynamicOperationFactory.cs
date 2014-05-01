// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class LoweredDynamicOperationFactory
    {
        private readonly SyntheticBoundNodeFactory factory;
        private NamedTypeSymbol currentDynamicCallSiteContainer;

        internal LoweredDynamicOperationFactory(SyntheticBoundNodeFactory factory)
        {
            Debug.Assert(factory != null);
            this.factory = factory;
        }

        // We could read the values of the following enums from metadata instead of hardcoding them here but 
        // - they can never change since existing programs have the values inlined and would be broken if the values changed their meaning,
        // - if any new flags are added to the runtime binder the compiler will change as well to produce them.

        // The only scenario that is not supported by hardcoding the values is when a completely new Framework is created 
        // that redefines these constants and is not supposed to run existing programs.

        /// <summary>
        /// Corresponds to <see cref="T:Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags"/>.
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
        /// Corresponds to <see cref="T:Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags"/>.
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
            factory.Syntax = loweredOperand.Syntax;

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
                factory.Literal((int)binderFlags),

                // target type:
                factory.Typeof(resultType),

                // context:
                factory.Typeof(factory.CurrentClass)
            });

            return MakeDynamicOperation(binderConstruction, null, RefKind.None, loweredArguments, ImmutableArray<RefKind>.Empty, null, resultType);
        }

        internal LoweredDynamicOperation MakeDynamicUnaryOperator(
            UnaryOperatorKind operatorKind,
            BoundExpression loweredOperand,
            TypeSymbol resultType)
        {
            Debug.Assert(operatorKind.IsDynamic());

            factory.Syntax = loweredOperand.Syntax;

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
                factory.Literal((int)binderFlags),

                // expression type:
                factory.Literal((int)operatorKind.ToExpressionType()),

                // context:
                factory.Typeof(factory.CurrentClass),

                // argument infos:
                MakeCallSiteArgumentInfos(argumentInfoFactory, loweredArguments)
            }) : null;

            return MakeDynamicOperation(binderConstruction, null, RefKind.None, loweredArguments, ImmutableArray<RefKind>.Empty, null, resultType);
        }

        internal LoweredDynamicOperation MakeDynamicBinaryOperator(
            BinaryOperatorKind operatorKind,
            BoundExpression loweredLeft,
            BoundExpression loweredRight,
            bool isCompoundAssignment,
            TypeSymbol resultType)
        {
            Debug.Assert(operatorKind.IsDynamic());

            factory.Syntax = loweredLeft.Syntax;

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
                factory.Literal((int)binderFlags),

                // expression type:
                factory.Literal((int)operatorKind.ToExpressionType(isCompoundAssignment)),

                // context:
                factory.Typeof(factory.CurrentClass),

                // argument infos:
                MakeCallSiteArgumentInfos(argumentInfoFactory, loweredArguments)
            }) : null;

            return MakeDynamicOperation(binderConstruction, null, RefKind.None, loweredArguments, ImmutableArray<RefKind>.Empty, null, resultType);
        }

        internal LoweredDynamicOperation MakeDynamicMemberInvocation(
            string name,
            BoundExpression loweredReceiver,
            ImmutableArray<TypeSymbol> typeArguments,
            ImmutableArray<BoundExpression> loweredArguments,
            ImmutableArray<string> argumentNames,
            ImmutableArray<RefKind> refKinds,
            bool hasImplicitReceiver,
            bool resultDiscarded)
        {
            factory.Syntax = loweredReceiver.Syntax;

            CSharpBinderFlags binderFlags = 0;
            if (hasImplicitReceiver && !factory.TopLevelMethod.IsStatic)
            {
                binderFlags |= CSharpBinderFlags.InvokeSimpleName;
            }

            TypeSymbol resultType;
            if (resultDiscarded)
            {
                binderFlags |= CSharpBinderFlags.ResultDiscarded;
                resultType = factory.SpecialType(SpecialType.System_Void);
            }
            else
            {
                resultType = AssemblySymbol.DynamicType;
            }

            RefKind receiverRefKind;
            bool receiverIsStaticType;
            if (loweredReceiver.Kind == BoundKind.TypeExpression)
            {
                loweredReceiver = factory.Typeof(((BoundTypeExpression)loweredReceiver).Type);
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
                factory.Literal((int)binderFlags),

                // member name:
                factory.Literal(name),

                // type arguments:
                typeArguments.IsDefaultOrEmpty ?
                    factory.Null(factory.WellKnownArrayType(WellKnownType.System_Type)) :
                    factory.Array(factory.WellKnownType(WellKnownType.System_Type), factory.TypeOfs(typeArguments)),

                // context:
                factory.Typeof(factory.CurrentClass),

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
            factory.Syntax = loweredReceiver.Syntax;

            CSharpBinderFlags binderFlags = CSharpBinderFlags.InvokeSpecialName | CSharpBinderFlags.ResultDiscarded;

            var loweredArguments = ImmutableArray<BoundExpression>.Empty;
            var resultType = AssemblySymbol.DynamicType;

            MethodSymbol argumentInfoFactory = GetArgumentInfoFactory();
            var binderConstruction = ((object)argumentInfoFactory != null) ? MakeBinderConstruction(WellKnownMember.Microsoft_CSharp_RuntimeBinder_Binder__InvokeMember, new[]
            {
                // flags:
                factory.Literal((int)binderFlags),

                // member name:
                factory.Literal(accessorName),

                // type arguments:
                factory.Null(factory.WellKnownArrayType(WellKnownType.System_Type)),

                // context:
                factory.Typeof(factory.CurrentClass),

                // argument infos:
                MakeCallSiteArgumentInfos(argumentInfoFactory, loweredArguments, loweredReceiver: loweredReceiver, loweredRight: loweredHandler)
            }) : null;

            return MakeDynamicOperation(binderConstruction, loweredReceiver, RefKind.None, loweredArguments, ImmutableArray<RefKind>.Empty, loweredHandler, resultType);
        }

        internal LoweredDynamicOperation MakeDynamicInvocation(
            BoundExpression loweredReceiver,
            ImmutableArray<BoundExpression> loweredArguments,
            ImmutableArray<string> argumentNames,
            ImmutableArray<RefKind> refKinds,
            bool resultDiscarded)
        {
            factory.Syntax = loweredReceiver.Syntax;

            TypeSymbol resultType;
            CSharpBinderFlags binderFlags = 0;
            if (resultDiscarded)
            {
                binderFlags |= CSharpBinderFlags.ResultDiscarded;
                resultType = factory.SpecialType(SpecialType.System_Void);
            }
            else
            {
                resultType = AssemblySymbol.DynamicType;
            }

            MethodSymbol argumentInfoFactory = GetArgumentInfoFactory();
            var binderConstruction = ((object)argumentInfoFactory != null) ? MakeBinderConstruction(WellKnownMember.Microsoft_CSharp_RuntimeBinder_Binder__Invoke, new[]
            {
                // flags:
                factory.Literal((int)binderFlags),

                // context:
                factory.Typeof(factory.CurrentClass),

                // argument infos:
                MakeCallSiteArgumentInfos(argumentInfoFactory, loweredArguments, argumentNames, refKinds, loweredReceiver)
            }) : null;

            return MakeDynamicOperation(binderConstruction, loweredReceiver, RefKind.None, loweredArguments, refKinds, null, resultType);
        }

        internal LoweredDynamicOperation MakeDynamicConstructorInvocation(
        CSharpSyntaxNode syntax,
            TypeSymbol type,
            ImmutableArray<BoundExpression> loweredArguments,
            ImmutableArray<string> argumentNames,
            ImmutableArray<RefKind> refKinds)
        {
            factory.Syntax = syntax;

            var loweredReceiver = factory.Typeof(type);

            MethodSymbol argumentInfoFactory = GetArgumentInfoFactory();
            var binderConstruction = ((object)argumentInfoFactory != null) ? MakeBinderConstruction(WellKnownMember.Microsoft_CSharp_RuntimeBinder_Binder__InvokeConstructor, new[]
            {
                // flags:
                factory.Literal(0),

                // context:
                factory.Typeof(factory.CurrentClass),

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
            factory.Syntax = loweredReceiver.Syntax;

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
                factory.Literal((int)binderFlags),

                // name:
                factory.Literal(name),

                // context:
                factory.Typeof(factory.CurrentClass),

                // argument infos:
                MakeCallSiteArgumentInfos(argumentInfoFactory, loweredArguments, loweredReceiver: loweredReceiver)
            }) : null;

            return MakeDynamicOperation(binderConstruction, loweredReceiver, RefKind.None, loweredArguments, ImmutableArray<RefKind>.Empty, null, resultType);
        }

        internal LoweredDynamicOperation MakeDynamicSetMember(
            BoundExpression loweredReceiver,
            string name,
            BoundExpression loweredRight,
            bool isCompoundAssignment = false,
            bool isChecked = false)
        {
            factory.Syntax = loweredReceiver.Syntax;

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
                factory.Literal((int)binderFlags),

                // name:
                factory.Literal(name),

                // context:
                factory.Typeof(factory.CurrentClass),

                // argument infos:
                MakeCallSiteArgumentInfos(argumentInfoFactory, loweredArguments, loweredReceiver: loweredReceiver, loweredRight: loweredRight)
            }) : null;

            return MakeDynamicOperation(binderConstruction, loweredReceiver, RefKind.None, loweredArguments, ImmutableArray<RefKind>.Empty, loweredRight, AssemblySymbol.DynamicType);
        }

        internal LoweredDynamicOperation MakeDynamicGetIndex(
            BoundExpression loweredReceiver,
            ImmutableArray<BoundExpression> loweredArguments,
            ImmutableArray<string> argumentNames,
            ImmutableArray<RefKind> refKinds)
        {
            factory.Syntax = loweredReceiver.Syntax;

            var resultType = DynamicTypeSymbol.Instance;

            MethodSymbol argumentInfoFactory = GetArgumentInfoFactory();
            var binderConstruction = ((object)argumentInfoFactory != null) ? MakeBinderConstruction(WellKnownMember.Microsoft_CSharp_RuntimeBinder_Binder__GetIndex, new[]
            {
                // flags (unused):
                factory.Literal((int)CSharpBinderFlags.None),

                // context:
                factory.Typeof(factory.CurrentClass),

                // argument infos:
                MakeCallSiteArgumentInfos(argumentInfoFactory, loweredArguments, argumentNames, refKinds, loweredReceiver: loweredReceiver)
            }) : null;

            return MakeDynamicOperation(binderConstruction, loweredReceiver, RefKind.None, loweredArguments, refKinds, null, resultType);
        }

        internal LoweredDynamicOperation MakeDynamicSetIndex(
            BoundExpression loweredReceiver,
            ImmutableArray<BoundExpression> loweredArguments,
            ImmutableArray<string> argumentNames,
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
                factory.Literal((int)binderFlags),

                // context:
                factory.Typeof(factory.CurrentClass),

                // argument infos:
                MakeCallSiteArgumentInfos(argumentInfoFactory, loweredArguments, argumentNames, refKinds, loweredReceiver, loweredReceiverRefKind, loweredRight: loweredRight)
            }) : null;

            return MakeDynamicOperation(binderConstruction, loweredReceiver, loweredReceiverRefKind, loweredArguments, refKinds, loweredRight, resultType);
        }

        internal LoweredDynamicOperation MakeDynamicIsEventTest(string name, BoundExpression loweredReceiver)
        {
            factory.Syntax = loweredReceiver.Syntax;
            var resultType = factory.SpecialType(SpecialType.System_Boolean);
            var binderConstruction = MakeBinderConstruction(WellKnownMember.Microsoft_CSharp_RuntimeBinder_Binder__IsEvent, new[]
            {
                // flags (unused):
                factory.Literal((int)0),

                // member name:
                factory.Literal(name),

                // context:
                factory.Typeof(factory.CurrentClass)
            });

            return MakeDynamicOperation(binderConstruction, loweredReceiver, RefKind.None, ImmutableArray<BoundExpression>.Empty, ImmutableArray<RefKind>.Empty, null, resultType);
        }

        private MethodSymbol GetArgumentInfoFactory()
        {
            return factory.WellKnownMethod(WellKnownMember.Microsoft_CSharp_RuntimeBinder_CSharpArgumentInfo__Create, isOptional: false);
        }

        private BoundExpression MakeBinderConstruction(WellKnownMember factoryMethod, BoundExpression[] args)
        {
            var binderFactory = factory.WellKnownMember(factoryMethod);
            if ((object)binderFactory == null)
            {
                return null;
            }

            return factory.Call(null, (MethodSymbol)binderFactory, args.AsImmutableOrNull());
        }

        // If we have a struct calling object, then we need to pass it by ref, provided
        // that it was an Lvalue. For instance,
        //     Struct s = ...; dynamic d = ...;
        //     s.M(d); // becomes Site(ref s, d)
        // however
        //     dynamic d = ...;
        //     GetS().M(d); // becomes Site(GetS(), d) without ref on the target obj arg
        internal static RefKind GetReceiverRefKind(BoundExpression loweredReceiver)
        {
            if (!loweredReceiver.Type.IsValueType)
            {
                return RefKind.None;
            }

            switch (loweredReceiver.Kind)
            {
                case BoundKind.Local:
                case BoundKind.Parameter:
                case BoundKind.ArrayAccess:
                case BoundKind.ThisReference:
                case BoundKind.PointerIndirectionOperator:
                case BoundKind.PointerElementAccess:
                case BoundKind.RefValueOperator:
                    return RefKind.Ref;

                case BoundKind.BaseReference:
                // base dynamic dispatch is not supported, an error has already been reported
                case BoundKind.TypeExpression:
                    throw ExceptionUtilities.UnexpectedValue(loweredReceiver.Kind);
            }

            return RefKind.None;
        }

        internal BoundExpression MakeCallSiteArgumentInfos(
            MethodSymbol argumentInfoFactory,
            ImmutableArray<BoundExpression> loweredArguments,
            ImmutableArray<string> argumentNames = default(ImmutableArray<string>),
            ImmutableArray<RefKind> refKinds = default(ImmutableArray<RefKind>),
            BoundExpression loweredReceiver = null,
            RefKind receiverRefKind = RefKind.None,
            bool receiverIsStaticType = false,
            BoundExpression loweredRight = null)
        {
            const string NoName = null;
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

            return factory.Array(argumentInfoFactory.ContainingType, infos);
        }

        internal bool GeneratedDynamicOperations
        {
            get { return (object)this.currentDynamicCallSiteContainer != null; }
        }

        internal LoweredDynamicOperation MakeDynamicOperation(
            BoundExpression binderConstruction,
            BoundExpression loweredReceiver,
            RefKind receiverRefKind,
            ImmutableArray<BoundExpression> loweredArguments,
            ImmutableArray<RefKind> refKinds,
            BoundExpression loweredRight,
            TypeSymbol resultType)
        {
            Debug.Assert(!loweredArguments.IsDefault);

            // get well-known types and members we need:
            NamedTypeSymbol delegateTypeOverMethodTypeParameters = GetDelegateType(loweredReceiver, receiverRefKind, loweredArguments, refKinds, loweredRight, resultType);
            NamedTypeSymbol callSiteTypeGeneric = factory.WellKnownType(WellKnownType.System_Runtime_CompilerServices_CallSite_T);
            MethodSymbol callSiteFactoryGeneric = factory.WellKnownMethod(WellKnownMember.System_Runtime_CompilerServices_CallSite_T__Create);
            FieldSymbol callSiteTargetFieldGeneric = (FieldSymbol)factory.WellKnownMember(WellKnownMember.System_Runtime_CompilerServices_CallSite_T__Target);
            MethodSymbol delegateInvoke;

            if (binderConstruction == null ||
                (object)delegateTypeOverMethodTypeParameters == null ||
                delegateTypeOverMethodTypeParameters.IsErrorType() ||
                (object)(delegateInvoke = delegateTypeOverMethodTypeParameters.DelegateInvokeMethod) == null ||
                callSiteTypeGeneric.IsErrorType() ||
                (object)callSiteFactoryGeneric == null ||
                (object)callSiteTargetFieldGeneric == null)
            {
                // CS1969: One or more types required to compile a dynamic expression cannot be found.
                // Dev11 reports it with source location for each dynamic operation, which results in many error messages.
                // The diagnostic that names the specific missing type or member has already been reported.
                factory.Diagnostics.Add(ErrorCode.ERR_DynamicRequiredTypesMissing, NoLocation.Singleton);

                return LoweredDynamicOperation.Bad(loweredReceiver, loweredArguments, loweredRight, resultType);
            }

            if ((object)this.currentDynamicCallSiteContainer == null)
            {
                this.currentDynamicCallSiteContainer = CreateCallSiteContainer(this.factory);
            }

            var containerDef = (SynthesizedContainer)this.currentDynamicCallSiteContainer.OriginalDefinition;
            var methodToContainerTypeParametersMap = containerDef.TypeMap;

            var callSiteType = callSiteTypeGeneric.Construct(new[] { delegateTypeOverMethodTypeParameters });
            var callSiteFactoryMethod = callSiteFactoryGeneric.AsMember(callSiteType);
            var callSiteTargetField = callSiteTargetFieldGeneric.AsMember(callSiteType);
            var callSiteField = DefineCallSiteStorageSymbol(containerDef, delegateTypeOverMethodTypeParameters, methodToContainerTypeParametersMap);
            var callSiteFieldAccess = factory.Field(null, callSiteField);
            var callSiteArguments = GetCallSiteArguments(callSiteFieldAccess, loweredReceiver, loweredArguments, loweredRight);

            var nullCallSite = factory.Null(callSiteField.Type);

            var siteInitialization = factory.Conditional(
                factory.ObjectEqual(callSiteFieldAccess, nullCallSite),
                factory.AssignmentExpression(callSiteFieldAccess, factory.Call(null, callSiteFactoryMethod, new[] { binderConstruction })),
                nullCallSite,
                callSiteField.Type);

            var siteInvocation = factory.Call(
                factory.Field(callSiteFieldAccess, callSiteTargetField),
                delegateInvoke,
                callSiteArguments);

            return new LoweredDynamicOperation(factory, siteInitialization, siteInvocation, resultType);
        }

        private static NamedTypeSymbol CreateCallSiteContainer(SyntheticBoundNodeFactory factory)
        {
            // TODO (tomat): consider - why do we need to include a method name at all? We could save some metadata bytes by not including it.
            // Dev11 uses an empty string for explicit interface method implementation:
            var containerName = GeneratedNames.MakeDynamicCallSiteContainerName(
                factory.TopLevelMethod.IsExplicitInterfaceImplementation ? "" : factory.TopLevelMethod.Name, factory.CompilationState.GenerateTempNumber());

            var synthesizedContainer = new DynamicSiteContainer(containerName, factory.TopLevelMethod);
            factory.AddNestedType(synthesizedContainer);

            if (factory.TopLevelMethod.IsGenericMethod)
            {
                return synthesizedContainer.Construct(factory.TopLevelMethod.TypeParameters.Cast<TypeParameterSymbol, TypeSymbol>());
            }

            return synthesizedContainer;
        }

        internal FieldSymbol DefineCallSiteStorageSymbol(NamedTypeSymbol containerDefinition, NamedTypeSymbol delegateTypeOverMethodTypeParameters, TypeMap methodToContainerTypeParametersMap)
        {
            var fieldName = GeneratedNames.MakeDynamicCallSiteFieldName(factory.CompilationState.GenerateTempNumber());
            var delegateTypeOverContainerTypeParameters = methodToContainerTypeParametersMap.SubstituteNamedType(delegateTypeOverMethodTypeParameters);
            var callSiteType = factory.Compilation.GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_CallSite_T).Construct(new[] { delegateTypeOverContainerTypeParameters });
            var field = new SynthesizedFieldSymbol(containerDefinition, callSiteType, fieldName, isPublic: true, isStatic: true);
            factory.AddField(containerDefinition, field);
            return currentDynamicCallSiteContainer.IsGenericType ? field.AsMember(currentDynamicCallSiteContainer) : field;
        }

        internal NamedTypeSymbol GetDelegateType(
            BoundExpression loweredReceiver,
            RefKind receiverRefKind,
            ImmutableArray<BoundExpression> loweredArguments,
            ImmutableArray<RefKind> refKinds,
            BoundExpression loweredRight,
            TypeSymbol resultType)
        {
            Debug.Assert(refKinds.IsDefaultOrEmpty || refKinds.Length == loweredArguments.Length);

            var callSiteType = factory.WellKnownType(WellKnownType.System_Runtime_CompilerServices_CallSite);
            if (callSiteType.IsErrorType())
            {
                return null;
            }

            var delegateSignature = MakeCallSiteDelegateSignature(callSiteType, loweredReceiver, loweredArguments, loweredRight, resultType);
            bool returnsVoid = resultType.SpecialType == SpecialType.System_Void;
            bool hasByRefs = receiverRefKind != RefKind.None || !refKinds.IsDefaultOrEmpty;

            if (!hasByRefs)
            {
                var wkDelegateType = returnsVoid ?
                    WellKnownTypes.GetWellKnownActionDelegate(invokeArgumentCount: delegateSignature.Length) :
                    WellKnownTypes.GetWellKnownFunctionDelegate(invokeArgumentCount: delegateSignature.Length - 1);

                if (wkDelegateType != WellKnownType.Unknown)
                {
                    var delegateType = factory.Compilation.GetWellKnownType(wkDelegateType);
                    if (!delegateType.HasUseSiteError)
                    {
                        return delegateType.Construct(delegateSignature);
                    }
                }
            }

            BitArray byRefs;
            if (hasByRefs)
            {
                byRefs = BitArray.Create(1 + (loweredReceiver != null ? 1 : 0) + loweredArguments.Length + (loweredRight != null ? 1 : 0));

                int j = 1;
                if (loweredReceiver != null)
                {
                    byRefs[j++] = receiverRefKind != RefKind.None;
                }

                if (!refKinds.IsDefault)
                {
                    for (int i = 0; i < refKinds.Length; i++, j++)
                    {
                        if (refKinds[i] != RefKind.None)
                        {
                            byRefs[j] = true;
                        }
                    }
                }
            }
            else
            {
                byRefs = default(BitArray);
            }

            int parameterCount = delegateSignature.Length - (returnsVoid ? 0 : 1);

            return factory.Compilation.AnonymousTypeManager.SynthesizeDelegate(parameterCount, byRefs, returnsVoid).Construct(delegateSignature);
        }

        internal BoundExpression GetArgumentInfo(
        MethodSymbol argumentInfoFactory,
            BoundExpression boundArgument,
            string name,
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

            // But the compilication is that null values lose their type when they get to the runtime binder,
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

            if (boundArgument.ConstantValue != null)
            {
                flags |= CSharpArgumentInfoFlags.Constant;
            }

            // Check compile time type.
            // See also DynamicRewriter::GenerateCallingObjectFlags.
            if ((object)argType != null && !argType.IsDynamic())
            {
                flags |= CSharpArgumentInfoFlags.UseCompileTimeType;
            }

            return factory.Call(null, argumentInfoFactory, new[] { factory.Literal((int)flags), factory.Literal(name) });
        }

        internal static ImmutableArray<BoundExpression> GetCallSiteArguments(BoundExpression callSiteFieldAccess, BoundExpression receiver, ImmutableArray<BoundExpression> arguments, BoundExpression right)
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

        internal TypeSymbol[] MakeCallSiteDelegateSignature(TypeSymbol callSiteType, BoundExpression receiver, ImmutableArray<BoundExpression> arguments, BoundExpression right, TypeSymbol resultType)
        {
            var systemObjectType = factory.SpecialType(SpecialType.System_Object);
            var result = new TypeSymbol[1 + (receiver != null ? 1 : 0) + arguments.Length + (right != null ? 1 : 0) + (resultType.SpecialType == SpecialType.System_Void ? 0 : 1)];
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
