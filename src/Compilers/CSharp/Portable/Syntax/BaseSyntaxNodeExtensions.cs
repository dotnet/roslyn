// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using CoreInternalSyntax = Microsoft.CodeAnalysis.Syntax.InternalSyntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    internal static class GreenNodeExtensions
    {
        internal static CoreInternalSyntax.CommonSyntaxList<T> ToGreenList<T>(this SyntaxNode node) where T : Syntax.InternalSyntax.CSharpSyntaxNode
        {
            return node != null ?
                ToGreenList<T>(node.Green) :
                default(CoreInternalSyntax.CommonSyntaxList<T>);
        }

        internal static CoreInternalSyntax.CommonSeparatedSyntaxList<T> ToGreenSeparatedList<T>(this SyntaxNode node) where T : Syntax.InternalSyntax.CSharpSyntaxNode
        {
            return node != null ?
                new CoreInternalSyntax.CommonSeparatedSyntaxList<T>(ToGreenList<T>(node.Green)) :
                default(CoreInternalSyntax.CommonSeparatedSyntaxList<T>);
        }

        internal static CoreInternalSyntax.CommonSyntaxList<T> ToGreenList<T>(this GreenNode node) where T : Syntax.InternalSyntax.CSharpSyntaxNode
        {
            return new CoreInternalSyntax.CommonSyntaxList<T>((Syntax.InternalSyntax.CSharpSyntaxNode)node);
        }
    }
}