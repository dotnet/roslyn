// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Syntax.InternalSyntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    internal static class GreenNodeExtensions
    {
        internal static CommonSyntaxList<T> ToGreenList<T>(this SyntaxNode node) where T : InternalSyntax.CSharpSyntaxNode
        {
            return node != null ?
                ToGreenList<T>(node.Green) :
                default(CommonSyntaxList<T>);
        }

        internal static CommonSeparatedSyntaxList<T> ToGreenSeparatedList<T>(this SyntaxNode node) where T : InternalSyntax.CSharpSyntaxNode
        {
            return node != null ?
                new CommonSeparatedSyntaxList<T>(ToGreenList<T>(node.Green)) :
                default(CommonSeparatedSyntaxList<T>);
        }

        internal static CommonSyntaxList<T> ToGreenList<T>(this GreenNode node) where T : InternalSyntax.CSharpSyntaxNode
        {
            return new CommonSyntaxList<T>(node);
        }
    }
}