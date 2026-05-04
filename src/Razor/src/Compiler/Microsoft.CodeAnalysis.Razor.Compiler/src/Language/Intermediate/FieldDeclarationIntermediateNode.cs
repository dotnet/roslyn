// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class FieldDeclarationIntermediateNode : MemberDeclarationIntermediateNode
{
    public required string Name { get; init; }
    public required string Type { get; init; }
    public ImmutableArray<string> Modifiers { get; init => field = value.NullToEmpty(); } = [];
    public ImmutableArray<string> SuppressWarnings { get; init => field = value.NullToEmpty(); } = [];

    public bool IsTagHelperField { get; init; }

    public override IntermediateNodeCollection Children => IntermediateNodeCollection.ReadOnly;

    public override void Accept(IntermediateNodeVisitor visitor)
        => visitor.VisitFieldDeclaration(this);

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        formatter.WriteContent(Name);

        formatter.WriteProperty(nameof(Name), Name);
        formatter.WriteProperty(nameof(Type), Type);
        formatter.WriteProperty(nameof(Modifiers), string.Join(" ", Modifiers));
    }
}
