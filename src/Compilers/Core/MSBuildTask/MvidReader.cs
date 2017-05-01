// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;

namespace Microsoft.CodeAnalysis.BuildTasks
{
    public static class MvidReader
    {
        public static Guid ReadAssemblyMvidOrEmpty(Stream stream)
        {
            return ReadAssemblyMvidOrEmpty(new BinaryReader(stream));
        }

        private static Guid ReadAssemblyMvidOrEmpty(BinaryReader reader)
        {
            Guid empty = Guid.Empty;

            // DOS Header (64), DOS Stub (64), PE Signature (4), COFF Header (20), PE Header (224), 1x Section Header (40)
            if (reader.BaseStream.Length < 64 + 64 + 4 + 20 + 224 + 40)
            {
                return empty;
            }

            // DOS Header: Magic number (2)
            if (reader.ReadUInt16() != 0x5a4d) // "MZ"
            {
                return empty;
            }

            // DOS Header: Address of PE Signature (at 0x3C)
            MoveTo(0x3C, reader);
            uint lfanew = reader.ReadUInt32();

            MoveTo(lfanew, reader); // jump over the MS DOS Stub to the PE Signature

            // PE Signature ('P' 'E' null null)
            if (reader.ReadUInt32() != 0x00004550)
            {
                return empty;
            }

            // COFF Header: Machine (2)
            Skip(2, reader);

            // COFF Header: NumberOfSections (2)
            ushort sections = reader.ReadUInt16();

            // COFF Header: TimeDateStamp (4), PointerToSymbolTable (4), NumberOfSymbols (4)
            Skip(12, reader);

            // COFF Header: OptionalHeaderSize (2)
            ushort optionalHeaderSize = reader.ReadUInt16();

            // COFF Header: Characteristics (2)
            Skip(2, reader);

            // Optional header
            Skip(optionalHeaderSize, reader);

            // Section headers
            return FindMvidInSections(sections, reader);
        }

        private static Guid FindMvidInSections(ushort count, BinaryReader reader)
        {
            if (count == 0)
            {
                return Guid.Empty;
            }

            // .mvid section must be first, if it's there
            // Section: Name (8)
            byte[] name = reader.ReadBytes(8);
            if (name.Length == 8 && name[0] == '.' &&
                name[1] == 'm' && name[2] == 'v' && name[3] == 'i' && name[4] == 'd' && name[5] == '\0')
            {
                // Section: VirtualSize (4)
                uint virtualSize = reader.ReadUInt32();

                // Section: VirtualAddress (4), SizeOfRawData (4)
                Skip(8, reader);

                // Section: PointerToRawData (4)
                uint pointerToRawData = reader.ReadUInt32();

                // The .mvid section only stores a Guid
                if (virtualSize != 16)
                {
                    Debug.Assert(false);
                    return Guid.Empty;
                }

                if (MoveTo(pointerToRawData, reader))
                {
                    byte[] guidBytes = new byte[16];
                    if (reader.BaseStream.Read(guidBytes, 0, 16) == 16)
                    {
                        return new Guid(guidBytes);
                    }
                }
            }

            return Guid.Empty;
        }

        private static void Skip(int bytes, BinaryReader reader)
        {
            reader.BaseStream.Seek(bytes, SeekOrigin.Current);
        }

        private static bool MoveTo(uint position, BinaryReader reader)
        {
            if (reader.BaseStream.Length < position)
            {
                return false;
            }

            reader.BaseStream.Seek(position, SeekOrigin.Begin);
            return true;
        }
    }
}
