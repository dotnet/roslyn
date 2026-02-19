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

        var sourceDocuments = new FixedSizeArrayBuilder<SourceDocument>(documentHandles.Count);

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

        return sourceDocuments.MoveToImmutable();
    }

    private string? TryGetSourceLinkUrl(DocumentHandle handle)
    {
        var document = _pdbReader.GetDocument(handle);
        if (document.Name.IsNil)
            return null;

        var documentName = _pdbReader.GetString(document.Name);
        if (documentName is null)
            return null;

        if (!_pdbReader.TryGetCustomDebugInformation(EntityHandle.ModuleDefinition, PortableCustomDebugInfoKinds.SourceLink, out var cdi) || cdi.Value.IsNil)
            return null;

        var blobReader = _pdbReader.GetBlobReader(cdi.Value);
        var sourceLinkJson = blobReader.ReadUTF8(blobReader.Length);

        var map = SourceLinkMap.Parse(sourceLinkJson);

        return map.TryGetUri(documentName, out var uri) ? uri : null;
    }

    private byte[]? TryGetEmbeddedTextBytes(DocumentHandle handle)
        => _pdbReader.TryGetCustomDebugInformation(handle, PortableCustomDebugInfoKinds.EmbeddedSource, out var cdi)
            ? _pdbReader.GetBlobBytes(cdi.Value)
            : null;

    public ImmutableDictionary<string, string> GetCompilationOptions()
        => _pdbReader.GetCompilationOptions();

    public void Dispose()
    {
        _pdbReaderProvider.Dispose();
        _peReader.Dispose();
    }
}
