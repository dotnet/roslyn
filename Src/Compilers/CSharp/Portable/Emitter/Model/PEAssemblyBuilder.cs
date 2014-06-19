// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly SourceAssemblySymbol sourceAssembly;
        private readonly ImmutableArray<NamedTypeSymbol> additionalTypes;
        private ImmutableArray<Cci.IFileReference> lazyFiles;

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
        private readonly string metadataName;

        public PEAssemblyBuilderBase(
            SourceAssemblySymbol sourceAssembly,
            string outputName,
            OutputKind outputKind,
            ModulePropertiesForSerialization serializationProperties,
            IEnumerable<ResourceDescription> manifestResources,
            Func<AssemblySymbol, AssemblyIdentity> assemblySymbolMapper,
            ImmutableArray<NamedTypeSymbol> additionalTypes,
            bool metadataOnly)
            : base((SourceModuleSymbol)sourceAssembly.Modules[0], outputName, outputKind, serializationProperties, manifestResources, assemblySymbolMapper, metadataOnly)
        {
            Debug.Assert((object)sourceAssembly != null);

            this.sourceAssembly = sourceAssembly;
            this.additionalTypes = additionalTypes.NullToEmpty();
            this.metadataName = outputName == null ? sourceAssembly.MetadataName : PathUtilities.RemoveExtension(outputName);

            AssemblyOrModuleSymbolToModuleRefMap.Add(sourceAssembly, this);
        }

        public override void Dispatch(Cci.MetadataVisitor visitor)
        {
            visitor.Visit((Cci.IAssembly)this);
        }


        internal override ImmutableArray<NamedTypeSymbol> GetAdditionalTopLevelTypes()
        {
            return this.additionalTypes;
        }

        IEnumerable<Cci.IFileReference> Cci.IAssembly.GetFiles(EmitContext context)
        {
            if (lazyFiles.IsDefault)
            {
                var builder = ArrayBuilder<Cci.IFileReference>.GetInstance();
                try
                {
                    var modules = sourceAssembly.Modules;
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
                    if (ImmutableInterlocked.InterlockedInitialize(ref lazyFiles, builder.ToImmutable()) && lazyFiles.Length > 0)
                    {
                        if (!CryptographicHashProvider.IsSupportedAlgorithm(sourceAssembly.AssemblyHashAlgorithm))
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

        uint Cci.IAssembly.Flags
        {
            get
            {
                AssemblyNameFlags result = sourceAssembly.Flags & ~AssemblyNameFlags.PublicKey;

                if (!sourceAssembly.PublicKey.IsDefaultOrEmpty)
                    result |= AssemblyNameFlags.PublicKey;

                return (uint)result;
            }
        }

        string Cci.IAssembly.SignatureKey
        {
            get
            {
                return sourceAssembly.SignatureKey;
            }
        }

        IEnumerable<byte> Cci.IAssembly.PublicKey
        {
            get
            {
                return sourceAssembly.Identity.PublicKey;
            }
        }

        protected override void AddEmbeddedResourcesFromAddedModules(ArrayBuilder<Cci.ManagedResource> builder, DiagnosticBag diagnostics)
        {
            var modules = sourceAssembly.Modules;
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

        string Cci.IAssemblyReference.Culture
        {
            get
            {
                return sourceAssembly.Identity.CultureName;
            }
        }

        bool Cci.IAssemblyReference.IsRetargetable
        {
            get
            {
                return sourceAssembly.Identity.IsRetargetable;
            }
        }

        AssemblyContentType Cci.IAssemblyReference.ContentType
        {
            get
            {
                return sourceAssembly.Identity.ContentType;
            }
        }

        IEnumerable<byte> Cci.IAssemblyReference.PublicKeyToken
        {
            get { return sourceAssembly.Identity.PublicKeyToken; }
        }

        Version Cci.IAssemblyReference.Version
        {
            get { return sourceAssembly.Identity.Version; }
        }

        internal override string Name
        {
            get { return metadataName; }
        }

        AssemblyHashAlgorithm Cci.IAssembly.HashAlgorithm
        {
            get
            {
                return sourceAssembly.AssemblyHashAlgorithm;
            }
        }
    }

    internal sealed class PEAssemblyBuilder : PEAssemblyBuilderBase
    {
        public PEAssemblyBuilder(
            SourceAssemblySymbol sourceAssembly,
            string outputName,
            OutputKind outputKind,
            ModulePropertiesForSerialization serializationProperties,
            IEnumerable<ResourceDescription> manifestResources,
            Func<AssemblySymbol, AssemblyIdentity> assemblySymbolMapper = null,
            ImmutableArray<NamedTypeSymbol> additionalTypes = default(ImmutableArray<NamedTypeSymbol>),
            bool metadataOnly = false)
            : base(sourceAssembly, outputName, outputKind, serializationProperties, manifestResources, assemblySymbolMapper, additionalTypes, metadataOnly)
        {
        }
    }
}
