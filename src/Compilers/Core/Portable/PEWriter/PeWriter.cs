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

    internal sealed class PeWriter
    {
        private const string ResourceSectionName = ".rsrc";
        private const string RelocationSectionName = ".reloc";

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
            var ilBuilder = new BlobBuilder(32 * 1024);
            var metadataBuilder = new BlobBuilder(16 * 1024);
            var mappedFieldDataBuilder = new BlobBuilder();
            var managedResourceBuilder = new BlobBuilder(1024);
            var textSectionBlob = new BlobBuilder();
            var resourceSectionBlobOpt = (!IteratorHelper.EnumerableIsEmpty(_nativeResourcesOpt) || _nativeResourceSectionOpt != null) ? new BlobBuilder() : null;
            var relocationSectionBlobOpt = _properties.RequiresStartupStub ? new BlobBuilder() : null;
            var debugMetadataBuilderOpt = (getPortablePdbStreamOpt != null) ? new BlobBuilder(16 * 1024) : null;

            nativePdbWriterOpt?.SetMetadataEmitter(mdWriter);

            // Since we are producing a full assembly, we should not have a module version ID
            // imposed ahead-of time. Instead we will compute a deterministic module version ID
            // based on the contents of the generated stream.
            Debug.Assert(_properties.PersistentIdentifier == default(Guid));

            var peBuilder = new PEBuilder(
                sectionCount: 1 + (resourceSectionBlobOpt != null ? 1 : 0) + (relocationSectionBlobOpt != null ? 1 : 0),
                sectionAlignment: _properties.SectionAlignment,
                fileAlignment: _properties.FileAlignment,
                is32Bit: _is32bit);

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
                _properties.ImageCharacteristics,
                _properties.Machine,
                textSectionLocation.RelativeVirtualAddress,
                _pdbPathOpt,
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

            Stream peStream = getPeStream();
            if (peStream == null)
            {
                return false;
            }

            BlobBuilder debugTableBuilderOpt;
            if (EmitPdb || _deterministic)
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
                _properties.GetCorHeaderFlags(),
                _properties.BaseAddress,
                metadataBuilder,
                ilBuilder,
                mappedFieldDataBuilder,
                managedResourceBuilder,
                debugTableBuilderOpt,
                out metadataPosition);

            peBuilder.Machine = _properties.Machine;
            peBuilder.TimeDateStamp = _timeStamp;
            peBuilder.ImageCharacteristics = _properties.ImageCharacteristics;
            peBuilder.MajorLinkerVersion = _properties.LinkerMajorVersion;
            peBuilder.MinorLinkerVersion = _properties.LinkerMinorVersion;
            peBuilder.AddressOfEntryPoint = entryPointAddress;
            peBuilder.ImageBase = _properties.BaseAddress;
            peBuilder.MajorSubsystemVersion = _properties.MajorSubsystemVersion;
            peBuilder.MinorSubsystemVersion = _properties.MinorSubsystemVersion;
            peBuilder.Subsystem = _properties.Subsystem;
            peBuilder.DllCharacteristics = _properties.DllCharacteristics;
            peBuilder.SizeOfStackReserve = _properties.SizeOfStackReserve;
            peBuilder.SizeOfStackCommit = _properties.SizeOfStackCommit;
            peBuilder.SizeOfHeapReserve = _properties.SizeOfHeapReserve;
            peBuilder.SizeOfHeapCommit = _properties.SizeOfHeapCommit;

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
                var location = peBuilder.NextSectionLocation;

                WriteResourceSection(resourceSectionBlobOpt, location.RelativeVirtualAddress);

                peBuilder.AddSection(
                    ResourceSectionName,
                    SectionCharacteristics.MemRead | SectionCharacteristics.ContainsInitializedData,
                    resourceSectionBlobOpt);

                peBuilder.ResourceTable = new DirectoryEntry(location.RelativeVirtualAddress, resourceSectionBlobOpt.Count);
            }

            // .reloc
            if (relocationSectionBlobOpt != null)
            {
                var location = peBuilder.NextSectionLocation;

                WriteRelocSection(relocationSectionBlobOpt, entryPointAddress);

                peBuilder.AddSection(
                    RelocationSectionName,
                    SectionCharacteristics.MemRead | SectionCharacteristics.MemDiscardable | SectionCharacteristics.ContainsInitializedData,
                    relocationSectionBlobOpt);

                peBuilder.BaseRelocationTable = new DirectoryEntry(location.RelativeVirtualAddress, relocationSectionBlobOpt.Count);
            }

            var peBlob = new BlobBuilder();
            peBuilder.Serialize(peBlob, out peHeaderTimestampPosition);
            peBlob.WriteContentTo(peStream);

            if (_deterministic)
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
        
        private void WriteRelocSection(BlobBuilder builder, int entryPointAddress)
        {
            Debug.Assert(builder.Count == 0);

            builder.WriteUInt32((((uint)entryPointAddress + 2) / 0x1000) * 0x1000);
            builder.WriteUInt32(_properties.Requires64bits && !_properties.RequiresAmdInstructionSet ? 14u : 12u);
            uint offsetWithinPage = ((uint)entryPointAddress + 2) % 0x1000;
            uint relocType = _properties.Requires64bits ? 10u : 3u;
            ushort s = (ushort)((relocType << 12) | offsetWithinPage);
            builder.WriteUInt16(s);
            if (_properties.Requires64bits && !_properties.RequiresAmdInstructionSet)
            {
                builder.WriteUInt32(relocType << 12);
            }

            builder.WriteUInt16(0); // next chunk's RVA
        }

        private void WriteResourceSection(BlobBuilder builder, int rva)
        {
            this.SerializeWin32Resources(rva);
            // TODO: avoid copy 
            _win32ResourceWriter.WriteContentTo(builder);
        }
    }
}
