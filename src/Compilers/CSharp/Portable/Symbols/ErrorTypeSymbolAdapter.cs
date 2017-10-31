// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal abstract partial class ErrorTypeSymbol : Cci.INamespaceTypeReference
    {
        Cci.IUnitReference Cci.INamespaceTypeReference.GetUnit(EmitContext context)
        {
            // error types, when emitted, belong to a recognizable error assembly
            return ErrorAssembly.Singleton;
        }

        string Cci.INamespaceTypeReference.NamespaceName
        {
            get
            {
                if (ContainingType is null)
                {
                    return "";
                }
                return ContainingType.ToDisplayString();
            }
        }

        ushort Cci.INamedTypeReference.GenericParameterCount => (ushort)Arity;

        bool Cci.INamedTypeReference.MangleName => Arity != 0;

        bool Cci.ITypeReference.IsEnum => false;

        bool Cci.ITypeReference.IsValueType => false;

        Cci.ITypeDefinition Cci.ITypeReference.GetResolvedType(EmitContext context)
        {
            return null;
        }

        Cci.PrimitiveTypeCode Cci.ITypeReference.TypeCode => Cci.PrimitiveTypeCode.NotPrimitive;

        TypeDefinitionHandle Cci.ITypeReference.TypeDef => default(TypeDefinitionHandle);

        Cci.IGenericMethodParameterReference Cci.ITypeReference.AsGenericMethodParameterReference => null;

        Cci.IGenericTypeInstanceReference Cci.ITypeReference.AsGenericTypeInstanceReference => null;

        Cci.IGenericTypeParameterReference Cci.ITypeReference.AsGenericTypeParameterReference => null;

        Cci.INamespaceTypeDefinition Cci.ITypeReference.AsNamespaceTypeDefinition(EmitContext context)
        {
            return null;
        }

        Cci.INamespaceTypeReference Cci.ITypeReference.AsNamespaceTypeReference => this;

        Cci.INestedTypeDefinition Cci.ITypeReference.AsNestedTypeDefinition(EmitContext context)
        {
            return null;
        }

        Cci.INestedTypeReference Cci.ITypeReference.AsNestedTypeReference => null;

        Cci.ISpecializedNestedTypeReference Cci.ITypeReference.AsSpecializedNestedTypeReference => null;

        Cci.ITypeDefinition Cci.ITypeReference.AsTypeDefinition(EmitContext context)
        {
            return null;
        }

        IEnumerable<Cci.ICustomAttribute> Cci.IReference.GetAttributes(EmitContext context)
        {
            return SpecializedCollections.EmptyEnumerable<Cci.ICustomAttribute>();
        }

        void Cci.IReference.Dispatch(Cci.MetadataVisitor visitor)
        {
            visitor.Visit((Cci.INamespaceTypeReference)this);
        }

        Cci.IDefinition Cci.IReference.AsDefinition(EmitContext context)
        {
            return null;
        }

        string Cci.INamedEntity.Name => Name;
    }
}
