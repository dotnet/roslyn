// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.DocumentHighlighting;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.DocumentHighlighting;
using Xunit;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.UnitTests;

public class FSharpHighlightSpanKindTests
{
    public static IEnumerable<object[]> enumValues()
    {
        foreach (var number in Enum.GetValues(typeof(FSharpHighlightSpanKind)))
        {
            yield return new object[] { number };
        }
    }

    internal static HighlightSpanKind GetExpectedHighlightSpanKind(FSharpHighlightSpanKind kind)
    {
        switch (kind)
        {
            case FSharpHighlightSpanKind.None:
                {
                    return HighlightSpanKind.None;
                }

            case FSharpHighlightSpanKind.Definition:
                {
                    return HighlightSpanKind.Definition;
                }

            case FSharpHighlightSpanKind.Reference:
                {
                    return HighlightSpanKind.Reference;
                }

            case FSharpHighlightSpanKind.WrittenReference:
                {
                    return HighlightSpanKind.WrittenReference;
                }

            default:
                {
                    throw ExceptionUtilities.UnexpectedValue(kind);
                }
        }
    }

    [Theory]
    [MemberData(nameof(enumValues))]
    internal void MapsCorrectly(FSharpHighlightSpanKind kind)
    {
        var actual = FSharpHighlightSpanKindHelpers.ConvertTo(kind);
        var expected = GetExpectedHighlightSpanKind(kind);
        Assert.Equal(expected, actual);
    }
}
