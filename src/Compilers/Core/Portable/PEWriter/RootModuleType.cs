// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;

namespace Microsoft.Cci
{
    /// <summary>
    /// Special type &lt;Module&gt;
    /// </summary>
    internal class RootModuleType : INamespaceTypeDefinition
    {
        public TypeDefinitionHandle TypeDef => default;

        public ITypeDefinition ResolvedType => this;

        public IEnumerable<ICustomAttribute> GetAttributes(EmitContext context) => SpecializedCollections.EmptyEnumerable<ICustomAttribute>();

        public bool MangleName => false;

        public string Name => "<Module>";

        public ushort Alignment => 0;

        public ITypeReference? GetBaseClass(EmitContext context) => null;

        public IEnumerable<IEventDefinition> GetEvents(EmitContext context) => SpecializedCollections.EmptyEnumerable<IEventDefinition>();

        public IEnumerable<MethodImplementation> GetExplicitImplementationOverrides(EmitContext context) => SpecializedCollections.EmptyEnumerable<MethodImplementation>();

        public IEnumerable<IFieldDefinition> GetFields(EmitContext context) => SpecializedCollections.EmptyEnumerable<IFieldDefinition>();

        public bool HasDeclarativeSecurity => false;

        public IEnumerable<TypeReferenceWithAttributes> Interfaces(EmitContext context) => SpecializedCollections.EmptyEnumerable<TypeReferenceWithAttributes>();

        public bool IsAbstract => false;

        public bool IsBeforeFieldInit => false;

        public bool IsComObject => false;

        public bool IsGeneric => false;

        public bool IsInterface => false;

        public bool IsDelegate => false;

        public bool IsRuntimeSpecial => false;

        public bool IsSerializable => false;

        public bool IsSpecialName => false;

        public bool IsWindowsRuntimeImport => false;

        public bool IsSealed => false;

        public LayoutKind Layout => LayoutKind.Auto;

        public IEnumerable<IMethodDefinition> GetMethods(EmitContext context) => SpecializedCollections.EmptyEnumerable<IMethodDefinition>();

        public IEnumerable<INestedTypeDefinition> GetNestedTypes(EmitContext context) => SpecializedCollections.EmptyEnumerable<INestedTypeDefinition>();

        public IEnumerable<IPropertyDefinition> GetProperties(EmitContext context) => SpecializedCollections.EmptyEnumerable<IPropertyDefinition>();

        public uint SizeOf => 0;

        public CharSet StringFormat => CharSet.Ansi;

        public bool IsPublic => false;

        public bool IsNested => false;

        IEnumerable<IGenericTypeParameter> ITypeDefinition.GenericParameters => throw ExceptionUtilities.Unreachable;

        ushort ITypeDefinition.GenericParameterCount => 0;

        IEnumerable<SecurityAttribute> ITypeDefinition.SecurityAttributes => throw ExceptionUtilities.Unreachable;

        void IReference.Dispatch(MetadataVisitor visitor) => throw ExceptionUtilities.Unreachable;

        bool ITypeReference.IsEnum => throw ExceptionUtilities.Unreachable;

        bool ITypeReference.IsValueType => throw ExceptionUtilities.Unreachable;

        ITypeDefinition ITypeReference.GetResolvedType(EmitContext context) => this;

        PrimitiveTypeCode ITypeReference.TypeCode => throw ExceptionUtilities.Unreachable;

        ushort INamedTypeReference.GenericParameterCount => throw ExceptionUtilities.Unreachable;

        IUnitReference INamespaceTypeReference.GetUnit(EmitContext context) => throw ExceptionUtilities.Unreachable;

        string INamespaceTypeReference.NamespaceName => string.Empty;

        IGenericMethodParameterReference? ITypeReference.AsGenericMethodParameterReference => null;

        IGenericTypeInstanceReference? ITypeReference.AsGenericTypeInstanceReference => null;

        IGenericTypeParameterReference? ITypeReference.AsGenericTypeParameterReference => null;

        INamespaceTypeDefinition ITypeReference.AsNamespaceTypeDefinition(EmitContext context) => this;

        INamespaceTypeReference ITypeReference.AsNamespaceTypeReference => this;

        INestedTypeDefinition? ITypeReference.AsNestedTypeDefinition(EmitContext context) => null;

        INestedTypeReference? ITypeReference.AsNestedTypeReference => null;

        ISpecializedNestedTypeReference? ITypeReference.AsSpecializedNestedTypeReference => null;

        ITypeDefinition ITypeReference.AsTypeDefinition(EmitContext context) => this;

        IDefinition IReference.AsDefinition(EmitContext context) => this;
    }
}
