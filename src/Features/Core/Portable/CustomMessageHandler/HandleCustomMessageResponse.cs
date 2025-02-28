// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CustomMessageHandler;

/// <summary>
/// Return type for the <see cref="IRemoteCustomMessageHandlerService.HandleCustomMessageAsync"/> request.
/// </summary>
[DataContract]
internal readonly struct HandleCustomMessageResponse
{
    /// <summary>
    /// Gets the json response returned by the custom message handler.
    /// </summary>
    [DataMember(Order = 0)]
    public required string Response { get; init; }

    /// <summary>
    /// Gets the list of <see cref="LinePosition"/> objects the <see cref="Response"/> refers to.
    /// </summary>
    /// <remarks>
    /// All elemements in <see cref="Positions"/> refer to the <see cref="DocumentId"/> passed as parameter
    /// to <see cref="IRemoteCustomMessageHandlerService.HandleCustomMessageAsync"/>.
    /// </remarks>
    [DataMember(Order = 1)]
    public ImmutableArray<LinePosition> Positions { get; init; }
}
