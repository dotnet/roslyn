// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.Remote;

[DataContract]
internal readonly struct RemoteMappedSpanResult
{
    [DataMember(Order = 0)]
    public readonly string FilePath;

    [DataMember(Order = 1)]
    public readonly LinePositionSpan LinePositionSpan;

    [DataMember(Order = 2)]
    public readonly TextSpan Span;

    public RemoteMappedSpanResult(string filePath, LinePositionSpan linePositionSpan, TextSpan span)
    {
        FilePath = filePath;
        LinePositionSpan = linePositionSpan;
        Span = span;
    }

    public bool IsDefault => FilePath == null;
}

[DataContract]
internal readonly record struct RemoteMappedEditResult(
    [property: DataMember(Order = 0)] string FilePath,
    [property: DataMember(Order = 1)] TextChange[] TextChanges)
{
    public bool IsDefault => FilePath == null || TextChanges == null;
}
