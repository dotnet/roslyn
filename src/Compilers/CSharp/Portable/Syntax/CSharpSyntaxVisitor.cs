// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Represents a <see cref="CSharpSyntaxNode"/> visitor that visits only the single CSharpSyntaxNode
    /// passed into its Visit method and produces 
    /// a value of the type specified by the <typeparamref name="TResult"/> parameter.
    /// </summary>
    /// <typeparam name="TResult">
    /// The type of the return value this visitor's Visit method.
    /// </typeparam>
    public abstract partial class CSharpSyntaxVisitor<TResult>
    {
        public virtual TResult Visit(SyntaxNode node)
        {
            if (node != null)
            {
                return ((CSharpSyntaxNode)node).Accept(this);
            }

            // should not come here too often so we will put this at the end of the method.
            return default(TResult);
        }

        public virtual TResult DefaultVisit(SyntaxNode node)
        {
            return default(TResult);
        }
    }

    /// <summary>
    /// Represents a <see cref="CSharpSyntaxNode"/> visitor that visits only the single CSharpSyntaxNode
    /// passed into its Visit method.
    /// </summary>
    public abstract partial class CSharpSyntaxVisitor
    {
        public virtual void Visit(SyntaxNode node)
        {
            if (node != null)
            {
                ((CSharpSyntaxNode)node).Accept(this);
            }
        }

        public virtual void DefaultVisit(SyntaxNode node)
        {
        }
    }
}
