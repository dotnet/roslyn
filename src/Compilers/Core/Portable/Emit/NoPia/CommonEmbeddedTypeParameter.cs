// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
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
        internal abstract class CommonEmbeddedTypeParameter : Cci.IEmbeddedDefinition, Cci.IGenericMethodParameter
        {
            public readonly TEmbeddedMethod ContainingMethod;
            public readonly TTypeParameterSymbol UnderlyingTypeParameter;

            protected CommonEmbeddedTypeParameter(TEmbeddedMethod containingMethod, TTypeParameterSymbol underlyingTypeParameter)
            {
                this.ContainingMethod = containingMethod;
                this.UnderlyingTypeParameter = underlyingTypeParameter;
            }

            public bool IsEncDeleted
                => false;

            protected abstract IEnumerable<Cci.TypeReferenceWithAttributes> GetConstraints(EmitContext context);
            protected abstract bool MustBeReferenceType { get; }
            protected abstract bool MustBeValueType { get; }
            protected abstract bool AllowsRefLikeType { get; }
            protected abstract bool MustHaveDefaultConstructor { get; }
            protected abstract string Name { get; }
            protected abstract ushort Index { get; }

            Cci.IMethodDefinition Cci.IGenericMethodParameter.DefiningMethod
            {
                get
                {
                    return ContainingMethod;
                }
            }

            IEnumerable<Cci.TypeReferenceWithAttributes> Cci.IGenericParameter.GetConstraints(EmitContext context)
            {
                return GetConstraints(context);
            }

            bool Cci.IGenericParameter.MustBeReferenceType
            {
                get
                {
                    return MustBeReferenceType;
                }
            }

            bool Cci.IGenericParameter.MustBeValueType
            {
                get
                {
                    return MustBeValueType;
                }
            }

            bool Cci.IGenericParameter.AllowsRefLikeType
            {
                get
                {
                    return AllowsRefLikeType;
                }
            }

            bool Cci.IGenericParameter.MustHaveDefaultConstructor
            {
                get
                {
                    return MustHaveDefaultConstructor;
                }
            }

            Cci.TypeParameterVariance Cci.IGenericParameter.Variance
            {
                get
                {
                    // Method type parameters are not variant
                    return Cci.TypeParameterVariance.NonVariant;
                }
            }

            Cci.IGenericMethodParameter Cci.IGenericParameter.AsGenericMethodParameter
            {
                get
                {
                    return this;
                }
            }

            Cci.IGenericTypeParameter Cci.IGenericParameter.AsGenericTypeParameter
            {
                get
                {
                    return null;
                }
            }

            bool Cci.ITypeReference.IsEnum
            {
                get { return false; }
            }

            bool Cci.ITypeReference.IsValueType
            {
                get { return false; }
            }

            Cci.ITypeDefinition Cci.ITypeReference.GetResolvedType(EmitContext context)
            {
                return null;
            }

            Cci.PrimitiveTypeCode Cci.ITypeReference.TypeCode
            {
                get
                {
                    return Cci.PrimitiveTypeCode.NotPrimitive;
                }
            }

            TypeDefinitionHandle Cci.ITypeReference.TypeDef
            {
                get { return default(TypeDefinitionHandle); }
            }

            Cci.IGenericMethodParameterReference Cci.ITypeReference.AsGenericMethodParameterReference
            {
                get { return this; }
            }

            Cci.IGenericTypeInstanceReference Cci.ITypeReference.AsGenericTypeInstanceReference
            {
                get { return null; }
            }

            Cci.IGenericTypeParameterReference Cci.ITypeReference.AsGenericTypeParameterReference
            {
                get { return null; }
            }

            Cci.INamespaceTypeDefinition Cci.ITypeReference.AsNamespaceTypeDefinition(EmitContext context)
            {
                return null;
            }

            Cci.INamespaceTypeReference Cci.ITypeReference.AsNamespaceTypeReference
            {
                get { return null; }
            }

            Cci.INestedTypeDefinition Cci.ITypeReference.AsNestedTypeDefinition(EmitContext context)
            {
                return null;
            }

            Cci.INestedTypeReference Cci.ITypeReference.AsNestedTypeReference
            {
                get { return null; }
            }

            Cci.ISpecializedNestedTypeReference Cci.ITypeReference.AsSpecializedNestedTypeReference
            {
                get { return null; }
            }

            Cci.ITypeDefinition Cci.ITypeReference.AsTypeDefinition(EmitContext context)
            {
                return null;
            }

            IEnumerable<Cci.ICustomAttribute> Cci.IReference.GetAttributes(EmitContext context)
            {
                // TODO:
                return SpecializedCollections.EmptyEnumerable<Cci.ICustomAttribute>();
            }

            void Cci.IReference.Dispatch(Cci.MetadataVisitor visitor)
            {
                throw ExceptionUtilities.Unreachable();
            }

            Cci.IDefinition Cci.IReference.AsDefinition(EmitContext context)
            {
                return null;
            }

            CodeAnalysis.Symbols.ISymbolInternal Cci.IReference.GetInternalSymbol() => null;

            string Cci.INamedEntity.Name
            {
                get { return Name; }
            }

            ushort Cci.IParameterListEntry.Index
            {
                get
                {
                    return Index;
                }
            }

            Cci.IMethodReference Cci.IGenericMethodParameterReference.DefiningMethod
            {
                get { return ContainingMethod; }
            }

            public sealed override bool Equals(object obj)
            {
                // It is not supported to rely on default equality of these Cci objects, an explicit way to compare and hash them should be used.
                throw Roslyn.Utilities.ExceptionUtilities.Unreachable();
            }

            public sealed override int GetHashCode()
            {
                // It is not supported to rely on default equality of these Cci objects, an explicit way to compare and hash them should be used.
                throw Roslyn.Utilities.ExceptionUtilities.Unreachable();
            }
        }
    }
}
