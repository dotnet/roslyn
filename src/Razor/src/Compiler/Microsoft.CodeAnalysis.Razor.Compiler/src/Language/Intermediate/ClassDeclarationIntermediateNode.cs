// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class ClassDeclarationIntermediateNode : MemberDeclarationIntermediateNode
{
    public string? Name { get; set; }
    public BaseTypeWithModel? BaseType { get; set; }
    public ImmutableArray<string> Modifiers { get; set => field = value.NullToEmpty(); } = [];
    public ImmutableArray<IntermediateToken> Interfaces { get; set => field = value.NullToEmpty(); } = [];
    public ImmutableArray<TypeParameter> TypeParameters { get; set => field = value.NullToEmpty(); } = [];

    public bool IsPrimaryClass { get; init; }
    public bool NullableContext { get; set; }

    public override IntermediateNodeCollection Children { get => field ??= []; }

    public override void Accept(IntermediateNodeVisitor visitor)
        => visitor.VisitClassDeclaration(this);

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        formatter.WriteContent(Name);
        formatter.WriteProperty(nameof(Name), Name);
        formatter.WriteProperty(nameof(Interfaces), string.Join(", ", Interfaces.Select(i => i.Content)));
        formatter.WriteProperty(nameof(Modifiers), string.Join(", ", Modifiers));
        formatter.WriteProperty(nameof(TypeParameters), string.Join(", ", TypeParameters.Select(t => t.Name.Content)));
    }
}
