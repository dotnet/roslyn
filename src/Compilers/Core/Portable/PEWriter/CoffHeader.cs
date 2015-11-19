// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Reflection.PortableExecutable;

namespace Microsoft.Cci
{
    // TODO: merge with System.Reflection.PortableExecutable.CoffHeader
    internal sealed class CoffHeader
    {
        /// <summary>
        /// The type of target machine.
        /// </summary>
        public Machine Machine { get; }

        /// <summary>
        /// The number of sections. This indicates the size of the section table, which immediately follows the headers.
        /// </summary>
        public short NumberOfSections { get; }

        /// <summary>
        /// The low 32 bits of the number of seconds since 00:00 January 1, 1970, that indicates when the file was created.
        /// </summary>
        public int TimeDateStamp { get; }

        /// <summary>
        /// The file pointer to the COFF symbol table, or zero if no COFF symbol table is present. 
        /// This value should be zero for a PE image.
        /// </summary>
        public int PointerToSymbolTable { get; }

        /// <summary>
        /// The number of entries in the symbol table. This data can be used to locate the string table, 
        /// which immediately follows the symbol table. This value should be zero for a PE image.
        /// </summary>
        public int NumberOfSymbols { get; }

        /// <summary>
        /// The size of the optional header, which is required for executable files but not for object files. 
        /// This value should be zero for an object file. 
        /// </summary>
        public short SizeOfOptionalHeader { get; }

        /// <summary>
        /// The flags that indicate the attributes of the file. 
        /// </summary>
        public Characteristics Characteristics { get; }

        public CoffHeader(
            Machine machine,
            short numberOfSections,
            int timeDateStamp,
            int pointerToSymbolTable,
            int numberOfSymbols,
            short sizeOfOptionalHeader,
            Characteristics characteristics)
        {
            Machine = machine;
            NumberOfSections = numberOfSections;
            TimeDateStamp = timeDateStamp;
            PointerToSymbolTable = pointerToSymbolTable;
            NumberOfSymbols = numberOfSymbols;
            SizeOfOptionalHeader = sizeOfOptionalHeader;
            Characteristics = characteristics;
        }
    }
}
