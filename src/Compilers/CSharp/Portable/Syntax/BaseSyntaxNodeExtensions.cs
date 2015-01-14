// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    internal static class GreenNodeExtensions
    {
        internal static Syntax.InternalSyntax.SyntaxList<T> ToGreenList<T>(this SyntaxNode node) where T : Syntax.InternalSyntax.CSharpSyntaxNode
        {
            return node != null ?
                ToGreenList<T>(node.Green) :
                default(Syntax.InternalSyntax.SyntaxList<T>);
        }

        internal static Syntax.InternalSyntax.SeparatedSyntaxList<T> ToGreenSeparatedList<T>(this SyntaxNode node) where T : Syntax.InternalSyntax.CSharpSyntaxNode
        {
            return node != null ?
                new Syntax.InternalSyntax.SeparatedSyntaxList<T>(ToGreenList<T>(node.Green)) :
                default(Syntax.InternalSyntax.SeparatedSyntaxList<T>);
        }

        internal static Syntax.InternalSyntax.SyntaxList<T> ToGreenList<T>(this GreenNode node) where T : Syntax.InternalSyntax.CSharpSyntaxNode
        {
            return new Syntax.InternalSyntax.SyntaxList<T>((Syntax.InternalSyntax.CSharpSyntaxNode)node);
        }
    }
}
