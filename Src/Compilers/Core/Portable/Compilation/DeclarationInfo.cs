// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Struct containing information about a source declaration.
    /// </summary>
    public struct DeclarationInfo
    {
        private readonly SyntaxNode declaredNode;
        private readonly ImmutableArray<SyntaxNode> executableCodeBlocks;
        private readonly ISymbol declaredSymbol;

        internal DeclarationInfo(SyntaxNode declaredNode, ImmutableArray<SyntaxNode> executableCodeBlocks, ISymbol declaredSymbol)
        {
            Debug.Assert(declaredNode != null);
            Debug.Assert(!executableCodeBlocks.IsDefault);
            Debug.Assert(executableCodeBlocks.All(n => n.Ancestors().Any(a => a == declaredNode)));

            this.declaredNode = declaredNode;
            this.executableCodeBlocks = executableCodeBlocks;
            this.declaredSymbol = declaredSymbol;
        }

        /// <summary>
        /// Topmost syntax node for this declaration.
        /// </summary>
        public SyntaxNode DeclaredNode { get { return this.declaredNode; } }

        /// <summary>
        /// Syntax nodes for executable code blocks (method body, initializers, etc.) associated with this declaration.
        /// </summary>
        public ImmutableArray<SyntaxNode> ExecutableCodeBlocks { get { return this.executableCodeBlocks; } }

        /// <summary>
        /// Symbol declared by this declaration.
        /// </summary>
        public ISymbol DeclaredSymbol { get { return this.declaredSymbol; } }
    }
}