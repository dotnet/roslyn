// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Roslyn.Utilities;

namespace Roslyn.Reflection.PortableExecutable
{
    // TODO: move to SRM: https://github.com/dotnet/roslyn/issues/24712
    internal static class DebugDirectoryExtensions
    {
        private static FieldInfo s_dataBuilderField;
        private static Action<DebugDirectoryBuilder, DebugDirectoryEntryType, uint, uint, int> s_addEntry;

        internal const DebugDirectoryEntryType PdbChecksumEntryType = (DebugDirectoryEntryType)19;

        private static void InitializeReflection()
        {
            if (s_dataBuilderField == null)
            {
                var type = typeof(DebugDirectoryBuilder).GetTypeInfo();
                s_dataBuilderField = type.GetDeclaredField("_dataBuilder");
                s_addEntry = (Action<DebugDirectoryBuilder, DebugDirectoryEntryType, uint, uint, int>)
                    type.GetDeclaredMethod("AddEntry", typeof(DebugDirectoryEntryType), typeof(uint), typeof(uint), typeof(int)).
                    CreateDelegate(typeof(Action<DebugDirectoryBuilder, DebugDirectoryEntryType, uint, uint, int>));
            }
        }

        public static void AddPdbChecksumEntry(this DebugDirectoryBuilder builder, string algorithmName, ImmutableArray<byte> checksum)
        {
            InitializeReflection();
            int dataSize = WritePdbChecksumData((BlobBuilder)s_dataBuilderField.GetValue(builder), algorithmName, checksum);
            s_addEntry(builder, PdbChecksumEntryType, 0x00000001, 0x00000000, dataSize);
        }

        private static int WritePdbChecksumData(BlobBuilder builder, string algorithmName, ImmutableArray<byte> checksum)
        {
            int start = builder.Count;

            // NUL-terminated algorithm name:
            builder.WriteUTF8(algorithmName, allowUnpairedSurrogates: true);
            builder.WriteByte(0);

            // checksum:
            builder.WriteBytes(checksum);

            return builder.Count - start;
        }

        /// <summary>
        /// Reads the data pointed to by the specified Debug Directory entry and interprets them as PDB Checksum entry.
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="entry"/> is not a PDB Checksum entry.</exception>
        /// <exception cref="BadImageFormatException">Bad format of the data.</exception>
        /// <exception cref="IOException">IO error while reading from the underlying stream.</exception>
        /// <exception cref="InvalidOperationException">PE image not available.</exception>
        internal static PdbChecksumDebugDirectoryData ReadPdbChecksumDebugDirectoryData(this PEReader peReader, DebugDirectoryEntry entry)
        {
            if (entry.Type != PdbChecksumEntryType)
            {
                throw new ArgumentException("Unexpected debug directory entry type", nameof(entry));
            }

            var peImage = peReader.GetEntireImage();

            int dataOffset = peReader.IsLoadedImage ? entry.DataRelativeVirtualAddress : entry.DataPointer;
            var reader = peImage.GetReader(dataOffset, entry.DataSize);

            int nameLength = reader.IndexOf(0);
            if (nameLength <= 0)
            {
                throw new BadImageFormatException("Invalid PDB Checksum data format");
            }

            string algorithmName = reader.ReadUTF8(nameLength);

            // NUL
            reader.Offset += 1;

            var checksum = reader.ReadBytes(reader.RemainingBytes).ToImmutableArray();
            if (checksum.Length == 0)
            {
                throw new BadImageFormatException("Invalid PDB Checksum data format");
            }

            return new PdbChecksumDebugDirectoryData(algorithmName, checksum);
        }
    }

    internal readonly struct PdbChecksumDebugDirectoryData
    {
        /// <summary>
        /// Checksum algorithm name.
        /// </summary>
        public string AlgorithmName { get; }

        /// <summary>
        /// Checksum of the associated PDB.
        /// </summary>
        public ImmutableArray<byte> Checksum { get; }

        internal PdbChecksumDebugDirectoryData(string algorithmName, ImmutableArray<byte> checksum)
        {
            Debug.Assert(!string.IsNullOrEmpty(algorithmName));
            Debug.Assert(!checksum.IsDefaultOrEmpty);

            AlgorithmName = algorithmName;
            Checksum = checksum;
        }
    }
}
