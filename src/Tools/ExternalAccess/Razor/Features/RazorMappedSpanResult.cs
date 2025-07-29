// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor;

[DataContract]
internal readonly struct RazorMappedSpanResult
{
    [DataMember(Order = 0)]
    public readonly string FilePath;

    [DataMember(Order = 1)]
    public readonly LinePositionSpan LinePositionSpan;

    [DataMember(Order = 2)]
    public readonly TextSpan Span;

    public RazorMappedSpanResult(string filePath, LinePositionSpan linePositionSpan, TextSpan span)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            throw new ArgumentException(nameof(filePath));
        }

        FilePath = filePath;
        LinePositionSpan = linePositionSpan;
        Span = span;
    }

    public bool IsDefault => FilePath == null;
}

[DataContract]
internal readonly record struct RazorMappedEditResult(
    [property: DataMember(Order = 0)] string FilePath,
    [property: DataMember(Order = 1)] TextChange[] TextChanges)
{
    public bool IsDefault => FilePath == null || TextChanges == null;
}
