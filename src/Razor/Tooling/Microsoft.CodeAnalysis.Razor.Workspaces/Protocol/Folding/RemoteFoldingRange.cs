// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.Razor.Protocol.Folding;

[DataContract]
internal readonly record struct RemoteFoldingRange(
    [property: DataMember(Order = 0)] int StartLine,
    [property: DataMember(Order = 1)] int? StartCharacter,
    [property: DataMember(Order = 2)] int EndLine,
    [property: DataMember(Order = 3)] int? EndCharacter,
    [property: DataMember(Order = 4)] string? Kind,
    [property: DataMember(Order = 5)] string? CollapsedText)
{
    public override string ToString()
    {
        return $"({StartLine}, {StartCharacter})-({EndLine}, {EndCharacter}), {Kind}, {CollapsedText}";
    }

    public static RemoteFoldingRange FromLspFoldingRange(FoldingRange r)
        => new(
            r.StartLine,
            r.StartCharacter,
            r.EndLine,
            r.EndCharacter,
            r.Kind?.Value,
            r.CollapsedText);

    public static FoldingRange ToLspFoldingRange(RemoteFoldingRange r)
        => new()
        {
            StartLine = r.StartLine,
            StartCharacter = r.StartCharacter,
            EndLine = r.EndLine,
            EndCharacter = r.EndCharacter,
            Kind = r.Kind is null ? null : new FoldingRangeKind(r.Kind),
            CollapsedText = r.CollapsedText
        };
}
