﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.DocumentHighlighting;

[DataContract]
internal readonly record struct HighlightingOptions(
    [property: DataMember(Order = 0)] bool HighlightRelatedRegexComponentsUnderCursor = true,
    [property: DataMember(Order = 1)] bool HighlightRelatedJsonComponentsUnderCursor = true)
{
    public HighlightingOptions()
        : this(HighlightRelatedRegexComponentsUnderCursor: true)
    {
    }

    public static HighlightingOptions Default = new();
}
