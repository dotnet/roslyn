// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Emit
{
    internal abstract class PEAssemblyBuilderBase : PEModuleBuilder, Cci.IAssemblyReference
    {
        private readonly SourceAssemblySymbol _sourceAssembly;
        private readonly ImmutableArray<NamedTypeSymbol> _additionalTypes;
        private ImmutableDictionary<WellKnownMember, MethodSymbol> _embeddedAttributesConstructors;
        private ImmutableArray<Cci.IFileReference> _lazyFiles;

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
            Debug.Assert((object)sourceAssembly != null);

            _sourceAssembly = sourceAssembly;
            _additionalTypes = additionalTypes.NullToEmpty();
            _metadataName = (emitOptions.OutputNameOverride == null) ? sourceAssembly.MetadataName : FileNameUtilities.ChangeExtension(emitOptions.OutputNameOverride, extension: null);

            AssemblyOrModuleSymbolToModuleRefMap.Add(sourceAssembly, this);
        }

        public override ISourceAssemblySymbolInternal SourceAssemblyOpt => _sourceAssembly;

        internal override ImmutableArray<NamedTypeSymbol> GetAdditionalTopLevelTypes(DiagnosticBag diagnostics)
        {
            Interlocked.CompareExchange(ref _embeddedAttributesConstructors, CreateEmbeddedAttributesIfNeeded(diagnostics), null);

            if (_embeddedAttributesConstructors.Count == 0)
            {
                return _additionalTypes;
            }

            var builder = ArrayBuilder<NamedTypeSymbol>.GetInstance();
            builder.AddRange(_additionalTypes);

            foreach (var constructor in _embeddedAttributesConstructors.Values)
            {
                builder.Add(constructor.ContainingType);
            }

            return builder.ToImmutableAndFree();
        }

        public sealed override IEnumerable<Cci.IFileReference> GetFiles(EmitContext context)
        {
            if (_lazyFiles.IsDefault)
            {
                var builder = ArrayBuilder<Cci.IFileReference>.GetInstance();
                try
                {
                    var modules = _sourceAssembly.Modules;
                    for (int i = 1; i < modules.Length; i++)
                    {
                        builder.Add((Cci.IFileReference)Translate(modules[i], context.Diagnostics));
                    }

                    foreach (ResourceDescription resource in ManifestResources)
                    {
                        if (!resource.IsEmbedded)
                        {
                            builder.Add(resource);
                        }
                    }

                    // Dev12 compilers don't report ERR_CryptoHashFailed if there are no files to be hashed.
                    if (ImmutableInterlocked.InterlockedInitialize(ref _lazyFiles, builder.ToImmutable()) && _lazyFiles.Length > 0)
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

            return _lazyFiles;
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
            if (_embeddedAttributesConstructors.TryGetValue(WellKnownMember.Microsoft_CodeAnalysis_EmbeddedAttribute__ctor, out var constructor))
            {
                return new SynthesizedAttributeData(constructor, ImmutableArray<TypedConstant>.Empty, ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty);
            }

            return base.SynthesizeEmbeddedAttribute();
        }

        internal override SynthesizedAttributeData SynthesizeIsReadOnlyAttribute()
        {
            if (_embeddedAttributesConstructors.TryGetValue(WellKnownMember.System_Runtime_CompilerServices_IsReadOnlyAttribute__ctor, out var constructor))
            {
                return new SynthesizedAttributeData(constructor, ImmutableArray<TypedConstant>.Empty, ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty);
            }

            return base.SynthesizeIsReadOnlyAttribute();
        }

        private ImmutableDictionary<WellKnownMember, MethodSymbol> CreateEmbeddedAttributesIfNeeded(DiagnosticBag diagnostics)
        {
            var builder = ArrayBuilder<KeyValuePair<WellKnownMember, MethodSymbol>>.GetInstance();

            if (_sourceAssembly.DeclaringCompilation.NeedsGeneratedIsReadOnlyAttribute)
            {
                CreateEmbeddedAttributeIfNeeded(
                    builder,
                    diagnostics,
                    WellKnownType.Microsoft_CodeAnalysis_EmbeddedAttribute,
                    WellKnownMember.Microsoft_CodeAnalysis_EmbeddedAttribute__ctor,
                    allowUserDefined: false);

                CreateEmbeddedAttributeIfNeeded(
                    builder,
                    diagnostics,
                    WellKnownType.System_Runtime_CompilerServices_IsReadOnlyAttribute,
                    WellKnownMember.System_Runtime_CompilerServices_IsReadOnlyAttribute__ctor,
                    allowUserDefined: true);
            }

            return ImmutableDictionary.CreateRange(builder.ToArrayAndFree());
        }

        private void CreateEmbeddedAttributeIfNeeded(ArrayBuilder<KeyValuePair<WellKnownMember, MethodSymbol>> builder, DiagnosticBag diagnostics, WellKnownType type, WellKnownMember constructor, bool allowUserDefined)
        {
            var userDefinedAttribute = _sourceAssembly.DeclaringCompilation.GetWellKnownType(type);
            if (userDefinedAttribute == null ||
                userDefinedAttribute is MissingMetadataTypeSymbol ||
                !userDefinedAttribute.ContainingAssembly.Equals(_sourceAssembly))
            {
                var attributeSymbol = new SourceEmbeddedAttributeSymbol(type, _sourceAssembly.DeclaringCompilation);
                builder.Add(new KeyValuePair<WellKnownMember, MethodSymbol>(constructor, attributeSymbol.Constructor));
            }
            else if (!allowUserDefined)
            {
                diagnostics.Add(ErrorCode.ERR_TypeReserved, userDefinedAttribute.Locations[0], userDefinedAttribute);
            }
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
