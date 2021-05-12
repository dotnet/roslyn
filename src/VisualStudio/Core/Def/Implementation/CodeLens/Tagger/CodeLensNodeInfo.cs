// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Language.Intellisense;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeLens.Tagger
{
    internal readonly struct CodeLensNodeInfo : IEquatable<CodeLensNodeInfo>
    {
        public readonly SyntaxNode Node;
        public readonly SyntaxToken Identifier;
        public readonly CodeElementKinds Kind;
        public readonly string Description;

        public CodeLensNodeInfo(SyntaxNode node, SyntaxToken identifier, string description, CodeElementKinds kind)
        {
            Node = node;
            Identifier = identifier;
            Description = description;
            Kind = kind;
        }

        public override bool Equals(object? obj)
        {
            return obj is CodeLensNodeInfo info && Equals(info);
        }

        public bool Equals(CodeLensNodeInfo other)
        {
            return Identifier.ValueText == other.Identifier.ValueText &&
                   Kind == other.Kind &&
                   Description == other.Description;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(this.Identifier.ValueText,
                   Hash.Combine(this.Kind.GetHashCode(), this.Description.GetHashCode()));
        }

        public static bool operator ==(CodeLensNodeInfo left, CodeLensNodeInfo right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CodeLensNodeInfo left, CodeLensNodeInfo right)
        {
            return !(left == right);
        }
    }
}
