// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.DocumentHighlighting;

[DataContract]
internal readonly record struct HighlightingOptions()
{
    [DataMember] public bool HighlightRelatedRegexComponentsUnderCursor { get; init; } = true;
    [DataMember] public bool HighlightRelatedJsonComponentsUnderCursor { get; init; } = true;
    [DataMember] public bool FrozenPartialSemantics { get; init; }

    public static HighlightingOptions Default = new();
}
