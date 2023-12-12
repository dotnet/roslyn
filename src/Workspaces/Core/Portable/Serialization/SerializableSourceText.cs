// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
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
        public SourceText Text { get; }

        public SerializableSourceText(SourceText text)
        {
            Text = text;
        }

        public ImmutableArray<byte> GetContentHash()
        {
            return Text.GetContentHash();
        }

        public static ValueTask<SerializableSourceText> FromTextDocumentStateAsync(TextDocumentState state, CancellationToken cancellationToken)
        {
            return SpecializedTasks.TransformWithoutIntermediateCancellationExceptionAsync(
                static (state, cancellationToken) => state.GetTextAsync(cancellationToken),
                static (text, _) => new SerializableSourceText(text),
                state,
                cancellationToken);
        }

        public void Serialize(ObjectWriter writer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            writer.WriteInt32((int)this.Text.ChecksumAlgorithm);
            writer.WriteEncoding(this.Text.Encoding);
            this.Text.WriteTo(writer, cancellationToken);
        }

        public static SerializableSourceText Deserialize(
            ObjectReader reader,
            ITextFactoryService textService,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var checksumAlgorithm = (SourceHashAlgorithm)reader.ReadInt32();
            var encoding = (Encoding)reader.ReadValue();

            return new SerializableSourceText(SourceTextExtensions.ReadFrom(textService, reader, encoding, checksumAlgorithm, cancellationToken));
        }
    }
}
