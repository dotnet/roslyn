// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE
{
    /// <summary>
    /// Helper class to resolve metadata tokens and signatures.
    /// </summary>
    internal class MetadataDecoder : MetadataDecoder<TypeSymbol, MethodSymbol, FieldSymbol, AssemblySymbol, Symbol>
    {
        /// <summary>
        /// ModuleSymbol for the module - source of metadata.
        /// </summary>
        private readonly PEModuleSymbol moduleSymbol;

        /// <summary>
        /// Type context for resolving generic type arguments.
        /// </summary>
        private readonly PENamedTypeSymbol typeContextOpt;

        /// <summary>
        /// Method context for resolving generic method type arguments.
        /// </summary>
        private readonly PEMethodSymbol methodContextOpt;

        public MetadataDecoder(
            PEModuleSymbol moduleSymbol,
            PENamedTypeSymbol context) :
            this(moduleSymbol, context, null)
        {
        }

        public MetadataDecoder(
            PEModuleSymbol moduleSymbol,
            PEMethodSymbol context) :
            this(moduleSymbol, (PENamedTypeSymbol)context.ContainingType, context)
        {
        }

        public MetadataDecoder(
            PEModuleSymbol moduleSymbol) :
            this(moduleSymbol, null, null)
        {
        }

        private MetadataDecoder(PEModuleSymbol moduleSymbol, PENamedTypeSymbol typeContextOpt, PEMethodSymbol methodContextOpt)
            // TODO (tomat): if the containing assembly is a source assembly and we are about to decode assembly level attributes, we run into a cycle,
            // so for now ignore the assembly identity.
            : base(moduleSymbol.Module, (moduleSymbol.ContainingAssembly is PEAssemblySymbol) ? moduleSymbol.ContainingAssembly.Identity : null)
        {
            Debug.Assert((object)moduleSymbol != null);

            this.moduleSymbol = moduleSymbol;
            this.typeContextOpt = typeContextOpt;
            this.methodContextOpt = methodContextOpt;
        }

        internal PENamedTypeSymbol TypeContext
        {
            get { return typeContextOpt; }
        }

        internal PEModuleSymbol ModuleSymbol
        {
            get { return moduleSymbol; }
        }

        protected override TypeSymbol GetArrayTypeSymbol(int dims, TypeSymbol elementType)
        {
            if (elementType is UnsupportedMetadataTypeSymbol)
            {
                return elementType;
            }

            if (dims == 1)
            {
                // We do not support multi-dimensional arrays of rank 1, cannot distinguish
                // them from SZARRAY.
                // TODO(ngafter): what is the correct diagnostic for this situation?
                return new UnsupportedMetadataTypeSymbol(); // Found a multi-dimensional array of rank 1 in metadata
            }

            return new ArrayTypeSymbol(moduleSymbol.ContainingAssembly, elementType, ImmutableArray<CustomModifier>.Empty, dims);
        }

        protected override TypeSymbol GetSpecialType(SpecialType specialType)
        {
            return moduleSymbol.ContainingAssembly.GetSpecialType(specialType);
        }

        protected override TypeSymbol SystemTypeSymbol
        {
            get
            {
                return moduleSymbol.SystemTypeSymbol;
            }
        }

        protected override TypeSymbol GetEnumUnderlyingType(TypeSymbol type)
        {
            return type.GetEnumUnderlyingType();
        }

        protected override Cci.PrimitiveTypeCode GetPrimitiveTypeCode(TypeSymbol type)
        {
            return type.PrimitiveTypeCode;
        }

        protected override bool IsVolatileModifierType(TypeSymbol type)
        {
            return type.SpecialType == SpecialType.System_Runtime_CompilerServices_IsVolatile;
        }

        protected override TypeSymbol GetGenericMethodTypeParamSymbol(int position)
        {
            if ((object)methodContextOpt == null)
            {
                return new UnsupportedMetadataTypeSymbol(); // type parameter not associated with a method
            }

            var typeParameters = methodContextOpt.TypeParameters;

            if (typeParameters.Length <= position)
            {
                return new UnsupportedMetadataTypeSymbol(); // type parameter position too large
            }

            return typeParameters[position];
        }

        protected override TypeSymbol GetGenericTypeParamSymbol(int position)
        {
            PENamedTypeSymbol type = typeContextOpt;

            while ((object)type != null && (type.MetadataArity - type.Arity) > position)
            {
                type = type.ContainingSymbol as PENamedTypeSymbol;
            }

            if ((object)type == null || type.MetadataArity <= position)
            {
                return new UnsupportedMetadataTypeSymbol(); // position of type parameter too large
            }

            position -= type.MetadataArity - type.Arity;
            Debug.Assert(position >= 0 && position < type.Arity);

            return type.TypeParameters[position];
        }

        protected override TypeSymbol GetSZArrayTypeSymbol(TypeSymbol elementType, ImmutableArray<ModifierInfo> customModifiers)
        {
            if (elementType is UnsupportedMetadataTypeSymbol)
            {
                return elementType;
            }

            return new ArrayTypeSymbol(moduleSymbol.ContainingAssembly, elementType, CSharpCustomModifier.Convert(customModifiers));
        }

        protected override ConcurrentDictionary<TypeHandle, TypeSymbol> GetTypeHandleToTypeMap()
        {
            return moduleSymbol.TypeHandleToTypeMap;
        }

        protected override ConcurrentDictionary<TypeReferenceHandle, TypeSymbol> GetTypeRefHandleToTypeMap()
        {
            return moduleSymbol.TypeRefHandleToTypeMap;
        }

        protected override TypeSymbol GetUnsupportedMetadataTypeSymbol(BadImageFormatException mrEx = null)
        {
            return new UnsupportedMetadataTypeSymbol(mrEx);
        }

        protected override TypeSymbol GetByRefReturnTypeSymbol(TypeSymbol referencedType)
        {
            return new ByRefReturnErrorTypeSymbol(referencedType);
        }

        protected override TypeSymbol LookupNestedTypeDefSymbol(TypeSymbol container, ref MetadataTypeName emittedName)
        {
            var result = container.LookupMetadataType(ref emittedName);
            Debug.Assert((object)result != null);

            return result;
        }

        /// <summary>
        /// Lookup a type defined in referenced assembly.
        /// </summary>
        /// <param name="referencedAssemblyIndex"></param>
        /// <param name="emittedName"></param>
        protected override TypeSymbol LookupTopLevelTypeDefSymbol(
            int referencedAssemblyIndex,
            ref MetadataTypeName emittedName)
        {
            AssemblySymbol assembly = moduleSymbol.GetReferencedAssemblySymbols()[referencedAssemblyIndex];
            return assembly.LookupTopLevelMetadataType(ref emittedName, digThroughForwardedTypes: true);
        }

        /// <summary>
        /// Lookup a type defined in a module of a multi-module assembly.
        /// </summary>
        protected override TypeSymbol LookupTopLevelTypeDefSymbol(string moduleName, ref MetadataTypeName emittedName, out bool isNoPiaLocalType)
        {
            foreach (ModuleSymbol m in moduleSymbol.ContainingAssembly.Modules)
            {
                if (string.Equals(m.Name, moduleName, StringComparison.OrdinalIgnoreCase))
                {
                    if ((object)m == (object)moduleSymbol)
                    {
                        return moduleSymbol.LookupTopLevelMetadataType(ref emittedName, out isNoPiaLocalType);
                    }
                    else
                    {
                        isNoPiaLocalType = false;
                        return m.LookupTopLevelMetadataType(ref emittedName);
                    }
                }
            }

            isNoPiaLocalType = false;
            return new MissingMetadataTypeSymbol.TopLevel(new MissingModuleSymbolWithName(moduleSymbol.ContainingAssembly, moduleName), ref emittedName, SpecialType.None);
        }

        /// <summary>
        /// Lookup a type defined in this module.
        /// This method will be called only if the type we are
        /// looking for hasn't been loaded yet. Otherwise, MetadataDecoder
        /// would have found the type in TypeDefRowIdToTypeMap based on its 
        /// TypeDef row id. 
        /// </summary>
        protected override TypeSymbol LookupTopLevelTypeDefSymbol(ref MetadataTypeName emittedName, out bool isNoPiaLocalType)
        {
            return moduleSymbol.LookupTopLevelMetadataType(ref emittedName, out isNoPiaLocalType);
        }

        protected override TypeSymbol MakePointerTypeSymbol(TypeSymbol type, ImmutableArray<ModifierInfo> customModifiers)
        {
            if (type is UnsupportedMetadataTypeSymbol)
            {
                return type;
            }

            return new PointerTypeSymbol(type, CSharpCustomModifier.Convert(customModifiers));
        }

        /// <summary>
        /// Produce unbound generic type symbol if the type is a generic type.
        /// </summary>
        /// <param name="type">
        /// Symbol for type.
        /// </param>
        protected override TypeSymbol SubstituteWithUnboundIfGeneric(TypeSymbol type)
        {
            var namedType = type as NamedTypeSymbol;
            return ((object)namedType != null && namedType.IsGenericType) ? namedType.AsUnboundGenericType() : type;
        }

        /// <summary>
        /// Produce constructed type symbol.
        /// </summary>
        /// <param name="genericTypeDef">
        /// Symbol for generic type.
        /// </param>
        /// <param name="arguments">
        /// Generic type arguments, including those for nesting types.
        /// </param>
        /// <param name="refersToNoPiaLocalType">
        /// Flags for arguments. Each item indicates whether corresponding argument refers to NoPia local types.
        /// </param>
        /// <returns></returns>
        /// <remarks></remarks>
        protected override TypeSymbol SubstituteTypeParameters(
            TypeSymbol genericTypeDef,
            TypeSymbol[] arguments,
            bool[] refersToNoPiaLocalType)
        {
            if (genericTypeDef is UnsupportedMetadataTypeSymbol)
            {
                return genericTypeDef;
            }
            else
            {
                // Let's return unsupported metadata type if any argument is unsupported metadata type 
                foreach (var arg in arguments)
                {
                    if (arg.Kind == SymbolKind.ErrorType &&
                        arg is UnsupportedMetadataTypeSymbol)
                    {
                        return new UnsupportedMetadataTypeSymbol();
                    }
                }

                NamedTypeSymbol genericType = (NamedTypeSymbol)genericTypeDef;

                // See if it is or its enclosing type is a non-interface closed over NoPia local types. 
                ImmutableArray<AssemblySymbol> linkedAssemblies = moduleSymbol.ContainingAssembly.GetLinkedReferencedAssemblies();

                bool noPiaIllegalGenericInstantiation = false;

                if (!linkedAssemblies.IsDefaultOrEmpty || Module.ContainsNoPiaLocalTypes())
                {
                    NamedTypeSymbol typeToCheck = genericType;
                    int argumentIndex = refersToNoPiaLocalType.Length - 1;

                    do
                    {
                        if (!typeToCheck.IsInterface)
                        {
                            break;
                        }
                        else
                        {
                            argumentIndex -= typeToCheck.Arity;
                        }

                        typeToCheck = typeToCheck.ContainingType;
                    }
                    while ((object)typeToCheck != null);

                    for (int i = argumentIndex; i >= 0; i--)
                    {
                        if (refersToNoPiaLocalType[i] ||
                            (!linkedAssemblies.IsDefaultOrEmpty &&
                            IsOrClosedOverATypeFromAssemblies(arguments[i], linkedAssemblies)))
                        {
                            noPiaIllegalGenericInstantiation = true;
                            break;
                        }
                    }
                }

                // Collect generic parameters for the type and its containers in the order
                // that matches passed in arguments, i.e. sorted by the nesting.
                ImmutableArray<TypeParameterSymbol> typeParameters = genericType.GetAllTypeParameters();
                Debug.Assert(typeParameters.Length > 0);

                if (typeParameters.Length != arguments.Length)
                {
                    return new UnsupportedMetadataTypeSymbol();
                }

                TypeMap substitution = new TypeMap(typeParameters, ImmutableArray.Create(arguments));

                NamedTypeSymbol constructedType = substitution.SubstituteNamedType(genericType);

                if (noPiaIllegalGenericInstantiation)
                {
                    constructedType = new NoPiaIllegalGenericInstantiationSymbol(moduleSymbol, constructedType);
                }

                return constructedType;
            }
        }

        /// <summary>
        /// Perform a check whether the type or at least one of its generic arguments 
        /// is defined in the specified assemblies. The check is performed recursively. 
        /// </summary>
        public static bool IsOrClosedOverATypeFromAssemblies(TypeSymbol symbol, ImmutableArray<AssemblySymbol> assemblies)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.TypeParameter:
                    return false;

                case SymbolKind.ArrayType:
                    return IsOrClosedOverATypeFromAssemblies(((ArrayTypeSymbol)symbol).ElementType, assemblies);

                case SymbolKind.PointerType:
                    return IsOrClosedOverATypeFromAssemblies(((PointerTypeSymbol)symbol).PointedAtType, assemblies);

                case SymbolKind.DynamicType:
                    return false;

                case SymbolKind.ErrorType:
                    goto case SymbolKind.NamedType;
                case SymbolKind.NamedType:

                    var namedType = (NamedTypeSymbol)symbol;
                    AssemblySymbol containingAssembly = symbol.OriginalDefinition.ContainingAssembly;
                    int i;

                    if ((object)containingAssembly != null)
                    {
                        for (i = 0; i < assemblies.Length; i++)
                        {
                            if (ReferenceEquals(containingAssembly, assemblies[i]))
                            {
                                return true;
                            }
                        }
                    }

                    do
                    {
                        var arguments = namedType.TypeArgumentsNoUseSiteDiagnostics;
                        int count = arguments.Length;

                        for (i = 0; i < count; i++)
                        {
                            if (IsOrClosedOverATypeFromAssemblies(arguments[i], assemblies))
                            {
                                return true;
                            }
                        }

                        namedType = namedType.ContainingType;
                    }
                    while ((object)namedType != null);

                    return false;

                default:
                    throw ExceptionUtilities.UnexpectedValue(symbol.Kind);
            }
        }

        protected override TypeSymbol SubstituteNoPiaLocalType(
            TypeHandle typeDef,
            ref MetadataTypeName name,
            string interfaceGuid,
            string scope,
            string identifier)
        {
            ImmutableArray<AssemblySymbol> lookupIn;

            lookupIn = moduleSymbol.ContainingAssembly.GetNoPiaResolutionAssemblies();

            TypeSymbol result = null;

            if (!lookupIn.IsDefault)
            {
                try
                {
                    bool isInterface = Module.IsInterfaceOrThrow(typeDef);
                    TypeSymbol baseType = null;

                    if (!isInterface)
                    {
                        Handle baseToken = Module.GetBaseTypeOfTypeOrThrow(typeDef);

                        if (!baseToken.IsNil)
                        {
                            baseType = GetTypeOfToken(baseToken);
                        }
                    }

                    result = SubstituteNoPiaLocalType(
                        ref name,
                        isInterface,
                        baseType,
                        interfaceGuid,
                        scope,
                        identifier,
                        moduleSymbol.ContainingAssembly,
                        lookupIn);
                }
                catch (BadImageFormatException mrEx)
                {
                    result = GetUnsupportedMetadataTypeSymbol(mrEx);
                }

                Debug.Assert((object)result != null);
            }

            if ((object)result != null)
            {
                ConcurrentDictionary<TypeHandle, TypeSymbol> cache = GetTypeHandleToTypeMap();

                if (cache != null)
                {
                    TypeSymbol newresult = cache.GetOrAdd(typeDef, result);
                    Debug.Assert(ReferenceEquals(newresult, result) || (newresult.Kind == SymbolKind.ErrorType));
                    result = newresult;
                }
            }

            return result;
        }

        /// <summary>
        /// Find canonical type for NoPia embedded type.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="isInterface"></param>
        /// <param name="baseType"></param>
        /// <param name="interfaceGuid"></param>
        /// <param name="scope"></param>
        /// <param name="identifier"></param>
        /// <param name="referringAssembly"></param>
        /// <param name="lookupIn"></param>
        /// <returns>
        /// Symbol for the canonical type or an ErrorTypeSymbol. Never returns null.
        /// </returns>
        internal static NamedTypeSymbol SubstituteNoPiaLocalType(
            ref MetadataTypeName name,
            bool isInterface,
            TypeSymbol baseType,
            string interfaceGuid,
            string scope,
            string identifier,
            AssemblySymbol referringAssembly,
            ImmutableArray<AssemblySymbol> lookupIn)
        {
            NamedTypeSymbol result = null;

            Guid interfaceGuidValue = new Guid();
            bool haveInterfaceGuidValue = false;
            Guid scopeGuidValue = new Guid();
            bool haveScopeGuidValue = false;

            if (isInterface && interfaceGuid != null)
            {
                haveInterfaceGuidValue = Guid.TryParse(interfaceGuid, out interfaceGuidValue);

                if (haveInterfaceGuidValue)
                {
                    // To have consistent errors.
                    scope = null;
                    identifier = null;
                }
            }

            if (scope != null)
            {
                haveScopeGuidValue = Guid.TryParse(scope, out scopeGuidValue);
            }

            foreach (AssemblySymbol assembly in lookupIn)
            {
                if ((object)assembly == null || ReferenceEquals(assembly, referringAssembly))
                {
                    continue;
                }

                NamedTypeSymbol candidate = assembly.LookupTopLevelMetadataType(ref name, digThroughForwardedTypes: false);
                Debug.Assert(!candidate.IsGenericType);

                // Ignore type forwarders, error symbols and non-public types
                if (candidate.Kind == SymbolKind.ErrorType ||
                    !ReferenceEquals(candidate.ContainingAssembly, assembly) ||
                    candidate.DeclaredAccessibility != Accessibility.Public)
                {
                    continue;
                }

                // Ignore NoPia local types.
                // If candidate is coming from metadata, we don't need to do any special check,
                // because we do not create symbols for local types. However, local types defined in source 
                // is another story. However, if compilation explicitly defines a local type, it should be
                // represented by a retargeting assembly, which is supposed to hide the local type.
                Debug.Assert(!(assembly is SourceAssemblySymbol) || !((SourceAssemblySymbol)assembly).SourceModule.MightContainNoPiaLocalTypes());

                string candidateGuid;
                bool haveCandidateGuidValue = false;
                Guid candidateGuidValue = new Guid();

                // The type must be of the same kind (interface, struct, delegate or enum).
                switch (candidate.TypeKind)
                {
                    case TypeKind.Interface:
                        if (!isInterface)
                        {
                            continue;
                        }

                        // Get candidate's Guid
                        if (candidate.GetGuidString(out candidateGuid) && candidateGuid != null)
                        {
                            haveCandidateGuidValue = Guid.TryParse(candidateGuid, out candidateGuidValue);
                        }

                        break;

                    case TypeKind.Delegate:
                    case TypeKind.Enum:
                    case TypeKind.Struct:

                        if (isInterface)
                        {
                            continue;
                        }

                        // Let's use a trick. To make sure the kind is the same, make sure
                        // base type is the same.
                        if (!ReferenceEquals(baseType, candidate.BaseTypeNoUseSiteDiagnostics))
                        {
                            continue;
                        }

                        break;

                    default:
                        continue;
                }

                if (haveInterfaceGuidValue || haveCandidateGuidValue)
                {
                    if (!haveInterfaceGuidValue || !haveCandidateGuidValue ||
                        candidateGuidValue != interfaceGuidValue)
                    {
                        continue;
                    }
                }
                else
                {
                    if (!haveScopeGuidValue || identifier == null || !identifier.Equals(name.FullName))
                    {
                        continue;
                    }

                    // Scope guid must match candidate's assembly guid.
                    haveCandidateGuidValue = false;
                    if (assembly.GetGuidString(out candidateGuid) && candidateGuid != null)
                    {
                        haveCandidateGuidValue = Guid.TryParse(candidateGuid, out candidateGuidValue);
                    }

                    if (!haveCandidateGuidValue || scopeGuidValue != candidateGuidValue)
                    {
                        continue;
                    }
                }

                // OK. It looks like we found canonical type definition.
                if ((object)result != null)
                {
                    // Ambiguity 
                    result = new NoPiaAmbiguousCanonicalTypeSymbol(referringAssembly, result, candidate);
                    break;
                }

                result = candidate;
            }

            if ((object)result == null)
            {
                result = new NoPiaMissingCanonicalTypeSymbol(
                                referringAssembly,
                                name.FullName,
                                interfaceGuid,
                                scope,
                                identifier);
            }

            return result;
        }

        protected override MethodSymbol FindMethodSymbolInType(TypeSymbol typeSymbol, MethodHandle targetMethodDef)
        {
            Debug.Assert(typeSymbol is PENamedTypeSymbol || typeSymbol is ErrorTypeSymbol);

            foreach (Symbol member in typeSymbol.GetMembersUnordered())
            {
                PEMethodSymbol method = member as PEMethodSymbol;
                if ((object)method != null && method.Handle == targetMethodDef)
                {
                    return method;
                }
            }

            return null;
        }

        protected override FieldSymbol FindFieldSymbolInType(TypeSymbol typeSymbol, FieldHandle fieldDef)
        {
            Debug.Assert(typeSymbol is PENamedTypeSymbol || typeSymbol is ErrorTypeSymbol);

            foreach (Symbol member in typeSymbol.GetMembersUnordered())
            {
                PEFieldSymbol field = member as PEFieldSymbol;
                if ((object)field != null && field.Handle == fieldDef)
                {
                    return field;
                }
            }

            return null;
        }

        internal override Symbol GetSymbolForMemberRef(MemberReferenceHandle memberRef, TypeSymbol scope = null, bool methodsOnly = false)
        {
            TypeSymbol targetTypeSymbol = GetMemberRefTypeSymbol(memberRef);

            if ((object)scope != null)
            {
                Debug.Assert(scope.Kind == SymbolKind.NamedType || scope.Kind == SymbolKind.ErrorType);

                // We only want to consider members that are at or above "scope" in the type hierarchy.
                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                if (scope != targetTypeSymbol &&
                    !(targetTypeSymbol.IsInterfaceType()
                        ? scope.AllInterfacesNoUseSiteDiagnostics.Contains((NamedTypeSymbol)targetTypeSymbol)
                        : scope.IsDerivedFrom(targetTypeSymbol, ignoreDynamic: false, useSiteDiagnostics: ref useSiteDiagnostics)))
                {
                    return null;
                }
            }

            // We're going to use a special decoder that can generate useable symbols for type parameters without full context.
            // (We're not just using a different type - we're also changing the type context.)
            var memberRefDecoder = new MemberRefMetadataDecoder(moduleSymbol, targetTypeSymbol);

            return memberRefDecoder.FindMember(targetTypeSymbol, memberRef, methodsOnly);
        }

        protected override void EnqueueTypeSymbolInterfacesAndBaseTypes(Queue<TypeHandle> typeDefsToSearch, Queue<TypeSymbol> typeSymbolsToSearch, TypeSymbol typeSymbol)
        {
            foreach (NamedTypeSymbol @interface in typeSymbol.InterfacesNoUseSiteDiagnostics)
            {
                EnqueueTypeSymbol(typeDefsToSearch, typeSymbolsToSearch, @interface);
            }

            EnqueueTypeSymbol(typeDefsToSearch, typeSymbolsToSearch, typeSymbol.BaseTypeNoUseSiteDiagnostics);
        }

        protected override void EnqueueTypeSymbol(Queue<TypeHandle> typeDefsToSearch, Queue<TypeSymbol> typeSymbolsToSearch, TypeSymbol typeSymbol)
        {
            if ((object)typeSymbol != null)
            {
                PENamedTypeSymbol peTypeSymbol = typeSymbol as PENamedTypeSymbol;
                if ((object)peTypeSymbol != null && ReferenceEquals(peTypeSymbol.ContainingPEModule, moduleSymbol))
                {
                    typeDefsToSearch.Enqueue(peTypeSymbol.Handle);
                }
                else
                {
                    typeSymbolsToSearch.Enqueue(typeSymbol);
                }
            }
        }

        protected override MethodHandle GetMethodHandle(MethodSymbol method)
        {
            PEMethodSymbol peMethod = method as PEMethodSymbol;
            if ((object)peMethod != null && ReferenceEquals(peMethod.ContainingModule, moduleSymbol))
            {
                return peMethod.Handle;
            }

            return default(MethodHandle);
        }
    }
}
