// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Structure;

internal class FSharpBlockStructure
{
    public ImmutableArray<FSharpBlockSpan> Spans { get; }

    public FSharpBlockStructure(ImmutableArray<FSharpBlockSpan> spans)
    {
        Spans = spans;
    }
}
