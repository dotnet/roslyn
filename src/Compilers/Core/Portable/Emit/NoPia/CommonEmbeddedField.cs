// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
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
        internal abstract class CommonEmbeddedField : CommonEmbeddedMember<TFieldSymbol>, Cci.IFieldDefinition
        {
            public readonly TEmbeddedType ContainingType;

            protected CommonEmbeddedField(TEmbeddedType containingType, TFieldSymbol underlyingField) :
                base(underlyingField)
            {
                this.ContainingType = containingType;
            }

            public TFieldSymbol UnderlyingField
            {
                get
                {
                    return UnderlyingSymbol;
                }
            }

            protected abstract MetadataConstant GetCompileTimeValue(EmitContext context);
            protected abstract bool IsCompileTimeConstant { get; }
            protected abstract bool IsNotSerialized { get; }
            protected abstract bool IsReadOnly { get; }
            protected abstract bool IsRuntimeSpecial { get; }
            protected abstract bool IsSpecialName { get; }
            protected abstract bool IsStatic { get; }
            protected abstract bool IsMarshalledExplicitly { get; }
            protected abstract Cci.IMarshallingInformation MarshallingInformation { get; }
            protected abstract ImmutableArray<byte> MarshallingDescriptor { get; }
            protected abstract int? TypeLayoutOffset { get; }
            protected abstract Cci.TypeMemberVisibility Visibility { get; }
            protected abstract string Name { get; }

            MetadataConstant Cci.IFieldDefinition.GetCompileTimeValue(EmitContext context)
            {
                return GetCompileTimeValue(context);
            }

            ImmutableArray<byte> Cci.IFieldDefinition.MappedData
            {
                get
                {
                    return default(ImmutableArray<byte>);
                }
            }

            bool Cci.IFieldDefinition.IsCompileTimeConstant
            {
                get
                {
                    return IsCompileTimeConstant;
                }
            }

            bool Cci.IFieldDefinition.IsNotSerialized
            {
                get
                {
                    return IsNotSerialized;
                }
            }

            bool Cci.IFieldDefinition.IsReadOnly
            {
                get
                {
                    return IsReadOnly;
                }
            }

            bool Cci.IFieldDefinition.IsRuntimeSpecial
            {
                get
                {
                    return IsRuntimeSpecial;
                }
            }

            bool Cci.IFieldDefinition.IsSpecialName
            {
                get
                {
                    return IsSpecialName;
                }
            }

            bool Cci.IFieldDefinition.IsStatic
            {
                get
                {
                    return IsStatic;
                }
            }

            bool Cci.IFieldDefinition.IsMarshalledExplicitly
            {
                get
                {
                    return IsMarshalledExplicitly;
                }
            }

            Cci.IMarshallingInformation Cci.IFieldDefinition.MarshallingInformation
            {
                get
                {
                    return MarshallingInformation;
                }
            }

            ImmutableArray<byte> Cci.IFieldDefinition.MarshallingDescriptor
            {
                get
                {
                    return MarshallingDescriptor;
                }
            }

            int Cci.IFieldDefinition.Offset
            {
                get
                {
                    return TypeLayoutOffset ?? 0;
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
                visitor.Visit((Cci.IFieldDefinition)this);
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

            Cci.ITypeReference Cci.IFieldReference.GetType(EmitContext context)
            {
                return UnderlyingField.GetType(context);
            }

            ImmutableArray<Cci.ICustomModifier> Cci.IFieldReference.RefCustomModifiers => UnderlyingField.RefCustomModifiers;

            bool Cci.IFieldReference.IsByReference => UnderlyingField.IsByReference;

            Cci.IFieldDefinition Cci.IFieldReference.GetResolvedField(EmitContext context)
            {
                return this;
            }

            Cci.ISpecializedFieldReference Cci.IFieldReference.AsSpecializedFieldReference
            {
                get
                {
                    return null;
                }
            }

            bool Cci.IFieldReference.IsContextualNamedEntity
            {
                get
                {
                    return false;
                }
            }
        }
    }
}
