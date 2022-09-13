// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Serialization
{
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
        private readonly ITemporaryTextStorageWithName? _storage;

        /// <summary>
        /// The <see cref="SourceText"/> in the current process.
        /// </summary>
        /// <remarks>
        /// <inheritdoc cref="Storage"/>
        /// </remarks>
        private readonly SourceText? _text;

        /// <summary>
        /// Weak reference to a SourceText computed from <see cref="_storage"/>.  Useful so that if multiple requests
        /// come in for the source text, the same one can be returned as long as something is holding it alive.
        /// </summary>
        private readonly WeakReference<SourceText?> _computedText = new(target: null);

        public SerializableSourceText(ITemporaryTextStorageWithName storage)
            : this(storage, text: null)
        {
        }

        public SerializableSourceText(SourceText text)
            : this(storage: null, text)
        {
        }

        private SerializableSourceText(ITemporaryTextStorageWithName? storage, SourceText? text)
        {
            Debug.Assert(storage is null != text is null);

            _storage = storage;
            _text = text;
        }

        /// <summary>
        /// Returns the strongly referenced SourceText if we have it, or tries to retrieve it from the weak reference if
        /// it's still being held there.
        /// </summary>
        /// <returns></returns>
        private SourceText? TryGetText()
            => _text ?? _computedText.GetTarget();

        public ImmutableArray<byte> GetChecksum(CancellationToken cancellationToken)
        {
            return TryGetText()?.GetChecksum() ?? _storage!.GetChecksum();
        }

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

        public static ValueTask<SerializableSourceText> FromTextDocumentStateAsync(TextDocumentState state, CancellationToken cancellationToken)
        {
            if (state.Storage is ITemporaryTextStorageWithName storage)
            {
                return new ValueTask<SerializableSourceText>(new SerializableSourceText(storage));
            }
            else
            {
                return SpecializedTasks.TransformWithoutIntermediateCancellationExceptionAsync(
                    static (state, cancellationToken) => state.GetTextAsync(cancellationToken),
                    static (text, _) => new SerializableSourceText(text),
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
                writer.WriteInt32((int)SerializationKinds.Bits);
                _text.WriteTo(writer, cancellationToken);
            }
        }
    }
}
