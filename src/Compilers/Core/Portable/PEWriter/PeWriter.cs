// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Threading;
using Microsoft.CodeAnalysis;
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
        public static bool WritePeToStream(
            EmitContext context,
            CommonMessageProvider messageProvider,
            Func<Stream> getPeStream,
            Func<Stream> getPortablePdbStreamOpt,
            PdbWriter nativePdbWriterOpt,
            string pdbPathOpt,
            bool allowMissingMethodBodies,
            bool isDeterministic,
            CancellationToken cancellationToken)
        {
            // If PDB writer is given, we have to have PDB path.
            Debug.Assert(nativePdbWriterOpt == null || pdbPathOpt != null);

            var mdWriter = FullMetadataWriter.Create(context, messageProvider, allowMissingMethodBodies, isDeterministic, getPortablePdbStreamOpt != null, cancellationToken);

            var properties = context.Module.Properties;

            nativePdbWriterOpt?.SetMetadataEmitter(mdWriter);

            // Since we are producing a full assembly, we should not have a module version ID
            // imposed ahead-of time. Instead we will compute a deterministic module version ID
            // based on the contents of the generated stream.
            Debug.Assert(properties.PersistentIdentifier == default(Guid));

            var ilBuilder = new BlobBuilder(32 * 1024);
            var metadataBuilder = new BlobBuilder(16 * 1024);
            var mappedFieldDataBuilder = new BlobBuilder();
            var managedResourceBuilder = new BlobBuilder(1024);
            var debugMetadataBuilderOpt = (getPortablePdbStreamOpt != null) ? new BlobBuilder(16 * 1024) : null;

            mdWriter.BuildMetadataAndIL(
                nativePdbWriterOpt,
                ilBuilder,
                mappedFieldDataBuilder,
                managedResourceBuilder);

            int entryPointToken;
            int debugEntryPointToken;
            mdWriter.GetEntryPointTokens(out entryPointToken, out debugEntryPointToken);

            // entry point can only be a MethodDef:
            Debug.Assert(entryPointToken == 0 || (entryPointToken & 0xff000000) == 0x06000000);
            Debug.Assert(debugEntryPointToken == 0 || (debugEntryPointToken & 0xff000000) == 0x06000000);

            if (debugEntryPointToken != 0)
            {
                nativePdbWriterOpt?.SetEntryPoint((uint)debugEntryPointToken);
            }

            if (nativePdbWriterOpt != null)
            {
                var assembly = mdWriter.Module.AsAssembly;
                if (assembly != null && assembly.Kind == OutputKind.WindowsRuntimeMetadata)
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
            }

            Stream peStream = getPeStream();
            if (peStream == null)
            {
                return false;
            }

            var peBuilder = new PEBuilder(
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
                sizeOfHeapCommit: properties.SizeOfHeapCommit,
                isDeterministic: isDeterministic);

            var peDirectoriesBuilder = new PEDirectoriesBuilder();

            long peHeaderTimestampPosition;
            long metadataPosition = 0;
            int entryPointAddress = 0;
            var textSectionLocation = default(PESectionLocation);
            int moduleVersionIdOffsetInMetadataStream = 0;

            // .text
            peBuilder.AddSection(".text", SectionCharacteristics.MemRead | SectionCharacteristics.MemExecute | SectionCharacteristics.ContainsCode, location =>
            {
                textSectionLocation = location;

                var sectionBuilder = new BlobBuilder();
                ManagedTextSection textSection;
                MetadataSizes metadataSizes;

                mdWriter.SerializeManagedTextSection(
                    metadataBuilder,
                    ilBuilder,
                    mappedFieldDataBuilder,
                    managedResourceBuilder,
                    properties.ImageCharacteristics,
                    properties.Machine,
                    location.RelativeVirtualAddress,
                    pdbPathOpt,
                    out moduleVersionIdOffsetInMetadataStream,
                    out textSection,
                    out metadataSizes);

                int pdbIdOffsetInPortablePdbStream;
                if (mdWriter.EmitStandaloneDebugMetadata)
                {
                    mdWriter.SerializeStandaloneDebugMetadata(
                        debugMetadataBuilderOpt,
                        metadataSizes,
                        debugEntryPointToken,
                        out pdbIdOffsetInPortablePdbStream);
                }
                else
                {
                    pdbIdOffsetInPortablePdbStream = 0;
                }

                ContentId nativePdbContentId;
                if (nativePdbWriterOpt != null)
                {
                    nativePdbContentId = nativePdbWriterOpt.GetContentId();

                    // the writer shall not be used after this point for writing:
                    nativePdbWriterOpt = null;
                }
                else
                {
                    nativePdbContentId = default(ContentId);
                }

                // write to Portable PDB stream:
                ContentId portablePdbContentId;
                Stream portablePdbStream = getPortablePdbStreamOpt?.Invoke();
                if (portablePdbStream != null)
                {
                    debugMetadataBuilderOpt.WriteContentTo(portablePdbStream);

                    if (isDeterministic)
                    {
                        portablePdbContentId = ContentId.FromHash(CryptographicHashProvider.ComputeSha1(portablePdbStream));
                    }
                    else
                    {
                        portablePdbContentId = new ContentId(Guid.NewGuid().ToByteArray(), BitConverter.GetBytes(peBuilder.TimeDateStamp));
                    }

                    // fill in the PDB id:
                    long previousPosition = portablePdbStream.Position;
                    CheckZeroDataInStream(portablePdbStream, pdbIdOffsetInPortablePdbStream, ContentId.Size);
                    portablePdbStream.Position = pdbIdOffsetInPortablePdbStream;
                    portablePdbStream.Write(portablePdbContentId.Guid, 0, portablePdbContentId.Guid.Length);
                    portablePdbStream.Write(portablePdbContentId.Stamp, 0, portablePdbContentId.Stamp.Length);
                    portablePdbStream.Position = previousPosition;
                }
                else
                {
                    portablePdbContentId = default(ContentId);
                }

                BlobBuilder debugTableBuilderOpt;
                if (pdbPathOpt != null || isDeterministic)
                {
                    debugTableBuilderOpt = new BlobBuilder();
                    textSection.WriteDebugTable(debugTableBuilderOpt, location, nativePdbContentId, portablePdbContentId);
                }
                else
                {
                    debugTableBuilderOpt = null;
                }

                entryPointAddress = textSection.GetEntryPointAddress(location.RelativeVirtualAddress);

                textSection.Serialize(
                    sectionBuilder,
                    location.RelativeVirtualAddress,
                    entryPointToken,
                    properties.CorFlags,
                    properties.BaseAddress,
                    metadataBuilder,
                    ilBuilder,
                    mappedFieldDataBuilder,
                    managedResourceBuilder,
                    debugTableBuilderOpt,
                    out metadataPosition);

                peDirectoriesBuilder.AddressOfEntryPoint = entryPointAddress;
                peDirectoriesBuilder.DebugTable = textSection.GetDebugDirectoryEntry(location.RelativeVirtualAddress);
                peDirectoriesBuilder.ImportAddressTable = textSection.GetImportAddressTableDirectoryEntry(location.RelativeVirtualAddress);
                peDirectoriesBuilder.ImportTable = textSection.GetImportTableDirectoryEntry(location.RelativeVirtualAddress);
                peDirectoriesBuilder.CorHeaderTable = textSection.GetCorHeaderDirectoryEntry(location.RelativeVirtualAddress);

                return sectionBuilder;
            });

            // .rsrc
            var nativeResourcesOpt = context.Module.Win32Resources;
            var nativeResourceSectionOpt = context.Module.Win32ResourceSection;
            if (!IteratorHelper.EnumerableIsEmpty(nativeResourcesOpt) || nativeResourceSectionOpt != null)
            {
                // Win32 resources are supplied to the compiler in one of two forms, .RES (the output of the resource compiler),
                // or .OBJ (the output of running cvtres.exe on a .RES file). A .RES file is parsed and processed into
                // a set of objects implementing IWin32Resources. These are then ordered and the final image form is constructed
                // and written to the resource section. Resources in .OBJ form are already very close to their final output
                // form. Rather than reading them and parsing them into a set of objects similar to those produced by 
                // processing a .RES file, we process them like the native linker would, copy the relevant sections from 
                // the .OBJ into our output and apply some fixups.

                peBuilder.AddSection(".rsrc", SectionCharacteristics.MemRead | SectionCharacteristics.ContainsInitializedData, location =>
                {
                    var sectionBuilder = new BlobBuilder();

                    if (nativeResourceSectionOpt != null)
                    {
                        NativeResourceWriter.SerializeWin32Resources(sectionBuilder, nativeResourceSectionOpt, location.RelativeVirtualAddress);
                    }
                    else
                    {
                        NativeResourceWriter.SerializeWin32Resources(sectionBuilder, nativeResourcesOpt, location.RelativeVirtualAddress);
                    }

                    peDirectoriesBuilder.ResourceTable = new DirectoryEntry(location.RelativeVirtualAddress, sectionBuilder.Count);

                    return sectionBuilder;
                });
            }

            // .reloc
            if (properties.Machine == Machine.I386 || properties.Machine == 0)
            {
                peBuilder.AddSection(".reloc", SectionCharacteristics.MemRead | SectionCharacteristics.MemDiscardable | SectionCharacteristics.ContainsInitializedData, location =>
                {
                    var sectionBuilder = new BlobBuilder();
                    WriteRelocSection(sectionBuilder, properties.Machine, entryPointAddress);

                    peDirectoriesBuilder.BaseRelocationTable = new DirectoryEntry(location.RelativeVirtualAddress, sectionBuilder.Count);
                    return sectionBuilder;
                });
            }

            var peBlob = new BlobBuilder();
            peBuilder.Serialize(peBlob, peDirectoriesBuilder, out peHeaderTimestampPosition);
            peBlob.WriteContentTo(peStream);

            if (isDeterministic)
            {
                var mvidPosition = textSectionLocation.PointerToRawData + metadataPosition + moduleVersionIdOffsetInMetadataStream;
                WriteDeterministicGuidAndTimestamps(peStream, mvidPosition, peHeaderTimestampPosition);
            }

            return true;
        }

        /// <summary>
        /// Compute a deterministic Guid and timestamp based on the contents of the stream, and replace
        /// the 16 zero bytes at the given position and one or two 4-byte values with that computed Guid and timestamp.
        /// </summary>
        /// <param name="peStream">PE stream.</param>
        /// <param name="mvidPosition">Position in the stream of 16 zero bytes to be replaced by a Guid</param>
        /// <param name="peHeaderTimestampPosition">Position in the stream of four zero bytes to be replaced by a timestamp</param>
        private static void WriteDeterministicGuidAndTimestamps(
            Stream peStream,
            long mvidPosition,
            long peHeaderTimestampPosition)
        {
            Debug.Assert(mvidPosition != 0);
            Debug.Assert(peHeaderTimestampPosition != 0);

            var previousPosition = peStream.Position;

            // Compute and write deterministic guid data over the relevant portion of the stream
            peStream.Position = 0;
            var contentId = ContentId.FromHash(CryptographicHashProvider.ComputeSha1(peStream));

            // The existing Guid should be zero.
            CheckZeroDataInStream(peStream, mvidPosition, contentId.Guid.Length);
            peStream.Position = mvidPosition;
            peStream.Write(contentId.Guid, 0, contentId.Guid.Length);

            // The existing timestamp should be zero.
            CheckZeroDataInStream(peStream, peHeaderTimestampPosition, contentId.Stamp.Length);
            peStream.Position = peHeaderTimestampPosition;
            peStream.Write(contentId.Stamp, 0, contentId.Stamp.Length);

            peStream.Position = previousPosition;
        }

        [Conditional("DEBUG")]
        private static void CheckZeroDataInStream(Stream stream, long position, int bytes)
        {
            stream.Position = position;
            for (int i = 0; i < bytes; i++)
            {
                int value = stream.ReadByte();
                Debug.Assert(value == 0);
            }
        }
                
        private static void WriteRelocSection(BlobBuilder builder, Machine machine, int entryPointAddress)
        {
            Debug.Assert(builder.Count == 0);

            builder.WriteUInt32((((uint)entryPointAddress + 2) / 0x1000) * 0x1000);
            builder.WriteUInt32((machine == Machine.IA64) ? 14u : 12u);
            uint offsetWithinPage = ((uint)entryPointAddress + 2) % 0x1000;
            uint relocType = (machine == Machine.Amd64 || machine == Machine.IA64) ? 10u : 3u;
            ushort s = (ushort)((relocType << 12) | offsetWithinPage);
            builder.WriteUInt16(s);
            if (machine == Machine.IA64)
            {
                builder.WriteUInt32(relocType << 12);
            }

            builder.WriteUInt16(0); // next chunk's RVA
        }
    }
}
