// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel;

internal sealed class CodeModelEvent : IEquatable<CodeModelEvent>
{
    public readonly SyntaxNode Node;
    public readonly SyntaxNode ParentNode;
    public CodeModelEventType Type;

    public CodeModelEvent(SyntaxNode node, SyntaxNode parentNode, CodeModelEventType type)
    {
        this.Node = node;
        this.ParentNode = parentNode;
        this.Type = type;
    }

    public override int GetHashCode()
        => Hash.Combine(Node, Hash.Combine(ParentNode, ((int)Type).GetHashCode()));

    public override bool Equals(object obj)
        => Equals(obj as CodeModelEvent);

    public bool Equals(CodeModelEvent other)
    {
        if (other == null)
        {
            return false;
        }

        return Node == other.Node
            && ParentNode == other.ParentNode
            && Type == other.Type;
    }
}
