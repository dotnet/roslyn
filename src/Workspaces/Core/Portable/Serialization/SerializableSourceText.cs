// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
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
        /// The <see cref="SourceText"/> in the current process.
        /// </summary>
        /// <remarks>
        /// <inheritdoc cref="Storage"/>
        /// </remarks>
        private readonly SourceText _text;

        public SerializableSourceText(SourceText text)
        {
            _text = text;
        }

        /// <summary>
        /// Returns the strongly referenced SourceText if we have it, or tries to retrieve it from the weak reference if
        /// it's still being held there.
        /// </summary>
        /// <returns></returns>
        private SourceText TryGetText()
            => _text;

        public ImmutableArray<byte> GetChecksum()
        {
            return TryGetText().GetChecksum();
        }

        public async ValueTask<SourceText> GetTextAsync(CancellationToken cancellationToken)
        {
            return TryGetText();
        }

        public SourceText GetText(CancellationToken cancellationToken)
        {
            return TryGetText();
        }

        public static ValueTask<SerializableSourceText> FromTextDocumentStateAsync(TextDocumentState state, CancellationToken cancellationToken)
        {
            return SpecializedTasks.TransformWithoutIntermediateCancellationExceptionAsync(
                static (state, cancellationToken) => state.GetTextAsync(cancellationToken),
                static (text, _) => new SerializableSourceText(text),
                state,
                cancellationToken);
        }

        public void Serialize(ObjectWriter writer, SolutionReplicationContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            writer.WriteInt32((int)_text.ChecksumAlgorithm);
            writer.WriteEncoding(_text.Encoding);
            writer.WriteInt32((int)SerializationKinds.Bits);
            _text.WriteTo(writer, cancellationToken);
        }

        public static SerializableSourceText Deserialize(
            ObjectReader reader,
            ITemporaryStorageServiceInternal storageService,
            ITextFactoryService textService,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var checksumAlgorithm = (SourceHashAlgorithm)reader.ReadInt32();
            var encoding = (Encoding)reader.ReadValue();

            var kind = (SerializationKinds)reader.ReadInt32();
            Contract.ThrowIfFalse(kind == SerializationKinds.Bits);
            return new SerializableSourceText(SourceTextExtensions.ReadFrom(textService, reader, encoding, checksumAlgorithm, cancellationToken));
        }
    }
}
