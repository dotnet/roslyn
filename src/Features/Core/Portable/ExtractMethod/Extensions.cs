// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.CodeAnalysis.ExtractMethod;

internal static class Extensions
{
    extension(SemanticModel binding)
    {
        public ITypeSymbol? GetLambdaOrAnonymousMethodReturnType(SyntaxNode node)
        {
            var info = binding.GetSymbolInfo(node);
            if (info.Symbol == null)
            {
                return null;
            }

            var methodSymbol = info.Symbol as IMethodSymbol;
            if (methodSymbol?.MethodKind != MethodKind.AnonymousFunction)
            {
                return null;
            }

            return methodSymbol.ReturnType;
        }
    }

    extension(SemanticDocument document)
    {
        /// <summary>
        /// get tokens with given annotation in current document
        /// </summary>
        public SyntaxToken GetTokenWithAnnotation(SyntaxAnnotation annotation)
            => document.Root.GetAnnotatedNodesAndTokens(annotation).Single().AsToken();
    }

    extension(SemanticModel semanticModel)
    {
        /// <summary>
        /// resolve the given symbol against compilation this snapshot has
        /// </summary>
        public T ResolveType<T>(T symbol) where T : class, ITypeSymbol
        {
            // Can be cleaned up when https://github.com/dotnet/roslyn/issues/38061 is resolved
            var typeSymbol = (T?)symbol.GetSymbolKey().Resolve(semanticModel.Compilation).GetAnySymbol();
            Contract.ThrowIfNull(typeSymbol);
            return (T)typeSymbol.WithNullableAnnotation(symbol.NullableAnnotation);
        }
    }

    extension(SyntaxNode node)
    {
        /// <summary>
        /// check whether node contains error for itself but not from its child node
        /// </summary>
        public bool HasDiagnostics()
        {
            var set = new HashSet<Diagnostic>(node.GetDiagnostics());

            foreach (var child in node.ChildNodes())
            {
                set.ExceptWith(child.GetDiagnostics());
            }

            return set.Count > 0;
        }

        public bool FromScript()
        {
            if (node.SyntaxTree == null)
            {
                return false;
            }

            return node.SyntaxTree.Options.Kind != SourceCodeKind.Regular;
        }
    }
}
