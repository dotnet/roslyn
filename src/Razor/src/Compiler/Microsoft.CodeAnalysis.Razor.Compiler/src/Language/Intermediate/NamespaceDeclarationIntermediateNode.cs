// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class NamespaceDeclarationIntermediateNode : IntermediateNode
{
    public string? Name { get; set; }

    public bool IsPrimaryNamespace { get; init; }
    public bool IsGenericTyped { get; set; }

    public override IntermediateNodeCollection Children { get => field ??= []; }

    public override void Accept(IntermediateNodeVisitor visitor)
        => visitor.VisitNamespaceDeclaration(this);

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        formatter.WriteContent(Name);
        formatter.WriteProperty(nameof(Name), Name);
    }
}
