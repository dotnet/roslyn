// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Microsoft.CodeAnalysis.BuildTasks
{
    public sealed class MvidReader : BinaryReader
    {
        public MvidReader(Stream stream) : base(stream)
        {
        }

        public static Guid ReadAssemblyMvidOrEmpty(Stream stream)
        {
            try
            {
                var mvidReader = new MvidReader(stream);
                return mvidReader.TryFindMvid();
            }
            catch (EndOfStreamException)
            {
            }

            return Guid.Empty;
        }

        public Guid TryFindMvid()
        {
            Guid empty = Guid.Empty;

            // DOS Header (64), DOS Stub (64), PE Signature (4), COFF Header (20), PE Header (224)
            if (BaseStream.Length < 64 + 64 + 4 + 20 + 224)
            {
                return empty;
            }

            // DOS Header: PE (2)
            if (ReadUInt16() != 0x5a4d)
            {
                return empty;
            }

            // DOS Header: Start (58)
            Skip(58);

            // DOS Header: Address of PE Signature
            MoveTo(ReadUInt32());

            // PE Signature ('P' 'E' null null)
            if (ReadUInt32() != 0x00004550)
            {
                return empty;
            }

            // COFF Header: Machine (2)
            Skip(2);

            // COFF Header: NumberOfSections (2)
            ushort sections = ReadUInt16();

            // COFF Header: TimeDateStamp (4), PointerToSymbolTable (4), NumberOfSymbols (4)
            Skip(12);

            // COFF Header: OptionalHeaderSize (2)
            ushort optionalHeaderSize = ReadUInt16();

            // COFF Header: Characteristics (2)
            Skip(2);

            // Optional header
            Skip(optionalHeaderSize);

            // Section headers
            return FindMvidInSections(sections);
        }

        private Guid FindMvidInSections(ushort count)
        {
            for (int i = 0; i < count; i++)
            {
                // Section: Name (8)
                byte[] name = ReadBytes(8);
                if (name.Length == 8 && name[0] == '.' &&
                    name[1] == 'm' && name[2] == 'v' && name[3] == 'i' && name[4] == 'd' && name[5] == '\0')
                {
                    // Section: VirtualSize (4)
                    uint virtualSize = ReadUInt32();

                    // Section: VirtualAddress (4), SizeOfRawData (4)
                    Skip(8);

                    // Section: PointerToRawData (4)
                    uint pointerToRawData = ReadUInt32();

                    // The .mvid section only stores a Guid
                    Debug.Assert(virtualSize == 16);

                    BaseStream.Position = pointerToRawData;
                    byte[] guidBytes = new byte[16];
                    BaseStream.Read(guidBytes, 0, 16);

                    return new Guid(guidBytes);
                }

                // Section: VirtualSize (4), VirtualAddress (4), SizeOfRawData (4),
                // PointerToRawData (4), PointerToRelocations (4), PointerToLineNumbers (4),
                // NumberOfRelocations (2), NumberOfLineNumbers (2), Characteristics (4)
                Skip(4 + 4 + 4 + 4 + 4 + 4 + 2 + 2 + 4);
            }

            return Guid.Empty;
        }

        public void Skip(int bytes)
        {
            BaseStream.Seek(bytes, SeekOrigin.Current);
        }

        public void MoveTo(uint position)
        {
            BaseStream.Seek(position, SeekOrigin.Begin);
        }
    }
}
