// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Asset that is part of solution
    /// </summary>
    internal sealed class SolutionAsset : RemotableData
    {
        private readonly object _value;

        public SolutionAsset(Checksum checksum, object value)
            : base(checksum, value.GetWellKnownSynchronizationKind())
        {
            _value = value;
        }

        public override Task WriteObjectToAsync(ObjectWriter writer, ISerializerService serializer, CancellationToken cancellationToken)
        {
            serializer.Serialize(_value, writer, cancellationToken);
            return Task.CompletedTask;
        }
    }
}
