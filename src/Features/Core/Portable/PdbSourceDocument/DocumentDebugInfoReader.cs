// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Microsoft.SourceLink.Tools;

namespace Microsoft.CodeAnalysis.PdbSourceDocument;

/// <summary>
/// Gets information from DLL and/or PDB files needed for navigating to source documents
/// </summary>
internal sealed class DocumentDebugInfoReader : IDisposable
{
    private readonly MetadataReaderProvider _pdbReaderProvider;
    private readonly PEReader _peReader;

    private readonly MetadataReader _dllReader;
    private readonly MetadataReader _pdbReader;

    public DocumentDebugInfoReader(PEReader peReader, MetadataReaderProvider pdbReaderProvider)
    {
        _peReader = peReader;
        _pdbReaderProvider = pdbReaderProvider;

        _dllReader = _peReader.GetMetadataReader();
        _pdbReader = _pdbReaderProvider.GetMetadataReader();
    }

    public ImmutableArray<SourceDocument> FindSourceDocuments(EntityHandle entityHandle)
    {
        var documentHandles = SymbolSourceDocumentFinder.FindDocumentHandles(entityHandle, _dllReader, _pdbReader);

        using var _ = ArrayBuilder<SourceDocument>.GetInstance(out var sourceDocuments);

        foreach (var handle in documentHandles)
        {
            var document = _pdbReader.GetDocument(handle);
            var filePath = _pdbReader.GetString(document.Name);

            var hashAlgorithmGuid = _pdbReader.GetGuid(document.HashAlgorithm);
            var hashAlgorithm = SourceHashAlgorithms.GetSourceHashAlgorithm(hashAlgorithmGuid);
            var checksum = _pdbReader.GetBlobContent(document.Hash);

            var embeddedTextBytes = TryGetEmbeddedTextBytes(handle);
            var sourceLinkUrl = TryGetSourceLinkUrl(handle);

            sourceDocuments.Add(new SourceDocument(filePath, hashAlgorithm, checksum, embeddedTextBytes, sourceLinkUrl));
        }

        return sourceDocuments.ToImmutable();
    }

    private string? TryGetSourceLinkUrl(DocumentHandle handle)
    {
        var document = _pdbReader.GetDocument(handle);
        if (document.Name.IsNil)
            return null;

        var documentName = _pdbReader.GetString(document.Name);
        if (documentName is null)
            return null;

        foreach (var cdiHandle in _pdbReader.GetCustomDebugInformation(EntityHandle.ModuleDefinition))
        {
            var cdi = _pdbReader.GetCustomDebugInformation(cdiHandle);
            if (_pdbReader.GetGuid(cdi.Kind) == PortableCustomDebugInfoKinds.SourceLink && !cdi.Value.IsNil)
            {
                var blobReader = _pdbReader.GetBlobReader(cdi.Value);
                var sourceLinkJson = blobReader.ReadUTF8(blobReader.Length);

                var map = SourceLinkMap.Parse(sourceLinkJson);

                if (map.TryGetUri(documentName, out var uri))
                {
                    return uri;
                }
            }
        }

        return null;
    }

    private byte[]? TryGetEmbeddedTextBytes(DocumentHandle handle)
    {
        var handles = _pdbReader.GetCustomDebugInformation(handle);
        foreach (var cdiHandle in handles)
        {
            var cdi = _pdbReader.GetCustomDebugInformation(cdiHandle);
            var guid = _pdbReader.GetGuid(cdi.Kind);
            if (guid == PortableCustomDebugInfoKinds.EmbeddedSource)
            {
                return _pdbReader.GetBlobBytes(cdi.Value);
            }
        }

        return null;
    }

    public ImmutableDictionary<string, string> GetCompilationOptions()
    {
        using var _ = PooledDictionary<string, string>.GetInstance(out var result);

        foreach (var handle in _pdbReader.GetCustomDebugInformation(EntityHandle.ModuleDefinition))
        {
            var customDebugInformation = _pdbReader.GetCustomDebugInformation(handle);
            if (_pdbReader.GetGuid(customDebugInformation.Kind) == PortableCustomDebugInfoKinds.CompilationOptions)
            {
                var blobReader = _pdbReader.GetBlobReader(customDebugInformation.Value);

                // Compiler flag bytes are UTF-8 null-terminated key-value pairs
                var nullIndex = blobReader.IndexOf(0);
                while (nullIndex >= 0)
                {
                    var key = blobReader.ReadUTF8(nullIndex);

                    // Skip the null terminator
                    blobReader.ReadByte();

                    nullIndex = blobReader.IndexOf(0);
                    var value = blobReader.ReadUTF8(nullIndex);

                    result.Add(key, value);

                    // Skip the null terminator
                    blobReader.ReadByte();
                    nullIndex = blobReader.IndexOf(0);
                }
            }
        }

        return result.ToImmutableDictionary();
    }

    public void Dispose()
    {
        _pdbReaderProvider.Dispose();
        _peReader.Dispose();
    }
}
