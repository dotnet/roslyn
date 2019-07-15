// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Emit
{
    internal abstract class PEAssemblyBuilderBase : PEModuleBuilder, Cci.IAssemblyReference
    {
        private readonly SourceAssemblySymbol _sourceAssembly;

        /// <summary>
        /// Additional types injected by the Expression Evaluator.
        /// </summary>
        private readonly ImmutableArray<NamedTypeSymbol> _additionalTypes;

        private ImmutableArray<Cci.IFileReference> _lazyFiles;

        /// <summary>This is a cache of a subset of <seealso cref="_lazyFiles"/>. We don't include manifest resources in ref assemblies</summary>
        private ImmutableArray<Cci.IFileReference> _lazyFilesWithoutManifestResources;

        private SynthesizedEmbeddedAttributeSymbol _lazyEmbeddedAttribute;
        private SynthesizedEmbeddedAttributeSymbol _lazyIsReadOnlyAttribute;
        private SynthesizedEmbeddedAttributeSymbol _lazyIsByRefLikeAttribute;
        private SynthesizedEmbeddedAttributeSymbol _lazyIsUnmanagedAttribute;
        private SynthesizedEmbeddedNullableAttributeSymbol _lazyNullableAttribute;
        private SynthesizedEmbeddedNullableContextAttributeSymbol _lazyNullableContextAttribute;
        private SynthesizedEmbeddedNullablePublicOnlyAttributeSymbol _lazyNullablePublicOnlyAttribute;

        /// <summary>
        /// The behavior of the C# command-line compiler is as follows:
        ///   1) If the /out switch is specified, then the explicit assembly name is used.
        ///   2) Otherwise,
        ///      a) if the assembly is executable, then the assembly name is derived from
        ///         the name of the file containing the entrypoint;
        ///      b) otherwise, the assembly name is derived from the name of the first input
        ///         file.
        /// 
        /// Since we don't know which method is the entrypoint until well after the
        /// SourceAssemblySymbol is created, in case 2a, its name will not reflect the
        /// name of the file containing the entrypoint.  We leave it to our caller to
        /// provide that name explicitly.
        /// </summary>
        /// <remarks>
        /// In cases 1 and 2b, we expect (metadataName == sourceAssembly.MetadataName).
        /// </remarks>
        private readonly string _metadataName;

        public PEAssemblyBuilderBase(
            SourceAssemblySymbol sourceAssembly,
            EmitOptions emitOptions,
            OutputKind outputKind,
            Cci.ModulePropertiesForSerialization serializationProperties,
            IEnumerable<ResourceDescription> manifestResources,
            ImmutableArray<NamedTypeSymbol> additionalTypes)
            : base((SourceModuleSymbol)sourceAssembly.Modules[0], emitOptions, outputKind, serializationProperties, manifestResources)
        {
            Debug.Assert(sourceAssembly is object);

            _sourceAssembly = sourceAssembly;
            _additionalTypes = additionalTypes.NullToEmpty();
            _metadataName = (emitOptions.OutputNameOverride == null) ? sourceAssembly.MetadataName : FileNameUtilities.ChangeExtension(emitOptions.OutputNameOverride, extension: null);

            AssemblyOrModuleSymbolToModuleRefMap.Add(sourceAssembly, this);
        }

        public sealed override ISourceAssemblySymbolInternal SourceAssemblyOpt
            => _sourceAssembly;

        public sealed override ImmutableArray<NamedTypeSymbol> GetAdditionalTopLevelTypes(DiagnosticBag diagnostics)
            => _additionalTypes;

        public sealed override ImmutableArray<NamedTypeSymbol> GetEmbeddedTypes(DiagnosticBag diagnostics)
        {
            var builder = ArrayBuilder<NamedTypeSymbol>.GetInstance();

            CreateEmbeddedAttributesIfNeeded(diagnostics);

            builder.AddIfNotNull(_lazyEmbeddedAttribute);
            builder.AddIfNotNull(_lazyIsReadOnlyAttribute);
            builder.AddIfNotNull(_lazyIsUnmanagedAttribute);
            builder.AddIfNotNull(_lazyIsByRefLikeAttribute);
            builder.AddIfNotNull(_lazyNullableAttribute);
            builder.AddIfNotNull(_lazyNullableContextAttribute);
            builder.AddIfNotNull(_lazyNullablePublicOnlyAttribute);

            return builder.ToImmutableAndFree();
        }

        public sealed override IEnumerable<Cci.IFileReference> GetFiles(EmitContext context)
        {
            if (!context.IsRefAssembly)
            {
                return getFiles(ref _lazyFiles);
            }
            return getFiles(ref _lazyFilesWithoutManifestResources);

            ImmutableArray<Cci.IFileReference> getFiles(ref ImmutableArray<Cci.IFileReference> lazyFiles)
            {
                if (lazyFiles.IsDefault)
                {
                    var builder = ArrayBuilder<Cci.IFileReference>.GetInstance();
                    try
                    {
                        var modules = _sourceAssembly.Modules;
                        for (int i = 1; i < modules.Length; i++)
                        {
                            builder.Add((Cci.IFileReference)Translate(modules[i], context.Diagnostics));
                        }

                        if (!context.IsRefAssembly)
                        {
                            // resources are not emitted into ref assemblies
                            foreach (ResourceDescription resource in ManifestResources)
                            {
                                if (!resource.IsEmbedded)
                                {
                                    builder.Add(resource);
                                }
                            }
                        }

                        // Dev12 compilers don't report ERR_CryptoHashFailed if there are no files to be hashed.
                        if (ImmutableInterlocked.InterlockedInitialize(ref lazyFiles, builder.ToImmutable()) && lazyFiles.Length > 0)
                        {
                            if (!CryptographicHashProvider.IsSupportedAlgorithm(_sourceAssembly.HashAlgorithm))
                            {
                                context.Diagnostics.Add(new CSDiagnostic(new CSDiagnosticInfo(ErrorCode.ERR_CryptoHashFailed), NoLocation.Singleton));
                            }
                        }
                    }
                    finally
                    {
                        builder.Free();
                    }
                }

                return lazyFiles;
            }
        }

        protected override void AddEmbeddedResourcesFromAddedModules(ArrayBuilder<Cci.ManagedResource> builder, DiagnosticBag diagnostics)
        {
            var modules = _sourceAssembly.Modules;
            int count = modules.Length;

            for (int i = 1; i < count; i++)
            {
                var file = (Cci.IFileReference)Translate(modules[i], diagnostics);

                try
                {
                    foreach (EmbeddedResource resource in ((Symbols.Metadata.PE.PEModuleSymbol)modules[i]).Module.GetEmbeddedResourcesOrThrow())
                    {
                        builder.Add(new Cci.ManagedResource(
                            resource.Name,
                            (resource.Attributes & ManifestResourceAttributes.Public) != 0,
                            null,
                            file,
                            resource.Offset));
                    }
                }
                catch (BadImageFormatException)
                {
                    diagnostics.Add(new CSDiagnosticInfo(ErrorCode.ERR_BindToBogus, modules[i]), NoLocation.Singleton);
                }
            }
        }

        public override string Name => _metadataName;
        public AssemblyIdentity Identity => _sourceAssembly.Identity;
        public Version AssemblyVersionPattern => _sourceAssembly.AssemblyVersionPattern;

        internal override SynthesizedAttributeData SynthesizeEmbeddedAttribute()
        {
            // _lazyEmbeddedAttribute should have been created before calling this method.
            return new SynthesizedAttributeData(
                _lazyEmbeddedAttribute.Constructors[0],
                ImmutableArray<TypedConstant>.Empty,
                ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty);
        }

        internal override SynthesizedAttributeData SynthesizeNullableAttribute(WellKnownMember member, ImmutableArray<TypedConstant> arguments)
        {
            if ((object)_lazyNullableAttribute != null)
            {
                var constructorIndex = (member == WellKnownMember.System_Runtime_CompilerServices_NullableAttribute__ctorTransformFlags) ? 1 : 0;
                return new SynthesizedAttributeData(
                    _lazyNullableAttribute.Constructors[constructorIndex],
                    arguments,
                    ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty);
            }

            return base.SynthesizeNullableAttribute(member, arguments);
        }

        internal override SynthesizedAttributeData SynthesizeNullableContextAttribute(ImmutableArray<TypedConstant> arguments)
        {
            if ((object)_lazyNullableContextAttribute != null)
            {
                return new SynthesizedAttributeData(
                    _lazyNullableContextAttribute.Constructors[0],
                    arguments,
                    ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty);
            }

            return base.SynthesizeNullableContextAttribute(arguments);
        }

        internal override SynthesizedAttributeData SynthesizeNullablePublicOnlyAttribute(ImmutableArray<TypedConstant> arguments)
        {
            if ((object)_lazyNullablePublicOnlyAttribute != null)
            {
                return new SynthesizedAttributeData(
                    _lazyNullablePublicOnlyAttribute.Constructors[0],
                    arguments,
                    ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty);
            }

            return base.SynthesizeNullablePublicOnlyAttribute(arguments);
        }

        protected override SynthesizedAttributeData TrySynthesizeIsReadOnlyAttribute()
        {
            if ((object)_lazyIsReadOnlyAttribute != null)
            {
                return new SynthesizedAttributeData(
                    _lazyIsReadOnlyAttribute.Constructors[0],
                    ImmutableArray<TypedConstant>.Empty,
                    ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty);
            }

            return base.TrySynthesizeIsReadOnlyAttribute();
        }

        protected override SynthesizedAttributeData TrySynthesizeIsUnmanagedAttribute()
        {
            if ((object)_lazyIsUnmanagedAttribute != null)
            {
                return new SynthesizedAttributeData(
                    _lazyIsUnmanagedAttribute.Constructors[0],
                    ImmutableArray<TypedConstant>.Empty,
                    ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty);
            }

            return base.TrySynthesizeIsUnmanagedAttribute();
        }

        protected override SynthesizedAttributeData TrySynthesizeIsByRefLikeAttribute()
        {
            if ((object)_lazyIsByRefLikeAttribute != null)
            {
                return new SynthesizedAttributeData(
                    _lazyIsByRefLikeAttribute.Constructors[0],
                    ImmutableArray<TypedConstant>.Empty,
                    ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty);
            }

            return base.TrySynthesizeIsByRefLikeAttribute();
        }

        private void CreateEmbeddedAttributesIfNeeded(DiagnosticBag diagnostics)
        {
            EmbeddableAttributes needsAttributes = GetNeedsGeneratedAttributes();

            if (ShouldEmitNullablePublicOnlyAttribute() &&
                Compilation.CheckIfAttributeShouldBeEmbedded(EmbeddableAttributes.NullablePublicOnlyAttribute, diagnostics, Location.None))
            {
                needsAttributes |= EmbeddableAttributes.NullablePublicOnlyAttribute;
            }
            else if (needsAttributes == 0)
            {
                return;
            }

            var createParameterlessEmbeddedAttributeSymbol = new Func<string, NamespaceSymbol, DiagnosticBag, SynthesizedEmbeddedAttributeSymbol>(CreateParameterlessEmbeddedAttributeSymbol);

            CreateAttributeIfNeeded(
                ref _lazyEmbeddedAttribute,
                diagnostics,
                AttributeDescription.CodeAnalysisEmbeddedAttribute,
                createParameterlessEmbeddedAttributeSymbol);

            if ((needsAttributes & EmbeddableAttributes.IsReadOnlyAttribute) != 0)
            {
                CreateAttributeIfNeeded(
                    ref _lazyIsReadOnlyAttribute,
                    diagnostics,
                    AttributeDescription.IsReadOnlyAttribute,
                    createParameterlessEmbeddedAttributeSymbol);
            }

            if ((needsAttributes & EmbeddableAttributes.IsByRefLikeAttribute) != 0)
            {
                CreateAttributeIfNeeded(
                    ref _lazyIsByRefLikeAttribute,
                    diagnostics,
                    AttributeDescription.IsByRefLikeAttribute,
                    createParameterlessEmbeddedAttributeSymbol);
            }

            if ((needsAttributes & EmbeddableAttributes.IsUnmanagedAttribute) != 0)
            {
                CreateAttributeIfNeeded(
                    ref _lazyIsUnmanagedAttribute,
                    diagnostics,
                    AttributeDescription.IsUnmanagedAttribute,
                    createParameterlessEmbeddedAttributeSymbol);
            }

            if ((needsAttributes & EmbeddableAttributes.NullableAttribute) != 0)
            {
                CreateAttributeIfNeeded(
                    ref _lazyNullableAttribute,
                    diagnostics,
                    AttributeDescription.NullableAttribute,
                    CreateNullableAttributeSymbol);
            }

            if ((needsAttributes & EmbeddableAttributes.NullableContextAttribute) != 0)
            {
                CreateAttributeIfNeeded(
                    ref _lazyNullableContextAttribute,
                    diagnostics,
                    AttributeDescription.NullableContextAttribute,
                    CreateNullableContextAttributeSymbol);
            }

            if ((needsAttributes & EmbeddableAttributes.NullablePublicOnlyAttribute) != 0)
            {
                CreateAttributeIfNeeded(
                    ref _lazyNullablePublicOnlyAttribute,
                    diagnostics,
                    AttributeDescription.NullablePublicOnlyAttribute,
                    CreateNullablePublicOnlyAttributeSymbol);
            }
        }

        private SynthesizedEmbeddedAttributeSymbol CreateParameterlessEmbeddedAttributeSymbol(string name, NamespaceSymbol containingNamespace, DiagnosticBag diagnostics)
            => new SynthesizedEmbeddedAttributeSymbol(
                    name,
                    containingNamespace,
                    SourceModule,
                    baseType: GetWellKnownType(WellKnownType.System_Attribute, diagnostics));

        private SynthesizedEmbeddedNullableAttributeSymbol CreateNullableAttributeSymbol(string name, NamespaceSymbol containingNamespace, DiagnosticBag diagnostics)
            => new SynthesizedEmbeddedNullableAttributeSymbol(
                    name,
                    containingNamespace,
                    SourceModule,
                    GetWellKnownType(WellKnownType.System_Attribute, diagnostics),
                    GetSpecialType(SpecialType.System_Byte, diagnostics));

        private SynthesizedEmbeddedNullableContextAttributeSymbol CreateNullableContextAttributeSymbol(string name, NamespaceSymbol containingNamespace, DiagnosticBag diagnostics)
            => new SynthesizedEmbeddedNullableContextAttributeSymbol(
                    name,
                    containingNamespace,
                    SourceModule,
                    GetWellKnownType(WellKnownType.System_Attribute, diagnostics),
                    GetSpecialType(SpecialType.System_Byte, diagnostics));

        private SynthesizedEmbeddedNullablePublicOnlyAttributeSymbol CreateNullablePublicOnlyAttributeSymbol(string name, NamespaceSymbol containingNamespace, DiagnosticBag diagnostics)
            => new SynthesizedEmbeddedNullablePublicOnlyAttributeSymbol(
                    name,
                    containingNamespace,
                    SourceModule,
                    GetWellKnownType(WellKnownType.System_Attribute, diagnostics),
                    GetSpecialType(SpecialType.System_Boolean, diagnostics));

        private void CreateAttributeIfNeeded<T>(
            ref T symbol,
            DiagnosticBag diagnostics,
            AttributeDescription description,
            Func<string, NamespaceSymbol, DiagnosticBag, T> factory)
            where T : SynthesizedEmbeddedAttributeSymbolBase
        {
            if (symbol is null)
            {
                AddDiagnosticsForExistingAttribute(description, diagnostics);

                var containingNamespace = GetOrSynthesizeNamespace(description.Namespace);

                symbol = factory(description.Name, containingNamespace, diagnostics);
                Debug.Assert(symbol.Constructors.Length == description.Signatures.Length);

                if (symbol.GetAttributeUsageInfo() != AttributeUsageInfo.Default)
                {
                    EnsureAttributeUsageAttributeMembersAvailable(diagnostics);
                }

                AddSynthesizedDefinition(containingNamespace, symbol);
            }
        }

        private void AddDiagnosticsForExistingAttribute(AttributeDescription description, DiagnosticBag diagnostics)
        {
            var attributeMetadataName = MetadataTypeName.FromFullName(description.FullName);
            var userDefinedAttribute = _sourceAssembly.SourceModule.LookupTopLevelMetadataType(ref attributeMetadataName);
            Debug.Assert((object)userDefinedAttribute.ContainingModule == _sourceAssembly.SourceModule);

            if (!(userDefinedAttribute is MissingMetadataTypeSymbol))
            {
                diagnostics.Add(ErrorCode.ERR_TypeReserved, userDefinedAttribute.Locations[0], description.FullName);
            }
        }

        private NamespaceSymbol GetOrSynthesizeNamespace(string namespaceFullName)
        {
            var result = SourceModule.GlobalNamespace;

            foreach (var partName in namespaceFullName.Split('.'))
            {
                var subnamespace = (NamespaceSymbol)result.GetMembers(partName).FirstOrDefault(m => m.Kind == SymbolKind.Namespace);
                if (subnamespace == null)
                {
                    subnamespace = new SynthesizedNamespaceSymbol(result, partName);
                    AddSynthesizedDefinition(result, subnamespace);
                }

                result = subnamespace;
            }

            return result;
        }

        private NamedTypeSymbol GetWellKnownType(WellKnownType type, DiagnosticBag diagnostics)
        {
            var result = _sourceAssembly.DeclaringCompilation.GetWellKnownType(type);
            Binder.ReportUseSiteDiagnostics(result, diagnostics, Location.None);
            return result;
        }

        private NamedTypeSymbol GetSpecialType(SpecialType type, DiagnosticBag diagnostics)
        {
            var result = _sourceAssembly.DeclaringCompilation.GetSpecialType(type);
            Binder.ReportUseSiteDiagnostics(result, diagnostics, Location.None);
            return result;
        }

        private void EnsureAttributeUsageAttributeMembersAvailable(DiagnosticBag diagnostics)
        {
            var compilation = _sourceAssembly.DeclaringCompilation;
            Binder.GetWellKnownTypeMember(compilation, WellKnownMember.System_AttributeUsageAttribute__ctor, diagnostics, Location.None);
            Binder.GetWellKnownTypeMember(compilation, WellKnownMember.System_AttributeUsageAttribute__AllowMultiple, diagnostics, Location.None);
            Binder.GetWellKnownTypeMember(compilation, WellKnownMember.System_AttributeUsageAttribute__Inherited, diagnostics, Location.None);
        }
    }

    internal sealed class PEAssemblyBuilder : PEAssemblyBuilderBase
    {
        public PEAssemblyBuilder(
            SourceAssemblySymbol sourceAssembly,
            EmitOptions emitOptions,
            OutputKind outputKind,
            Cci.ModulePropertiesForSerialization serializationProperties,
            IEnumerable<ResourceDescription> manifestResources)
            : base(sourceAssembly, emitOptions, outputKind, serializationProperties, manifestResources, ImmutableArray<NamedTypeSymbol>.Empty)
        {
        }

        public override int CurrentGenerationOrdinal => 0;
    }
}
