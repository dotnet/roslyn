// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    using Microsoft.CodeAnalysis.Syntax.InternalSyntax;

    internal static class GreenNodeExtensions
    {
        internal static SyntaxList<T> ToGreenList<T>(this SyntaxNode node) where T : InternalSyntax.CSharpSyntaxNode
        {
            return node != null ?
                ToGreenList<T>(node.Green) :
                default(SyntaxList<T>);
        }

        internal static SeparatedSyntaxList<T> ToGreenSeparatedList<T>(this SyntaxNode node) where T : InternalSyntax.CSharpSyntaxNode
        {
            return node != null ?
                new SeparatedSyntaxList<T>(ToGreenList<T>(node.Green)) :
                default(SeparatedSyntaxList<T>);
        }

        internal static SyntaxList<T> ToGreenList<T>(this GreenNode node) where T : InternalSyntax.CSharpSyntaxNode
        {
            return new SyntaxList<T>(node);
        }
    }
}