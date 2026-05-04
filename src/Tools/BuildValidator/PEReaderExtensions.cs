// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;

namespace BuildValidator
{
    public class PEExportTable
    {
        private readonly Dictionary<string, int> _namedExportRva;

        private PEExportTable(PEReader peReader)
        {
            Debug.Assert(peReader.PEHeaders is object);
            Debug.Assert(peReader.PEHeaders.PEHeader is object);

            _namedExportRva = new Dictionary<string, int>();

            DirectoryEntry exportTable = peReader.PEHeaders.PEHeader.ExportTableDirectory;
            if ((exportTable.Size == 0) || (exportTable.RelativeVirtualAddress == 0))
                return;

            PEMemoryBlock peImage = peReader.GetEntireImage();
            BlobReader exportTableHeader = peImage.GetReader(peReader.GetOffset(exportTable.RelativeVirtualAddress), exportTable.Size);
            if (exportTableHeader.Length == 0)
            {
                return;
            }

            // +0x00: reserved
            exportTableHeader.ReadUInt32();
            // +0x04: TODO: time/date stamp
            exportTableHeader.ReadUInt32();
            // +0x08: major version
            exportTableHeader.ReadUInt16();
            // +0x0A: minor version
            exportTableHeader.ReadUInt16();
            // +0x0C: DLL name RVA
            exportTableHeader.ReadUInt32();
            // +0x10: ordinal base
            int minOrdinal = exportTableHeader.ReadInt32();
            // +0x14: number of entries in the address table
            int addressEntryCount = exportTableHeader.ReadInt32();
            // +0x18: number of name pointers
            int namePointerCount = exportTableHeader.ReadInt32();
            // +0x1C: export address table RVA
            int addressTableRVA = exportTableHeader.ReadInt32();
            // +0x20: name pointer RVA
            int namePointerRVA = exportTableHeader.ReadInt32();
            // +0x24: ordinal table RVA
            int ordinalTableRVA = exportTableHeader.ReadInt32();

            int[] addressTable = new int[addressEntryCount];
            BlobReader addressTableReader = peImage.GetReader(peReader.GetOffset(addressTableRVA), sizeof(int) * addressEntryCount);
            for (int entryIndex = 0; entryIndex < addressEntryCount; entryIndex++)
            {
                addressTable[entryIndex] = addressTableReader.ReadInt32();
            }

            ushort[] ordinalTable = new ushort[namePointerCount];
            BlobReader ordinalTableReader = peImage.GetReader(peReader.GetOffset(ordinalTableRVA), sizeof(ushort) * namePointerCount);
            for (int entryIndex = 0; entryIndex < namePointerCount; entryIndex++)
            {
                ushort ordinalIndex = ordinalTableReader.ReadUInt16();
                ordinalTable[entryIndex] = ordinalIndex;
            }

            BlobReader namePointerReader = peImage.GetReader(peReader.GetOffset(namePointerRVA), sizeof(int) * namePointerCount);
            for (int entryIndex = 0; entryIndex < namePointerCount; entryIndex++)
            {
                int nameRVA = namePointerReader.ReadInt32();
                if (nameRVA != 0)
                {
                    int nameOffset = peReader.GetOffset(nameRVA);
                    BlobReader nameReader = peImage.GetReader(nameOffset, peImage.Length - nameOffset);
                    StringBuilder nameBuilder = new StringBuilder();
                    for (byte ascii; (ascii = nameReader.ReadByte()) != 0;)
                    {
                        nameBuilder.Append((char)ascii);
                    }
                    _namedExportRva.Add(nameBuilder.ToString(), addressTable[ordinalTable[entryIndex]]);
                }
            }
        }

        public static PEExportTable Parse(PEReader peReader)
        {
            return new PEExportTable(peReader);
        }

        public bool TryGetValue(string exportName, out int rva) => _namedExportRva.TryGetValue(exportName, out rva);
    }

    public static class PEReaderExtensions
    {
        /// <summary>
        /// Get the index in the image byte array corresponding to the RVA
        /// </summary>
        /// <param name="reader">PE reader representing the executable image to parse</param>
        /// <param name="rva">The relative virtual address</param>
        public static int GetOffset(this PEReader reader, int rva)
        {
            int index = reader.PEHeaders.GetContainingSectionIndex(rva);
            if (index == -1)
            {
                throw new BadImageFormatException("Failed to convert invalid RVA to offset: " + rva);
            }
            SectionHeader containingSection = reader.PEHeaders.SectionHeaders[index];
            return rva - containingSection.VirtualAddress + containingSection.PointerToRawData;
        }

        /// <summary>
        /// Parse export table directory for a given PE reader.
        /// </summary>
        /// <param name="reader">PE reader representing the executable image to parse</param>
        public static PEExportTable GetExportTable(this PEReader reader)
        {
            return PEExportTable.Parse(reader);
        }
    }
}
