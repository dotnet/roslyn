// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class SetKeyIntermediateNode : IntermediateNode
{
    public IntermediateToken KeyValueToken { get; }

    public override IntermediateNodeCollection Children => IntermediateNodeCollection.ReadOnly;

    public SetKeyIntermediateNode(IntermediateToken keyValueToken)
    {
        ArgHelper.ThrowIfNull(keyValueToken);

        KeyValueToken = keyValueToken;
        Source = KeyValueToken.Source;
    }

    public override void Accept(IntermediateNodeVisitor visitor)
    {
        visitor.VisitSetKey(this);
    }

    public override void FormatNode(IntermediateNodeFormatter formatter)
    {
        formatter.WriteContent(KeyValueToken.Content);

        formatter.WriteProperty(nameof(KeyValueToken), KeyValueToken.Content);
    }
}
