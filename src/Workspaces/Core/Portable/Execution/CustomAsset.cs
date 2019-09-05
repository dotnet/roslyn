// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Execution
{
    /// <summary>
    /// Asset that is not part of solution, but want to participate in <see cref="IRemotableDataService"/>
    /// </summary>
    internal abstract class CustomAsset : RemotableData
    {
        public CustomAsset(Checksum checksum, WellKnownSynchronizationKind kind) : base(checksum, kind)
        {
        }
    }

    /// <summary>
    /// helper type for custom asset
    /// </summary>
    internal sealed class SimpleCustomAsset : CustomAsset
    {
        private readonly Action<ObjectWriter, CancellationToken> _writer;

        public SimpleCustomAsset(WellKnownSynchronizationKind kind, Action<ObjectWriter, CancellationToken> writer)
            : base(CreateChecksumFromStreamWriter(kind, writer), kind)
        {
            // unlike SolutionAsset which gets checksum from solution states, this one build one by itself.
            _writer = writer;
        }

        public override Task WriteObjectToAsync(ObjectWriter writer, CancellationToken cancellationToken)
        {
            _writer(writer, cancellationToken);
            return Task.CompletedTask;
        }

        private static Checksum CreateChecksumFromStreamWriter(WellKnownSynchronizationKind kind, Action<ObjectWriter, CancellationToken> writer)
        {
            using var stream = SerializableBytes.CreateWritableStream();
            using var objectWriter = new ObjectWriter(stream);

            objectWriter.WriteInt32((int)kind);
            writer(objectWriter, CancellationToken.None);
            return Checksum.Create(stream);
        }
    }

    /// <summary>
    /// workspace analyzer specific asset.
    /// 
    /// we need this to prevent dlls from other languages such as typescript, f#, xaml and etc
    /// from loading at OOP start up.
    /// 
    /// unlike project analyzer, analyzer that got installed from vsix doesn't do shadow copying
    /// so we don't need to load assembly to find out actual filepath.
    /// </summary>
    internal sealed class WorkspaceAnalyzerReferenceAsset : CustomAsset
    {
        // host analyzer is not shadow copied, no need to load assembly to get real path
        // this also prevent us from loading assemblies for all vsix analyzers preemptively
        private const bool usePathFromAssembly = false;

        private readonly AnalyzerReference _reference;
        private readonly ISerializerService _serializer;

        public static WorkspaceAnalyzerReferenceAsset Create(
            AnalyzerReference reference,
            ISerializerService serializer,
            IReferenceSerializationService hostSerializationService,
            CancellationToken cancellationToken)
        {
            var checksum = Checksum.Create(
                WellKnownSynchronizationKind.AnalyzerReference,
                hostSerializationService.CreateChecksum(reference, usePathFromAssembly, cancellationToken));

            return new WorkspaceAnalyzerReferenceAsset(reference, serializer, checksum);
        }

        private WorkspaceAnalyzerReferenceAsset(AnalyzerReference reference, ISerializerService serializer, Checksum checksum)
            : base(checksum, WellKnownSynchronizationKind.AnalyzerReference)
        {
            _reference = reference;
            _serializer = serializer;
        }

        public override Task WriteObjectToAsync(ObjectWriter writer, CancellationToken cancellationToken)
        {
            _serializer.SerializeAnalyzerReference(_reference, writer, usePathFromAssembly, cancellationToken);
            return Task.CompletedTask;
        }
    }
}
