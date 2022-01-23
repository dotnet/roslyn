// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeGen;
using Cci = Microsoft.Cci;

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
        internal abstract class CommonEmbeddedProperty : CommonEmbeddedMember<TPropertySymbol>, Cci.IPropertyDefinition
        {
            private readonly ImmutableArray<TEmbeddedParameter> _parameters;
            private readonly TEmbeddedMethod _getter;
            private readonly TEmbeddedMethod _setter;

            protected CommonEmbeddedProperty(TPropertySymbol underlyingProperty, TEmbeddedMethod getter, TEmbeddedMethod setter) :
                base(underlyingProperty)
            {
                Debug.Assert(getter != null || setter != null);

                _getter = getter;
                _setter = setter;
                _parameters = GetParameters();
            }

            internal override TEmbeddedTypesManager TypeManager
            {
                get
                {
                    return AnAccessor.TypeManager;
                }
            }

            protected abstract ImmutableArray<TEmbeddedParameter> GetParameters();
            protected abstract bool IsRuntimeSpecial { get; }
            protected abstract bool IsSpecialName { get; }
            protected abstract Cci.ISignature UnderlyingPropertySignature { get; }
            protected abstract TEmbeddedType ContainingType { get; }
            protected abstract Cci.TypeMemberVisibility Visibility { get; }
            protected abstract string Name { get; }

            public TPropertySymbol UnderlyingProperty
            {
                get
                {
                    return this.UnderlyingSymbol;
                }
            }

            Cci.IMethodReference Cci.IPropertyDefinition.Getter
            {
                get { return _getter; }
            }

            Cci.IMethodReference Cci.IPropertyDefinition.Setter
            {
                get { return _setter; }
            }

            IEnumerable<Cci.IMethodReference> Cci.IPropertyDefinition.GetAccessors(EmitContext context)
            {
                if (_getter != null)
                {
                    yield return _getter;
                }

                if (_setter != null)
                {
                    yield return _setter;
                }
            }

            bool Cci.IPropertyDefinition.HasDefaultValue
            {
                get { return false; }
            }

            MetadataConstant Cci.IPropertyDefinition.DefaultValue
            {
                get { return null; }
            }

            bool Cci.IPropertyDefinition.IsRuntimeSpecial
            {
                get { return IsRuntimeSpecial; }
            }

            bool Cci.IPropertyDefinition.IsSpecialName
            {
                get
                {
                    return IsSpecialName;
                }
            }

            ImmutableArray<Cci.IParameterDefinition> Cci.IPropertyDefinition.Parameters
            {
                get { return StaticCast<Cci.IParameterDefinition>.From(_parameters); }
            }

            Cci.CallingConvention Cci.ISignature.CallingConvention
            {
                get
                {
                    return UnderlyingPropertySignature.CallingConvention;
                }
            }

            ushort Cci.ISignature.ParameterCount
            {
                get { return (ushort)_parameters.Length; }
            }

            ImmutableArray<Cci.IParameterTypeInformation> Cci.ISignature.GetParameters(EmitContext context)
            {
                return StaticCast<Cci.IParameterTypeInformation>.From(_parameters);
            }

            ImmutableArray<Cci.ICustomModifier> Cci.ISignature.ReturnValueCustomModifiers
            {
                get
                {
                    return UnderlyingPropertySignature.ReturnValueCustomModifiers;
                }
            }

            ImmutableArray<Cci.ICustomModifier> Cci.ISignature.RefCustomModifiers
            {
                get
                {
                    return UnderlyingPropertySignature.RefCustomModifiers;
                }
            }

            bool Cci.ISignature.ReturnValueIsByRef
            {
                get
                {
                    return UnderlyingPropertySignature.ReturnValueIsByRef;
                }
            }

            Cci.ITypeReference Cci.ISignature.GetType(EmitContext context)
            {
                return UnderlyingPropertySignature.GetType(context);
            }

            protected TEmbeddedMethod AnAccessor
            {
                get
                {
                    return _getter ?? _setter;
                }
            }

            Cci.ITypeDefinition Cci.ITypeDefinitionMember.ContainingTypeDefinition
            {
                get { return ContainingType; }
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
                visitor.Visit((Cci.IPropertyDefinition)this);
            }

            Cci.IDefinition Cci.IReference.AsDefinition(EmitContext context)
            {
                return this;
            }

            string Cci.INamedEntity.Name
            {
                get
                {
                    return Name;
                }
            }
        }
    }
}
