// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Serialization
{
    internal sealed class SerializableSourceText
    {
        public ITemporaryStorageWithName? Storage { get; }
        public SourceText? Text { get; }

        public SerializableSourceText(ITemporaryStorageWithName storage)
            : this(storage, text: null)
        {
        }

        public SerializableSourceText(SourceText text)
            : this(storage: null, text)
        {
        }

        private SerializableSourceText(ITemporaryStorageWithName? storage, SourceText? text)
        {
            Debug.Assert(storage is not null || text is not null);

            Storage = storage;
            Text = text;
        }

        public ImmutableArray<byte> GetChecksum()
        {
            if (Storage is not null)
                return Storage.GetChecksum();
            else if (Text is not null)
                return Text.GetChecksum();
            else
                return ImmutableArray<byte>.Empty;
        }

        public async ValueTask<SourceText> GetTextAsync(CancellationToken cancellationToken)
        {
            if (Text is not null)
                return Text;

            if (Storage is not ITemporaryTextStorage textStorage)
                throw new NotSupportedException();

            return await textStorage.ReadTextAsync(cancellationToken).ConfigureAwait(false);
        }

        public static async ValueTask<SerializableSourceText> FromTextDocumentStateAsync(TextDocumentState state, CancellationToken cancellationToken)
        {
            if (state.Storage is ITemporaryStorageWithName storage)
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
