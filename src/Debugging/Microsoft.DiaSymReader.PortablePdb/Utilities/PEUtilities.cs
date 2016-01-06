// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace System.Reflection.PortableExecutable
{
    // TODO: use impl from System.Reflection.Metadata v1.2

    internal enum DebugDirectoryEntryType
    {
        /// <summary>
        /// An unknown value that is ignored by all tools.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// The COFF debug information (line numbers, symbol table, and string table). 
        /// This type of debug information is also pointed to by fields in the file headers.
        /// </summary>
        Coff = 1,

        /// <summary>
        /// Associated PDB file description.
        /// </summary>
        CodeView = 2,
    }

    internal struct CodeViewDebugDirectoryData
    {
        public string Path { get; }
        public Guid PdbId { get; }
        public int Age { get; }

        public CodeViewDebugDirectoryData(string path, Guid pdbId, int age)
        {
            Path = path;
            PdbId = pdbId;
            Age = age;
        }
    }

    internal struct DebugDirectoryEntry
    {
        public uint Stamp { get; }
        public ushort MajorVersion { get; }
        public ushort MinorVersion { get; }
        public DebugDirectoryEntryType EntryType { get; }
        public int DataSize { get; }
        public int DataRelativeVirtualAddress { get; }
        public int DataPointer { get; }

        public DebugDirectoryEntry(
            uint stamp,
            ushort majorVersion,
            ushort minorVersion,
            DebugDirectoryEntryType entryType,
            int dataSize,
            int dataRelativeVirtualAddress,
            int dataPointer)
        {
            Stamp = stamp;
            MajorVersion = majorVersion;
            MinorVersion = minorVersion;
            EntryType = entryType;
            DataSize = dataSize;
            DataRelativeVirtualAddress = dataRelativeVirtualAddress;
            DataPointer = dataPointer;
        }
    }

    internal static class PEUtilities
    {
        public static ImmutableArray<DebugDirectoryEntry> ReadDebugDirectory(PEReader peReader, Stream peStream)
        {
            var debugDirectory = peReader.PEHeaders.PEHeader.DebugTableDirectory;

            // TODO: Add API to PEReader to get a memory block for a directory

            int position;
            if (!peReader.PEHeaders.TryGetDirectoryOffset(debugDirectory, out position))
            {
                throw new BadImageFormatException();
            }

            const int entrySize = 0x1c;

            if (debugDirectory.Size % entrySize != 0)
            {
                throw new BadImageFormatException();
            }

            peStream.Position = position;
            var reader = new BinaryReader(peStream);

            int entryCount = debugDirectory.Size / entrySize;
            var builder = ImmutableArray.CreateBuilder<DebugDirectoryEntry>(entryCount);
            for (int i = 0; i < entryCount; i++)
            {
                // Reserved, must be zero.
                int characteristics = reader.ReadInt32();
                if (characteristics != 0)
                {
                    throw new BadImageFormatException();
                }

                uint stamp = reader.ReadUInt32();
                ushort majorVersion = reader.ReadUInt16();
                ushort minorVersion = reader.ReadUInt16();

                var type = (DebugDirectoryEntryType)reader.ReadInt32();

                int dataSize = reader.ReadInt32();
                int dataRva = reader.ReadInt32();
                int dataPointer = reader.ReadInt32();

                builder.Add(new DebugDirectoryEntry(stamp, majorVersion, minorVersion, type, dataSize, dataRva, dataPointer));
            }

            return builder.MoveToImmutable();
        }

        public static CodeViewDebugDirectoryData ReadCodeViewDebugDirectoryData(Stream peStream, DebugDirectoryEntry entry)
        {
            var reader = new BinaryReader(peStream);
            peStream.Position = entry.DataPointer;

            if (reader.ReadByte() != (byte)'R' ||
                reader.ReadByte() != (byte)'S' ||
                reader.ReadByte() != (byte)'D' ||
                reader.ReadByte() != (byte)'S')
            {
                throw new BadImageFormatException();
            }

            byte[] guidBlob = new byte[16];
            reader.Read(guidBlob, 0, guidBlob.Length);

            int age = reader.ReadInt32();

            byte[] pathBlob = new byte[entry.DataSize - 24];
            reader.Read(pathBlob, 0, pathBlob.Length);

            int terminator = Array.IndexOf(pathBlob, (byte)0);
            if (terminator < 0)
            {
                throw new BadImageFormatException("Path should be NUL terminated");
            }

            for (int i = terminator + 1; i < pathBlob.Length; i++)
            {
                if (pathBlob[i] != 0)
                {
                    throw new BadImageFormatException();
                }
            }

            // TODO: handle unpaired surrogates
            string path = Encoding.UTF8.GetString(pathBlob, 0, terminator);
            return new CodeViewDebugDirectoryData(path, new Guid(guidBlob), age);
        }
    }
}
