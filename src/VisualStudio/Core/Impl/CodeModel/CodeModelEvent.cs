// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
{
    internal class CodeModelEvent : IEquatable<CodeModelEvent>
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
        {
            return Hash.Combine(Node, Hash.Combine(ParentNode, Type.GetHashCode()));
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as CodeModelEvent);
        }

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
}
