// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.Razor.Protocol.DevTools;

[DataContract]
internal sealed class SyntaxVisualizerNode
{
    [DataMember(Order = 0)]
    public required string Kind { get; set; }

    [DataMember(Order = 1)]
    public required int SpanStart { get; set; }

    [DataMember(Order = 2)]
    public required int SpanEnd { get; set; }

    [DataMember(Order = 3)]
    public required SyntaxVisualizerNode[] Children { get; set; }
}
