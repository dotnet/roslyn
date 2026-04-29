// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class MethodDeclarationIntermediateNode : MemberDeclarationIntermediateNode
{
    public string? Name { get; set; }
    public string? ReturnType { get; set; }

    public ImmutableArray<string> Modifiers { get; set => field = value.NullToEmpty(); } = [];
    public ImmutableArray<MethodParameter> Parameters { get; set => field = value.NullToEmpty(); } = [];

    public bool IsPrimaryMethod { get; init; }

    public override IntermediateNodeCollection Children { get => field ??= []; }

    public override void Accept(IntermediateNodeVisitor visitor)
        => visitor.VisitMethodDeclaration(this);

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        formatter.WriteContent(Name);

        formatter.WriteProperty(nameof(Name), Name);
        formatter.WriteProperty(nameof(Modifiers), string.Join(", ", Modifiers));
        formatter.WriteProperty(nameof(Parameters), string.Join(", ", Parameters.Select(FormatMethodParameter)));
        formatter.WriteProperty(nameof(ReturnType), ReturnType);
    }

    private static string FormatMethodParameter(MethodParameter parameter)
    {
        using var _ = StringBuilderPool.GetPooledObject(out var builder);

        foreach (var modifier in parameter.Modifiers)
        {
            builder.Append(modifier);
            builder.Append(' ');
        }

        builder.Append(parameter.Type);
        builder.Append(' ');

        builder.Append(parameter.Name);

        return builder.ToString();
    }
}
