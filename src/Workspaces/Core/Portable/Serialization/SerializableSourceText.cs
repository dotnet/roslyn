// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        /// Gate for controlling access to <see cref="_storage"/> and <see cref="_text"/>.
        /// </summary>
        private readonly SemaphoreSlim _gate = new(initialCount: 1);

        /// <summary>
        /// The storage location for <see cref="SourceText"/>.
        /// </summary>
        /// <remarks>
        /// Exactly one of <see cref="_storage"/> or <see cref="_text"/> will be non-<see langword="null"/>. This value
        /// will be set to <see langword="null"/> once <see cref="_text"/> has been computed and cached.
        /// </remarks>
        private ITemporaryTextStorageWithName? _storage;

        /// <summary>
        /// The <see cref="SourceText"/> in the current process.  May be initially null, but will become non-null once
        /// computed and cached.
        /// </summary>
        /// <remarks>
        /// <inheritdoc cref="Storage"/>
        /// </remarks>
        private SourceText? _text;

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

        public ImmutableArray<byte> GetChecksum(CancellationToken cancellationToken)
        {
            SourceText? text;
            ITemporaryTextStorageWithName? storage;
            using (_gate.DisposableWait(cancellationToken))
            {
                text = _text;
                storage = _storage;
            }

            return text?.GetChecksum() ?? storage!.GetChecksum();
        }

        public async ValueTask<SourceText> GetTextAsync(CancellationToken cancellationToken)
        {
            using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                // if not computed, then compute and swap over to that value.
                if (_text == null)
                {
                    _text = await _storage!.ReadTextAsync(cancellationToken).ConfigureAwait(false);
                    _storage = null;
                }

                return _text;
            }
        }

        public SourceText GetText(CancellationToken cancellationToken)
        {
            using (_gate.DisposableWait(cancellationToken))
            {
                // if not computed, then compute and swap over to that value.
                if (_text == null)
                {
                    _text = _storage!.ReadText(cancellationToken);
                    _storage = null;
                }

                return _text;
            }
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
            SourceText? text;
            ITemporaryTextStorageWithName? storage;
            using (_gate.DisposableWait(cancellationToken))
            {
                text = _text;
                storage = _storage;
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (storage is not null)
            {
                context.AddResource(storage);

                writer.WriteInt32((int)storage.ChecksumAlgorithm);
                writer.WriteEncoding(storage.Encoding);

                writer.WriteInt32((int)SerializationKinds.MemoryMapFile);
                writer.WriteString(storage.Name);
                writer.WriteInt64(storage.Offset);
                writer.WriteInt64(storage.Size);
            }
            else
            {
                RoslynDebug.AssertNotNull(text);

                writer.WriteInt32((int)text.ChecksumAlgorithm);
                writer.WriteEncoding(text.Encoding);
                writer.WriteInt32((int)SerializationKinds.Bits);
                text.WriteTo(writer, cancellationToken);
            }
        }
    }
}
