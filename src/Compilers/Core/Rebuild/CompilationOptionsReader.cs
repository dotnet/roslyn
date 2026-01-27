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
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Rebuild
{
    public class CompilationOptionsReader
    {
        // GUIDs specified in https://github.com/dotnet/runtime/blob/main/docs/design/specs/PortablePdb-Metadata.md#document-table-0x30
        public static readonly Guid HashAlgorithmSha1 = unchecked(new Guid((int)0xff1816ec, (short)0xaa5e, 0x4d10, 0x87, 0xf7, 0x6f, 0x49, 0x63, 0x83, 0x34, 0x60));
        public static readonly Guid HashAlgorithmSha256 = unchecked(new Guid((int)0x8829d00f, 0x11b8, 0x4213, 0x87, 0x8b, 0x77, 0x0e, 0x85, 0x97, 0xac, 0x16));

        // https://github.com/dotnet/runtime/blob/main/docs/design/specs/PortablePdb-Metadata.md#compilation-metadata-references-c-and-vb-compilers
        public static readonly Guid MetadataReferenceInfoGuid = new Guid("7E4D4708-096E-4C5C-AEDA-CB10BA6A740D");

        // https://github.com/dotnet/runtime/blob/main/docs/design/specs/PortablePdb-Metadata.md#compilation-options-c-and-vb-compilers
        public static readonly Guid CompilationOptionsGuid = new Guid("B5FEEC05-8CD0-4A83-96DA-466284BB4BD8");

        // https://github.com/dotnet/runtime/blob/main/docs/design/specs/PortablePdb-Metadata.md#embedded-source-c-and-vb-compilers
        public static readonly Guid EmbeddedSourceGuid = new Guid("0E8A571B-6926-466E-B4AD-8AB04611F5FE");

        // https://github.com/dotnet/runtime/blob/main/docs/design/specs/PortablePdb-Metadata.md#source-link-c-and-vb-compilers
        public static readonly Guid SourceLinkGuid = new Guid("CC110556-A091-4D38-9FEC-25AB9A351A6A");

        public MetadataReader PdbReader { get; }
        public PEReader PeReader { get; }
        private readonly ILogger _logger;

        public bool HasMetadataCompilationOptions => TryGetMetadataCompilationOptions(out _);

        private MetadataCompilationOptions? _metadataCompilationOptions;
        private byte[]? _sourceLinkUtf8;

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
                throw new InvalidOperationException(RebuildResources.Does_not_contain_metadata_compilation_options);
            }
            return reader;
        }

        internal bool TryGetMetadataCompilationOptions([NotNullWhen(true)] out MetadataCompilationOptions? options)
        {
            if (_metadataCompilationOptions is null && TryGetMetadataCompilationOptionsBlobReader(out var optionsBlob))
            {
                _metadataCompilationOptions = new MetadataCompilationOptions(ParseCompilationOptions(optionsBlob));
            }

            options = _metadataCompilationOptions;
            return options != null;
        }

        internal MetadataCompilationOptions GetMetadataCompilationOptions()
        {
            if (_metadataCompilationOptions is null)
            {
                var optionsBlob = GetMetadataCompilationOptionsBlobReader();
                _metadataCompilationOptions = new MetadataCompilationOptions(ParseCompilationOptions(optionsBlob));
            }

            return _metadataCompilationOptions;
        }

        /// <summary>
        /// Get the specified <see cref="LanguageNames"/> for this compilation.
        /// </summary>
        public string GetLanguageName()
        {
            var pdbCompilationOptions = GetMetadataCompilationOptions();
            if (!pdbCompilationOptions.TryGetUniqueOption(CompilationOptionNames.Language, out var language))
            {
                throw new Exception(RebuildResources.Invalid_language_name);
            }

            return language;
        }

        public Encoding GetEncoding()
        {
            using var scope = _logger.BeginScope("Encoding");

            var optionsReader = GetMetadataCompilationOptions();
            optionsReader.TryGetUniqueOption(_logger, CompilationOptionNames.DefaultEncoding, out var defaultEncoding);
            optionsReader.TryGetUniqueOption(_logger, CompilationOptionNames.FallbackEncoding, out var fallbackEncoding);

            var encodingString = defaultEncoding ?? fallbackEncoding;
            var encoding = encodingString is null
                ? Encoding.UTF8
                : Encoding.GetEncoding(encodingString);

            return encoding;
        }

        public byte[]? GetSourceLinkUtf8()
        {
            if (_sourceLinkUtf8 is null && TryGetCustomDebugInformationBlobReader(SourceLinkGuid, out var optionsBlob))
            {
                _sourceLinkUtf8 = optionsBlob.ReadBytes(optionsBlob.Length);
            }
            return _sourceLinkUtf8;
        }

        public string? GetMainTypeName() => GetMainMethodInfo()?.MainTypeName;

        public (string MainTypeName, string MainMethodName)? GetMainMethodInfo()
        {
            if (!(PdbReader.DebugMetadataHeader is { } header) ||
                header.EntryPoint.IsNil)
            {
                return null;
            }

            var mdReader = PeReader.GetMetadataReader();
            var methodDefinition = mdReader.GetMethodDefinition(header.EntryPoint);
            var methodName = mdReader.GetString(methodDefinition.Name);

            // Here we only want to give the caller the main method name and containing type name if the method is named "Main" per convention.
            // If the main method has another name, we have to assume that specifying a main type name won't work.
            // For example, if the compilation uses top-level statements.
            if (methodName != WellKnownMemberNames.EntryPointMethodName)
            {
                return null;
            }

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

        public int GetSourceFileCount()
            => int.Parse(GetMetadataCompilationOptions().GetUniqueOption(CompilationOptionNames.SourceFileCount));

        public IEnumerable<EmbeddedSourceTextInfo> GetEmbeddedSourceTextInfo()
            => GetSourceTextInfoCore()
                .Select(x => ResolveEmbeddedSource(x.DocumentHandle, x.SourceTextInfo))
                .WhereNotNull();

        private IEnumerable<(DocumentHandle DocumentHandle, SourceTextInfo SourceTextInfo)> GetSourceTextInfoCore()
        {
            var encoding = GetEncoding();
            var sourceFileCount = GetSourceFileCount();
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
                var sourceTextInfo = new SourceTextInfo(name, hashAlgorithm, hash.ToImmutableArray(), encoding);
                yield return (documentHandle, sourceTextInfo);
            }
        }

        private EmbeddedSourceTextInfo? ResolveEmbeddedSource(DocumentHandle document, SourceTextInfo sourceTextInfo)
        {
            var bytes = (from handle in PdbReader.GetCustomDebugInformation(document)
                         let cdi = PdbReader.GetCustomDebugInformation(handle)
                         where PdbReader.GetGuid(cdi.Kind) == EmbeddedSourceGuid
                         select PdbReader.GetBlobBytes(cdi.Value)).SingleOrDefault();

            if (bytes is null)
            {
                return null;
            }

            int uncompressedSize = BitConverter.ToInt32(bytes, 0);
            var stream = new MemoryStream(bytes, sizeof(int), bytes.Length - sizeof(int));

            byte[]? compressedHash = null;
            if (uncompressedSize != 0)
            {
                using var algorithm = CryptographicHashProvider.TryGetAlgorithm(sourceTextInfo.HashAlgorithm) ?? throw new InvalidOperationException();
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
                var embeddedText = SourceText.From(stream, encoding: sourceTextInfo.SourceTextEncoding, checksumAlgorithm: sourceTextInfo.HashAlgorithm, canBeEmbedded: true);
                return new EmbeddedSourceTextInfo(sourceTextInfo, embeddedText, compressedHash?.ToImmutableArray() ?? ImmutableArray<byte>.Empty);
            }
        }

        public byte[]? GetPublicKey()
        {
            var metadataReader = PeReader.GetMetadataReader();
            if (!metadataReader.IsAssembly)
            {
                return null;
            }

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

        public (ImmutableArray<SyntaxTree> SyntaxTrees, ImmutableArray<MetadataReference> MetadataReferences) ResolveArtifacts(
            IRebuildArtifactResolver resolver,
            Func<string, SourceText, SyntaxTree> createSyntaxTreeFunc)
        {
            var syntaxTrees = ResolveSyntaxTrees();
            var metadataReferences = ResolveMetadataReferences();
            return (syntaxTrees, metadataReferences);

            ImmutableArray<SyntaxTree> ResolveSyntaxTrees()
            {
                var sourceFileCount = GetSourceFileCount();
                var builder = ImmutableArray.CreateBuilder<SyntaxTree>(sourceFileCount);
                foreach (var (documentHandle, sourceTextInfo) in GetSourceTextInfoCore())
                {
                    SourceText sourceText;
                    if (ResolveEmbeddedSource(documentHandle, sourceTextInfo) is { } embeddedSourceTextInfo)
                    {
                        sourceText = embeddedSourceTextInfo.SourceText;
                    }
                    else
                    {
                        sourceText = resolver.ResolveSourceText(sourceTextInfo);
                        if (!sourceText.GetChecksum().SequenceEqual(sourceTextInfo.Hash))
                        {
                            throw new InvalidOperationException();
                        }
                    }

                    var syntaxTree = createSyntaxTreeFunc(sourceTextInfo.OriginalSourceFilePath, sourceText);
                    builder.Add(syntaxTree);
                }

                return builder.MoveToImmutable();
            }

            ImmutableArray<MetadataReference> ResolveMetadataReferences()
            {
                var builder = ImmutableArray.CreateBuilder<MetadataReference>();
                foreach (var metadataReferenceInfo in GetMetadataReferenceInfo())
                {
                    var metadataReference = resolver.ResolveMetadataReference(metadataReferenceInfo);
                    if (metadataReference.Properties.EmbedInteropTypes != metadataReferenceInfo.EmbedInteropTypes)
                    {
                        throw new InvalidOperationException();
                    }

                    if (!(
                        (metadataReferenceInfo.ExternAlias is null && metadataReference.Properties.Aliases.IsEmpty) ||
                        (metadataReferenceInfo.ExternAlias == metadataReference.Properties.Aliases.SingleOrDefault())
                        ))
                    {
                        throw new InvalidOperationException();
                    }

                    builder.Add(metadataReference);
                }

                return builder.ToImmutable();
            }
        }

        public IEnumerable<MetadataReferenceInfo> GetMetadataReferenceInfo()
        {
            if (!TryGetCustomDebugInformationBlobReader(MetadataReferenceInfoGuid, out var blobReader))
            {
                throw new InvalidOperationException();
            }

            var builder = ImmutableArray.CreateBuilder<MetadataReference>();
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
                    throw new InvalidDataException(string.Format(RebuildResources.Unexpected_value_for_EmbedInteropTypes_MetadataImageKind_0, embedInteropTypesAndKind));
                }

                var embedInteropTypes = (embedInteropTypesAndKind & 0b10) == 0b10;
                var kind = (embedInteropTypesAndKind & 0b1) == 0b1
                    ? MetadataImageKind.Assembly
                    : MetadataImageKind.Module;

                var timestamp = blobReader.ReadInt32();
                var imageSize = blobReader.ReadInt32();
                var mvid = blobReader.ReadGuid();

                if (string.IsNullOrEmpty(externAliases))
                {
                    yield return new MetadataReferenceInfo(
                        name,
                        mvid,
                        ExternAlias: null,
                        kind,
                        embedInteropTypes,
                        timestamp,
                        imageSize);
                }
                else
                {
                    foreach (var alias in externAliases.Split(','))
                    {
                        // The "global" alias is an invention of the tooling on top of the compiler. 
                        // The compiler itself just sees "global" as a reference without any aliases
                        // and we need to mimic that here.
                        yield return new MetadataReferenceInfo(
                            name,
                            mvid,
                            ExternAlias: alias == "global" ? null : alias,
                            kind,
                            embedInteropTypes,
                            timestamp,
                            imageSize);
                    }
                }
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

        public bool HasEmbeddedPdb => PeReader.ReadDebugDirectory().Any(static entry => entry.Type == DebugDirectoryEntryType.EmbeddedPortablePdb);

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
                        throw new InvalidDataException(RebuildResources.Encountered_null_or_empty_key_for_compilation_options_pairs);
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
