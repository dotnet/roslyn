// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Language.Intellisense;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeLens.Tagger
{
    internal readonly struct CodeLensNodeInfo
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
    }
}
