// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CustomMessageHandler;

[DataContract]
internal readonly record struct HandleCustomMessageResponse
{
    [DataMember(Order = 0)]
    public required string Message { get; init; }

    [DataMember(Order = 1)]
    public LinePosition[]? Positions { get; init; }
}
