// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis;

namespace BuildValidator
{
    internal class CompilationOptionsReader
    {
        public static readonly Guid MetadataReferenceInfoGuid = new Guid("7E4D4708-096E-4C5C-AEDA-CB10BA6A740D");
        public static readonly Guid CompilationOptionsGuid = new Guid("B5FEEC05-8CD0-4A83-96DA-466284BB4BD8");
        public static readonly Guid EmbeddedSourceGuid = new Guid("0E8A571B-6926-466E-B4AD-8AB04611F5FE");
        public static readonly Guid SourceLinkGuid = new Guid("CC110556-A091-4D38-9FEC-25AB9A351A6A");

        private readonly MetadataReader _metadataReader;

        private MetadataCompilationOptions? _compilationOptions;
        private ImmutableArray<MetadataReferenceInfo> _metadataReferenceInfo;

        public CompilationOptionsReader(MetadataReader metadataReader)
        {
            _metadataReader = metadataReader;
        }

        public MetadataCompilationOptions GetCompilationOptions()
        {
            if (_compilationOptions is null)
            {
                var optionsBlob = GetCustomDebugInformationBlobReader(CompilationOptionsGuid);
                _compilationOptions = new MetadataCompilationOptions(ParseCompilationOptions(optionsBlob));
            }

            return _compilationOptions;
        }

        public ImmutableArray<MetadataReferenceInfo> GetMetadataReferences()
        {
            if (_metadataReferenceInfo.IsDefault)
            {
                var referencesBlob = GetCustomDebugInformationBlobReader(MetadataReferenceInfoGuid);
                _metadataReferenceInfo = ParseMetadataReferenceInfo(referencesBlob).ToImmutableArray();
            }

            return _metadataReferenceInfo;
        }

        public OutputKind GetOutputKind() => OutputKind.DynamicallyLinkedLibrary;

        public IEnumerable<string> GetSourceFileNames()
        {
            foreach (var documentHandle in _metadataReader.Documents)
            {
                var document = _metadataReader.GetDocument(documentHandle);
                yield return _metadataReader.GetString(document.Name);
            }
        }

        private static IEnumerable<MetadataReferenceInfo> ParseMetadataReferenceInfo(BlobReader blobReader)
        {
            while (blobReader.RemainingBytes > 0)
            {
                // Order of information
                // File name (null terminated string): A.exe
                // Extern Alias (null terminated string): a1,a2,a3
                // EmbedInteropTypes/MetadataImageKind (byte)
                // COFF header Timestamp field (4 byte int)
                // COFF header SizeOfImage field (4 byte int)
                // MVID (Guid, 24 bytes)

                var terminatorIndex = blobReader.IndexOf(0);

                var name = blobReader.ReadUTF8(terminatorIndex);

                blobReader.SkipNullTerminator();

                terminatorIndex = blobReader.IndexOf(0);

                var externAliases = blobReader.ReadUTF8(terminatorIndex);

                blobReader.SkipNullTerminator();

                var embedInteropTypesAndKind = blobReader.ReadByte();

                // Only the last two bits are used, verify nothing else in the 
                // byte has data. 
                if ((embedInteropTypesAndKind & 0b11111100) != 0)
                {
                    throw new InvalidDataException($"Unexpected value for EmbedInteropTypes/MetadataImageKind {embedInteropTypesAndKind}");
                }

                var embedInteropTypes = (embedInteropTypesAndKind & 0b10) == 0b10;
                var kind = (embedInteropTypesAndKind & 0b1) == 0b1
                    ? MetadataImageKind.Assembly
                    : MetadataImageKind.Module;

                var timestamp = blobReader.ReadInt32();
                var imageSize = blobReader.ReadInt32();
                var mvid = blobReader.ReadGuid();

                yield return new MetadataReferenceInfo(
                    timestamp,
                    imageSize,
                    name,
                    mvid,
                    string.IsNullOrEmpty(externAliases)
                        ? ImmutableArray<string>.Empty
                        : externAliases.Split(',').ToImmutableArray(),
                    kind,
                    embedInteropTypes);
            }
        }

        private BlobReader GetCustomDebugInformationBlobReader(Guid infoGuid)
        {
            var blobs = from cdiHandle in _metadataReader.GetCustomDebugInformation(EntityHandle.ModuleDefinition)
                        let cdi = _metadataReader.GetCustomDebugInformation(cdiHandle)
                        where _metadataReader.GetGuid(cdi.Kind) == infoGuid
                        select _metadataReader.GetBlobReader(cdi.Value);

            if (blobs.Any())
            {
                return blobs.Single();
            }

            throw new InvalidDataException($"No blob found for {infoGuid}");
        }

        private static ImmutableArray<(string, string)> ParseCompilationOptions(BlobReader blobReader)
        {

            // Compiler flag bytes are UTF-8 null-terminated key-value pairs
            string? key = null;
            List<(string, string)> options = new List<(string, string)>();
            for (; ; )
            {
                var nullIndex = blobReader.IndexOf(0);
                if (nullIndex == -1)
                {
                    break;
                }

                var value = blobReader.ReadUTF8(nullIndex);

                blobReader.SkipNullTerminator();

                if (key is null)
                {
                    if (value is null or { Length: 0 })
                    {
                        throw new InvalidDataException("Encountered null or empty key for compilation options pairs");
                    }

                    key = value;
                }
                else
                {
                    options.Add((key, value));
                    key = null;
                }
            }

            return options.ToImmutableArray();
        }
    }
}
