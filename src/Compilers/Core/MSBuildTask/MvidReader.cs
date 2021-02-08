// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Microsoft.CodeAnalysis.BuildTasks
{
    public static class MvidReader
    {
        private static readonly Guid s_empty = Guid.Empty;

        public static Guid ReadAssemblyMvidOrEmpty(Stream stream)
        {
            return ReadAssemblyMvidOrEmpty(new BinaryReader(stream));
        }

        private static Guid ReadAssemblyMvidOrEmpty(BinaryReader reader)
        {
            // DOS Header: Magic number (2)
            if (!ReadUInt16(reader, out ushort magicNumber) || magicNumber != 0x5a4d) // "MZ"
            {
                return s_empty;
            }

            // DOS Header: Address of PE Signature (at 0x3C)
            if (!MoveTo(0x3C, reader))
            {
                return s_empty;
            }
            if (!ReadUInt32(reader, out uint pointerToPeSignature))
            {
                return s_empty;
            }

            // jump over the MS DOS Stub to the PE Signature
            if (!MoveTo(pointerToPeSignature, reader))
            {
                return s_empty;
            }

            // PE Signature ('P' 'E' null null)
            if (!ReadUInt32(reader, out uint peSig) || peSig != 0x00004550)
            {
                return s_empty;
            }

            // COFF Header: Machine (2)
            if (!Skip(2, reader))
            {
                return s_empty;
            }

            // COFF Header: NumberOfSections (2)
            if (!ReadUInt16(reader, out ushort sections))
            {
                return s_empty;
            }

            // COFF Header: TimeDateStamp (4), PointerToSymbolTable (4), NumberOfSymbols (4)
            if (!Skip(12, reader))
            {
                return s_empty;
            }

            // COFF Header: OptionalHeaderSize (2)
            if (!ReadUInt16(reader, out ushort optionalHeaderSize))
            {
                return s_empty;
            }

            // COFF Header: Characteristics (2)
            if (!Skip(2, reader))
            {
                return s_empty;
            }

            // Optional header
            if (!Skip(optionalHeaderSize, reader))
            {
                return s_empty;
            }

            // Section headers
            return FindMvidInSections(sections, reader);
        }

        private static Guid FindMvidInSections(ushort count, BinaryReader reader)
        {
            for (int i = 0; i < count; i++)
            {
                // Section: Name (8)
                if (!ReadBytes(reader, 8, out byte[]? name))
                {
                    return s_empty;
                }

                if (name!.Length == 8 && name[0] == '.' &&
                    name[1] == 'm' && name[2] == 'v' && name[3] == 'i' && name[4] == 'd' && name[5] == '\0')
                {
                    // Section: VirtualSize (4)
                    if (!ReadUInt32(reader, out uint virtualSize) || virtualSize != 16)
                    {
                        // The .mvid section only stores a Guid
                        return s_empty;
                    }

                    // Section: VirtualAddress (4), SizeOfRawData (4)
                    if (!Skip(8, reader))
                    {
                        return s_empty;
                    }

                    // Section: PointerToRawData (4)
                    if (!ReadUInt32(reader, out uint pointerToRawData))
                    {
                        return s_empty;
                    }

                    return ReadMvidSection(reader, pointerToRawData);
                }
                else
                {
                    // Section: VirtualSize (4), VirtualAddress (4), SizeOfRawData (4),
                    // PointerToRawData (4), PointerToRelocations (4), PointerToLineNumbers (4),
                    // NumberOfRelocations (2), NumberOfLineNumbers (2), Characteristics (4)
                    if (!Skip(4 + 4 + 4 + 4 + 4 + 4 + 2 + 2 + 4, reader))
                    {
                        return s_empty;
                    }
                }
            }

            return s_empty;
        }

        private static Guid ReadMvidSection(BinaryReader reader, uint pointerToMvidSection)
        {
            if (!MoveTo(pointerToMvidSection, reader))
            {
                return s_empty;
            }

            if (!ReadBytes(reader, 16, out byte[]? guidBytes))
            {
                return s_empty;
            }

            return new Guid(guidBytes!);
        }

        private static bool ReadUInt16(BinaryReader reader, out ushort output)
        {
            if (reader.BaseStream.Position + 2 >= reader.BaseStream.Length)
            {
                output = 0;
                return false;
            }

            output = reader.ReadUInt16();
            return true;
        }

        private static bool ReadUInt32(BinaryReader reader, out uint output)
        {
            if (reader.BaseStream.Position + 4 >= reader.BaseStream.Length)
            {
                output = 0;
                return false;
            }

            output = reader.ReadUInt32();
            return true;
        }

        private static bool ReadBytes(BinaryReader reader, int count, out byte[]? output)
        {
            if (reader.BaseStream.Position + count >= reader.BaseStream.Length)
            {
                output = null;
                return false;
            }

            output = reader.ReadBytes(count);
            return true;
        }

        private static bool Skip(int bytes, BinaryReader reader)
        {
            if (reader.BaseStream.Position + bytes >= reader.BaseStream.Length)
            {
                return false;
            }

            reader.BaseStream.Seek(bytes, SeekOrigin.Current);
            return true;
        }

        private static bool MoveTo(uint position, BinaryReader reader)
        {
            if (position >= reader.BaseStream.Length)
            {
                return false;
            }

            reader.BaseStream.Seek(position, SeekOrigin.Begin);
            return true;
        }
    }
}
