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

//<<<<<<< HEAD
//        public ImmutableArray<byte> GetChecksum()
//        {
//            return this.Text.GetChecksum();
//=======
//        private SerializableSourceText(ITemporaryTextStorageWithName? storage, SourceText? text)
//        {
//            Debug.Assert(storage is null != text is null);

//            _storage = storage;
//            _text = text;
//        }

//        /// <summary>
//        /// Returns the strongly referenced SourceText if we have it, or tries to retrieve it from the weak reference if
//        /// it's still being held there.
//        /// </summary>
//        /// <returns></returns>
//        private SourceText? TryGetText()
//            => _text ?? _computedText.GetTarget();

//        public ImmutableArray<byte> GetContentHash()
//        {
//            return TryGetText()?.GetContentHash() ?? _storage!.GetContentHash();
//        }

//        public async ValueTask<SourceText> GetTextAsync(CancellationToken cancellationToken)
//        {
//            var text = TryGetText();
//            if (text != null)
//                return text;

//            // Read and cache the text from the storage object so that other requests may see it if still kept alive by something.
//            text = await _storage!.ReadTextAsync(cancellationToken).ConfigureAwait(false);
//            _computedText.SetTarget(text);
//            return text;
//        }

//        public SourceText GetText(CancellationToken cancellationToken)
//        {
//            var text = TryGetText();
//            if (text != null)
//                return text;

//            // Read and cache the text from the storage object so that other requests may see it if still kept alive by something.
//            text = _storage!.ReadText(cancellationToken);
//            _computedText.SetTarget(text);
//            return text;
//>>>>>>> upstream/main
//        }

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
            writer.WriteInt32((int)SerializationKinds.Bits);
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

            var kind = (SerializationKinds)reader.ReadInt32();
            Contract.ThrowIfFalse(kind == SerializationKinds.Bits);
            return new SerializableSourceText(SourceTextExtensions.ReadFrom(textService, reader, encoding, checksumAlgorithm, cancellationToken));
        }
    }
}
