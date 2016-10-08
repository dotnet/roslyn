// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Execution
{
    /// <summary>
    /// Asset that is not part of solution, but want to participate in ISolutionSynchronizationService
    /// </summary>
    internal abstract class CustomAsset : SynchronizationObject
    {
        public CustomAsset(Checksum checksum, string kind) : base(checksum, kind)
        {
        }
    }

    /// <summary>
    /// helper type for custom asset
    /// </summary>
    internal sealed class SimpleCustomAsset : CustomAsset
    {
        private readonly Action<ObjectWriter, CancellationToken> _writer;

        public SimpleCustomAsset(string kind, Action<ObjectWriter, CancellationToken> writer) :
            base(Checksum.Create(kind, writer), kind)
        {
            // unlike SolutionAsset which gets checksum from solution states, this one build one by itself.
            _writer = writer;
        }

        public override Task WriteObjectToAsync(ObjectWriter writer, CancellationToken cancellationToken)
        {
            _writer(writer, cancellationToken);
            return SpecializedTasks.EmptyTask;
        }
    }
}
