// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A reference to a syntax node.
    /// </summary>
    public abstract class SyntaxReference
    {
        /// <summary>
        /// The syntax tree that this references a node within.
        /// </summary>
        public abstract SyntaxTree SyntaxTree { get; }

        /// <summary>
        /// The span of the node referenced.
        /// </summary>
        public abstract TextSpan Span { get; }

        /// <summary>
        /// Retrieves the original referenced syntax node.  
        /// This action may cause a parse to happen to recover the syntax node.
        /// </summary>
        /// <returns>The original referenced syntax node.</returns>
        public abstract SyntaxNode GetSyntax(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Retrieves the original referenced syntax node.  
        /// This action may cause a parse to happen to recover the syntax node.
        /// </summary>
        /// <returns>The original referenced syntax node.</returns>
        public virtual Task<SyntaxNode> GetSyntaxAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(this.GetSyntax(cancellationToken));
        }

        /// <summary>
        /// The location of this syntax reference.
        /// </summary>
        /// <returns>The location of this syntax reference.</returns>
        /// <remarks>
        /// More performant than GetSyntax().GetLocation().
        /// </remarks>
        internal Location GetLocation()
        {
            return this.SyntaxTree.GetLocation(this.Span);
        }
    }
}
