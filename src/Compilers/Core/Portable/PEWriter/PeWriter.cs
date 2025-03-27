// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DiaSymReader;
using static Microsoft.CodeAnalysis.SigningUtilities;
using EmitContext = Microsoft.CodeAnalysis.Emit.EmitContext;

namespace Microsoft.Cci
{
    internal sealed class PeWritingException : Exception
    {
        public PeWritingException(Exception inner)
            : base(inner.Message, inner)
        { }
    }

    internal static class PeWriter
    {
        internal struct EmitBuilders
        {
            internal BlobBuilder IlBlobBuilder;
            internal PooledBlobBuilder? MappedFieldDataBlobBuilder;
            internal PooledBlobBuilder? ManagedResourceBlobBuilder;
            internal PooledBlobBuilder? PortableExecutableBlobBuilder;
            internal PooledBlobBuilder? PortablePdbBlobBuilder;

            public EmitBuilders()
            {
                IlBlobBuilder = new BlobBuilder(32 * 1024);
                MappedFieldDataBlobBuilder = null;
                ManagedResourceBlobBuilder = null;
                PortableExecutableBlobBuilder = null;
                PortablePdbBlobBuilder = null;
            }

            internal void Free()
            {
                // There is a bug in LinkSuffix / LinkPrefix which causes the ownership to not
                // transfer when these have Count of 0. To avoid this problem we should not be
                // creating these builders unless we will actually put content into them.
                //
                // https://github.com/dotnet/runtime/issues/99266
                Debug.Assert(ManagedResourceBlobBuilder == null || ManagedResourceBlobBuilder.Count > 0);
                Debug.Assert(MappedFieldDataBlobBuilder == null || MappedFieldDataBlobBuilder.Count > 0);

                if (PortableExecutableBlobBuilder is null)
                {
                    MappedFieldDataBlobBuilder?.Free();
                    ManagedResourceBlobBuilder?.Free();
                }
                else
                {
                    // Once PortableExecutableBuilder is created it becomes the owner of the 
                    // MappedFieldDataBuilder and ManagedResourceBuilder instances. Freeing 
                    // it is sufficient to free both of them.
                    PortableExecutableBlobBuilder.Free();
                }

                PortablePdbBlobBuilder?.Free();
            }
        }

        internal static bool WritePeToStream(
            EmitContext context,
            CommonMessageProvider messageProvider,
            Func<Stream?> getPeStream,
            Func<Stream?>? getPortablePdbStreamOpt,
            PdbWriter? nativePdbWriterOpt,
            string? pdbPathOpt,
            bool metadataOnly,
            bool isDeterministic,
            bool emitTestCoverageData,
            RSAParameters? privateKeyOpt,
            CancellationToken cancellationToken)
        {
            // If PDB writer is given, we have to have PDB path.
            Debug.Assert(nativePdbWriterOpt == null || pdbPathOpt != null);

            var mdWriter = FullMetadataWriter.Create(context, messageProvider, metadataOnly, isDeterministic,
                emitTestCoverageData, getPortablePdbStreamOpt != null, cancellationToken);

            var properties = context.Module.SerializationProperties;

            context.Module.TestData?.SetMetadataWriter(mdWriter);
            nativePdbWriterOpt?.SetMetadataEmitter(mdWriter);

            // Since we are producing a full assembly, we should not have a module version ID
            // imposed ahead-of time. Instead we will compute a deterministic module version ID
            // based on the contents of the generated stream.
            Debug.Assert(properties.PersistentIdentifier == default(Guid));

            var emitBuilders = new EmitBuilders();
            Blob mvidFixup, mvidStringFixup;
            mdWriter.BuildMetadataAndIL(
                nativePdbWriterOpt,
                emitBuilders.IlBlobBuilder,
                out emitBuilders.MappedFieldDataBlobBuilder,
                out emitBuilders.ManagedResourceBlobBuilder,
                out mvidFixup,
                out mvidStringFixup);

            MethodDefinitionHandle entryPointHandle;
            MethodDefinitionHandle debugEntryPointHandle;
            mdWriter.GetEntryPoints(out entryPointHandle, out debugEntryPointHandle);

            if (!debugEntryPointHandle.IsNil)
            {
                nativePdbWriterOpt?.SetEntryPoint(MetadataTokens.GetToken(debugEntryPointHandle));
            }

            if (nativePdbWriterOpt != null)
            {
                if (context.Module.SourceLinkStreamOpt != null)
                {
                    nativePdbWriterOpt.EmbedSourceLink(context.Module.SourceLinkStreamOpt);
                }

                if (mdWriter.Module.OutputKind == OutputKind.WindowsRuntimeMetadata)
                {
                    // Dev12: If compiling to winmdobj, we need to add to PDB source spans of
                    //        all types and members for better error reporting by WinMDExp.
                    nativePdbWriterOpt.WriteDefinitionLocations(mdWriter.Module.GetSymbolToLocationMap());
                }
                else
                {
#if DEBUG
                    // validate that all definitions are writable
                    // if same scenario would happen in a winmdobj project
                    nativePdbWriterOpt.AssertAllDefinitionsHaveTokens(mdWriter.Module.GetSymbolToLocationMap());
#endif
                }

                nativePdbWriterOpt.WriteRemainingDebugDocuments(mdWriter.Module.DebugDocumentsBuilder.DebugDocuments);

                nativePdbWriterOpt.WriteCompilerVersion(context.Module.CommonCompilation.Language);
            }

            Stream? peStream = getPeStream();
            if (peStream == null)
            {
                emitBuilders.Free();
                return false;
            }

            BlobContentId pdbContentId = nativePdbWriterOpt?.GetContentId() ?? default;

            // the writer shall not be used after this point for writing:
            nativePdbWriterOpt = null;

            ushort portablePdbVersion = 0;
            var metadataRootBuilder = mdWriter.GetRootBuilder();

            var peHeaderBuilder = new PEHeaderBuilder(
                machine: properties.Machine,
                sectionAlignment: properties.SectionAlignment,
                fileAlignment: properties.FileAlignment,
                imageBase: properties.BaseAddress,
                majorLinkerVersion: properties.LinkerMajorVersion,
                minorLinkerVersion: properties.LinkerMinorVersion,
                majorOperatingSystemVersion: 4,
                minorOperatingSystemVersion: 0,
                majorImageVersion: 0,
                minorImageVersion: 0,
                majorSubsystemVersion: properties.MajorSubsystemVersion,
                minorSubsystemVersion: properties.MinorSubsystemVersion,
                subsystem: properties.Subsystem,
                dllCharacteristics: properties.DllCharacteristics,
                imageCharacteristics: properties.ImageCharacteristics,
                sizeOfStackReserve: properties.SizeOfStackReserve,
                sizeOfStackCommit: properties.SizeOfStackCommit,
                sizeOfHeapReserve: properties.SizeOfHeapReserve,
                sizeOfHeapCommit: properties.SizeOfHeapCommit);

            var peIdProvider = isDeterministic ?
                new Func<IEnumerable<Blob>, BlobContentId>(content => BlobContentId.FromHash(CryptographicHashProvider.ComputeSourceHash(content))) :
                null;

            // We need to calculate the PDB checksum, so we may as well use the calculated hash for PDB ID regardless of whether deterministic build is requested.
            var portablePdbContentHash = default(ImmutableArray<byte>);

            PooledBlobBuilder? portablePdbToEmbed = null;
            if (mdWriter.EmitPortableDebugMetadata)
            {
                mdWriter.AddRemainingDebugDocuments(mdWriter.Module.DebugDocumentsBuilder.DebugDocuments);

                // The algorithm must be specified for deterministic builds (checked earlier).
                Debug.Assert(!isDeterministic || context.Module.PdbChecksumAlgorithm.Name != null);

                var portablePdbIdProvider = (context.Module.PdbChecksumAlgorithm.Name != null) ?
                    new Func<IEnumerable<Blob>, BlobContentId>(content => BlobContentId.FromHash(portablePdbContentHash = CryptographicHashProvider.ComputeHash(context.Module.PdbChecksumAlgorithm, content))) :
                    null;

                emitBuilders.PortablePdbBlobBuilder = PooledBlobBuilder.GetInstance(zero: true);
                var portablePdbBuilder = mdWriter.GetPortablePdbBuilder(metadataRootBuilder.Sizes.RowCounts, debugEntryPointHandle, portablePdbIdProvider);
                pdbContentId = portablePdbBuilder.Serialize(emitBuilders.PortablePdbBlobBuilder);
                portablePdbVersion = portablePdbBuilder.FormatVersion;

                if (getPortablePdbStreamOpt == null)
                {
                    // embed to debug directory:
                    portablePdbToEmbed = emitBuilders.PortablePdbBlobBuilder;
                }
                else
                {
                    // write to Portable PDB stream:
                    Stream? portablePdbStream = getPortablePdbStreamOpt();
                    if (portablePdbStream != null)
                    {
                        try
                        {
                            emitBuilders.PortablePdbBlobBuilder.WriteContentTo(portablePdbStream);
                        }
                        catch (Exception e) when (!(e is OperationCanceledException))
                        {
                            throw new SymUnmanagedWriterException(e.Message, e);
                        }
                    }
                }
            }

            DebugDirectoryBuilder? debugDirectoryBuilder;
            if (pdbPathOpt != null || isDeterministic || portablePdbToEmbed != null)
            {
                debugDirectoryBuilder = new DebugDirectoryBuilder();
                if (pdbPathOpt != null)
                {
                    string paddedPath = isDeterministic ? pdbPathOpt : PadPdbPath(pdbPathOpt);
                    debugDirectoryBuilder.AddCodeViewEntry(paddedPath, pdbContentId, portablePdbVersion);

                    if (!portablePdbContentHash.IsDefault)
                    {
                        // Emit PDB Checksum entry for Portable and Embedded PDBs. The checksum is not as useful when the PDB is embedded, 
                        // however it allows the client to efficiently validate a standalone Portable PDB that 
                        // has been extracted from Embedded PDB and placed next to the PE file.
                        debugDirectoryBuilder.AddPdbChecksumEntry(context.Module.PdbChecksumAlgorithm.Name!, portablePdbContentHash);
                    }
                }

                if (isDeterministic)
                {
                    debugDirectoryBuilder.AddReproducibleEntry();
                }

                if (portablePdbToEmbed != null)
                {
                    debugDirectoryBuilder.AddEmbeddedPortablePdbEntry(portablePdbToEmbed, portablePdbVersion);
                }
            }
            else
            {
                debugDirectoryBuilder = null;
            }

            var strongNameProvider = context.Module.CommonCompilation.Options.StrongNameProvider;
            var corFlags = properties.CorFlags;

            var peBuilder = new ExtendedPEBuilder(
                peHeaderBuilder,
                metadataRootBuilder,
                emitBuilders.IlBlobBuilder,
                emitBuilders.MappedFieldDataBlobBuilder,
                emitBuilders.ManagedResourceBlobBuilder,
                CreateNativeResourceSectionSerializer(context.Module),
                debugDirectoryBuilder,
                CalculateStrongNameSignatureSize(context.Module, privateKeyOpt),
                entryPointHandle,
                corFlags,
                peIdProvider,
                metadataOnly && !context.IncludePrivateMembers);

            // This needs to force the backing builder to zero due to the issue writing COFF
            // headers. Can remove once this issue is fixed and we've moved to SRM with the 
            // fix
            // https://github.com/dotnet/runtime/issues/99244
            emitBuilders.PortableExecutableBlobBuilder = PooledBlobBuilder.GetInstance(zero: true);
            var peContentId = peBuilder.Serialize(emitBuilders.PortableExecutableBlobBuilder, out Blob mvidSectionFixup);

            PatchModuleVersionIds(mvidFixup, mvidSectionFixup, mvidStringFixup, peContentId.Guid);

            if (privateKeyOpt != null && corFlags.HasFlag(CorFlags.StrongNameSigned))
            {
                Debug.Assert(strongNameProvider != null);
                strongNameProvider.SignBuilder(peBuilder, emitBuilders.PortableExecutableBlobBuilder, privateKeyOpt.Value);
            }

            try
            {
                emitBuilders.PortableExecutableBlobBuilder.WriteContentTo(peStream);
            }
            catch (Exception e) when (!(e is OperationCanceledException))
            {
                throw new PeWritingException(e);
            }

            emitBuilders.Free();
            return true;
        }

        private static MethodInfo? s_calculateChecksumMethod;

        // internal for testing
        internal static uint CalculateChecksum(BlobBuilder peBlob, Blob checksumBlob)
        {
            if (s_calculateChecksumMethod == null)
            {
                s_calculateChecksumMethod = typeof(PEBuilder).GetRuntimeMethods()
                    .Where(m => m.Name == "CalculateChecksum" && m.GetParameters().Length == 2)
                    .Single();
            }

            return (uint)s_calculateChecksumMethod.Invoke(null, new object[]
            {
                peBlob,
                checksumBlob,
            })!;
        }

        private static void PatchModuleVersionIds(Blob guidFixup, Blob guidSectionFixup, Blob stringFixup, Guid mvid)
        {
            if (!guidFixup.IsDefault)
            {
                var writer = new BlobWriter(guidFixup);
                writer.WriteGuid(mvid);
                Debug.Assert(writer.RemainingBytes == 0);
            }

            if (!guidSectionFixup.IsDefault)
            {
                var writer = new BlobWriter(guidSectionFixup);
                writer.WriteGuid(mvid);
                Debug.Assert(writer.RemainingBytes == 0);
            }

            if (!stringFixup.IsDefault)
            {
                var writer = new BlobWriter(stringFixup);
                writer.WriteUserString(mvid.ToString());
                Debug.Assert(writer.RemainingBytes == 0);
            }
        }

        // Padding: We pad the path to this minimal size to
        // allow some tools to patch the path without the need to rewrite the entire image.
        // This is a workaround put in place until these tools are retired.
        private static string PadPdbPath(string path)
        {
            const int minLength = 260;
            return path + new string('\0', Math.Max(0, minLength - Encoding.UTF8.GetByteCount(path) - 1));
        }

        private static ResourceSectionBuilder? CreateNativeResourceSectionSerializer(CommonPEModuleBuilder module)
        {
            // Win32 resources are supplied to the compiler in one of two forms, .RES (the output of the resource compiler),
            // or .OBJ (the output of running cvtres.exe on a .RES file). A .RES file is parsed and processed into
            // a set of objects implementing IWin32Resources. These are then ordered and the final image form is constructed
            // and written to the resource section. Resources in .OBJ form are already very close to their final output
            // form. Rather than reading them and parsing them into a set of objects similar to those produced by
            // processing a .RES file, we process them like the native linker would, copy the relevant sections from
            // the .OBJ into our output and apply some fixups.

            var nativeResourceSectionOpt = module.Win32ResourceSection;
            if (nativeResourceSectionOpt != null)
            {
                return new ResourceSectionBuilderFromObj(nativeResourceSectionOpt);
            }

            var nativeResourcesOpt = module.Win32Resources;
            if (nativeResourcesOpt?.Any() == true)
            {
                return new ResourceSectionBuilderFromResources(nativeResourcesOpt);
            }

            var rawResourcesOpt = module.RawWin32Resources;
            if (rawResourcesOpt != null)
            {
                return new ResourceSectionBuilderFromRaw(rawResourcesOpt);
            }

            return null;
        }

        private sealed class ResourceSectionBuilderFromObj : ResourceSectionBuilder
        {
            private readonly ResourceSection _resourceSection;

            public ResourceSectionBuilderFromObj(ResourceSection resourceSection)
            {
                Debug.Assert(resourceSection != null);
                _resourceSection = resourceSection;
            }

            protected override void Serialize(BlobBuilder builder, SectionLocation location)
            {
                NativeResourceWriter.SerializeWin32Resources(builder, _resourceSection, location.RelativeVirtualAddress);
            }
        }

        private sealed class ResourceSectionBuilderFromResources : ResourceSectionBuilder
        {
            private readonly IEnumerable<IWin32Resource> _resources;

            public ResourceSectionBuilderFromResources(IEnumerable<IWin32Resource> resources)
            {
                Debug.Assert(resources.Any());
                _resources = resources;
            }

            protected override void Serialize(BlobBuilder builder, SectionLocation location)
            {
                NativeResourceWriter.SerializeWin32Resources(builder, _resources, location.RelativeVirtualAddress);
            }
        }

        private sealed class ResourceSectionBuilderFromRaw : ResourceSectionBuilder
        {
            private readonly Stream _resources;
            public ResourceSectionBuilderFromRaw(Stream resources)
            {
                _resources = resources;
            }

            protected override void Serialize(BlobBuilder builder, SectionLocation location)
            {
                int value;
                while ((value = _resources.ReadByte()) >= 0)
                {
                    builder.WriteByte((byte)value);
                }
            }
        }
    }
}
