// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.CSharp.Emit;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE
{
    /// <summary>
    /// The class to represent all types imported from a PE/module.
    /// </summary>
    internal abstract class PENamedTypeSymbol : NamedTypeSymbol
    {
        private static readonly Dictionary<string, ImmutableArray<PENamedTypeSymbol>> emptyNestedTypes = new Dictionary<string, ImmutableArray<PENamedTypeSymbol>>();

        private readonly NamespaceOrTypeSymbol container;
        private readonly TypeHandle handle;
        private readonly string name;
        private readonly TypeAttributes flags;
        private readonly SpecialType corTypeId;

        /// <summary>
        /// A set of all the names of the members in this type.
        /// We can get names without getting members (which is a more expensive operation)
        /// </summary>
        private ICollection<string> lazyMemberNames;

        /// <summary>
        /// We used to sort symbols on demand and relied on row ids to figure out the order between symbols of the same kind.
        /// However, that was fragile because, when map tables are used in metadata, row ids in the map table define the order
        /// and we don't have them.
        /// Members are grouped by kind. First we store fields, then methods, then properties, then events and finally nested types.
        /// Within groups, members are sorted based on declaration order.
        /// </summary>
        private ImmutableArray<Symbol> lazyMembersInDeclarationOrder;

        /// <summary>
        /// A map of members immediately contained within this type 
        /// grouped by their name (case-sensitively).
        /// </summary>
        private Dictionary<string, ImmutableArray<Symbol>> lazyMembersByName;

        /// <summary>
        /// A map of types immediately contained within this type 
        /// grouped by their name (case-sensitively).
        /// </summary>
        private Dictionary<string, ImmutableArray<PENamedTypeSymbol>> lazyNestedTypes;

        /// <summary>
        /// Lazily initialized by TypeKind property.
        /// </summary>
        private TypeKind lazyKind;

        private NamedTypeSymbol lazyBaseType = ErrorTypeSymbol.UnknownResultType;
        private ImmutableArray<NamedTypeSymbol> lazyInterfaces = default(ImmutableArray<NamedTypeSymbol>);
        private NamedTypeSymbol lazyDeclaredBaseType = ErrorTypeSymbol.UnknownResultType;
        private ImmutableArray<NamedTypeSymbol> lazyDeclaredInterfaces = default(ImmutableArray<NamedTypeSymbol>);

        private Tuple<CultureInfo, string> lazyDocComment;

        private DiagnosticInfo lazyUseSiteDiagnostic = CSDiagnosticInfo.EmptyErrorInfo; // Indicates unknown state. 


        // There is a bunch of type properties relevant only for enums or types with custom attributes.
        // It is fairly easy to check whether a type s is not "uncommon". So we store uncommon properties in 
        // a separate class with a noUncommonProperties singleton used for cases when type is "common".
        // this is done purely to save memory with expectation that "uncommon" cases are indeed uncommon. 
        #region "Uncommon properties"
        private static readonly UncommonProperties noUncommonProperties = new UncommonProperties();
        private UncommonProperties lazyUncommonProperties;

        private UncommonProperties GetUncommonProperties()
        {
            var result = this.lazyUncommonProperties;
            if (result != null)
            {
                Debug.Assert(result != noUncommonProperties || result.IsDefaultValue(), "default value was modified");
                return result;
            }

            if (this.IsUncommon())
            {
                result = new UncommonProperties();
                return Interlocked.CompareExchange(ref this.lazyUncommonProperties, result, null) ?? result;
            }

            this.lazyUncommonProperties = result = noUncommonProperties;
            return result;
        }

        // enums and types with custom attributes are considered uncommon
        private bool IsUncommon()
        {
            if (this.ContainingPEModule.HasAnyCustomAttributes(this.handle))
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
            internal string lazyDefaultMemberName;
            internal NamedTypeSymbol lazyComImportCoClassType = ErrorTypeSymbol.UnknownResultType;

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
                    (object)lazyComImportCoClassType == (object)ErrorTypeSymbol.UnknownResultType;
            }
        }

        #endregion  // Uncommon properties

        internal static PENamedTypeSymbol Create(
            PEModuleSymbol moduleSymbol,
            PENamespaceSymbol containingNamespace,
            TypeHandle handle,
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
                result.lazyUseSiteDiagnostic = new CSDiagnosticInfo(ErrorCode.ERR_BogusType, result);
            }

            return result;
        }

        private static void GetGenericInfo(PEModuleSymbol moduleSymbol, TypeHandle handle, out GenericParameterHandleCollection genericParameterHandles, out ushort arity, out BadImageFormatException mrEx)
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
            TypeHandle handle)
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
                result.lazyUseSiteDiagnostic = new CSDiagnosticInfo(ErrorCode.ERR_BogusType, result);
            }

            return result;
        }

        private PENamedTypeSymbol(
            PEModuleSymbol moduleSymbol,
            NamespaceOrTypeSymbol container,
            TypeHandle handle,
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

            this.handle = handle;
            this.container = container;

            try
            {
                this.flags = moduleSymbol.Module.GetTypeDefFlagsOrThrow(handle);
            }
            catch (BadImageFormatException)
            {
                makeBad = true;
            }

            if (arity == 0)
            {
                this.name = metadataName;
                mangleName = false;
            }
            else
            {
                // Unmangle name for a generic type.
                this.name = MetadataHelpers.UnmangleMetadataNameForArity(metadataName, arity);
                Debug.Assert(ReferenceEquals(this.name, metadataName) == (this.name == metadataName));
                mangleName = !ReferenceEquals(this.name, metadataName);
            }

            // check if this is one of the COR library types
            if (emittedNamespaceName != null &&
                moduleSymbol.ContainingAssembly.KeepLookingForDeclaredSpecialTypes &&
                this.DeclaredAccessibility == Accessibility.Public) // NB: this.flags was set above.
            {
                corTypeId = SpecialTypes.GetTypeFromMetadataName(MetadataHelpers.BuildQualifiedName(emittedNamespaceName, metadataName));
            }
            else
            {
                corTypeId = SpecialType.None;
            }

            if (makeBad)
            {
                this.lazyUseSiteDiagnostic = new CSDiagnosticInfo(ErrorCode.ERR_BogusType, this);
            }
        }

        public override SpecialType SpecialType
        {
            get
            {
                return corTypeId;
            }
        }

        internal PEModuleSymbol ContainingPEModule
        {
            get
            {
                Symbol s = container;

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

        internal TypeHandle Handle
        {
            get
            {
                return handle;
            }
        }

        internal override NamedTypeSymbol BaseTypeNoUseSiteDiagnostics
        {
            get
            {
                if (ReferenceEquals(lazyBaseType, ErrorTypeSymbol.UnknownResultType))
                {
                    Interlocked.CompareExchange(ref lazyBaseType, MakeAcyclicBaseType(), ErrorTypeSymbol.UnknownResultType);
                }

                return lazyBaseType;
            }
        }

        internal override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics
        {
            get
            {
                if (lazyInterfaces.IsDefault)
                {
                    ImmutableInterlocked.InterlockedCompareExchange(ref lazyInterfaces, MakeAcyclicInterfaces(), default(ImmutableArray<NamedTypeSymbol>));
                }

                return lazyInterfaces;
            }
        }

        internal override ImmutableArray<NamedTypeSymbol> GetInterfacesToEmit()
        {
            return InterfacesNoUseSiteDiagnostics;
        }

        internal override NamedTypeSymbol GetDeclaredBaseType(ConsList<Symbol> basesBeingResolved)
        {
            if (ReferenceEquals(lazyDeclaredBaseType, ErrorTypeSymbol.UnknownResultType))
            {
                Interlocked.CompareExchange(ref lazyDeclaredBaseType, MakeDeclaredBaseType(), ErrorTypeSymbol.UnknownResultType);
            }

            return lazyDeclaredBaseType;
        }

        internal override ImmutableArray<NamedTypeSymbol> GetDeclaredInterfaces(ConsList<Symbol> basesBeingResolved)
        {
            if (lazyDeclaredInterfaces.IsDefault)
            {
                ImmutableInterlocked.InterlockedCompareExchange(ref lazyDeclaredInterfaces, MakeDeclaredInterfaces(), default(ImmutableArray<NamedTypeSymbol>));
            }

            return lazyDeclaredInterfaces;
        }

        private NamedTypeSymbol MakeDeclaredBaseType()
        {
            if (!flags.IsInterface())
            {
                try
                {
                    var moduleSymbol = ContainingPEModule;
                    Handle token = moduleSymbol.Module.GetBaseTypeOfTypeOrThrow(handle);

                    if (!token.IsNil)
                    {
                        TypeSymbol decodedType = new MetadataDecoder(moduleSymbol, this).GetTypeOfToken(token);
                        return (NamedTypeSymbol)DynamicTypeDecoder.TransformType(decodedType, 0, this.handle, moduleSymbol);
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
                var interfaceImpls = moduleSymbol.Module.GetImplementedInterfacesOrThrow(handle);

                if (interfaceImpls.Count > 0)
                {
                    NamedTypeSymbol[] symbols = new NamedTypeSymbol[interfaceImpls.Count];
                    var tokenDecoder = new MetadataDecoder(moduleSymbol, this);

                    int i = 0;
                    foreach (var interfaceImpl in interfaceImpls)
                    {
                        TypeSymbol typeSymbol = tokenDecoder.GetTypeOfToken(interfaceImpl);

                        var namedTypeSymbol = typeSymbol as NamedTypeSymbol;
                        symbols[i++] = (object)namedTypeSymbol != null ? namedTypeSymbol : new UnsupportedMetadataTypeSymbol(); // interface tmpList contains a bad type
                    }

                    return symbols.AsImmutableOrNull();
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
                return this.container;
            }
        }

        public override NamedTypeSymbol ContainingType
        {
            get
            {
                return this.container as NamedTypeSymbol;
            }
        }

        public override Accessibility DeclaredAccessibility
        {
            get
            {
                Accessibility access = Accessibility.Private;

                switch (this.flags & TypeAttributes.VisibilityMask)
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
                        Debug.Assert(false, "Unexpected!!!");
                        break;
                }

                return access;
            }
        }

        public override NamedTypeSymbol EnumUnderlyingType
        {
            get
            {
                var uncommon = GetUncommonProperties();
                if (uncommon == noUncommonProperties)
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
            if (uncommon == noUncommonProperties)
            {
                return ImmutableArray<CSharpAttributeData>.Empty;
            }

            if (uncommon.lazyCustomAttributes.IsDefault)
            {
                if (MightContainExtensionMethods)
                {
                    this.ContainingPEModule.LoadCustomAttributesFilterExtensions(
                        this.Handle,
                        ref uncommon.lazyCustomAttributes);
                }
                else
                {
                    this.ContainingPEModule.LoadCustomAttributes(this.Handle,
                        ref uncommon.lazyCustomAttributes);
                }
            }

            return uncommon.lazyCustomAttributes;
        }

        internal override IEnumerable<CSharpAttributeData> GetCustomAttributesToEmit(ModuleCompilationState compilationState)
        {
            return GetAttributes();
        }

        public override IEnumerable<string> MemberNames
        {
            get
            {
                EnsureNonTypeMemberNamesAreLoaded();
                return lazyMemberNames;
            }
        }

        private void EnsureNonTypeMemberNamesAreLoaded()
        {
            if (lazyMemberNames == null)
            {
                var moduleSymbol = ContainingPEModule;
                var module = moduleSymbol.Module;

                var names = new HashSet<string>();

                try
                {
                    foreach (var methodDef in module.GetMethodsOfTypeOrThrow(this.handle))
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
                    foreach (var propertyDef in module.GetPropertiesOfTypeOrThrow(this.handle))
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
                    foreach (var fieldDef in module.GetFieldsOfTypeOrThrow(this.handle))
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

                // TODO(cyrusn): Handle Events

                Interlocked.CompareExchange(ref lazyMemberNames, CreateReadOnlyMemberNames(names), null);
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

        internal override ImmutableArray<Symbol> GetMembersUnordered()
        {
            ImmutableArray<Symbol> result = GetMembers();
#if DEBUG
            // In DEBUG, swap first and last elements so that use of Unordered in a place it isn't warranted is caught
            // more obviously.
            return result.DeOrder();
#else
            return result;
#endif
        }

        public override ImmutableArray<Symbol> GetMembers()
        {
            EnsureAllMembersAreLoaded();
            return lazyMembersInDeclarationOrder;
        }

        private IEnumerable<FieldSymbol> GetEnumFieldsToEmit()
        {
            var uncommon = GetUncommonProperties();
            if (uncommon == noUncommonProperties)
            {
                yield break;
            }

            var moduleSymbol = this.ContainingPEModule;
            var module = moduleSymbol.Module;

            // Non-static fields of enum types are not imported by default because they are not bindable,
            // but we need them for NoPia.

            var fieldDefs = ArrayBuilder<FieldHandle>.GetInstance();

            try
            {
                foreach (var fieldDef in module.GetFieldsOfTypeOrThrow(handle))
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
                // If there are any fields, they are at the very beginning.
                return GetMembers<FieldSymbol>(this.GetMembers(), SymbolKind.Field, offset: 0);
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
                    if (!method.IsParameterlessValueTypeConstructor(requireSynthesized: true))
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

                var methodDefs = ArrayBuilder<MethodHandle>.GetInstance();

                try
                {
                    foreach (var methodDef in module.GetMethodsOfTypeOrThrow(this.handle))
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
                    foreach (var fieldDef in module.GetFieldsOfTypeOrThrow(this.handle))
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
                            ImmutableArray<MetadataDecoder.ModifierInfo> customModifiers;
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
            if (lazyMembersByName == null)
            {
                LoadMembers();
            }
        }

        private void LoadMembers()
        {
            ArrayBuilder<Symbol> members = null;

            if (lazyMembersInDeclarationOrder.IsDefault)
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
                        foreach (var fieldDef in module.GetFieldsOfTypeOrThrow(this.handle))
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
                    this.CreateFields(members);
                    int fieldCount = members.Count;

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
                    Dictionary<MethodHandle, PEMethodSymbol> methodHandleToSymbol = this.CreateMethods(members);

                    if (this.TypeKind == TypeKind.Struct)
                    {
                        bool haveParameterlessConstructor = false;
                        for (int i = fieldCount; i < members.Count; i++)
                        {
                            var method = (MethodSymbol)members[i];

                            if (method.IsParameterlessValueTypeConstructor())
                            {
                                haveParameterlessConstructor = true;
                                break;
                            }
                        }

                        // Structs have an implicit parameterless constructor, even if it
                        // does not appear in metadata (11.3.8)
                        if (!haveParameterlessConstructor)
                        {
                            members.Insert(fieldCount, new SynthesizedInstanceConstructor(this));
                        }
                    }

                    this.CreateProperties(methodHandleToSymbol, members);
                    this.CreateEvents(methodHandleToSymbol, members);
                }

                // Now add types to the end.
                int membersCount = members.Count;

                foreach (var typeArray in lazyNestedTypes.Values)
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

                if (!ImmutableInterlocked.InterlockedInitialize(ref lazyMembersInDeclarationOrder, membersInDeclarationOrder))
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

            if (lazyMembersByName == null)
            {
                if (members == null)
                {
                    members = ArrayBuilder<Symbol>.GetInstance();
                    foreach (var member in lazyMembersInDeclarationOrder)
                    {
                        if (member.Kind == SymbolKind.NamedType)
                        {
                            break;
                        }
                        members.Add(member);
                    }
                }

                Dictionary<string, ImmutableArray<Symbol>> membersDict = GroupByName(members);

                var exchangeResult = Interlocked.CompareExchange(ref lazyMembersByName, membersDict, null);
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
                    Interlocked.Exchange(ref lazyMemberNames, memberNames);
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
            if (!lazyMembersByName.TryGetValue(name, out m))
            {
                m = ImmutableArray<Symbol>.Empty;
            }

            return m;
        }

        public override ImmutableArray<Symbol> GetMembers(string name)
        {
            EnsureAllMembersAreLoaded();

            ImmutableArray<Symbol> m;
            if (!lazyMembersByName.TryGetValue(name, out m))
            {
                m = ImmutableArray<Symbol>.Empty;
            }

            // nested types are not common, but we need to check just in case
            ImmutableArray<PENamedTypeSymbol> t;
            if (lazyNestedTypes.TryGetValue(name, out t))
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
            ImmutableArray<NamedTypeSymbol> result = GetTypeMembers();
#if DEBUG
            // In DEBUG, swap first and last elements so that use of Unordered in a place it isn't warranted is caught
            // more obviously.
            return result.DeOrder();
#else
            return result;
#endif
        }

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers()
        {
            EnsureNestedTypesAreLoaded();
            return GetMemberTypesPrivate();
        }

        private ImmutableArray<NamedTypeSymbol> GetMemberTypesPrivate()
        {
            var builder = ArrayBuilder<NamedTypeSymbol>.GetInstance();
            foreach (var typeArray in lazyNestedTypes.Values)
            {
                builder.AddRange(typeArray);
            }

            return builder.ToImmutableAndFree();
        }

        private void EnsureNestedTypesAreLoaded()
        {
            if (lazyNestedTypes == null)
            {
                var types = ArrayBuilder<PENamedTypeSymbol>.GetInstance();
                types.AddRange(this.CreateNestedTypes());
                var typesDict = GroupByName(types);

                var exchangeResult = Interlocked.CompareExchange(ref lazyNestedTypes, typesDict, null);
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

            if (lazyNestedTypes.TryGetValue(name, out t))
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
                return name;
            }
        }

        internal override bool HasSpecialName
        {
            get
            {
                return (flags & TypeAttributes.SpecialName) != 0;
            }
        }

        internal override ImmutableArray<TypeSymbol> TypeArgumentsNoUseSiteDiagnostics
        {
            get
            {
                return ImmutableArray<TypeSymbol>.Empty;
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
                    (flags & TypeAttributes.Sealed) != 0 &&
                    (flags & TypeAttributes.Abstract) != 0;
            }
        }

        public override bool IsAbstract
        {
            get
            {
                return
                    (flags & TypeAttributes.Abstract) != 0 &&
                    (flags & TypeAttributes.Sealed) == 0;
            }
        }

        internal override bool IsMetadataAbstract
        {
            get
            {
                return (flags & TypeAttributes.Abstract) != 0;
            }
        }

        public override bool IsSealed
        {
            get
            {
                return
                    (flags & TypeAttributes.Sealed) != 0 &&
                    (flags & TypeAttributes.Abstract) == 0;
            }
        }

        internal override bool IsMetadataSealed
        {
            get
            {
                return (flags & TypeAttributes.Sealed) != 0;
            }
        }

        internal TypeAttributes Flags
        {
            get
            {
                return flags;
            }
        }

        public override bool MightContainExtensionMethods
        {
            get
            {
                var uncommon = GetUncommonProperties();
                if (uncommon == noUncommonProperties)
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
                            bool moduleHasExtension = module.HasExtensionAttribute(this.handle, ignoreCase: false);

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
                if (lazyKind == TypeKind.Unknown)
                {
                    TypeKind result;

                    if (flags.IsInterface())
                    {
                        result = TypeKind.Interface;
                    }
                    else
                    {
                        TypeSymbol @base = GetDeclaredBaseType(null);

                        result = TypeKind.Class;

                        if ((object)@base != null)
                        {
                            SpecialType baseCorTypeId = @base.SpecialType;

                            // Code is cloned from MetaImport::DoImportBaseAndImplements()
                            if (baseCorTypeId == SpecialType.System_Enum)
                            {
                                // Enum
                                result = TypeKind.Enum;
                            }
                            else if (baseCorTypeId == SpecialType.System_MulticastDelegate)
                            {
                                // Delegate
                                result = TypeKind.Delegate;
                            }
                            else if (baseCorTypeId == SpecialType.System_ValueType &&
                                     this.SpecialType != SpecialType.System_Enum)
                            {
                                // Struct
                                result = TypeKind.Struct;
                            }
                        }
                    }

                    lazyKind = result;
                }

                return lazyKind;
            }
        }

        internal sealed override bool IsInterface
        {
            get
            {
                return flags.IsInterface();
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

            if (BaseTypeAnalysis.ClassDependsOn(declaredBase, this))
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
                .SelectAsArray(t => BaseTypeAnalysis.InterfaceDependsOn(t, this) ? CyclicInheritanceError(this, t) : t);
        }

        public override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return PEDocumentationCommentUtils.GetDocumentationComment(this, ContainingPEModule, preferredCulture, cancellationToken, ref lazyDocComment);
        }

        private IEnumerable<PENamedTypeSymbol> CreateNestedTypes()
        {
            var moduleSymbol = this.ContainingPEModule;
            var module = moduleSymbol.Module;

            ImmutableArray<TypeHandle> nestedTypeDefs;

            try
            {
                nestedTypeDefs = module.GetNestedTypeDefsOrThrow(this.handle);
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

        private void CreateFields(ArrayBuilder<Symbol> members)
        {
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
                foreach (var fieldRid in module.GetFieldsOfTypeOrThrow(this.handle))
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

                    members.Add(new PEFieldSymbol(moduleSymbol, this, fieldRid));
                }
            }
            catch (BadImageFormatException)
            { }
        }

        private Dictionary<MethodHandle, PEMethodSymbol> CreateMethods(ArrayBuilder<Symbol> members)
        {
            var moduleSymbol = this.ContainingPEModule;
            var module = moduleSymbol.Module;
            var map = new Dictionary<MethodHandle, PEMethodSymbol>();

            // for ordinary embeddable struct types we import private members so that we can report appropriate errors if the structure is used 
            var isOrdinaryEmbeddableStruct = (this.TypeKind == TypeKind.Struct) && (this.SpecialType == Microsoft.CodeAnalysis.SpecialType.None) && this.ContainingAssembly.IsLinked;

            try
            {
                foreach (var methodHandle in module.GetMethodsOfTypeOrThrow(this.handle))
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

        private void CreateProperties(Dictionary<MethodHandle, PEMethodSymbol> methodHandleToSymbol, ArrayBuilder<Symbol> members)
        {
            var moduleSymbol = this.ContainingPEModule;
            var module = moduleSymbol.Module;

            try
            {
                foreach (var propertyDef in module.GetPropertiesOfTypeOrThrow(this.handle))
                {
                    try
                    {
                        var methods = module.GetPropertyMethodsOrThrow(propertyDef);

                        PEMethodSymbol getMethod = GetAccessorMethod(module, methodHandleToSymbol, methods.Getter);
                        PEMethodSymbol setMethod = GetAccessorMethod(module, methodHandleToSymbol, methods.Setter);

                        if (((object)getMethod != null) || ((object)setMethod != null))
                        {
                            members.Add(new PEPropertySymbol(moduleSymbol, this, propertyDef, getMethod, setMethod));
                        }
                    }
                    catch (BadImageFormatException)
                    { }
                }
            }
            catch (BadImageFormatException)
            { }
        }

        private void CreateEvents(Dictionary<MethodHandle, PEMethodSymbol> methodHandleToSymbol, ArrayBuilder<Symbol> members)
        {
            var moduleSymbol = this.ContainingPEModule;
            var module = moduleSymbol.Module;

            try
            {
                foreach (var eventRid in module.GetEventsOfTypeOrThrow(this.handle))
                {
                    try
                    {
                        var methods = module.GetEventMethodsOrThrow(eventRid);

                        // NOTE: C# ignores all other accessors (most notably, raise/fire).
                        PEMethodSymbol addMethod = GetAccessorMethod(module, methodHandleToSymbol, methods.AddOn);
                        PEMethodSymbol removeMethod = GetAccessorMethod(module, methodHandleToSymbol, methods.RemoveOn);

                        // NOTE: both accessors are required, but that will be reported separately.
                        // Create the symbol unless both accessors are missing.
                        if (((object)addMethod != null) || ((object)removeMethod != null))
                        {
                            members.Add(new PEEventSymbol(moduleSymbol, this, eventRid, addMethod, removeMethod));
                        }
                    }
                    catch (BadImageFormatException)
                    { }
                }
            }
            catch (BadImageFormatException)
            { }
        }

        private PEMethodSymbol GetAccessorMethod(PEModule module, Dictionary<MethodHandle, PEMethodSymbol> methodHandleToSymbol, MethodHandle methodDef)
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
            return symbols.ToDictionary(s => s.Name);
        }

        private static Dictionary<string, ImmutableArray<PENamedTypeSymbol>> GroupByName(ArrayBuilder<PENamedTypeSymbol> symbols)
        {
            if (symbols.Count == 0)
            {
                return emptyNestedTypes;
            }

            return symbols.ToDictionary(s => s.Name);
        }


        internal override DiagnosticInfo GetUseSiteDiagnostic()
        {
            if (ReferenceEquals(lazyUseSiteDiagnostic, CSDiagnosticInfo.EmptyErrorInfo))
            {
                lazyUseSiteDiagnostic = GetUseSiteDiagnosticImpl();
            }

            return lazyUseSiteDiagnostic;
        }

        protected virtual DiagnosticInfo GetUseSiteDiagnosticImpl()
        {
            DiagnosticInfo diagnostic = null;

            if (!MergeUseSiteDiagnostics(ref diagnostic, CalculateUseSiteDiagnostic()))
            {
                // Check if this type is marked by RequiredAttribute attribute.
                // If so mark the type as bad, because it relies upon semantics that are not understood by the C# compiler.
                if (this.ContainingPEModule.Module.HasRequiredAttributeAttribute(this.handle))
                {
                    diagnostic = new CSDiagnosticInfo(ErrorCode.ERR_BogusType, this);
                }
            }

            return diagnostic;
        }

        internal string DefaultMemberName
        {
            get
            {
                var uncommon = GetUncommonProperties();
                if (uncommon == noUncommonProperties)
                {
                    return "";
                }

                if (uncommon.lazyDefaultMemberName == null)
                {
                    string defaultMemberName;
                    this.ContainingPEModule.Module.HasDefaultMemberAttribute(this.handle, out defaultMemberName);

                    // NOTE: the default member name is frequently null (e.g. if there is not indexer in the type).
                    // Make sure we set a non-null value so that we don't recompute it repeatedly.
                    // CONSIDER: this makes it impossible to distinguish between not having the attribute and
                    // haveing the attribute with a value of "".
                    Interlocked.CompareExchange(ref uncommon.lazyDefaultMemberName, defaultMemberName ?? "", null);
                }
                return uncommon.lazyDefaultMemberName;
            }
        }

        internal override bool IsComImport
        {
            get
            {
                return (flags & TypeAttributes.Import) != 0;
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
                return (flags & TypeAttributes.WindowsRuntime) != 0;
            }
        }

        internal override bool GetGuidString(out string guidString)
        {
            return ContainingPEModule.Module.HasGuidAttribute(this.handle, out guidString);
        }

        internal override TypeLayout Layout
        {
            get
            {
                return this.ContainingPEModule.Module.GetTypeLayout(handle);
            }
        }

        internal override CharSet MarshallingCharSet
        {
            get
            {
                CharSet result = flags.ToCharSet();

                if (result == 0)
                {
                    // TODO(tomat): report error
                    return CharSet.Ansi;
                }

                return result;
            }
        }

        internal override bool IsSerializable
        {
            get { return (flags & TypeAttributes.Serializable) != 0; }
        }

        internal override bool HasDeclarativeSecurity
        {
            get { return (flags & TypeAttributes.HasSecurity) != 0; }
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
                if (uncommon == noUncommonProperties)
                {
                    return null;
                }

                if (ReferenceEquals(uncommon.lazyComImportCoClassType, ErrorTypeSymbol.UnknownResultType))
                {
                    Interlocked.CompareExchange(ref uncommon.lazyComImportCoClassType, MakeComImportCoClassType(), ErrorTypeSymbol.UnknownResultType);
                }

                return uncommon.lazyComImportCoClassType;
            }
        }

        private NamedTypeSymbol MakeComImportCoClassType()
        {
            Debug.Assert(this.IsInterfaceType());
            string coClassTypeName;
            if (this.ContainingPEModule.Module.HasCoClassAttribute(this.handle, out coClassTypeName))
            {
                var decoder = new MetadataDecoder(this.ContainingPEModule);
                var namedType = decoder.GetTypeSymbolForSerializedType(coClassTypeName);
                if (namedType.TypeKind == TypeKind.Class || namedType.IsErrorType())
                {
                    return (NamedTypeSymbol)namedType;
                }
            }

            return null;
        }

        internal override ImmutableArray<string> GetAppliedConditionalSymbols()
        {
            var uncommon = GetUncommonProperties();
            if (uncommon == noUncommonProperties)
            {
                return ImmutableArray<string>.Empty;
            }

            if (uncommon.lazyConditionalAttributeSymbols.IsDefault)
            {
                ImmutableArray<string> conditionalSymbols = this.ContainingPEModule.Module.GetConditionalAttributeValues(this.handle);
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
                if (uncommon == noUncommonProperties)
                {
                    return null;
                }

                ObsoleteAttributeHelpers.InitializeObsoleteDataFromMetadata(ref uncommon.lazyObsoleteAttributeData, this.handle, ContainingPEModule);
                return uncommon.lazyObsoleteAttributeData;
            }
        }

        internal override AttributeUsageInfo GetAttributeUsageInfo()
        {
            var uncommon = GetUncommonProperties();
            if (uncommon == noUncommonProperties)
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
            var handle = this.ContainingPEModule.Module.GetAttributeUsageAttributeHandle(this.handle);

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
                TypeHandle handle,
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
                    var containingType = this.container as PENamedTypeSymbol;
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
            private readonly GenericParameterHandleCollection genericParameterHandles;
            private readonly ushort arity;
            private readonly bool mangleName;
            private ImmutableArray<TypeParameterSymbol> lazyTypeParameters;

            internal PENamedTypeSymbolGeneric(
                    PEModuleSymbol moduleSymbol,
                    NamespaceOrTypeSymbol container,
                    TypeHandle handle,
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
                this.arity = arity;
                this.genericParameterHandles = genericParameterHandles;
                this.mangleName = mangleName;
            }

            public override int Arity
            {
                get
                {
                    return this.arity;
                }
            }

            internal override bool MangleName
            {
                get
                {
                    return this.mangleName;
                }
            }

            override internal int MetadataArity
            {
                get
                {
                    return genericParameterHandles.Count;
                }
            }

            internal override ImmutableArray<TypeSymbol> TypeArgumentsNoUseSiteDiagnostics
            {
                get
                {
                    // This is always the instance type, so the type arguments are the same as the type parameters.
                    return this.TypeParameters.Cast<TypeParameterSymbol, TypeSymbol>();
                }
            }

            public override ImmutableArray<TypeParameterSymbol> TypeParameters
            {
                get
                {
                    EnsureTypeParametersAreLoaded();
                    return lazyTypeParameters;
                }
            }

            private void EnsureTypeParametersAreLoaded()
            {
                if (lazyTypeParameters.IsDefault)
                {
                    var moduleSymbol = ContainingPEModule;

                    // If this is a nested type generic parameters in metadata include generic parameters of the outer types.
                    int firstIndex = genericParameterHandles.Count - arity;

                    TypeParameterSymbol[] ownedParams = new TypeParameterSymbol[arity];
                    for (int i = 0; i < ownedParams.Length; i++)
                    {
                        ownedParams[i] = new PETypeParameterSymbol(moduleSymbol, this, (ushort)i, genericParameterHandles[firstIndex + i]);
                    }

                    ImmutableInterlocked.InterlockedInitialize(ref lazyTypeParameters,
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
                var nestedType = Create(this.ContainingPEModule, (PENamespaceSymbol)this.ContainingNamespace, this.handle, null);
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
