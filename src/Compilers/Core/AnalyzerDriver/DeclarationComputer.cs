// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
namespace Microsoft.CodeAnalysis
{
    internal class DeclarationComputer
    {
        internal static DeclarationInfo GetDeclarationInfo(SemanticModel model, SyntaxNode node, bool getSymbol, ArrayBuilder<SyntaxNode>? executableCodeBlocks, CancellationToken cancellationToken)
        {
            var declaredSymbol = GetDeclaredSymbol(model, node, getSymbol, cancellationToken);
            return GetDeclarationInfo(node, declaredSymbol, executableCodeBlocks);
        }

        internal static DeclarationInfo GetDeclarationInfo(SyntaxNode node, ISymbol? declaredSymbol, ArrayBuilder<SyntaxNode>? executableCodeBlocks)
        {
            var codeBlocks = ImmutableArray<SyntaxNode>.Empty;
            if (executableCodeBlocks != null)
            {
                executableCodeBlocks.RemoveAll(c => c == null);

                codeBlocks = executableCodeBlocks.ToImmutable();
            }

            return new DeclarationInfo(node, codeBlocks, declaredSymbol);
        }

        internal static DeclarationInfo GetDeclarationInfo(SemanticModel model, SyntaxNode node, bool getSymbol, CancellationToken cancellationToken)
        {
            return GetDeclarationInfo(model, node, getSymbol, (ArrayBuilder<SyntaxNode>?)null, cancellationToken);
        }

        internal static DeclarationInfo GetDeclarationInfo(SemanticModel model, SyntaxNode node, bool getSymbol, SyntaxNode executableCodeBlock, CancellationToken cancellationToken)
        {
            var builder = ArrayBuilder<SyntaxNode>.GetInstance();
            builder.Add(executableCodeBlock);

            var result = GetDeclarationInfo(model, node, getSymbol, builder, cancellationToken);

            builder.Free();
            return result;
        }

        internal static DeclarationInfo GetDeclarationInfo(SemanticModel model, SyntaxNode node, bool getSymbol, CancellationToken cancellationToken, params SyntaxNode[] executableCodeBlocks)
        {
            var builder = ArrayBuilder<SyntaxNode>.GetInstance();
            builder.AddRange(executableCodeBlocks);

            var result = GetDeclarationInfo(model, node, getSymbol, builder, cancellationToken);

            builder.Free();
            return result;
        }

        private static ISymbol? GetDeclaredSymbol(SemanticModel model, SyntaxNode node, bool getSymbol, CancellationToken cancellationToken)
        {
            if (!getSymbol)
            {
                return null;
            }

            var declaredSymbol = model.GetDeclaredSymbol(node, cancellationToken);

            // For namespace declarations, GetDeclaredSymbol returns a compilation scoped namespace symbol,
            // which includes declarations across the compilation, including those in referenced assemblies.
            // However, we are only interested in the namespace symbol scoped to the compilation's source assembly.
            if (declaredSymbol is INamespaceSymbol namespaceSymbol && namespaceSymbol.ConstituentNamespaces.Length > 1)
            {
                var assemblyToScope = model.Compilation.Assembly;
                var assemblyScopedNamespaceSymbol = namespaceSymbol.ConstituentNamespaces.FirstOrDefault(static (ns, assemblyToScope) => ns.ContainingAssembly == assemblyToScope, assemblyToScope);
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
