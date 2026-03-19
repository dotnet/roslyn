// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Emit
{
    internal abstract class NamedTypeReference : Cci.INamedTypeReference
    {
        protected readonly NamedTypeSymbol UnderlyingNamedType;

        public NamedTypeReference(NamedTypeSymbol underlyingNamedType)
        {
            Debug.Assert((object)underlyingNamedType != null);

            this.UnderlyingNamedType = underlyingNamedType;
        }

        ushort Cci.INamedTypeReference.GenericParameterCount
        {
            get
            {
                return (ushort)UnderlyingNamedType.Arity;
            }
        }

        bool Cci.INamedTypeReference.MangleName
        {
            get
            {
                return UnderlyingNamedType.MangleName;
            }
        }

#nullable enable
        string? Cci.INamedTypeReference.AssociatedFileIdentifier
        {
            get
            {
                return UnderlyingNamedType.GetFileLocalTypeMetadataNamePrefix();
            }
        }
#nullable disable

        string Cci.INamedEntity.Name
        {
            get
            {
                return UnderlyingNamedType.MetadataName;
            }
        }

        bool Cci.ITypeReference.IsEnum
        {
            get
            {
                return UnderlyingNamedType.IsEnumType();
            }
        }

        bool Cci.ITypeReference.IsValueType
        {
            get
            {
                return UnderlyingNamedType.IsValueType;
            }
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
            get
            {
                return default(TypeDefinitionHandle);
            }
        }

        Cci.IGenericMethodParameterReference Cci.ITypeReference.AsGenericMethodParameterReference
        {
            get
            {
                return null;
            }
        }

        public abstract Cci.IGenericTypeInstanceReference AsGenericTypeInstanceReference
        {
            get;
        }

        Cci.IGenericTypeParameterReference Cci.ITypeReference.AsGenericTypeParameterReference
        {
            get
            {
                return null;
            }
        }

        Cci.INamespaceTypeDefinition Cci.ITypeReference.AsNamespaceTypeDefinition(EmitContext context)
        {
            return null;
        }

        public abstract Cci.INamespaceTypeReference AsNamespaceTypeReference
        {
            get;
        }

        Cci.INestedTypeDefinition Cci.ITypeReference.AsNestedTypeDefinition(EmitContext context)
        {
            return null;
        }

        public abstract Cci.INestedTypeReference AsNestedTypeReference
        {
            get;
        }

        public abstract Cci.ISpecializedNestedTypeReference AsSpecializedNestedTypeReference
        {
            get;
        }

        Cci.ITypeDefinition Cci.ITypeReference.AsTypeDefinition(EmitContext context)
        {
            return null;
        }

        public override string ToString()
        {
            return UnderlyingNamedType.ToDisplayString(SymbolDisplayFormat.ILVisualizationFormat);
        }

        IEnumerable<Cci.ICustomAttribute> Cci.IReference.GetAttributes(EmitContext context)
        {
            return SpecializedCollections.EmptyEnumerable<Cci.ICustomAttribute>();
        }

        public abstract void Dispatch(Cci.MetadataVisitor visitor);

        Cci.IDefinition Cci.IReference.AsDefinition(EmitContext context)
        {
            return null;
        }

        CodeAnalysis.Symbols.ISymbolInternal Cci.IReference.GetInternalSymbol() => UnderlyingNamedType;

        public sealed override bool Equals(object obj)
        {
            // It is not supported to rely on default equality of these Cci objects, an explicit way to compare and hash them should be used.
            throw ExceptionUtilities.Unreachable();
        }

        public sealed override int GetHashCode()
        {
            // It is not supported to rely on default equality of these Cci objects, an explicit way to compare and hash them should be used.
            throw ExceptionUtilities.Unreachable();
        }
    }
}
