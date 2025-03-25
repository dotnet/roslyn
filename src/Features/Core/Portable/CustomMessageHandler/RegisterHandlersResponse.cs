// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.CustomMessageHandler;

[DataContract]
internal readonly struct RegisterHandlersResponse(string[] handlers, string[] documentHandlers)
{
    [DataMember(Order = 0)]
    public string[] Handlers { get; } = handlers;

    [DataMember(Order = 1)]
    public string[] DocumentHandlers { get; } = documentHandlers;
}
