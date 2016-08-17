// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Syntax.InternalSyntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    internal static class GreenNodeExtensions
    {
        internal static Microsoft.CodeAnalysis.Syntax.InternalSyntax.SyntaxList<T> ToGreenList<T>(this SyntaxNode node) where T : InternalSyntax.CSharpSyntaxNode
        {
            return node != null ?
                ToGreenList<T>(node.Green) :
                default(CodeAnalysis.Syntax.InternalSyntax.SyntaxList<T>);
        }

        internal static Microsoft.CodeAnalysis.Syntax.InternalSyntax.SeparatedSyntaxList<T> ToGreenSeparatedList<T>(this SyntaxNode node) where T : InternalSyntax.CSharpSyntaxNode
        {
            return node != null ?
                new CodeAnalysis.Syntax.InternalSyntax.SeparatedSyntaxList<T>(ToGreenList<T>(node.Green)) :
                default(CodeAnalysis.Syntax.InternalSyntax.SeparatedSyntaxList<T>);
        }

        internal static Microsoft.CodeAnalysis.Syntax.InternalSyntax.SyntaxList<T> ToGreenList<T>(this GreenNode node) where T : InternalSyntax.CSharpSyntaxNode
        {
            return new CodeAnalysis.Syntax.InternalSyntax.SyntaxList<T>(node);
        }
    }
}