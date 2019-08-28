// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Execution
{
    /// <summary>
    /// Asset that is part of solution
    /// </summary>
    internal abstract class SolutionAsset : RemotableData
    {
        protected SolutionAsset(Checksum checksum, WellKnownSynchronizationKind kind)
            : base(checksum, kind)
        {
        }

        public static RemotableData Create(Checksum checksum, object value, ISerializerService serializer)
        {
            // treat SourceText specially since we get TextDocumentState rather than SourceText when searching the checksum
            // from solution due to it requiring async-ness when retrieving SourceText.
            //
            // right now, SourceText is the only one that requires async-ness and making all calls async just for this
            // one makes us to have a lot of unnecessary allocations due to Task and overall slow down of several seconds.
            //
            // all calls used to be all async and converted back to synchronous due to all those unnecessary overhead of tasks.
            if (value is TextDocumentState state)
            {
                return new SourceTextAsset(checksum, state, serializer);
            }

            return new SimpleSolutionAsset(checksum, value, serializer);
        }

        internal sealed class SimpleSolutionAsset : SolutionAsset
        {
            private readonly object _value;
            private readonly ISerializerService _serializer;

            public SimpleSolutionAsset(Checksum checksum, object value, ISerializerService serializer)
                : base(checksum, value.GetWellKnownSynchronizationKind())
            {
                _value = value;
                _serializer = serializer;
            }

            public override Task WriteObjectToAsync(ObjectWriter writer, CancellationToken cancellationToken)
            {
                _serializer.Serialize(_value, writer, cancellationToken);
                return Task.CompletedTask;
            }
        }

        internal sealed class SourceTextAsset : SolutionAsset
        {
            // TODO: the way recoverable text works is a bit different than how recoverable tree works.
            //       due to that, we can't just hold onto recoverable text but have to hold onto document state.
            //       we should think about whether we can change recoverable text to work like recoverable tree
            //       so we can serialize text without bring in text to memory first
            private readonly TextDocumentState _state;
            private readonly ISerializerService _serializer;

            public SourceTextAsset(Checksum checksum, TextDocumentState state, ISerializerService serializer)
                : base(checksum, WellKnownSynchronizationKind.SourceText)
            {
                _state = state;
                _serializer = serializer;
            }

            public override async Task WriteObjectToAsync(ObjectWriter writer, CancellationToken cancellationToken)
            {
                var text = await _state.GetTextAsync(cancellationToken).ConfigureAwait(false);

                // TODO: make TextDocumentState to implement ISupportTemporaryStorage?
                _serializer.SerializeSourceText(_state.Storage as ITemporaryStorageWithName, text, writer, cancellationToken);
            }
        }
    }
}
