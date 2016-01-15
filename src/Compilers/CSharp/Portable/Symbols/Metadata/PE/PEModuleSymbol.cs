// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.CSharp.Symbols.Retargeting;
using Roslyn.Utilities;
using System.Reflection.PortableExecutable;
using System.Reflection;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE
{
    /// <summary>
    /// Represents a net-module imported from a PE. Can be a primary module of an assembly.
    /// </summary>
    internal sealed class PEModuleSymbol : NonMissingModuleSymbol
    {
        /// <summary>
        /// Owning AssemblySymbol. This can be a PEAssemblySymbol or a SourceAssemblySymbol.
        /// </summary>
        private readonly AssemblySymbol _assemblySymbol;
        private readonly int _ordinal;

        /// <summary>
        /// A Module object providing metadata.
        /// </summary>
        private readonly PEModule _module;

        /// <summary>
        /// Global namespace.
        /// </summary>
        private readonly PENamespaceSymbol _globalNamespace;

        /// <summary>
        /// Cache the symbol for well-known type System.Type because we use it frequently
        /// (for attributes).
        /// </summary>
        private NamedTypeSymbol _lazySystemTypeSymbol;
        private NamedTypeSymbol _lazyEventRegistrationTokenSymbol;
        private NamedTypeSymbol _lazyEventRegistrationTokenTableSymbol;

        /// <summary>
        /// The same value as ConcurrentDictionary.DEFAULT_CAPACITY
        /// </summary>
        private const int DefaultTypeMapCapacity = 31;

        /// <summary>
        /// This is a map from TypeDef handle to the target <see cref="TypeSymbol"/>. 
        /// It is used by <see cref="MetadataDecoder"/> to speed up type reference resolution
        /// for metadata coming from this module. The map is lazily populated
        /// as we load types from the module.
        /// </summary>
        internal readonly ConcurrentDictionary<TypeDefinitionHandle, TypeSymbol> TypeHandleToTypeMap =
                                    new ConcurrentDictionary<TypeDefinitionHandle, TypeSymbol>(concurrencyLevel: 2, capacity: DefaultTypeMapCapacity);

        /// <summary>
        /// This is a map from TypeRef row id to the target <see cref="TypeSymbol"/>. 
        /// It is used by <see cref="MetadataDecoder"/> to speed up type reference resolution
        /// for metadata coming from this module. The map is lazily populated
        /// by <see cref="MetadataDecoder"/> as we resolve TypeRefs from the module.
        /// </summary>
        internal readonly ConcurrentDictionary<TypeReferenceHandle, TypeSymbol> TypeRefHandleToTypeMap =
                                    new ConcurrentDictionary<TypeReferenceHandle, TypeSymbol>(concurrencyLevel: 2, capacity: DefaultTypeMapCapacity);

        internal readonly ImmutableArray<MetadataLocation> MetadataLocation;

        internal readonly MetadataImportOptions ImportOptions;

        /// <summary>
        /// Module's custom attributes
        /// </summary>
        private ImmutableArray<CSharpAttributeData> _lazyCustomAttributes;

        /// <summary>
        /// Module's assembly attributes
        /// </summary>
        private ImmutableArray<CSharpAttributeData> _lazyAssemblyAttributes;

        // Type names from module
        private ICollection<string> _lazyTypeNames;

        // Namespace names from module
        private ICollection<string> _lazyNamespaceNames;

        internal PEModuleSymbol(PEAssemblySymbol assemblySymbol, PEModule module, MetadataImportOptions importOptions, int ordinal)
            : this((AssemblySymbol)assemblySymbol, module, importOptions, ordinal)
        {
            Debug.Assert(ordinal >= 0);
        }

        internal PEModuleSymbol(SourceAssemblySymbol assemblySymbol, PEModule module, MetadataImportOptions importOptions, int ordinal)
            : this((AssemblySymbol)assemblySymbol, module, importOptions, ordinal)
        {
            Debug.Assert(ordinal > 0);
        }

        internal PEModuleSymbol(RetargetingAssemblySymbol assemblySymbol, PEModule module, MetadataImportOptions importOptions, int ordinal)
            : this((AssemblySymbol)assemblySymbol, module, importOptions, ordinal)
        {
            Debug.Assert(ordinal > 0);
        }

        private PEModuleSymbol(AssemblySymbol assemblySymbol, PEModule module, MetadataImportOptions importOptions, int ordinal)
        {
            Debug.Assert((object)assemblySymbol != null);
            Debug.Assert(module != null);

            _assemblySymbol = assemblySymbol;
            _ordinal = ordinal;
            _module = module;
            this.ImportOptions = importOptions;
            _globalNamespace = new PEGlobalNamespaceSymbol(this);

            this.MetadataLocation = ImmutableArray.Create<MetadataLocation>(new MetadataLocation(this));
        }

        internal override int Ordinal
        {
            get
            {
                return _ordinal;
            }
        }

        internal override Machine Machine
        {
            get
            {
                return _module.Machine;
            }
        }

        internal override bool Bit32Required
        {
            get
            {
                return _module.Bit32Required;
            }
        }

        internal PEModule Module
        {
            get
            {
                return _module;
            }
        }

        public override NamespaceSymbol GlobalNamespace
        {
            get { return _globalNamespace; }
        }

        public override string Name
        {
            get
            {
                return _module.Name;
            }
        }

        private static EntityHandle Token
        {
            get
            {
                return EntityHandle.ModuleDefinition;
            }
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return _assemblySymbol;
            }
        }

        public override AssemblySymbol ContainingAssembly
        {
            get
            {
                return _assemblySymbol;
            }
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return this.MetadataLocation.Cast<MetadataLocation, Location>();
            }
        }

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            if (_lazyCustomAttributes.IsDefault)
            {
                this.LoadCustomAttributes(Token, ref _lazyCustomAttributes);
            }
            return _lazyCustomAttributes;
        }

        internal ImmutableArray<CSharpAttributeData> GetAssemblyAttributes()
        {
            if (_lazyAssemblyAttributes.IsDefault)
            {
                ArrayBuilder<CSharpAttributeData> moduleAssemblyAttributesBuilder = null;

                string corlibName = ContainingAssembly.CorLibrary.Name;
                EntityHandle assemblyMSCorLib = Module.GetAssemblyRef(corlibName);
                if (!assemblyMSCorLib.IsNil)
                {
                    foreach (var qualifier in Cci.MetadataWriter.dummyAssemblyAttributeParentQualifier)
                    {
                        EntityHandle typerefAssemblyAttributesGoHere =
                                    Module.GetTypeRef(
                                        assemblyMSCorLib,
                                        Cci.MetadataWriter.dummyAssemblyAttributeParentNamespace,
                                        Cci.MetadataWriter.dummyAssemblyAttributeParentName + qualifier);

                        if (!typerefAssemblyAttributesGoHere.IsNil)
                        {
                            try
                            {
                                foreach (var customAttributeHandle in Module.GetCustomAttributesOrThrow(typerefAssemblyAttributesGoHere))
                                {
                                    if (moduleAssemblyAttributesBuilder == null)
                                    {
                                        moduleAssemblyAttributesBuilder = new ArrayBuilder<CSharpAttributeData>();
                                    }
                                    moduleAssemblyAttributesBuilder.Add(new PEAttributeData(this, customAttributeHandle));
                                }
                            }
                            catch (BadImageFormatException)
                            { }
                        }
                    }
                }

                ImmutableInterlocked.InterlockedCompareExchange(
                    ref _lazyAssemblyAttributes,
                    (moduleAssemblyAttributesBuilder != null) ? moduleAssemblyAttributesBuilder.ToImmutableAndFree() : ImmutableArray<CSharpAttributeData>.Empty,
                    default(ImmutableArray<CSharpAttributeData>));
            }
            return _lazyAssemblyAttributes;
        }

        internal void LoadCustomAttributes(EntityHandle token, ref ImmutableArray<CSharpAttributeData> customAttributes)
        {
            var loaded = GetCustomAttributesForToken(token);
            ImmutableInterlocked.InterlockedInitialize(ref customAttributes, loaded);
        }

        internal void LoadCustomAttributesFilterExtensions(EntityHandle token,
            ref ImmutableArray<CSharpAttributeData> customAttributes,
            out bool foundExtension)
        {
            var loadedCustomAttributes = GetCustomAttributesFilterExtensions(token, out foundExtension);
            ImmutableInterlocked.InterlockedInitialize(ref customAttributes, loadedCustomAttributes);
        }

        internal void LoadCustomAttributesFilterExtensions(EntityHandle token,
            ref ImmutableArray<CSharpAttributeData> customAttributes)
        {
            // Ignore whether or not extension attributes were found
            bool ignore;
            var loadedCustomAttributes = GetCustomAttributesFilterExtensions(token, out ignore);
            ImmutableInterlocked.InterlockedInitialize(ref customAttributes, loadedCustomAttributes);
        }

        /// <summary>
        /// Returns a possibly ExtensionAttribute filtered roArray of attributes. If
        /// filterExtensionAttributes is set to true, the method will remove all ExtensionAttributes
        /// from the returned array. If it is false, the parameter foundExtension will always be set to
        /// false and can be safely ignored.
        /// 
        /// The paramArrayAttribute parameter is similar to the foundExtension parameter, but instead
        /// of just indicating if the attribute was found, the parameter is set to the attribute handle
        /// for the ParamArrayAttribute if any is found and is null otherwise. This allows NoPia to filter
        /// the attribute out for the symbol but still cache it separately for emit.
        /// </summary>
        internal ImmutableArray<CSharpAttributeData> GetCustomAttributesForToken(EntityHandle token,
            out CustomAttributeHandle filteredOutAttribute1,
            AttributeDescription filterOut1,
            out CustomAttributeHandle filteredOutAttribute2,
            AttributeDescription filterOut2)
        {
            filteredOutAttribute1 = default(CustomAttributeHandle);
            filteredOutAttribute2 = default(CustomAttributeHandle);
            ArrayBuilder<CSharpAttributeData> customAttributesBuilder = null;

            try
            {
                foreach (var customAttributeHandle in _module.GetCustomAttributesOrThrow(token))
                {
                    if (filterOut1.Signatures != null &&
                        Module.GetTargetAttributeSignatureIndex(customAttributeHandle, filterOut1) != -1)
                    {
                        // It is important to capture the last application of the attribute that we run into,
                        // it makes a difference for default and constant values.
                        filteredOutAttribute1 = customAttributeHandle;
                        continue;
                    }

                    if (filterOut2.Signatures != null &&
                        Module.GetTargetAttributeSignatureIndex(customAttributeHandle, filterOut2) != -1)
                    {
                        // It is important to capture the last application of the attribute that we run into,
                        // it makes a difference for default and constant values.
                        filteredOutAttribute2 = customAttributeHandle;
                        continue;
                    }

                    if (customAttributesBuilder == null)
                    {
                        customAttributesBuilder = ArrayBuilder<CSharpAttributeData>.GetInstance();
                    }

                    customAttributesBuilder.Add(new PEAttributeData(this, customAttributeHandle));
                }
            }
            catch (BadImageFormatException)
            { }

            if (customAttributesBuilder != null)
            {
                return customAttributesBuilder.ToImmutableAndFree();
            }

            return ImmutableArray<CSharpAttributeData>.Empty;
        }

        internal ImmutableArray<CSharpAttributeData> GetCustomAttributesForToken(EntityHandle token)
        {
            // Do not filter anything and therefore ignore the out results
            CustomAttributeHandle ignore1;
            CustomAttributeHandle ignore2;
            return GetCustomAttributesForToken(token,
                out ignore1,
                default(AttributeDescription),
                out ignore2,
                default(AttributeDescription));
        }

        /// <summary>
        /// Get the custom attributes, but filter out any ParamArrayAttributes.
        /// </summary>
        /// <param name="token">The parameter token handle.</param>
        /// <param name="paramArrayAttribute">Set to a ParamArrayAttribute</param>
        /// CustomAttributeHandle if any are found. Nil token otherwise.
        internal ImmutableArray<CSharpAttributeData> GetCustomAttributesForToken(EntityHandle token,
            out CustomAttributeHandle paramArrayAttribute)
        {
            CustomAttributeHandle ignore;
            return GetCustomAttributesForToken(
                token,
                out paramArrayAttribute,
                AttributeDescription.ParamArrayAttribute,
                out ignore,
                default(AttributeDescription));
        }


        internal bool HasAnyCustomAttributes(EntityHandle token)
        {
            try
            {
                foreach (var attr in _module.GetCustomAttributesOrThrow(token))
                {
                    return true;
                }
            }
            catch (BadImageFormatException)
            { }

            return false;
        }

        internal TypeSymbol TryDecodeAttributeWithTypeArgument(EntityHandle handle, AttributeDescription attributeDescription)
        {
            string typeName;
            if (_module.HasStringValuedAttribute(handle, attributeDescription, out typeName))
            {
                return new MetadataDecoder(this).GetTypeSymbolForSerializedType(typeName);
            }

            return null;
        }

        /// <summary>
        /// Filters extension attributes from the attribute results.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="foundExtension">True if we found an extension method, false otherwise.</param>
        /// <returns>The attributes on the token, minus any ExtensionAttributes.</returns>
        internal ImmutableArray<CSharpAttributeData> GetCustomAttributesFilterExtensions(EntityHandle token, out bool foundExtension)
        {
            CustomAttributeHandle extensionAttribute;
            CustomAttributeHandle ignore;
            var result = GetCustomAttributesForToken(token,
                out extensionAttribute,
                AttributeDescription.CaseSensitiveExtensionAttribute,
                out ignore,
                default(AttributeDescription));

            foundExtension = !extensionAttribute.IsNil;
            return result;
        }

        internal void OnNewTypeDeclarationsLoaded(
            Dictionary<string, ImmutableArray<PENamedTypeSymbol>> typesDict)
        {
            bool keepLookingForDeclaredCorTypes = (_ordinal == 0 && _assemblySymbol.KeepLookingForDeclaredSpecialTypes);

            foreach (var types in typesDict.Values)
            {
                foreach (var type in types)
                {
                    bool added;
                    added = TypeHandleToTypeMap.TryAdd(type.Handle, type);
                    Debug.Assert(added);

                    // Register newly loaded COR types
                    if (keepLookingForDeclaredCorTypes && type.SpecialType != SpecialType.None)
                    {
                        _assemblySymbol.RegisterDeclaredSpecialType(type);
                        keepLookingForDeclaredCorTypes = _assemblySymbol.KeepLookingForDeclaredSpecialTypes;
                    }
                }
            }
        }

        internal override ICollection<string> TypeNames
        {
            get
            {
                if (_lazyTypeNames == null)
                {
                    Interlocked.CompareExchange(ref _lazyTypeNames, _module.TypeNames.AsCaseSensitiveCollection(), null);
                }

                return _lazyTypeNames;
            }
        }

        internal override ICollection<string> NamespaceNames
        {
            get
            {
                if (_lazyNamespaceNames == null)
                {
                    Interlocked.CompareExchange(ref _lazyNamespaceNames, _module.NamespaceNames.AsCaseSensitiveCollection(), null);
                }

                return _lazyNamespaceNames;
            }
        }

        internal override ImmutableArray<byte> GetHash(AssemblyHashAlgorithm algorithmId)
        {
            return _module.GetHash(algorithmId);
        }

        internal DocumentationProvider DocumentationProvider
        {
            get
            {
                var assembly = _assemblySymbol as PEAssemblySymbol;
                if ((object)assembly != null)
                {
                    return assembly.DocumentationProvider;
                }
                else
                {
                    return DocumentationProvider.Default;
                }
            }
        }


        internal NamedTypeSymbol EventRegistrationToken
        {
            get
            {
                if ((object)_lazyEventRegistrationTokenSymbol == null)
                {
                    Interlocked.CompareExchange(ref _lazyEventRegistrationTokenSymbol,
                                                GetTypeSymbolForWellKnownType(
                                                    WellKnownType.System_Runtime_InteropServices_WindowsRuntime_EventRegistrationToken
                                                    ),
                                                null);
                    Debug.Assert((object)_lazyEventRegistrationTokenSymbol != null);
                }
                return _lazyEventRegistrationTokenSymbol;
            }
        }


        internal NamedTypeSymbol EventRegistrationTokenTable_T
        {
            get
            {
                if ((object)_lazyEventRegistrationTokenTableSymbol == null)
                {
                    Interlocked.CompareExchange(ref _lazyEventRegistrationTokenTableSymbol,
                                                GetTypeSymbolForWellKnownType(
                                                    WellKnownType.System_Runtime_InteropServices_WindowsRuntime_EventRegistrationTokenTable_T
                                                    ),
                                                null);
                    Debug.Assert((object)_lazyEventRegistrationTokenTableSymbol != null);
                }
                return _lazyEventRegistrationTokenTableSymbol;
            }
        }

        internal NamedTypeSymbol SystemTypeSymbol
        {
            get
            {
                if ((object)_lazySystemTypeSymbol == null)
                {
                    Interlocked.CompareExchange(ref _lazySystemTypeSymbol,
                                                GetTypeSymbolForWellKnownType(WellKnownType.System_Type),
                                                null);
                    Debug.Assert((object)_lazySystemTypeSymbol != null);
                }
                return _lazySystemTypeSymbol;
            }
        }

        private NamedTypeSymbol GetTypeSymbolForWellKnownType(WellKnownType type)
        {
            MetadataTypeName emittedName = MetadataTypeName.FromFullName(type.GetMetadataName(), useCLSCompliantNameArityEncoding: true);
            // First, check this module
            NamedTypeSymbol currentModuleResult = this.LookupTopLevelMetadataType(ref emittedName);

            if (IsAcceptableSystemTypeSymbol(currentModuleResult))
            {
                // It doesn't matter if there's another of this type in a referenced assembly -
                // we prefer the one in the current module.
                return currentModuleResult;
            }

            // If we didn't find it in this module, check the referenced assemblies
            NamedTypeSymbol referencedAssemblyResult = null;
            foreach (AssemblySymbol assembly in this.GetReferencedAssemblySymbols())
            {
                NamedTypeSymbol currResult = assembly.LookupTopLevelMetadataType(ref emittedName, digThroughForwardedTypes: true);
                if (IsAcceptableSystemTypeSymbol(currResult))
                {
                    if ((object)referencedAssemblyResult == null)
                    {
                        referencedAssemblyResult = currResult;
                    }
                    else
                    {
                        // CONSIDER: setting result to null will result in a MissingMetadataTypeSymbol 
                        // being returned.  Do we want to differentiate between no result and ambiguous
                        // results?  There doesn't seem to be an existing error code for "duplicate well-
                        // known type".
                        if (referencedAssemblyResult != currResult)
                        {
                            referencedAssemblyResult = null;
                        }
                        break;
                    }
                }
            }

            if ((object)referencedAssemblyResult != null)
            {
                Debug.Assert(IsAcceptableSystemTypeSymbol(referencedAssemblyResult));
                return referencedAssemblyResult;
            }

            Debug.Assert((object)currentModuleResult != null);
            return currentModuleResult;
        }

        private static bool IsAcceptableSystemTypeSymbol(NamedTypeSymbol candidate)
        {
            return candidate.Kind != SymbolKind.ErrorType || !(candidate is MissingMetadataTypeSymbol);
        }

        internal override bool HasAssemblyCompilationRelaxationsAttribute
        {
            get
            {
                var assemblyAttributes = GetAssemblyAttributes();
                return assemblyAttributes.IndexOfAttribute(this, AttributeDescription.CompilationRelaxationsAttribute) >= 0;
            }
        }

        internal override bool HasAssemblyRuntimeCompatibilityAttribute
        {
            get
            {
                var assemblyAttributes = GetAssemblyAttributes();
                return assemblyAttributes.IndexOfAttribute(this, AttributeDescription.RuntimeCompatibilityAttribute) >= 0;
            }
        }

        internal override CharSet? DefaultMarshallingCharSet
        {
            get
            {
                // not used by the compiler
                throw ExceptionUtilities.Unreachable;
            }
        }

        internal sealed override CSharpCompilation DeclaringCompilation // perf, not correctness
        {
            get { return null; }
        }

        internal NamedTypeSymbol LookupTopLevelMetadataType(ref MetadataTypeName emittedName, out bool isNoPiaLocalType)
        {
            NamedTypeSymbol result;
            PENamespaceSymbol scope = (PENamespaceSymbol)this.GlobalNamespace.LookupNestedNamespace(emittedName.NamespaceSegments);

            if ((object)scope == null)
            {
                // We failed to locate the namespace
                isNoPiaLocalType = false;
                result = new MissingMetadataTypeSymbol.TopLevel(this, ref emittedName);
            }
            else
            {
                result = scope.LookupMetadataType(ref emittedName, out isNoPiaLocalType);
                Debug.Assert((object)result != null);
            }

            return result;
        }

        /// <summary>
        /// If this module forwards the given type to another assembly, return that assembly;
        /// otherwise, return null.
        /// </summary>
        /// <param name="fullName">Type to look up.</param>
        /// <returns>Assembly symbol or null.</returns>
        /// <remarks>
        /// The returned assembly may also forward the type.
        /// </remarks>
        internal AssemblySymbol GetAssemblyForForwardedType(ref MetadataTypeName fullName)
        {
            try
            {
                string matchedName;
                AssemblyReferenceHandle assemblyRef = Module.GetAssemblyForForwardedType(fullName.FullName, ignoreCase: false, matchedName: out matchedName);
                return assemblyRef.IsNil ? null : this.GetReferencedAssemblySymbols()[Module.GetAssemblyReferenceIndexOrThrow(assemblyRef)];
            }
            catch (BadImageFormatException)
            {
                return null;
            }
        }

        internal IEnumerable<NamedTypeSymbol> GetForwardedTypes()
        {
            foreach (KeyValuePair<string, AssemblyReferenceHandle> forwarder in Module.GetForwardedTypes())
            {
                var name = MetadataTypeName.FromFullName(forwarder.Key);
                AssemblySymbol assemblySymbol;

                try
                {
                    assemblySymbol = this.GetReferencedAssemblySymbols()[Module.GetAssemblyReferenceIndexOrThrow(forwarder.Value)];
                }
                catch (BadImageFormatException)
                {
                    continue;
                }

                yield return assemblySymbol.LookupTopLevelMetadataType(ref name, digThroughForwardedTypes: true);
            }
        }

        internal override bool UtilizesNullableReferenceTypes
        {
            get
            {
                return Module.UtilizesNullableReferenceTypes();
            }
        }

        internal override bool NullableOptOut
        {
            get
            {
                bool optOut;
                return _module.HasNullableOptOutAttribute(EntityHandle.ModuleDefinition, out optOut) ? optOut : base.NullableOptOut;
            }
        }
    }
}
