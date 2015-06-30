// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        /// <summary>
        /// True if we should attempt to generate a deterministic output (no timestamps or random data).
        /// </summary>
        private readonly bool _deterministic;

        private readonly IModule _module;
        private readonly string _pdbPathOpt;
        private readonly bool _emitRuntimeStartupStub;
        private readonly int _sizeOfImportAddressTable;

        private MemoryStream _headerStream = new MemoryStream(1024);

        private readonly MemoryStream _emptyStream = new MemoryStream(0);

        private readonly NtHeader _ntHeader = new NtHeader();

        private readonly BinaryWriter _rdataWriter = new BinaryWriter(new MemoryStream());
        private readonly BinaryWriter _sdataWriter = new BinaryWriter(new MemoryStream());
        private readonly BinaryWriter _tlsDataWriter = new BinaryWriter(new MemoryStream());
        private readonly BinaryWriter _win32ResourceWriter = new BinaryWriter(new MemoryStream(1024));
        private readonly BinaryWriter _coverageDataWriter = new BinaryWriter(new MemoryStream());

        private SectionHeader _coverSection;
        private SectionHeader _relocSection;
        private SectionHeader _resourceSection;
        private SectionHeader _rdataSection;
        private SectionHeader _sdataSection;
        private SectionHeader _textSection;
        private SectionHeader _tlsSection;

        private PeWriter(IModule module, string pdbPathOpt, bool deterministic)
        {
            _module = module;
            _emitRuntimeStartupStub = module.RequiresStartupStub;
            _pdbPathOpt = pdbPathOpt;
            _deterministic = deterministic;
            _sizeOfImportAddressTable = _emitRuntimeStartupStub ? (!_module.Requires64bits ? 8 : 16) : 0;
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

            var peWriter = new PeWriter(context.Module, pdbPathOpt, deterministic);
            var mdWriter = FullMetadataWriter.Create(context, messageProvider, allowMissingMethodBodies, deterministic, getPortablePdbStreamOpt != null, cancellationToken);

            return peWriter.WritePeToStream(mdWriter, getPeStream, getPortablePdbStreamOpt, nativePdbWriterOpt);
        }

        private bool WritePeToStream(MetadataWriter mdWriter, Func<Stream> getPeStream, Func<Stream> getPortablePdbStreamOpt, PdbWriter nativePdbWriterOpt)
        {
            // TODO: we can precalculate the exact size of IL stream
            var ilBuffer = new MemoryStream(32 * 1024);
            var ilWriter = new BinaryWriter(ilBuffer);
            var metadataBuffer = new MemoryStream(16 * 1024);
            var metadataWriter = new BinaryWriter(metadataBuffer);
            var mappedFieldDataBuffer = new MemoryStream();
            var mappedFieldDataWriter = new BinaryWriter(mappedFieldDataBuffer);
            var managedResourceBuffer = new MemoryStream(1024);
            var managedResourceWriter = new BinaryWriter(managedResourceBuffer);

            var debugMetadataBuffer = (getPortablePdbStreamOpt != null) ? new MemoryStream(16 * 1024) : null;
            var debugMetadataWriterOpt = new BinaryWriter(debugMetadataBuffer);

            nativePdbWriterOpt?.SetMetadataEmitter(mdWriter);

            // Since we are producing a full assembly, we should not have a module version ID
            // imposed ahead-of time. Instead we will compute a deterministic module version ID
            // based on the contents of the generated stream.
            Debug.Assert(_module.PersistentIdentifier == default(Guid));

            uint moduleVersionIdOffsetInMetadataStream;
            var calculateMethodBodyStreamRva = new Func<MetadataSizes, int>(mdSizes =>
            {
                FillInTextSectionHeader(mdSizes);
                return (int)_textSection.RelativeVirtualAddress + _sizeOfImportAddressTable + 72;
            });

            uint entryPointToken;
            MetadataSizes metadataSizes;
            mdWriter.SerializeMetadataAndIL(
                metadataWriter,
                debugMetadataWriterOpt,
                nativePdbWriterOpt,
                ilWriter,
                mappedFieldDataWriter,
                managedResourceWriter,
                calculateMethodBodyStreamRva,
                CalculateMappedFieldDataStreamRva,
                out moduleVersionIdOffsetInMetadataStream,
                out entryPointToken,
                out metadataSizes);

            ContentId nativePdbContentId;
            if (nativePdbWriterOpt != null)
            {
                if (entryPointToken != 0)
                {
                    nativePdbWriterOpt.SetEntryPoint(entryPointToken);
                }

                var assembly = _module.AsAssembly;
                if (assembly != null && assembly.Kind == ModuleKind.WindowsRuntimeMetadata)
                {
                    // Dev12: If compiling to winmdobj, we need to add to PDB source spans of
                    //        all types and members for better error reporting by WinMDExp.
                    nativePdbWriterOpt.WriteDefinitionLocations(_module.GetSymbolToLocationMap());
                }
                else
                {
#if DEBUG
                    // validate that all definitions are writable
                    // if same scenario would happen in an winmdobj project
                    nativePdbWriterOpt.AssertAllDefinitionsHaveTokens(_module.GetSymbolToLocationMap());
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

            FillInSectionHeaders();

            // fill in header fields.
            FillInNtHeader(metadataSizes, CalculateMappedFieldDataStreamRva(metadataSizes));
            var corHeader = CreateCorHeader(metadataSizes, entryPointToken);

            // write to PE stream.
            Stream peStream = getPeStream();
            if (peStream == null)
            {
                return false;
            }

            long ntHeaderTimestampPosition;
            long metadataPosition;

            WriteHeaders(peStream, out ntHeaderTimestampPosition);

            WriteTextSection(
                peStream,
                corHeader,
                metadataBuffer,
                ilBuffer,
                mappedFieldDataBuffer,
                managedResourceBuffer,
                metadataSizes,
                nativePdbContentId,
                out metadataPosition);

            WriteRdataSection(peStream);
            WriteSdataSection(peStream);
            WriteCoverSection(peStream);
            WriteTlsSection(peStream);
            WriteResourceSection(peStream);
            WriteRelocSection(peStream);

            if (_deterministic)
            {
                var mvidPosition = metadataPosition + moduleVersionIdOffsetInMetadataStream;
                WriteDeterministicGuidAndTimestamps(peStream, mvidPosition, ntHeaderTimestampPosition);
            }

            return true;
        }

        private int CalculateMappedFieldDataStreamRva(MetadataSizes metadataSizes)
        {
            FillInTextSectionHeader(metadataSizes);

            Debug.Assert(metadataSizes.MappedFieldDataSize % MetadataWriter.MappedFieldDataAlignment == 0);
            return (int)(_textSection.RelativeVirtualAddress + _textSection.VirtualSize - metadataSizes.MappedFieldDataSize);
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

        private int ComputeStrongNameSignatureSize()
        {
            IAssembly assembly = _module.AsAssembly;
            if (assembly == null)
            {
                return 0;
            }

            // EDMAURER the count of characters divided by two because the each pair of characters will turn in to one byte.
            int keySize = (assembly.SignatureKey == null) ? 0 : assembly.SignatureKey.Length / 2;

            if (keySize == 0)
            {
                keySize = assembly.PublicKey.Length;
            }

            if (keySize == 0)
            {
                return 0;
            }

            return (keySize < 128 + 32) ? 128 : keySize - 32;
        }

        private int ComputeOffsetToDebugTable(MetadataSizes metadataSizes)
        {
            return
                ComputeOffsetToMetadata(metadataSizes.ILStreamSize) +
                metadataSizes.MetadataSize +
                metadataSizes.ResourceDataSize +
                ComputeStrongNameSignatureSize(); // size of strong name hash
        }

        private int ComputeOffsetToImportTable(MetadataSizes metadataSizes)
        {
            // TODO: add size of unmanaged export stubs (when and if these are ever supported).
            return
                ComputeOffsetToDebugTable(metadataSizes) +
                ComputeSizeOfDebugDirectory();
        }

        private int ComputeOffsetToMetadata(int ilStreamLength)
        {
            return
                _sizeOfImportAddressTable +
                72 + // size of CLR header
                BitArithmeticUtilities.Align(ilStreamLength, 4);
        }

        private const int ImageDebugDirectoryBaseSize =
            sizeof(uint) +   // Characteristics
            sizeof(uint) +   // TimeDataStamp
            sizeof(uint) +   // Version
            sizeof(uint) +   // Type
            sizeof(uint) +   // SizeOfData
            sizeof(uint) +   // AddressOfRawData
            sizeof(uint);    // PointerToRawData

        private int ComputeSizeOfDebugDirectoryData()
        {
            return
                4 +              // 4B signature "RSDS"
                16 +             // GUID
                sizeof(uint) +   // Age
                Encoding.UTF8.GetByteCount(_pdbPathOpt) +
                1;               // Null terminator
        }

        private int ComputeSizeOfDebugDirectory()
        {
            return EmitPdb ? ImageDebugDirectoryBaseSize + ComputeSizeOfDebugDirectoryData() : 0;
        }

        private uint ComputeSizeOfPeHeaders()
        {
            ushort numberOfSections = 1; // .text 
            if (_emitRuntimeStartupStub) numberOfSections++; //.reloc
            if (_tlsDataWriter.BaseStream.Length > 0) numberOfSections++; //.tls
            if (_rdataWriter.BaseStream.Length > 0) numberOfSections++; //.rdata
            if (_sdataWriter.BaseStream.Length > 0) numberOfSections++; //.sdata
            if (_coverageDataWriter.BaseStream.Length > 0) numberOfSections++; //.cover
            if (!IteratorHelper.EnumerableIsEmpty(_module.Win32Resources) ||
                _module.Win32ResourceSection != null)
                numberOfSections++; //.rsrc;

            _ntHeader.NumberOfSections = numberOfSections;
            uint sizeOfPeHeaders = 128 + 4 + 20 + 224 + 40u * numberOfSections;
            if (_module.Requires64bits)
            {
                sizeOfPeHeaders += 16;
            }

            return sizeOfPeHeaders;
        }

        private int ComputeSizeOfTextSection(MetadataSizes metadataSizes)
        {
            int textSectionLength = this.ComputeOffsetToImportTable(metadataSizes);

            if (_emitRuntimeStartupStub)
            {
                textSectionLength += !_module.Requires64bits ? 66 : 70; //size of import table
                textSectionLength += 14; //size of name table
                textSectionLength = BitArithmeticUtilities.Align(textSectionLength, !_module.Requires64bits ? 4 : 8); //optional padding to make startup stub's target address align on word or double word boundary
                textSectionLength += !_module.Requires64bits ? 8 : 16; //fixed size of runtime startup stub
            }

            Debug.Assert(metadataSizes.MappedFieldDataSize % MetadataWriter.MappedFieldDataAlignment == 0);
            textSectionLength += metadataSizes.MappedFieldDataSize;
            return textSectionLength;
        }

        private uint ComputeSizeOfWin32Resources(uint resourcesRva)
        {
            this.SerializeWin32Resources(resourcesRva);
            uint result = 0;
            if (_win32ResourceWriter.BaseStream.Length > 0)
            {
                result += BitArithmeticUtilities.Align(_win32ResourceWriter.BaseStream.Length, 4);
            }            // result += Align(this.win32ResourceWriter.BaseStream.Length+1, 8);

            return result;
        }

        private CorHeader CreateCorHeader(MetadataSizes metadataSizes, uint entryPointToken)
        {
            CorHeader corHeader = new CorHeader();
            corHeader.CodeManagerTable.RelativeVirtualAddress = 0;
            corHeader.CodeManagerTable.Size = 0;
            corHeader.EntryPointToken = entryPointToken;
            corHeader.ExportAddressTableJumps.RelativeVirtualAddress = 0;
            corHeader.ExportAddressTableJumps.Size = 0;
            corHeader.Flags = this.GetCorHeaderFlags();
            corHeader.MajorRuntimeVersion = 2;
            corHeader.MetadataDirectory.RelativeVirtualAddress = _textSection.RelativeVirtualAddress + (uint)ComputeOffsetToMetadata(metadataSizes.ILStreamSize);
            corHeader.MetadataDirectory.Size = (uint)metadataSizes.MetadataSize;
            corHeader.MinorRuntimeVersion = 5;
            corHeader.Resources.RelativeVirtualAddress = corHeader.MetadataDirectory.RelativeVirtualAddress + corHeader.MetadataDirectory.Size;
            corHeader.Resources.Size = (uint)metadataSizes.ResourceDataSize;
            corHeader.StrongNameSignature.RelativeVirtualAddress = corHeader.Resources.RelativeVirtualAddress + corHeader.Resources.Size;
            corHeader.StrongNameSignature.Size = (uint)ComputeStrongNameSignatureSize();
            corHeader.VTableFixups.RelativeVirtualAddress = 0;
            corHeader.VTableFixups.Size = 0;

            return corHeader;
        }

        private void FillInNtHeader(MetadataSizes metadataSizes, int mappedFieldDataStreamRva)
        {
            bool use32bitAddresses = !_module.Requires64bits;
            NtHeader ntHeader = _ntHeader;
            ntHeader.AddressOfEntryPoint = _emitRuntimeStartupStub ? (uint)mappedFieldDataStreamRva - (use32bitAddresses ? 6u : 10u) : 0;
            ntHeader.BaseOfCode = _textSection.RelativeVirtualAddress;
            ntHeader.BaseOfData = _rdataSection.RelativeVirtualAddress;
            ntHeader.PointerToSymbolTable = 0;
            ntHeader.SizeOfCode = _textSection.SizeOfRawData;
            ntHeader.SizeOfInitializedData = _rdataSection.SizeOfRawData + _coverSection.SizeOfRawData + _sdataSection.SizeOfRawData + _tlsSection.SizeOfRawData + _resourceSection.SizeOfRawData + _relocSection.SizeOfRawData;
            ntHeader.SizeOfHeaders = BitArithmeticUtilities.Align(this.ComputeSizeOfPeHeaders(), _module.FileAlignment);
            ntHeader.SizeOfImage = BitArithmeticUtilities.Align(_relocSection.RelativeVirtualAddress + _relocSection.VirtualSize, 0x2000);
            ntHeader.SizeOfUninitializedData = 0;

            // In the PE File Header this is a "Time/Date Stamp" whose description is "Time and date
            // the file was created in seconds since January 1st 1970 00:00:00 or 0"
            // However, when we want to make it deterministic we fill it in (later) with bits from the hash of the full PE file.
            ntHeader.TimeDateStamp = _deterministic ? 0 : (uint)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;

            ntHeader.ImportAddressTable.RelativeVirtualAddress = (_emitRuntimeStartupStub) ? _textSection.RelativeVirtualAddress : 0;
            ntHeader.ImportAddressTable.Size = (uint)_sizeOfImportAddressTable;

            ntHeader.CliHeaderTable.RelativeVirtualAddress = _textSection.RelativeVirtualAddress + ntHeader.ImportAddressTable.Size;
            ntHeader.CliHeaderTable.Size = 72;

            ntHeader.ImportTable.RelativeVirtualAddress = _textSection.RelativeVirtualAddress + (uint)ComputeOffsetToImportTable(metadataSizes);

            if (!_emitRuntimeStartupStub)
            {
                ntHeader.ImportTable.Size = 0;
                ntHeader.ImportTable.RelativeVirtualAddress = 0;
            }
            else
            {
                ntHeader.ImportTable.Size = use32bitAddresses ? 66u : 70u;
                ntHeader.ImportTable.Size += 13;  //size of nametable
            }

            ntHeader.BaseRelocationTable.RelativeVirtualAddress = (_emitRuntimeStartupStub) ? _relocSection.RelativeVirtualAddress : 0;
            ntHeader.BaseRelocationTable.Size = _relocSection.VirtualSize;
            ntHeader.BoundImportTable.RelativeVirtualAddress = 0;
            ntHeader.BoundImportTable.Size = 0;
            ntHeader.CertificateTable.RelativeVirtualAddress = 0;
            ntHeader.CertificateTable.Size = 0;
            ntHeader.CopyrightTable.RelativeVirtualAddress = 0;
            ntHeader.CopyrightTable.Size = 0;
            ntHeader.DebugTable.RelativeVirtualAddress = EmitPdb ? _textSection.RelativeVirtualAddress + (uint)ComputeOffsetToDebugTable(metadataSizes) : 0u;
            ntHeader.DebugTable.Size = EmitPdb ? ImageDebugDirectoryBaseSize : 0u; // Only the size of the fixed part of the debug table goes here.
            ntHeader.DelayImportTable.RelativeVirtualAddress = 0;
            ntHeader.DelayImportTable.Size = 0;
            ntHeader.ExceptionTable.RelativeVirtualAddress = 0;
            ntHeader.ExceptionTable.Size = 0;
            ntHeader.ExportTable.RelativeVirtualAddress = 0;
            ntHeader.ExportTable.Size = 0;
            ntHeader.GlobalPointerTable.RelativeVirtualAddress = 0;
            ntHeader.GlobalPointerTable.Size = 0;
            ntHeader.LoadConfigTable.RelativeVirtualAddress = 0;
            ntHeader.LoadConfigTable.Size = 0;
            ntHeader.Reserved.RelativeVirtualAddress = 0;
            ntHeader.Reserved.Size = 0;
            ntHeader.ResourceTable.RelativeVirtualAddress = _resourceSection.SizeOfRawData == 0 ? 0u : _resourceSection.RelativeVirtualAddress;
            ntHeader.ResourceTable.Size = _resourceSection.VirtualSize;
            ntHeader.ThreadLocalStorageTable.RelativeVirtualAddress = _tlsSection.SizeOfRawData == 0 ? 0u : _tlsSection.RelativeVirtualAddress;
            ntHeader.ThreadLocalStorageTable.Size = _tlsSection.SizeOfRawData;
        }

        private void FillInTextSectionHeader(MetadataSizes metadataSizes)
        {
            if (_textSection == null)
            {
                uint sizeOfPeHeaders = (uint)ComputeSizeOfPeHeaders();
                uint sizeOfTextSection = (uint)ComputeSizeOfTextSection(metadataSizes);

                _textSection = new SectionHeader
                {
                    Characteristics = 0x60000020, // section is read + execute + code 
                    Name = ".text",
                    NumberOfLinenumbers = 0,
                    NumberOfRelocations = 0,
                    PointerToLinenumbers = 0,
                    PointerToRawData = BitArithmeticUtilities.Align(sizeOfPeHeaders, _module.FileAlignment),
                    PointerToRelocations = 0,
                    RelativeVirtualAddress = BitArithmeticUtilities.Align(sizeOfPeHeaders, 0x2000),
                    SizeOfRawData = BitArithmeticUtilities.Align(sizeOfTextSection, _module.FileAlignment),
                    VirtualSize = sizeOfTextSection
                };
            }
        }

        private void FillInSectionHeaders()
        {
            _rdataSection = new SectionHeader
            {
                Characteristics = 0x40000040, // section is read + initialized
                Name = ".rdata",
                NumberOfLinenumbers = 0,
                NumberOfRelocations = 0,
                PointerToLinenumbers = 0,
                PointerToRawData = _textSection.PointerToRawData + _textSection.SizeOfRawData,
                PointerToRelocations = 0,
                RelativeVirtualAddress = BitArithmeticUtilities.Align(_textSection.RelativeVirtualAddress + _textSection.VirtualSize, 0x2000),
                SizeOfRawData = BitArithmeticUtilities.Align(_rdataWriter.BaseStream.Length, _module.FileAlignment),
                VirtualSize = _rdataWriter.BaseStream.Length,
            };

            _sdataSection = new SectionHeader
            {
                Characteristics = 0xC0000040, // section is write + read + initialized 
                Name = ".sdata",
                NumberOfLinenumbers = 0,
                NumberOfRelocations = 0,
                PointerToLinenumbers = 0,
                PointerToRawData = _rdataSection.PointerToRawData + _rdataSection.SizeOfRawData,
                PointerToRelocations = 0,
                RelativeVirtualAddress = BitArithmeticUtilities.Align(_rdataSection.RelativeVirtualAddress + _rdataSection.VirtualSize, 0x2000),
                SizeOfRawData = BitArithmeticUtilities.Align(_sdataWriter.BaseStream.Length, _module.FileAlignment),
                VirtualSize = _sdataWriter.BaseStream.Length,
            };

            _coverSection = new SectionHeader
            {
                Characteristics = 0xC8000040, // section is not paged + write + read + initialized 
                Name = ".cover",
                NumberOfLinenumbers = 0,
                NumberOfRelocations = 0,
                PointerToLinenumbers = 0,
                PointerToRawData = _sdataSection.PointerToRawData + _sdataSection.SizeOfRawData,
                PointerToRelocations = 0,
                RelativeVirtualAddress = BitArithmeticUtilities.Align(_sdataSection.RelativeVirtualAddress + _sdataSection.VirtualSize, 0x2000),
                SizeOfRawData = BitArithmeticUtilities.Align(_coverageDataWriter.BaseStream.Length, _module.FileAlignment),
                VirtualSize = _coverageDataWriter.BaseStream.Length,
            };

            _tlsSection = new SectionHeader
            {
                Characteristics = 0xC0000040, // section is write + read + initialized 
                Name = ".tls",
                NumberOfLinenumbers = 0,
                NumberOfRelocations = 0,
                PointerToLinenumbers = 0,
                PointerToRawData = _coverSection.PointerToRawData + _coverSection.SizeOfRawData,
                PointerToRelocations = 0,
                RelativeVirtualAddress = BitArithmeticUtilities.Align(_coverSection.RelativeVirtualAddress + _coverSection.VirtualSize, 0x2000),
                SizeOfRawData = BitArithmeticUtilities.Align(_tlsDataWriter.BaseStream.Length, _module.FileAlignment),
                VirtualSize = _tlsDataWriter.BaseStream.Length,
            };

            uint resourcesRva = BitArithmeticUtilities.Align(_tlsSection.RelativeVirtualAddress + _tlsSection.VirtualSize, 0x2000);
            uint sizeOfWin32Resources = this.ComputeSizeOfWin32Resources(resourcesRva);

            _resourceSection = new SectionHeader
            {
                Characteristics = 0x40000040, // section is read + initialized  
                Name = ".rsrc",
                NumberOfLinenumbers = 0,
                NumberOfRelocations = 0,
                PointerToLinenumbers = 0,
                PointerToRawData = _tlsSection.PointerToRawData + _tlsSection.SizeOfRawData,
                PointerToRelocations = 0,
                RelativeVirtualAddress = resourcesRva,
                SizeOfRawData = BitArithmeticUtilities.Align(sizeOfWin32Resources, _module.FileAlignment),
                VirtualSize = sizeOfWin32Resources,
            };

            _relocSection = new SectionHeader
            {
                Characteristics = 0x42000040, // section is read + discardable + initialized  
                Name = ".reloc",
                NumberOfLinenumbers = 0,
                NumberOfRelocations = 0,
                PointerToLinenumbers = 0,
                PointerToRawData = _resourceSection.PointerToRawData + _resourceSection.SizeOfRawData,
                PointerToRelocations = 0,
                RelativeVirtualAddress = BitArithmeticUtilities.Align(_resourceSection.RelativeVirtualAddress + _resourceSection.VirtualSize, 0x2000),
                SizeOfRawData = _emitRuntimeStartupStub ? _module.FileAlignment : 0,
                VirtualSize = _emitRuntimeStartupStub ? (_module.Requires64bits && !_module.RequiresAmdInstructionSet ? 14u : 12u) : 0,
            };
        }

        private CorFlags GetCorHeaderFlags()
        {
            CorFlags result = 0;
            if (_module.ILOnly)
            {
                result |= CorFlags.ILOnly;
            }

            if (_module.Requires32bits)
            {
                result |= CorFlags.Requires32Bit;
            }

            if (_module.StrongNameSigned)
            {
                result |= CorFlags.StrongNameSigned;
            }

            if (_module.TrackDebugData)
            {
                result |= CorFlags.TrackDebugData;
            }

            if (_module.Prefers32bits)
            {
                result |= CorFlags.Requires32Bit | CorFlags.Prefers32Bit;
            }

            return result;
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
        private void SerializeWin32Resources(uint resourcesRva)
        {
            var resourceSection = _module.Win32ResourceSection;
            if (resourceSection != null)
            {
                SerializeWin32Resources(resourceSection, resourcesRva);
                return;
            }

            var theResources = _module.Win32Resources;

            if (IteratorHelper.EnumerableIsEmpty(theResources))
            {
                return;
            }

            SerializeWin32Resources(theResources, resourcesRva);
        }

        private void SerializeWin32Resources(IEnumerable<IWin32Resource> theResources, uint resourcesRva)
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

            MemoryStream stream = MemoryStream.GetInstance();
            BinaryWriter dataWriter = new BinaryWriter(stream, true);

            //'dataWriter' is where opaque resource data goes as well as strings that are used as type or name identifiers
            this.WriteDirectory(typeDirectory, _win32ResourceWriter, 0, 0, sizeOfDirectoryTree, resourcesRva, dataWriter);
            dataWriter.BaseStream.WriteTo(_win32ResourceWriter.BaseStream);
            _win32ResourceWriter.WriteByte(0);
            while ((_win32ResourceWriter.BaseStream.Length % 4) != 0)
            {
                _win32ResourceWriter.WriteByte(0);
            }
            stream.Free();
        }

        private void WriteDirectory(Directory directory, BinaryWriter writer, uint offset, uint level, uint sizeOfDirectoryTree, uint virtualAddressBase, BinaryWriter dataWriter)
        {
            writer.WriteUint(0); // Characteristics
            writer.WriteUint(0); // Timestamp
            writer.WriteUint(0); // Version
            writer.WriteUshort(directory.NumberOfNamedEntries);
            writer.WriteUshort(directory.NumberOfIdEntries);
            uint n = (uint)directory.Entries.Count;
            uint k = offset + 16 + n * 8;
            for (int i = 0; i < n; i++)
            {
                int id;
                string name;
                uint nameOffset = dataWriter.BaseStream.Position + sizeOfDirectoryTree;
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
                    dataWriter.WriteUint(virtualAddressBase + sizeOfDirectoryTree + 16 + dataWriter.BaseStream.Position);
                    byte[] data = new List<byte>(r.Data).ToArray();
                    dataWriter.WriteUint((uint)data.Length);
                    dataWriter.WriteUint(r.CodePage);
                    dataWriter.WriteUint(0);
                    dataWriter.WriteBytes(data);
                    while ((dataWriter.BaseStream.Length % 4) != 0)
                    {
                        dataWriter.WriteByte(0);
                    }
                }

                if (id >= 0)
                {
                    writer.WriteInt(id);
                }
                else
                {
                    if (name == null)
                    {
                        name = string.Empty;
                    }

                    writer.WriteUint(nameOffset | 0x80000000);
                    dataWriter.WriteUshort((ushort)name.Length);
                    dataWriter.WriteChars(name.ToCharArray());  // REVIEW: what happens if the name contains chars that do not fit into a single utf8 code point?
                }

                if (subDir != null)
                {
                    writer.WriteUint(directoryOffset | 0x80000000);
                }
                else
                {
                    writer.WriteUint(nameOffset);
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

        private void SerializeWin32Resources(ResourceSection resourceSections, uint resourcesRva)
        {
            _win32ResourceWriter.WriteBytes(resourceSections.SectionBytes);

            var savedPosition = _win32ResourceWriter.BaseStream.Position;

            var readStream = new System.IO.MemoryStream(resourceSections.SectionBytes);
            var reader = new BinaryReader(readStream);

            foreach (int addressToFixup in resourceSections.Relocations)
            {
                _win32ResourceWriter.BaseStream.Position = (uint)addressToFixup;
                reader.BaseStream.Position = addressToFixup;
                _win32ResourceWriter.WriteUint(reader.ReadUInt32() + resourcesRva);
            }

            _win32ResourceWriter.BaseStream.Position = savedPosition;
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

        private void WriteHeaders(Stream peStream, out long ntHeaderTimestampPosition)
        {
            IModule module = _module;
            NtHeader ntHeader = _ntHeader;
            BinaryWriter writer = new BinaryWriter(_headerStream);

            // MS-DOS stub (128 bytes)
            writer.WriteBytes(s_dosHeader); // TODO: provide an option to suppress the second half of the DOS header?

            // PE Signature (4 bytes)
            writer.WriteUint(0x00004550); /* "PE\0\0" */

            // COFF Header 20 bytes
            writer.WriteUshort((ushort)module.Machine);
            writer.WriteUshort(ntHeader.NumberOfSections);
            ntHeaderTimestampPosition = writer.BaseStream.Position + peStream.Position;
            writer.WriteUint(ntHeader.TimeDateStamp);
            writer.WriteUint(ntHeader.PointerToSymbolTable);
            writer.WriteUint(0); // NumberOfSymbols
            writer.WriteUshort((ushort)(!module.Requires64bits ? 224 : 240)); // SizeOfOptionalHeader
            // ushort characteristics = 0x0002|0x0004|0x0008; // executable | no COFF line nums | no COFF symbols (as required by the standard)
            ushort characteristics = 0x0002; // executable (as required by the Linker team).
            if (module.Kind == ModuleKind.DynamicallyLinkedLibrary || module.Kind == ModuleKind.WindowsRuntimeMetadata)
            {
                characteristics |= 0x2000;
            }

            if (module.Requires32bits)
            {
                characteristics |= 0x0100; // 32 bit machine (The standard says to always set this, the linker team says otherwise)
                                           //The loader team says that this is not used for anything in the OS. 
            }
            else
            {
                characteristics |= 0x0020; // large address aware (the standard says never to set this, the linker team says otherwise).
                                           //The loader team says that this is not overridden for managed binaries and will be respected if set.
            }

            writer.WriteUshort(characteristics);

            // PE Header (224 bytes if 32 bits, 240 bytes if 64 bit)
            if (!module.Requires64bits)
            {
                writer.WriteUshort(0x10B); // Magic = PE32  // 2
            }
            else
            {
                writer.WriteUshort(0x20B); // Magic = PE32+ // 2
            }

            writer.WriteByte(module.LinkerMajorVersion); // 3
            writer.WriteByte(module.LinkerMinorVersion); // 4
            writer.WriteUint(ntHeader.SizeOfCode); // 8
            writer.WriteUint(ntHeader.SizeOfInitializedData); // 12
            writer.WriteUint(ntHeader.SizeOfUninitializedData); // 16
            writer.WriteUint(ntHeader.AddressOfEntryPoint); // 20
            writer.WriteUint(ntHeader.BaseOfCode); // 24
            if (!module.Requires64bits)
            {
                writer.WriteUint(ntHeader.BaseOfData); // 28
                writer.WriteUint((uint)module.BaseAddress); // 32
            }
            else
            {
                writer.WriteUlong(module.BaseAddress); // 32
            }

            writer.WriteUint(0x2000); // SectionAlignment 36
            writer.WriteUint(module.FileAlignment); // 40
            writer.WriteUshort(4); // MajorOperatingSystemVersion 42
            writer.WriteUshort(0); // MinorOperatingSystemVersion 44
            writer.WriteUshort(0); // MajorImageVersion 46
            writer.WriteUshort(0); // MinorImageVersion 48
            writer.WriteUshort(module.MajorSubsystemVersion); // MajorSubsystemVersion 50
            writer.WriteUshort(module.MinorSubsystemVersion); // MinorSubsystemVersion 52
            writer.WriteUint(0); // Win32VersionValue 56
            writer.WriteUint(ntHeader.SizeOfImage); // 60
            writer.WriteUint(ntHeader.SizeOfHeaders); // 64
            writer.WriteUint(0); // CheckSum 68
            switch (module.Kind)
            {
                case ModuleKind.ConsoleApplication:
                case ModuleKind.DynamicallyLinkedLibrary:
                case ModuleKind.WindowsRuntimeMetadata:
                    writer.WriteUshort(3); // 70
                    break;
                case ModuleKind.WindowsApplication:
                    writer.WriteUshort(2); // 70
                    break;
                default:
                    writer.WriteUshort(0); //
                    break;
            }

            writer.WriteUshort(module.DllCharacteristics);

            if (!module.Requires64bits)
            {
                writer.WriteUint((uint)module.SizeOfStackReserve); // 76
                writer.WriteUint((uint)module.SizeOfStackCommit); // 80
                writer.WriteUint((uint)module.SizeOfHeapReserve); // 84
                writer.WriteUint((uint)module.SizeOfHeapCommit); // 88
            }
            else
            {
                writer.WriteUlong(module.SizeOfStackReserve); // 80
                writer.WriteUlong(module.SizeOfStackCommit); // 88
                writer.WriteUlong(module.SizeOfHeapReserve); // 96
                writer.WriteUlong(module.SizeOfHeapCommit); // 104
            }

            writer.WriteUint(0); // LoaderFlags 92|108
            writer.WriteUint(16); // numberOfDataDirectories 96|112

            writer.WriteUint(ntHeader.ExportTable.RelativeVirtualAddress); // 100|116
            writer.WriteUint(ntHeader.ExportTable.Size); // 104|120
            writer.WriteUint(ntHeader.ImportTable.RelativeVirtualAddress); // 108|124
            writer.WriteUint(ntHeader.ImportTable.Size); // 112|128
            writer.WriteUint(ntHeader.ResourceTable.RelativeVirtualAddress); // 116|132
            writer.WriteUint(ntHeader.ResourceTable.Size); // 120|136
            writer.WriteUint(ntHeader.ExceptionTable.RelativeVirtualAddress); // 124|140
            writer.WriteUint(ntHeader.ExceptionTable.Size); // 128|144
            writer.WriteUint(ntHeader.CertificateTable.RelativeVirtualAddress); // 132|148
            writer.WriteUint(ntHeader.CertificateTable.Size); // 136|152
            writer.WriteUint(ntHeader.BaseRelocationTable.RelativeVirtualAddress); // 140|156
            writer.WriteUint(ntHeader.BaseRelocationTable.Size); // 144|160
            writer.WriteUint(ntHeader.DebugTable.RelativeVirtualAddress); // 148|164
            writer.WriteUint(ntHeader.DebugTable.Size); // 152|168
            writer.WriteUint(ntHeader.CopyrightTable.RelativeVirtualAddress); // 156|172
            writer.WriteUint(ntHeader.CopyrightTable.Size); // 160|176
            writer.WriteUint(ntHeader.GlobalPointerTable.RelativeVirtualAddress); // 164|180
            writer.WriteUint(ntHeader.GlobalPointerTable.Size); // 168|184
            writer.WriteUint(ntHeader.ThreadLocalStorageTable.RelativeVirtualAddress); // 172|188
            writer.WriteUint(ntHeader.ThreadLocalStorageTable.Size); // 176|192
            writer.WriteUint(ntHeader.LoadConfigTable.RelativeVirtualAddress); // 180|196
            writer.WriteUint(ntHeader.LoadConfigTable.Size); // 184|200
            writer.WriteUint(ntHeader.BoundImportTable.RelativeVirtualAddress); // 188|204
            writer.WriteUint(ntHeader.BoundImportTable.Size); // 192|208
            writer.WriteUint(ntHeader.ImportAddressTable.RelativeVirtualAddress); // 196|212
            writer.WriteUint(ntHeader.ImportAddressTable.Size); // 200|216
            writer.WriteUint(ntHeader.DelayImportTable.RelativeVirtualAddress); // 204|220
            writer.WriteUint(ntHeader.DelayImportTable.Size); // 208|224
            writer.WriteUint(ntHeader.CliHeaderTable.RelativeVirtualAddress); // 212|228
            writer.WriteUint(ntHeader.CliHeaderTable.Size); // 216|232
            writer.WriteUlong(0); // 224|240

            // Section Headers
            WriteSectionHeader(_textSection, writer);
            WriteSectionHeader(_rdataSection, writer);
            WriteSectionHeader(_sdataSection, writer);
            WriteSectionHeader(_coverSection, writer);
            WriteSectionHeader(_resourceSection, writer);
            WriteSectionHeader(_relocSection, writer);
            WriteSectionHeader(_tlsSection, writer);

            writer.BaseStream.WriteTo(peStream);
            _headerStream = _emptyStream;
        }

        private static void WriteSectionHeader(SectionHeader sectionHeader, BinaryWriter writer)
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

            writer.WriteUint(sectionHeader.VirtualSize);
            writer.WriteUint(sectionHeader.RelativeVirtualAddress);
            writer.WriteUint(sectionHeader.SizeOfRawData);
            writer.WriteUint(sectionHeader.PointerToRawData);
            writer.WriteUint(sectionHeader.PointerToRelocations);
            writer.WriteUint(sectionHeader.PointerToLinenumbers);
            writer.WriteUshort(sectionHeader.NumberOfRelocations);
            writer.WriteUshort(sectionHeader.NumberOfLinenumbers);
            writer.WriteUint(sectionHeader.Characteristics);
        }

        private void WriteTextSection(
            Stream peStream,
            CorHeader corHeader,
            MemoryStream metadataStream,
            MemoryStream ilStream,
            MemoryStream mappedFieldDataStream,
            MemoryStream managedResourceStream,
            MetadataSizes metadataSizes,
            ContentId pdbContentId,
            out long metadataPosition)
        {
            peStream.Position = _textSection.PointerToRawData;
            if (_emitRuntimeStartupStub)
            {
                this.WriteImportAddressTable(peStream);
            }

            WriteCorHeader(peStream, corHeader);
            WriteIL(peStream, ilStream);

            metadataPosition = peStream.Position;
            WriteMetadata(peStream, metadataStream);

            WriteManagedResources(peStream, managedResourceStream);
            WriteSpaceForHash(peStream, (int)corHeader.StrongNameSignature.Size);
            WriteDebugTable(peStream, pdbContentId, metadataSizes);

            if (_emitRuntimeStartupStub)
            {
                WriteImportTable(peStream);
                WriteNameTable(peStream);
                WriteRuntimeStartupStub(peStream);
            }

            WriteMappedFieldData(peStream, mappedFieldDataStream);
        }

        private void WriteImportAddressTable(Stream peStream)
        {
            BinaryWriter writer = new BinaryWriter(new MemoryStream(16));
            bool use32bitAddresses = !_module.Requires64bits;
            uint importTableRVA = _ntHeader.ImportTable.RelativeVirtualAddress;
            uint ilRVA = importTableRVA + 40;
            uint hintRva = ilRVA + (use32bitAddresses ? 12u : 16u);

            // Import Address Table
            if (use32bitAddresses)
            {
                writer.WriteUint(hintRva); // 4
                writer.WriteUint(0); // 8
            }
            else
            {
                writer.WriteUlong(hintRva); // 8
                writer.WriteUlong(0); // 16
            }

            writer.BaseStream.WriteTo(peStream);
        }

        private void WriteImportTable(Stream peStream)
        {
            BinaryWriter writer = new BinaryWriter(new MemoryStream(70));
            bool use32bitAddresses = !_module.Requires64bits;
            uint importTableRVA = _ntHeader.ImportTable.RelativeVirtualAddress;
            uint ilRVA = importTableRVA + 40;
            uint hintRva = ilRVA + (use32bitAddresses ? 12u : 16u);
            uint nameRva = hintRva + 12 + 2;

            // Import table
            writer.WriteUint(ilRVA); // 4
            writer.WriteUint(0); // 8
            writer.WriteUint(0); // 12
            writer.WriteUint(nameRva); // 16
            writer.WriteUint(_ntHeader.ImportAddressTable.RelativeVirtualAddress); // 20
            writer.BaseStream.Position += 20; // 40

            // Import Lookup table
            if (use32bitAddresses)
            {
                writer.WriteUint(hintRva); // 44
                writer.WriteUint(0); // 48
                writer.WriteUint(0); // 52
            }
            else
            {
                writer.WriteUlong(hintRva); // 48
                writer.WriteUlong(0); // 56
            }

            // Hint table
            writer.WriteUshort(0); // Hint 54|58
            string entryPointName =
                (_module.Kind == ModuleKind.DynamicallyLinkedLibrary || _module.Kind == ModuleKind.WindowsRuntimeMetadata)
                ? "_CorDllMain" : "_CorExeMain";

            foreach (char ch in entryPointName)
            {
                writer.WriteByte((byte)ch); // 65|69
            }

            writer.WriteByte(0); // 66|70

            writer.BaseStream.WriteTo(peStream);
        }

        private static void WriteNameTable(Stream peStream)
        {
            BinaryWriter writer = new BinaryWriter(new MemoryStream(14));
            foreach (char ch in "mscoree.dll")
            {
                writer.WriteByte((byte)ch); // 11
            }

            writer.WriteByte(0); // 12
            writer.WriteUshort(0); // 14
            writer.BaseStream.WriteTo(peStream);
        }

        private static void WriteCorHeader(Stream peStream, CorHeader corHeader)
        {
            BinaryWriter writer = new BinaryWriter(new MemoryStream(72));
            writer.WriteUint(72); // Number of bytes in this header  4
            writer.WriteUshort(corHeader.MajorRuntimeVersion); // 6 
            writer.WriteUshort(corHeader.MinorRuntimeVersion); // 8
            writer.WriteUint(corHeader.MetadataDirectory.RelativeVirtualAddress); // 12
            writer.WriteUint(corHeader.MetadataDirectory.Size); // 16
            writer.WriteUint((uint)corHeader.Flags); // 20
            writer.WriteUint(corHeader.EntryPointToken); // 24
            writer.WriteUint(corHeader.Resources.Size == 0 ? 0u : corHeader.Resources.RelativeVirtualAddress); // 28
            writer.WriteUint(corHeader.Resources.Size); // 32
            writer.WriteUint(corHeader.StrongNameSignature.Size == 0 ? 0u : corHeader.StrongNameSignature.RelativeVirtualAddress); // 36
            writer.WriteUint(corHeader.StrongNameSignature.Size); // 40
            writer.WriteUint(corHeader.CodeManagerTable.RelativeVirtualAddress); // 44
            writer.WriteUint(corHeader.CodeManagerTable.Size); // 48
            writer.WriteUint(corHeader.VTableFixups.RelativeVirtualAddress); // 52
            writer.WriteUint(corHeader.VTableFixups.Size); // 56
            writer.WriteUint(corHeader.ExportAddressTableJumps.RelativeVirtualAddress); // 60
            writer.WriteUint(corHeader.ExportAddressTableJumps.Size); // 64
            writer.WriteUlong(0); // 72
            writer.BaseStream.WriteTo(peStream);
        }

        private static void WriteIL(Stream peStream, MemoryStream ilStream)
        {
            ilStream.WriteTo(peStream);
            while (peStream.Position % 4 != 0)
            {
                peStream.WriteByte(0);
            }
        }

        private static void WriteMappedFieldData(Stream peStream, MemoryStream dataStream)
        {
            dataStream.WriteTo(peStream);
            while (peStream.Position % 4 != 0)
            {
                peStream.WriteByte(0);
            }
        }

        private static void WriteSpaceForHash(Stream peStream, int strongNameSignatureSize)
        {
            while (strongNameSignatureSize > 0)
            {
                peStream.WriteByte(0);
                strongNameSignatureSize--;
            }
        }

        private static void WriteMetadata(Stream peStream, MemoryStream metadataStream)
        {
            metadataStream.WriteTo(peStream);
            while (peStream.Position % 4 != 0)
            {
                peStream.WriteByte(0);
            }
        }

        private static void WriteManagedResources(Stream peStream, MemoryStream managedResourceStream)
        {
            managedResourceStream.WriteTo(peStream);
            while (peStream.Position % 4 != 0)
            {
                peStream.WriteByte(0);
            }
        }

        private void WriteDebugTable(Stream peStream, ContentId nativePdbContentId, MetadataSizes metadataSizes)
        {
            if (!EmitPdb)
            {
                return;
            }

            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);

            // characteristics:
            writer.WriteUint(0);

            // PDB stamp
            writer.WriteBytes(nativePdbContentId.Stamp);

            // version
            writer.WriteUint(0);

            // type: 
            const int ImageDebugTypeCodeView = 2;
            writer.WriteUint(ImageDebugTypeCodeView);

            // size of data:
            writer.WriteUint((uint)ComputeSizeOfDebugDirectoryData());

            uint dataOffset = (uint)ComputeOffsetToDebugTable(metadataSizes) + ImageDebugDirectoryBaseSize;

            // PointerToRawData (RVA of the data):
            writer.WriteUint(_textSection.RelativeVirtualAddress + dataOffset);

            // AddressOfRawData (position of the data in the PE stream):
            writer.WriteUint(_textSection.PointerToRawData + dataOffset);

            writer.WriteByte((byte)'R');
            writer.WriteByte((byte)'S');
            writer.WriteByte((byte)'D');
            writer.WriteByte((byte)'S');

            // PDB id:
            writer.WriteBytes(nativePdbContentId.Guid);

            // age
            writer.WriteUint(PdbWriter.Age);

            // UTF-8 encoded zero-terminated path to PDB
            writer.WriteString(_pdbPathOpt, emitNullTerminator: true);

            writer.BaseStream.WriteTo(peStream);
            stream.Free();
        }

        private void WriteRuntimeStartupStub(Stream peStream)
        {
            BinaryWriter writer = new BinaryWriter(new MemoryStream(16));
            // entry point code, consisting of a jump indirect to _CorXXXMain
            if (!_module.Requires64bits)
            {
                //emit 0's (nops) to pad the entry point code so that the target address is aligned on a 4 byte boundary.
                for (uint i = 0, n = (uint)(BitArithmeticUtilities.Align((uint)peStream.Position, 4) - peStream.Position); i < n; i++) writer.WriteByte(0);
                writer.WriteUshort(0);
                writer.WriteByte(0xff);
                writer.WriteByte(0x25); //4
                writer.WriteUint(_ntHeader.ImportAddressTable.RelativeVirtualAddress + (uint)_module.BaseAddress); //8
            }
            else
            {
                //emit 0's (nops) to pad the entry point code so that the target address is aligned on a 8 byte boundary.
                for (uint i = 0, n = (uint)(BitArithmeticUtilities.Align((uint)peStream.Position, 8) - peStream.Position); i < n; i++) writer.WriteByte(0);
                writer.WriteUint(0);
                writer.WriteUshort(0);
                writer.WriteByte(0xff);
                writer.WriteByte(0x25); //8
                writer.WriteUlong(_ntHeader.ImportAddressTable.RelativeVirtualAddress + _module.BaseAddress); //16
            }
            writer.BaseStream.WriteTo(peStream);
        }

        private void WriteCoverSection(Stream peStream)
        {
            peStream.Position = _coverSection.PointerToRawData;
            _coverageDataWriter.BaseStream.WriteTo(peStream);
        }

        private void WriteRdataSection(Stream peStream)
        {
            peStream.Position = _rdataSection.PointerToRawData;
            _rdataWriter.BaseStream.WriteTo(peStream);
        }

        private void WriteSdataSection(Stream peStream)
        {
            peStream.Position = _sdataSection.PointerToRawData;
            _sdataWriter.BaseStream.WriteTo(peStream);
        }

        private void WriteRelocSection(Stream peStream)
        {
            if (!_emitRuntimeStartupStub)
            {
                //No need to write out a reloc section, but there is still a need to pad out the peStream so that it is an even multiple of module.FileAlignment
                if (_relocSection.PointerToRawData != peStream.Position)
                { //for example, the resource section did not end bang on the alignment boundary
                    peStream.Position = _relocSection.PointerToRawData - 1;
                    peStream.WriteByte(0);
                }
                return;
            }

            peStream.Position = _relocSection.PointerToRawData;
            BinaryWriter writer = new BinaryWriter(new MemoryStream(_module.FileAlignment));
            writer.WriteUint(((_ntHeader.AddressOfEntryPoint + 2) / 0x1000) * 0x1000);
            writer.WriteUint(_module.Requires64bits && !_module.RequiresAmdInstructionSet ? 14u : 12u);
            uint offsetWithinPage = (_ntHeader.AddressOfEntryPoint + 2) % 0x1000;
            uint relocType = _module.Requires64bits ? 10u : 3u;
            ushort s = (ushort)((relocType << 12) | offsetWithinPage);
            writer.WriteUshort(s);
            if (_module.Requires64bits && !_module.RequiresAmdInstructionSet)
            {
                writer.WriteUint(relocType << 12);
            }

            writer.WriteUshort(0); // next chunk's RVA
            writer.BaseStream.Position = _module.FileAlignment;
            writer.BaseStream.WriteTo(peStream);
        }

        private void WriteResourceSection(Stream peStream)
        {
            if (_win32ResourceWriter.BaseStream.Length == 0)
            {
                return;
            }

            peStream.Position = _resourceSection.PointerToRawData;
            _win32ResourceWriter.BaseStream.WriteTo(peStream);
            peStream.WriteByte(0);
            while (peStream.Position % 8 != 0)
            {
                peStream.WriteByte(0);
            }
        }

        private void WriteTlsSection(Stream peStream)
        {
            peStream.Position = _tlsSection.PointerToRawData;
            _tlsDataWriter.BaseStream.WriteTo(peStream);
        }
    }
}
