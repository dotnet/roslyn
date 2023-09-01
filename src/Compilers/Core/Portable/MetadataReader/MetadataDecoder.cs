// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    [StructLayout(LayoutKind.Auto)]
    internal readonly struct ModifierInfo<TypeSymbol>
        where TypeSymbol : class
    {
        internal readonly bool IsOptional;
        internal readonly TypeSymbol Modifier;

        public ModifierInfo(bool isOptional, TypeSymbol modifier)
        {
            IsOptional = isOptional;
            Modifier = modifier;
        }
    }

    internal static class ModifierInfoExtensions
    {
        internal static bool AnyRequired<TypeSymbol>(this ImmutableArray<ModifierInfo<TypeSymbol>> modifiers) where TypeSymbol : class
        {
            return !modifiers.IsDefaultOrEmpty && modifiers.Any(static m => !m.IsOptional);
        }
    }

    internal readonly struct FieldInfo<TypeSymbol>
        where TypeSymbol : class
    {
        internal readonly bool IsByRef;
        internal readonly ImmutableArray<ModifierInfo<TypeSymbol>> RefCustomModifiers;
        internal readonly TypeSymbol Type;
        internal readonly ImmutableArray<ModifierInfo<TypeSymbol>> CustomModifiers;

        internal FieldInfo(bool isByRef, ImmutableArray<ModifierInfo<TypeSymbol>> refCustomModifiers, TypeSymbol type, ImmutableArray<ModifierInfo<TypeSymbol>> customModifiers)
        {
            IsByRef = isByRef;
            RefCustomModifiers = refCustomModifiers;
            Type = type;
            CustomModifiers = customModifiers;
        }

        internal FieldInfo(TypeSymbol type) : this(isByRef: false, refCustomModifiers: default, type, customModifiers: default)
        {
        }
    }

    [StructLayout(LayoutKind.Auto)]
    internal struct ParamInfo<TypeSymbol>
        where TypeSymbol : class
    {
        internal bool IsByRef;
        internal TypeSymbol Type;
        internal ParameterHandle Handle; // may be nil
        internal ImmutableArray<ModifierInfo<TypeSymbol>> RefCustomModifiers;
        internal ImmutableArray<ModifierInfo<TypeSymbol>> CustomModifiers;
    }

    [StructLayout(LayoutKind.Auto)]
    internal readonly struct LocalInfo<TypeSymbol>
        where TypeSymbol : class
    {
        internal readonly byte[] SignatureOpt;
        internal readonly TypeSymbol Type;
        internal readonly ImmutableArray<ModifierInfo<TypeSymbol>> CustomModifiers;
        internal readonly LocalSlotConstraints Constraints;

        internal LocalInfo(TypeSymbol type, ImmutableArray<ModifierInfo<TypeSymbol>> customModifiers, LocalSlotConstraints constraints, byte[] signatureOpt)
        {
            Debug.Assert(type != null);

            this.Type = type;
            this.CustomModifiers = customModifiers;
            this.Constraints = constraints;
            this.SignatureOpt = signatureOpt;
        }

        internal LocalInfo<TypeSymbol> WithSignature(byte[] signature)
        {
            return new LocalInfo<TypeSymbol>(this.Type, this.CustomModifiers, this.Constraints, signature);
        }

        public bool IsByRef => (Constraints & LocalSlotConstraints.ByRef) != 0;

        public bool IsPinned => (Constraints & LocalSlotConstraints.Pinned) != 0;
    }

#nullable enable
    internal interface IAttributeNamedArgumentDecoder
    {
        (KeyValuePair<string, TypedConstant> nameValuePair, bool isProperty, SerializationTypeCode typeCode, SerializationTypeCode elementTypeCode) DecodeCustomAttributeNamedArgumentOrThrow(ref BlobReader argReader);
    }

    internal abstract class MetadataDecoder<ModuleSymbol, TypeSymbol, MethodSymbol, FieldSymbol, Symbol> :
        TypeNameDecoder<ModuleSymbol, TypeSymbol>,
        IAttributeNamedArgumentDecoder
        where ModuleSymbol : class, IModuleSymbolInternal
        where TypeSymbol : class, Symbol, ITypeSymbolInternal
        where MethodSymbol : class, Symbol, IMethodSymbolInternal
        where FieldSymbol : class, Symbol, IFieldSymbolInternal
        where Symbol : class, ISymbolInternal
#nullable disable
    {
        public readonly PEModule Module;

        // Identity of an assembly containing the module, or null if the module is a standalone module
        private readonly AssemblyIdentity _containingAssemblyIdentity;

        internal MetadataDecoder(PEModule module, AssemblyIdentity containingAssemblyIdentity, SymbolFactory<ModuleSymbol, TypeSymbol> factory, ModuleSymbol moduleSymbol) :
            base(factory, moduleSymbol)
        {
            Debug.Assert(module != null);
            this.Module = module;
            _containingAssemblyIdentity = containingAssemblyIdentity;
        }

        internal TypeSymbol GetTypeOfToken(EntityHandle token)
        {
            bool isNoPiaLocalType;
            return GetTypeOfToken(token, out isNoPiaLocalType);
        }

        internal TypeSymbol GetTypeOfToken(EntityHandle token, out bool isNoPiaLocalType)
        {
            Debug.Assert(!token.IsNil);

            TypeSymbol type;
            HandleKind tokenType = token.Kind;

            switch (tokenType)
            {
                case HandleKind.TypeDefinition:
                    type = GetTypeOfTypeDef((TypeDefinitionHandle)token, out isNoPiaLocalType, isContainingType: false);
                    break;

                case HandleKind.TypeSpecification:
                    isNoPiaLocalType = false;
                    type = GetTypeOfTypeSpec((TypeSpecificationHandle)token);
                    break;

                case HandleKind.TypeReference:
                    type = GetTypeOfTypeRef((TypeReferenceHandle)token, out isNoPiaLocalType);
                    break;

                default:
                    isNoPiaLocalType = false;
                    type = GetUnsupportedMetadataTypeSymbol();
                    break;
            }

            Debug.Assert(type != null);
            return type;
        }

        private TypeSymbol GetTypeOfTypeSpec(TypeSpecificationHandle typeSpec)
        {
            TypeSymbol ptype;

            try
            {
                BlobReader memoryReader = this.Module.GetTypeSpecificationSignatureReaderOrThrow(typeSpec);

                bool refersToNoPiaLocalType;
                ptype = DecodeTypeOrThrow(ref memoryReader, out refersToNoPiaLocalType);
            }
            catch (BadImageFormatException mrEx)
            {
                ptype = GetUnsupportedMetadataTypeSymbol(mrEx); // an exception from metadata reader.
            }
            catch (UnsupportedSignatureContent)
            {
                ptype = GetUnsupportedMetadataTypeSymbol(); // unsupported signature content
            }

            return ptype;
        }

        /// <exception cref="UnsupportedSignatureContent">If the encoded type is invalid.</exception>
        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        private TypeSymbol DecodeTypeOrThrow(ref BlobReader ppSig, out bool refersToNoPiaLocalType)
        {
            SignatureTypeCode typeCode = ppSig.ReadSignatureTypeCode();
            return DecodeTypeOrThrow(ref ppSig, typeCode, out refersToNoPiaLocalType);
        }

        /// <exception cref="UnsupportedSignatureContent">If the encoded type is invalid.</exception>
        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        private TypeSymbol DecodeTypeOrThrow(ref BlobReader ppSig, SignatureTypeCode typeCode, out bool refersToNoPiaLocalType)
        {
            TypeSymbol typeSymbol;
            int paramPosition;
            ImmutableArray<ModifierInfo<TypeSymbol>> modifiers;

            refersToNoPiaLocalType = false;

            // Switch on the type.
            switch (typeCode)
            {
                case SignatureTypeCode.Void:
                case SignatureTypeCode.Boolean:
                case SignatureTypeCode.SByte:
                case SignatureTypeCode.Byte:
                case SignatureTypeCode.Int16:
                case SignatureTypeCode.UInt16:
                case SignatureTypeCode.Int32:
                case SignatureTypeCode.UInt32:
                case SignatureTypeCode.Int64:
                case SignatureTypeCode.UInt64:
                case SignatureTypeCode.Single:
                case SignatureTypeCode.Double:
                case SignatureTypeCode.Char:
                case SignatureTypeCode.String:
                case SignatureTypeCode.IntPtr:
                case SignatureTypeCode.UIntPtr:
                case SignatureTypeCode.Object:
                case SignatureTypeCode.TypedReference:
                    typeSymbol = GetSpecialType(typeCode.ToSpecialType());
                    break;

                case SignatureTypeCode.TypeHandle:
                    // Spec (6th edition): In II.23.2.12 and II.23.2.14, it is implied that the token in (CLASS | VALUETYPE) TypeDefOrRefOrSpecEncoded 
                    // can be a TypeSpec, when in fact it must be a TypeDef or TypeRef.
                    // See https://github.com/dotnet/roslyn/issues/7970
                    typeSymbol = GetSymbolForTypeHandleOrThrow(ppSig.ReadTypeHandle(), out refersToNoPiaLocalType, allowTypeSpec: false, requireShortForm: true);
                    break;

                case SignatureTypeCode.Array:
                    int countOfDimensions;
                    int countOfSizes;
                    int countOfLowerBounds;

                    modifiers = DecodeModifiersOrThrow(ref ppSig, out typeCode);
                    typeSymbol = DecodeTypeOrThrow(ref ppSig, typeCode, out refersToNoPiaLocalType);
                    if (!ppSig.TryReadCompressedInteger(out countOfDimensions) ||
                        !ppSig.TryReadCompressedInteger(out countOfSizes))
                    {
                        throw new UnsupportedSignatureContent();
                    }

                    // The most common case is when countOfSizes is 0.
                    ImmutableArray<int> sizes;

                    if (countOfSizes == 0)
                    {
                        sizes = ImmutableArray<int>.Empty;
                    }
                    else
                    {
                        var builder = ArrayBuilder<int>.GetInstance(countOfSizes);

                        for (int i = 0; i < countOfSizes; i++)
                        {
                            int size;
                            if (ppSig.TryReadCompressedInteger(out size))
                            {
                                builder.Add(size);
                            }
                            else
                            {
                                throw new UnsupportedSignatureContent();
                            }
                        }

                        sizes = builder.ToImmutableAndFree();
                    }

                    if (!ppSig.TryReadCompressedInteger(out countOfLowerBounds))
                    {
                        throw new UnsupportedSignatureContent();
                    }

                    // The most common case is when countOfLowerBounds == countOfDimensions and they are all 0.
                    // This is what Default will stand for.
                    ImmutableArray<int> lowerBounds = default(ImmutableArray<int>);

                    if (countOfLowerBounds == 0)
                    {
                        lowerBounds = ImmutableArray<int>.Empty;
                    }
                    else
                    {
                        ArrayBuilder<int> builder = countOfLowerBounds != countOfDimensions ? ArrayBuilder<int>.GetInstance(countOfLowerBounds, 0) : null;

                        for (int i = 0; i < countOfLowerBounds; i++)
                        {
                            int lowerBound;
                            if (ppSig.TryReadCompressedSignedInteger(out lowerBound))
                            {
                                if (lowerBound != 0)
                                {
                                    if (builder == null)
                                    {
                                        builder = ArrayBuilder<int>.GetInstance(countOfLowerBounds, 0);
                                    }

                                    builder[i] = lowerBound;
                                }
                            }
                            else
                            {
                                throw new UnsupportedSignatureContent();
                            }
                        }

                        if (builder != null)
                        {
                            lowerBounds = builder.ToImmutableAndFree();
                        }
                    }

                    typeSymbol = GetMDArrayTypeSymbol(countOfDimensions, typeSymbol, modifiers, sizes, lowerBounds);
                    break;

                case SignatureTypeCode.SZArray:
                    modifiers = DecodeModifiersOrThrow(ref ppSig, out typeCode);
                    typeSymbol = DecodeTypeOrThrow(ref ppSig, typeCode, out refersToNoPiaLocalType);
                    typeSymbol = GetSZArrayTypeSymbol(typeSymbol, modifiers);
                    break;

                case SignatureTypeCode.Pointer:
                    modifiers = DecodeModifiersOrThrow(ref ppSig, out typeCode);
                    typeSymbol = DecodeTypeOrThrow(ref ppSig, typeCode, out refersToNoPiaLocalType);
                    typeSymbol = MakePointerTypeSymbol(typeSymbol, modifiers);
                    break;

                case SignatureTypeCode.GenericTypeParameter:
                    if (!ppSig.TryReadCompressedInteger(out paramPosition))
                    {
                        throw new UnsupportedSignatureContent();
                    }

                    typeSymbol = GetGenericTypeParamSymbol(paramPosition);
                    break;

                case SignatureTypeCode.GenericMethodParameter:
                    if (!ppSig.TryReadCompressedInteger(out paramPosition))
                    {
                        throw new UnsupportedSignatureContent();
                    }

                    typeSymbol = GetGenericMethodTypeParamSymbol(paramPosition);
                    break;

                case SignatureTypeCode.GenericTypeInstance:
                    typeSymbol = DecodeGenericTypeInstanceOrThrow(ref ppSig, out refersToNoPiaLocalType);
                    break;

                case SignatureTypeCode.FunctionPointer:
                    var signatureHeader = ppSig.ReadSignatureHeader();
                    var parameters = DecodeSignatureParametersOrThrow(ref ppSig, signatureHeader, typeParameterCount: out int typeParamCount, shouldProcessAllBytes: false, isFunctionPointerSignature: true);

                    if (typeParamCount != 0)
                    {
                        throw new UnsupportedSignatureContent();
                    }

                    typeSymbol = MakeFunctionPointerTypeSymbol(Cci.CallingConventionUtils.FromSignatureConvention(signatureHeader.CallingConvention), ImmutableArray.Create(parameters));
                    break;

                default:
                    throw new UnsupportedSignatureContent();
            }

            return typeSymbol;
        }

        private TypeSymbol DecodeGenericTypeInstanceOrThrow(ref BlobReader ppSig, out bool refersToNoPiaLocalType)
        {
            SignatureTypeCode elementTypeCode = ppSig.ReadSignatureTypeCode();
            if (elementTypeCode != SignatureTypeCode.TypeHandle)
            {
                throw new UnsupportedSignatureContent();
            }

            EntityHandle tokenGeneric = ppSig.ReadTypeHandle();
            int argumentCount;
            if (!ppSig.TryReadCompressedInteger(out argumentCount))
            {
                throw new UnsupportedSignatureContent();
            }

            TypeSymbol generic = GetTypeOfToken(tokenGeneric, out refersToNoPiaLocalType);
            Debug.Assert(!refersToNoPiaLocalType || generic.TypeKind == TypeKind.Error);

            var argumentsBuilder = ArrayBuilder<KeyValuePair<TypeSymbol, ImmutableArray<ModifierInfo<TypeSymbol>>>>.GetInstance(argumentCount);
            var argumentRefersToNoPiaLocalTypeBuilder = ArrayBuilder<bool>.GetInstance(argumentCount);

            for (int argumentIndex = 0; argumentIndex < argumentCount; argumentIndex++)
            {
                bool argumentRefersToNoPia;
                SignatureTypeCode typeCode;
                ImmutableArray<ModifierInfo<TypeSymbol>> modifiers = DecodeModifiersOrThrow(ref ppSig, out typeCode);
                argumentsBuilder.Add(KeyValuePairUtil.Create(DecodeTypeOrThrow(ref ppSig, typeCode, out argumentRefersToNoPia), modifiers));
                argumentRefersToNoPiaLocalTypeBuilder.Add(argumentRefersToNoPia);
            }

            // The instantiated type might have a generic parent, in which case some or all of the type
            // arguments might actually be for the parent.

            var arguments = argumentsBuilder.ToImmutableAndFree();
            var argumentRefersToNoPiaLocalType = argumentRefersToNoPiaLocalTypeBuilder.ToImmutableAndFree();
            TypeSymbol typeSymbol = SubstituteTypeParameters(generic, arguments, argumentRefersToNoPiaLocalType);

            foreach (bool flag in argumentRefersToNoPiaLocalType)
            {
                if (flag)
                {
                    refersToNoPiaLocalType = true;
                    break;
                }
            }

            return typeSymbol;
        }

        /// <exception cref="UnsupportedSignatureContent">If the encoded type is invalid.</exception>
        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        internal TypeSymbol GetSymbolForTypeHandleOrThrow(EntityHandle handle, out bool isNoPiaLocalType, bool allowTypeSpec, bool requireShortForm)
        {
            if (handle.IsNil)
            {
                throw new UnsupportedSignatureContent();
            }

            TypeSymbol typeSymbol;
            switch (handle.Kind)
            {
                case HandleKind.TypeDefinition:
                    typeSymbol = GetTypeOfTypeDef((TypeDefinitionHandle)handle, out isNoPiaLocalType, isContainingType: false);
                    break;

                case HandleKind.TypeReference:
                    typeSymbol = GetTypeOfTypeRef((TypeReferenceHandle)handle, out isNoPiaLocalType);
                    break;

                case HandleKind.TypeSpecification:
                    if (!allowTypeSpec)
                    {
                        throw new UnsupportedSignatureContent();
                    }

                    isNoPiaLocalType = false;
                    typeSymbol = GetTypeOfTypeSpec((TypeSpecificationHandle)handle);
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(handle.Kind);
            }

            // tomat: Breaking change
            // Metadata spec II.23.2.16 (Short form signatures) requires primitive types to be encoded using a short form:
            // 
            //  "The general specification for signatures leaves some leeway in how to encode certain items. For
            //   example, it appears valid to encode a String as either 
            //     long-form: (ELEMENT_TYPE_CLASS, TypeRef-to-System.String )
            //     short-form: ELEMENT_TYPE_STRING
            //   Only the short form is valid."
            // 
            // Native compilers accept long form signatures (actually IMetadataImport does).
            // When a MemberRef is emitted the signature blob is copied from the metadata reference to the resulting assembly. 
            // Such assembly doesn't PEVerify but the CLR type loader matches the MemberRef with the original signature 
            // (since they are identical copies).
            // 
            // Roslyn doesn't copy signature blobs to the resulting assembly, it encodes the MemberRef using the 
            // correct short type codes. If we allowed long forms in a signature we would produce IL that PEVerifies but
            // the type loader isn't able to load it since the MemberRef signature wouldn't match the original signature.
            // 
            // Rather then producing broken code we report an error at compile time.

            if (requireShortForm && typeSymbol.SpecialType.HasShortFormSignatureEncoding())
            {
                throw new UnsupportedSignatureContent();
            }

            return typeSymbol;
        }

        // MetaImport::GetTypeOfTypeRef
        private TypeSymbol GetTypeOfTypeRef(TypeReferenceHandle typeRef, out bool isNoPiaLocalType)
        {
            // This is a cache similar to one used by native compiler in MetaImport::GetTypeOfTypeRef.
            // TypeRef tokens are unique only within a Module
            ConcurrentDictionary<TypeReferenceHandle, TypeSymbol> cache = GetTypeRefHandleToTypeMap();
            TypeSymbol result;

            if (cache != null && cache.TryGetValue(typeRef, out result))
            {
                isNoPiaLocalType = false; // We do not cache otherwise.
                return result;
            }

            try
            {
                string name, @namespace;
                EntityHandle resolutionScope;
                Module.GetTypeRefPropsOrThrow(typeRef, out name, out @namespace, out resolutionScope);
                Debug.Assert(MetadataHelpers.IsValidMetadataIdentifier(name));

                MetadataTypeName mdName = @namespace.Length > 0
                    ? MetadataTypeName.FromNamespaceAndTypeName(@namespace, name)
                    : MetadataTypeName.FromTypeName(name);

                result = GetTypeByNameOrThrow(ref mdName, resolutionScope, out isNoPiaLocalType);
            }
            catch (BadImageFormatException mrEx)
            {
                result = GetUnsupportedMetadataTypeSymbol(mrEx); // an exception from metadata reader.
                isNoPiaLocalType = false;
            }

            Debug.Assert(result != null);

            // Cache the result, but only if it is not a local type because the cache doesn't retain this information.
            if (cache != null && !isNoPiaLocalType)
            {
                TypeSymbol result1 = cache.GetOrAdd(typeRef, result);
                Debug.Assert(result1.Equals(result, TypeCompareKind.ConsiderEverything));
            }

            return result;
        }

        // MetaImport::GetTypeByName
        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        private TypeSymbol GetTypeByNameOrThrow(
            ref MetadataTypeName fullName,
            EntityHandle tokenResolutionScope,
            out bool isNoPiaLocalType)
        {
            HandleKind tokenType = tokenResolutionScope.Kind;

            // TODO: I believe refs can be parented by a def tokens too, not common, but.
            //       Should also do NoPia related checks.

            // The resolution scope should be either a type ref, an assembly or a module.
            if (tokenType == HandleKind.TypeReference)
            {
                if (tokenResolutionScope.IsNil)
                {
                    throw new BadImageFormatException();
                }
                TypeSymbol psymContainer = GetTypeOfToken(tokenResolutionScope);
                Debug.Assert(fullName.NamespaceName.Length == 0);
                isNoPiaLocalType = false;
                return LookupNestedTypeDefSymbol(psymContainer, ref fullName);
            }

            if (tokenType == HandleKind.AssemblyReference)
            {
                isNoPiaLocalType = false;
                var assemblyRef = (AssemblyReferenceHandle)tokenResolutionScope;
                if (assemblyRef.IsNil)
                {
                    throw new BadImageFormatException();
                }
                return LookupTopLevelTypeDefSymbol(Module.GetAssemblyReferenceIndexOrThrow(assemblyRef), ref fullName);
            }

            if (tokenType == HandleKind.ModuleReference)
            {
                var moduleRef = (ModuleReferenceHandle)tokenResolutionScope;
                if (moduleRef.IsNil)
                {
                    throw new BadImageFormatException();
                }
                return LookupTopLevelTypeDefSymbol(
                    Module.GetModuleRefNameOrThrow(moduleRef),
                    ref fullName,
                    out isNoPiaLocalType);
            }

            if (tokenResolutionScope == EntityHandle.ModuleDefinition)
            {
                // The last case is a little bit strange.  Here, the TypeRef's TypeDef
                // lives in the same module as the TypeRef itself.  This is represented
                // as a resolution scope of 0x00000001.
                return LookupTopLevelTypeDefSymbol(ref fullName, out isNoPiaLocalType);
            }

            isNoPiaLocalType = false;
            return GetUnsupportedMetadataTypeSymbol();
        }

        private TypeSymbol GetTypeOfTypeDef(TypeDefinitionHandle typeDef)
        {
            bool isNoPiaLocalType;
            return GetTypeOfTypeDef(typeDef, out isNoPiaLocalType, isContainingType: false);
        }

        private TypeSymbol GetTypeOfTypeDef(TypeDefinitionHandle typeDef, out bool isNoPiaLocalType, bool isContainingType)
        {
            try
            {
                // This is a cache similar to one used in MetaImport::GetTypeOfToken by native compiler.
                // TypeDef tokens are unique within Module.
                // This cache makes lookup of top level types about twice as fast, about three times as fast if 
                // EmittedNameToTypeMap in LookupTopLevelType doesn't contain the name. 
                // It is likely that gain for nested types will be bigger because we don't cache names of nested types.

                ConcurrentDictionary<TypeDefinitionHandle, TypeSymbol> cache = GetTypeHandleToTypeMap();

                TypeSymbol result;

                if (cache != null && cache.TryGetValue(typeDef, out result))
                {
                    if (!Module.IsNestedTypeDefOrThrow(typeDef) && Module.IsNoPiaLocalType(typeDef))
                    {
                        isNoPiaLocalType = true;
                    }
                    else
                    {
                        isNoPiaLocalType = false;
                    }

                    return result;
                }

                MetadataTypeName mdName;
                string name = Module.GetTypeDefNameOrThrow(typeDef);
                Debug.Assert(MetadataHelpers.IsValidMetadataIdentifier(name));

                if (Module.IsNestedTypeDefOrThrow(typeDef))
                {
                    // first resolve nesting type 
                    TypeDefinitionHandle containerTypeDef = Module.GetContainingTypeOrThrow(typeDef);

                    // invalid metadata?
                    if (containerTypeDef.IsNil)
                    {
                        isNoPiaLocalType = false;
                        return GetUnsupportedMetadataTypeSymbol();
                    }

                    TypeSymbol container = GetTypeOfTypeDef(containerTypeDef, out isNoPiaLocalType, isContainingType: true);

                    if (isNoPiaLocalType)
                    {
                        // Types nested into local types are not supported.
                        if (!isContainingType)
                        {
                            isNoPiaLocalType = false;
                        }

                        return GetUnsupportedMetadataTypeSymbol();
                    }

                    mdName = MetadataTypeName.FromTypeName(name);
                    return LookupNestedTypeDefSymbol(container, ref mdName);
                }

                string namespaceName = Module.GetTypeDefNamespaceOrThrow(typeDef);

                mdName = namespaceName.Length > 0
                    ? MetadataTypeName.FromNamespaceAndTypeName(namespaceName, name)
                    : MetadataTypeName.FromTypeName(name);
                // It is extremely difficult to hit the last branch because it is executed 
                // only for types in the Global namespace and they are getting loaded 
                // as soon as we start traversing Symbol Table, therefore, their TypeDef
                // handle is getting cached and lookup in the cache succeeds. 
                // Probably we can hit it if the first thing we do is to interrogate 
                // Module/Assembly level attributes, which refer to a TypeDef in the 
                // Global namespace.

                // Check if this is NoPia local type which should be substituted 
                // with corresponding canonical type
                string interfaceGuid;
                string scope;
                string identifier;
                if (Module.IsNoPiaLocalType(
                    typeDef,
                    out interfaceGuid,
                    out scope,
                    out identifier))
                {
                    isNoPiaLocalType = true;

                    if (!Module.HasGenericParametersOrThrow(typeDef))
                    {
                        MetadataTypeName localTypeName = MetadataTypeName.FromNamespaceAndTypeName(mdName.NamespaceName, mdName.TypeName, forcedArity: 0);
                        result = SubstituteNoPiaLocalType(typeDef,
                            ref localTypeName,
                            interfaceGuid,
                            scope,
                            identifier);
                        Debug.Assert((object)result != null);
                        return result;
                    }

                    // Unification of generic local types is not supported 
                    result = GetUnsupportedMetadataTypeSymbol();

                    if (cache != null)
                    {
                        result = cache.GetOrAdd(typeDef, result);
                    }

                    return result;
                }

                isNoPiaLocalType = false;
                result = LookupTopLevelTypeDefSymbol(ref mdName, out isNoPiaLocalType);
                Debug.Assert(!isNoPiaLocalType);
                return result;
            }
            catch (BadImageFormatException mrEx)
            {
                isNoPiaLocalType = false;
                return GetUnsupportedMetadataTypeSymbol(mrEx); // an exception from metadata reader.
            }
        }

        /// <exception cref="UnsupportedSignatureContent">If the encoded type is invalid.</exception>
        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        private ImmutableArray<ModifierInfo<TypeSymbol>> DecodeModifiersOrThrow(
            ref BlobReader signatureReader,
            out SignatureTypeCode typeCode)
        {
            ArrayBuilder<ModifierInfo<TypeSymbol>> modifiers = null;

            for (; ; )
            {
                typeCode = signatureReader.ReadSignatureTypeCode();
                bool isOptional;

                if (typeCode == SignatureTypeCode.RequiredModifier)
                {
                    isOptional = false;
                }
                else if (typeCode == SignatureTypeCode.OptionalModifier)
                {
                    isOptional = true;
                }
                else
                {
                    break;
                }

                TypeSymbol type = DecodeModifierTypeOrThrow(ref signatureReader);
                ModifierInfo<TypeSymbol> modifier = new ModifierInfo<TypeSymbol>(isOptional, type);

                if (modifiers == null)
                {
                    modifiers = ArrayBuilder<ModifierInfo<TypeSymbol>>.GetInstance();
                }

                modifiers.Add(modifier);
            }

            return modifiers?.ToImmutableAndFree() ?? default;
        }

        private TypeSymbol DecodeModifierTypeOrThrow(ref BlobReader signatureReader)
        {
            EntityHandle token = signatureReader.ReadTypeHandle();
            TypeSymbol type;
            bool isNoPiaLocalType;

// According to ECMA spec:
//  The CMOD_OPT or CMOD_REQD is followed by a metadata token that
//  indexes a row in the TypeDef table or the TypeRef table.
tryAgain:
            switch (token.Kind)
            {
                case HandleKind.TypeDefinition:
                    type = GetTypeOfTypeDef((TypeDefinitionHandle)token, out isNoPiaLocalType, isContainingType: false);
                    // it is valid for a modifier to refer to an unconstructed type, we need to preserve this fact
                    type = SubstituteWithUnboundIfGeneric(type);
                    break;

                case HandleKind.TypeReference:
                    type = GetTypeOfTypeRef((TypeReferenceHandle)token, out isNoPiaLocalType);
                    // it is valid for a modifier to refer to an unconstructed type, we need to preserve this fact
                    type = SubstituteWithUnboundIfGeneric(type);
                    break;

                case HandleKind.TypeSpecification:
                    // Section 23.2.7 of the CLI spec specifically says that this is not allowed (see comment on method),
                    // but, apparently, ilasm turns modopt(int32) into a TypeSpec.
                    // In addition, managed C++ compiler can use constructed generic types as modifiers, for example Nullable<bool>, etc.
                    // We will support only cases like these even though it looks like CLR allows any types that can be encoded through a TypeSpec.

                    BlobReader memoryReader = this.Module.GetTypeSpecificationSignatureReaderOrThrow((TypeSpecificationHandle)token);

                    SignatureTypeCode typeCode = memoryReader.ReadSignatureTypeCode();
                    bool refersToNoPiaLocalType;

                    switch (typeCode)
                    {
                        case SignatureTypeCode.Void:
                        case SignatureTypeCode.Boolean:
                        case SignatureTypeCode.SByte:
                        case SignatureTypeCode.Byte:
                        case SignatureTypeCode.Int16:
                        case SignatureTypeCode.UInt16:
                        case SignatureTypeCode.Int32:
                        case SignatureTypeCode.UInt32:
                        case SignatureTypeCode.Int64:
                        case SignatureTypeCode.UInt64:
                        case SignatureTypeCode.Single:
                        case SignatureTypeCode.Double:
                        case SignatureTypeCode.Char:
                        case SignatureTypeCode.String:
                        case SignatureTypeCode.IntPtr:
                        case SignatureTypeCode.UIntPtr:
                        case SignatureTypeCode.Object:
                        case SignatureTypeCode.TypedReference:
                            type = GetSpecialType(typeCode.ToSpecialType());
                            break;

                        case SignatureTypeCode.TypeHandle:

                            token = memoryReader.ReadTypeHandle();
                            goto tryAgain;

                        case SignatureTypeCode.GenericTypeInstance:
                            type = DecodeGenericTypeInstanceOrThrow(ref memoryReader, out refersToNoPiaLocalType);
                            break;

                        default:
                            throw new UnsupportedSignatureContent();
                    }
                    break;

                default:
                    throw new UnsupportedSignatureContent();
            }

            return type;
        }

        /// <exception cref="UnsupportedSignatureContent">If the encoded local variable type is invalid.</exception>
        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        internal ImmutableArray<LocalInfo<TypeSymbol>> DecodeLocalSignatureOrThrow(ref BlobReader signatureReader)
        {
            SignatureHeader signatureHeader = signatureReader.ReadSignatureHeader();

            if (signatureHeader.Kind != SignatureKind.LocalVariables)
            {
                throw new UnsupportedSignatureContent();
            }

            int localCount;
            int typeParameterCount;
            GetSignatureCountsOrThrow(ref signatureReader, signatureHeader, out localCount, out typeParameterCount);
            Debug.Assert(typeParameterCount == 0);

            var locals = ArrayBuilder<LocalInfo<TypeSymbol>>.GetInstance(localCount);
            var offsets = ArrayBuilder<int>.GetInstance(localCount);
            try
            {
                for (int i = 0; i < localCount; i++)
                {
                    offsets.Add(signatureReader.Offset);
                    locals.Add(DecodeLocalVariableOrThrow(ref signatureReader));
                }

                if (signatureReader.RemainingBytes > 0)
                {
                    throw new UnsupportedSignatureContent();
                }

                // Include signatures with each local.
                signatureReader.Reset();
                for (int i = 0; i < localCount; i++)
                {
                    int start = offsets[i];
                    Debug.Assert(signatureReader.Offset <= start);
                    while (signatureReader.Offset < start)
                    {
                        signatureReader.ReadByte();
                    }

                    int n = (i < localCount - 1) ? (offsets[i + 1] - start) : signatureReader.RemainingBytes;
                    var signature = signatureReader.ReadBytes(n);

                    locals[i] = locals[i].WithSignature(signature);
                }

                return locals.ToImmutable();
            }
            finally
            {
                offsets.Free();
                locals.Free();
            }
        }

        internal TypeSymbol DecodeGenericParameterConstraint(EntityHandle token, out ImmutableArray<ModifierInfo<TypeSymbol>> modifiers)
        {
            modifiers = ImmutableArray<ModifierInfo<TypeSymbol>>.Empty;

            switch (token.Kind)
            {
                case HandleKind.TypeSpecification:
                    {
                        try
                        {
                            var memoryReader = this.Module.GetTypeSpecificationSignatureReaderOrThrow((TypeSpecificationHandle)token);
                            modifiers = DecodeModifiersOrThrow(ref memoryReader, out var typeCode);
                            var type = DecodeTypeOrThrow(ref memoryReader, typeCode, out _);
                            return type;
                        }
                        catch (BadImageFormatException mrEx)
                        {
                            return GetUnsupportedMetadataTypeSymbol(mrEx);
                        }
                        catch (UnsupportedSignatureContent)
                        {
                            return GetUnsupportedMetadataTypeSymbol();
                        }
                    }
                case HandleKind.TypeReference:
                    return GetTypeOfTypeRef((TypeReferenceHandle)token, out _);
                case HandleKind.TypeDefinition:
                    return GetTypeOfTypeDef((TypeDefinitionHandle)token);
                default:
                    return GetUnsupportedMetadataTypeSymbol();
            }
        }

        /// <exception cref="UnsupportedSignatureContent">If the encoded local variable type is invalid.</exception>
        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        internal LocalInfo<TypeSymbol> DecodeLocalVariableOrThrow(ref BlobReader signatureReader)
        {
            SignatureTypeCode typeCode;

            var customModifiers = DecodeModifiersOrThrow(ref signatureReader, out typeCode);

            if (customModifiers.AnyRequired())
            {
                throw new UnsupportedSignatureContent();
            }

            var constraints = LocalSlotConstraints.None;
            TypeSymbol typeSymbol;

            if (typeCode == SignatureTypeCode.Pinned)
            {
                constraints |= LocalSlotConstraints.Pinned;
                typeCode = signatureReader.ReadSignatureTypeCode();
            }

            if (typeCode == SignatureTypeCode.ByReference)
            {
                constraints |= LocalSlotConstraints.ByRef;
                typeCode = signatureReader.ReadSignatureTypeCode();
            }

            // TypedReference can't be by-ref or pinned:
            if (typeCode == SignatureTypeCode.TypedReference && constraints != LocalSlotConstraints.None)
            {
                typeSymbol = GetUnsupportedMetadataTypeSymbol();
            }
            else
            {
                try
                {
                    bool refersToNoPiaLocalType;
                    typeSymbol = DecodeTypeOrThrow(ref signatureReader, typeCode, out refersToNoPiaLocalType);
                }
                catch (UnsupportedSignatureContent)
                {
                    typeSymbol = GetUnsupportedMetadataTypeSymbol();
                }
            }

            return new LocalInfo<TypeSymbol>(typeSymbol, customModifiers, constraints, signatureOpt: null);
        }

        internal void DecodeLocalConstantBlobOrThrow(ref BlobReader sigReader, out TypeSymbol type, out ConstantValue value)
        {
            SignatureTypeCode typeCode;

            var customModifiers = DecodeModifiersOrThrow(ref sigReader, out typeCode);

            if (customModifiers.AnyRequired())
            {
                throw new UnsupportedSignatureContent();
            }

            if (typeCode == SignatureTypeCode.TypeHandle)
            {
                // TypeDefOrRefOrSpec encoded
                // From the PortablePDB spec: https://github.com/dotnet/runtime/blob/main/src/libraries/System.Reflection.Metadata/specs/PortablePdb-Metadata.md#localconstant-table-0x34
                // The encoding of the GeneralValue is determined based upon the type expressed by TypeDefOrRefOrSpecEncoded
                // specified in GeneralConstant. GeneralValue for special types listed in the table below has to be present
                // and is encoded as specified.
                // If the GeneralValue is not present the value of the constant is the default value of the type. If the type 
                // is a reference type the value is a null reference, if the type is a pointer type the value is a null pointer, etc.
                bool refersToNoPiaLocalType;
                type = GetSymbolForTypeHandleOrThrow(sigReader.ReadTypeHandle(), out refersToNoPiaLocalType, allowTypeSpec: true, requireShortForm: true);

                if (type.SpecialType == SpecialType.System_Decimal)
                {
                    value = ConstantValue.Create(sigReader.ReadDecimal());
                }
                else if (type.SpecialType == SpecialType.System_DateTime)
                {
                    value = ConstantValue.Create(sigReader.ReadDateTime());
                }
                else if (sigReader.RemainingBytes == 0)
                {
                    // Note: even though the PortablePDB spec permits constants of pointer types, C# does not, so those
                    // should only ever be seen if the PDB being decoded is a custom-assembled PDB for non-legal C#.
                    value = (type.IsReferenceType || type.TypeKind == TypeKind.Pointer || type.TypeKind == TypeKind.FunctionPointer) ? ConstantValue.Null : ConstantValue.Bad;
                }
                else
                {
                    value = ConstantValue.Bad;
                }
            }
            else
            {
                bool isEnumTypeCode;
                value = DecodePrimitiveConstantValue(ref sigReader, typeCode, out isEnumTypeCode);
                var specialType = typeCode.ToSpecialType();

                if (isEnumTypeCode && sigReader.RemainingBytes > 0)
                {
                    bool refersToNoPiaLocalType;
                    type = GetSymbolForTypeHandleOrThrow(sigReader.ReadTypeHandle(), out refersToNoPiaLocalType, allowTypeSpec: true, requireShortForm: true);

                    if (GetEnumUnderlyingType(type)?.SpecialType != specialType)
                    {
                        throw new UnsupportedSignatureContent();
                    }
                }
                else
                {
                    type = GetSpecialType(specialType);
                }

                if (sigReader.RemainingBytes > 0)
                {
                    throw new UnsupportedSignatureContent();
                }
            }
        }

        private static ConstantValue DecodePrimitiveConstantValue(ref BlobReader sigReader, SignatureTypeCode typeCode, out bool isEnumTypeCode)
        {
            switch (typeCode)
            {
                case SignatureTypeCode.Boolean:
                    isEnumTypeCode = true;
                    return ConstantValue.Create(sigReader.ReadBoolean());

                case SignatureTypeCode.Char:
                    isEnumTypeCode = true;
                    return ConstantValue.Create(sigReader.ReadChar());

                case SignatureTypeCode.SByte:
                    isEnumTypeCode = true;
                    return ConstantValue.Create(sigReader.ReadSByte());

                case SignatureTypeCode.Byte:
                    isEnumTypeCode = true;
                    return ConstantValue.Create(sigReader.ReadByte());

                case SignatureTypeCode.Int16:
                    isEnumTypeCode = true;
                    return ConstantValue.Create(sigReader.ReadInt16());

                case SignatureTypeCode.UInt16:
                    isEnumTypeCode = true;
                    return ConstantValue.Create(sigReader.ReadUInt16());

                case SignatureTypeCode.Int32:
                    isEnumTypeCode = true;
                    return ConstantValue.Create(sigReader.ReadInt32());

                case SignatureTypeCode.UInt32:
                    isEnumTypeCode = true;
                    return ConstantValue.Create(sigReader.ReadUInt32());

                case SignatureTypeCode.Int64:
                    isEnumTypeCode = true;
                    return ConstantValue.Create(sigReader.ReadInt64());

                case SignatureTypeCode.UInt64:
                    isEnumTypeCode = true;
                    return ConstantValue.Create(sigReader.ReadUInt64());

                case SignatureTypeCode.Single:
                    isEnumTypeCode = false;
                    return ConstantValue.Create(sigReader.ReadSingle());

                case SignatureTypeCode.Double:
                    isEnumTypeCode = false;
                    return ConstantValue.Create(sigReader.ReadDouble());

                case SignatureTypeCode.String:
                    isEnumTypeCode = false;

                    if (sigReader.RemainingBytes == 1)
                    {
                        if (sigReader.ReadByte() != 0xff)
                        {
                            return ConstantValue.Bad;
                        }

                        return ConstantValue.Null;
                    }

                    if (sigReader.RemainingBytes % 2 != 0)
                    {
                        return ConstantValue.Bad;
                    }

                    return ConstantValue.Create(sigReader.ReadUTF16(sigReader.RemainingBytes));

                case SignatureTypeCode.Object:
                    // null reference
                    isEnumTypeCode = false;
                    return ConstantValue.Null;

                default:
                    throw new UnsupportedSignatureContent();
            }
        }

        internal ImmutableArray<LocalInfo<TypeSymbol>> GetLocalsOrThrow(StandaloneSignatureHandle handle)
        {
            var signatureHandle = Module.MetadataReader.GetStandaloneSignature(handle).Signature;
            var signatureReader = Module.MetadataReader.GetBlobReader(signatureHandle);
            return DecodeLocalSignatureOrThrow(ref signatureReader);
        }

        /// <summary>
        /// Used to decode signatures of local constants returned by SymReader.
        /// </summary>
        internal unsafe TypeSymbol DecodeLocalVariableTypeOrThrow(ImmutableArray<byte> signature)
        {
            if (signature.IsDefaultOrEmpty)
            {
                throw new UnsupportedSignatureContent();
            }

            fixed (byte* ptr = signature.AsSpan())
            {
                var blobReader = new BlobReader(ptr, signature.Length);
                var info = DecodeLocalVariableOrThrow(ref blobReader);

                if (info.IsByRef || info.IsPinned)
                {
                    throw new UnsupportedSignatureContent();
                }

                return info.Type;
            }
        }

        /// <summary>
        /// Returns the local info for all locals indexed by slot.
        /// </summary>
        internal ImmutableArray<LocalInfo<TypeSymbol>> GetLocalInfo(StandaloneSignatureHandle localSignatureHandle)
        {
            if (localSignatureHandle.IsNil)
            {
                return ImmutableArray<LocalInfo<TypeSymbol>>.Empty;
            }

            var reader = Module.MetadataReader;
            var signature = reader.GetStandaloneSignature(localSignatureHandle).Signature;
            var blobReader = reader.GetBlobReader(signature);
            return DecodeLocalSignatureOrThrow(ref blobReader);
        }

        /// <exception cref="UnsupportedSignatureContent">If the encoded parameter type is invalid.</exception>
        private void DecodeParameterOrThrow(ref BlobReader signatureReader, /*out*/ ref ParamInfo<TypeSymbol> info)
        {
            info.CustomModifiers = DecodeModifiersOrThrow(
                ref signatureReader,
                out SignatureTypeCode typeCode);

            if (typeCode == SignatureTypeCode.ByReference)
            {
                info.IsByRef = true;

                info.RefCustomModifiers = info.CustomModifiers;
                info.CustomModifiers = DecodeModifiersOrThrow(ref signatureReader, out typeCode);
            }

            info.Type = DecodeTypeOrThrow(ref signatureReader, typeCode, out _);
        }

        // MetaImport::DecodeMethodSignature
        internal ParamInfo<TypeSymbol>[] GetSignatureForMethod(MethodDefinitionHandle methodDef, out SignatureHeader signatureHeader, out BadImageFormatException metadataException, bool setParamHandles = true)
        {
            ParamInfo<TypeSymbol>[] paramInfo = null;
            signatureHeader = default(SignatureHeader);

            try
            {
                BlobHandle signature = Module.GetMethodSignatureOrThrow(methodDef);
                BlobReader signatureReader = DecodeSignatureHeaderOrThrow(signature, out signatureHeader);

                int typeParameterCount; //CONSIDER: expose to caller?
                paramInfo = DecodeSignatureParametersOrThrow(ref signatureReader, signatureHeader, out typeParameterCount);

                if (setParamHandles)
                {
                    int paramInfoLength = paramInfo.Length;

                    // For each parameter, get corresponding row id from Param table. 
                    foreach (var param in Module.GetParametersOfMethodOrThrow(methodDef))
                    {
                        int sequenceNumber = Module.GetParameterSequenceNumberOrThrow(param);

                        // Ignore possible errors in parameter table.
                        if (sequenceNumber >= 0 && sequenceNumber < paramInfoLength && paramInfo[sequenceNumber].Handle.IsNil)
                        {
                            paramInfo[sequenceNumber].Handle = param;
                        }
                    }
                }

                metadataException = null;
            }
            catch (BadImageFormatException mrEx)
            {
                metadataException = mrEx;

                // An exception from metadata reader.
                if (paramInfo == null)
                {
                    // Pretend there are no parameters and capture error information in the return type.
                    paramInfo = new ParamInfo<TypeSymbol>[1];
                    paramInfo[0].Type = GetUnsupportedMetadataTypeSymbol(mrEx);
                }
            }

            return paramInfo;
        }

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        internal static void GetSignatureCountsOrThrow(PEModule module, MethodDefinitionHandle methodDef, out int parameterCount, out int typeParameterCount)
        {
            BlobHandle signature = module.GetMethodSignatureOrThrow(methodDef);
            SignatureHeader signatureHeader;
            BlobReader signatureReader = DecodeSignatureHeaderOrThrow(module, signature, out signatureHeader);

            GetSignatureCountsOrThrow(ref signatureReader, signatureHeader, out parameterCount, out typeParameterCount);
        }

        internal ParamInfo<TypeSymbol>[] GetSignatureForProperty(PropertyDefinitionHandle handle, out SignatureHeader signatureHeader, out BadImageFormatException BadImageFormatException)
        {
            ParamInfo<TypeSymbol>[] paramInfo = null;
            signatureHeader = default(SignatureHeader);

            try
            {
                var signature = Module.GetPropertySignatureOrThrow(handle);
                BlobReader signatureReader = DecodeSignatureHeaderOrThrow(signature, out signatureHeader);

                int typeParameterCount; //CONSIDER: expose to caller?
                paramInfo = DecodeSignatureParametersOrThrow(ref signatureReader, signatureHeader, out typeParameterCount);
                BadImageFormatException = null;
            }
            catch (BadImageFormatException mrEx)
            {
                BadImageFormatException = mrEx;

                // An exception from metadata reader.
                if (paramInfo == null)
                {
                    // Pretend there are no parameters and capture error information in the return type as well.
                    paramInfo = new ParamInfo<TypeSymbol>[1];
                    paramInfo[0].Type = GetUnsupportedMetadataTypeSymbol(mrEx);
                }
            }

            return paramInfo;
        }

        internal SignatureHeader GetSignatureHeaderForProperty(PropertyDefinitionHandle handle)
        {
            try
            {
                var signature = Module.GetPropertySignatureOrThrow(handle);
                SignatureHeader signatureHeader;
                DecodeSignatureHeaderOrThrow(signature, out signatureHeader);
                return signatureHeader;
            }
            catch (BadImageFormatException)
            {
                return default(SignatureHeader);
            }
        }

        #region Custom Attributes

        /// <summary>
        /// Decodes attribute parameter type from method signature.
        /// </summary>
        /// <exception cref="UnsupportedSignatureContent">If the encoded parameter type is invalid.</exception>
        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        private void DecodeCustomAttributeParameterTypeOrThrow(ref BlobReader sigReader, out SerializationTypeCode typeCode, out TypeSymbol type, out SerializationTypeCode elementTypeCode, out TypeSymbol elementType, bool isElementType)
        {
            SignatureTypeCode paramTypeCode = sigReader.ReadSignatureTypeCode();

            if (paramTypeCode == SignatureTypeCode.SZArray)
            {
                if (isElementType)
                {
                    // nested arrays not allowed
                    throw new UnsupportedSignatureContent();
                }

                SerializationTypeCode unusedElementTypeCode;
                TypeSymbol unusedElementType;
                DecodeCustomAttributeParameterTypeOrThrow(ref sigReader, out elementTypeCode, out elementType, out unusedElementTypeCode, out unusedElementType, isElementType: true);
                type = GetSZArrayTypeSymbol(elementType, customModifiers: default(ImmutableArray<ModifierInfo<TypeSymbol>>));
                typeCode = SerializationTypeCode.SZArray;
                return;
            }

            elementTypeCode = SerializationTypeCode.Invalid;
            elementType = null;

            switch (paramTypeCode)
            {
                case SignatureTypeCode.Object:
                    type = GetSpecialType(SpecialType.System_Object);
                    typeCode = SerializationTypeCode.TaggedObject;
                    return;

                case SignatureTypeCode.String:
                case SignatureTypeCode.Boolean:
                case SignatureTypeCode.Char:
                case SignatureTypeCode.SByte:
                case SignatureTypeCode.Byte:
                case SignatureTypeCode.Int16:
                case SignatureTypeCode.UInt16:
                case SignatureTypeCode.Int32:
                case SignatureTypeCode.UInt32:
                case SignatureTypeCode.Int64:
                case SignatureTypeCode.UInt64:
                case SignatureTypeCode.Single:
                case SignatureTypeCode.Double:
                    type = GetSpecialType(paramTypeCode.ToSpecialType());
                    typeCode = (SerializationTypeCode)paramTypeCode;
                    return;

                case SignatureTypeCode.TypeHandle:
                    // The type of the parameter can either be an enum type or System.Type.
                    bool isNoPiaLocalType;
                    type = GetSymbolForTypeHandleOrThrow(sigReader.ReadTypeHandle(), out isNoPiaLocalType, allowTypeSpec: true, requireShortForm: true);

                    var underlyingEnumType = GetEnumUnderlyingType(type);

                    // Spec: If the parameter kind is an enum -- simply store the value of the enum's underlying integer type.
                    if ((object)underlyingEnumType != null)
                    {
                        Debug.Assert(!isNoPiaLocalType);

                        // GetEnumUnderlyingType always returns a valid enum underlying type
                        typeCode = underlyingEnumType.SpecialType.ToSerializationType();
                        return;
                    }

                    if ((object)type == SystemTypeSymbol)
                    {
                        Debug.Assert(!isNoPiaLocalType);
                        typeCode = SerializationTypeCode.Type;
                        return;
                    }

                    break;
            }

            throw new UnsupportedSignatureContent();
        }

        /// <summary>
        /// Decodes attribute argument type from attribute blob (called FieldOrPropType in the spec).
        /// </summary>
        /// <exception cref="UnsupportedSignatureContent">If the encoded argument type is invalid.</exception>
        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        private void DecodeCustomAttributeFieldOrPropTypeOrThrow(ref BlobReader argReader, out SerializationTypeCode typeCode, out TypeSymbol type, out SerializationTypeCode elementTypeCode, out TypeSymbol elementType, bool isElementType)
        {
            typeCode = argReader.ReadSerializationTypeCode();

            // Spec:
            // The FieldOrPropType (typeCode) shall be exactly one of: ELEMENT_TYPE_BOOLEAN,
            // ELEMENT_TYPE_CHAR, ELEMENT_TYPE_I1, ELEMENT_TYPE_U1, ELEMENT_TYPE_I2,
            // ELEMENT_TYPE_U2, ELEMENT_TYPE_I4, ELEMENT_TYPE_U4, ELEMENT_TYPE_I8,
            // ELEMENT_TYPE_U8, ELEMENT_TYPE_R4, ELEMENT_TYPE_R8, ELEMENT_TYPE_STRING.
            // 
            // A single-dimensional, zero-based array is specified as a single byte 0x1D followed
            // by the FieldOrPropType of the element type. (See §II.23.1.16) An enum is
            // specified as a single byte 0x55 followed by a SerString.
            // 
            // tomat: The spec is missing ELEMENT_TYPE_TYPE.

            if (typeCode == SerializationTypeCode.SZArray)
            {
                if (isElementType)
                {
                    // nested array not allowed:
                    throw new UnsupportedSignatureContent();
                }

                SerializationTypeCode unusedElementTypeCode;
                TypeSymbol unusedElementType;
                DecodeCustomAttributeFieldOrPropTypeOrThrow(ref argReader, out elementTypeCode, out elementType, out unusedElementTypeCode, out unusedElementType, isElementType: true);
                type = GetSZArrayTypeSymbol(elementType, customModifiers: default(ImmutableArray<ModifierInfo<TypeSymbol>>));
                return;
            }

            elementTypeCode = SerializationTypeCode.Invalid;
            elementType = null;

            switch (typeCode)
            {
                case SerializationTypeCode.TaggedObject:
                    type = GetSpecialType(SpecialType.System_Object);
                    return;

                case SerializationTypeCode.Enum:
                    string enumTypeName;
                    if (!PEModule.CrackStringInAttributeValue(out enumTypeName, ref argReader))
                    {
                        throw new UnsupportedSignatureContent();
                    }

                    type = GetTypeSymbolForSerializedType(enumTypeName);
                    var underlyingType = GetEnumUnderlyingType(type);
                    if ((object)underlyingType == null)
                    {
                        throw new UnsupportedSignatureContent();
                    }

                    // GetEnumUnderlyingType always returns a valid enum underlying type
                    typeCode = underlyingType.SpecialType.ToSerializationType();
                    return;

                case SerializationTypeCode.Type:
                    type = SystemTypeSymbol;
                    return;

                case SerializationTypeCode.String:
                case SerializationTypeCode.Boolean:
                case SerializationTypeCode.Char:
                case SerializationTypeCode.SByte:
                case SerializationTypeCode.Byte:
                case SerializationTypeCode.Int16:
                case SerializationTypeCode.UInt16:
                case SerializationTypeCode.Int32:
                case SerializationTypeCode.UInt32:
                case SerializationTypeCode.Int64:
                case SerializationTypeCode.UInt64:
                case SerializationTypeCode.Single:
                case SerializationTypeCode.Double:
                    type = GetSpecialType(((SignatureTypeCode)typeCode).ToSpecialType());
                    return;
            }

            throw new UnsupportedSignatureContent();
        }

        /// <exception cref="UnsupportedSignatureContent">If the encoded attribute argument is invalid.</exception>
        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        private TypedConstant DecodeCustomAttributeFixedArgumentOrThrow(ref BlobReader sigReader, ref BlobReader argReader)
        {
            SerializationTypeCode typeCode, elementTypeCode;
            TypeSymbol type, elementType;
            DecodeCustomAttributeParameterTypeOrThrow(ref sigReader, out typeCode, out type, out elementTypeCode, out elementType, isElementType: false);

            // arrays are allowed only on top-level:
            if (typeCode == SerializationTypeCode.SZArray)
            {
                return DecodeCustomAttributeElementArrayOrThrow(ref argReader, elementTypeCode, elementType, type);
            }

            return DecodeCustomAttributeElementOrThrow(ref argReader, typeCode, type);
        }

        /// <exception cref="UnsupportedSignatureContent">If the encoded attribute argument is invalid.</exception>
        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        private TypedConstant DecodeCustomAttributeElementOrThrow(ref BlobReader argReader, SerializationTypeCode typeCode, TypeSymbol type)
        {
            if (typeCode == SerializationTypeCode.TaggedObject)
            {
                // Spec: If the parameter kind is System.Object, the value stored represents the "boxed" instance of that value-type.
                SerializationTypeCode elementTypeCode;
                TypeSymbol elementType;
                DecodeCustomAttributeFieldOrPropTypeOrThrow(ref argReader, out typeCode, out type, out elementTypeCode, out elementType, isElementType: false);

                if (typeCode == SerializationTypeCode.SZArray)
                {
                    return DecodeCustomAttributeElementArrayOrThrow(ref argReader, elementTypeCode, elementType, type);
                }
            }

            return DecodeCustomAttributePrimitiveElementOrThrow(ref argReader, typeCode, type);
        }

        /// <exception cref="UnsupportedSignatureContent">If the encoded attribute argument is invalid.</exception>
        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        private TypedConstant DecodeCustomAttributeElementArrayOrThrow(ref BlobReader argReader, SerializationTypeCode elementTypeCode, TypeSymbol elementType, TypeSymbol arrayType)
        {
            int count = argReader.ReadInt32();
            TypedConstant[] values;

            if (count == -1)
            {
                values = null;
            }
            else if (count == 0)
            {
                values = Array.Empty<TypedConstant>();
            }
            else
            {
                values = new TypedConstant[count];
                for (int i = 0; i < count; i++)
                {
                    values[i] = DecodeCustomAttributeElementOrThrow(ref argReader, elementTypeCode, elementType);
                }
            }

            return CreateArrayTypedConstant(arrayType, values.AsImmutableOrNull());
        }

        /// <exception cref="UnsupportedSignatureContent">If the given <paramref name="typeCode"/> is invalid.</exception>
        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        private TypedConstant DecodeCustomAttributePrimitiveElementOrThrow(ref BlobReader argReader, SerializationTypeCode typeCode, TypeSymbol type)
        {
            Debug.Assert(type != null);

            switch (typeCode)
            {
                case SerializationTypeCode.Boolean:
                    return CreateTypedConstant(type, GetPrimitiveOrEnumTypedConstantKind(type), argReader.ReadSByte() != 0);

                case SerializationTypeCode.SByte:
                    return CreateTypedConstant(type, GetPrimitiveOrEnumTypedConstantKind(type), argReader.ReadSByte());

                case SerializationTypeCode.Byte:
                    return CreateTypedConstant(type, GetPrimitiveOrEnumTypedConstantKind(type), argReader.ReadByte());

                case SerializationTypeCode.Int16:
                    return CreateTypedConstant(type, GetPrimitiveOrEnumTypedConstantKind(type), argReader.ReadInt16());

                case SerializationTypeCode.UInt16:
                    return CreateTypedConstant(type, GetPrimitiveOrEnumTypedConstantKind(type), argReader.ReadUInt16());

                case SerializationTypeCode.Int32:
                    return CreateTypedConstant(type, GetPrimitiveOrEnumTypedConstantKind(type), argReader.ReadInt32());

                case SerializationTypeCode.UInt32:
                    return CreateTypedConstant(type, GetPrimitiveOrEnumTypedConstantKind(type), argReader.ReadUInt32());

                case SerializationTypeCode.Int64:
                    return CreateTypedConstant(type, GetPrimitiveOrEnumTypedConstantKind(type), argReader.ReadInt64());

                case SerializationTypeCode.UInt64:
                    return CreateTypedConstant(type, GetPrimitiveOrEnumTypedConstantKind(type), argReader.ReadUInt64());

                case SerializationTypeCode.Single:
                    return CreateTypedConstant(type, GetPrimitiveOrEnumTypedConstantKind(type), argReader.ReadSingle());

                case SerializationTypeCode.Double:
                    return CreateTypedConstant(type, GetPrimitiveOrEnumTypedConstantKind(type), argReader.ReadDouble());

                case SerializationTypeCode.Char:
                    return CreateTypedConstant(type, GetPrimitiveOrEnumTypedConstantKind(type), argReader.ReadChar());

                case SerializationTypeCode.String:
                    string s;
                    TypedConstantKind kind = PEModule.CrackStringInAttributeValue(out s, ref argReader) ?
                        TypedConstantKind.Primitive :
                        TypedConstantKind.Error;

                    return CreateTypedConstant(type, kind, s);

                case SerializationTypeCode.Type:
                    string typeName;
                    TypeSymbol serializedType = PEModule.CrackStringInAttributeValue(out typeName, ref argReader) ?
                        (typeName != null ? GetTypeSymbolForSerializedType(typeName) : null) :
                        GetUnsupportedMetadataTypeSymbol();

                    return CreateTypedConstant(type, TypedConstantKind.Type, serializedType);

                default:
                    throw new UnsupportedSignatureContent();
            }
        }

        private static TypedConstantKind GetPrimitiveOrEnumTypedConstantKind(TypeSymbol type)
        {
            return (type.TypeKind == TypeKind.Enum) ? TypedConstantKind.Enum : TypedConstantKind.Primitive;
        }

        /// <exception cref="UnsupportedSignatureContent">If the encoded named argument is invalid.</exception>
        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        public (KeyValuePair<string, TypedConstant> nameValuePair, bool isProperty, SerializationTypeCode typeCode, SerializationTypeCode elementTypeCode) DecodeCustomAttributeNamedArgumentOrThrow(ref BlobReader argReader)
        {
            // Ecma-335 23.3 - A NamedArg is simply a FixedArg preceded by information to identify which field or
            // property it represents. [Note: Recall that the CLI allows fields and properties to have the same name; so
            // we require a means to disambiguate such situations. end note] FIELD is the single byte 0x53. PROPERTY is
            // the single byte 0x54.

            var kind = (CustomAttributeNamedArgumentKind)argReader.ReadCompressedInteger();
            if (kind != CustomAttributeNamedArgumentKind.Field && kind != CustomAttributeNamedArgumentKind.Property)
            {
                throw new UnsupportedSignatureContent();
            }

            SerializationTypeCode typeCode, elementTypeCode;
            TypeSymbol type, elementType;
            DecodeCustomAttributeFieldOrPropTypeOrThrow(ref argReader, out typeCode, out type, out elementTypeCode, out elementType, isElementType: false);

            string name;
            if (!PEModule.CrackStringInAttributeValue(out name, ref argReader))
            {
                throw new UnsupportedSignatureContent();
            }

            TypedConstant value = typeCode == SerializationTypeCode.SZArray
                ? DecodeCustomAttributeElementArrayOrThrow(ref argReader, elementTypeCode, elementType, type)
                : DecodeCustomAttributeElementOrThrow(ref argReader, typeCode, type);

            return (new KeyValuePair<string, TypedConstant>(name, value), kind == CustomAttributeNamedArgumentKind.Property, typeCode, elementTypeCode);
        }

        internal bool IsTargetAttribute(
            CustomAttributeHandle customAttribute,
            string namespaceName,
            string typeName,
            bool ignoreCase = false)
        {
            try
            {
                EntityHandle ctor;

                return Module.IsTargetAttribute(
                    customAttribute,
                    namespaceName,
                    typeName,
                    out ctor,
                    ignoreCase);
            }
            catch (BadImageFormatException)
            {
                return false;
            }
        }

        internal int GetTargetAttributeSignatureIndex(CustomAttributeHandle customAttribute, AttributeDescription description)
        {
            try
            {
                return Module.GetTargetAttributeSignatureIndex(customAttribute, description);
            }
            catch (BadImageFormatException)
            {
                return -1;
            }
        }

        internal bool GetCustomAttribute(
            CustomAttributeHandle handle,
            out TypedConstant[] positionalArgs,
            out KeyValuePair<string, TypedConstant>[] namedArgs)
        {
            try
            {
                positionalArgs = Array.Empty<TypedConstant>();
                namedArgs = Array.Empty<KeyValuePair<string, TypedConstant>>();

                // We could call decoder.GetSignature and use that to decode the arguments. However, materializing the
                // constructor signature is more work. We try to decode the arguments directly from the metadata bytes.
                EntityHandle attributeType;
                EntityHandle ctor;

                if (Module.GetTypeAndConstructor(handle, out attributeType, out ctor))
                {
                    BlobReader argsReader = Module.GetMemoryReaderOrThrow(Module.GetCustomAttributeValueOrThrow(handle));
                    BlobReader sigReader = Module.GetMemoryReaderOrThrow(Module.GetMethodSignatureOrThrow(ctor));

                    uint prolog = argsReader.ReadUInt16();
                    if (prolog != 1)
                    {
                        return false;
                    }

                    // Read the signature header.
                    SignatureHeader signatureHeader = sigReader.ReadSignatureHeader();

                    // Get the type parameter count.
                    if (signatureHeader.IsGeneric && sigReader.ReadCompressedInteger() != 0)
                    {
                        return false;
                    }

                    // Get the parameter count
                    int paramCount = sigReader.ReadCompressedInteger();

                    // Get the type return type.
                    var returnTypeCode = sigReader.ReadSignatureTypeCode();
                    if (returnTypeCode != SignatureTypeCode.Void)
                    {
                        return false;
                    }

                    if (paramCount > 0)
                    {
                        positionalArgs = new TypedConstant[paramCount];

                        for (int i = 0; i < positionalArgs.Length; i++)
                        {
                            positionalArgs[i] = DecodeCustomAttributeFixedArgumentOrThrow(ref sigReader, ref argsReader);
                        }
                    }

                    short namedParamCount = argsReader.ReadInt16();

                    if (namedParamCount > 0)
                    {
                        namedArgs = new KeyValuePair<string, TypedConstant>[namedParamCount];

                        for (int i = 0; i < namedArgs.Length; i++)
                        {
                            (namedArgs[i], _, _, _) = DecodeCustomAttributeNamedArgumentOrThrow(ref argsReader);
                        }
                    }

                    return true;
                }
            }
            catch (Exception e) when (e is UnsupportedSignatureContent || e is BadImageFormatException)
            {
                positionalArgs = Array.Empty<TypedConstant>();
                namedArgs = Array.Empty<KeyValuePair<String, TypedConstant>>();
            }

            return false;
        }

#nullable enable
        internal bool GetCustomAttribute(CustomAttributeHandle handle, [NotNullWhen(true)] out TypeSymbol? attributeClass, [NotNullWhen(true)] out MethodSymbol? attributeCtor)
        {
            EntityHandle attributeType;
            EntityHandle ctor;

            try
            {
                if (!Module.GetTypeAndConstructor(handle, out attributeType, out ctor))
                {
                    attributeClass = null;
                    attributeCtor = null;
                    return false;
                }
            }
            catch (BadImageFormatException)
            {
                attributeClass = null;
                attributeCtor = null;
                return false;
            }

            attributeClass = GetTypeOfToken(attributeType);
            attributeCtor = GetMethodSymbolForMethodDefOrMemberRef(ctor, attributeClass);
            return true;
        }
#nullable disable

        internal bool GetCustomAttributeWellKnownType(CustomAttributeHandle handle, out WellKnownType wellKnownAttribute)
        {
            wellKnownAttribute = WellKnownType.Unknown;

            try
            {
                EntityHandle attributeType;
                EntityHandle ctor;

                if (!Module.GetTypeAndConstructor(handle, out attributeType, out ctor))
                {
                    return false;
                }

                StringHandle namespaceHandle;
                StringHandle nameHandle;
                if (!Module.GetAttributeNamespaceAndName(attributeType, out namespaceHandle, out nameHandle))
                {
                    return false;
                }

                string fullName = Module.GetFullNameOrThrow(namespaceHandle, nameHandle);
                wellKnownAttribute = WellKnownTypes.GetTypeFromMetadataName(fullName);
                return true;
            }
            catch (BadImageFormatException)
            {
                return false;
            }
        }

        #endregion

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        private TypeSymbol[] DecodeMethodSpecTypeArgumentsOrThrow(BlobHandle signature)
        {
            SignatureHeader signatureHeader;
            var signatureReader = DecodeSignatureHeaderOrThrow(signature, out signatureHeader);
            if (signatureHeader.Kind != SignatureKind.MethodSpecification)
            {
                throw new BadImageFormatException();
            }

            int argumentCount = signatureReader.ReadCompressedInteger();
            if (argumentCount == 0)
            {
                throw new BadImageFormatException();
            }

            var result = new TypeSymbol[argumentCount];
            for (int i = 0; i < result.Length; i++)
            {
                bool refersToNoPiaLocalType;
                result[i] = DecodeTypeOrThrow(ref signatureReader, out refersToNoPiaLocalType);
            }

            return result;
        }

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        internal BlobReader DecodeSignatureHeaderOrThrow(BlobHandle signature, out SignatureHeader signatureHeader)
        {
            return DecodeSignatureHeaderOrThrow(Module, signature, out signatureHeader);
        }

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        internal static BlobReader DecodeSignatureHeaderOrThrow(PEModule module, BlobHandle signature, out SignatureHeader signatureHeader)
        {
            BlobReader reader = module.GetMemoryReaderOrThrow(signature);
            signatureHeader = reader.ReadSignatureHeader();
            return reader;
        }

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        protected ParamInfo<TypeSymbol>[] DecodeSignatureParametersOrThrow(ref BlobReader signatureReader, SignatureHeader signatureHeader, out int typeParameterCount, bool shouldProcessAllBytes = true, bool isFunctionPointerSignature = false)
        {
            int paramCount;
            GetSignatureCountsOrThrow(ref signatureReader, signatureHeader, out paramCount, out typeParameterCount);

            ParamInfo<TypeSymbol>[] paramInfo = new ParamInfo<TypeSymbol>[paramCount + 1];

            uint paramIndex = 0;

            try
            {
                // get the return type
                DecodeParameterOrThrow(ref signatureReader, ref paramInfo[0]);

                // Get all of the parameters.
                for (paramIndex = 1; paramIndex <= paramCount; paramIndex++)
                {
                    // Figure out the type.
                    DecodeParameterOrThrow(ref signatureReader, ref paramInfo[paramIndex]);
                }

                if (shouldProcessAllBytes && signatureReader.RemainingBytes > 0)
                {
                    throw new UnsupportedSignatureContent();
                }
            }
            catch (Exception e) when ((e is UnsupportedSignatureContent || e is BadImageFormatException) && !isFunctionPointerSignature)
            {
                for (; paramIndex <= paramCount; paramIndex++)
                {
                    paramInfo[paramIndex].Type = GetUnsupportedMetadataTypeSymbol(e as BadImageFormatException);
                }
            }

            return paramInfo;
        }

        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        private static void GetSignatureCountsOrThrow(ref BlobReader signatureReader, SignatureHeader signatureHeader, out int parameterCount, out int typeParameterCount)
        {
            // Get the type parameter count.
            typeParameterCount = signatureHeader.IsGeneric ? signatureReader.ReadCompressedInteger() : 0;

            // Get the parameter count
            parameterCount = signatureReader.ReadCompressedInteger();
        }

        internal FieldInfo<TypeSymbol> DecodeFieldSignature(FieldDefinitionHandle fieldHandle)
        {
            try
            {
                BlobHandle signature = Module.GetFieldSignatureOrThrow(fieldHandle);

                SignatureHeader signatureHeader;
                BlobReader signatureReader = DecodeSignatureHeaderOrThrow(signature, out signatureHeader);

                if (signatureHeader.Kind != SignatureKind.Field)
                {
                    return new FieldInfo<TypeSymbol>(GetUnsupportedMetadataTypeSymbol()); // unsupported signature content
                }
                else
                {
                    return DecodeFieldSignature(ref signatureReader);
                }
            }
            catch (BadImageFormatException mrEx)
            {
                return new FieldInfo<TypeSymbol>(GetUnsupportedMetadataTypeSymbol(mrEx));
            }
        }

        // MetaImport::DecodeFieldSignature
        protected FieldInfo<TypeSymbol> DecodeFieldSignature(ref BlobReader signatureReader)
        {
            try
            {
                SignatureTypeCode typeCode;
                bool isByRef = false;
                ImmutableArray<ModifierInfo<TypeSymbol>> refCustomModifiers = default;
                ImmutableArray<ModifierInfo<TypeSymbol>> customModifiers = DecodeModifiersOrThrow(
                    ref signatureReader,
                    out typeCode);

                if (typeCode == SignatureTypeCode.ByReference)
                {
                    isByRef = true;
                    refCustomModifiers = customModifiers;
                    customModifiers = DecodeModifiersOrThrow(
                        ref signatureReader,
                        out typeCode);
                }

                var type = DecodeTypeOrThrow(ref signatureReader, typeCode, out _);
                return new FieldInfo<TypeSymbol>(isByRef, refCustomModifiers, type, customModifiers);
            }
            catch (UnsupportedSignatureContent)
            {
                return new FieldInfo<TypeSymbol>(GetUnsupportedMetadataTypeSymbol()); // unsupported signature content
            }
            catch (BadImageFormatException mrEx)
            {
                return new FieldInfo<TypeSymbol>(GetUnsupportedMetadataTypeSymbol(mrEx));
            }
        }

        /// <summary>
        /// Find the methods that a given method explicitly overrides.
        /// </summary>
        /// <remarks>
        /// Methods may be on class or interfaces.
        /// Containing classes/interfaces will be supertypes of the implementing type.
        /// </remarks>
        /// <param name="implementingTypeDef">TypeDef handle of the implementing type.</param>
        /// <param name="implementingMethodDef">MethodDef handle of the implementing method.</param>
        /// <param name="implementingTypeSymbol">The type symbol for the implementing type.</param>
        /// <returns>Array of implemented methods.</returns>
        internal ImmutableArray<MethodSymbol> GetExplicitlyOverriddenMethods(TypeDefinitionHandle implementingTypeDef, MethodDefinitionHandle implementingMethodDef, TypeSymbol implementingTypeSymbol)
        {
            ArrayBuilder<MethodSymbol> resultBuilder = ArrayBuilder<MethodSymbol>.GetInstance();

            try
            {
                foreach (var methodImpl in Module.GetMethodImplementationsOrThrow(implementingTypeDef))
                {
                    EntityHandle methodDebugHandle;
                    EntityHandle implementedMethodHandle;
                    Module.GetMethodImplPropsOrThrow(methodImpl, out methodDebugHandle, out implementedMethodHandle);

                    // Though it is rare in practice, the spec allows the MethodImpl table to represent
                    // methods defined in the current module as MemberRefs rather than MethodDefs.
                    if (methodDebugHandle.Kind == HandleKind.MemberReference)
                    {
                        MethodSymbol methodBodySymbol = GetMethodSymbolForMemberRef((MemberReferenceHandle)methodDebugHandle, implementingTypeSymbol);
                        if (methodBodySymbol != null)
                        {
                            // Note: this might have a nil row ID, but that won't cause a problem
                            // since it will simply fail to be equal to the implementingMethodToken.
                            methodDebugHandle = GetMethodHandle(methodBodySymbol);
                        }
                    }

                    if (methodDebugHandle == implementingMethodDef)
                    {
                        if (!implementedMethodHandle.IsNil)
                        {
                            HandleKind implementedMethodTokenType = implementedMethodHandle.Kind;

                            MethodSymbol methodSymbol = null;

                            if (implementedMethodTokenType == HandleKind.MethodDefinition)
                            {
                                methodSymbol = FindMethodSymbolInSuperType(implementingTypeDef, (MethodDefinitionHandle)implementedMethodHandle);
                            }
                            else if (implementedMethodTokenType == HandleKind.MemberReference)
                            {
                                methodSymbol = GetMethodSymbolForMemberRef((MemberReferenceHandle)implementedMethodHandle, implementingTypeSymbol);
                            }

                            if (methodSymbol != null)
                            {
                                resultBuilder.Add(methodSymbol);
                            }
                        }
                    }
                }
            }
            catch (BadImageFormatException)
            { }

            return resultBuilder.ToImmutableAndFree();
        }

        /// <summary>
        /// Search for the <typeparamref name="MethodSymbol"/> corresponding to the given MethodDef token. Search amongst
        /// the supertypes (classes and interfaces) of a designated type.
        /// </summary>
        /// <remarks>
        /// Generally, the type will be a type that explicitly implements an interface and the method will be the
        /// implemented method (i.e. on the interface).
        /// </remarks>
        /// <param name="searchTypeDef">TypeDef token of the type from which the search should begin.</param>
        /// <param name="targetMethodDef">MethodDef token of the target method.</param>
        /// <returns>Corresponding <typeparamref name="MethodSymbol"/> or null, if none is found.</returns>
        private MethodSymbol FindMethodSymbolInSuperType(TypeDefinitionHandle searchTypeDef, MethodDefinitionHandle targetMethodDef)
        {
            try
            {
                // We're using queues (i.e. BFS), rather than stacks (i.e. DFS), because we expect the common case
                // to be implementing a method on an immediate supertype, rather than a remote ancestor.
                // We're using more than one queue for two reasons: 1) some of our TypeDef tokens come directly from the
                // metadata tables and we'd prefer not to manipulate the corresponding symbol objects; 2) we bump TypeDefs
                // to the front of the search order (i.e. ahead of symbols) because a MethodDef can correspond to a TypeDef
                // but not to a type ref (i.e. symbol).
                Queue<TypeDefinitionHandle> typeDefsToSearch = new Queue<TypeDefinitionHandle>();
                Queue<TypeSymbol> typeSymbolsToSearch = new Queue<TypeSymbol>();

                // A method def represents a method defined in this module, so we can
                // just search the method defs of this module.
                EnqueueTypeDefInterfacesAndBaseTypeOrThrow(typeDefsToSearch, typeSymbolsToSearch, searchTypeDef);

                //catch both cycles and duplicate interfaces
                HashSet<TypeDefinitionHandle> visitedTypeDefTokens = new HashSet<TypeDefinitionHandle>();
                HashSet<TypeSymbol> visitedTypeSymbols = new HashSet<TypeSymbol>();

                bool hasMoreTypeDefs;
                while ((hasMoreTypeDefs = (typeDefsToSearch.Count > 0)) || typeSymbolsToSearch.Count > 0)
                {
                    if (hasMoreTypeDefs)
                    {
                        TypeDefinitionHandle typeDef = typeDefsToSearch.Dequeue();
                        Debug.Assert(!typeDef.IsNil);

                        if (visitedTypeDefTokens.Add(typeDef))
                        {
                            foreach (MethodDefinitionHandle methodDef in Module.GetMethodsOfTypeOrThrow(typeDef))
                            {
                                if (methodDef == targetMethodDef)
                                {
                                    TypeSymbol typeSymbol = this.GetTypeOfToken(typeDef);
                                    return FindMethodSymbolInType(typeSymbol, targetMethodDef);
                                }
                            }

                            EnqueueTypeDefInterfacesAndBaseTypeOrThrow(typeDefsToSearch, typeSymbolsToSearch, typeDef);
                        }
                    }
                    else //has more type symbols
                    {
                        TypeSymbol typeSymbol = typeSymbolsToSearch.Dequeue();
                        Debug.Assert(typeSymbol != null);

                        if (visitedTypeSymbols.Add(typeSymbol))
                        {
                            //we're looking for a method def but we're currently on a type *ref*, so just enqueue supertypes
                            EnqueueTypeSymbolInterfacesAndBaseTypes(typeDefsToSearch, typeSymbolsToSearch, typeSymbol);
                        }
                    }
                }
            }
            catch (BadImageFormatException)
            { }

            return null;
        }

        /// <summary>
        /// Enqueue the interfaces implemented and the type extended by a given TypeDef.
        /// </summary>
        /// <param name="typeDefsToSearch">Queue of TypeDefs to search.</param>
        /// <param name="typeSymbolsToSearch">Queue of TypeSymbols (representing typeRefs to search).</param>
        /// <param name="searchTypeDef">Handle of the TypeDef for which we want to enqueue supertypes.</param>
        /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
        private void EnqueueTypeDefInterfacesAndBaseTypeOrThrow(Queue<TypeDefinitionHandle> typeDefsToSearch, Queue<TypeSymbol> typeSymbolsToSearch, TypeDefinitionHandle searchTypeDef)
        {
            foreach (var interfaceImplHandle in Module.GetInterfaceImplementationsOrThrow(searchTypeDef))
            {
                var interfaceImpl = Module.MetadataReader.GetInterfaceImplementation(interfaceImplHandle);
                EnqueueTypeToken(typeDefsToSearch, typeSymbolsToSearch, interfaceImpl.Interface);
            }

            EnqueueTypeToken(typeDefsToSearch, typeSymbolsToSearch, Module.GetBaseTypeOfTypeOrThrow(searchTypeDef));
        }

        /// <summary>
        /// Helper method for enqueuing a type token in the right queue.
        /// Def -> typeDefsToSearch
        /// Ref -> typeSymbolsToSearch
        /// null -> neither
        /// </summary>
        private void EnqueueTypeToken(Queue<TypeDefinitionHandle> typeDefsToSearch, Queue<TypeSymbol> typeSymbolsToSearch, EntityHandle typeToken)
        {
            if (!typeToken.IsNil)
            {
                if (typeToken.Kind == HandleKind.TypeDefinition)
                {
                    typeDefsToSearch.Enqueue((TypeDefinitionHandle)typeToken);
                }
                else
                {
                    EnqueueTypeSymbol(typeDefsToSearch, typeSymbolsToSearch, this.GetTypeOfToken(typeToken));
                }
            }
        }

        /// <summary>
        /// Enqueue the interfaces implemented and the type extended by a given TypeDef.
        /// </summary>
        /// <param name="typeDefsToSearch">Queue of TypeDefs to search.</param>
        /// <param name="typeSymbolsToSearch">Queue of TypeSymbols (representing typeRefs to search).</param>
        /// <param name="typeSymbol">Symbol for which we want to enqueue supertypes.</param>
        protected abstract void EnqueueTypeSymbolInterfacesAndBaseTypes(Queue<TypeDefinitionHandle> typeDefsToSearch, Queue<TypeSymbol> typeSymbolsToSearch, TypeSymbol typeSymbol);

        /// <summary>
        /// Enqueue the given type as either a def or a ref.
        /// </summary>
        /// <param name="typeDefsToSearch">Queue of TypeDefs to search.</param>
        /// <param name="typeSymbolsToSearch">Queue of TypeSymbols (representing typeRefs to search).</param>
        /// <param name="typeSymbol">Symbol to enqueue.</param>
        protected abstract void EnqueueTypeSymbol(Queue<TypeDefinitionHandle> typeDefsToSearch, Queue<TypeSymbol> typeSymbolsToSearch, TypeSymbol typeSymbol);

        /// <summary>
        /// Search the members of a TypeSymbol to find the one that matches a given MethodDef token.
        /// </summary>
        /// <param name="type">Type to search for method.</param>
        /// <param name="methodDef">MethodDef handle of the method to find.</param>
        /// <returns>The corresponding MethodSymbol or null.</returns>
        protected abstract MethodSymbol FindMethodSymbolInType(TypeSymbol type, MethodDefinitionHandle methodDef);

        /// <summary>
        /// Search the members of a TypeSymbol to find the one that matches a given FieldDef token.
        /// </summary>
        /// <param name="type">Type to search for field.</param>
        /// <param name="fieldDef">FieldDef handle of the field to find.</param>
        /// <returns>The corresponding FieldSymbol or null.</returns>
        protected abstract FieldSymbol FindFieldSymbolInType(TypeSymbol type, FieldDefinitionHandle fieldDef);

        /// <summary>
        /// Given a MemberRef token for a method, we can find a corresponding MethodSymbol by
        /// searching for the name and signature.
        /// </summary>
        /// <param name="memberRef">A MemberRef token for a method.</param>
        /// <param name="implementingTypeSymbol">Scope the search to supertypes of the implementing type.</param>
        /// <param name="methodsOnly">True to only return method symbols, null if the token resolves to a field.</param>
        /// <returns>The corresponding MethodSymbol or null.</returns>
        internal abstract Symbol GetSymbolForMemberRef(MemberReferenceHandle memberRef, TypeSymbol implementingTypeSymbol = null, bool methodsOnly = false);

        internal MethodSymbol GetMethodSymbolForMemberRef(MemberReferenceHandle methodRef, TypeSymbol implementingTypeSymbol)
        {
            return (MethodSymbol)GetSymbolForMemberRef(methodRef, implementingTypeSymbol, methodsOnly: true);
        }

        internal FieldSymbol GetFieldSymbolForMemberRef(MemberReferenceHandle methodRef, TypeSymbol implementingTypeSymbol)
        {
            return (FieldSymbol)GetSymbolForMemberRef(methodRef, implementingTypeSymbol, methodsOnly: true);
        }

        protected override bool IsContainingAssembly(AssemblyIdentity identity)
        {
            return _containingAssemblyIdentity != null && _containingAssemblyIdentity.Equals(identity);
        }

        /// <summary>
        /// Given a method symbol, return the MethodDef token, if it is defined in
        /// this module, or a nil token, otherwise.
        /// </summary>
        /// <param name="method">The method symbol for which to return a MethodDef token.</param>
        /// <returns>A MethodDef token or nil.</returns>
        protected abstract MethodDefinitionHandle GetMethodHandle(MethodSymbol method);

        protected abstract ConcurrentDictionary<TypeDefinitionHandle, TypeSymbol> GetTypeHandleToTypeMap();
        protected abstract ConcurrentDictionary<TypeReferenceHandle, TypeSymbol> GetTypeRefHandleToTypeMap();

        protected abstract TypeSymbol SubstituteNoPiaLocalType(TypeDefinitionHandle typeDef, ref MetadataTypeName name, string interfaceGuid, string scope, string identifier);

        protected abstract TypeSymbol LookupTopLevelTypeDefSymbol(string moduleName, ref MetadataTypeName emittedName, out bool isNoPiaLocalType);

        protected abstract TypeSymbol GetGenericTypeParamSymbol(int position);
        protected abstract TypeSymbol GetGenericMethodTypeParamSymbol(int position);

        private static TypedConstant CreateArrayTypedConstant(TypeSymbol type, ImmutableArray<TypedConstant> array)
        {
            if (type.TypeKind == TypeKind.Error)
            {
                return new TypedConstant(type, TypedConstantKind.Error, null);
            }

            Debug.Assert(type.TypeKind == TypeKind.Array);
            return new TypedConstant(type, array);
        }

        private static TypedConstant CreateTypedConstant(TypeSymbol type, TypedConstantKind kind, object value)
        {
            if (type.TypeKind == TypeKind.Error)
            {
                return new TypedConstant(type, TypedConstantKind.Error, null);
            }

            return new TypedConstant(type, kind, value);
        }

        private static TypedConstant CreateTypedConstant(TypeSymbol type, TypedConstantKind kind, bool value)
        {
            return CreateTypedConstant(type, kind, Boxes.Box(value));
        }

        /// <summary>
        /// Returns a symbol that given token resolves to or null of the token represents an entity that isn't represented by a symbol,
        /// such as vararg MemberRef.
        /// </summary>
        internal Symbol GetSymbolForILToken(EntityHandle token)
        {
            try
            {
                switch (token.Kind)
                {
                    case HandleKind.TypeDefinition:
                    case HandleKind.TypeSpecification:
                    case HandleKind.TypeReference:
                        return GetTypeOfToken(token);

                    case HandleKind.MethodDefinition:
                        {
                            TypeDefinitionHandle typeDef = Module.FindContainingTypeOrThrow((MethodDefinitionHandle)token);

                            if (typeDef.IsNil)
                            {
                                // error
                                return null;
                            }

                            TypeSymbol type = GetTypeOfTypeDef(typeDef);
                            if (type == null)
                            {
                                // error
                                return null;
                            }

                            return GetMethodSymbolForMethodDefOrMemberRef(token, type);
                        }

                    case HandleKind.FieldDefinition:
                        {
                            TypeDefinitionHandle typeDef = Module.FindContainingTypeOrThrow((FieldDefinitionHandle)token);
                            if (typeDef.IsNil)
                            {
                                // error
                                return null;
                            }

                            TypeSymbol type = GetTypeOfToken(typeDef);
                            if (type == null)
                            {
                                // error
                                return null;
                            }

                            return GetFieldSymbolForFieldDefOrMemberRef(token, type);
                        }

                    case HandleKind.MethodSpecification:
                        EntityHandle method;
                        BlobHandle instantiation;
                        this.Module.GetMethodSpecificationOrThrow((MethodSpecificationHandle)token, out method, out instantiation);

                        var genericDefinition = (MethodSymbol)GetSymbolForILToken(method);
                        if (genericDefinition == null)
                        {
                            // error
                            return null;
                        }

                        var genericArguments = DecodeMethodSpecTypeArgumentsOrThrow(instantiation);
                        return (MethodSymbol)genericDefinition.Construct(genericArguments);

                    case HandleKind.MemberReference:
                        return GetSymbolForMemberRef((MemberReferenceHandle)token);
                }
            }
            catch (BadImageFormatException)
            { }

            // error: unexpected token in IL
            return null;
        }

        /// <summary>
        /// Given a MemberRef token, return the TypeSymbol for its Class field.
        /// </summary>
        internal TypeSymbol GetMemberRefTypeSymbol(MemberReferenceHandle memberRef)
        {
            try
            {
                EntityHandle container = Module.GetContainingTypeOrThrow(memberRef);

                HandleKind containerType = container.Kind;
                Debug.Assert(
                    containerType == HandleKind.MethodDefinition ||
                    containerType == HandleKind.ModuleReference ||
                    containerType == HandleKind.TypeDefinition ||
                    containerType == HandleKind.TypeReference ||
                    containerType == HandleKind.TypeSpecification);

                if (containerType != HandleKind.TypeDefinition &&
                    containerType != HandleKind.TypeReference &&
                    containerType != HandleKind.TypeSpecification)
                {
                    // C# symbols don't support global methods
                    return null;
                }

                return this.GetTypeOfToken(container);
            }
            catch (BadImageFormatException)
            {
                return null;
            }
        }

        internal MethodSymbol GetMethodSymbolForMethodDefOrMemberRef(EntityHandle memberToken, TypeSymbol container)
        {
            HandleKind type = memberToken.Kind;
            Debug.Assert(type == HandleKind.MethodDefinition || type == HandleKind.MemberReference);

            return type == HandleKind.MethodDefinition
                ? FindMethodSymbolInType(container, (MethodDefinitionHandle)memberToken)
                : GetMethodSymbolForMemberRef((MemberReferenceHandle)memberToken, container);
        }

        internal FieldSymbol GetFieldSymbolForFieldDefOrMemberRef(EntityHandle memberToken, TypeSymbol container)
        {
            HandleKind type = memberToken.Kind;
            Debug.Assert(type == HandleKind.FieldDefinition ||
                            type == HandleKind.MemberReference);

            return type == HandleKind.FieldDefinition
                ? FindFieldSymbolInType(container, (FieldDefinitionHandle)memberToken)
                : GetFieldSymbolForMemberRef((MemberReferenceHandle)memberToken, container);
        }

        /// <summary>
        /// Checks whether signatures match where the signatures are either from a property
        /// and an accessor or two accessors. When comparing a property or getter to setter, the
        /// setter signature must be the second argument and 'comparingToSetter' must be true.
        /// </summary>
        /// <param name="signature1">
        /// Signature of the property containing the accessor, or the getter (type, then parameters).
        /// </param>
        /// <param name="signature2">
        /// Signature of the accessor when comparing property and accessor,
        /// or the setter when comparing getter and setter (return type and then parameters).
        /// </param>
        /// <param name="comparingToSetter">
        /// True when comparing a property or getter to a setter, false otherwise.
        /// </param>
        /// <param name="compareParamByRef">
        /// True if differences in IsByRef for parameters should be treated as significant.
        /// </param>
        /// <param name="compareReturnType">
        /// True if differences in return type (or value parameter for setter) should be treated as significant.
        /// </param>
        /// <returns>True if the accessor signature is appropriate for the containing property.</returns>
        internal bool DoPropertySignaturesMatch(ParamInfo<TypeSymbol>[] signature1, ParamInfo<TypeSymbol>[] signature2, bool comparingToSetter, bool compareParamByRef, bool compareReturnType)
        {
            int additionalParamCount = (comparingToSetter ? 1 : 0);

            // Check the number of parameters.
            if ((signature2.Length - additionalParamCount) != signature1.Length)
            {
                return false;
            }

            // Check the setter has a void type.
            if (comparingToSetter &&
                (GetPrimitiveTypeCode(signature2[0].Type) != Cci.PrimitiveTypeCode.Void))
            {
                return false;
            }

            // Check the type of each parameter.
            for (int paramIndex1 = compareReturnType ? 0 : 1; paramIndex1 < signature1.Length; paramIndex1++)
            {
                int paramIndex2 =
                    ((paramIndex1 == 0) && comparingToSetter) ?
                    signature1.Length :
                    paramIndex1;
                var param1 = signature1[paramIndex1];
                var param2 = signature2[paramIndex2];
                if (compareParamByRef && (param2.IsByRef != param1.IsByRef))
                {
                    return false;
                }
                if (!param2.Type.Equals(param1.Type, TypeCompareKind.ConsiderEverything))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Check whether an event accessor has an appropriate signature.
        /// </summary>
        /// <param name="eventType">Type of the event containing the accessor.</param>
        /// <param name="methodParams">Signature of the accessor (return type and then parameters).</param>
        /// <returns>True if the accessor signature is appropriate for the containing event.</returns>
        internal bool DoesSignatureMatchEvent(TypeSymbol eventType, ParamInfo<TypeSymbol>[] methodParams)
        {
            // Check the number of parameters.
            if (methodParams.Length != 2)
            {
                return false;
            }

            // Check the accessor has a void type.
            if (GetPrimitiveTypeCode(methodParams[0].Type) != Cci.PrimitiveTypeCode.Void)
            {
                return false;
            }

            var methodParam = methodParams[1];
            return !methodParam.IsByRef && methodParam.Type.Equals(eventType);
        }
    }
}
