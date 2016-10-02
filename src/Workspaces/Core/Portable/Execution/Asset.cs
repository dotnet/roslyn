// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Execution
{
    /// <summary>
    /// Asset represents actual data. <see cref="ChecksumObjectWithChildren"/> represents
    /// collections of checksums of these data.
    /// 
    /// in hierarchical checksum tree, <see cref="Asset"/>  is leaf node, and <see cref="ChecksumObjectWithChildren"/> 
    /// is node that has children
    /// </summary>
    internal abstract class Asset : ChecksumObject
    {
        public static readonly Asset Null = new NullAsset();

        public Asset(Checksum checksum, string kind) : base(checksum, kind)
        {
            // TODO: find out a way to reduce number of asset implementations.
            //       tried once but couldn't figure out
        }

        /// <summary>
        /// null asset indicating things that doesn't actually exist
        /// </summary>
        private sealed class NullAsset : Asset
        {
            public NullAsset() :
                base(Checksum.Null, WellKnownChecksumObjects.Null)
            {
            }

            public override Task WriteObjectToAsync(ObjectWriter writer, CancellationToken cancellationToken)
            {
                return SpecializedTasks.EmptyTask;
            }
        }
    }

    /// <summary>
    /// helper type for common assets
    /// 
    /// this asset will be used for data that require only 1 information
    /// to serialize such as project reference, solution/project/document info and etc
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

        public override Task WriteObjectToAsync(ObjectWriter writer, CancellationToken cancellationToken)
        {
            _writer(_value, writer, cancellationToken);
            return SpecializedTasks.EmptyTask;
        }
    }

    /// <summary>
    /// helper type for language specific assets
    /// 
    /// this asset will be used for language specific data such as CompilationOption/ParseOption
    /// </summary>
    internal sealed class LanguageSpecificAsset<T> : Asset
    {
        private readonly string _language;
        private readonly T _value;
        private readonly Action<string, T, ObjectWriter, CancellationToken> _writer;

        public LanguageSpecificAsset(string language, T value, string kind, Action<string, T, ObjectWriter, CancellationToken> writer) :
            base(Checksum.Create(language, value, kind, writer), kind)
        {
            _language = language;
            _value = value;
            _writer = writer;
        }

        public override Task WriteObjectToAsync(ObjectWriter writer, CancellationToken cancellationToken)
        {
            _writer(_language, _value, writer, cancellationToken);
            return SpecializedTasks.EmptyTask;
        }
    }

    internal sealed class MetadataReferenceAsset : Asset
    {
        private readonly Serializer _serializer;
        private readonly MetadataReference _reference;

        public MetadataReferenceAsset(Serializer serializer, MetadataReference reference, Checksum checksum, string kind) :
            base(Checksum.Create(kind, checksum), WellKnownChecksumObjects.MetadataReference)
        {
            Contract.Requires(kind == WellKnownChecksumObjects.MetadataReference);

            _serializer = serializer;
            _reference = reference;
        }

        public override Task WriteObjectToAsync(ObjectWriter writer, CancellationToken cancellationToken)
        {
            _serializer.SerializeMetadataReference(_reference, writer, cancellationToken);
            return SpecializedTasks.EmptyTask;
        }
    }

    internal sealed class AnalyzerReferenceAsset : Asset
    {
        private readonly Serializer _serializer;
        private readonly AnalyzerReference _reference;

        public AnalyzerReferenceAsset(Serializer serializer, AnalyzerReference reference, Checksum checksum, string kind) :
            base(Checksum.Create(kind, checksum), WellKnownChecksumObjects.AnalyzerReference)
        {
            Contract.Requires(kind == WellKnownChecksumObjects.AnalyzerReference);

            _serializer = serializer;
            _reference = reference;
        }

        public override Task WriteObjectToAsync(ObjectWriter writer, CancellationToken cancellationToken)
        {
            _serializer.SerializeAnalyzerReference(_reference, writer, cancellationToken);
            return SpecializedTasks.EmptyTask;
        }
    }

    internal sealed class SourceTextAsset : Asset
    {
        private readonly Serializer _serializer;

        // TODO: the way recoverable text works is a bit different than how recoverable tree works.
        //       due to that, we can't just hold onto recoverable text but to document state
        //       we should think about whether we can change recoverable text to work like recoverable tree
        //       so we can serialize text without bring in text to memory first
        private readonly TextDocumentState _state;

        public SourceTextAsset(Serializer serializer, TextDocumentState state, Checksum checksum, string kind) :
            base(Checksum.Create(kind, checksum), WellKnownChecksumObjects.SourceText)
        {
            Contract.Requires(kind == WellKnownChecksumObjects.SourceText);

            _serializer = serializer;
            _state = state;
        }

        public override async Task WriteObjectToAsync(ObjectWriter writer, CancellationToken cancellationToken)
        {
            var text = await _state.GetTextAsync(cancellationToken).ConfigureAwait(false);

            // TODO: make TextDocumentState to implement ISupportTemporaryStorage?
            _serializer.SerializeSourceText(_state.Storage as ITemporaryStorageWithName, text, writer, cancellationToken);
        }
    }
}
