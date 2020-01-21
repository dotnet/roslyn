// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Struct containing information about a source declaration.
    /// </summary>
    internal readonly struct DeclarationInfo
    {
        private readonly SyntaxNode _declaredNode;
        private readonly ImmutableArray<SyntaxNode> _executableCodeBlocks;
        private readonly ISymbol _declaredSymbol;

        internal DeclarationInfo(SyntaxNode declaredNode, ImmutableArray<SyntaxNode> executableCodeBlocks, ISymbol declaredSymbol)
        {
            Debug.Assert(declaredNode != null);
            Debug.Assert(!executableCodeBlocks.IsDefault);

            // TODO: Below assert has been commented out as is not true for VB field decls where multiple variables can share same initializer.
            // Declared node is the identifier, which doesn't contain the initializer. Can we tweak the assert somehow to handle this case?
            // Debug.Assert(executableCodeBlocks.All(n => n.Ancestors().Contains(declaredNode)));

            _declaredNode = declaredNode;
            _executableCodeBlocks = executableCodeBlocks;
            _declaredSymbol = declaredSymbol;
        }

        /// <summary>
        /// Topmost syntax node for this declaration.
        /// </summary>
        public SyntaxNode DeclaredNode => _declaredNode;

        /// <summary>
        /// Syntax nodes for executable code blocks (method body, initializers, etc.) associated with this declaration.
        /// </summary>
        public ImmutableArray<SyntaxNode> ExecutableCodeBlocks => _executableCodeBlocks;

        /// <summary>
        /// Symbol declared by this declaration.
        /// </summary>
        public ISymbol DeclaredSymbol => _declaredSymbol;
    }
}
