// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Cci;
using Microsoft.CodeAnalysis.Emit;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests
{
    internal sealed class NamespaceTypeDefinitionNoBase : INamespaceTypeDefinition
    {
        internal readonly INamespaceTypeDefinition UnderlyingType;

        internal NamespaceTypeDefinitionNoBase(INamespaceTypeDefinition underlyingType)
        {
            UnderlyingType = underlyingType;
        }

        ushort ITypeDefinition.Alignment => UnderlyingType.Alignment;

        IGenericMethodParameterReference ITypeReference.AsGenericMethodParameterReference => UnderlyingType.AsGenericMethodParameterReference;

        IGenericTypeInstanceReference ITypeReference.AsGenericTypeInstanceReference => UnderlyingType.AsGenericTypeInstanceReference;

        IGenericTypeParameterReference ITypeReference.AsGenericTypeParameterReference => UnderlyingType.AsGenericTypeParameterReference;

        INamespaceTypeReference ITypeReference.AsNamespaceTypeReference => UnderlyingType.AsNamespaceTypeReference;

        INestedTypeReference ITypeReference.AsNestedTypeReference => UnderlyingType.AsNestedTypeReference;

        ISpecializedNestedTypeReference ITypeReference.AsSpecializedNestedTypeReference => UnderlyingType.AsSpecializedNestedTypeReference;

        IEnumerable<IEventDefinition> ITypeDefinition.GetEvents(EmitContext context) => UnderlyingType.GetEvents(context);

        ushort INamedTypeReference.GenericParameterCount => 0;

        ushort ITypeDefinition.GenericParameterCount => 0;

        IEnumerable<IGenericTypeParameter> ITypeDefinition.GenericParameters => UnderlyingType.GenericParameters;

        bool ITypeDefinition.HasDeclarativeSecurity => UnderlyingType.HasDeclarativeSecurity;

        bool ITypeDefinition.IsAbstract => UnderlyingType.IsAbstract;

        bool ITypeDefinition.IsBeforeFieldInit => UnderlyingType.IsBeforeFieldInit;

        bool ITypeDefinition.IsComObject => UnderlyingType.IsComObject;

        bool ITypeReference.IsEnum => UnderlyingType.IsEnum;

        bool ITypeDefinition.IsGeneric => UnderlyingType.IsGeneric;

        bool ITypeDefinition.IsInterface => UnderlyingType.IsInterface;

        bool ITypeDefinition.IsDelegate => UnderlyingType.IsDelegate;

        bool INamespaceTypeDefinition.IsPublic => UnderlyingType.IsPublic;

        bool ITypeDefinition.IsRuntimeSpecial => UnderlyingType.IsRuntimeSpecial;

        bool ITypeDefinition.IsSealed => UnderlyingType.IsSealed;

        bool ITypeDefinition.IsSerializable => UnderlyingType.IsSerializable;

        bool ITypeDefinition.IsSpecialName => UnderlyingType.IsSpecialName;

        bool ITypeReference.IsValueType => UnderlyingType.IsValueType;

        bool ITypeDefinition.IsWindowsRuntimeImport => UnderlyingType.IsWindowsRuntimeImport;

        LayoutKind ITypeDefinition.Layout => UnderlyingType.Layout;

        bool INamedTypeReference.MangleName => UnderlyingType.MangleName;

        string INamedEntity.Name => UnderlyingType.Name;

        string INamespaceTypeReference.NamespaceName => UnderlyingType.NamespaceName;

        IEnumerable<SecurityAttribute> ITypeDefinition.SecurityAttributes => UnderlyingType.SecurityAttributes;

        uint ITypeDefinition.SizeOf => UnderlyingType.SizeOf;

        CharSet ITypeDefinition.StringFormat => UnderlyingType.StringFormat;

        TypeDefinitionHandle ITypeReference.TypeDef => UnderlyingType.TypeDef;

        IDefinition IReference.AsDefinition(EmitContext context) => UnderlyingType.AsDefinition(context);

        INamespaceTypeDefinition ITypeReference.AsNamespaceTypeDefinition(EmitContext context) => UnderlyingType.AsNamespaceTypeDefinition(context);

        INestedTypeDefinition ITypeReference.AsNestedTypeDefinition(EmitContext context) => UnderlyingType.AsNestedTypeDefinition(context);

        ITypeDefinition ITypeReference.AsTypeDefinition(EmitContext context) => UnderlyingType.AsTypeDefinition(context);

        void IReference.Dispatch(MetadataVisitor visitor) => UnderlyingType.Dispatch(visitor);

        IEnumerable<ICustomAttribute> IReference.GetAttributes(EmitContext context) => UnderlyingType.GetAttributes(context);

        ITypeReference ITypeDefinition.GetBaseClass(EmitContext context) => null;

        IEnumerable<Cci.MethodImplementation> ITypeDefinition.GetExplicitImplementationOverrides(EmitContext context) => UnderlyingType.GetExplicitImplementationOverrides(context);

        IEnumerable<IFieldDefinition> ITypeDefinition.GetFields(EmitContext context) => UnderlyingType.GetFields(context);

        IEnumerable<IMethodDefinition> ITypeDefinition.GetMethods(EmitContext context) => UnderlyingType.GetMethods(context);

        IEnumerable<INestedTypeDefinition> ITypeDefinition.GetNestedTypes(EmitContext context) => UnderlyingType.GetNestedTypes(context);

        IEnumerable<IPropertyDefinition> ITypeDefinition.GetProperties(EmitContext context) => UnderlyingType.GetProperties(context);

        ITypeDefinition ITypeReference.GetResolvedType(EmitContext context) => UnderlyingType.GetResolvedType(context);

        IUnitReference INamespaceTypeReference.GetUnit(EmitContext context) => UnderlyingType.GetUnit(context);

        IEnumerable<TypeReferenceWithAttributes> ITypeDefinition.Interfaces(EmitContext context) => UnderlyingType.Interfaces(context);

        Cci.PrimitiveTypeCode ITypeReference.TypeCode => UnderlyingType.TypeCode;
    }
}
