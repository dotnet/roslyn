// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Emit
{
    /// <summary>
    /// Error type symbols should be replaced with an object of this class 
    /// in the translation layer for emit.
    /// </summary>
    internal class ErrorType : Cci.INamespaceTypeReference
    {
        public static readonly ErrorType Singleton = new ErrorType();

        /// <summary>
        /// For the name we will use a word "Error" followed by a guid, generated on the spot.
        /// </summary>
        private static readonly string s_name = "Error" + Guid.NewGuid().ToString("B");

        Cci.IUnitReference Cci.INamespaceTypeReference.GetUnit(EmitContext context)
        {
            return ErrorAssembly.Singleton;
        }

        string Cci.INamespaceTypeReference.NamespaceName
        {
            get
            {
                return "";
            }
        }

        ushort Cci.INamedTypeReference.GenericParameterCount
        {
            get
            {
                return 0;
            }
        }

        bool Cci.INamedTypeReference.MangleName
        {
            get
            {
                return false;
            }
        }

#nullable enable
        string? Cci.INamedTypeReference.AssociatedFileIdentifier
        {
            get
            {
                return null;
            }
        }
#nullable disable

        bool Cci.ITypeReference.IsEnum
        {
            get
            {
                return false;
            }
        }

        bool Cci.ITypeReference.IsValueType
        {
            get
            {
                return false;
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

        Cci.IGenericTypeInstanceReference Cci.ITypeReference.AsGenericTypeInstanceReference
        {
            get
            {
                return null;
            }
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

        Cci.INamespaceTypeReference Cci.ITypeReference.AsNamespaceTypeReference
        {
            get
            {
                return this;
            }
        }

        Cci.INestedTypeDefinition Cci.ITypeReference.AsNestedTypeDefinition(EmitContext context)
        {
            return null;
        }

        Cci.INestedTypeReference Cci.ITypeReference.AsNestedTypeReference
        {
            get
            {
                return null;
            }
        }

        Cci.ISpecializedNestedTypeReference Cci.ITypeReference.AsSpecializedNestedTypeReference
        {
            get
            {
                return null;
            }
        }

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

        Symbols.ISymbolInternal Cci.IReference.GetInternalSymbol() => null;

        string Cci.INamedEntity.Name
        {
            get
            {
                return s_name;
            }
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

        /// <summary>
        /// A fake containing assembly for an ErrorType object.
        /// </summary>
        private sealed class ErrorAssembly : Cci.IAssemblyReference
        {
            public static readonly ErrorAssembly Singleton = new ErrorAssembly();

            /// <summary>
            /// For the name we will use a word "Error" followed by a guid, generated on the spot.
            /// </summary>
            private static readonly AssemblyIdentity s_identity = new AssemblyIdentity(
                name: "Error" + Guid.NewGuid().ToString("B"),
                version: AssemblyIdentity.NullVersion,
                cultureName: "",
                publicKeyOrToken: ImmutableArray<byte>.Empty,
                hasPublicKey: false,
                isRetargetable: false,
                contentType: AssemblyContentType.Default);

            AssemblyIdentity Cci.IAssemblyReference.Identity => s_identity;
            Version Cci.IAssemblyReference.AssemblyVersionPattern => null;

            Cci.IAssemblyReference Cci.IModuleReference.GetContainingAssembly(EmitContext context)
            {
                return this;
            }

            IEnumerable<Cci.ICustomAttribute> Cci.IReference.GetAttributes(EmitContext context)
            {
                return SpecializedCollections.EmptyEnumerable<Cci.ICustomAttribute>();
            }

            void Cci.IReference.Dispatch(Cci.MetadataVisitor visitor)
            {
                visitor.Visit((Cci.IAssemblyReference)this);
            }

            Cci.IDefinition Cci.IReference.AsDefinition(EmitContext context)
            {
                return null;
            }

            Symbols.ISymbolInternal Cci.IReference.GetInternalSymbol() => null;

            string Cci.INamedEntity.Name => s_identity.Name;
        }
    }
}
