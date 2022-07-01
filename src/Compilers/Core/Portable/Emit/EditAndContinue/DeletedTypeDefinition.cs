// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.Symbols;

namespace Microsoft.CodeAnalysis.Emit.EditAndContinue
{
    /// <summary>
    /// Represents a type referenced from a deleted member (as distinct from a type that has been deleted)
    /// </summary>
    internal sealed class DeletedTypeDefinition : ITypeDefinition
    {
        [return: NotNullIfNotNull("typeReference")]
        public static ITypeReference? TryCreate(ITypeReference? typeReference, Dictionary<ITypeDefinition, DeletedTypeDefinition> cache)
        {
            if (typeReference is ITypeDefinition typeDef)
            {
                if (!cache.TryGetValue(typeDef, out var deletedType))
                {
                    deletedType = new DeletedTypeDefinition(typeDef);
                    cache.Add(typeDef, deletedType);
                }

                return deletedType;
            }

            return typeReference;
        }

        private readonly ITypeDefinition _oldTypeReference;

        public ITypeDefinition Original => _oldTypeReference;

        private DeletedTypeDefinition(ITypeDefinition typeReference)
        {
            _oldTypeReference = typeReference;
        }

        public ushort Alignment => _oldTypeReference.Alignment;

        public IEnumerable<IGenericTypeParameter> GenericParameters => _oldTypeReference.GenericParameters;

        public ushort GenericParameterCount => _oldTypeReference.GenericParameterCount;

        public bool HasDeclarativeSecurity => _oldTypeReference.HasDeclarativeSecurity;

        public bool IsAbstract => _oldTypeReference.IsAbstract;

        public bool IsBeforeFieldInit => _oldTypeReference.IsBeforeFieldInit;

        public bool IsComObject => _oldTypeReference.IsComObject;

        public bool IsGeneric => _oldTypeReference.IsGeneric;

        public bool IsInterface => _oldTypeReference.IsInterface;

        public bool IsDelegate => _oldTypeReference.IsDelegate;

        public bool IsRuntimeSpecial => _oldTypeReference.IsRuntimeSpecial;

        public bool IsSerializable => _oldTypeReference.IsSerializable;

        public bool IsSpecialName => _oldTypeReference.IsSpecialName;

        public bool IsWindowsRuntimeImport => _oldTypeReference.IsWindowsRuntimeImport;

        public bool IsSealed => _oldTypeReference.IsSealed;

        public LayoutKind Layout => _oldTypeReference.Layout;

        public IEnumerable<SecurityAttribute> SecurityAttributes => _oldTypeReference.SecurityAttributes;

        public uint SizeOf => _oldTypeReference.SizeOf;

        public CharSet StringFormat => _oldTypeReference.StringFormat;

        public bool IsEnum => _oldTypeReference.IsEnum;

        public bool IsValueType => _oldTypeReference.IsValueType;

        public Cci.PrimitiveTypeCode TypeCode => _oldTypeReference.TypeCode;

        public TypeDefinitionHandle TypeDef => _oldTypeReference.TypeDef;

        public IGenericMethodParameterReference? AsGenericMethodParameterReference => _oldTypeReference.AsGenericMethodParameterReference;

        public IGenericTypeInstanceReference? AsGenericTypeInstanceReference => _oldTypeReference.AsGenericTypeInstanceReference;

        public IGenericTypeParameterReference? AsGenericTypeParameterReference => _oldTypeReference.AsGenericTypeParameterReference;

        public INamespaceTypeReference? AsNamespaceTypeReference => _oldTypeReference.AsNamespaceTypeReference;

        public INestedTypeReference? AsNestedTypeReference => _oldTypeReference.AsNestedTypeReference;

        public ISpecializedNestedTypeReference? AsSpecializedNestedTypeReference => _oldTypeReference.AsSpecializedNestedTypeReference;

        public IDefinition? AsDefinition(EmitContext context)
        {
            return this;
        }

        public INamespaceTypeDefinition? AsNamespaceTypeDefinition(EmitContext context)
        {
            return _oldTypeReference.AsNamespaceTypeDefinition(context);
        }

        public INestedTypeDefinition? AsNestedTypeDefinition(EmitContext context)
        {
            return _oldTypeReference.AsNestedTypeDefinition(context);
        }

        public ITypeDefinition? AsTypeDefinition(EmitContext context)
        {
            return this;
        }

        public void Dispatch(MetadataVisitor visitor)
        {
            _oldTypeReference.Dispatch(visitor);
        }

        public IEnumerable<ICustomAttribute> GetAttributes(EmitContext context)
        {
            return _oldTypeReference.GetAttributes(context);
        }

        public ITypeReference? GetBaseClass(EmitContext context)
        {
            return _oldTypeReference.GetBaseClass(context);
        }

        public IEnumerable<IEventDefinition> GetEvents(EmitContext context)
        {
            return _oldTypeReference.GetEvents(context);
        }

        public IEnumerable<Cci.MethodImplementation> GetExplicitImplementationOverrides(EmitContext context)
        {
            return _oldTypeReference.GetExplicitImplementationOverrides(context);
        }

        public IEnumerable<IFieldDefinition> GetFields(EmitContext context)
        {
            return _oldTypeReference.GetFields(context);
        }

        public ISymbolInternal? GetInternalSymbol()
        {
            return _oldTypeReference.GetInternalSymbol();
        }

        public IEnumerable<IMethodDefinition> GetMethods(EmitContext context)
        {
            return _oldTypeReference.GetMethods(context);
        }

        public IEnumerable<INestedTypeDefinition> GetNestedTypes(EmitContext context)
        {
            return _oldTypeReference.GetNestedTypes(context);
        }

        public IEnumerable<IPropertyDefinition> GetProperties(EmitContext context)
        {
            return _oldTypeReference.GetProperties(context);
        }

        public ITypeDefinition? GetResolvedType(EmitContext context)
        {
            return _oldTypeReference.GetResolvedType(context);
        }

        public IEnumerable<TypeReferenceWithAttributes> Interfaces(EmitContext context)
        {
            return _oldTypeReference.Interfaces(context);
        }
    }
}
