// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.Host.TemporaryStorageService;

namespace Microsoft.CodeAnalysis.Serialization;

#pragma warning disable CA1416 // Validate platform compatibility

/// <summary>
/// Represents a <see cref="SourceText"/> which can be serialized for sending to another process. The text is not
/// required to be a live object in the current process, and can instead be held in temporary storage accessible by
/// both processes.
/// </summary>
internal sealed class SerializableSourceText
{
    /// <summary>
    /// The storage location for <see cref="SourceText"/>.
    /// </summary>
    /// <remarks>
    /// Exactly one of <see cref="_storage"/> or <see cref="_text"/> will be non-<see langword="null"/>.
    /// </remarks>
    private readonly TemporaryTextStorage? _storage;

    /// <summary>
    /// The <see cref="SourceText"/> in the current process.
    /// </summary>
    /// <remarks>
    /// <inheritdoc cref="Storage"/>
    /// </remarks>
    private readonly SourceText? _text;

    /// <summary>
    /// The hash that would be produced by calling <inheritdoc cref="SourceText.GetContentHash"/> on <see
    /// cref="_text"/>.  Can be passed in when already known to avoid unnecessary computation costs.
    /// </summary>
    public readonly ImmutableArray<byte> ContentHash;

    /// <summary>
    /// Weak reference to a SourceText computed from <see cref="_storage"/>.  Useful so that if multiple requests
    /// come in for the source text, the same one can be returned as long as something is holding it alive.
    /// </summary>
    private readonly WeakReference<SourceText?> _computedText = new(target: null);

    public SerializableSourceText(TemporaryTextStorage storage, ImmutableArray<byte> contentHash)
        : this(storage, text: null, contentHash)
    {
    }

    public SerializableSourceText(SourceText text, ImmutableArray<byte> contentHash)
        : this(storage: null, text, contentHash)
    {
    }

    private SerializableSourceText(TemporaryTextStorage? storage, SourceText? text, ImmutableArray<byte> contentHash)
    {
        Debug.Assert(storage is null != text is null);

        _storage = storage;
        _text = text;
        ContentHash = contentHash;

#if DEBUG
        var computedContentHash = TryGetText()?.GetContentHash() ?? _storage!.ContentHash;
        Debug.Assert(contentHash.SequenceEqual(computedContentHash));
#endif
    }

    /// <summary>
    /// Returns the strongly referenced SourceText if we have it, or tries to retrieve it from the weak reference if
    /// it's still being held there.
    /// </summary>
    /// <returns></returns>
    private SourceText? TryGetText()
        => _text ?? _computedText.GetTarget();

    public async ValueTask<SourceText> GetTextAsync(CancellationToken cancellationToken)
    {
        var text = TryGetText();
        if (text != null)
            return text;

        // Read and cache the text from the storage object so that other requests may see it if still kept alive by something.
        text = await _storage!.ReadTextAsync(cancellationToken).ConfigureAwait(false);
        _computedText.SetTarget(text);
        return text;
    }

    public SourceText GetText(CancellationToken cancellationToken)
    {
        var text = TryGetText();
        if (text != null)
            return text;

        // Read and cache the text from the storage object so that other requests may see it if still kept alive by something.
        text = _storage!.ReadText(cancellationToken);
        _computedText.SetTarget(text);
        return text;
    }

    public static ValueTask<SerializableSourceText> FromTextDocumentStateAsync(
        TextDocumentState state, CancellationToken cancellationToken)
    {
        if (state.Storage is TemporaryTextStorage storage)
        {
            return new ValueTask<SerializableSourceText>(new SerializableSourceText(storage, storage.ContentHash));
        }
        else
        {
            return SpecializedTasks.TransformWithoutIntermediateCancellationExceptionAsync(
                static (state, cancellationToken) => state.GetTextAsync(cancellationToken),
                static (text, _) => new SerializableSourceText(text, text.GetContentHash()),
                state,
                cancellationToken);
        }
    }

    public void Serialize(ObjectWriter writer, SolutionReplicationContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_storage is not null)
        {
            context.AddResource(_storage);

            writer.WriteInt32((int)_storage.ChecksumAlgorithm);
            writer.WriteEncoding(_storage.Encoding);
            writer.WriteByteArray(ImmutableCollectionsMarshal.AsArray(_storage.ContentHash)!);

            writer.WriteInt32((int)SerializationKinds.MemoryMapFile);
            writer.WriteString(_storage.Name);
            writer.WriteInt64(_storage.Offset);
            writer.WriteInt64(_storage.Size);
        }
        else
        {
            RoslynDebug.AssertNotNull(_text);

            writer.WriteInt32((int)_text.ChecksumAlgorithm);
            writer.WriteEncoding(_text.Encoding);
            writer.WriteByteArray(ImmutableCollectionsMarshal.AsArray(_text.GetContentHash())!);

            writer.WriteInt32((int)SerializationKinds.Bits);
            _text.WriteTo(writer, cancellationToken);
        }
    }

    public static SerializableSourceText Deserialize(
        ObjectReader reader,
        ITemporaryStorageServiceInternal storageService,
        ITextFactoryService textService,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var checksumAlgorithm = (SourceHashAlgorithm)reader.ReadInt32();
        var encoding = reader.ReadEncoding();
        var contentHash = ImmutableCollectionsMarshal.AsImmutableArray(reader.ReadByteArray());

        var kind = (SerializationKinds)reader.ReadInt32();
        Contract.ThrowIfFalse(kind is SerializationKinds.Bits or SerializationKinds.MemoryMapFile);

        if (kind == SerializationKinds.MemoryMapFile)
        {
            var storage2 = (TemporaryStorageService)storageService;

            var name = reader.ReadRequiredString();
            var offset = reader.ReadInt64();
            var size = reader.ReadInt64();

            var storage = storage2.AttachTemporaryTextStorage(name, offset, size, checksumAlgorithm, encoding, contentHash);
            return new SerializableSourceText(storage, contentHash);
        }
        else
        {
            return new SerializableSourceText(
                SourceTextExtensions.ReadFrom(textService, reader, encoding, checksumAlgorithm, cancellationToken),
                contentHash);
        }
    }
}
