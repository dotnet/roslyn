// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
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

    internal sealed class PeWriter
    {
        private const string ResourceSectionName = ".rsrc";
        private const string RelocationSectionName = ".reloc";

        /// <summary>
        /// Minimal size of PDB path in Debug Directory. We pad the path to this minimal size to
        /// allow some tools to patch the path without the need to rewrite the entire image.
        /// This is a workaround put in place until these tools are retired.
        /// </summary>
        private readonly int _minPdbPath;

        /// <summary>
        /// True if we should attempt to generate a deterministic output (no timestamps or random data).
        /// </summary>
        private readonly bool _deterministic;
        private readonly int _timeStamp;

        private readonly string _pdbPathOpt;
        private readonly bool _is32bit;
        private readonly ModulePropertiesForSerialization _properties;

        private readonly IEnumerable<IWin32Resource> _nativeResourcesOpt;
        private readonly ResourceSection _nativeResourceSectionOpt;

        private readonly BlobBuilder _win32ResourceWriter = new BlobBuilder(1024);
        
        private PeWriter(
            ModulePropertiesForSerialization properties,
            IEnumerable<IWin32Resource> nativeResourcesOpt,
            ResourceSection nativeResourceSectionOpt,
            string pdbPathOpt, 
            bool deterministic)
        {
            _properties = properties;
            _pdbPathOpt = pdbPathOpt;
            _deterministic = deterministic;

            // The PDB padding workaround is only needed for legacy tools that don't use deterministic build.
            _minPdbPath = deterministic ? 0 : 260;
            _nativeResourcesOpt = nativeResourcesOpt;
            _nativeResourceSectionOpt = nativeResourceSectionOpt;
            _is32bit = !_properties.Requires64bits;

            // In the PE File Header this is a "Time/Date Stamp" whose description is "Time and date
            // the file was created in seconds since January 1st 1970 00:00:00 or 0"
            // However, when we want to make it deterministic we fill it in (later) with bits from the hash of the full PE file.
            _timeStamp = _deterministic ? 0 : (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
        }

        private bool EmitPdb => _pdbPathOpt != null;

        public static bool WritePeToStream(
            EmitContext context,
            CommonMessageProvider messageProvider,
            Func<Stream> getPeStream,
            Func<Stream> getPortablePdbStreamOpt,
            PdbWriter nativePdbWriterOpt,
            string pdbPathOpt,
            bool allowMissingMethodBodies,
            bool deterministic,
            CancellationToken cancellationToken)
        {
            // If PDB writer is given, we have to have PDB path.
            Debug.Assert(nativePdbWriterOpt == null || pdbPathOpt != null);

            var peWriter = new PeWriter(context.Module.Properties, context.Module.Win32Resources, context.Module.Win32ResourceSection, pdbPathOpt, deterministic);
            var mdWriter = FullMetadataWriter.Create(context, messageProvider, allowMissingMethodBodies, deterministic, getPortablePdbStreamOpt != null, cancellationToken);

            return peWriter.WritePeToStream(mdWriter, getPeStream, getPortablePdbStreamOpt, nativePdbWriterOpt);
        }

        private bool WritePeToStream(MetadataWriter mdWriter, Func<Stream> getPeStream, Func<Stream> getPortablePdbStreamOpt, PdbWriter nativePdbWriterOpt)
        {
            // TODO: we can precalculate the exact size of IL stream
            var ilWriter = new BlobBuilder(32 * 1024);
            var metadataWriter = new BlobBuilder(16 * 1024);
            var mappedFieldDataWriter = new BlobBuilder();
            var managedResourceWriter = new BlobBuilder(1024);

            var debugMetadataWriterOpt = (getPortablePdbStreamOpt != null) ? new BlobBuilder(16 * 1024) : null;

            nativePdbWriterOpt?.SetMetadataEmitter(mdWriter);

            // Since we are producing a full assembly, we should not have a module version ID
            // imposed ahead-of time. Instead we will compute a deterministic module version ID
            // based on the contents of the generated stream.
            Debug.Assert(_properties.PersistentIdentifier == default(Guid));

            int sectionCount = 1;
            if (_properties.RequiresStartupStub) sectionCount++; //.reloc
            if (!IteratorHelper.EnumerableIsEmpty(_nativeResourcesOpt) || _nativeResourceSectionOpt != null) sectionCount++; //.rsrc;

            int sizeOfPeHeaders = ComputeSizeOfPeHeaders(sectionCount);
            int textSectionRva = BitArithmeticUtilities.Align(sizeOfPeHeaders, _properties.SectionAlignment);

            int moduleVersionIdOffsetInMetadataStream;
            int methodBodyStreamRva = textSectionRva + OffsetToILStream;
            int pdbIdOffsetInPortablePdbStream;

            int entryPointToken;
            MetadataSizes metadataSizes;
            mdWriter.SerializeMetadataAndIL(
                metadataWriter,
                debugMetadataWriterOpt,
                nativePdbWriterOpt,
                ilWriter,
                mappedFieldDataWriter,
                managedResourceWriter,
                methodBodyStreamRva,
                mdSizes => CalculateMappedFieldDataStreamRva(textSectionRva, mdSizes),
                out moduleVersionIdOffsetInMetadataStream,
                out pdbIdOffsetInPortablePdbStream,
                out entryPointToken,
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
                debugMetadataWriterOpt.WriteContentTo(portablePdbStream);

                if (_deterministic)
                {
                    portablePdbContentId = ContentId.FromHash(CryptographicHashProvider.ComputeSha1(portablePdbStream));
                }
                else
                {
                    portablePdbContentId = new ContentId(Guid.NewGuid().ToByteArray(), BitConverter.GetBytes(_timeStamp));
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

            // Only the size of the fixed part of the debug table goes here.
            DirectoryEntry debugDirectory = default(DirectoryEntry);
            DirectoryEntry importTable = default(DirectoryEntry);
            DirectoryEntry importAddressTable = default(DirectoryEntry);
            int entryPointAddress = 0;

            if (EmitPdb || _deterministic)
            {
                debugDirectory = new DirectoryEntry(textSectionRva + ComputeOffsetToDebugTable(metadataSizes), ImageDebugDirectoryBaseSize);
            }

            if (_properties.RequiresStartupStub)
            {
                importAddressTable = new DirectoryEntry(textSectionRva, SizeOfImportAddressTable);
                entryPointAddress = CalculateMappedFieldDataStreamRva(textSectionRva, metadataSizes) - (_is32bit ? 6 : 10); // TODO: constants
                importTable = new DirectoryEntry(textSectionRva + ComputeOffsetToImportTable(metadataSizes), (_is32bit ? 66 : 70) + 13); // TODO: constants
            }

            var corHeaderDirectory = new DirectoryEntry(textSectionRva + SizeOfImportAddressTable, size: CorHeaderSize);

            long ntHeaderTimestampPosition;
            long metadataPosition;

            List<SectionHeader> sectionHeaders = CreateSectionHeaders(metadataSizes, sectionCount);

            CoffHeader coffHeader;
            NtHeader ntHeader;
            FillInNtHeader(sectionHeaders, entryPointAddress, corHeaderDirectory, importTable, importAddressTable, debugDirectory, out coffHeader, out ntHeader);

            Stream peStream = getPeStream();
            if (peStream == null)
            {
                return false;
            }

            WriteHeaders(peStream, ntHeader, coffHeader, sectionHeaders, out ntHeaderTimestampPosition);

            WriteTextSection(
                peStream,
                sectionHeaders[0],
                importTable.RelativeVirtualAddress,
                importAddressTable.RelativeVirtualAddress,
                entryPointToken,
                metadataWriter,
                ilWriter,
                mappedFieldDataWriter,
                managedResourceWriter,
                metadataSizes,
                nativePdbContentId,
                portablePdbContentId,
                out metadataPosition);

            var resourceSection = sectionHeaders.FirstOrDefault(s => s.Name == ResourceSectionName);
            if (resourceSection != null)
            {
                WriteResourceSection(peStream, resourceSection);
            }

            var relocSection = sectionHeaders.FirstOrDefault(s => s.Name == RelocationSectionName);
            if (relocSection != null)
            {
                WriteRelocSection(peStream, relocSection, entryPointAddress);
            }

            if (_deterministic)
            {
                var mvidPosition = metadataPosition + moduleVersionIdOffsetInMetadataStream;
                WriteDeterministicGuidAndTimestamps(peStream, mvidPosition, ntHeaderTimestampPosition);
            }

            return true;
        }

        private List<SectionHeader> CreateSectionHeaders(MetadataSizes metadataSizes, int sectionCount)
        {
            var sectionHeaders = new List<SectionHeader>();
            SectionHeader lastSection;
            int sizeOfPeHeaders = ComputeSizeOfPeHeaders(sectionCount);
            int sizeOfTextSection = ComputeSizeOfTextSection(metadataSizes);

            sectionHeaders.Add(lastSection = new SectionHeader(
                characteristics: SectionCharacteristics.MemRead |
                                 SectionCharacteristics.MemExecute |
                                 SectionCharacteristics.ContainsCode,
                name: ".text",
                numberOfLinenumbers: 0,
                numberOfRelocations: 0,
                pointerToLinenumbers: 0,
                pointerToRawData: BitArithmeticUtilities.Align(sizeOfPeHeaders, _properties.FileAlignment),
                pointerToRelocations: 0,
                relativeVirtualAddress: BitArithmeticUtilities.Align(sizeOfPeHeaders, _properties.SectionAlignment),
                sizeOfRawData: BitArithmeticUtilities.Align(sizeOfTextSection, _properties.FileAlignment),
                virtualSize: sizeOfTextSection
            ));

            int resourcesRva = BitArithmeticUtilities.Align(lastSection.RelativeVirtualAddress + lastSection.VirtualSize, _properties.SectionAlignment);
            int sizeOfWin32Resources = this.ComputeSizeOfWin32Resources(resourcesRva);

            if (sizeOfWin32Resources > 0)
            {
                sectionHeaders.Add(lastSection = new SectionHeader(
                    characteristics: SectionCharacteristics.MemRead |
                                     SectionCharacteristics.ContainsInitializedData,
                    name: ResourceSectionName,
                    numberOfLinenumbers: 0,
                    numberOfRelocations: 0,
                    pointerToLinenumbers: 0,
                    pointerToRawData: lastSection.PointerToRawData + lastSection.SizeOfRawData,
                    pointerToRelocations: 0,
                    relativeVirtualAddress: resourcesRva,
                    sizeOfRawData: BitArithmeticUtilities.Align(sizeOfWin32Resources, _properties.FileAlignment),
                    virtualSize: sizeOfWin32Resources
                ));
            }

            if (_properties.RequiresStartupStub)
            {
                var size = (_properties.Requires64bits && !_properties.RequiresAmdInstructionSet) ? 14 : 12; // TODO: constants

                sectionHeaders.Add(lastSection = new SectionHeader(
                    characteristics: SectionCharacteristics.MemRead |
                                     SectionCharacteristics.MemDiscardable |
                                     SectionCharacteristics.ContainsInitializedData,
                    name: RelocationSectionName,
                    numberOfLinenumbers: 0,
                    numberOfRelocations: 0,
                    pointerToLinenumbers: 0,
                    pointerToRawData: lastSection.PointerToRawData + lastSection.SizeOfRawData,
                    pointerToRelocations: 0,
                    relativeVirtualAddress: BitArithmeticUtilities.Align(lastSection.RelativeVirtualAddress + lastSection.VirtualSize, _properties.SectionAlignment),
                    sizeOfRawData: BitArithmeticUtilities.Align(size, _properties.FileAlignment),
                    virtualSize: size));
            }

            Debug.Assert(sectionHeaders.Count == sectionCount);
            return sectionHeaders;
        }

        private const string CorEntryPointDll = "mscoree.dll";
        private string CorEntryPointName => (_properties.ImageCharacteristics & Characteristics.Dll) != 0 ? "_CorDllMain" : "_CorExeMain";

        private int SizeOfImportAddressTable => _properties.RequiresStartupStub ? (_is32bit ? 2 * sizeof(uint) : 2 * sizeof(ulong)) : 0;

        // (_is32bit ? 66 : 70);
        private int SizeOfImportTable =>
            sizeof(uint) + // RVA
            sizeof(uint) + // 0           
            sizeof(uint) + // 0
            sizeof(uint) + // name RVA
            sizeof(uint) + // import address table RVA
            20 +           // ?
            (_is32bit ? 3 * sizeof(uint) : 2 * sizeof(ulong)) + // import lookup table
            sizeof(ushort) + // hint
            CorEntryPointName.Length + 
            1;    // NUL

        private static int SizeOfNameTable =>
            CorEntryPointDll.Length + 1 + sizeof(ushort);

        private int SizeOfRuntimeStartupStub => _is32bit ? 8 : 16;

        private int CalculateOffsetToMappedFieldDataStream(MetadataSizes metadataSizes)
        {
            int result = ComputeOffsetToImportTable(metadataSizes);

            if (_properties.RequiresStartupStub)
            {
                result += SizeOfImportTable + SizeOfNameTable;
                result = BitArithmeticUtilities.Align(result, _is32bit ? 4 : 8); //optional padding to make startup stub's target address align on word or double word boundary
                result += SizeOfRuntimeStartupStub;
            }

            return result;
        }

        private int CalculateMappedFieldDataStreamRva(int textSectionRva, MetadataSizes metadataSizes)
        {
            return textSectionRva + CalculateOffsetToMappedFieldDataStream(metadataSizes);
        }

        /// <summary>
        /// Compute a deterministic Guid and timestamp based on the contents of the stream, and replace
        /// the 16 zero bytes at the given position and one or two 4-byte values with that computed Guid and timestamp.
        /// </summary>
        /// <param name="peStream">PE stream.</param>
        /// <param name="mvidPosition">Position in the stream of 16 zero bytes to be replaced by a Guid</param>
        /// <param name="ntHeaderTimestampPosition">Position in the stream of four zero bytes to be replaced by a timestamp</param>
        private static void WriteDeterministicGuidAndTimestamps(
            Stream peStream,
            long mvidPosition,
            long ntHeaderTimestampPosition)
        {
            Debug.Assert(mvidPosition != 0);
            Debug.Assert(ntHeaderTimestampPosition != 0);

            var previousPosition = peStream.Position;

            // Compute and write deterministic guid data over the relevant portion of the stream
            peStream.Position = 0;
            var contentId = ContentId.FromHash(CryptographicHashProvider.ComputeSha1(peStream));

            // The existing Guid should be zero.
            CheckZeroDataInStream(peStream, mvidPosition, contentId.Guid.Length);
            peStream.Position = mvidPosition;
            peStream.Write(contentId.Guid, 0, contentId.Guid.Length);

            // The existing timestamp should be zero.
            CheckZeroDataInStream(peStream, ntHeaderTimestampPosition, contentId.Stamp.Length);
            peStream.Position = ntHeaderTimestampPosition;
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

        private int ComputeOffsetToDebugTable(MetadataSizes metadataSizes)
        {
            Debug.Assert(metadataSizes.MetadataSize % 4 == 0);
            Debug.Assert(metadataSizes.ResourceDataSize % 4 == 0);

            return
                ComputeOffsetToMetadata(metadataSizes.ILStreamSize) +
                metadataSizes.MetadataSize +
                metadataSizes.ResourceDataSize +
                metadataSizes.StrongNameSignatureSize;
        }

        private int ComputeOffsetToImportTable(MetadataSizes metadataSizes)
        {
            return
                ComputeOffsetToDebugTable(metadataSizes) +
                ComputeSizeOfDebugDirectory();
        }

        private const int CorHeaderSize =
            sizeof(int) +    // header size
            sizeof(short) +  // major runtime version
            sizeof(short) +  // minor runtime version
            sizeof(long) +   // metadata directory
            sizeof(int) +    // COR flags
            sizeof(int) +    // entry point
            sizeof(long) +   // resources directory
            sizeof(long) +   // strong name signature directory
            sizeof(long) +   // code manager table directory
            sizeof(long) +   // vtable fixups directory
            sizeof(long) +   // export address table jumps directory
            sizeof(long);   // managed-native header directory

        private int OffsetToILStream => SizeOfImportAddressTable + CorHeaderSize; 

        private int ComputeOffsetToMetadata(int ilStreamLength)
        {
            return OffsetToILStream + BitArithmeticUtilities.Align(ilStreamLength, 4);
        }

        /// <summary>
        /// The size of a single entry in the "Debug Directory (Image Only)"
        /// </summary>
        private const int ImageDebugDirectoryEntrySize =
                    sizeof(uint) +   // Characteristics
                    sizeof(uint) +   // TimeDataStamp
                    sizeof(uint) +   // Version
                    sizeof(uint) +   // Type
                    sizeof(uint) +   // SizeOfData
                    sizeof(uint) +   // AddressOfRawData
                    sizeof(uint);    // PointerToRawData

        /// <summary>
        /// The size of our debug directory: one entry for debug information, and an optional second one indicating
        /// that the timestamp is deterministic (i.e. not really a timestamp)
        /// </summary>
        private int ImageDebugDirectoryBaseSize =>
            (_deterministic ? ImageDebugDirectoryEntrySize : 0) +
            (EmitPdb ? ImageDebugDirectoryEntrySize : 0);

        private int ComputeSizeOfDebugDirectoryData()
        {
            // The debug directory data is only needed if this.EmitPdb.
            return (!EmitPdb) ? 0 :
                4 +              // 4B signature "RSDS"
                16 +             // GUID
                sizeof(uint) +   // Age
                Math.Max(BlobUtilities.GetUTF8ByteCount(_pdbPathOpt) + 1, _minPdbPath);
        }

        private int ComputeSizeOfDebugDirectory()
        {
            return ImageDebugDirectoryBaseSize + ComputeSizeOfDebugDirectoryData();
        }

        private int ComputeSizeOfPeHeaders(int sectionCount)
        {
            int sizeOfPeHeaders = 128 + 4 + 20 + 224 + 40 * sectionCount; // TODO: constants
            if (!_is32bit)
            {
                sizeOfPeHeaders += 16;
            }

            return sizeOfPeHeaders;
        }

        private int ComputeSizeOfTextSection(MetadataSizes metadataSizes)
        {
            Debug.Assert(metadataSizes.MappedFieldDataSize % MetadataWriter.MappedFieldDataAlignment == 0);
            return CalculateOffsetToMappedFieldDataStream(metadataSizes) + metadataSizes.MappedFieldDataSize;
        }

        private int ComputeSizeOfWin32Resources(int resourcesRva)
        {
            this.SerializeWin32Resources(resourcesRva);
            int result = 0;
            if (_win32ResourceWriter.Count > 0)
            {
                result += BitArithmeticUtilities.Align(_win32ResourceWriter.Count, 4);
            }            // result += Align(this.win32ResourceWriter.Length+1, 8);

            return result;
        }

        private CorHeader CreateCorHeader(MetadataSizes metadataSizes, int textSectionRva, int entryPointToken)
        {
            int metadataRva = textSectionRva + ComputeOffsetToMetadata(metadataSizes.ILStreamSize);
            int resourcesRva = metadataRva + metadataSizes.MetadataSize;
            int signatureRva = resourcesRva + metadataSizes.ResourceDataSize;

            return new CorHeader(
                entryPointTokenOrRelativeVirtualAddress: entryPointToken,
                flags: _properties.GetCorHeaderFlags(),
                metadataDirectory: new DirectoryEntry(metadataRva, metadataSizes.MetadataSize),
                resourcesDirectory: new DirectoryEntry(resourcesRva, metadataSizes.ResourceDataSize),
                strongNameSignatureDirectory: new DirectoryEntry(signatureRva, metadataSizes.StrongNameSignatureSize));
        }

        private void FillInNtHeader(
            List<SectionHeader> sectionHeaders, 
            int entryPointAddress,
            DirectoryEntry corHeader,
            DirectoryEntry importTable,
            DirectoryEntry importAddressTable,
            DirectoryEntry debugTable,
            out CoffHeader coffHeader,
            out NtHeader ntHeader)
        {
            short sectionCount = (short)sectionHeaders.Count;

            coffHeader = new CoffHeader(
                machine: (_properties.Machine == 0) ? Machine.I386 : _properties.Machine,
                numberOfSections: sectionCount,
                timeDateStamp: _timeStamp,
                pointerToSymbolTable: 0,
                numberOfSymbols: 0,
                sizeOfOptionalHeader: (short)(_is32bit ? 224 : 240), // TODO: constants
                characteristics: _properties.ImageCharacteristics);

            SectionHeader codeSection = sectionHeaders.FirstOrDefault(sh => (sh.Characteristics & SectionCharacteristics.ContainsCode) != 0);
            SectionHeader dataSection = sectionHeaders.FirstOrDefault(sh => (sh.Characteristics & SectionCharacteristics.ContainsInitializedData) != 0);

            ntHeader = new NtHeader();
            ntHeader.Magic = _is32bit ? PEMagic.PE32 : PEMagic.PE32Plus;
            ntHeader.MajorLinkerVersion = _properties.LinkerMajorVersion;
            ntHeader.MinorLinkerVersion = _properties.LinkerMinorVersion;
            ntHeader.AddressOfEntryPoint = entryPointAddress;
            ntHeader.BaseOfCode = codeSection?.RelativeVirtualAddress ?? 0;
            ntHeader.BaseOfData = dataSection?.RelativeVirtualAddress ?? 0;
            ntHeader.ImageBase = _properties.BaseAddress;
            ntHeader.FileAlignment = _properties.FileAlignment;
            ntHeader.MajorSubsystemVersion = _properties.MajorSubsystemVersion;
            ntHeader.MinorSubsystemVersion = _properties.MinorSubsystemVersion;

            ntHeader.Subsystem = _properties.Subsystem;
            ntHeader.DllCharacteristics = _properties.DllCharacteristics;

            ntHeader.SizeOfStackReserve = _properties.SizeOfStackReserve;
            ntHeader.SizeOfStackCommit = _properties.SizeOfStackCommit;
            ntHeader.SizeOfHeapReserve = _properties.SizeOfHeapReserve; 
            ntHeader.SizeOfHeapCommit = _properties.SizeOfHeapCommit;

            ntHeader.SizeOfCode = codeSection?.SizeOfRawData ?? 0;

            ntHeader.SizeOfInitializedData = sectionHeaders.Sum(
                sectionHeader => (sectionHeader.Characteristics & SectionCharacteristics.ContainsInitializedData) != 0 ? sectionHeader.SizeOfRawData : 0);

            ntHeader.SizeOfHeaders = BitArithmeticUtilities.Align(ComputeSizeOfPeHeaders(sectionCount), _properties.FileAlignment);

            var lastSection = sectionHeaders.Last();
            ntHeader.SizeOfImage = BitArithmeticUtilities.Align(lastSection.RelativeVirtualAddress + lastSection.VirtualSize, _properties.SectionAlignment);
            ntHeader.SizeOfUninitializedData = 0;

            ntHeader.ImportAddressTable = importAddressTable;
            ntHeader.CliHeaderTable = corHeader;
            ntHeader.ImportTable = importTable;

            var relocSection = sectionHeaders.FirstOrDefault(sectionHeader => sectionHeader.Name == RelocationSectionName);
            if (relocSection != null)
            {
                ntHeader.BaseRelocationTable = new DirectoryEntry(relocSection.RelativeVirtualAddress, relocSection.VirtualSize);
            }

            ntHeader.DebugTable = debugTable;

            var resourceSection = sectionHeaders.FirstOrDefault(sectionHeader => sectionHeader.Name == ResourceSectionName);
            if (resourceSection != null)
            {
                ntHeader.ResourceTable = new DirectoryEntry(resourceSection.RelativeVirtualAddress, resourceSection.VirtualSize);
            }
        }

        ////
        //// Resource Format.
        ////

        ////
        //// Resource directory consists of two counts, following by a variable length
        //// array of directory entries.  The first count is the number of entries at
        //// beginning of the array that have actual names associated with each entry.
        //// The entries are in ascending order, case insensitive strings.  The second
        //// count is the number of entries that immediately follow the named entries.
        //// This second count identifies the number of entries that have 16-bit integer
        //// Ids as their name.  These entries are also sorted in ascending order.
        ////
        //// This structure allows fast lookup by either name or number, but for any
        //// given resource entry only one form of lookup is supported, not both.
        //// This is consistent with the syntax of the .RC file and the .RES file.
        ////

        //typedef struct _IMAGE_RESOURCE_DIRECTORY {
        //    DWORD   Characteristics;
        //    DWORD   TimeDateStamp;
        //    WORD    MajorVersion;
        //    WORD    MinorVersion;
        //    WORD    NumberOfNamedEntries;
        //    WORD    NumberOfIdEntries;
        ////  IMAGE_RESOURCE_DIRECTORY_ENTRY DirectoryEntries[];
        //} IMAGE_RESOURCE_DIRECTORY, *PIMAGE_RESOURCE_DIRECTORY;

        //#define IMAGE_RESOURCE_NAME_IS_STRING        0x80000000
        //#define IMAGE_RESOURCE_DATA_IS_DIRECTORY     0x80000000
        ////
        //// Each directory contains the 32-bit Name of the entry and an offset,
        //// relative to the beginning of the resource directory of the data associated
        //// with this directory entry.  If the name of the entry is an actual text
        //// string instead of an integer Id, then the high order bit of the name field
        //// is set to one and the low order 31-bits are an offset, relative to the
        //// beginning of the resource directory of the string, which is of type
        //// IMAGE_RESOURCE_DIRECTORY_STRING.  Otherwise the high bit is clear and the
        //// low-order 16-bits are the integer Id that identify this resource directory
        //// entry. If the directory entry is yet another resource directory (i.e. a
        //// subdirectory), then the high order bit of the offset field will be
        //// set to indicate this.  Otherwise the high bit is clear and the offset
        //// field points to a resource data entry.
        ////

        //typedef struct _IMAGE_RESOURCE_DIRECTORY_ENTRY {
        //    union {
        //        struct {
        //            DWORD NameOffset:31;
        //            DWORD NameIsString:1;
        //        } DUMMYSTRUCTNAME;
        //        DWORD   Name;
        //        WORD    Id;
        //    } DUMMYUNIONNAME;
        //    union {
        //        DWORD   OffsetToData;
        //        struct {
        //            DWORD   OffsetToDirectory:31;
        //            DWORD   DataIsDirectory:1;
        //        } DUMMYSTRUCTNAME2;
        //    } DUMMYUNIONNAME2;
        //} IMAGE_RESOURCE_DIRECTORY_ENTRY, *PIMAGE_RESOURCE_DIRECTORY_ENTRY;

        ////
        //// For resource directory entries that have actual string names, the Name
        //// field of the directory entry points to an object of the following type.
        //// All of these string objects are stored together after the last resource
        //// directory entry and before the first resource data object.  This minimizes
        //// the impact of these variable length objects on the alignment of the fixed
        //// size directory entry objects.
        ////

        //typedef struct _IMAGE_RESOURCE_DIRECTORY_STRING {
        //    WORD    Length;
        //    CHAR    NameString[ 1 ];
        //} IMAGE_RESOURCE_DIRECTORY_STRING, *PIMAGE_RESOURCE_DIRECTORY_STRING;


        //typedef struct _IMAGE_RESOURCE_DIR_STRING_U {
        //    WORD    Length;
        //    WCHAR   NameString[ 1 ];
        //} IMAGE_RESOURCE_DIR_STRING_U, *PIMAGE_RESOURCE_DIR_STRING_U;


        ////
        //// Each resource data entry describes a leaf node in the resource directory
        //// tree.  It contains an offset, relative to the beginning of the resource
        //// directory of the data for the resource, a size field that gives the number
        //// of bytes of data at that offset, a CodePage that should be used when
        //// decoding code point values within the resource data.  Typically for new
        //// applications the code page would be the unicode code page.
        ////

        //typedef struct _IMAGE_RESOURCE_DATA_ENTRY {
        //    DWORD   OffsetToData;
        //    DWORD   Size;
        //    DWORD   CodePage;
        //    DWORD   Reserved;
        //} IMAGE_RESOURCE_DATA_ENTRY, *PIMAGE_RESOURCE_DATA_ENTRY;

        private class Directory
        {
            internal readonly string Name;
            internal readonly int ID;
            internal ushort NumberOfNamedEntries;
            internal ushort NumberOfIdEntries;
            internal readonly List<object> Entries;

            internal Directory(string name, int id)
            {
                this.Name = name;
                this.ID = id;
                this.Entries = new List<object>();
            }
        }

        private static int CompareResources(IWin32Resource left, IWin32Resource right)
        {
            int result = CompareResourceIdentifiers(left.TypeId, left.TypeName, right.TypeId, right.TypeName);

            return (result == 0) ? CompareResourceIdentifiers(left.Id, left.Name, right.Id, right.Name) : result;
        }

        //when comparing a string vs ordinal, the string should always be less than the ordinal. Per the spec,
        //entries identified by string must precede those identified by ordinal.
        private static int CompareResourceIdentifiers(int xOrdinal, string xString, int yOrdinal, string yString)
        {
            if (xString == null)
            {
                if (yString == null)
                {
                    return xOrdinal - yOrdinal;
                }
                else
                {
                    return 1;
                }
            }
            else if (yString == null)
            {
                return -1;
            }
            else
            {
                return String.Compare(xString, yString, StringComparison.OrdinalIgnoreCase);
            }
        }

        //sort the resources by ID least to greatest then by NAME.
        //Where strings and ordinals are compared, strings are less than ordinals.
        internal static IEnumerable<IWin32Resource> SortResources(IEnumerable<IWin32Resource> resources)
        {
            return resources.OrderBy(CompareResources);
        }

        //Win32 resources are supplied to the compiler in one of two forms, .RES (the output of the resource compiler),
        //or .OBJ (the output of running cvtres.exe on a .RES file). A .RES file is parsed and processed into
        //a set of objects implementing IWin32Resources. These are then ordered and the final image form is constructed
        //and written to the resource section. Resources in .OBJ form are already very close to their final output
        //form. Rather than reading them and parsing them into a set of objects similar to those produced by 
        //processing a .RES file, we process them like the native linker would, copy the relevant sections from 
        //the .OBJ into our output and apply some fixups.
        private void SerializeWin32Resources(int resourcesRva)
        {
            if (_nativeResourceSectionOpt != null)
            {
                SerializeWin32Resources(_nativeResourceSectionOpt, resourcesRva);
                return;
            }

            if (IteratorHelper.EnumerableIsEmpty(_nativeResourcesOpt))
            {
                return;
            }

            SerializeWin32Resources(_nativeResourcesOpt, resourcesRva);
        }

        private void SerializeWin32Resources(IEnumerable<IWin32Resource> theResources, int resourcesRva)
        {
            theResources = SortResources(theResources);

            Directory typeDirectory = new Directory(string.Empty, 0);
            Directory nameDirectory = null;
            Directory languageDirectory = null;
            int lastTypeID = int.MinValue;
            string lastTypeName = null;
            int lastID = int.MinValue;
            string lastName = null;
            uint sizeOfDirectoryTree = 16;

            //EDMAURER note that this list is assumed to be sorted lowest to highest 
            //first by typeId, then by Id.
            foreach (IWin32Resource r in theResources)
            {
                bool typeDifferent = (r.TypeId < 0 && r.TypeName != lastTypeName) || r.TypeId > lastTypeID;
                if (typeDifferent)
                {
                    lastTypeID = r.TypeId;
                    lastTypeName = r.TypeName;
                    if (lastTypeID < 0)
                    {
                        Debug.Assert(typeDirectory.NumberOfIdEntries == 0, "Not all Win32 resources with types encoded as strings precede those encoded as ints");
                        typeDirectory.NumberOfNamedEntries++;
                    }
                    else
                    {
                        typeDirectory.NumberOfIdEntries++;
                    }

                    sizeOfDirectoryTree += 24;
                    typeDirectory.Entries.Add(nameDirectory = new Directory(lastTypeName, lastTypeID));
                }

                if (typeDifferent || (r.Id < 0 && r.Name != lastName) || r.Id > lastID)
                {
                    lastID = r.Id;
                    lastName = r.Name;
                    if (lastID < 0)
                    {
                        Debug.Assert(nameDirectory.NumberOfIdEntries == 0, "Not all Win32 resources with names encoded as strings precede those encoded as ints");
                        nameDirectory.NumberOfNamedEntries++;
                    }
                    else
                    {
                        nameDirectory.NumberOfIdEntries++;
                    }

                    sizeOfDirectoryTree += 24;
                    nameDirectory.Entries.Add(languageDirectory = new Directory(lastName, lastID));
                }

                languageDirectory.NumberOfIdEntries++;
                sizeOfDirectoryTree += 8;
                languageDirectory.Entries.Add(r);
            }

            var dataWriter = new BlobBuilder();

            //'dataWriter' is where opaque resource data goes as well as strings that are used as type or name identifiers
            this.WriteDirectory(typeDirectory, _win32ResourceWriter, 0, 0, sizeOfDirectoryTree, resourcesRva, dataWriter);
            _win32ResourceWriter.LinkSuffix(dataWriter);
            _win32ResourceWriter.WriteByte(0);
            while ((_win32ResourceWriter.Count % 4) != 0)
            {
                _win32ResourceWriter.WriteByte(0);
            }
        }

        private void WriteDirectory(Directory directory, BlobBuilder writer, uint offset, uint level, uint sizeOfDirectoryTree, int virtualAddressBase, BlobBuilder dataWriter)
        {
            writer.WriteUInt32(0); // Characteristics
            writer.WriteUInt32(0); // Timestamp
            writer.WriteUInt32(0); // Version
            writer.WriteUInt16(directory.NumberOfNamedEntries);
            writer.WriteUInt16(directory.NumberOfIdEntries);
            uint n = (uint)directory.Entries.Count;
            uint k = offset + 16 + n * 8;
            for (int i = 0; i < n; i++)
            {
                int id;
                string name;
                uint nameOffset = (uint)dataWriter.Position + sizeOfDirectoryTree;
                uint directoryOffset = k;
                Directory subDir = directory.Entries[i] as Directory;
                if (subDir != null)
                {
                    id = subDir.ID;
                    name = subDir.Name;
                    if (level == 0)
                    {
                        k += SizeOfDirectory(subDir);
                    }
                    else
                    {
                        k += 16 + 8 * (uint)subDir.Entries.Count;
                    }
                }
                else
                {
                    //EDMAURER write out an IMAGE_RESOURCE_DATA_ENTRY followed
                    //immediately by the data that it refers to. This results
                    //in a layout different than that produced by pulling the resources
                    //from an OBJ. In that case all of the data bits of a resource are
                    //contiguous in .rsrc$02. After processing these will end up at
                    //the end of .rsrc following all of the directory
                    //info and IMAGE_RESOURCE_DATA_ENTRYs
                    IWin32Resource r = (IWin32Resource)directory.Entries[i];
                    id = level == 0 ? r.TypeId : level == 1 ? r.Id : (int)r.LanguageId;
                    name = level == 0 ? r.TypeName : level == 1 ? r.Name : null;
                    dataWriter.WriteUInt32((uint)(virtualAddressBase + sizeOfDirectoryTree + 16 + dataWriter.Position));
                    byte[] data = new List<byte>(r.Data).ToArray();
                    dataWriter.WriteUInt32((uint)data.Length);
                    dataWriter.WriteUInt32(r.CodePage);
                    dataWriter.WriteUInt32(0);
                    dataWriter.WriteBytes(data);
                    while ((dataWriter.Count % 4) != 0)
                    {
                        dataWriter.WriteByte(0);
                    }
                }

                if (id >= 0)
                {
                    writer.WriteInt32(id);
                }
                else
                {
                    if (name == null)
                    {
                        name = string.Empty;
                    }

                    writer.WriteUInt32(nameOffset | 0x80000000);
                    dataWriter.WriteUInt16((ushort)name.Length);
                    dataWriter.WriteUTF16(name);
                }

                if (subDir != null)
                {
                    writer.WriteUInt32(directoryOffset | 0x80000000);
                }
                else
                {
                    writer.WriteUInt32(nameOffset);
                }
            }

            k = offset + 16 + n * 8;
            for (int i = 0; i < n; i++)
            {
                Directory subDir = directory.Entries[i] as Directory;
                if (subDir != null)
                {
                    this.WriteDirectory(subDir, writer, k, level + 1, sizeOfDirectoryTree, virtualAddressBase, dataWriter);
                    if (level == 0)
                    {
                        k += SizeOfDirectory(subDir);
                    }
                    else
                    {
                        k += 16 + 8 * (uint)subDir.Entries.Count;
                    }
                }
            }
        }

        private static uint SizeOfDirectory(Directory/*!*/ directory)
        {
            uint n = (uint)directory.Entries.Count;
            uint size = 16 + 8 * n;
            for (int i = 0; i < n; i++)
            {
                Directory subDir = directory.Entries[i] as Directory;
                if (subDir != null)
                {
                    size += 16 + 8 * (uint)subDir.Entries.Count;
                }
            }

            return size;
        }

        private void SerializeWin32Resources(ResourceSection resourceSections, int resourcesRva)
        {
            var sectionWriter = _win32ResourceWriter.ReserveBytes(resourceSections.SectionBytes.Length);
            sectionWriter.WriteBytes(resourceSections.SectionBytes);

            var readStream = new MemoryStream(resourceSections.SectionBytes);
            var reader = new BinaryReader(readStream);

            foreach (int addressToFixup in resourceSections.Relocations)
            {
                sectionWriter.Offset = addressToFixup;
                reader.BaseStream.Position = addressToFixup;
                sectionWriter.WriteUInt32(reader.ReadUInt32() + (uint)resourcesRva);
            }
        }

        //#define IMAGE_FILE_RELOCS_STRIPPED           0x0001  // Relocation info stripped from file.
        //#define IMAGE_FILE_EXECUTABLE_IMAGE          0x0002  // File is executable  (i.e. no unresolved external references).
        //#define IMAGE_FILE_LINE_NUMS_STRIPPED        0x0004  // Line numbers stripped from file.
        //#define IMAGE_FILE_LOCAL_SYMS_STRIPPED       0x0008  // Local symbols stripped from file.
        //#define IMAGE_FILE_AGGRESIVE_WS_TRIM         0x0010  // Aggressively trim working set
        //#define IMAGE_FILE_LARGE_ADDRESS_AWARE       0x0020  // App can handle >2gb addresses
        //#define IMAGE_FILE_BYTES_REVERSED_LO         0x0080  // Bytes of machine word are reversed.
        //#define IMAGE_FILE_32BIT_MACHINE             0x0100  // 32 bit word machine.
        //#define IMAGE_FILE_DEBUG_STRIPPED            0x0200  // Debugging info stripped from file in .DBG file
        //#define IMAGE_FILE_REMOVABLE_RUN_FROM_SWAP   0x0400  // If Image is on removable media, copy and run from the swap file.
        //#define IMAGE_FILE_NET_RUN_FROM_SWAP         0x0800  // If Image is on Net, copy and run from the swap file.
        //#define IMAGE_FILE_SYSTEM                    0x1000  // System File.
        //#define IMAGE_FILE_DLL                       0x2000  // File is a DLL.
        //#define IMAGE_FILE_UP_SYSTEM_ONLY            0x4000  // File should only be run on a UP machine
        //#define IMAGE_FILE_BYTES_REVERSED_HI         0x8000  // Bytes of machine word are reversed.

        private static readonly byte[] s_dosHeader = new byte[]
        {
            0x4d, 0x5a, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00,
            0x04, 0x00, 0x00, 0x00, 0xff, 0xff, 0x00, 0x00,
            0xb8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x40, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00,
            0x0e, 0x1f, 0xba, 0x0e, 0x00, 0xb4, 0x09, 0xcd,
            0x21, 0xb8, 0x01, 0x4c, 0xcd, 0x21, 0x54, 0x68,
            0x69, 0x73, 0x20, 0x70, 0x72, 0x6f, 0x67, 0x72,
            0x61, 0x6d, 0x20, 0x63, 0x61, 0x6e, 0x6e, 0x6f,
            0x74, 0x20, 0x62, 0x65, 0x20, 0x72, 0x75, 0x6e,
            0x20, 0x69, 0x6e, 0x20, 0x44, 0x4f, 0x53, 0x20,
            0x6d, 0x6f, 0x64, 0x65, 0x2e, 0x0d, 0x0d, 0x0a,
            0x24, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };

        private void WriteHeaders(Stream peStream, NtHeader ntHeader, CoffHeader coffHeader, List<SectionHeader> sectionHeaders, out long ntHeaderTimestampPosition)
        {
            var writer = PooledBlobBuilder.GetInstance();

            // MS-DOS stub (128 bytes)
            writer.WriteBytes(s_dosHeader);

            // PE Signature "PE\0\0" 
            writer.WriteUInt32(0x00004550);

            // COFF Header (20 bytes)
            writer.WriteUInt16((ushort)coffHeader.Machine);
            writer.WriteUInt16((ushort)coffHeader.NumberOfSections);
            ntHeaderTimestampPosition = writer.Position + peStream.Position;
            writer.WriteUInt32((uint)coffHeader.TimeDateStamp);
            writer.WriteUInt32((uint)coffHeader.PointerToSymbolTable);
            writer.WriteUInt32((uint)coffHeader.NumberOfSymbols);
            writer.WriteUInt16((ushort)(_is32bit ? 224 : 240)); // SizeOfOptionalHeader
            writer.WriteUInt16((ushort)coffHeader.Characteristics);

            // PE Headers:
            writer.WriteUInt16((ushort)(_is32bit ? PEMagic.PE32 : PEMagic.PE32Plus)); // 2
            writer.WriteByte(ntHeader.MajorLinkerVersion); // 3
            writer.WriteByte(ntHeader.MinorLinkerVersion); // 4
            writer.WriteUInt32((uint)ntHeader.SizeOfCode); // 8
            writer.WriteUInt32((uint)ntHeader.SizeOfInitializedData); // 12
            writer.WriteUInt32((uint)ntHeader.SizeOfUninitializedData); // 16
            writer.WriteUInt32((uint)ntHeader.AddressOfEntryPoint); // 20
            writer.WriteUInt32((uint)ntHeader.BaseOfCode); // 24

            if (_is32bit)
            {
                writer.WriteUInt32((uint)ntHeader.BaseOfData); // 28
                writer.WriteUInt32((uint)ntHeader.ImageBase); // 32
            }
            else
            {
                writer.WriteUInt64(ntHeader.ImageBase); // 32
            }

            // NT additional fields:
            writer.WriteUInt32((uint)ntHeader.SectionAlignment); // 36
            writer.WriteUInt32((uint)ntHeader.FileAlignment); // 40
            writer.WriteUInt16(ntHeader.MajorOperatingSystemVersion); // 42
            writer.WriteUInt16(ntHeader.MinorOperatingSystemVersion); // 44
            writer.WriteUInt16(ntHeader.MajorImageVersion); // 46
            writer.WriteUInt16(ntHeader.MinorImageVersion); // 48
            writer.WriteUInt16(ntHeader.MajorSubsystemVersion); // MajorSubsystemVersion 50
            writer.WriteUInt16(ntHeader.MinorSubsystemVersion); // MinorSubsystemVersion 52

            // Win32VersionValue (reserved, should be 0)
            writer.WriteUInt32(0); // 56

            writer.WriteUInt32((uint)ntHeader.SizeOfImage); // 60
            writer.WriteUInt32((uint)ntHeader.SizeOfHeaders); // 64
            writer.WriteUInt32(ntHeader.Checksum); // 68            
            writer.WriteUInt16((ushort)ntHeader.Subsystem); // 70
            writer.WriteUInt16((ushort)ntHeader.DllCharacteristics);

            if (_is32bit)
            {
                writer.WriteUInt32((uint)ntHeader.SizeOfStackReserve); // 76
                writer.WriteUInt32((uint)ntHeader.SizeOfStackCommit); // 80
                writer.WriteUInt32((uint)ntHeader.SizeOfHeapReserve); // 84
                writer.WriteUInt32((uint)ntHeader.SizeOfHeapCommit); // 88
            }
            else
            {
                writer.WriteUInt64(ntHeader.SizeOfStackReserve); // 80
                writer.WriteUInt64(ntHeader.SizeOfStackCommit); // 88
                writer.WriteUInt64(ntHeader.SizeOfHeapReserve); // 96
                writer.WriteUInt64(ntHeader.SizeOfHeapCommit); // 104
            }

            // LoaderFlags
            writer.WriteUInt32(0); // 92|108

            // The number of data-directory entries in the remainder of the header.
            writer.WriteUInt32(16); //  96|112

            // directory entries:
            writer.WriteUInt32((uint)ntHeader.ExportTable.RelativeVirtualAddress); // 100|116
            writer.WriteUInt32((uint)ntHeader.ExportTable.Size); // 104|120
            writer.WriteUInt32((uint)ntHeader.ImportTable.RelativeVirtualAddress); // 108|124
            writer.WriteUInt32((uint)ntHeader.ImportTable.Size); // 112|128
            writer.WriteUInt32((uint)ntHeader.ResourceTable.RelativeVirtualAddress); // 116|132
            writer.WriteUInt32((uint)ntHeader.ResourceTable.Size); // 120|136
            writer.WriteUInt32((uint)ntHeader.ExceptionTable.RelativeVirtualAddress); // 124|140
            writer.WriteUInt32((uint)ntHeader.ExceptionTable.Size); // 128|144
            writer.WriteUInt32((uint)ntHeader.CertificateTable.RelativeVirtualAddress); // 132|148
            writer.WriteUInt32((uint)ntHeader.CertificateTable.Size); // 136|152
            writer.WriteUInt32((uint)ntHeader.BaseRelocationTable.RelativeVirtualAddress); // 140|156
            writer.WriteUInt32((uint)ntHeader.BaseRelocationTable.Size); // 144|160
            writer.WriteUInt32((uint)ntHeader.DebugTable.RelativeVirtualAddress); // 148|164
            writer.WriteUInt32((uint)ntHeader.DebugTable.Size); // 152|168
            writer.WriteUInt32((uint)ntHeader.CopyrightTable.RelativeVirtualAddress); // 156|172
            writer.WriteUInt32((uint)ntHeader.CopyrightTable.Size); // 160|176
            writer.WriteUInt32((uint)ntHeader.GlobalPointerTable.RelativeVirtualAddress); // 164|180
            writer.WriteUInt32((uint)ntHeader.GlobalPointerTable.Size); // 168|184
            writer.WriteUInt32((uint)ntHeader.ThreadLocalStorageTable.RelativeVirtualAddress); // 172|188
            writer.WriteUInt32((uint)ntHeader.ThreadLocalStorageTable.Size); // 176|192
            writer.WriteUInt32((uint)ntHeader.LoadConfigTable.RelativeVirtualAddress); // 180|196
            writer.WriteUInt32((uint)ntHeader.LoadConfigTable.Size); // 184|200
            writer.WriteUInt32((uint)ntHeader.BoundImportTable.RelativeVirtualAddress); // 188|204
            writer.WriteUInt32((uint)ntHeader.BoundImportTable.Size); // 192|208
            writer.WriteUInt32((uint)ntHeader.ImportAddressTable.RelativeVirtualAddress); // 196|212
            writer.WriteUInt32((uint)ntHeader.ImportAddressTable.Size); // 200|216
            writer.WriteUInt32((uint)ntHeader.DelayImportTable.RelativeVirtualAddress); // 204|220
            writer.WriteUInt32((uint)ntHeader.DelayImportTable.Size); // 208|224
            writer.WriteUInt32((uint)ntHeader.CliHeaderTable.RelativeVirtualAddress); // 212|228
            writer.WriteUInt32((uint)ntHeader.CliHeaderTable.Size); // 216|232
            writer.WriteUInt64(0); // 224|240

            // Section Headers
            foreach (var sectionHeader in sectionHeaders)
            {
                WriteSectionHeader(sectionHeader, writer);
            }

            writer.WriteContentTo(peStream);
            writer.Free();
        }

        private static void WriteSectionHeader(SectionHeader sectionHeader, BlobBuilder writer)
        {
            if (sectionHeader.VirtualSize == 0)
            {
                return;
            }

            for (int j = 0, m = sectionHeader.Name.Length; j < 8; j++)
            {
                if (j < m)
                {
                    writer.WriteByte((byte)sectionHeader.Name[j]);
                }
                else
                {
                    writer.WriteByte(0);
                }
            }

            writer.WriteUInt32((uint)sectionHeader.VirtualSize);
            writer.WriteUInt32((uint)sectionHeader.RelativeVirtualAddress);
            writer.WriteUInt32((uint)sectionHeader.SizeOfRawData);
            writer.WriteUInt32((uint)sectionHeader.PointerToRawData);
            writer.WriteUInt32((uint)sectionHeader.PointerToRelocations);
            writer.WriteUInt32((uint)sectionHeader.PointerToLinenumbers);
            writer.WriteUInt16(sectionHeader.NumberOfRelocations);
            writer.WriteUInt16(sectionHeader.NumberOfLinenumbers);
            writer.WriteUInt32((uint)sectionHeader.Characteristics);
        }

        private void WriteTextSection(
            Stream peStream,
            SectionHeader textSection,
            int importTableRva,
            int importAddressTableRva,
            int entryPointToken,
            BlobBuilder metadataWriter,
            BlobBuilder ilWriter,
            BlobBuilder mappedFieldDataWriter,
            BlobBuilder managedResourceWriter,
            MetadataSizes metadataSizes,
            ContentId nativePdbContentId,
            ContentId portablePdbContentId,
            out long metadataPosition)
        {
            // TODO: zero out all bytes:
            peStream.Position = textSection.PointerToRawData;

            if (_properties.RequiresStartupStub)
            {
                WriteImportAddressTable(peStream, importTableRva);
            }

            var corHeader = CreateCorHeader(metadataSizes, textSection.RelativeVirtualAddress, entryPointToken);
            WriteCorHeader(peStream, corHeader);

            // IL:
            ilWriter.Align(4);
            ilWriter.WriteContentTo(peStream);

            // metadata:
            metadataPosition = peStream.Position;
            Debug.Assert(metadataWriter.Count % 4 == 0);
            metadataWriter.WriteContentTo(peStream);

            // managed resources:
            Debug.Assert(managedResourceWriter.Count % 4 == 0);
            managedResourceWriter.WriteContentTo(peStream);

            // strong name signature:
            WriteSpaceForHash(peStream, metadataSizes.StrongNameSignatureSize);

            if (EmitPdb || _deterministic)
            {
                WriteDebugTable(peStream, textSection, nativePdbContentId, portablePdbContentId, metadataSizes);
            }

            if (_properties.RequiresStartupStub)
            {
                WriteImportTable(peStream, importTableRva, importAddressTableRva);
                WriteNameTable(peStream);
                WriteRuntimeStartupStub(peStream, importAddressTableRva);
            }

            // mapped field data:            
            mappedFieldDataWriter.WriteContentTo(peStream);

            // TODO: zero out all bytes:
            int alignedPosition = textSection.PointerToRawData + textSection.SizeOfRawData;
            if (peStream.Position != alignedPosition)
            {
                peStream.Position = alignedPosition - 1;
                peStream.WriteByte(0);
            }
        }

        private void WriteImportAddressTable(Stream peStream, int importTableRva)
        {
            var writer = new BlobBuilder(SizeOfImportAddressTable);
            int ilRVA = importTableRva + 40;
            int hintRva = ilRVA + (_is32bit ? 12 : 16);

            // Import Address Table
            if (_is32bit)
            {
                writer.WriteUInt32((uint)hintRva); // 4
                writer.WriteUInt32(0); // 8
            }
            else
            {
                writer.WriteUInt64((uint)hintRva); // 8
                writer.WriteUInt64(0); // 16
            }

            Debug.Assert(writer.Count == SizeOfImportAddressTable);
            writer.WriteContentTo(peStream);
        }

        private void WriteImportTable(Stream peStream, int importTableRva, int importAddressTableRva)
        {
            var writer = new BlobBuilder(SizeOfImportTable);
            int ilRVA = importTableRva + 40;
            int hintRva = ilRVA + (_is32bit ? 12 : 16);
            int nameRva = hintRva + 12 + 2;

            // Import table
            writer.WriteUInt32((uint)ilRVA); // 4
            writer.WriteUInt32(0); // 8
            writer.WriteUInt32(0); // 12
            writer.WriteUInt32((uint)nameRva); // 16
            writer.WriteUInt32((uint)importAddressTableRva); // 20
            writer.WriteBytes(0, 20); // 40

            // Import Lookup table
            if (_is32bit)
            {
                writer.WriteUInt32((uint)hintRva); // 44
                writer.WriteUInt32(0); // 48
                writer.WriteUInt32(0); // 52
            }
            else
            {
                writer.WriteUInt64((uint)hintRva); // 48
                writer.WriteUInt64(0); // 56
            }

            // Hint table
            writer.WriteUInt16(0); // Hint 54|58

            foreach (char ch in CorEntryPointName)
            {
                writer.WriteByte((byte)ch); // 65|69
            }

            writer.WriteByte(0); // 66|70
            Debug.Assert(writer.Count == SizeOfImportTable);

            writer.WriteContentTo(peStream);
        }

        private static void WriteNameTable(Stream peStream)
        {
            var writer = new BlobBuilder(SizeOfNameTable);
            foreach (char ch in CorEntryPointDll)
            {
                writer.WriteByte((byte)ch);
            }

            writer.WriteByte(0);
            writer.WriteUInt16(0);
            Debug.Assert(writer.Count == SizeOfNameTable);

            writer.WriteContentTo(peStream);
        }

        private static void WriteCorHeader(Stream peStream, CorHeader corHeader)
        {
            var writer = new BlobBuilder(CorHeaderSize);
            writer.WriteUInt32(CorHeaderSize);
            writer.WriteUInt16(corHeader.MajorRuntimeVersion);
            writer.WriteUInt16(corHeader.MinorRuntimeVersion); 
            writer.WriteUInt32((uint)corHeader.MetadataDirectory.RelativeVirtualAddress); 
            writer.WriteUInt32((uint)corHeader.MetadataDirectory.Size);
            writer.WriteUInt32((uint)corHeader.Flags);
            writer.WriteUInt32((uint)corHeader.EntryPointTokenOrRelativeVirtualAddress);
            writer.WriteUInt32((uint)(corHeader.ResourcesDirectory.Size == 0 ? 0 : corHeader.ResourcesDirectory.RelativeVirtualAddress)); // 28
            writer.WriteUInt32((uint)corHeader.ResourcesDirectory.Size);
            writer.WriteUInt32((uint)(corHeader.StrongNameSignatureDirectory.Size == 0 ? 0 : corHeader.StrongNameSignatureDirectory.RelativeVirtualAddress)); // 36
            writer.WriteUInt32((uint)corHeader.StrongNameSignatureDirectory.Size);
            writer.WriteUInt32((uint)corHeader.CodeManagerTableDirectory.RelativeVirtualAddress); 
            writer.WriteUInt32((uint)corHeader.CodeManagerTableDirectory.Size); 
            writer.WriteUInt32((uint)corHeader.VtableFixupsDirectory.RelativeVirtualAddress); 
            writer.WriteUInt32((uint)corHeader.VtableFixupsDirectory.Size); 
            writer.WriteUInt32((uint)corHeader.ExportAddressTableJumpsDirectory.RelativeVirtualAddress);
            writer.WriteUInt32((uint)corHeader.ExportAddressTableJumpsDirectory.Size);
            writer.WriteUInt64(0);
            Debug.Assert(writer.Count == CorHeaderSize);
            Debug.Assert(writer.Count % 4 == 0);

            writer.WriteContentTo(peStream);
        }

        private static void WriteSpaceForHash(Stream peStream, int strongNameSignatureSize)
        {
            while (strongNameSignatureSize > 0)
            {
                peStream.WriteByte(0);
                strongNameSignatureSize--;
            }
        }

        /// <summary>
        /// Write one entry in the "Debug Directory (Image Only)"
        /// See https://msdn.microsoft.com/en-us/windows/hardware/gg463119.aspx
        /// section 5.1.1 (pages 71-72).
        /// </summary>
        private static void WriteDebugTableEntry(
            PooledBlobBuilder writer,
            byte[] stamp,
            uint version, // major and minor version, combined
            uint debugType,
            uint sizeOfData,
            uint addressOfRawData,
            uint pointerToRawData
            )
        {
            writer.WriteUInt32(0); // characteristics
            Debug.Assert(stamp.Length == 4);
            writer.WriteBytes(stamp);
            writer.WriteUInt32(version);
            writer.WriteUInt32(debugType);
            writer.WriteUInt32(sizeOfData);
            writer.WriteUInt32(addressOfRawData);
            writer.WriteUInt32(pointerToRawData);
        }

        private readonly static byte[] zeroStamp = new byte[4]; // four bytes of zero

        /// <summary>
        /// Write the entire "Debug Directory (Image Only)" along with data that it points to.
        /// </summary>
        private void WriteDebugTable(Stream peStream, SectionHeader textSection, ContentId nativePdbContentId, ContentId portablePdbContentId, MetadataSizes metadataSizes)
        {
            int tableSize = ImageDebugDirectoryBaseSize;
            Debug.Assert(tableSize != 0);
            Debug.Assert(nativePdbContentId.IsDefault || portablePdbContentId.IsDefault);
            Debug.Assert(!EmitPdb || (nativePdbContentId.IsDefault ^ portablePdbContentId.IsDefault));

            var writer = PooledBlobBuilder.GetInstance();

            int dataSize = ComputeSizeOfDebugDirectoryData();
            if (this.EmitPdb)
            {
                const int IMAGE_DEBUG_TYPE_CODEVIEW = 2; // from PE spec
                uint dataOffset = (uint)(ComputeOffsetToDebugTable(metadataSizes) + tableSize);
                WriteDebugTableEntry(writer,
                    stamp: nativePdbContentId.Stamp ?? portablePdbContentId.Stamp,
                    version: portablePdbContentId.IsDefault ? (uint)0 : ('P' << 24 | 'M' << 16 | 0x01 << 8 | 0x00),
                    debugType: IMAGE_DEBUG_TYPE_CODEVIEW,
                    sizeOfData: (uint)dataSize,
                    addressOfRawData: (uint)textSection.RelativeVirtualAddress + dataOffset, // RVA of the data
                    pointerToRawData: (uint)textSection.PointerToRawData + dataOffset); // position of the data in the PE stream
            }

            if (this._deterministic)
            {
                const int IMAGE_DEBUG_TYPE_NO_TIMESTAMP = 16; // from PE spec
                WriteDebugTableEntry(writer,
                    stamp: zeroStamp,
                    version: 0,
                    debugType: IMAGE_DEBUG_TYPE_NO_TIMESTAMP,
                    sizeOfData: 0,
                    addressOfRawData: 0,
                    pointerToRawData: 0);
            }

            // We should now have written all and precisely the data we said we'd write for the table entries.
            Debug.Assert(writer.Count == tableSize);

            // ====================
            // The following is additional data beyond the debug directory at the offset `dataOffset`
            // pointed to by the ImageDebugTypeCodeView entry.

            if (EmitPdb)
            {
                writer.WriteByte((byte)'R');
                writer.WriteByte((byte)'S');
                writer.WriteByte((byte)'D');
                writer.WriteByte((byte)'S');

                // PDB id:
                writer.WriteBytes(nativePdbContentId.Guid ?? portablePdbContentId.Guid);

                // age
                writer.WriteUInt32(PdbWriter.Age);

                // UTF-8 encoded zero-terminated path to PDB
                int pathStart = writer.Position;
                writer.WriteUTF8(_pdbPathOpt, allowUnpairedSurrogates: true);
                writer.WriteByte(0);

                // padding:
                writer.WriteBytes(0, Math.Max(0, _minPdbPath - (writer.Position - pathStart)));
            }

            // We should now have written all and precisely the data we said we'd write for the table and its data.
            Debug.Assert(writer.Count == tableSize + dataSize);

            writer.WriteContentTo(peStream);
            writer.Free();
        }

        private void WriteRuntimeStartupStub(Stream peStream, int importAddressTableRva)
        {
            var writer = new BlobBuilder(16);
            // entry point code, consisting of a jump indirect to _CorXXXMain
            if (_is32bit)
            {
                //emit 0's (nops) to pad the entry point code so that the target address is aligned on a 4 byte boundary.
                for (uint i = 0, n = (uint)(BitArithmeticUtilities.Align((uint)peStream.Position, 4) - peStream.Position); i < n; i++)
                {
                    writer.WriteByte(0);
                }

                writer.WriteUInt16(0);
                writer.WriteByte(0xff);
                writer.WriteByte(0x25); //4
                writer.WriteUInt32((uint)importAddressTableRva + (uint)_properties.BaseAddress); //8
            }
            else
            {
                //emit 0's (nops) to pad the entry point code so that the target address is aligned on a 8 byte boundary.
                for (uint i = 0, n = (uint)(BitArithmeticUtilities.Align((uint)peStream.Position, 8) - peStream.Position); i < n; i++)
                {
                    writer.WriteByte(0);
                }

                writer.WriteUInt32(0);
                writer.WriteUInt16(0);
                writer.WriteByte(0xff);
                writer.WriteByte(0x25); //8
                writer.WriteUInt64((ulong)importAddressTableRva + _properties.BaseAddress); //16
            }

            writer.WriteContentTo(peStream);
        }

        private void WriteRelocSection(Stream peStream, SectionHeader relocSection, int entryPointAddress)
        {
            peStream.Position = relocSection.PointerToRawData;
            var writer = new BlobBuilder(relocSection.SizeOfRawData);
            writer.WriteUInt32((((uint)entryPointAddress + 2) / 0x1000) * 0x1000);
            writer.WriteUInt32(_properties.Requires64bits && !_properties.RequiresAmdInstructionSet ? 14u : 12u);
            uint offsetWithinPage = ((uint)entryPointAddress + 2) % 0x1000;
            uint relocType = _properties.Requires64bits ? 10u : 3u;
            ushort s = (ushort)((relocType << 12) | offsetWithinPage);
            writer.WriteUInt16(s);
            if (_properties.Requires64bits && !_properties.RequiresAmdInstructionSet)
            {
                writer.WriteUInt32(relocType << 12);
            }

            writer.WriteUInt16(0); // next chunk's RVA
            writer.PadTo(relocSection.SizeOfRawData);
            writer.WriteContentTo(peStream);
        }

        private void WriteResourceSection(Stream peStream, SectionHeader resourceSection)
        {
            peStream.Position = resourceSection.PointerToRawData;
            _win32ResourceWriter.PadTo(resourceSection.SizeOfRawData);
            _win32ResourceWriter.WriteContentTo(peStream);
        }
    }
}
