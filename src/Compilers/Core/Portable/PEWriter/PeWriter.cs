// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;
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
            var nativeResourcesOpt = context.Module.Win32Resources;
            var nativeResourceSectionOpt = context.Module.Win32ResourceSection;

            // In the PE File Header this is a "Time/Date Stamp" whose description is "Time and date
            // the file was created in seconds since January 1st 1970 00:00:00 or 0"
            // However, when we want to make it deterministic we fill it in (later) with bits from the hash of the full PE file.
            int timestamp = isDeterministic ? 0 : (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;

            // TODO: we can precalculate the exact size of IL stream
            var ilBuilder = new BlobBuilder(32 * 1024);
            var metadataBuilder = new BlobBuilder(16 * 1024);
            var mappedFieldDataBuilder = new BlobBuilder();
            var managedResourceBuilder = new BlobBuilder(1024);
            var textSectionBlob = new BlobBuilder();
            var resourceSectionBlobOpt = (!IteratorHelper.EnumerableIsEmpty(nativeResourcesOpt) || nativeResourceSectionOpt != null) ? new BlobBuilder() : null;
            var relocationSectionBlobOpt = properties.RequiresStartupStub ? new BlobBuilder() : null;
            var debugMetadataBuilderOpt = (getPortablePdbStreamOpt != null) ? new BlobBuilder(16 * 1024) : null;

            nativePdbWriterOpt?.SetMetadataEmitter(mdWriter);

            // Since we are producing a full assembly, we should not have a module version ID
            // imposed ahead-of time. Instead we will compute a deterministic module version ID
            // based on the contents of the generated stream.
            Debug.Assert(properties.PersistentIdentifier == default(Guid));

            var peBuilder = new PEBuilder(
                sectionCount: 1 + (resourceSectionBlobOpt != null ? 1 : 0) + (relocationSectionBlobOpt != null ? 1 : 0),
                sectionAlignment: properties.SectionAlignment,
                fileAlignment: properties.FileAlignment,
                is32Bit: !properties.Requires64bits);

            // We emit the .text section as the first section in the PE image.
            var textSectionLocation = peBuilder.NextSectionLocation;

            ManagedTextSection textSection;

            int moduleVersionIdOffsetInMetadataStream;
            int pdbIdOffsetInPortablePdbStream;

            int entryPointToken;
            MetadataSizes metadataSizes;
            mdWriter.SerializeMetadataAndIL(
                metadataBuilder,
                debugMetadataBuilderOpt,
                nativePdbWriterOpt,
                ilBuilder,
                mappedFieldDataBuilder,
                managedResourceBuilder,
                properties.ImageCharacteristics,
                properties.Machine,
                textSectionLocation.RelativeVirtualAddress,
                pdbPathOpt,
                out moduleVersionIdOffsetInMetadataStream,
                out pdbIdOffsetInPortablePdbStream,
                out entryPointToken,
                out textSection,
                out metadataSizes);

            ContentId nativePdbContentId;
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
                    portablePdbContentId = new ContentId(Guid.NewGuid().ToByteArray(), BitConverter.GetBytes(timestamp));
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

            Stream peStream = getPeStream();
            if (peStream == null)
            {
                return false;
            }

            BlobBuilder debugTableBuilderOpt;
            if (pdbPathOpt != null || isDeterministic)
            {
                debugTableBuilderOpt = new BlobBuilder();
                textSection.WriteDebugTable(debugTableBuilderOpt, textSectionLocation, nativePdbContentId, portablePdbContentId);
            }
            else
            {
                debugTableBuilderOpt = null;
            }

            int entryPointAddress = textSection.GetEntryPointAddress(textSectionLocation.RelativeVirtualAddress);

            long peHeaderTimestampPosition;
            long metadataPosition;

            textSection.Serialize(
                textSectionBlob,
                textSectionLocation.RelativeVirtualAddress,
                entryPointToken,
                properties.GetCorHeaderFlags(),
                properties.BaseAddress,
                metadataBuilder,
                ilBuilder,
                mappedFieldDataBuilder,
                managedResourceBuilder,
                debugTableBuilderOpt,
                out metadataPosition);

            peBuilder.Machine = properties.Machine;
            peBuilder.TimeDateStamp = timestamp;
            peBuilder.ImageCharacteristics = properties.ImageCharacteristics;
            peBuilder.MajorLinkerVersion = properties.LinkerMajorVersion;
            peBuilder.MinorLinkerVersion = properties.LinkerMinorVersion;
            peBuilder.AddressOfEntryPoint = entryPointAddress;
            peBuilder.ImageBase = properties.BaseAddress;
            peBuilder.MajorSubsystemVersion = properties.MajorSubsystemVersion;
            peBuilder.MinorSubsystemVersion = properties.MinorSubsystemVersion;
            peBuilder.Subsystem = properties.Subsystem;
            peBuilder.DllCharacteristics = properties.DllCharacteristics;
            peBuilder.SizeOfStackReserve = properties.SizeOfStackReserve;
            peBuilder.SizeOfStackCommit = properties.SizeOfStackCommit;
            peBuilder.SizeOfHeapReserve = properties.SizeOfHeapReserve;
            peBuilder.SizeOfHeapCommit = properties.SizeOfHeapCommit;

            // .text
            peBuilder.AddSection(
                ".text",
                SectionCharacteristics.MemRead | SectionCharacteristics.MemExecute | SectionCharacteristics.ContainsCode,
                textSectionBlob);

            peBuilder.DebugTable = textSection.GetDebugDirectoryEntry(textSectionLocation.RelativeVirtualAddress);
            peBuilder.ImportAddressTable = textSection.GetImportAddressTableDirectoryEntry(textSectionLocation.RelativeVirtualAddress);
            peBuilder.ImportTable = textSection.GetImportTableDirectoryEntry(textSectionLocation.RelativeVirtualAddress);
            peBuilder.CorHeaderTable = textSection.GetCorHeaderDirectoryEntry(textSectionLocation.RelativeVirtualAddress);

            // .rsrc
            if (resourceSectionBlobOpt != null)
            {
                // Win32 resources are supplied to the compiler in one of two forms, .RES (the output of the resource compiler),
                // or .OBJ (the output of running cvtres.exe on a .RES file). A .RES file is parsed and processed into
                // a set of objects implementing IWin32Resources. These are then ordered and the final image form is constructed
                // and written to the resource section. Resources in .OBJ form are already very close to their final output
                // form. Rather than reading them and parsing them into a set of objects similar to those produced by 
                // processing a .RES file, we process them like the native linker would, copy the relevant sections from 
                // the .OBJ into our output and apply some fixups.
                var location = peBuilder.NextSectionLocation;

                if (nativeResourceSectionOpt != null)
                {
                    NativeResourceWriter.SerializeWin32Resources(resourceSectionBlobOpt, nativeResourceSectionOpt, location.RelativeVirtualAddress);
                }
                else 
                {
                    NativeResourceWriter.SerializeWin32Resources(resourceSectionBlobOpt, nativeResourcesOpt, location.RelativeVirtualAddress);
                }

                peBuilder.AddSection(
                    ".rsrc",
                    SectionCharacteristics.MemRead | SectionCharacteristics.ContainsInitializedData,
                    resourceSectionBlobOpt);

                peBuilder.ResourceTable = new DirectoryEntry(location.RelativeVirtualAddress, resourceSectionBlobOpt.Count);
            }

            // .reloc
            if (relocationSectionBlobOpt != null)
            {
                var location = peBuilder.NextSectionLocation;

                WriteRelocSection(relocationSectionBlobOpt, properties, entryPointAddress);

                peBuilder.AddSection(
                    ".reloc",
                    SectionCharacteristics.MemRead | SectionCharacteristics.MemDiscardable | SectionCharacteristics.ContainsInitializedData,
                    relocationSectionBlobOpt);

                peBuilder.BaseRelocationTable = new DirectoryEntry(location.RelativeVirtualAddress, relocationSectionBlobOpt.Count);
            }

            var peBlob = new BlobBuilder();
            peBuilder.Serialize(peBlob, out peHeaderTimestampPosition);
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
                
        private static void WriteRelocSection(BlobBuilder builder, ModulePropertiesForSerialization properties, int entryPointAddress)
        {
            Debug.Assert(builder.Count == 0);

            builder.WriteUInt32((((uint)entryPointAddress + 2) / 0x1000) * 0x1000);
            builder.WriteUInt32(properties.Requires64bits && !properties.RequiresAmdInstructionSet ? 14u : 12u);
            uint offsetWithinPage = ((uint)entryPointAddress + 2) % 0x1000;
            uint relocType = properties.Requires64bits ? 10u : 3u;
            ushort s = (ushort)((relocType << 12) | offsetWithinPage);
            builder.WriteUInt16(s);
            if (properties.Requires64bits && !properties.RequiresAmdInstructionSet)
            {
                builder.WriteUInt32(relocType << 12);
            }

            builder.WriteUInt16(0); // next chunk's RVA
        }
    }
}
