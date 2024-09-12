// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis;

internal partial struct SymbolKey
{
    private sealed class AliasSymbolKey : AbstractSymbolKey<IAliasSymbol>
    {
        public static readonly AliasSymbolKey Instance = new();

        public sealed override void Create(IAliasSymbol symbol, SymbolKeyWriter visitor)
        {
            visitor.WriteString(symbol.Name);
            visitor.WriteSymbolKey(symbol.Target);
            visitor.WriteString(symbol.DeclaringSyntaxReferences.FirstOrDefault()?.SyntaxTree.FilePath ?? "");
        }

        protected sealed override SymbolKeyResolution Resolve(
            SymbolKeyReader reader, IAliasSymbol? contextualSymbol, out string? failureReason)
        {
            var name = reader.ReadRequiredString();
            var targetResolution = reader.ReadSymbolKey(contextualSymbol?.Target, out var targetFailureReason);
            var filePath = reader.ReadRequiredString();

            if (targetFailureReason != null)
            {
                failureReason = $"({nameof(AliasSymbolKey)} {nameof(targetResolution)} failed -> {targetFailureReason})";
                return default;
            }

            var syntaxTree = reader.GetSyntaxTree(filePath);
            if (syntaxTree != null)
            {
                var target = targetResolution.GetAnySymbol();
                if (target != null)
                {
                    var semanticModel = reader.Compilation.GetSemanticModel(syntaxTree);
                    var result = Resolve(semanticModel, syntaxTree.GetRoot(reader.CancellationToken), name, target, reader.CancellationToken);
                    if (result.HasValue)
                    {
                        failureReason = null;
                        return result.Value;
                    }
                }
            }

            failureReason = $"({nameof(AliasSymbolKey)} '{name}' not found)";
            return default;
        }

        private static SymbolKeyResolution? Resolve(
            SemanticModel semanticModel, SyntaxNode syntaxNode, string name, ISymbol target,
            CancellationToken cancellationToken)
        {
            var symbol = semanticModel.GetDeclaredSymbol(syntaxNode, cancellationToken);
            if (symbol != null)
            {
                if (symbol.Kind == SymbolKind.Alias)
                {
                    var aliasSymbol = (IAliasSymbol)symbol;
                    if (aliasSymbol.Name == name &&
                        SymbolEquivalenceComparer.Instance.Equals(aliasSymbol.Target, target))
                    {
                        return new SymbolKeyResolution(aliasSymbol);
                    }
                }
                else if (symbol.Kind != SymbolKind.Namespace)
                {
                    // Don't recurse into anything except namespaces.  We can't find aliases
                    // any deeper than that.
                    return null;
                }
            }

            foreach (var child in syntaxNode.ChildNodesAndTokens())
            {
                if (child.IsNode)
                {
                    var result = Resolve(semanticModel, child.AsNode()!, name, target, cancellationToken);
                    if (result.HasValue)
                    {
                        return result;
                    }
                }
            }

            return null;
        }
    }
}
