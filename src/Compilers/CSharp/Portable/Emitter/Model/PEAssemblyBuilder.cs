// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Emit
{
    internal abstract class PEAssemblyBuilderBase : PEModuleBuilder, Cci.IAssembly
    {
        private readonly SourceAssemblySymbol _sourceAssembly;
        private readonly ImmutableArray<NamedTypeSymbol> _additionalTypes;
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

        public override void Dispatch(Cci.MetadataVisitor visitor)
        {
            visitor.Visit((Cci.IAssembly)this);
        }


        internal override ImmutableArray<NamedTypeSymbol> GetAdditionalTopLevelTypes()
        {
            return _additionalTypes;
        }

        IEnumerable<Cci.IFileReference> Cci.IAssembly.GetFiles(EmitContext context)
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
                        if (!CryptographicHashProvider.IsSupportedAlgorithm(_sourceAssembly.AssemblyHashAlgorithm))
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

        uint Cci.IAssembly.Flags
        {
            get
            {
                AssemblyNameFlags result = _sourceAssembly.Flags & ~AssemblyNameFlags.PublicKey;

                if (!_sourceAssembly.PublicKey.IsDefaultOrEmpty)
                    result |= AssemblyNameFlags.PublicKey;

                return (uint)result;
            }
        }

        string Cci.IAssembly.SignatureKey
        {
            get
            {
                return _sourceAssembly.SignatureKey;
            }
        }

        ImmutableArray<byte> Cci.IAssembly.PublicKey
        {
            get
            {
                return _sourceAssembly.Identity.PublicKey;
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

        AssemblyIdentity Cci.IAssemblyReference.Identity => _sourceAssembly.Identity;
        Version Cci.IAssemblyReference.AssemblyVersionPattern => _sourceAssembly.AssemblyVersionPattern;

        internal override string Name
        {
            get { return _metadataName; }
        }

        AssemblyHashAlgorithm Cci.IAssembly.HashAlgorithm
        {
            get
            {
                return _sourceAssembly.AssemblyHashAlgorithm;
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
