// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class PropertyDeclarationIntermediateNode : MemberDeclarationIntermediateNode
{
    public required string Name { get; init; }
    public required IntermediateToken Type { get; init; }
    public required string ExpressionBody { get; init; }
    public ImmutableArray<string> Modifiers { get; init => field = value.NullToEmpty(); } = [];

    public override IntermediateNodeCollection Children => IntermediateNodeCollection.ReadOnly;

    public override void Accept(IntermediateNodeVisitor visitor)
        => visitor.VisitPropertyDeclaration(this);
}
