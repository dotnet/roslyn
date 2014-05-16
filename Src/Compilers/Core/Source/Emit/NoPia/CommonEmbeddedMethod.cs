// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Roslyn.Utilities;
using System.Collections.Generic;
using System;

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
            private readonly ImmutableArray<TEmbeddedTypeParameter> typeParameters;
            private readonly ImmutableArray<TEmbeddedParameter> parameters;

            protected CommonEmbeddedMethod(TEmbeddedType containingType, TMethodSymbol underlyingMethod) :
                base(underlyingMethod)
            {
                this.ContainingType = containingType;
                this.typeParameters = GetTypeParameters();
                this.parameters = GetParameters();
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

            public TMethodSymbol UnderlyingMethod
            {
                get
                {
                    return this.UnderlyingSymbol;
                }
            }

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
                if (Microsoft.Cci.Extensions.HasBody(this))
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
                public readonly CommonEmbeddedMethod Method;

                public EmptyBody(CommonEmbeddedMethod method)
                {
                    this.Method = method;
                }

                void Cci.IMethodBody.Dispatch(Cci.MetadataVisitor visitor)
                {
                    visitor.Visit((Cci.IMethodBody)this);
                }

                ImmutableArray<Cci.ExceptionHandlerRegion> Cci.IMethodBody.ExceptionRegions
                {
                    get { return ImmutableArray<Cci.ExceptionHandlerRegion>.Empty; }
                }

                bool Cci.IMethodBody.LocalsAreZeroed
                {
                    get { return false; }
                }

                ImmutableArray<Cci.ILocalDefinition> Cci.IMethodBody.LocalVariables
                {
                    get { return ImmutableArray<Cci.ILocalDefinition>.Empty; }
                }

                Cci.IMethodDefinition Cci.IMethodBody.MethodDefinition
                {
                    get { return Method; }
                }

                ushort Cci.IMethodBody.MaxStack
                {
                    get { return 0; }
                }

                byte[] Cci.IMethodBody.IL
                {
                    get { return SpecializedCollections.EmptyArray<byte>(); }
                }

                bool Cci.IMethodBody.HasAnyLocations
                {
                    get { return false; }
                }

                bool Cci.IMethodBody.HasAnySequencePoints
                {
                    get { return false; }
                }

                ImmutableArray<Cci.SequencePoint> Cci.IMethodBody.GetSequencePoints()
                {
                    return ImmutableArray<Cci.SequencePoint>.Empty;
                }

                ImmutableArray<Cci.SequencePoint> Cci.IMethodBody.GetLocations()
                {
                    return ImmutableArray<Cci.SequencePoint>.Empty;
                }

                Cci.CustomDebugInfoKind Cci.IMethodBody.CustomDebugInfoKind
                {
                    get { return Cci.CustomDebugInfoKind.CSharpStyle; }
                }

                bool Cci.IMethodBody.HasDynamicLocalVariables
                {
                    get { return false; }
                }

                Cci.AsyncMethodBodyDebugInfo Cci.IMethodBody.AsyncMethodDebugInfo
                {
                    get { return null; }
                }

                ImmutableArray<Cci.LocalScope> Cci.IMethodBody.LocalScopes
                {
                    get { return ImmutableArray<Cci.LocalScope>.Empty; }
                }

                ImmutableArray<Cci.NamespaceScope> Cci.IMethodBody.NamespaceScopes
                {
                    get { return ImmutableArray<Cci.NamespaceScope>.Empty; }
                }

                ImmutableArray<Cci.LocalScope> Cci.IMethodBody.IteratorScopes
                {
                    get { return ImmutableArray<Cci.LocalScope>.Empty; }
                }

                string Cci.IMethodBody.IteratorClassName
                {
                    get { return null; }
                }
            }

            IEnumerable<Cci.IGenericMethodParameter> Cci.IMethodDefinition.GenericParameters
            {
                get
                {
                    return typeParameters;
                }
            }

            bool Cci.IMethodDefinition.IsImplicitlyDeclared
            {
                get
                {
                    return true;
                }
            }

            bool Cci.IMethodDefinition.HasDeclarativeSecurity
            {
                get
                {
                    return false;
                }
            }

            bool Cci.IMethodDefinition.IsAbstract
            {
                get
                {
                    return IsAbstract;
                }
            }

            bool Cci.IMethodDefinition.IsAccessCheckedOnOverride
            {
                get
                {
                    return IsAccessCheckedOnOverride;
                }
            }

            bool Cci.IMethodDefinition.IsConstructor
            {
                get
                {
                    return IsConstructor;
                }
            }

            bool Cci.IMethodDefinition.IsExternal
            {
                get
                {
                    return IsExternal;
                }
            }

            bool Cci.IMethodDefinition.IsHiddenBySignature
            {
                get
                {
                    return IsHiddenBySignature;
                }
            }

            bool Cci.IMethodDefinition.IsNewSlot
            {
                get
                {
                    return IsNewSlot;
                }
            }

            bool Cci.IMethodDefinition.IsPlatformInvoke
            {
                get
                {
                    return PlatformInvokeData != null;
                }
            }

            Cci.IPlatformInvokeInformation Cci.IMethodDefinition.PlatformInvokeData
            {
                get
                {
                    return PlatformInvokeData;
                }
            }

            bool Cci.IMethodDefinition.IsRuntimeSpecial
            {
                get
                {
                    return IsRuntimeSpecial;
                }
            }

            bool Cci.IMethodDefinition.IsSpecialName
            {
                get
                {
                    return IsSpecialName;
                }
            }

            bool Cci.IMethodDefinition.IsSealed
            {
                get
                {
                    return IsSealed;
                }
            }

            bool Cci.IMethodDefinition.IsStatic
            {
                get
                {
                    return IsStatic;
                }
            }

            bool Cci.IMethodDefinition.IsVirtual
            {
                get
                {
                    return IsVirtual;
                }
            }

            System.Reflection.MethodImplAttributes Cci.IMethodDefinition.GetImplementationAttributes(EmitContext context)
            {
                return GetImplementationAttributes(context);
            }

            ImmutableArray<Cci.IParameterDefinition> Cci.IMethodDefinition.Parameters
            {
                get
                {
                    return StaticCast<Cci.IParameterDefinition>.From(parameters);
                }
            }

            bool Cci.IMethodDefinition.RequiresSecurityObject
            {
                get
                {
                    return false;
                }
            }

            IEnumerable<Cci.ICustomAttribute> Cci.IMethodDefinition.ReturnValueAttributes
            {
                get
                {
                    // TODO:
                    return SpecializedCollections.EmptyEnumerable<Cci.ICustomAttribute>();
                }
            }

            bool Cci.IMethodDefinition.ReturnValueIsMarshalledExplicitly
            {
                get
                {
                    return ReturnValueIsMarshalledExplicitly;
                }
            }

            Cci.IMarshallingInformation Cci.IMethodDefinition.ReturnValueMarshallingInformation
            {
                get
                {
                    return ReturnValueMarshallingInformation;
                }
            }

            ImmutableArray<byte> Cci.IMethodDefinition.ReturnValueMarshallingDescriptor
            {
                get
                {
                    return ReturnValueMarshallingDescriptor;
                }
            }

            IEnumerable<Cci.SecurityAttribute> Cci.IMethodDefinition.SecurityAttributes
            {
                get
                {
                    return SpecializedCollections.EmptyEnumerable<Cci.SecurityAttribute>();
                }
            }

            Cci.ITypeDefinition Cci.ITypeDefinitionMember.ContainingTypeDefinition
            {
                get
                {
                    return ContainingType;
                }
            }

            Cci.TypeMemberVisibility Cci.ITypeDefinitionMember.Visibility
            {
                get
                {
                    return Visibility;
                }
            }

            Cci.ITypeReference Cci.ITypeMemberReference.GetContainingType(EmitContext context)
            {
                return ContainingType;
            }

            void Cci.IReference.Dispatch(Cci.MetadataVisitor visitor)
            {
                visitor.Visit((Cci.IMethodDefinition)this);
            }

            Cci.IDefinition Cci.IReference.AsDefinition(EmitContext context)
            {
                return this;
            }

            string Cci.INamedEntity.Name
            {
                get { return Name; }
            }

            bool Cci.IMethodReference.AcceptsExtraArguments
            {
                get
                {
                    return AcceptsExtraArguments;
                }
            }

            ushort Cci.IMethodReference.GenericParameterCount
            {
                get
                {
                    return (ushort)typeParameters.Length;
                }
            }

            bool Cci.IMethodReference.IsGeneric
            {
                get
                {
                    return typeParameters.Length > 0;
                }
            }

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

            Cci.IGenericMethodInstanceReference Cci.IMethodReference.AsGenericMethodInstanceReference
            {
                get
                {
                    return null;
                }
            }

            Cci.ISpecializedMethodReference Cci.IMethodReference.AsSpecializedMethodReference
            {
                get
                {
                    return null;
                }
            }

            Cci.CallingConvention Cci.ISignature.CallingConvention
            {
                get
                {
                    return CallingConvention;
                }
            }

            ushort Cci.ISignature.ParameterCount
            {
                get
                {
                    return (ushort)parameters.Length;
                }
            }

            ImmutableArray<Cci.IParameterTypeInformation> Cci.ISignature.GetParameters(EmitContext context)
            {
                return StaticCast<Cci.IParameterTypeInformation>.From(parameters);
            }

            ImmutableArray<Cci.ICustomModifier> Cci.ISignature.ReturnValueCustomModifiers
            {
                get
                {
                    return UnderlyingMethodSignature.ReturnValueCustomModifiers;
                }
            }

            bool Cci.ISignature.ReturnValueIsByRef
            {
                get
                {
                    return UnderlyingMethodSignature.ReturnValueIsByRef;
                }
            }

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
