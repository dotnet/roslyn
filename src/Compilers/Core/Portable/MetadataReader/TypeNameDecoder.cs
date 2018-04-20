// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis
{
    internal abstract class TypeNameDecoder<ModuleSymbol, TypeSymbol>
        where ModuleSymbol : class
        where TypeSymbol : class
    {
        private readonly SymbolFactory<ModuleSymbol, TypeSymbol> _factory;
        protected readonly ModuleSymbol moduleSymbol;

        internal TypeNameDecoder(SymbolFactory<ModuleSymbol, TypeSymbol> factory, ModuleSymbol moduleSymbol)
        {
            _factory = factory;
            this.moduleSymbol = moduleSymbol;
        }

        protected abstract bool IsContainingAssembly(AssemblyIdentity identity);

        /// <summary>
        /// Lookup a type defined in this module.
        /// </summary>
        protected abstract TypeSymbol LookupTopLevelTypeDefSymbol(ref MetadataTypeName emittedName, out bool isNoPiaLocalType);

        /// <summary>
        /// Lookup a type defined in referenced assembly.
        /// </summary>
        protected abstract TypeSymbol LookupTopLevelTypeDefSymbol(int referencedAssemblyIndex, ref MetadataTypeName emittedName);
        protected abstract TypeSymbol LookupNestedTypeDefSymbol(TypeSymbol container, ref MetadataTypeName emittedName);

        /// <summary>
        /// Given the identity of an assembly referenced by this module, finds
        /// the index of that assembly in the list of assemblies referenced by
        /// the current module.
        /// </summary>
        protected abstract int GetIndexOfReferencedAssembly(AssemblyIdentity identity);

        internal TypeSymbol GetTypeSymbolForSerializedType(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return GetUnsupportedMetadataTypeSymbol();
            }

            MetadataHelpers.AssemblyQualifiedTypeName fullName = MetadataHelpers.DecodeTypeName(s);
            bool refersToNoPiaLocalType;
            return GetTypeSymbol(fullName, out refersToNoPiaLocalType);
        }

        protected TypeSymbol GetUnsupportedMetadataTypeSymbol(BadImageFormatException exception = null)
        {
            return _factory.GetUnsupportedMetadataTypeSymbol(this.moduleSymbol, exception);
        }

        protected TypeSymbol GetSZArrayTypeSymbol(TypeSymbol elementType, ImmutableArray<ModifierInfo<TypeSymbol>> customModifiers)
        {
            return _factory.GetSZArrayTypeSymbol(this.moduleSymbol, elementType, customModifiers);
        }

        protected TypeSymbol GetMDArrayTypeSymbol(int rank, TypeSymbol elementType, ImmutableArray<ModifierInfo<TypeSymbol>> customModifiers, ImmutableArray<int> sizes, ImmutableArray<int> lowerBounds)
        {
            return _factory.GetMDArrayTypeSymbol(this.moduleSymbol, rank, elementType, customModifiers, sizes, lowerBounds);
        }

        protected TypeSymbol MakePointerTypeSymbol(TypeSymbol type, ImmutableArray<ModifierInfo<TypeSymbol>> customModifiers)
        {
            return _factory.MakePointerTypeSymbol(this.moduleSymbol, type, customModifiers);
        }

        protected TypeSymbol GetSpecialType(SpecialType specialType)
        {
            return _factory.GetSpecialType(this.moduleSymbol, specialType);
        }

        protected TypeSymbol SystemTypeSymbol
        {
            get { return _factory.GetSystemTypeSymbol(this.moduleSymbol); }
        }

        protected TypeSymbol GetEnumUnderlyingType(TypeSymbol type)
        {
            return _factory.GetEnumUnderlyingType(this.moduleSymbol, type);
        }

        protected bool IsAcceptedVolatileModifierType(TypeSymbol type)
        {
            return _factory.IsAcceptedVolatileModifierType(this.moduleSymbol, type);
        }

        protected bool IsAcceptedInAttributeModifierType(TypeSymbol type)
        {
            return _factory.IsAcceptedInAttributeModifierType(type);
        }

        protected bool IsAcceptedUnmanagedTypeModifierType(TypeSymbol type)
        {
            return _factory.IsAcceptedUnmanagedTypeModifierType(type);
        }

        protected Microsoft.Cci.PrimitiveTypeCode GetPrimitiveTypeCode(TypeSymbol type)
        {
            return _factory.GetPrimitiveTypeCode(this.moduleSymbol, type);
        }

        protected TypeSymbol SubstituteWithUnboundIfGeneric(TypeSymbol type)
        {
            return _factory.MakeUnboundIfGeneric(this.moduleSymbol, type);
        }

        protected TypeSymbol SubstituteTypeParameters(TypeSymbol genericType, ImmutableArray<KeyValuePair<TypeSymbol, ImmutableArray<ModifierInfo<TypeSymbol>>>> arguments, ImmutableArray<bool> refersToNoPiaLocalType)
        {
            return _factory.SubstituteTypeParameters(this.moduleSymbol, genericType, arguments, refersToNoPiaLocalType);
        }

        internal TypeSymbol GetTypeSymbol(MetadataHelpers.AssemblyQualifiedTypeName fullName, out bool refersToNoPiaLocalType)
        {
            //
            // Section 23.3 (Custom Attributes) of CLI Spec Partition II:
            //
            // If the parameter kind is System.Type, (also, the middle line in above diagram) its value is 
            // stored as a SerString (as defined in the previous paragraph), representing its canonical name. 
            // The canonical name is its full type name, followed optionally by the assembly where it is defined, 
            // its version, culture and public-key-token. If the assembly name is omitted, the CLI looks first 
            // in the current assembly, and then in the system library (mscorlib); in these two special cases, 
            // it is permitted to omit the assembly-name, version, culture and public-key-token.

            int referencedAssemblyIndex;
            if (fullName.AssemblyName != null)
            {
                AssemblyIdentity identity;
                if (!AssemblyIdentity.TryParseDisplayName(fullName.AssemblyName, out identity))
                {
                    refersToNoPiaLocalType = false;
                    return GetUnsupportedMetadataTypeSymbol();
                }

                // the assembly name has to be a full name:
                referencedAssemblyIndex = GetIndexOfReferencedAssembly(identity);
                if (referencedAssemblyIndex == -1)
                {
                    // In rare cases (e.g. assemblies emitted by Reflection.Emit) the identity 
                    // might be the identity of the containing assembly. The metadata spec doesn't disallow this.
                    if (!this.IsContainingAssembly(identity))
                    {
                        refersToNoPiaLocalType = false;
                        return GetUnsupportedMetadataTypeSymbol();
                    }
                }
            }
            else
            {
                // Use this assembly
                referencedAssemblyIndex = -1;
            }

            // Find the top level type
            Debug.Assert(MetadataHelpers.IsValidMetadataIdentifier(fullName.TopLevelType));
            var mdName = MetadataTypeName.FromFullName(fullName.TopLevelType);
            TypeSymbol container = LookupTopLevelTypeDefSymbol(ref mdName, referencedAssemblyIndex, out refersToNoPiaLocalType);

            // Process any nested types
            if (fullName.NestedTypes != null)
            {
                if (refersToNoPiaLocalType)
                {
                    // Types nested into local types are not supported.
                    refersToNoPiaLocalType = false;
                    return GetUnsupportedMetadataTypeSymbol();
                }

                for (int i = 0; i < fullName.NestedTypes.Length; i++)
                {
                    Debug.Assert(MetadataHelpers.IsValidMetadataIdentifier(fullName.NestedTypes[i]));
                    mdName = MetadataTypeName.FromTypeName(fullName.NestedTypes[i]);
                    // Find nested type in the container
                    container = LookupNestedTypeDefSymbol(container, ref mdName);
                }
            }

            //  Substitute type arguments if any
            if (fullName.TypeArguments != null)
            {
                ImmutableArray<bool> argumentRefersToNoPiaLocalType;
                var typeArguments = ResolveTypeArguments(fullName.TypeArguments, out argumentRefersToNoPiaLocalType);
                container = SubstituteTypeParameters(container, typeArguments, argumentRefersToNoPiaLocalType);

                foreach (bool flag in argumentRefersToNoPiaLocalType)
                {
                    if (flag)
                    {
                        refersToNoPiaLocalType = true;
                        break;
                    }
                }
            }
            else
            {
                container = SubstituteWithUnboundIfGeneric(container);
            }

            for (int i = 0; i < fullName.PointerCount; i++)
            {
                container = MakePointerTypeSymbol(container, ImmutableArray<ModifierInfo<TypeSymbol>>.Empty);
            }

            // Process any array type ranks
            if (fullName.ArrayRanks != null)
            {
                foreach (int rank in fullName.ArrayRanks)
                {
                    Debug.Assert(rank >= 0);
                    container = rank == 0 ?
                                GetSZArrayTypeSymbol(container, default(ImmutableArray<ModifierInfo<TypeSymbol>>)) :
                                GetMDArrayTypeSymbol(rank, container, default(ImmutableArray<ModifierInfo<TypeSymbol>>), ImmutableArray<int>.Empty, default(ImmutableArray<int>));
                }
            }

            return container;
        }

        private ImmutableArray<KeyValuePair<TypeSymbol, ImmutableArray<ModifierInfo<TypeSymbol>>>> ResolveTypeArguments(MetadataHelpers.AssemblyQualifiedTypeName[] arguments, out ImmutableArray<bool> refersToNoPiaLocalType)
        {
            int count = arguments.Length;
            var typeArgumentsBuilder = ArrayBuilder<KeyValuePair<TypeSymbol, ImmutableArray<ModifierInfo<TypeSymbol>>>>.GetInstance(count);
            var refersToNoPiaBuilder = ArrayBuilder<bool>.GetInstance(count);

            foreach (var argument in arguments)
            {
                bool refersToNoPia;
                typeArgumentsBuilder.Add(new KeyValuePair<TypeSymbol, ImmutableArray<ModifierInfo<TypeSymbol>>>(GetTypeSymbol(argument, out refersToNoPia), ImmutableArray<ModifierInfo<TypeSymbol>>.Empty));
                refersToNoPiaBuilder.Add(refersToNoPia);
            }

            refersToNoPiaLocalType = refersToNoPiaBuilder.ToImmutableAndFree();
            return typeArgumentsBuilder.ToImmutableAndFree();
        }

        private TypeSymbol LookupTopLevelTypeDefSymbol(ref MetadataTypeName emittedName, int referencedAssemblyIndex, out bool isNoPiaLocalType)
        {
            TypeSymbol container;

            if (referencedAssemblyIndex >= 0)
            {
                // Find  top level type in referenced assembly
                isNoPiaLocalType = false;
                container = LookupTopLevelTypeDefSymbol(referencedAssemblyIndex, ref emittedName);
            }
            else
            {
                // TODO : lookup in mscorlib
                // Find top level type in this assembly or mscorlib:
                container = LookupTopLevelTypeDefSymbol(ref emittedName, out isNoPiaLocalType);
            }

            return container;
        }
    }
}
