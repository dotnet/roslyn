﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
using Roslyn.Reflection.PortableExecutable;
using static Microsoft.CodeAnalysis.SigningUtilities;
using EmitContext = Microsoft.CodeAnalysis.Emit.EmitContext;
using Microsoft.DiaSymReader;

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
        // Remove this function when  https://github.com/dotnet/roslyn/issues/25185 is fixed
        private static byte ReadByteFromBlobBuilder(BlobBuilder blobBuilder, int offset)
        {
            BlobBuilder.Blobs blobs = blobBuilder.GetBlobs();
            int startOffsetOfBlob = 0;
            foreach (Blob b in blobs)
            {
                ArraySegment<byte> bytesInBlob = b.GetBytes();
                int offsetInBlob = offset - startOffsetOfBlob;

                if (offsetInBlob < bytesInBlob.Count)
                    return ((IList<Byte>)bytesInBlob)[offsetInBlob];

                startOffsetOfBlob += bytesInBlob.Count;
            }
            throw new IndexOutOfRangeException();
        }

        // Remove this function when  https://github.com/dotnet/roslyn/issues/25185 is fixed
        private static void WriteByteToBlobBuilder(BlobBuilder blobBuilder, int offset, byte data)
        {
            BlobBuilder.Blobs blobs = blobBuilder.GetBlobs();
            int startOffsetOfBlob = 0;
            foreach (Blob b in blobs)
            {
                ArraySegment<byte> bytesInBlob = b.GetBytes();
                int offsetInBlob = offset - startOffsetOfBlob;

                if (offsetInBlob < bytesInBlob.Count)
                {
                    ((IList<Byte>)bytesInBlob)[offsetInBlob] = data;
                    return;
                }

                startOffsetOfBlob += bytesInBlob.Count;
            }
        }

        internal static bool WritePeToStream(
            EmitContext context,
            CommonMessageProvider messageProvider,
            Func<Stream> getPeStream,
            Func<Stream> getPortablePdbStreamOpt,
            PdbWriter nativePdbWriterOpt,
            string pdbPathOpt,
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

            nativePdbWriterOpt?.SetMetadataEmitter(mdWriter);

            // Since we are producing a full assembly, we should not have a module version ID
            // imposed ahead-of time. Instead we will compute a deterministic module version ID
            // based on the contents of the generated stream.
            Debug.Assert(properties.PersistentIdentifier == default(Guid));

            var ilBuilder = new BlobBuilder(32 * 1024);
            var mappedFieldDataBuilder = new BlobBuilder();
            var managedResourceBuilder = new BlobBuilder(1024);

            Blob mvidFixup, mvidStringFixup;
            mdWriter.BuildMetadataAndIL(
                nativePdbWriterOpt,
                ilBuilder,
                mappedFieldDataBuilder,
                managedResourceBuilder,
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
                    // if same scenario would happen in an winmdobj project
                    nativePdbWriterOpt.AssertAllDefinitionsHaveTokens(mdWriter.Module.GetSymbolToLocationMap());
#endif
                }

                nativePdbWriterOpt.WriteRemainingEmbeddedDocuments(mdWriter.Module.DebugDocumentsBuilder.EmbeddedDocuments);
            }

            Stream peStream = getPeStream();
            if (peStream == null)
            {
                return false;
            }

            BlobContentId pdbContentId = nativePdbWriterOpt?.GetContentId() ?? default;

            // the writer shall not be used after this point for writing:
            nativePdbWriterOpt = null;

            ushort portablePdbVersion = 0;
            var metadataRootBuilder = mdWriter.GetRootBuilder();

            Machine SRMVisibleMachine = properties.Machine;

            // When attempting to write an Arm64 PE file, tell the PEBuilder that an Amd64 pe is being written instead
            // then replace the bits in the produced PE header with the correct bits. This will allow an Arm64 pe to be
            // generated without requiring the System.Reflection.Metadata dll to be updated to a newer version.
            // Remove this code when  https://github.com/dotnet/roslyn/issues/25185 is fixed
            if (SRMVisibleMachine == (Machine)0xAA64)
                SRMVisibleMachine = Machine.Amd64;

            var peHeaderBuilder = new PEHeaderBuilder(
                machine: SRMVisibleMachine,
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

            // TODO: replace SAH1 with non-crypto alg: https://github.com/dotnet/roslyn/issues/24737
            var peIdProvider = isDeterministic ?
                new Func<IEnumerable<Blob>, BlobContentId>(content => BlobContentId.FromHash(CryptographicHashProvider.ComputeHash(HashAlgorithmName.SHA1, content))) :
                null;
                
            // We need to calculate the PDB checksum, so we may as well use the calculated hash for PDB ID regardless of whether deterministic build is requested.
            var portablePdbContentHash = default(ImmutableArray<byte>);
            
            BlobBuilder portablePdbToEmbed = null;
            if (mdWriter.EmitPortableDebugMetadata)
            {
                mdWriter.AddRemainingEmbeddedDocuments(mdWriter.Module.DebugDocumentsBuilder.EmbeddedDocuments);

                // The algorithm must be specified for deterministic builds (checked earlier).
                Debug.Assert(!isDeterministic || context.Module.PdbChecksumAlgorithm.Name != null);

                var portablePdbIdProvider = (context.Module.PdbChecksumAlgorithm.Name != null) ?
                    new Func<IEnumerable<Blob>, BlobContentId>(content => BlobContentId.FromHash(portablePdbContentHash = CryptographicHashProvider.ComputeHash(context.Module.PdbChecksumAlgorithm, content))) :
                    null;

                var portablePdbBlob = new BlobBuilder();
                var portablePdbBuilder = mdWriter.GetPortablePdbBuilder(metadataRootBuilder.Sizes.RowCounts, debugEntryPointHandle, portablePdbIdProvider);
                pdbContentId = portablePdbBuilder.Serialize(portablePdbBlob);
                portablePdbVersion = portablePdbBuilder.FormatVersion;

                if (getPortablePdbStreamOpt == null)
                {
                    // embed to debug directory:
                    portablePdbToEmbed = portablePdbBlob;
                }
                else
                {
                    // write to Portable PDB stream:
                    Stream portablePdbStream = getPortablePdbStreamOpt();
                    if (portablePdbStream != null)
                    {
                        try
                        {
                            portablePdbBlob.WriteContentTo(portablePdbStream);
                        }
                        catch (Exception e) when (!(e is OperationCanceledException))
                        {
                            throw new SymUnmanagedWriterException(e.Message, e);
                        }
                    }
                }
            }

            DebugDirectoryBuilder debugDirectoryBuilder;
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
                        debugDirectoryBuilder.AddPdbChecksumEntry(context.Module.PdbChecksumAlgorithm.Name, portablePdbContentHash);
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
                ilBuilder,
                mappedFieldDataBuilder,
                managedResourceBuilder,
                CreateNativeResourceSectionSerializer(context.Module),
                debugDirectoryBuilder,
                CalculateStrongNameSignatureSize(context.Module, privateKeyOpt),
                entryPointHandle,
                corFlags,
                peIdProvider,
                metadataOnly && !context.IncludePrivateMembers);

            var peBlob = new BlobBuilder();
            var peContentId = peBuilder.Serialize(peBlob, out Blob mvidSectionFixup);

            // Remove this code when  https://github.com/dotnet/roslyn/issues/25185 is fixed
            if (SRMVisibleMachine != properties.Machine)
            {
                // ARM64 pe
                // Update Amd64 machine type in PE header to 0xAA64
                // Machine type in PEHeader is located at...
                // 128 bytes (dos header)
                // 4 bytes (PE Signature)
                // 2 bytes (Machine)
                // 2 bytes (NumberOfSections)
                // 4 bytes (TimeDateStamp)
                int machineOffset = 128 + 4;
                int timeDateStampOffset = 128 + 4 + 2 + 2;
                ushort amd64MachineType = (ushort)Machine.Amd64;
                Debug.Assert(ReadByteFromBlobBuilder(peBlob, machineOffset) == (byte)amd64MachineType);
                Debug.Assert(ReadByteFromBlobBuilder(peBlob, machineOffset + 1) == (byte)(amd64MachineType >> 8));

                ushort realMachineType = (ushort)properties.Machine;
                byte lsbMachineType = (byte)realMachineType;
                byte msbMachineType = (byte)(realMachineType >> 8);

                // This code path is currently only expected to be used for ARM64
                Debug.Assert(lsbMachineType == 0x64);
                Debug.Assert(msbMachineType == 0xAA);

                WriteByteToBlobBuilder(peBlob, machineOffset, lsbMachineType);
                WriteByteToBlobBuilder(peBlob, machineOffset + 1, msbMachineType);

                if (peIdProvider != null)
                {
                    // Patch timestamp in COFF Header to all zeroes
                    WriteByteToBlobBuilder(peBlob, timeDateStampOffset, 0);
                    WriteByteToBlobBuilder(peBlob, timeDateStampOffset + 1, 0);
                    WriteByteToBlobBuilder(peBlob, timeDateStampOffset + 2, 0);
                    WriteByteToBlobBuilder(peBlob, timeDateStampOffset + 3, 0);

                    peContentId = peIdProvider(peBlob.GetBlobs());

                    // patch timestamp in COFF header:
                    uint newTimestamp = peContentId.Stamp;
                    WriteByteToBlobBuilder(peBlob, timeDateStampOffset, (byte)newTimestamp);
                    WriteByteToBlobBuilder(peBlob, timeDateStampOffset + 1, (byte)(newTimestamp >> 8));
                    WriteByteToBlobBuilder(peBlob, timeDateStampOffset + 2, (byte)(newTimestamp >> 16));
                    WriteByteToBlobBuilder(peBlob, timeDateStampOffset + 3, (byte)(newTimestamp >> 24));
                }
            }

            PatchModuleVersionIds(mvidFixup, mvidSectionFixup, mvidStringFixup, peContentId.Guid);

            if (privateKeyOpt != null && corFlags.HasFlag(CorFlags.StrongNameSigned))
            {
                Debug.Assert(strongNameProvider.Capability == SigningCapability.SignsPeBuilder);
                strongNameProvider.SignPeBuilder(peBuilder, peBlob, privateKeyOpt.Value);
                FixupChecksum(peBuilder, peBlob);
            }

            try
            {
                peBlob.WriteContentTo(peStream);
            }
            catch (Exception e) when (!(e is OperationCanceledException))
            {
                throw new PeWritingException(e);
            }

            return true;
        }

        private static void FixupChecksum(ExtendedPEBuilder peBuilder, BlobBuilder peBlob)
        {
            // Checksum fixup, workaround for https://github.com/dotnet/corefx/issues/25829
            // Tracked by https://github.com/dotnet/roslyn/issues/23762
            // Since the checksum is calculated before signing in the PEBuilder,
            // we need to redo the calculation and write in the correct checksum
            Blob checksumBlob = getChecksumBlob(peBuilder);

            ArraySegment<byte> checksumSegment = checksumBlob.GetBytes();
            uint oldChecksum = BitConverter.ToUInt32(checksumSegment.Array, checksumSegment.Offset);
            uint newChecksum = CalculateChecksum(peBlob, checksumBlob);
            new BlobWriter(checksumBlob).WriteUInt32(newChecksum);

            // If this assert fires, the above bug has been fixed and this workaround should
            // be removed
            Debug.Assert(oldChecksum != newChecksum);

            Blob getChecksumBlob(PEBuilder builder)
                => (Blob)typeof(PEBuilder).GetRuntimeFields()
                    .Where(f => f.Name == "_lazyChecksum")
                    .Single()
                    .GetValue(builder);
        }

        private static MethodInfo s_calculateChecksumMethod;
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
            });
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

        private static ResourceSectionBuilder CreateNativeResourceSectionSerializer(CommonPEModuleBuilder module)
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

            return null;
        }

        private class ResourceSectionBuilderFromObj : ResourceSectionBuilder
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

        private class ResourceSectionBuilderFromResources : ResourceSectionBuilder
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
    }
}
