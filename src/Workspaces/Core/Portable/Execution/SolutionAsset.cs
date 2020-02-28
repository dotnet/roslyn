// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
