// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

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
        /// Exactly one of <see cref="Storage"/> or <see cref="Text"/> will be non-<see langword="null"/>.
        /// </remarks>
        public ITemporaryTextStorageWithName? Storage { get; }

        /// <summary>
        /// The <see cref="SourceText"/> in the current process.
        /// </summary>
        /// <remarks>
        /// <inheritdoc cref="Storage"/>
        /// </remarks>
        public SourceText? Text { get; }

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

            Storage = storage;
            Text = text;
        }

        public ImmutableArray<byte> GetChecksum()
        {
            return Text?.GetChecksum() ?? Storage!.GetChecksum();
        }

        public async ValueTask<SourceText> GetTextAsync(CancellationToken cancellationToken)
        {
            if (Text is not null)
                return Text;

            return await Storage!.ReadTextAsync(cancellationToken).ConfigureAwait(false);
        }

        public static async ValueTask<SerializableSourceText> FromTextDocumentStateAsync(TextDocumentState state, CancellationToken cancellationToken)
        {
            if (state.Storage is ITemporaryTextStorageWithName storage)
            {
                return new SerializableSourceText(storage);
            }
            else
            {
                return new SerializableSourceText(await state.GetTextAsync(cancellationToken).ConfigureAwait(false));
            }
        }
    }
}
