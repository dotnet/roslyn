// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection.Metadata;
using Roslyn.Utilities;

namespace Microsoft.Cci
{
    /// <summary>
    /// Special type &lt;Module&gt;
    /// </summary>
    internal class RootModuleType : INamespaceTypeDefinition
    {
        public TypeHandle TypeDef
        {
            get { return default(TypeHandle); }
        }

        public ITypeDefinition ResolvedType
        {
            get { return this; }
        }

        public IEnumerable<ICustomAttribute> GetAttributes(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return SpecializedCollections.EmptyEnumerable<ICustomAttribute>();
        }

        public bool MangleName
        {
            get { return false; }
        }

        public string Name
        {
            get { return "<Module>"; }
        }

        public ushort Alignment
        {
            get { return 0; }
        }

        public ITypeReference GetBaseClass(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return null;
        }

        public IEnumerable<IEventDefinition> Events
        {
            get { return SpecializedCollections.EmptyEnumerable<IEventDefinition>(); }
        }

        public IEnumerable<IMethodImplementation> GetExplicitImplementationOverrides(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return SpecializedCollections.EmptyEnumerable<IMethodImplementation>();
        }

        public IEnumerable<IFieldDefinition> GetFields(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return SpecializedCollections.EmptyEnumerable<IFieldDefinition>();
        }

        public bool HasDeclarativeSecurity
        {
            get { return false; }
        }

        public IEnumerable<ITypeReference> Interfaces(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return SpecializedCollections.EmptyEnumerable<ITypeReference>();
        }

        public bool IsAbstract
        {
            get { return false; }
        }

        public bool IsBeforeFieldInit
        {
            get { return false; }
        }

        public bool IsComObject
        {
            get { return false; }
        }

        public bool IsGeneric
        {
            get { return false; }
        }

        public bool IsInterface
        {
            get { return false; }
        }

        public bool IsRuntimeSpecial
        {
            get { return false; }
        }

        public bool IsSerializable
        {
            get { return false; }
        }

        public bool IsSpecialName
        {
            get { return false; }
        }

        public bool IsWindowsRuntimeImport
        {
            get { return false; }
        }

        public bool IsSealed
        {
            get { return false; }
        }

        public LayoutKind Layout
        {
            get { return LayoutKind.Auto; }
        }

        public IEnumerable<IMethodDefinition> GetMethods(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return SpecializedCollections.EmptyEnumerable<IMethodDefinition>();
        }

        public IEnumerable<INestedTypeDefinition> GetNestedTypes(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return SpecializedCollections.EmptyEnumerable<INestedTypeDefinition>();
        }

        public IEnumerable<IPropertyDefinition> GetProperties(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return SpecializedCollections.EmptyEnumerable<IPropertyDefinition>();
        }

        public uint SizeOf
        {
            get { return 0; }
        }

        public CharSet StringFormat
        {
            get { return CharSet.Ansi; }
        }

        public bool IsPublic
        {
            get { return false; }
        }

        public bool IsNested
        {
            get { return false; }
        }

        IEnumerable<IGenericTypeParameter> ITypeDefinition.GenericParameters
        {
            get { throw ExceptionUtilities.Unreachable; }
        }

        ushort ITypeDefinition.GenericParameterCount
        {
            get
            {
                return 0;
            }
        }

        IEnumerable<SecurityAttribute> ITypeDefinition.SecurityAttributes
        {
            get { throw ExceptionUtilities.Unreachable; }
        }

        void IReference.Dispatch(MetadataVisitor visitor)
        {
            throw ExceptionUtilities.Unreachable;
        }

        bool ITypeReference.IsEnum
        {
            get { throw ExceptionUtilities.Unreachable; }
        }

        bool ITypeReference.IsValueType
        {
            get { throw ExceptionUtilities.Unreachable; }
        }

        ITypeDefinition ITypeReference.GetResolvedType(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return this;
        }

        PrimitiveTypeCode ITypeReference.TypeCode(Microsoft.CodeAnalysis.Emit.Context context)
        {
            throw ExceptionUtilities.Unreachable;
        }

        ushort INamedTypeReference.GenericParameterCount
        {
            get { throw ExceptionUtilities.Unreachable; }
        }

        IUnitReference INamespaceTypeReference.GetUnit(Microsoft.CodeAnalysis.Emit.Context context)
        {
            throw ExceptionUtilities.Unreachable;
        }

        string INamespaceTypeReference.NamespaceName
        {
            get
            {
                return string.Empty;
            }
        }

        IGenericMethodParameterReference ITypeReference.AsGenericMethodParameterReference
        {
            get
            {
                return null;
            }
        }

        IGenericTypeInstanceReference ITypeReference.AsGenericTypeInstanceReference
        {
            get
            {
                return null;
            }
        }

        IGenericTypeParameterReference ITypeReference.AsGenericTypeParameterReference
        {
            get
            {
                return null;
            }
        }

        INamespaceTypeDefinition ITypeReference.AsNamespaceTypeDefinition(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return this;
        }

        INamespaceTypeReference ITypeReference.AsNamespaceTypeReference
        {
            get
            {
                return this;
            }
        }

        INestedTypeDefinition ITypeReference.AsNestedTypeDefinition(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return null;
        }

        INestedTypeReference ITypeReference.AsNestedTypeReference
        {
            get
            {
                return null;
            }
        }

        ISpecializedNestedTypeReference ITypeReference.AsSpecializedNestedTypeReference
        {
            get
            {
                return null;
            }
        }

        ITypeDefinition ITypeReference.AsTypeDefinition(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return this;
        }

        IDefinition IReference.AsDefinition(Microsoft.CodeAnalysis.Emit.Context context)
        {
            return this;
        }
    }
}
