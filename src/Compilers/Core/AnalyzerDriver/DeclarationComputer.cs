// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Microsoft.CodeAnalysis
{
    internal class DeclarationComputer
    {
        internal static DeclarationInfo GetDeclarationInfo(SemanticModel model, SyntaxNode node, bool getSymbol, IEnumerable<SyntaxNode> executableCodeBlocks, CancellationToken cancellationToken)
        {
            var declaredSymbol = getSymbol ? model.GetDeclaredSymbol(node, cancellationToken) : null;
            var codeBlocks = executableCodeBlocks?.Where(c => c != null).AsImmutableOrEmpty() ?? ImmutableArray<SyntaxNode>.Empty;
            return new DeclarationInfo(node, codeBlocks, declaredSymbol);
        }

        internal static DeclarationInfo GetDeclarationInfo(SemanticModel model, SyntaxNode node, bool getSymbol, CancellationToken cancellationToken)
        {
            return GetDeclarationInfo(model, node, getSymbol, (IEnumerable<SyntaxNode>)null, cancellationToken);
        }

        internal static DeclarationInfo GetDeclarationInfo(SemanticModel model, SyntaxNode node, bool getSymbol, SyntaxNode executableCodeBlock, CancellationToken cancellationToken)
        {
            var declaredSymbol = getSymbol ? model.GetDeclaredSymbol(node, cancellationToken) : null;
            var codeBlock = executableCodeBlock == null ? ImmutableArray<SyntaxNode>.Empty : ImmutableArray.Create(executableCodeBlock);
            return new DeclarationInfo(node, codeBlock, declaredSymbol);
        }

        internal static DeclarationInfo GetDeclarationInfo(SemanticModel model, SyntaxNode node, bool getSymbol, CancellationToken cancellationToken, params SyntaxNode[] executableCodeBlocks)
        {
            return GetDeclarationInfo(model, node, getSymbol, executableCodeBlocks.AsEnumerable(), cancellationToken);
        }
    }
}
