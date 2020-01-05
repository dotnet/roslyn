// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Execution
{
    /// <summary>
    /// Asset that is part of solution
    /// </summary>
    internal sealed class SolutionAsset : RemotableData
    {
        private readonly object _value;
        private readonly ISerializerService _serializer;

        public SolutionAsset(Checksum checksum, object value, ISerializerService serializer)
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
}
