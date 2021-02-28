// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Cci;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace BuildValidator
{
    internal readonly struct SourceFileInfo
    {
        internal string SourceFilePath { get; }
        internal SourceHashAlgorithm HashAlgorithm { get; }
        internal byte[] Hash { get; }
        internal SourceText? EmbeddedText { get; }
        internal byte[]? EmbeddedCompressedHash { get; }

        internal SourceFileInfo(
            string sourceFilePath,
            SourceHashAlgorithm hashAlgorithm,
            byte[] hash,
            SourceText? embeddedText,
            byte[]? embeddedCompressedHash)
        {
            SourceFilePath = sourceFilePath;
            HashAlgorithm = hashAlgorithm;
            Hash = hash;
            EmbeddedText = embeddedText;
            EmbeddedCompressedHash = embeddedCompressedHash;
        }
    }

    internal class CompilationOptionsReader
    {
        // GUIDs specified in https://github.com/dotnet/runtime/blob/master/docs/design/specs/PortablePdb-Metadata.md#document-table-0x30
        public static readonly Guid HashAlgorithmSha1 = unchecked(new Guid((int)0xff1816ec, (short)0xaa5e, 0x4d10, 0x87, 0xf7, 0x6f, 0x49, 0x63, 0x83, 0x34, 0x60));
        public static readonly Guid HashAlgorithmSha256 = unchecked(new Guid((int)0x8829d00f, 0x11b8, 0x4213, 0x87, 0x8b, 0x77, 0x0e, 0x85, 0x97, 0xac, 0x16));

        // https://github.com/dotnet/runtime/blob/master/docs/design/specs/PortablePdb-Metadata.md#compilation-metadata-references-c-and-vb-compilers
        public static readonly Guid MetadataReferenceInfoGuid = new Guid("7E4D4708-096E-4C5C-AEDA-CB10BA6A740D");

        // https://github.com/dotnet/runtime/blob/master/docs/design/specs/PortablePdb-Metadata.md#compilation-options-c-and-vb-compilers
        public static readonly Guid CompilationOptionsGuid = new Guid("B5FEEC05-8CD0-4A83-96DA-466284BB4BD8");

        // https://github.com/dotnet/runtime/blob/master/docs/design/specs/PortablePdb-Metadata.md#embedded-source-c-and-vb-compilers
        public static readonly Guid EmbeddedSourceGuid = new Guid("0E8A571B-6926-466E-B4AD-8AB04611F5FE");

        // https://github.com/dotnet/runtime/blob/master/docs/design/specs/PortablePdb-Metadata.md#source-link-c-and-vb-compilers
        public static readonly Guid SourceLinkGuid = new Guid("CC110556-A091-4D38-9FEC-25AB9A351A6A");

        public MetadataReader PdbReader { get; }
        public PEReader PeReader { get; }
        private readonly ILogger _logger;

        private MetadataCompilationOptions? _metadataCompilationOptions;
        private ImmutableArray<MetadataReferenceInfo> _metadataReferenceInfo;
        private byte[]? _sourceLinkUTF8;

        public CompilationOptionsReader(ILogger logger, MetadataReader pdbReader, PEReader peReader)
        {
            _logger = logger;
            PdbReader = pdbReader;
            PeReader = peReader;
        }

        public bool TryGetMetadataCompilationOptionsBlobReader(out BlobReader reader)
        {
            return TryGetCustomDebugInformationBlobReader(CompilationOptionsGuid, out reader);
        }

        public BlobReader GetMetadataCompilationOptionsBlobReader()
        {
            if (!TryGetMetadataCompilationOptionsBlobReader(out var reader))
            {
                throw new InvalidOperationException();
            }
            return reader;
        }

        public bool TryGetMetadataCompilationOptions([NotNullWhen(true)] out MetadataCompilationOptions? options)
        {
            if (_metadataCompilationOptions is null && TryGetMetadataCompilationOptionsBlobReader(out var optionsBlob))
            {
                _metadataCompilationOptions = new MetadataCompilationOptions(ParseCompilationOptions(optionsBlob));
            }

            options = _metadataCompilationOptions;
            return options != null;
        }

        public MetadataCompilationOptions GetMetadataCompilationOptions()
        {
            if (_metadataCompilationOptions is null)
            {
                var optionsBlob = GetMetadataCompilationOptionsBlobReader();
                _metadataCompilationOptions = new MetadataCompilationOptions(ParseCompilationOptions(optionsBlob));
            }

            return _metadataCompilationOptions;
        }

        public Encoding GetEncoding()
        {
            using var scope = _logger.BeginScope("Encoding");

            var optionsReader = GetMetadataCompilationOptions();
            optionsReader.TryGetUniqueOption(_logger, "default-encoding", out var defaultEncoding);
            optionsReader.TryGetUniqueOption(_logger, "fallback-encoding", out var fallbackEncoding);

            var encodingString = defaultEncoding ?? fallbackEncoding;
            var encoding = encodingString is null
                ? Encoding.UTF8
                : Encoding.GetEncoding(encodingString);

            return encoding;
        }

        public ImmutableArray<SourceLink> GetSourceLinksOpt()
        {
            var sourceLinkUTF8 = GetSourceLinkUTF8();
            if (sourceLinkUTF8 is null)
            {
                return default;
            }

            var parseResult = JsonConvert.DeserializeAnonymousType(Encoding.UTF8.GetString(sourceLinkUTF8), new { documents = (Dictionary<string, string>?)null });
            return parseResult.documents.Select(makeSourceLink).ToImmutableArray();

            static SourceLink makeSourceLink(KeyValuePair<string, string> entry)
            {
                // TODO: determine if this subsitution is correct
                var (key, value) = (entry.Key, entry.Value); // TODO: use Deconstruct in .NET Core
                var prefix = key.Remove(key.LastIndexOf("*"));
                var replace = value.Remove(value.LastIndexOf("*"));
                return new SourceLink(prefix, replace);
            }
        }

        public byte[]? GetSourceLinkUTF8()
        {
            if (_sourceLinkUTF8 is null && TryGetCustomDebugInformationBlobReader(SourceLinkGuid, out var optionsBlob))
            {
                _sourceLinkUTF8 = optionsBlob.ReadBytes(optionsBlob.Length);
            }
            return _sourceLinkUTF8;
        }

        public ImmutableArray<MetadataReferenceInfo> GetMetadataReferences()
        {
            if (_metadataReferenceInfo.IsDefault)
            {
                if (!TryGetCustomDebugInformationBlobReader(MetadataReferenceInfoGuid, out var referencesBlob))
                {
                    throw new InvalidOperationException();
                }

                _metadataReferenceInfo = ParseMetadataReferenceInfo(referencesBlob).ToImmutableArray();
            }

            return _metadataReferenceInfo;
        }

        public OutputKind GetOutputKind() =>
            (PdbReader.DebugMetadataHeader is { } header && !header.EntryPoint.IsNil)
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
            if (!(PdbReader.DebugMetadataHeader is { } header) ||
                header.EntryPoint.IsNil)
            {
                return null;
            }

            var mdReader = PeReader.GetMetadataReader();
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

        private (SourceText? embeddedText, byte[]? compressedHash) ResolveEmbeddedSource(DocumentHandle document, SourceHashAlgorithm hashAlgorithm, Encoding encoding)
        {
            byte[] bytes = (from handle in PdbReader.GetCustomDebugInformation(document)
                            let cdi = PdbReader.GetCustomDebugInformation(handle)
                            where PdbReader.GetGuid(cdi.Kind) == EmbeddedSourceGuid
                            select PdbReader.GetBlobBytes(cdi.Value)).SingleOrDefault();

            if (bytes == null)
            {
                return default;
            }

            int uncompressedSize = BitConverter.ToInt32(bytes, 0);
            var stream = new MemoryStream(bytes, sizeof(int), bytes.Length - sizeof(int));

            byte[]? compressedHash = null;
            if (uncompressedSize != 0)
            {
                using var algorithm = CryptographicHashProvider.TryGetAlgorithm(hashAlgorithm) ?? throw new InvalidOperationException();
                compressedHash = algorithm.ComputeHash(bytes);

                var decompressed = new MemoryStream(uncompressedSize);

                using (var deflater = new DeflateStream(stream, CompressionMode.Decompress))
                {
                    deflater.CopyTo(decompressed);
                }

                if (decompressed.Length != uncompressedSize)
                {
                    throw new InvalidDataException();
                }

                stream = decompressed;
            }

            using (stream)
            {
                // todo: IVT and EncodedStringText.Create?
                var embeddedText = SourceText.From(stream, encoding: encoding, checksumAlgorithm: hashAlgorithm, canBeEmbedded: true);
                return (embeddedText, compressedHash);
            }
        }

        public byte[]? GetPublicKey()
        {
            var metadataReader = PeReader.GetMetadataReader();
            var blob = metadataReader.GetAssemblyDefinition().PublicKey;
            if (blob.IsNil)
            {
                return null;
            }

            var reader = metadataReader.GetBlobReader(blob);
            return reader.ReadBytes(reader.Length);
        }

        public unsafe ResourceDescription[]? GetManifestResources()
        {
            var metadataReader = PeReader.GetMetadataReader();
            if (PeReader.PEHeaders.CorHeader is not { } corHeader
                || !PeReader.PEHeaders.TryGetDirectoryOffset(corHeader.ResourcesDirectory, out var resourcesOffset))
            {
                return null;
            }

            var result = metadataReader.ManifestResources.Select(handle =>
            {
                var resource = metadataReader.GetManifestResource(handle);
                var name = metadataReader.GetString(resource.Name);

                var resourceStart = PeReader.GetEntireImage().Pointer + resourcesOffset + resource.Offset;
                var length = *(int*)resourceStart;
                var contentPtr = resourceStart + sizeof(int);
                var content = new byte[length];
                Marshal.Copy(new IntPtr(contentPtr), content, 0, length);

                var isPublic = (resource.Attributes & ManifestResourceAttributes.Public) != 0;
                var description = new ResourceDescription(name, dataProvider: () => new MemoryStream(content), isPublic);
                return description;
            }).ToArray();

            return result;
        }

        public ImmutableArray<SourceFileInfo> GetSourceFileInfos(Encoding encoding)
        {
            var sourceFileCount = int.Parse(
                GetMetadataCompilationOptions()
                    .GetUniqueOption(CompilationOptionNames.SourceFileCount));

            var builder = ImmutableArray.CreateBuilder<SourceFileInfo>(sourceFileCount);
            foreach (var documentHandle in PdbReader.Documents.Take(sourceFileCount))
            {
                var document = PdbReader.GetDocument(documentHandle);
                var name = PdbReader.GetString(document.Name);

                var hashAlgorithmGuid = PdbReader.GetGuid(document.HashAlgorithm);
                var hashAlgorithm =
                    hashAlgorithmGuid == HashAlgorithmSha1 ? SourceHashAlgorithm.Sha1
                    : hashAlgorithmGuid == HashAlgorithmSha256 ? SourceHashAlgorithm.Sha256
                    : SourceHashAlgorithm.None;

                var hash = PdbReader.GetBlobBytes(document.Hash);
                var embeddedContent = ResolveEmbeddedSource(documentHandle, hashAlgorithm, encoding);

                builder.Add(new SourceFileInfo(name, hashAlgorithm, hash, embeddedContent.embeddedText, embeddedContent.compressedHash));
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

        private bool TryGetCustomDebugInformationBlobReader(Guid infoGuid, out BlobReader blobReader)
        {
            var blobs = from cdiHandle in PdbReader.GetCustomDebugInformation(EntityHandle.ModuleDefinition)
                        let cdi = PdbReader.GetCustomDebugInformation(cdiHandle)
                        where PdbReader.GetGuid(cdi.Kind) == infoGuid
                        select PdbReader.GetBlobReader(cdi.Value);

            if (blobs.Any())
            {
                blobReader = blobs.Single();
                return true;
            }

            blobReader = default;
            return false;
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
