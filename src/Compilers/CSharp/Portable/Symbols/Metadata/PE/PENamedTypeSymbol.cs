// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Reflection.Metadata;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.DocumentationComments;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE
{
    /// <summary>
    /// The class to represent all types imported from a PE/module.
    /// </summary>
    internal abstract class PENamedTypeSymbol : NamedTypeSymbol
    {
        private static readonly Dictionary<string, ImmutableArray<PENamedTypeSymbol>> s_emptyNestedTypes = new Dictionary<string, ImmutableArray<PENamedTypeSymbol>>(EmptyComparer.Instance);

        private readonly NamespaceOrTypeSymbol _container;
        private readonly TypeDefinitionHandle _handle;
        private readonly string _name;
        private readonly TypeAttributes _flags;
        private readonly SpecialType _corTypeId;

        /// <summary>
        /// A set of all the names of the members in this type.
        /// We can get names without getting members (which is a more expensive operation)
        /// </summary>
        private ICollection<string> _lazyMemberNames;

        /// <summary>
        /// We used to sort symbols on demand and relied on row ids to figure out the order between symbols of the same kind.
        /// However, that was fragile because, when map tables are used in metadata, row ids in the map table define the order
        /// and we don't have them.
        /// Members are grouped by kind. First we store fields, then methods, then properties, then events and finally nested types.
        /// Within groups, members are sorted based on declaration order.
        /// </summary>
        private ImmutableArray<Symbol> _lazyMembersInDeclarationOrder;

        /// <summary>
        /// A map of members immediately contained within this type 
        /// grouped by their name (case-sensitively).
        /// </summary>
        private Dictionary<string, ImmutableArray<Symbol>> _lazyMembersByName;

        /// <summary>
        /// A map of types immediately contained within this type 
        /// grouped by their name (case-sensitively).
        /// </summary>
        private Dictionary<string, ImmutableArray<PENamedTypeSymbol>> _lazyNestedTypes;

        /// <summary>
        /// Lazily initialized by TypeKind property.
        /// </summary>
        private TypeKind _lazyKind;

        private NullableContextKind _lazyNullableContextValue;

        private NamedTypeSymbol _lazyBaseType = ErrorTypeSymbol.UnknownResultType;
        private ImmutableArray<NamedTypeSymbol> _lazyInterfaces = default(ImmutableArray<NamedTypeSymbol>);
        private NamedTypeSymbol _lazyDeclaredBaseType = ErrorTypeSymbol.UnknownResultType;
        private ImmutableArray<NamedTypeSymbol> _lazyDeclaredInterfaces = default(ImmutableArray<NamedTypeSymbol>);

        private Tuple<CultureInfo, string> _lazyDocComment;

        private DiagnosticInfo _lazyUseSiteDiagnostic = CSDiagnosticInfo.EmptyErrorInfo; // Indicates unknown state. 

        // There is a bunch of type properties relevant only for enums or types with custom attributes.
        // It is fairly easy to check whether a type s is not "uncommon". So we store uncommon properties in 
        // a separate class with a noUncommonProperties singleton used for cases when type is "common".
        // this is done purely to save memory with expectation that "uncommon" cases are indeed uncommon. 
        #region "Uncommon properties"
        private static readonly UncommonProperties s_noUncommonProperties = new UncommonProperties();
        private UncommonProperties _lazyUncommonProperties;

        private UncommonProperties GetUncommonProperties()
        {
            var result = _lazyUncommonProperties;
            if (result != null)
            {
                Debug.Assert(result != s_noUncommonProperties || result.IsDefaultValue(), "default value was modified");
                return result;
            }

            if (this.IsUncommon())
            {
                result = new UncommonProperties();
                return Interlocked.CompareExchange(ref _lazyUncommonProperties, result, null) ?? result;
            }

            _lazyUncommonProperties = result = s_noUncommonProperties;
            return result;
        }

        // enums and types with custom attributes are considered uncommon
        private bool IsUncommon()
        {
            if (this.ContainingPEModule.HasAnyCustomAttributes(_handle))
            {
                return true;
            }

            if (this.TypeKind == TypeKind.Enum)
            {
                return true;
            }

            return false;
        }

        private class UncommonProperties
        {
            /// <summary>
            /// Need to import them for an enum from a linked assembly, when we are embedding it. These symbols are not included into lazyMembersInDeclarationOrder.  
            /// </summary>
            internal ImmutableArray<PEFieldSymbol> lazyInstanceEnumFields;
            internal NamedTypeSymbol lazyEnumUnderlyingType;

            // CONSIDER: Should we use a CustomAttributeBag for PE symbols?
            internal ImmutableArray<CSharpAttributeData> lazyCustomAttributes;
            internal ImmutableArray<string> lazyConditionalAttributeSymbols;
            internal ObsoleteAttributeData lazyObsoleteAttributeData = ObsoleteAttributeData.Uninitialized;
            internal AttributeUsageInfo lazyAttributeUsageInfo = AttributeUsageInfo.Null;
            internal ThreeState lazyContainsExtensionMethods;
            internal ThreeState lazyIsByRefLike;
            internal ThreeState lazyIsReadOnly;
            internal string lazyDefaultMemberName;
            internal NamedTypeSymbol lazyComImportCoClassType = ErrorTypeSymbol.UnknownResultType;
            internal ThreeState lazyHasEmbeddedAttribute = ThreeState.Unknown;

            internal bool IsDefaultValue()
            {
                return lazyInstanceEnumFields.IsDefault &&
                    (object)lazyEnumUnderlyingType == null &&
                    lazyCustomAttributes.IsDefault &&
                    lazyConditionalAttributeSymbols.IsDefault &&
                    lazyObsoleteAttributeData == ObsoleteAttributeData.Uninitialized &&
                    lazyAttributeUsageInfo.IsNull &&
                    !lazyContainsExtensionMethods.HasValue() &&
                    lazyDefaultMemberName == null &&
                    (object)lazyComImportCoClassType == (object)ErrorTypeSymbol.UnknownResultType &&
                    !lazyHasEmbeddedAttribute.HasValue();
            }
        }

        #endregion  // Uncommon properties

        internal static PENamedTypeSymbol Create(
            PEModuleSymbol moduleSymbol,
            PENamespaceSymbol containingNamespace,
            TypeDefinitionHandle handle,
            string emittedNamespaceName)
        {
            GenericParameterHandleCollection genericParameterHandles;
            ushort arity;
            BadImageFormatException mrEx = null;

            GetGenericInfo(moduleSymbol, handle, out genericParameterHandles, out arity, out mrEx);

            bool mangleName;
            PENamedTypeSymbol result;

            if (arity == 0)
            {
                result = new PENamedTypeSymbolNonGeneric(moduleSymbol, containingNamespace, handle, emittedNamespaceName, out mangleName);
            }
            else
            {
                result = new PENamedTypeSymbolGeneric(
                    moduleSymbol,
                    containingNamespace,
                    handle,
                    emittedNamespaceName,
                    genericParameterHandles,
                    arity,
                    out mangleName);
            }

            if (mrEx != null)
            {
                result._lazyUseSiteDiagnostic = new CSDiagnosticInfo(ErrorCode.ERR_BogusType, result);
            }

            return result;
        }

        private static void GetGenericInfo(PEModuleSymbol moduleSymbol, TypeDefinitionHandle handle, out GenericParameterHandleCollection genericParameterHandles, out ushort arity, out BadImageFormatException mrEx)
        {
            try
            {
                genericParameterHandles = moduleSymbol.Module.GetTypeDefGenericParamsOrThrow(handle);
                arity = (ushort)genericParameterHandles.Count;
                mrEx = null;
            }
            catch (BadImageFormatException e)
            {
                arity = 0;
                genericParameterHandles = default(GenericParameterHandleCollection);
                mrEx = e;
            }
        }

        internal static PENamedTypeSymbol Create(
            PEModuleSymbol moduleSymbol,
            PENamedTypeSymbol containingType,
            TypeDefinitionHandle handle)
        {
            GenericParameterHandleCollection genericParameterHandles;
            ushort metadataArity;
            BadImageFormatException mrEx = null;

            GetGenericInfo(moduleSymbol, handle, out genericParameterHandles, out metadataArity, out mrEx);

            ushort arity = 0;
            var containerMetadataArity = containingType.MetadataArity;

            if (metadataArity > containerMetadataArity)
            {
                arity = (ushort)(metadataArity - containerMetadataArity);
            }

            bool mangleName;
            PENamedTypeSymbol result;

            if (metadataArity == 0)
            {
                result = new PENamedTypeSymbolNonGeneric(moduleSymbol, containingType, handle, null, out mangleName);
            }
            else
            {
                result = new PENamedTypeSymbolGeneric(
                    moduleSymbol,
                    containingType,
                    handle,
                    null,
                    genericParameterHandles,
                    arity,
                    out mangleName);
            }

            if (mrEx != null || metadataArity < containerMetadataArity)
            {
                result._lazyUseSiteDiagnostic = new CSDiagnosticInfo(ErrorCode.ERR_BogusType, result);
            }

            return result;
        }

        private PENamedTypeSymbol(
            PEModuleSymbol moduleSymbol,
            NamespaceOrTypeSymbol container,
            TypeDefinitionHandle handle,
            string emittedNamespaceName,
            ushort arity,
            out bool mangleName)
        {
            Debug.Assert(!handle.IsNil);
            Debug.Assert((object)container != null);
            Debug.Assert(arity == 0 || this is PENamedTypeSymbolGeneric);

            string metadataName;
            bool makeBad = false;

            try
            {
                metadataName = moduleSymbol.Module.GetTypeDefNameOrThrow(handle);
            }
            catch (BadImageFormatException)
            {
                metadataName = string.Empty;
                makeBad = true;
            }

            _handle = handle;
            _container = container;

            try
            {
                _flags = moduleSymbol.Module.GetTypeDefFlagsOrThrow(handle);
            }
            catch (BadImageFormatException)
            {
                makeBad = true;
            }

            if (arity == 0)
            {
                _name = metadataName;
                mangleName = false;
            }
            else
            {
                // Unmangle name for a generic type.
                _name = MetadataHelpers.UnmangleMetadataNameForArity(metadataName, arity);
                Debug.Assert(ReferenceEquals(_name, metadataName) == (_name == metadataName));
                mangleName = !ReferenceEquals(_name, metadataName);
            }

            // check if this is one of the COR library types
            if (emittedNamespaceName != null &&
                moduleSymbol.ContainingAssembly.KeepLookingForDeclaredSpecialTypes &&
                this.DeclaredAccessibility == Accessibility.Public) // NB: this.flags was set above.
            {
                _corTypeId = SpecialTypes.GetTypeFromMetadataName(MetadataHelpers.BuildQualifiedName(emittedNamespaceName, metadataName));
            }
            else
            {
                _corTypeId = SpecialType.None;
            }

            if (makeBad)
            {
                _lazyUseSiteDiagnostic = new CSDiagnosticInfo(ErrorCode.ERR_BogusType, this);
            }
        }

        public override SpecialType SpecialType
        {
            get
            {
                return _corTypeId;
            }
        }

        internal PEModuleSymbol ContainingPEModule
        {
            get
            {
                Symbol s = _container;

                while (s.Kind != SymbolKind.Namespace)
                {
                    s = s.ContainingSymbol;
                }

                return ((PENamespaceSymbol)s).ContainingPEModule;
            }
        }

        internal override ModuleSymbol ContainingModule
        {
            get
            {
                return ContainingPEModule;
            }
        }

        public abstract override int Arity
        {
            get;
        }

        internal abstract override bool MangleName
        {
            get;
        }

        internal abstract int MetadataArity
        {
            get;
        }

        internal TypeDefinitionHandle Handle
        {
            get
            {
                return _handle;
            }
        }


        internal override bool HasCodeAnalysisEmbeddedAttribute
        {
            get
            {
                var uncommon = GetUncommonProperties();
                if (uncommon == s_noUncommonProperties)
                {
                    return false;
                }

                if (!uncommon.lazyHasEmbeddedAttribute.HasValue())
                {
                    uncommon.lazyHasEmbeddedAttribute = ContainingPEModule.Module.HasCodeAnalysisEmbeddedAttribute(_handle).ToThreeState();
                }

                return uncommon.lazyHasEmbeddedAttribute.Value();
            }
        }

        internal override NamedTypeSymbol BaseTypeNoUseSiteDiagnostics
        {
            get
            {
                if (ReferenceEquals(_lazyBaseType, ErrorTypeSymbol.UnknownResultType))
                {
                    Interlocked.CompareExchange(ref _lazyBaseType, MakeAcyclicBaseType(), ErrorTypeSymbol.UnknownResultType);
                }

                return _lazyBaseType;
            }
        }

        internal override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<TypeSymbol> basesBeingResolved = null)
        {
            if (_lazyInterfaces.IsDefault)
            {
                ImmutableInterlocked.InterlockedCompareExchange(ref _lazyInterfaces, MakeAcyclicInterfaces(), default(ImmutableArray<NamedTypeSymbol>));
            }

            return _lazyInterfaces;
        }

        internal override ImmutableArray<NamedTypeSymbol> GetInterfacesToEmit()
        {
            return InterfacesNoUseSiteDiagnostics();
        }

        internal override NamedTypeSymbol GetDeclaredBaseType(ConsList<TypeSymbol> basesBeingResolved)
        {
            return GetDeclaredBaseType(skipTransformsIfNecessary: false);
        }

        private NamedTypeSymbol GetDeclaredBaseType(bool skipTransformsIfNecessary)
        {
            if (ReferenceEquals(_lazyDeclaredBaseType, ErrorTypeSymbol.UnknownResultType))
            {
                var baseType = MakeDeclaredBaseType();
                if (baseType is object)
                {
                    if (skipTransformsIfNecessary)
                    {
                        // If the transforms are not necessary, return early without updating the
                        // base type field. This avoids cycles decoding nullability in particular.
                        return baseType;
                    }

                    var moduleSymbol = ContainingPEModule;
                    TypeSymbol decodedType = DynamicTypeDecoder.TransformType(baseType, 0, _handle, moduleSymbol);
                    decodedType = TupleTypeDecoder.DecodeTupleTypesIfApplicable(decodedType, _handle, moduleSymbol);
                    baseType = (NamedTypeSymbol)NullableTypeDecoder.TransformType(TypeWithAnnotations.Create(decodedType), _handle, moduleSymbol, accessSymbol: this, nullableContext: this).Type;
                }

                Interlocked.CompareExchange(ref _lazyDeclaredBaseType, baseType, ErrorTypeSymbol.UnknownResultType);
            }

            return _lazyDeclaredBaseType;
        }

        internal override ImmutableArray<NamedTypeSymbol> GetDeclaredInterfaces(ConsList<TypeSymbol> basesBeingResolved)
        {
            if (_lazyDeclaredInterfaces.IsDefault)
            {
                ImmutableInterlocked.InterlockedCompareExchange(ref _lazyDeclaredInterfaces, MakeDeclaredInterfaces(), default(ImmutableArray<NamedTypeSymbol>));
            }

            return _lazyDeclaredInterfaces;
        }

        private NamedTypeSymbol MakeDeclaredBaseType()
        {
            if (!_flags.IsInterface())
            {
                try
                {
                    var moduleSymbol = ContainingPEModule;
                    EntityHandle token = moduleSymbol.Module.GetBaseTypeOfTypeOrThrow(_handle);
                    if (!token.IsNil)
                    {
                        return (NamedTypeSymbol)new MetadataDecoder(moduleSymbol, this).GetTypeOfToken(token);
                    }
                }
                catch (BadImageFormatException mrEx)
                {
                    return new UnsupportedMetadataTypeSymbol(mrEx);
                }
            }

            return null;
        }

        private ImmutableArray<NamedTypeSymbol> MakeDeclaredInterfaces()
        {
            try
            {
                var moduleSymbol = ContainingPEModule;
                var interfaceImpls = moduleSymbol.Module.GetInterfaceImplementationsOrThrow(_handle);

                if (interfaceImpls.Count > 0)
                {
                    var symbols = ArrayBuilder<NamedTypeSymbol>.GetInstance(interfaceImpls.Count);
                    var tokenDecoder = new MetadataDecoder(moduleSymbol, this);

                    foreach (var interfaceImpl in interfaceImpls)
                    {
                        EntityHandle interfaceHandle = moduleSymbol.Module.MetadataReader.GetInterfaceImplementation(interfaceImpl).Interface;
                        TypeSymbol typeSymbol = tokenDecoder.GetTypeOfToken(interfaceHandle);

                        typeSymbol = TupleTypeDecoder.DecodeTupleTypesIfApplicable(typeSymbol, interfaceImpl, moduleSymbol);
                        typeSymbol = NullableTypeDecoder.TransformType(TypeWithAnnotations.Create(typeSymbol), interfaceImpl, moduleSymbol, accessSymbol: this, nullableContext: this).Type;

                        var namedTypeSymbol = typeSymbol as NamedTypeSymbol ?? new UnsupportedMetadataTypeSymbol(); // interface list contains a bad type
                        symbols.Add(namedTypeSymbol);
                    }

                    return symbols.ToImmutableAndFree();
                }

                return ImmutableArray<NamedTypeSymbol>.Empty;
            }
            catch (BadImageFormatException mrEx)
            {
                return ImmutableArray.Create<NamedTypeSymbol>(new UnsupportedMetadataTypeSymbol(mrEx));
            }
        }

        public override NamedTypeSymbol ConstructedFrom
        {
            get
            {
                return this;
            }
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return _container;
            }
        }

        public override NamedTypeSymbol ContainingType
        {
            get
            {
                return _container as NamedTypeSymbol;
            }
        }

        public override Accessibility DeclaredAccessibility
        {
            get
            {
                Accessibility access = Accessibility.Private;

                switch (_flags & TypeAttributes.VisibilityMask)
                {
                    case TypeAttributes.NestedAssembly:
                        access = Accessibility.Internal;
                        break;

                    case TypeAttributes.NestedFamORAssem:
                        access = Accessibility.ProtectedOrInternal;
                        break;

                    case TypeAttributes.NestedFamANDAssem:
                        access = Accessibility.ProtectedAndInternal;
                        break;

                    case TypeAttributes.NestedPrivate:
                        access = Accessibility.Private;
                        break;

                    case TypeAttributes.Public:
                    case TypeAttributes.NestedPublic:
                        access = Accessibility.Public;
                        break;

                    case TypeAttributes.NestedFamily:
                        access = Accessibility.Protected;
                        break;

                    case TypeAttributes.NotPublic:
                        access = Accessibility.Internal;
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(_flags & TypeAttributes.VisibilityMask);
                }

                return access;
            }
        }

        public override NamedTypeSymbol EnumUnderlyingType
        {
            get
            {
                var uncommon = GetUncommonProperties();
                if (uncommon == s_noUncommonProperties)
                {
                    return null;
                }

                this.EnsureEnumUnderlyingTypeIsLoaded(uncommon);
                return uncommon.lazyEnumUnderlyingType;
            }
        }

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            var uncommon = GetUncommonProperties();
            if (uncommon == s_noUncommonProperties)
            {
                return ImmutableArray<CSharpAttributeData>.Empty;
            }

            if (uncommon.lazyCustomAttributes.IsDefault)
            {
                var loadedCustomAttributes = ContainingPEModule.GetCustomAttributesForToken(
                    Handle,
                    out _,
                    // Filter out [Extension]
                    MightContainExtensionMethods ? AttributeDescription.CaseSensitiveExtensionAttribute : default,
                    out _,
                    // Filter out [Obsolete], unless it was user defined
                    (IsRefLikeType && ObsoleteAttributeData is null) ? AttributeDescription.ObsoleteAttribute : default,
                    out _,
                    // Filter out [IsReadOnly]
                    IsReadOnly ? AttributeDescription.IsReadOnlyAttribute : default,
                    out _,
                    // Filter out [IsByRefLike]
                    IsRefLikeType ? AttributeDescription.IsByRefLikeAttribute : default);

                ImmutableInterlocked.InterlockedInitialize(ref uncommon.lazyCustomAttributes, loadedCustomAttributes);
            }

            return uncommon.lazyCustomAttributes;
        }

        internal override IEnumerable<CSharpAttributeData> GetCustomAttributesToEmit(PEModuleBuilder moduleBuilder)
        {
            return GetAttributes();
        }

        internal override byte? GetNullableContextValue()
        {
            byte? value;
            if (!_lazyNullableContextValue.TryGetByte(out value))
            {
                value = ContainingPEModule.Module.HasNullableContextAttribute(_handle, out byte arg) ?
                    arg :
                    _container.GetNullableContextValue();
                _lazyNullableContextValue = value.ToNullableContextFlags();
            }
            return value;
        }

        internal override byte? GetLocalNullableContextValue()
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override IEnumerable<string> MemberNames
        {
            get
            {
                EnsureNonTypeMemberNamesAreLoaded();
                return _lazyMemberNames;
            }
        }

        private void EnsureNonTypeMemberNamesAreLoaded()
        {
            if (_lazyMemberNames == null)
            {
                var moduleSymbol = ContainingPEModule;
                var module = moduleSymbol.Module;

                var names = new HashSet<string>();

                try
                {
                    foreach (var methodDef in module.GetMethodsOfTypeOrThrow(_handle))
                    {
                        try
                        {
                            names.Add(module.GetMethodDefNameOrThrow(methodDef));
                        }
                        catch (BadImageFormatException)
                        { }
                    }
                }
                catch (BadImageFormatException)
                { }

                try
                {
                    foreach (var propertyDef in module.GetPropertiesOfTypeOrThrow(_handle))
                    {
                        try
                        {
                            names.Add(module.GetPropertyDefNameOrThrow(propertyDef));
                        }
                        catch (BadImageFormatException)
                        { }
                    }
                }
                catch (BadImageFormatException)
                { }

                try
                {
                    foreach (var eventDef in module.GetEventsOfTypeOrThrow(_handle))
                    {
                        try
                        {
                            names.Add(module.GetEventDefNameOrThrow(eventDef));
                        }
                        catch (BadImageFormatException)
                        { }
                    }
                }
                catch (BadImageFormatException)
                { }

                try
                {
                    foreach (var fieldDef in module.GetFieldsOfTypeOrThrow(_handle))
                    {
                        try
                        {
                            names.Add(module.GetFieldDefNameOrThrow(fieldDef));
                        }
                        catch (BadImageFormatException)
                        { }
                    }
                }
                catch (BadImageFormatException)
                { }

                // From C#'s perspective, structs always have a public constructor
                // (even if it's not in metadata).  Add it unconditionally and let
                // the hash set de-dup.
                if (this.IsValueType)
                {
                    names.Add(WellKnownMemberNames.InstanceConstructorName);
                }

                Interlocked.CompareExchange(ref _lazyMemberNames, CreateReadOnlyMemberNames(names), null);
            }
        }

        private static ICollection<string> CreateReadOnlyMemberNames(HashSet<string> names)
        {
            switch (names.Count)
            {
                case 0:
                    return SpecializedCollections.EmptySet<string>();

                case 1:
                    return SpecializedCollections.SingletonCollection(names.First());

                case 2:
                case 3:
                case 4:
                case 5:
                case 6:
                    // PERF: Small collections can be implemented as ImmutableArray.
                    // While lookup is O(n), when n is small, the memory savings are more valuable.
                    // Size 6 was chosen because that represented 50% of the names generated in the Picasso end to end.
                    // This causes boxing, but that's still superior to a wrapped HashSet
                    return ImmutableArray.CreateRange(names);

                default:
                    return SpecializedCollections.ReadOnlySet(names);
            }
        }

        public override ImmutableArray<Symbol> GetMembers()
        {
            EnsureAllMembersAreLoaded();
            return _lazyMembersInDeclarationOrder;
        }

        private IEnumerable<FieldSymbol> GetEnumFieldsToEmit()
        {
            var uncommon = GetUncommonProperties();
            if (uncommon == s_noUncommonProperties)
            {
                yield break;
            }

            var moduleSymbol = this.ContainingPEModule;
            var module = moduleSymbol.Module;

            // Non-static fields of enum types are not imported by default because they are not bindable,
            // but we need them for NoPia.

            var fieldDefs = ArrayBuilder<FieldDefinitionHandle>.GetInstance();

            try
            {
                foreach (var fieldDef in module.GetFieldsOfTypeOrThrow(_handle))
                {
                    fieldDefs.Add(fieldDef);
                }
            }
            catch (BadImageFormatException)
            { }

            if (uncommon.lazyInstanceEnumFields.IsDefault)
            {
                var builder = ArrayBuilder<PEFieldSymbol>.GetInstance();

                foreach (var fieldDef in fieldDefs)
                {
                    try
                    {
                        FieldAttributes fieldFlags = module.GetFieldDefFlagsOrThrow(fieldDef);
                        if ((fieldFlags & FieldAttributes.Static) == 0 && ModuleExtensions.ShouldImportField(fieldFlags, moduleSymbol.ImportOptions))
                        {
                            builder.Add(new PEFieldSymbol(moduleSymbol, this, fieldDef));
                        }
                    }
                    catch (BadImageFormatException)
                    { }
                }

                ImmutableInterlocked.InterlockedInitialize(ref uncommon.lazyInstanceEnumFields, builder.ToImmutableAndFree());
            }

            int staticIndex = 0;
            ImmutableArray<Symbol> staticFields = GetMembers();
            int instanceIndex = 0;

            foreach (var fieldDef in fieldDefs)
            {
                if (instanceIndex < uncommon.lazyInstanceEnumFields.Length && uncommon.lazyInstanceEnumFields[instanceIndex].Handle == fieldDef)
                {
                    yield return uncommon.lazyInstanceEnumFields[instanceIndex];
                    instanceIndex++;
                    continue;
                }

                if (staticIndex < staticFields.Length && staticFields[staticIndex].Kind == SymbolKind.Field)
                {
                    var field = (PEFieldSymbol)staticFields[staticIndex];

                    if (field.Handle == fieldDef)
                    {
                        yield return field;
                        staticIndex++;
                        continue;
                    }
                }
            }

            fieldDefs.Free();

            Debug.Assert(instanceIndex == uncommon.lazyInstanceEnumFields.Length);
            Debug.Assert(staticIndex == staticFields.Length || staticFields[staticIndex].Kind != SymbolKind.Field);
        }

        internal override IEnumerable<FieldSymbol> GetFieldsToEmit()
        {
            if (this.TypeKind == TypeKind.Enum)
            {
                return GetEnumFieldsToEmit();
            }
            else
            {
                // If there are any non-event fields, they are at the very beginning.
                IEnumerable<FieldSymbol> nonEventFields = GetMembers<FieldSymbol>(this.GetMembers(), SymbolKind.Field, offset: 0);

                // Event backing fields are not part of the set returned by GetMembers. Let's add them manually.
                ArrayBuilder<FieldSymbol> eventFields = null;

                foreach (var eventSymbol in GetEventsToEmit())
                {
                    FieldSymbol associatedField = eventSymbol.AssociatedField;
                    if ((object)associatedField != null)
                    {
                        Debug.Assert((object)associatedField.AssociatedSymbol != null);
                        Debug.Assert(!nonEventFields.Contains(associatedField));

                        if (eventFields == null)
                        {
                            eventFields = ArrayBuilder<FieldSymbol>.GetInstance();
                        }

                        eventFields.Add(associatedField);
                    }
                }

                if (eventFields == null)
                {
                    // Simple case
                    return nonEventFields;
                }

                // We need to merge non-event fields with event fields while preserving their relative declaration order
                var handleToFieldMap = new SmallDictionary<FieldDefinitionHandle, FieldSymbol>();
                int count = 0;

                foreach (PEFieldSymbol field in nonEventFields)
                {
                    handleToFieldMap.Add(field.Handle, field);
                    count++;
                }

                foreach (PEFieldSymbol field in eventFields)
                {
                    handleToFieldMap.Add(field.Handle, field);
                }

                count += eventFields.Count;
                eventFields.Free();

                var result = ArrayBuilder<FieldSymbol>.GetInstance(count);

                try
                {
                    foreach (var handle in this.ContainingPEModule.Module.GetFieldsOfTypeOrThrow(_handle))
                    {
                        FieldSymbol field;
                        if (handleToFieldMap.TryGetValue(handle, out field))
                        {
                            result.Add(field);
                        }
                    }
                }
                catch (BadImageFormatException)
                { }

                Debug.Assert(result.Count == count);

                return result.ToImmutableAndFree();
            }
        }

        internal override IEnumerable<MethodSymbol> GetMethodsToEmit()
        {
            ImmutableArray<Symbol> members = GetMembers();

            // Get to methods.
            int index = GetIndexOfFirstMember(members, SymbolKind.Method);

            if (!this.IsInterfaceType())
            {
                for (; index < members.Length; index++)
                {
                    if (members[index].Kind != SymbolKind.Method)
                    {
                        break;
                    }

                    var method = (MethodSymbol)members[index];

                    // Don't emit the default value type constructor - the runtime handles that
                    if (!method.IsDefaultValueTypeConstructor())
                    {
                        yield return method;
                    }
                }
            }
            else
            {
                // We do not create symbols for v-table gap methods, let's figure out where the gaps go.

                if (index >= members.Length || members[index].Kind != SymbolKind.Method)
                {
                    // We didn't import any methods, it is Ok to return an empty set.
                    yield break;
                }

                var method = (PEMethodSymbol)members[index];
                var module = this.ContainingPEModule.Module;

                var methodDefs = ArrayBuilder<MethodDefinitionHandle>.GetInstance();

                try
                {
                    foreach (var methodDef in module.GetMethodsOfTypeOrThrow(_handle))
                    {
                        methodDefs.Add(methodDef);
                    }
                }
                catch (BadImageFormatException)
                { }

                foreach (var methodDef in methodDefs)
                {
                    if (method.Handle == methodDef)
                    {
                        yield return method;
                        index++;

                        if (index == members.Length || members[index].Kind != SymbolKind.Method)
                        {
                            // no need to return any gaps at the end.
                            methodDefs.Free();
                            yield break;
                        }

                        method = (PEMethodSymbol)members[index];
                    }
                    else
                    {
                        // Encountered a gap.
                        int gapSize;

                        try
                        {
                            gapSize = ModuleExtensions.GetVTableGapSize(module.GetMethodDefNameOrThrow(methodDef));
                        }
                        catch (BadImageFormatException)
                        {
                            gapSize = 1;
                        }

                        // We don't have a symbol to return, so, even if the name doesn't represent a gap, we still have a gap.
                        do
                        {
                            yield return null;
                            gapSize--;
                        }
                        while (gapSize > 0);
                    }
                }

                // Ensure we explicitly returned from inside loop.
                throw ExceptionUtilities.Unreachable;
            }
        }

        internal override IEnumerable<PropertySymbol> GetPropertiesToEmit()
        {
            return GetMembers<PropertySymbol>(this.GetMembers(), SymbolKind.Property);
        }

        internal override IEnumerable<EventSymbol> GetEventsToEmit()
        {
            return GetMembers<EventSymbol>(this.GetMembers(), SymbolKind.Event);
        }

        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers()
        {
            return this.GetMembersUnordered();
        }

        internal override ImmutableArray<Symbol> GetEarlyAttributeDecodingMembers(string name)
        {
            return this.GetMembers(name);
        }

        private class DeclarationOrderTypeSymbolComparer : IComparer<Symbol>
        {
            public static readonly DeclarationOrderTypeSymbolComparer Instance = new DeclarationOrderTypeSymbolComparer();

            private DeclarationOrderTypeSymbolComparer() { }

            public int Compare(Symbol x, Symbol y)
            {
                return HandleComparer.Default.Compare(((PENamedTypeSymbol)x).Handle, ((PENamedTypeSymbol)y).Handle);
            }
        }


        private void EnsureEnumUnderlyingTypeIsLoaded(UncommonProperties uncommon)
        {
            if ((object)(uncommon.lazyEnumUnderlyingType) == null
                && this.TypeKind == TypeKind.Enum)
            {
                // From §8.5.2
                // An enum is considerably more restricted than a true type, as
                // follows:
                // - It shall have exactly one instance field, and the type of that field defines the underlying type of
                // the enumeration.
                // - It shall not have any static fields unless they are literal. (see §8.6.1.2)

                // The underlying type shall be a built-in integer type. Enums shall derive from System.Enum, hence they are
                // value types. Like all value types, they shall be sealed (see §8.9.9).

                var moduleSymbol = this.ContainingPEModule;
                var module = moduleSymbol.Module;
                var decoder = new MetadataDecoder(moduleSymbol, this);
                NamedTypeSymbol underlyingType = null;

                try
                {
                    foreach (var fieldDef in module.GetFieldsOfTypeOrThrow(_handle))
                    {
                        FieldAttributes fieldFlags;

                        try
                        {
                            fieldFlags = module.GetFieldDefFlagsOrThrow(fieldDef);
                        }
                        catch (BadImageFormatException)
                        {
                            continue;
                        }

                        if ((fieldFlags & FieldAttributes.Static) == 0)
                        {
                            // Instance field used to determine underlying type.
                            bool isVolatile;
                            ImmutableArray<ModifierInfo<TypeSymbol>> customModifiers;
                            TypeSymbol type = decoder.DecodeFieldSignature(fieldDef, out isVolatile, out customModifiers);

                            if (type.SpecialType.IsValidEnumUnderlyingType())
                            {
                                if ((object)underlyingType == null)
                                {
                                    underlyingType = (NamedTypeSymbol)type;
                                }
                                else
                                {
                                    underlyingType = new UnsupportedMetadataTypeSymbol(); // ambiguous underlying type
                                }
                            }
                        }
                    }

                    if ((object)underlyingType == null)
                    {
                        underlyingType = new UnsupportedMetadataTypeSymbol(); // undefined underlying type
                    }
                }
                catch (BadImageFormatException mrEx)
                {
                    if ((object)underlyingType == null)
                    {
                        underlyingType = new UnsupportedMetadataTypeSymbol(mrEx);
                    }
                }

                Interlocked.CompareExchange(ref uncommon.lazyEnumUnderlyingType, underlyingType, null);
            }
        }

        private void EnsureAllMembersAreLoaded()
        {
            if (_lazyMembersByName == null)
            {
                LoadMembers();
            }
        }

        private void LoadMembers()
        {
            ArrayBuilder<Symbol> members = null;

            if (_lazyMembersInDeclarationOrder.IsDefault)
            {
                EnsureNestedTypesAreLoaded();

                members = ArrayBuilder<Symbol>.GetInstance();

                Debug.Assert(SymbolKind.Field.ToSortOrder() < SymbolKind.Method.ToSortOrder());
                Debug.Assert(SymbolKind.Method.ToSortOrder() < SymbolKind.Property.ToSortOrder());
                Debug.Assert(SymbolKind.Property.ToSortOrder() < SymbolKind.Event.ToSortOrder());
                Debug.Assert(SymbolKind.Event.ToSortOrder() < SymbolKind.NamedType.ToSortOrder());

                if (this.TypeKind == TypeKind.Enum)
                {
                    EnsureEnumUnderlyingTypeIsLoaded(this.GetUncommonProperties());

                    var moduleSymbol = this.ContainingPEModule;
                    var module = moduleSymbol.Module;

                    try
                    {
                        foreach (var fieldDef in module.GetFieldsOfTypeOrThrow(_handle))
                        {
                            FieldAttributes fieldFlags;

                            try
                            {
                                fieldFlags = module.GetFieldDefFlagsOrThrow(fieldDef);
                                if ((fieldFlags & FieldAttributes.Static) == 0)
                                {
                                    continue;
                                }
                            }
                            catch (BadImageFormatException)
                            {
                                fieldFlags = 0;
                            }

                            if (ModuleExtensions.ShouldImportField(fieldFlags, moduleSymbol.ImportOptions))
                            {
                                var field = new PEFieldSymbol(moduleSymbol, this, fieldDef);
                                members.Add(field);
                            }
                        }
                    }
                    catch (BadImageFormatException)
                    { }

                    var syntheticCtor = new SynthesizedInstanceConstructor(this);
                    members.Add(syntheticCtor);
                }
                else
                {
                    ArrayBuilder<PEFieldSymbol> fieldMembers = ArrayBuilder<PEFieldSymbol>.GetInstance();
                    ArrayBuilder<Symbol> nonFieldMembers = ArrayBuilder<Symbol>.GetInstance();

                    MultiDictionary<string, PEFieldSymbol> privateFieldNameToSymbols = this.CreateFields(fieldMembers);

                    // A method may be referenced as an accessor by one or more properties. And,
                    // any of those properties may be "bogus" if one of the property accessors
                    // does not match the property signature. If the method is referenced by at
                    // least one non-bogus property, then the method is created as an accessor,
                    // and (for purposes of error reporting if the method is referenced directly) the
                    // associated property is set (arbitrarily) to the first non-bogus property found
                    // in metadata. If the method is not referenced by any non-bogus properties,
                    // then the method is created as a normal method rather than an accessor.

                    // Create a dictionary of method symbols indexed by metadata handle
                    // (to allow efficient lookup when matching property accessors).
                    PooledDictionary<MethodDefinitionHandle, PEMethodSymbol> methodHandleToSymbol = this.CreateMethods(nonFieldMembers);

                    if (this.TypeKind == TypeKind.Struct)
                    {
                        bool haveParameterlessConstructor = false;
                        foreach (MethodSymbol method in nonFieldMembers)
                        {
                            if (method.IsParameterlessConstructor())
                            {
                                haveParameterlessConstructor = true;
                                break;
                            }
                        }

                        // Structs have an implicit parameterless constructor, even if it
                        // does not appear in metadata (11.3.8)
                        if (!haveParameterlessConstructor)
                        {
                            nonFieldMembers.Insert(0, new SynthesizedInstanceConstructor(this));
                        }
                    }

                    this.CreateProperties(methodHandleToSymbol, nonFieldMembers);
                    this.CreateEvents(privateFieldNameToSymbols, methodHandleToSymbol, nonFieldMembers);

                    foreach (PEFieldSymbol field in fieldMembers)
                    {
                        if ((object)field.AssociatedSymbol == null)
                        {
                            members.Add(field);
                        }
                        else
                        {
                            // As for source symbols, our public API presents the fiction that all
                            // operations are performed on the event, rather than on the backing field.  
                            // The backing field is not accessible through the API.  As an additional 
                            // bonus, lookup is easier when the names don't collide.
                            Debug.Assert(field.AssociatedSymbol.Kind == SymbolKind.Event);
                        }
                    }

                    members.AddRange(nonFieldMembers);

                    nonFieldMembers.Free();
                    fieldMembers.Free();

                    methodHandleToSymbol.Free();
                }

                // Now add types to the end.
                int membersCount = members.Count;

                foreach (var typeArray in _lazyNestedTypes.Values)
                {
                    members.AddRange(typeArray);
                }

                // Sort the types based on row id.
                members.Sort(membersCount, DeclarationOrderTypeSymbolComparer.Instance);

                var membersInDeclarationOrder = members.ToImmutable();

#if DEBUG
                ISymbol previous = null;

                foreach (var s in membersInDeclarationOrder)
                {
                    if (previous == null)
                    {
                        previous = s;
                    }
                    else
                    {
                        ISymbol current = s;
                        Debug.Assert(previous.Kind.ToSortOrder() <= current.Kind.ToSortOrder());
                        previous = current;
                    }
                }
#endif

                if (!ImmutableInterlocked.InterlockedInitialize(ref _lazyMembersInDeclarationOrder, membersInDeclarationOrder))
                {
                    members.Free();
                    members = null;
                }
                else
                {
                    // remove the types
                    members.Clip(membersCount);
                }
            }

            if (_lazyMembersByName == null)
            {
                if (members == null)
                {
                    members = ArrayBuilder<Symbol>.GetInstance();
                    foreach (var member in _lazyMembersInDeclarationOrder)
                    {
                        if (member.Kind == SymbolKind.NamedType)
                        {
                            break;
                        }
                        members.Add(member);
                    }
                }

                Dictionary<string, ImmutableArray<Symbol>> membersDict = GroupByName(members);

                var exchangeResult = Interlocked.CompareExchange(ref _lazyMembersByName, membersDict, null);
                if (exchangeResult == null)
                {
                    // we successfully swapped in the members dictionary.

                    // Now, use these as the canonical member names.  This saves us memory by not having
                    // two collections around at the same time with redundant data in them.
                    //
                    // NOTE(cyrusn): We must use an interlocked exchange here so that the full
                    // construction of this object will be seen from 'MemberNames'.  Also, doing a
                    // straight InterlockedExchange here is the right thing to do.  Consider the case
                    // where one thread is calling in through "MemberNames" while we are in the middle
                    // of this method.  Either that thread will compute the member names and store it
                    // first (in which case we overwrite it), or we will store first (in which case
                    // their CompareExchange(..., ..., null) will fail.  Either way, this will be certain
                    // to become the canonical set of member names.
                    //
                    // NOTE(cyrusn): This means that it is possible (and by design) for people to get a
                    // different object back when they call MemberNames multiple times.  However, outside
                    // of object identity, both collections should appear identical to the user.
                    var memberNames = SpecializedCollections.ReadOnlyCollection(membersDict.Keys);
                    Interlocked.Exchange(ref _lazyMemberNames, memberNames);
                }
            }

            if (members != null)
            {
                members.Free();
            }
        }

        internal override ImmutableArray<Symbol> GetSimpleNonTypeMembers(string name)
        {
            EnsureAllMembersAreLoaded();

            ImmutableArray<Symbol> m;
            if (!_lazyMembersByName.TryGetValue(name, out m))
            {
                m = ImmutableArray<Symbol>.Empty;
            }

            return m;
        }

        public override ImmutableArray<Symbol> GetMembers(string name)
        {
            EnsureAllMembersAreLoaded();

            ImmutableArray<Symbol> m;
            if (!_lazyMembersByName.TryGetValue(name, out m))
            {
                m = ImmutableArray<Symbol>.Empty;
            }

            // nested types are not common, but we need to check just in case
            ImmutableArray<PENamedTypeSymbol> t;
            if (_lazyNestedTypes.TryGetValue(name, out t))
            {
                m = m.Concat(StaticCast<Symbol>.From(t));
            }

            return m;
        }

        internal override FieldSymbol FixedElementField
        {
            get
            {
                FieldSymbol result = null;

                var candidates = this.GetMembers(FixedFieldImplementationType.FixedElementFieldName);
                if (!candidates.IsDefault && candidates.Length == 1)
                {
                    result = candidates[0] as FieldSymbol;
                }

                return result;
            }
        }

        internal override ImmutableArray<NamedTypeSymbol> GetTypeMembersUnordered()
        {
            return GetTypeMembers().ConditionallyDeOrder();
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers()
        {
            EnsureNestedTypesAreLoaded();
            return GetMemberTypesPrivate();
        }

        private ImmutableArray<NamedTypeSymbol> GetMemberTypesPrivate()
        {
            var builder = ArrayBuilder<NamedTypeSymbol>.GetInstance();
            foreach (var typeArray in _lazyNestedTypes.Values)
            {
                builder.AddRange(typeArray);
            }

            return builder.ToImmutableAndFree();
        }

        private void EnsureNestedTypesAreLoaded()
        {
            if (_lazyNestedTypes == null)
            {
                var types = ArrayBuilder<PENamedTypeSymbol>.GetInstance();
                types.AddRange(this.CreateNestedTypes());
                var typesDict = GroupByName(types);

                var exchangeResult = Interlocked.CompareExchange(ref _lazyNestedTypes, typesDict, null);
                if (exchangeResult == null)
                {
                    // Build cache of TypeDef Tokens
                    // Potentially this can be done in the background.
                    var moduleSymbol = this.ContainingPEModule;
                    moduleSymbol.OnNewTypeDeclarationsLoaded(typesDict);
                }
                types.Free();
            }
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name)
        {
            EnsureNestedTypesAreLoaded();

            ImmutableArray<PENamedTypeSymbol> t;

            if (_lazyNestedTypes.TryGetValue(name, out t))
            {
                return StaticCast<NamedTypeSymbol>.From(t);
            }

            return ImmutableArray<NamedTypeSymbol>.Empty;
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(string name, int arity)
        {
            return GetTypeMembers(name).WhereAsArray(type => type.Arity == arity);
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return ContainingPEModule.MetadataLocation.Cast<MetadataLocation, Location>();
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return ImmutableArray<SyntaxReference>.Empty;
            }
        }

        public override string Name
        {
            get
            {
                return _name;
            }
        }

        internal override bool HasSpecialName
        {
            get
            {
                return (_flags & TypeAttributes.SpecialName) != 0;
            }
        }

        internal override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotationsNoUseSiteDiagnostics
        {
            get
            {
                return ImmutableArray<TypeWithAnnotations>.Empty;
            }
        }

        public override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get
            {
                return ImmutableArray<TypeParameterSymbol>.Empty;
            }
        }

        public override bool IsStatic
        {
            get
            {
                return
                    (_flags & TypeAttributes.Sealed) != 0 &&
                    (_flags & TypeAttributes.Abstract) != 0;
            }
        }

        public override bool IsAbstract
        {
            get
            {
                return
                    (_flags & TypeAttributes.Abstract) != 0 &&
                    (_flags & TypeAttributes.Sealed) == 0;
            }
        }

        internal override bool IsMetadataAbstract
        {
            get
            {
                return (_flags & TypeAttributes.Abstract) != 0;
            }
        }

        public override bool IsSealed
        {
            get
            {
                return
                    (_flags & TypeAttributes.Sealed) != 0 &&
                    (_flags & TypeAttributes.Abstract) == 0;
            }
        }

        internal override bool IsMetadataSealed
        {
            get
            {
                return (_flags & TypeAttributes.Sealed) != 0;
            }
        }

        internal TypeAttributes Flags
        {
            get
            {
                return _flags;
            }
        }

        public sealed override bool AreLocalsZeroed
        {
            get { throw ExceptionUtilities.Unreachable; }
        }

        public override bool MightContainExtensionMethods
        {
            get
            {
                var uncommon = GetUncommonProperties();
                if (uncommon == s_noUncommonProperties)
                {
                    return false;
                }

                if (!uncommon.lazyContainsExtensionMethods.HasValue())
                {
                    var contains = ThreeState.False;
                    // Dev11 supports extension methods defined on non-static
                    // classes, structs, delegates, and generic types.
                    switch (this.TypeKind)
                    {
                        case TypeKind.Class:
                        case TypeKind.Struct:
                        case TypeKind.Delegate:
                            var moduleSymbol = this.ContainingPEModule;
                            var module = moduleSymbol.Module;
                            bool moduleHasExtension = module.HasExtensionAttribute(_handle, ignoreCase: false);

                            var containingAssembly = this.ContainingAssembly as PEAssemblySymbol;
                            if ((object)containingAssembly != null)
                            {
                                contains = (moduleHasExtension
                                    && containingAssembly.MightContainExtensionMethods).ToThreeState();
                            }
                            else
                            {
                                contains = moduleHasExtension.ToThreeState();
                            }
                            break;
                    }

                    uncommon.lazyContainsExtensionMethods = contains;
                }

                return uncommon.lazyContainsExtensionMethods.Value();
            }
        }

        public override TypeKind TypeKind
        {
            get
            {
                TypeKind result = _lazyKind;

                if (result == TypeKind.Unknown)
                {
                    if (_flags.IsInterface())
                    {
                        result = TypeKind.Interface;
                    }
                    else
                    {
                        TypeSymbol @base = GetDeclaredBaseType(skipTransformsIfNecessary: true);

                        result = TypeKind.Class;

                        if ((object)@base != null)
                        {
                            SpecialType baseCorTypeId = @base.SpecialType;

                            switch (baseCorTypeId)
                            {
                                case SpecialType.System_Enum:
                                    // Enum
                                    result = TypeKind.Enum;
                                    break;

                                case SpecialType.System_MulticastDelegate:
                                    // Delegate
                                    result = TypeKind.Delegate;
                                    break;

                                case SpecialType.System_ValueType:
                                    // System.Enum is the only class that derives from ValueType
                                    if (this.SpecialType != SpecialType.System_Enum)
                                    {
                                        // Struct
                                        result = TypeKind.Struct;
                                    }
                                    break;
                            }
                        }
                    }

                    _lazyKind = result;
                }

                return result;
            }
        }

        internal sealed override bool IsInterface
        {
            get
            {
                return _flags.IsInterface();
            }
        }

        private static ExtendedErrorTypeSymbol CyclicInheritanceError(PENamedTypeSymbol type, TypeSymbol declaredBase)
        {
            var info = new CSDiagnosticInfo(ErrorCode.ERR_ImportedCircularBase, declaredBase, type);
            return new ExtendedErrorTypeSymbol(declaredBase, LookupResultKind.NotReferencable, info, true);
        }

        private NamedTypeSymbol MakeAcyclicBaseType()
        {
            NamedTypeSymbol declaredBase = GetDeclaredBaseType(null);

            // implicit base is not interesting for metadata cycle detection
            if ((object)declaredBase == null)
            {
                return null;
            }

            if (BaseTypeAnalysis.TypeDependsOn(declaredBase, this))
            {
                return CyclicInheritanceError(this, declaredBase);
            }

            this.SetKnownToHaveNoDeclaredBaseCycles();
            return declaredBase;
        }

        private ImmutableArray<NamedTypeSymbol> MakeAcyclicInterfaces()
        {
            var declaredInterfaces = GetDeclaredInterfaces(null);
            if (!IsInterface)
            {
                // only interfaces needs to check for inheritance cycles via interfaces.
                return declaredInterfaces;
            }

            return declaredInterfaces
                .SelectAsArray(t => BaseTypeAnalysis.TypeDependsOn(t, this) ? CyclicInheritanceError(this, t) : t);
        }

        public override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return PEDocumentationCommentUtils.GetDocumentationComment(this, ContainingPEModule, preferredCulture, cancellationToken, ref _lazyDocComment);
        }

        private IEnumerable<PENamedTypeSymbol> CreateNestedTypes()
        {
            var moduleSymbol = this.ContainingPEModule;
            var module = moduleSymbol.Module;

            ImmutableArray<TypeDefinitionHandle> nestedTypeDefs;

            try
            {
                nestedTypeDefs = module.GetNestedTypeDefsOrThrow(_handle);
            }
            catch (BadImageFormatException)
            {
                yield break;
            }

            foreach (var typeRid in nestedTypeDefs)
            {
                if (module.ShouldImportNestedType(typeRid))
                {
                    yield return PENamedTypeSymbol.Create(moduleSymbol, this, typeRid);
                }
            }
        }

        private MultiDictionary<string, PEFieldSymbol> CreateFields(ArrayBuilder<PEFieldSymbol> fieldMembers)
        {
            var privateFieldNameToSymbols = new MultiDictionary<string, PEFieldSymbol>();

            var moduleSymbol = this.ContainingPEModule;
            var module = moduleSymbol.Module;

            // for ordinary struct types we import private fields so that we can distinguish empty structs from non-empty structs
            var isOrdinaryStruct = false;
            // for ordinary embeddable struct types we import private members so that we can report appropriate errors if the structure is used 
            var isOrdinaryEmbeddableStruct = false;

            if (this.TypeKind == TypeKind.Struct)
            {
                if (this.SpecialType == Microsoft.CodeAnalysis.SpecialType.None)
                {
                    isOrdinaryStruct = true;
                    isOrdinaryEmbeddableStruct = this.ContainingAssembly.IsLinked;
                }
                else
                {
                    isOrdinaryStruct = (this.SpecialType == Microsoft.CodeAnalysis.SpecialType.System_Nullable_T);
                }
            }

            try
            {
                foreach (var fieldRid in module.GetFieldsOfTypeOrThrow(_handle))
                {
                    try
                    {
                        if (!(isOrdinaryEmbeddableStruct ||
                            (isOrdinaryStruct && (module.GetFieldDefFlagsOrThrow(fieldRid) & FieldAttributes.Static) == 0) ||
                            module.ShouldImportField(fieldRid, moduleSymbol.ImportOptions)))
                        {
                            continue;
                        }
                    }
                    catch (BadImageFormatException)
                    { }

                    var symbol = new PEFieldSymbol(moduleSymbol, this, fieldRid);
                    fieldMembers.Add(symbol);

                    // Only private fields are potentially backing fields for field-like events.
                    if (symbol.DeclaredAccessibility == Accessibility.Private)
                    {
                        var name = symbol.Name;
                        if (name.Length > 0)
                        {
                            privateFieldNameToSymbols.Add(name, symbol);
                        }
                    }
                }
            }
            catch (BadImageFormatException)
            { }

            return privateFieldNameToSymbols;
        }

        private PooledDictionary<MethodDefinitionHandle, PEMethodSymbol> CreateMethods(ArrayBuilder<Symbol> members)
        {
            var moduleSymbol = this.ContainingPEModule;
            var module = moduleSymbol.Module;
            var map = PooledDictionary<MethodDefinitionHandle, PEMethodSymbol>.GetInstance();

            // for ordinary embeddable struct types we import private members so that we can report appropriate errors if the structure is used 
            var isOrdinaryEmbeddableStruct = (this.TypeKind == TypeKind.Struct) && (this.SpecialType == Microsoft.CodeAnalysis.SpecialType.None) && this.ContainingAssembly.IsLinked;

            try
            {
                foreach (var methodHandle in module.GetMethodsOfTypeOrThrow(_handle))
                {
                    if (isOrdinaryEmbeddableStruct || module.ShouldImportMethod(methodHandle, moduleSymbol.ImportOptions))
                    {
                        var method = new PEMethodSymbol(moduleSymbol, this, methodHandle);
                        members.Add(method);
                        map.Add(methodHandle, method);
                    }
                }
            }
            catch (BadImageFormatException)
            { }

            return map;
        }

        private void CreateProperties(Dictionary<MethodDefinitionHandle, PEMethodSymbol> methodHandleToSymbol, ArrayBuilder<Symbol> members)
        {
            var moduleSymbol = this.ContainingPEModule;
            var module = moduleSymbol.Module;

            try
            {
                foreach (var propertyDef in module.GetPropertiesOfTypeOrThrow(_handle))
                {
                    try
                    {
                        var methods = module.GetPropertyMethodsOrThrow(propertyDef);

                        PEMethodSymbol getMethod = GetAccessorMethod(module, methodHandleToSymbol, methods.Getter);
                        PEMethodSymbol setMethod = GetAccessorMethod(module, methodHandleToSymbol, methods.Setter);

                        if (((object)getMethod != null) || ((object)setMethod != null))
                        {
                            members.Add(PEPropertySymbol.Create(moduleSymbol, this, propertyDef, getMethod, setMethod));
                        }
                    }
                    catch (BadImageFormatException)
                    { }
                }
            }
            catch (BadImageFormatException)
            { }
        }

        private void CreateEvents(
            MultiDictionary<string, PEFieldSymbol> privateFieldNameToSymbols,
            Dictionary<MethodDefinitionHandle, PEMethodSymbol> methodHandleToSymbol,
            ArrayBuilder<Symbol> members)
        {
            var moduleSymbol = this.ContainingPEModule;
            var module = moduleSymbol.Module;

            try
            {
                foreach (var eventRid in module.GetEventsOfTypeOrThrow(_handle))
                {
                    try
                    {
                        var methods = module.GetEventMethodsOrThrow(eventRid);

                        // NOTE: C# ignores all other accessors (most notably, raise/fire).
                        PEMethodSymbol addMethod = GetAccessorMethod(module, methodHandleToSymbol, methods.Adder);
                        PEMethodSymbol removeMethod = GetAccessorMethod(module, methodHandleToSymbol, methods.Remover);

                        // NOTE: both accessors are required, but that will be reported separately.
                        // Create the symbol unless both accessors are missing.
                        if (((object)addMethod != null) || ((object)removeMethod != null))
                        {
                            members.Add(new PEEventSymbol(moduleSymbol, this, eventRid, addMethod, removeMethod, privateFieldNameToSymbols));
                        }
                    }
                    catch (BadImageFormatException)
                    { }
                }
            }
            catch (BadImageFormatException)
            { }
        }

        private PEMethodSymbol GetAccessorMethod(PEModule module, Dictionary<MethodDefinitionHandle, PEMethodSymbol> methodHandleToSymbol, MethodDefinitionHandle methodDef)
        {
            if (methodDef.IsNil)
            {
                return null;
            }

            PEMethodSymbol method;
            bool found = methodHandleToSymbol.TryGetValue(methodDef, out method);
            Debug.Assert(found || !module.ShouldImportMethod(methodDef, this.ContainingPEModule.ImportOptions));
            return method;
        }

        private static Dictionary<string, ImmutableArray<Symbol>> GroupByName(ArrayBuilder<Symbol> symbols)
        {
            return symbols.ToDictionary(s => s.Name, StringOrdinalComparer.Instance);
        }

        private static Dictionary<string, ImmutableArray<PENamedTypeSymbol>> GroupByName(ArrayBuilder<PENamedTypeSymbol> symbols)
        {
            if (symbols.Count == 0)
            {
                return s_emptyNestedTypes;
            }

            return symbols.ToDictionary(s => s.Name, StringOrdinalComparer.Instance);
        }


        internal override DiagnosticInfo GetUseSiteDiagnostic()
        {
            if (ReferenceEquals(_lazyUseSiteDiagnostic, CSDiagnosticInfo.EmptyErrorInfo))
            {
                _lazyUseSiteDiagnostic = GetUseSiteDiagnosticImpl();
            }

            return _lazyUseSiteDiagnostic;
        }

        protected virtual DiagnosticInfo GetUseSiteDiagnosticImpl()
        {
            DiagnosticInfo diagnostic = null;

            if (!MergeUseSiteDiagnostics(ref diagnostic, CalculateUseSiteDiagnostic()))
            {
                // Check if this type is marked by RequiredAttribute attribute.
                // If so mark the type as bad, because it relies upon semantics that are not understood by the C# compiler.
                if (this.ContainingPEModule.Module.HasRequiredAttributeAttribute(_handle))
                {
                    diagnostic = new CSDiagnosticInfo(ErrorCode.ERR_BogusType, this);
                }
                else if (TypeKind == TypeKind.Class && SpecialType != SpecialType.System_Enum)
                {
                    TypeSymbol @base = GetDeclaredBaseType(null);
                    if (@base?.SpecialType == SpecialType.None && @base.ContainingAssembly?.IsMissing == true)
                    {
                        var missingType = @base as MissingMetadataTypeSymbol.TopLevel;
                        if ((object)missingType != null && missingType.Arity == 0)
                        {
                            string emittedName = MetadataHelpers.BuildQualifiedName(missingType.NamespaceName, missingType.MetadataName);
                            switch (SpecialTypes.GetTypeFromMetadataName(emittedName))
                            {
                                case SpecialType.System_Enum:
                                case SpecialType.System_MulticastDelegate:
                                case SpecialType.System_ValueType:
                                    // This might be a structure, an enum, or a delegate
                                    diagnostic = missingType.GetUseSiteDiagnostic();
                                    break;
                            }
                        }
                    }
                }
            }

            return diagnostic;
        }

        internal string DefaultMemberName
        {
            get
            {
                var uncommon = GetUncommonProperties();
                if (uncommon == s_noUncommonProperties)
                {
                    return "";
                }

                if (uncommon.lazyDefaultMemberName == null)
                {
                    string defaultMemberName;
                    this.ContainingPEModule.Module.HasDefaultMemberAttribute(_handle, out defaultMemberName);

                    // NOTE: the default member name is frequently null (e.g. if there is not indexer in the type).
                    // Make sure we set a non-null value so that we don't recompute it repeatedly.
                    // CONSIDER: this makes it impossible to distinguish between not having the attribute and
                    // having the attribute with a value of "".
                    Interlocked.CompareExchange(ref uncommon.lazyDefaultMemberName, defaultMemberName ?? "", null);
                }
                return uncommon.lazyDefaultMemberName;
            }
        }

        internal override bool IsComImport
        {
            get
            {
                return (_flags & TypeAttributes.Import) != 0;
            }
        }

        internal override bool ShouldAddWinRTMembers
        {
            get { return IsWindowsRuntimeImport; }
        }

        internal override bool IsWindowsRuntimeImport
        {
            get
            {
                return (_flags & TypeAttributes.WindowsRuntime) != 0;
            }
        }

        internal override bool GetGuidString(out string guidString)
        {
            return ContainingPEModule.Module.HasGuidAttribute(_handle, out guidString);
        }

        internal override TypeLayout Layout
        {
            get
            {
                return this.ContainingPEModule.Module.GetTypeLayout(_handle);
            }
        }

        internal override CharSet MarshallingCharSet
        {
            get
            {
                CharSet result = _flags.ToCharSet();

                if (result == 0)
                {
                    // TODO(tomat): report error
                    return CharSet.Ansi;
                }

                return result;
            }
        }

        public override bool IsSerializable
        {
            get { return (_flags & TypeAttributes.Serializable) != 0; }
        }

        public override bool IsRefLikeType
        {
            get
            {
                var uncommon = GetUncommonProperties();
                if (uncommon == s_noUncommonProperties)
                {
                    return false;
                }

                if (!uncommon.lazyIsByRefLike.HasValue())
                {
                    var isByRefLike = ThreeState.False;

                    if (this.TypeKind == TypeKind.Struct)
                    {
                        var moduleSymbol = this.ContainingPEModule;
                        var module = moduleSymbol.Module;
                        isByRefLike = module.HasIsByRefLikeAttribute(_handle).ToThreeState();
                    }

                    uncommon.lazyIsByRefLike = isByRefLike;
                }

                return uncommon.lazyIsByRefLike.Value();
            }
        }

        public override bool IsReadOnly
        {
            get
            {
                var uncommon = GetUncommonProperties();
                if (uncommon == s_noUncommonProperties)
                {
                    return false;
                }

                if (!uncommon.lazyIsReadOnly.HasValue())
                {
                    var isReadOnly = ThreeState.False;

                    if (this.TypeKind == TypeKind.Struct)
                    {
                        var moduleSymbol = this.ContainingPEModule;
                        var module = moduleSymbol.Module;
                        isReadOnly = module.HasIsReadOnlyAttribute(_handle).ToThreeState();
                    }

                    uncommon.lazyIsReadOnly = isReadOnly;
                }

                return uncommon.lazyIsReadOnly.Value();
            }
        }

        internal override bool HasDeclarativeSecurity
        {
            get { return (_flags & TypeAttributes.HasSecurity) != 0; }
        }

        internal override IEnumerable<Microsoft.Cci.SecurityAttribute> GetSecurityInformation()
        {
            throw ExceptionUtilities.Unreachable;
        }

        internal override NamedTypeSymbol ComImportCoClass
        {
            get
            {
                if (!this.IsInterfaceType())
                {
                    return null;
                }

                var uncommon = GetUncommonProperties();
                if (uncommon == s_noUncommonProperties)
                {
                    return null;
                }

                if (ReferenceEquals(uncommon.lazyComImportCoClassType, ErrorTypeSymbol.UnknownResultType))
                {
                    var type = this.ContainingPEModule.TryDecodeAttributeWithTypeArgument(this.Handle, AttributeDescription.CoClassAttribute);
                    var coClassType = ((object)type != null && (type.TypeKind == TypeKind.Class || type.IsErrorType())) ? (NamedTypeSymbol)type : null;

                    Interlocked.CompareExchange(ref uncommon.lazyComImportCoClassType, coClassType, ErrorTypeSymbol.UnknownResultType);
                }

                return uncommon.lazyComImportCoClassType;
            }
        }

        internal override ImmutableArray<string> GetAppliedConditionalSymbols()
        {
            var uncommon = GetUncommonProperties();
            if (uncommon == s_noUncommonProperties)
            {
                return ImmutableArray<string>.Empty;
            }

            if (uncommon.lazyConditionalAttributeSymbols.IsDefault)
            {
                ImmutableArray<string> conditionalSymbols = this.ContainingPEModule.Module.GetConditionalAttributeValues(_handle);
                Debug.Assert(!conditionalSymbols.IsDefault);
                ImmutableInterlocked.InterlockedCompareExchange(ref uncommon.lazyConditionalAttributeSymbols, conditionalSymbols, default(ImmutableArray<string>));
            }

            return uncommon.lazyConditionalAttributeSymbols;
        }

        internal override ObsoleteAttributeData ObsoleteAttributeData
        {
            get
            {
                var uncommon = GetUncommonProperties();
                if (uncommon == s_noUncommonProperties)
                {
                    return null;
                }

                bool ignoreByRefLikeMarker = this.IsRefLikeType;
                ObsoleteAttributeHelpers.InitializeObsoleteDataFromMetadata(ref uncommon.lazyObsoleteAttributeData, _handle, ContainingPEModule, ignoreByRefLikeMarker);
                return uncommon.lazyObsoleteAttributeData;
            }
        }

        internal override AttributeUsageInfo GetAttributeUsageInfo()
        {
            var uncommon = GetUncommonProperties();
            if (uncommon == s_noUncommonProperties)
            {
                return ((object)this.BaseTypeNoUseSiteDiagnostics != null) ? this.BaseTypeNoUseSiteDiagnostics.GetAttributeUsageInfo() : AttributeUsageInfo.Default;
            }

            if (uncommon.lazyAttributeUsageInfo.IsNull)
            {
                uncommon.lazyAttributeUsageInfo = this.DecodeAttributeUsageInfo();
            }

            return uncommon.lazyAttributeUsageInfo;
        }

        private AttributeUsageInfo DecodeAttributeUsageInfo()
        {
            var handle = this.ContainingPEModule.Module.GetAttributeUsageAttributeHandle(_handle);

            if (!handle.IsNil)
            {
                var decoder = new MetadataDecoder(ContainingPEModule);
                TypedConstant[] positionalArgs;
                KeyValuePair<string, TypedConstant>[] namedArgs;
                if (decoder.GetCustomAttribute(handle, out positionalArgs, out namedArgs))
                {
                    AttributeUsageInfo info = AttributeData.DecodeAttributeUsageAttribute(positionalArgs[0], namedArgs.AsImmutableOrNull());
                    return info.HasValidAttributeTargets ? info : AttributeUsageInfo.Default;
                }
            }

            return ((object)this.BaseTypeNoUseSiteDiagnostics != null) ? this.BaseTypeNoUseSiteDiagnostics.GetAttributeUsageInfo() : AttributeUsageInfo.Default;
        }

        internal sealed override CSharpCompilation DeclaringCompilation // perf, not correctness
        {
            get { return null; }
        }

        public IEnumerable<object> fieldDefs { get; set; }

        /// <summary>
        /// Returns the index of the first member of the specific kind.
        /// Returns the number of members if not found.
        /// </summary>
        private static int GetIndexOfFirstMember(ImmutableArray<Symbol> members, SymbolKind kind)
        {
            int n = members.Length;
            for (int i = 0; i < n; i++)
            {
                if (members[i].Kind == kind)
                {
                    return i;
                }
            }
            return n;
        }

        /// <summary>
        /// Returns all members of the specific kind, starting at the optional offset.
        /// Members of the same kind are assumed to be contiguous.
        /// </summary>
        private static IEnumerable<TSymbol> GetMembers<TSymbol>(ImmutableArray<Symbol> members, SymbolKind kind, int offset = -1)
            where TSymbol : Symbol
        {
            if (offset < 0)
            {
                offset = GetIndexOfFirstMember(members, kind);
            }
            int n = members.Length;
            for (int i = offset; i < n; i++)
            {
                var member = members[i];
                if (member.Kind != kind)
                {
                    yield break;
                }
                yield return (TSymbol)member;
            }
        }

        /// <summary>
        /// Specialized PENamedTypeSymbol for types with no type parameters in
        /// metadata (no type parameters on this type and all containing types).
        /// </summary>
        private sealed class PENamedTypeSymbolNonGeneric : PENamedTypeSymbol
        {
            internal PENamedTypeSymbolNonGeneric(
                PEModuleSymbol moduleSymbol,
                NamespaceOrTypeSymbol container,
                TypeDefinitionHandle handle,
                string emittedNamespaceName,
                out bool mangleName) :
                base(moduleSymbol, container, handle, emittedNamespaceName, 0, out mangleName)
            {
            }

            public override int Arity
            {
                get
                {
                    return 0;
                }
            }

            internal override bool MangleName
            {
                get
                {
                    return false;
                }
            }

            internal override int MetadataArity
            {
                get
                {
                    var containingType = _container as PENamedTypeSymbol;
                    return (object)containingType == null ? 0 : containingType.MetadataArity;
                }
            }
        }

        /// <summary>
        /// Specialized PENamedTypeSymbol for types with type parameters in metadata.
        /// NOTE: the type may have Arity == 0 if it has same metadata arity as the metadata arity of the containing type.
        /// </summary>
        private sealed class PENamedTypeSymbolGeneric : PENamedTypeSymbol
        {
            private readonly GenericParameterHandleCollection _genericParameterHandles;
            private readonly ushort _arity;
            private readonly bool _mangleName;
            private ImmutableArray<TypeParameterSymbol> _lazyTypeParameters;

            internal PENamedTypeSymbolGeneric(
                    PEModuleSymbol moduleSymbol,
                    NamespaceOrTypeSymbol container,
                    TypeDefinitionHandle handle,
                    string emittedNamespaceName,
                    GenericParameterHandleCollection genericParameterHandles,
                    ushort arity,
                    out bool mangleName
                )
                : base(moduleSymbol,
                      container,
                      handle,
                      emittedNamespaceName,
                      arity,
                      out mangleName)
            {
                Debug.Assert(genericParameterHandles.Count > 0);
                _arity = arity;
                _genericParameterHandles = genericParameterHandles;
                _mangleName = mangleName;
            }

            public override int Arity
            {
                get
                {
                    return _arity;
                }
            }

            internal override bool MangleName
            {
                get
                {
                    return _mangleName;
                }
            }

            override internal int MetadataArity
            {
                get
                {
                    return _genericParameterHandles.Count;
                }
            }

            internal override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotationsNoUseSiteDiagnostics
            {
                get
                {
                    // This is always the instance type, so the type arguments are the same as the type parameters.
                    return GetTypeParametersAsTypeArguments();
                }
            }

            public override ImmutableArray<TypeParameterSymbol> TypeParameters
            {
                get
                {
                    EnsureTypeParametersAreLoaded();
                    return _lazyTypeParameters;
                }
            }

            private void EnsureTypeParametersAreLoaded()
            {
                if (_lazyTypeParameters.IsDefault)
                {
                    var moduleSymbol = ContainingPEModule;

                    // If this is a nested type generic parameters in metadata include generic parameters of the outer types.
                    int firstIndex = _genericParameterHandles.Count - _arity;

                    TypeParameterSymbol[] ownedParams = new TypeParameterSymbol[_arity];
                    for (int i = 0; i < ownedParams.Length; i++)
                    {
                        ownedParams[i] = new PETypeParameterSymbol(moduleSymbol, this, (ushort)i, _genericParameterHandles[firstIndex + i]);
                    }

                    ImmutableInterlocked.InterlockedInitialize(ref _lazyTypeParameters,
                        ImmutableArray.Create<TypeParameterSymbol>(ownedParams));
                }
            }

            protected override DiagnosticInfo GetUseSiteDiagnosticImpl()
            {
                DiagnosticInfo diagnostic = null;

                if (!MergeUseSiteDiagnostics(ref diagnostic, base.GetUseSiteDiagnosticImpl()))
                {
                    // Verify type parameters for containing types
                    // match those on the containing types.
                    if (!MatchesContainingTypeParameters())
                    {
                        diagnostic = new CSDiagnosticInfo(ErrorCode.ERR_BogusType, this);
                    }
                }

                return diagnostic;
            }

            /// <summary>
            /// Return true if the type parameters specified on the nested type (this),
            /// that represent the corresponding type parameters on the containing
            /// types, in fact match the actual type parameters on the containing types.
            /// </summary>
            private bool MatchesContainingTypeParameters()
            {
                var container = this.ContainingType;
                if ((object)container == null)
                {
                    return true;
                }

                var containingTypeParameters = container.GetAllTypeParameters();
                int n = containingTypeParameters.Length;

                if (n == 0)
                {
                    return true;
                }

                // Create an instance of PENamedTypeSymbol for the nested type, but
                // with all type parameters, from the nested type and all containing
                // types. The type parameters on this temporary type instance are used
                // for comparison with those on the actual containing types. The
                // containing symbol for the temporary type is the namespace directly.
                var nestedType = Create(this.ContainingPEModule, (PENamespaceSymbol)this.ContainingNamespace, _handle, null);
                var nestedTypeParameters = nestedType.TypeParameters;
                var containingTypeMap = new TypeMap(containingTypeParameters, IndexedTypeParameterSymbol.Take(n), allowAlpha: false);
                var nestedTypeMap = new TypeMap(nestedTypeParameters, IndexedTypeParameterSymbol.Take(nestedTypeParameters.Length), allowAlpha: false);

                for (int i = 0; i < n; i++)
                {
                    var containingTypeParameter = containingTypeParameters[i];
                    var nestedTypeParameter = nestedTypeParameters[i];
                    if (!MemberSignatureComparer.HaveSameConstraints(containingTypeParameter, containingTypeMap, nestedTypeParameter, nestedTypeMap))
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}
