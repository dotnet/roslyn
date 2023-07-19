// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal readonly struct AliasAndUsingDirective
    {
        public readonly AliasSymbol Alias;
        public readonly SyntaxReference? UsingDirectiveReference;

        public AliasAndUsingDirective(AliasSymbol alias, UsingDirectiveSyntax? usingDirective)
        {
            this.Alias = alias;
            this.UsingDirectiveReference = usingDirective?.GetReference();
        }

        public UsingDirectiveSyntax? UsingDirective => (UsingDirectiveSyntax?)UsingDirectiveReference?.GetSyntax();
    }
}
