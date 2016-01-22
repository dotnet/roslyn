// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal class DeclarationComputer
    {
        internal static DeclarationInfo GetDeclarationInfo(SemanticModel model, SyntaxNode node, bool getSymbol, IEnumerable<SyntaxNode> executableCodeBlocks, CancellationToken cancellationToken)
        {
            var declaredSymbol = GetDeclaredSymbol(model, node, getSymbol, cancellationToken);
            var codeBlocks = executableCodeBlocks?.Where(c => c != null).AsImmutableOrEmpty() ?? ImmutableArray<SyntaxNode>.Empty;
            return new DeclarationInfo(node, codeBlocks, declaredSymbol);
        }

        internal static DeclarationInfo GetDeclarationInfo(SemanticModel model, SyntaxNode node, bool getSymbol, CancellationToken cancellationToken)
        {
            return GetDeclarationInfo(model, node, getSymbol, (IEnumerable<SyntaxNode>)null, cancellationToken);
        }

        internal static DeclarationInfo GetDeclarationInfo(SemanticModel model, SyntaxNode node, bool getSymbol, SyntaxNode executableCodeBlock, CancellationToken cancellationToken)
        {
            return GetDeclarationInfo(model, node, getSymbol, SpecializedCollections.SingletonEnumerable(executableCodeBlock), cancellationToken);
        }

        internal static DeclarationInfo GetDeclarationInfo(SemanticModel model, SyntaxNode node, bool getSymbol, CancellationToken cancellationToken, params SyntaxNode[] executableCodeBlocks)
        {
            return GetDeclarationInfo(model, node, getSymbol, executableCodeBlocks.AsEnumerable(), cancellationToken);
        }

        private static ISymbol GetDeclaredSymbol(SemanticModel model, SyntaxNode node, bool getSymbol, CancellationToken cancellationToken)
        {
            if (!getSymbol)
            {
                return null;
            }

            var declaredSymbol = model.GetDeclaredSymbol(node, cancellationToken);

            // For namespace declarations, GetDeclaredSymbol returns a compilation scoped namespace symbol,
            // which includes declarations across the compilation, including those in referenced assemblies.
            // However, we are only interested in the namespace symbol scoped to the compilation's source assembly.
            var namespaceSymbol = declaredSymbol as INamespaceSymbol;
            if (namespaceSymbol != null && namespaceSymbol.ConstituentNamespaces.Length > 1)
            {
                var assemblyToScope = model.Compilation.Assembly;
                var assemblyScopedNamespaceSymbol = namespaceSymbol.ConstituentNamespaces.FirstOrDefault(ns => ns.ContainingAssembly == assemblyToScope);
                if (assemblyScopedNamespaceSymbol != null)
                {
                    Debug.Assert(assemblyScopedNamespaceSymbol.ConstituentNamespaces.Length == 1);
                    declaredSymbol = assemblyScopedNamespaceSymbol;
                }
            }

            return declaredSymbol;
        }
    }
}
