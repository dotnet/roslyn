// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

#if SRM
namespace System.Reflection.PortableExecutable
#else
namespace Roslyn.Reflection.PortableExecutable
#endif
{
#if !SRM
    using Roslyn.Reflection.Metadata.Ecma335;
#endif

#if SRM
    public
#endif
    static class ManagedPEBuilder
    {
        public static void AddManagedSections(
            this PEBuilder peBuilder,
            PEDirectoriesBuilder peDirectoriesBuilder,
            TypeSystemMetadataSerializer metadataSerializer,
            BlobBuilder ilStream,
            BlobBuilder mappedFieldData,
            BlobBuilder managedResourceData,
            Action<BlobBuilder, PESectionLocation> nativeResourceSectionSerializer, // opt
            int strongNameSignatureSize, // TODO
            MethodDefinitionHandle entryPoint,
            string pdbPathOpt, // TODO
            ContentId nativePdbContentId, // TODO
            ContentId portablePdbContentId, // TODO
            CorFlags corFlags)
        {
            int entryPointAddress = 0;

            // .text
            peBuilder.AddSection(".text", SectionCharacteristics.MemRead | SectionCharacteristics.MemExecute | SectionCharacteristics.ContainsCode, location =>
            {
                var sectionBuilder = new BlobBuilder();
                var metadataBuilder = new BlobBuilder();

                var metadataSizes = metadataSerializer.MetadataSizes;

                var textSection = new ManagedTextSection(
                    metadataSizes.MetadataSize,
                    ilStreamSize: ilStream.Count,
                    mappedFieldDataSize: mappedFieldData.Count,
                    resourceDataSize: managedResourceData.Count,
                    strongNameSignatureSize: strongNameSignatureSize,
                    imageCharacteristics: peBuilder.ImageCharacteristics,
                    machine: peBuilder.Machine,
                    pdbPathOpt: pdbPathOpt,
                    isDeterministic: peBuilder.IsDeterministic);

                int methodBodyStreamRva = location.RelativeVirtualAddress + textSection.OffsetToILStream;
                int mappedFieldDataStreamRva = location.RelativeVirtualAddress + textSection.CalculateOffsetToMappedFieldDataStream();
                metadataSerializer.SerializeMetadata(metadataBuilder, methodBodyStreamRva, mappedFieldDataStreamRva);

                BlobBuilder debugTableBuilderOpt;
                if (pdbPathOpt != null || peBuilder.IsDeterministic)
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
                    entryPoint.IsNil ? 0 : MetadataTokens.GetToken(entryPoint),
                    corFlags,
                    peBuilder.ImageBase,
                    metadataBuilder,
                    ilStream,
                    mappedFieldData,
                    managedResourceData,
                    debugTableBuilderOpt);

                peDirectoriesBuilder.AddressOfEntryPoint = entryPointAddress;
                peDirectoriesBuilder.DebugTable = textSection.GetDebugDirectoryEntry(location.RelativeVirtualAddress);
                peDirectoriesBuilder.ImportAddressTable = textSection.GetImportAddressTableDirectoryEntry(location.RelativeVirtualAddress);
                peDirectoriesBuilder.ImportTable = textSection.GetImportTableDirectoryEntry(location.RelativeVirtualAddress);
                peDirectoriesBuilder.CorHeaderTable = textSection.GetCorHeaderDirectoryEntry(location.RelativeVirtualAddress);

                return sectionBuilder;
            });

            // .rsrc
            if (nativeResourceSectionSerializer != null)
            {
                peBuilder.AddSection(".rsrc", SectionCharacteristics.MemRead | SectionCharacteristics.ContainsInitializedData, location => 
                {
                    var sectionBuilder = new BlobBuilder();
                    nativeResourceSectionSerializer(sectionBuilder, location);
                    peDirectoriesBuilder.ResourceTable = new DirectoryEntry(location.RelativeVirtualAddress, sectionBuilder.Count);
                    return sectionBuilder;
                });
            }

            // .reloc
            if (peBuilder.Machine == Machine.I386 || peBuilder.Machine == 0)
            {
                peBuilder.AddSection(".reloc", SectionCharacteristics.MemRead | SectionCharacteristics.MemDiscardable | SectionCharacteristics.ContainsInitializedData, location =>
                {
                    var sectionBuilder = new BlobBuilder();
                    WriteRelocSection(sectionBuilder, peBuilder.Machine, entryPointAddress);

                    peDirectoriesBuilder.BaseRelocationTable = new DirectoryEntry(location.RelativeVirtualAddress, sectionBuilder.Count);
                    return sectionBuilder;
                });
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
