// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class CSharpCodeIntermediateNode : IntermediateNode
{
    public override IntermediateNodeCollection Children { get => field ??= []; }

    public override void Accept(IntermediateNodeVisitor visitor)
    {
        if (visitor == null)
        {
            throw new ArgumentNullException(nameof(visitor));
        }

        visitor.VisitCSharpCode(this);
    }

    protected override IntermediateNode CloneNode()
    {
        var clone = new CSharpCodeIntermediateNode
        {
            IsSynthesizedHelper = IsSynthesizedHelper,
        };

        return clone;
    }

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        formatter.WriteChildren(Children);
    }
}
