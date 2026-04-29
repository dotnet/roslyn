// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.Razor.Protocol.DevTools;

[DataContract]
internal sealed class SyntaxVisualizerTree
{
    [DataMember(Order = 0)]
    public required SyntaxVisualizerNode Root { get; set; }
}
