// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        public virtual TResult? Visit(SyntaxNode? node)
        {
            if (node != null)
            {
                return ((CSharpSyntaxNode)node).Accept(this);
            }

            // should not come here too often so we will put this at the end of the method.
            return default;
        }

        public virtual TResult? DefaultVisit(SyntaxNode node)
        {
            return default;
        }
    }

    /// <summary>
    /// Represents a <see cref="CSharpSyntaxNode"/> visitor that visits only the single CSharpSyntaxNode
    /// passed into its Visit method.
    /// </summary>
    public abstract partial class CSharpSyntaxVisitor
    {
        public virtual void Visit(SyntaxNode? node)
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
