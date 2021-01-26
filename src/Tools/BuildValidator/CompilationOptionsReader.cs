// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis;

namespace BuildValidator
{
    internal readonly struct SourceFileInfo
    {
        internal string SourceFilePath { get; }
        internal string HashAlgorithm { get; }
        internal byte[] Hash { get; }
        internal string SourceFileName => Path.GetFileName(SourceFilePath);

        internal SourceFileInfo(
            string sourceFilePath,
            string hashAlgorithm,
            byte[] hash)
        {
            SourceFilePath = sourceFilePath;
            HashAlgorithm = hashAlgorithm;
            Hash = hash;
        }
    }

    internal class CompilationOptionsReader
    {
        public static readonly Guid HashAlgorithmSha1 = unchecked(new Guid((int)0xff1816ec, (short)0xaa5e, 0x4d10, 0x87, 0xf7, 0x6f, 0x49, 0x63, 0x83, 0x34, 0x60));
        public static readonly Guid HashAlgorithmSha256 = unchecked(new Guid((int)0x8829d00f, 0x11b8, 0x4213, 0x87, 0x8b, 0x77, 0x0e, 0x85, 0x97, 0xac, 0x16));
        public static readonly Guid MetadataReferenceInfoGuid = new Guid("7E4D4708-096E-4C5C-AEDA-CB10BA6A740D");
        public static readonly Guid CompilationOptionsGuid = new Guid("B5FEEC05-8CD0-4A83-96DA-466284BB4BD8");
        public static readonly Guid EmbeddedSourceGuid = new Guid("0E8A571B-6926-466E-B4AD-8AB04611F5FE");
        public static readonly Guid SourceLinkGuid = new Guid("CC110556-A091-4D38-9FEC-25AB9A351A6A");

        private readonly MetadataReader _metadataReader;
        private readonly PEReader _peReader;

        private MetadataCompilationOptions? _metadataCompilationOptions;
        private ImmutableArray<MetadataReferenceInfo> _metadataReferenceInfo;

        public CompilationOptionsReader(MetadataReader metadataReader, PEReader peReader)
        {
            _metadataReader = metadataReader;
            _peReader = peReader;
        }

        public MetadataCompilationOptions GetMetadataCompilationOptions()
        {
            if (_metadataCompilationOptions is null)
            {
                var optionsBlob = GetCustomDebugInformationBlobReader(CompilationOptionsGuid);
                _metadataCompilationOptions = new MetadataCompilationOptions(ParseCompilationOptions(optionsBlob));
            }

            return _metadataCompilationOptions;
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

        public OutputKind GetOutputKind() =>
            (_metadataReader.DebugMetadataHeader is { } header && !header.EntryPoint.IsNil)
            ? OutputKind.ConsoleApplication
            : OutputKind.DynamicallyLinkedLibrary;

        public string? GetMainTypeName() => GetMainMethodInfo() is { } tuple
            ? tuple.MainTypeName
            : null;

        public string? GetMainMethodName() => GetMainMethodInfo() is { } tuple
            ? tuple.MainMethodName
            : null;

        private (string MainTypeName, string MainMethodName)? GetMainMethodInfo()
        {
            if (!(_metadataReader.DebugMetadataHeader is { } header) ||
                header.EntryPoint.IsNil)
            {
                return null;
            }

            var mdReader = _peReader.GetMetadataReader();
            var methodDefinition = mdReader.GetMethodDefinition(header.EntryPoint);
            var methodName = mdReader.GetString(methodDefinition.Name);
            var typeHandle = methodDefinition.GetDeclaringType();
            var typeDefinition = mdReader.GetTypeDefinition(typeHandle);
            var typeName = mdReader.GetString(typeDefinition.Name);
            if (!typeDefinition.Namespace.IsNil)
            {
                var namespaceName = mdReader.GetString(typeDefinition.Namespace);
                typeName = namespaceName + "." + typeName;
            }

            return (typeName, methodName);
        }

        public ImmutableArray<SourceFileInfo> GetSourceFileInfos()
        {
            // TODO: can we give this utility an IVT to roslyn so it can just read these constants.
            // Alternatively, since we consider the constants to be stable, can we make them public API?
            var sourceFileCount = int.Parse(
                GetMetadataCompilationOptions()
                    .GetUniqueOption("source-file-count"));

            var builder = ImmutableArray.CreateBuilder<SourceFileInfo>(sourceFileCount);
            foreach (var documentHandle in _metadataReader.Documents.Take(sourceFileCount))
            {
                var document = _metadataReader.GetDocument(documentHandle);
                var name = _metadataReader.GetString(document.Name);
                var hashAlgorithmGuid = _metadataReader.GetGuid(document.HashAlgorithm);
                string hashAlgorithm;
                if (hashAlgorithmGuid == HashAlgorithmSha1)
                {
                    hashAlgorithm = "SHA1";
                }
                else if (hashAlgorithmGuid == HashAlgorithmSha256)
                {
                    hashAlgorithm = "SHA256A";
                }
                else
                {
                    hashAlgorithm = $"Unknown {hashAlgorithmGuid}";
                }
                var hash = _metadataReader.GetBlobBytes(document.Hash);
                builder.Add(new SourceFileInfo(name, hashAlgorithm, hash));
            }

            return builder.MoveToImmutable();
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
