// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.Host.TemporaryStorageService;

#if DEBUG
#endif

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
    /// Exactly one of <see cref="_storageHandle"/> or <see cref="_text"/> will be non-<see langword="null"/>.
    /// </remarks>
    private readonly TemporaryStorageTextHandle? _storageHandle;

    /// <summary>
    /// The <see cref="SourceText"/> in the current process.
    /// </summary>
    /// <remarks>
    /// <inheritdoc cref="_storageHandle"/>
    /// </remarks>
    private readonly SourceText? _text;

    /// <summary>
    /// Weak reference to a SourceText computed from <see cref="_storageHandle"/>.  Useful so that if multiple requests
    /// come in for the source text, the same one can be returned as long as something is holding it alive.
    /// </summary>
    private readonly WeakReference<SourceText?> _computedText = new(target: null);

    /// <summary>
    /// Checksum of the contents (see <see cref="SourceText.GetContentHash"/>) of the text.
    /// </summary>
    public readonly Checksum ContentChecksum;

    public SerializableSourceText(TemporaryStorageTextHandle storageHandle)
        : this(storageHandle, text: null, Checksum.Create(storageHandle.ContentHash))
    {
    }

    public SerializableSourceText(SourceText text, ImmutableArray<byte> contentHash)
        : this(storageHandle: null, text, Checksum.Create(contentHash))
    {
    }

    public SerializableSourceText(SourceText text, Checksum contentChecksum)
        : this(storageHandle: null, text, contentChecksum)
    {
    }

    private SerializableSourceText(TemporaryStorageTextHandle? storageHandle, SourceText? text, Checksum contentChecksum)
    {
        Debug.Assert(storageHandle is null != text is null);

        _storageHandle = storageHandle;
        _text = text;
        ContentChecksum = contentChecksum;

#if DEBUG
        var computedContentHash = TryGetText()?.GetContentHash() ?? _storageHandle!.ContentHash;
        Debug.Assert(contentChecksum == Checksum.Create(computedContentHash));
#endif
    }

    /// <summary>
    /// Returns the strongly referenced SourceText if we have it, or tries to retrieve it from the weak reference if
    /// it's still being held there.
    /// </summary>
    private SourceText? TryGetText()
        => _text ?? _computedText.GetTarget();

    public async ValueTask<SourceText> GetTextAsync(CancellationToken cancellationToken)
    {
        var text = TryGetText();
        if (text != null)
            return text;

        // Read and cache the text from the storage object so that other requests may see it if still kept alive by something.
        text = await _storageHandle!.ReadFromTemporaryStorageAsync(cancellationToken).ConfigureAwait(false);
        _computedText.SetTarget(text);
        return text;
    }

    public SourceText GetText(CancellationToken cancellationToken)
    {
        var text = TryGetText();
        if (text != null)
            return text;

        // Read and cache the text from the storage object so that other requests may see it if still kept alive by something.
        text = _storageHandle!.ReadFromTemporaryStorage(cancellationToken);
        _computedText.SetTarget(text);
        return text;
    }

    public static async ValueTask<SerializableSourceText> FromTextDocumentStateAsync(
        TextDocumentState state, CancellationToken cancellationToken)
    {
        if (state.TextAndVersionSource.TextLoader is SerializableSourceTextLoader serializableLoader)
        {
            // If we're already pointing at a serializable loader, we can just use that directly.
            return serializableLoader.SerializableSourceText;
        }
        else if (state.StorageHandle is TemporaryStorageTextHandle storageHandle)
        {
            // Otherwise, if we're pointing at a memory mapped storage location, we can create the source text that directly wraps that.
            return new SerializableSourceText(storageHandle);
        }
        else
        {
            // Otherwise, the state object has reified the text into some other form, and dumped any original
            // information on how it got it.  In that case, we create a new text instance to represent the serializable
            // source text out of.

            return await SpecializedTasks.TransformWithoutIntermediateCancellationExceptionAsync(
                static (state, cancellationToken) => state.GetTextAsync(cancellationToken),
                static (text, _) => new SerializableSourceText(text, text.GetContentHash()),
                state,
                cancellationToken).ConfigureAwait(false);
        }
    }

    public void Serialize(ObjectWriter writer, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_storageHandle is not null)
        {
            writer.WriteInt32((int)SerializationKinds.MemoryMapFile);
            _storageHandle.Identifier.WriteTo(writer);
            writer.WriteInt32((int)_storageHandle.ChecksumAlgorithm);
            writer.WriteEncoding(_storageHandle.Encoding);
            writer.WriteByteArray(ImmutableCollectionsMarshal.AsArray(_storageHandle.ContentHash)!);
        }
        else
        {
            RoslynDebug.AssertNotNull(_text);
            writer.WriteInt32((int)SerializationKinds.Bits);

            writer.WriteInt32((int)_text.ChecksumAlgorithm);
            writer.WriteEncoding(_text.Encoding);
            writer.WriteByteArray(ImmutableCollectionsMarshal.AsArray(_text.GetContentHash())!);

            _text.WriteTo(writer, cancellationToken);
        }
    }

    public static SerializableSourceText Deserialize(
        ObjectReader reader,
        TemporaryStorageService storageService,
        ITextFactoryService textService,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var kind = (SerializationKinds)reader.ReadInt32();
        Contract.ThrowIfFalse(kind is SerializationKinds.Bits or SerializationKinds.MemoryMapFile);

        if (kind == SerializationKinds.MemoryMapFile)
        {
            var identifier = TemporaryStorageIdentifier.ReadFrom(reader);
            var checksumAlgorithm = (SourceHashAlgorithm)reader.ReadInt32();
            var encoding = reader.ReadEncoding();
            var contentHash = ImmutableCollectionsMarshal.AsImmutableArray(reader.ReadByteArray());
            var storageHandle = storageService.GetTextHandle(identifier, checksumAlgorithm, encoding, contentHash);

            return new SerializableSourceText(storageHandle);
        }
        else
        {
            var checksumAlgorithm = (SourceHashAlgorithm)reader.ReadInt32();
            var encoding = reader.ReadEncoding();
            var contentHash = ImmutableCollectionsMarshal.AsImmutableArray(reader.ReadByteArray());

            return new SerializableSourceText(
                SourceTextExtensions.ReadFrom(textService, reader, encoding, checksumAlgorithm, cancellationToken),
                contentHash);
        }
    }

    public TextLoader ToTextLoader(string? filePath)
        => new SerializableSourceTextLoader(this, filePath);

    /// <summary>
    /// A <see cref="TextLoader"/> that wraps a <see cref="SerializableSourceText"/> and provides access to the text in
    /// a deferred fashion.  In practice, during a host and OOP sync, while all the documents will be 'serialized' over
    /// to OOP, the actual contents of the documents will only need to be loaded depending on which files are open, and
    /// thus what compilations and trees are needed.  As such, we want to be able to lazily defer actually getting the
    /// contents of the text until it's actually needed.  This loader allows us to do that, allowing the OOP side to
    /// simply point to the segments in the memory-mapped-file the host has dumped its text into, and only actually
    /// realizing the real text values when they're needed.
    /// </summary>
    private sealed class SerializableSourceTextLoader : TextLoader
    {
        public readonly SerializableSourceText SerializableSourceText;
        private readonly VersionStamp _version = VersionStamp.Create();

        public SerializableSourceTextLoader(
            SerializableSourceText serializableSourceText,
            string? filePath)
        {
            SerializableSourceText = serializableSourceText;
            FilePath = filePath;
        }

        internal override string? FilePath { get; }

        /// <summary>
        /// Documents should always hold onto instances of this text loader strongly.  In other words, they should load
        /// from this, and then dump the contents into a RecoverableText object that then dumps the contents to a memory
        /// mapped file within this process.  Doing that is pointless as the contents of this text are already in a
        /// memory mapped file on the host side.
        /// </summary>
        internal override bool AlwaysHoldStrongly
            => true;

        public override async Task<TextAndVersion> LoadTextAndVersionAsync(LoadTextOptions options, CancellationToken cancellationToken)
            => TextAndVersion.Create(await this.SerializableSourceText.GetTextAsync(cancellationToken).ConfigureAwait(false), _version);

        internal override TextAndVersion LoadTextAndVersionSynchronously(LoadTextOptions options, CancellationToken cancellationToken)
            => TextAndVersion.Create(this.SerializableSourceText.GetText(cancellationToken), _version);
    }
}
