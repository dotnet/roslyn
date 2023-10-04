// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Serialization;

internal interface ISerializerService : IWorkspaceService
{
    void Serialize(object value, ObjectWriter writer, SolutionReplicationContext context, CancellationToken cancellationToken);

    void SerializeParseOptions(ParseOptions options, ObjectWriter writer);

    T? Deserialize<T>(WellKnownSynchronizationKind kind, ObjectReader reader, CancellationToken cancellationToken);

    Checksum CreateChecksum(object value, CancellationToken cancellationToken);
    Checksum CreateParseOptionsChecksum(ParseOptions value);
}
