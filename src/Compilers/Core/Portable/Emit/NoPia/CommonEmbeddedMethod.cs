// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.CodeGen;

namespace Microsoft.CodeAnalysis.Emit.NoPia
{
    internal abstract partial class EmbeddedTypesManager<
        TPEModuleBuilder,
        TModuleCompilationState,
        TEmbeddedTypesManager,
        TSyntaxNode,
        TAttributeData,
        TSymbol,
        TAssemblySymbol,
        TNamedTypeSymbol,
        TFieldSymbol,
        TMethodSymbol,
        TEventSymbol,
        TPropertySymbol,
        TParameterSymbol,
        TTypeParameterSymbol,
        TEmbeddedType,
        TEmbeddedField,
        TEmbeddedMethod,
        TEmbeddedEvent,
        TEmbeddedProperty,
        TEmbeddedParameter,
        TEmbeddedTypeParameter>
    {
        internal abstract class CommonEmbeddedMethod : CommonEmbeddedMember<TMethodSymbol>, Cci.IMethodDefinition
        {
            public readonly TEmbeddedType ContainingType;
            private readonly ImmutableArray<TEmbeddedTypeParameter> _typeParameters;
            private readonly ImmutableArray<TEmbeddedParameter> _parameters;

            protected CommonEmbeddedMethod(TEmbeddedType containingType, TMethodSymbol underlyingMethod) :
                base(underlyingMethod)
            {
                this.ContainingType = containingType;
                _typeParameters = GetTypeParameters();
                _parameters = GetParameters();
            }

            protected abstract ImmutableArray<TEmbeddedTypeParameter> GetTypeParameters();
            protected abstract ImmutableArray<TEmbeddedParameter> GetParameters();
            protected abstract bool IsAbstract { get; }
            protected abstract bool IsAccessCheckedOnOverride { get; }
            protected abstract bool IsConstructor { get; }
            protected abstract bool IsExternal { get; }
            protected abstract bool IsHiddenBySignature { get; }
            protected abstract bool IsNewSlot { get; }
            protected abstract Cci.IPlatformInvokeInformation PlatformInvokeData { get; }
            protected abstract bool IsRuntimeSpecial { get; }
            protected abstract bool IsSpecialName { get; }
            protected abstract bool IsSealed { get; }
            protected abstract bool IsStatic { get; }
            protected abstract bool IsVirtual { get; }
            protected abstract System.Reflection.MethodImplAttributes GetImplementationAttributes(EmitContext context);
            protected abstract bool ReturnValueIsMarshalledExplicitly { get; }
            protected abstract Cci.IMarshallingInformation ReturnValueMarshallingInformation { get; }
            protected abstract ImmutableArray<byte> ReturnValueMarshallingDescriptor { get; }
            protected abstract Cci.TypeMemberVisibility Visibility { get; }
            protected abstract string Name { get; }
            protected abstract bool AcceptsExtraArguments { get; }
            protected abstract Cci.CallingConvention CallingConvention { get; }
            protected abstract Cci.ISignature UnderlyingMethodSignature { get; }
            protected abstract Cci.INamespace ContainingNamespace { get; }

            public TMethodSymbol UnderlyingMethod => this.UnderlyingSymbol;

            protected sealed override TAttributeData PortAttributeIfNeedTo(TAttributeData attrData, TSyntaxNode syntaxNodeOpt, DiagnosticBag diagnostics)
            {
                // Note, when porting attributes, we are not using constructors from original symbol.
                // The constructors might be missing (for example, in metadata case) and doing lookup
                // will ensure that we report appropriate errors.

                if (TypeManager.IsTargetAttribute(UnderlyingMethod, attrData, AttributeDescription.LCIDConversionAttribute))
                {
                    if (attrData.CommonConstructorArguments.Length == 1)
                    {
                        return TypeManager.CreateSynthesizedAttribute(WellKnownMember.System_Runtime_InteropServices_LCIDConversionAttribute__ctor, attrData, syntaxNodeOpt, diagnostics);
                    }
                }

                return null;
            }

            Cci.IMethodBody Cci.IMethodDefinition.GetBody(EmitContext context)
            {
                if (Cci.Extensions.HasBody(this))
                {
                    // This is an error condition, which we already reported.
                    // To prevent metadata emitter/visitor from crashing, let's
                    // return an empty body.
                    return new EmptyBody(this);
                }

                return null;
            }

            private sealed class EmptyBody : Cci.IMethodBody
            {
                private readonly CommonEmbeddedMethod _method;

                public EmptyBody(CommonEmbeddedMethod method)
                {
                    _method = method;
                }

                void Cci.IMethodBody.Dispatch(Cci.MetadataVisitor visitor)
                {
                    visitor.Visit(this);
                }

                ImmutableArray<Cci.ExceptionHandlerRegion> Cci.IMethodBody.ExceptionRegions =>
                    ImmutableArray<Cci.ExceptionHandlerRegion>.Empty;

                bool Cci.IMethodBody.LocalsAreZeroed => false;

                ImmutableArray<Cci.ILocalDefinition> Cci.IMethodBody.LocalVariables =>
                    ImmutableArray<Cci.ILocalDefinition>.Empty;

                Cci.IMethodDefinition Cci.IMethodBody.MethodDefinition => _method;

                ushort Cci.IMethodBody.MaxStack => 0;

                ImmutableArray<byte> Cci.IMethodBody.IL => ImmutableArray<byte>.Empty;

                bool Cci.IMethodBody.HasAnySequencePoints => false;

                ImmutableArray<Cci.SequencePoint> Cci.IMethodBody.GetSequencePoints()
                {
                    return ImmutableArray<Cci.SequencePoint>.Empty;
                }

                ImmutableArray<Cci.SequencePoint> Cci.IMethodBody.GetLocations()
                {
                    return ImmutableArray<Cci.SequencePoint>.Empty;
                }

                bool Cci.IMethodBody.HasDynamicLocalVariables => false;

                Cci.AsyncMethodBodyDebugInfo Cci.IMethodBody.AsyncDebugInfo => null;

                DynamicAnalysisMethodBodyData Cci.IMethodBody.DynamicAnalysisData => null;

                ImmutableArray<Cci.LocalScope> Cci.IMethodBody.LocalScopes =>
                    ImmutableArray<Cci.LocalScope>.Empty;

                Cci.IImportScope Cci.IMethodBody.ImportScope => null;

                ImmutableArray<Cci.StateMachineHoistedLocalScope> Cci.IMethodBody.StateMachineHoistedLocalScopes =>
                    default(ImmutableArray<Cci.StateMachineHoistedLocalScope>);

                string Cci.IMethodBody.StateMachineTypeName => null;

                ImmutableArray<EncHoistedLocalInfo> Cci.IMethodBody.StateMachineHoistedLocalSlots =>
                    default(ImmutableArray<EncHoistedLocalInfo>);

                ImmutableArray<Cci.ITypeReference> Cci.IMethodBody.StateMachineAwaiterSlots =>
                    default(ImmutableArray<Cci.ITypeReference>);

                ImmutableArray<ClosureDebugInfo> Cci.IMethodBody.ClosureDebugInfo =>
                    default(ImmutableArray<ClosureDebugInfo>);

                ImmutableArray<LambdaDebugInfo> Cci.IMethodBody.LambdaDebugInfo =>
                    default(ImmutableArray<LambdaDebugInfo>);

                public DebugId MethodId => default(DebugId);
            }

            IEnumerable<Cci.IGenericMethodParameter> Cci.IMethodDefinition.GenericParameters => _typeParameters;

            bool Cci.IMethodDefinition.IsImplicitlyDeclared => true;

            bool Cci.IMethodDefinition.HasDeclarativeSecurity => false;

            bool Cci.IMethodDefinition.IsAbstract => IsAbstract;

            bool Cci.IMethodDefinition.IsAccessCheckedOnOverride => IsAccessCheckedOnOverride;

            bool Cci.IMethodDefinition.IsConstructor => IsConstructor;

            bool Cci.IMethodDefinition.IsExternal => IsExternal;

            bool Cci.IMethodDefinition.IsHiddenBySignature => IsHiddenBySignature;

            bool Cci.IMethodDefinition.IsNewSlot => IsNewSlot;

            bool Cci.IMethodDefinition.IsPlatformInvoke => PlatformInvokeData != null;

            Cci.IPlatformInvokeInformation Cci.IMethodDefinition.PlatformInvokeData => PlatformInvokeData;

            bool Cci.IMethodDefinition.IsRuntimeSpecial => IsRuntimeSpecial;

            bool Cci.IMethodDefinition.IsSpecialName => IsSpecialName;

            bool Cci.IMethodDefinition.IsSealed => IsSealed;

            bool Cci.IMethodDefinition.IsStatic => IsStatic;

            bool Cci.IMethodDefinition.IsVirtual => IsVirtual;

            System.Reflection.MethodImplAttributes Cci.IMethodDefinition.GetImplementationAttributes(EmitContext context)
            {
                return GetImplementationAttributes(context);
            }

            ImmutableArray<Cci.IParameterDefinition> Cci.IMethodDefinition.Parameters
            {
                get
                {
                    return StaticCast<Cci.IParameterDefinition>.From(_parameters);
                }
            }

            bool Cci.IMethodDefinition.RequiresSecurityObject => false;

            IEnumerable<Cci.ICustomAttribute> Cci.IMethodDefinition.ReturnValueAttributes
            {
                get
                {
                    // TODO:
                    return SpecializedCollections.EmptyEnumerable<Cci.ICustomAttribute>();
                }
            }

            bool Cci.IMethodDefinition.ReturnValueIsMarshalledExplicitly => ReturnValueIsMarshalledExplicitly;

            Cci.IMarshallingInformation Cci.IMethodDefinition.ReturnValueMarshallingInformation => ReturnValueMarshallingInformation;

            ImmutableArray<byte> Cci.IMethodDefinition.ReturnValueMarshallingDescriptor => ReturnValueMarshallingDescriptor;

            IEnumerable<Cci.SecurityAttribute> Cci.IMethodDefinition.SecurityAttributes =>
                SpecializedCollections.EmptyEnumerable<Cci.SecurityAttribute>();

            Cci.ITypeDefinition Cci.ITypeDefinitionMember.ContainingTypeDefinition => ContainingType;

            Cci.INamespace Cci.IMethodDefinition.ContainingNamespace => ContainingNamespace;

            Cci.TypeMemberVisibility Cci.ITypeDefinitionMember.Visibility => Visibility;

            Cci.ITypeReference Cci.ITypeMemberReference.GetContainingType(EmitContext context)
            {
                return ContainingType;
            }

            void Cci.IReference.Dispatch(Cci.MetadataVisitor visitor)
            {
                visitor.Visit(this);
            }

            Cci.IDefinition Cci.IReference.AsDefinition(EmitContext context)
            {
                return this;
            }

            string Cci.INamedEntity.Name => Name;

            bool Cci.IMethodReference.AcceptsExtraArguments => AcceptsExtraArguments;

            ushort Cci.IMethodReference.GenericParameterCount => (ushort)_typeParameters.Length;

            bool Cci.IMethodReference.IsGeneric => _typeParameters.Length > 0;

            Cci.IMethodDefinition Cci.IMethodReference.GetResolvedMethod(EmitContext context)
            {
                return this;
            }

            ImmutableArray<Cci.IParameterTypeInformation> Cci.IMethodReference.ExtraParameters
            {
                get
                {
                    // This is a definition, no information about extra parameters 
                    return ImmutableArray<Cci.IParameterTypeInformation>.Empty;
                }
            }

            Cci.IGenericMethodInstanceReference Cci.IMethodReference.AsGenericMethodInstanceReference => null;

            Cci.ISpecializedMethodReference Cci.IMethodReference.AsSpecializedMethodReference => null;

            Cci.CallingConvention Cci.ISignature.CallingConvention => CallingConvention;

            ushort Cci.ISignature.ParameterCount => (ushort)_parameters.Length;

            ImmutableArray<Cci.IParameterTypeInformation> Cci.ISignature.GetParameters(EmitContext context)
            {
                return StaticCast<Cci.IParameterTypeInformation>.From(_parameters);
            }

            ImmutableArray<Cci.ICustomModifier> Cci.ISignature.ReturnValueCustomModifiers =>
                UnderlyingMethodSignature.ReturnValueCustomModifiers;

            bool Cci.ISignature.ReturnValueIsByRef => UnderlyingMethodSignature.ReturnValueIsByRef;

            Cci.ITypeReference Cci.ISignature.GetType(EmitContext context)
            {
                return UnderlyingMethodSignature.GetType(context);
            }

            /// <remarks>
            /// This is only used for testing.
            /// </remarks>
            public override string ToString()
            {
                return ((ISymbol)UnderlyingMethod).ToDisplayString(SymbolDisplayFormat.ILVisualizationFormat);
            }
        }
    }
}
