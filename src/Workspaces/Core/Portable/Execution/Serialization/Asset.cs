// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Execution
{
    /// <summary>
    /// Asset indicates this type is flat data holder type. no nested checksum objects
    /// </summary>
    internal abstract class Asset : ChecksumObject
    {
        public Asset(Checksum checksum, string kind) : base(checksum, kind)
        {
        }
    }

    /// <summary>
    /// helper type for common assets
    /// </summary>
    internal sealed class Asset<T> : Asset
    {
        private readonly T _value;
        private readonly Action<T, ObjectWriter, CancellationToken> _writer;

        public Asset(T value, string kind, Action<T, ObjectWriter, CancellationToken> writer) :
            base(Checksum.Create(value, kind, writer), kind)
        {
            _value = value;
            _writer = writer;
        }

        public override Task WriteToAsync(ObjectWriter writer, CancellationToken cancellationToken)
        {
            _writer(_value, writer, cancellationToken);
            return SpecializedTasks.EmptyTask;
        }
    }

    /// <summary>
    /// helper type for common assets
    /// </summary>
    internal sealed class Asset<T1, T2> : Asset
    {
        private readonly T1 _value1;
        private readonly T2 _value2;
        private readonly Action<T1, T2, ObjectWriter, CancellationToken> _writer;

        public Asset(T1 value1, T2 value2, string kind, Action<T1, T2, ObjectWriter, CancellationToken> writer) :
            base(Checksum.Create(value1, value2, kind, writer), kind)
        {
            _value1 = value1;
            _value2 = value2;
            _writer = writer;
        }

        public override Task WriteToAsync(ObjectWriter writer, CancellationToken cancellationToken)
        {
            _writer(_value1, _value2, writer, cancellationToken);
            return SpecializedTasks.EmptyTask;
        }
    }

    internal sealed class SourceTextAsset : Asset
    {
        // TODO: change this to recoverable text rather than document state
        private readonly Serializer _serializer;
        private readonly TextDocumentState _state;

        public SourceTextAsset(Serializer serializer, TextDocumentState state, Checksum checksum, string kind) :
            base(checksum, WellKnownChecksumObjects.SourceText)
        {
            Contract.Requires(kind == WellKnownChecksumObjects.SourceText);

            _serializer = serializer;
            _state = state;
        }

        public override async Task WriteToAsync(ObjectWriter writer, CancellationToken cancellationToken)
        {
            var text = await _state.GetTextAsync(cancellationToken).ConfigureAwait(false);
            _serializer.Serialize(text, writer, cancellationToken);
        }
    }
}
