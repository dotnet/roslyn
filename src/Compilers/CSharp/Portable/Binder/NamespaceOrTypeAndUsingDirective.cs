// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal struct NamespaceOrTypeAndUsingDirective
    {
        public readonly NamespaceOrTypeSymbol NamespaceOrType;
        public readonly UsingDirectiveSyntax UsingDirective;

        public NamespaceOrTypeAndUsingDirective(NamespaceOrTypeSymbol namespaceOrType, UsingDirectiveSyntax usingDirective)
        {
            this.NamespaceOrType = namespaceOrType;
            this.UsingDirective = usingDirective;
        }
    }
}
