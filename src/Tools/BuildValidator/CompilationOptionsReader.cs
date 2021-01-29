// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Newtonsoft.Json;

namespace BuildValidator
{
    internal readonly struct SourceFileInfo
    {
        internal string SourceFilePath { get; }
        internal SourceHashAlgorithm HashAlgorithm { get; }
        internal byte[] Hash { get; }
        internal SourceText? EmbeddedText { get; }

        internal string SourceFileName => Path.GetFileName(SourceFilePath);
        internal string HashAlgorithmDescription
        {
            get
            {
                //string hashAlgorithmDescription;
                //if (HashAlgorithm == CompilationOptionsReader.HashAlgorithmSha1)
                //{
                //    hashAlgorithmDescription = "SHA1";
                //}
                //else if (HashAlgorithm == CompilationOptionsReader.HashAlgorithmSha256)
                //{
                //    hashAlgorithmDescription = "SHA256A";
                //}
                //else
                //{
                //    hashAlgorithmDescription = $"Unknown {HashAlgorithm}";
                //}
                return HashAlgorithm.ToString();
            }
        }

        internal SourceFileInfo(
            string sourceFilePath,
            SourceHashAlgorithm hashAlgorithm,
            byte[] hash,
            SourceText? embeddedText)
        {
            SourceFilePath = sourceFilePath;
            HashAlgorithm = hashAlgorithm;
            Hash = hash;
            EmbeddedText = embeddedText;
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

        private readonly MetadataReader _pdbReader;
        private readonly PEReader _peReader;

        private MetadataCompilationOptions? _metadataCompilationOptions;
        private ImmutableArray<MetadataReferenceInfo> _metadataReferenceInfo;
        private byte[]? _sourceLinkUTF8;

        public CompilationOptionsReader(MetadataReader pdbReader, PEReader peReader)
        {
            _pdbReader = pdbReader;
            _peReader = peReader;
        }

        public BlobReader GetMetadataCompilationOptionsBlobReader()
        {
            if (!TryGetCustomDebugInformationBlobReader(CompilationOptionsGuid, out var optionsBlob))
                throw new Exception();

            return optionsBlob;
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
                    throw new Exception();

                _metadataReferenceInfo = ParseMetadataReferenceInfo(referencesBlob).ToImmutableArray();
            }

            return _metadataReferenceInfo;
        }

        public OutputKind GetOutputKind() =>
            (_pdbReader.DebugMetadataHeader is { } header && !header.EntryPoint.IsNil)
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
            if (!(_pdbReader.DebugMetadataHeader is { } header) ||
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

        private SourceText? ResolveEmbeddedSource(DocumentHandle document, SourceHashAlgorithm hashAlgorithm, Encoding encoding)
        {
            byte[] bytes = (from handle in _pdbReader.GetCustomDebugInformation(document)
                            let cdi = _pdbReader.GetCustomDebugInformation(handle)
                            where _pdbReader.GetGuid(cdi.Kind) == EmbeddedSourceGuid
                            select _pdbReader.GetBlobBytes(cdi.Value)).SingleOrDefault();

            if (bytes == null)
            {
                return null;
            }

            int uncompressedSize = BitConverter.ToInt32(bytes, 0);
            var stream = new MemoryStream(bytes, sizeof(int), bytes.Length - sizeof(int));

            if (uncompressedSize != 0)
            {
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
                return SourceText.From(stream, encoding: encoding, checksumAlgorithm: hashAlgorithm, canBeEmbedded: true);
            }
        }

        public byte[]? GetPublicKey()
        {
            var metadataReader = _peReader.GetMetadataReader();
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
            var metadataReader = _peReader.GetMetadataReader();
            if (_peReader.PEHeaders.CorHeader is not { } corHeader
                || !_peReader.PEHeaders.TryGetDirectoryOffset(corHeader.ResourcesDirectory, out var resourcesOffset))
            {
                return null;
            }

            var result = metadataReader.ManifestResources.Select(handle =>
            {
                var resource = metadataReader.GetManifestResource(handle);
                var name = metadataReader.GetString(resource.Name);

                var resourceStart = _peReader.GetEntireImage().Pointer + resourcesOffset + resource.Offset;
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
            // TODO: can we give this utility an IVT to roslyn so it can just read these constants.
            // Alternatively, since we consider the constants to be stable, can we make them public API?
            var sourceFileCount = int.Parse(
                GetMetadataCompilationOptions()
                    .GetUniqueOption("source-file-count"));

            var builder = ImmutableArray.CreateBuilder<SourceFileInfo>(sourceFileCount);
            foreach (var documentHandle in _pdbReader.Documents.Take(sourceFileCount))
            {
                var document = _pdbReader.GetDocument(documentHandle);
                var name = _pdbReader.GetString(document.Name);
                
                var hashAlgorithmGuid = _pdbReader.GetGuid(document.HashAlgorithm);
                var hashAlgorithm =
                    hashAlgorithmGuid == HashAlgorithmSha1 ? SourceHashAlgorithm.Sha1
                    : hashAlgorithmGuid == HashAlgorithmSha256 ? SourceHashAlgorithm.Sha256
                    : SourceHashAlgorithm.None;

                var hash = _pdbReader.GetBlobBytes(document.Hash);
                var embeddedContent = ResolveEmbeddedSource(documentHandle, hashAlgorithm, encoding);

                builder.Add(new SourceFileInfo(name, hashAlgorithm, hash, embeddedContent));
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
            var blobs = from cdiHandle in _pdbReader.GetCustomDebugInformation(EntityHandle.ModuleDefinition)
                        let cdi = _pdbReader.GetCustomDebugInformation(cdiHandle)
                        where _pdbReader.GetGuid(cdi.Kind) == infoGuid
                        select _pdbReader.GetBlobReader(cdi.Value);

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
