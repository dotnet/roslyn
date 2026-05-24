// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class SplatIntermediateNode : IntermediateNode
{
    public override IntermediateNodeCollection Children { get => field ??= []; }

    public override void Accept(IntermediateNodeVisitor visitor)
    {
        visitor.VisitSplat(this);
    }

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        // No properties => do nothing
    }
}
