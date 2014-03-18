// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Emit
{
    internal abstract class NamedTypeReference : Microsoft.Cci.INamedTypeReference
    {
        protected readonly NamedTypeSymbol UnderlyingNamedType;

        public NamedTypeReference(NamedTypeSymbol underlyingNamedType)
        {
            Debug.Assert((object)underlyingNamedType != null);

            this.UnderlyingNamedType = underlyingNamedType;
        }

        ushort Microsoft.Cci.INamedTypeReference.GenericParameterCount
        {
            get
            {
                return (ushort)UnderlyingNamedType.Arity;
            }
        }

        bool Microsoft.Cci.INamedTypeReference.MangleName
        {
            get
            {
                return UnderlyingNamedType.MangleName;
            }
        }

        string Microsoft.Cci.INamedEntity.Name
        {
            get
            {
                return UnderlyingNamedType.MetadataName;
            }
        }

        bool Microsoft.Cci.ITypeReference.IsEnum
        {
            get
            {
                return UnderlyingNamedType.IsEnumType();
            }
        }

        bool Microsoft.Cci.ITypeReference.IsValueType
        {
            get
            {
                return UnderlyingNamedType.IsValueType;
            }
        }

        Microsoft.Cci.ITypeDefinition Microsoft.Cci.ITypeReference.GetResolvedType(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return null;
        }

        Microsoft.Cci.PrimitiveTypeCode Microsoft.Cci.ITypeReference.TypeCode(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return Microsoft.Cci.PrimitiveTypeCode.NotPrimitive;
        }

        TypeHandle Microsoft.Cci.ITypeReference.TypeDef
        {
            get
            {
                return default(TypeHandle);
            }
        }

        Microsoft.Cci.IGenericMethodParameterReference Microsoft.Cci.ITypeReference.AsGenericMethodParameterReference
        {
            get
            {
                return null;
            }
        }

        public abstract Microsoft.Cci.IGenericTypeInstanceReference /*Microsoft.Cci.ITypeReference.*/ AsGenericTypeInstanceReference
        {
            get;
        }

        Microsoft.Cci.IGenericTypeParameterReference Microsoft.Cci.ITypeReference.AsGenericTypeParameterReference
        {
            get
            {
                return null;
            }
        }

        Microsoft.Cci.INamespaceTypeDefinition Microsoft.Cci.ITypeReference.AsNamespaceTypeDefinition(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return null;
        }

        public abstract Microsoft.Cci.INamespaceTypeReference /*Microsoft.Cci.ITypeReference.*/ AsNamespaceTypeReference
        {
            get;
        }

        Microsoft.Cci.INestedTypeDefinition Microsoft.Cci.ITypeReference.AsNestedTypeDefinition(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return null;
        }

        public abstract Microsoft.Cci.INestedTypeReference /*Microsoft.Cci.ITypeReference.*/ AsNestedTypeReference
        {
            get;
        }

        public abstract Microsoft.Cci.ISpecializedNestedTypeReference /*Microsoft.Cci.ITypeReference.*/ AsSpecializedNestedTypeReference
        {
            get;
        }

        Microsoft.Cci.ITypeDefinition Microsoft.Cci.ITypeReference.AsTypeDefinition(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return null;
        }

        public override string ToString()
        {
            return UnderlyingNamedType.ToDisplayString(SymbolDisplayFormat.ILVisualizationFormat);
        }

        IEnumerable<Microsoft.Cci.ICustomAttribute> Microsoft.Cci.IReference.GetAttributes(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return SpecializedCollections.EmptyEnumerable<Microsoft.Cci.ICustomAttribute>();

            // foreach (var a in GetAttributes()) yield return a; // this throws today.
        }

        public abstract void /*Microsoft.Cci.IReference*/ Dispatch(Microsoft.Cci.MetadataVisitor visitor);

        Microsoft.Cci.IDefinition Microsoft.Cci.IReference.AsDefinition(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return null;
        }
    }
}
