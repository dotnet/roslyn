using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal partial struct SymbolKey
    {
        private static class AliasSymbolKey
        {
            public static void Create(IAliasSymbol symbol, SymbolKeyWriter visitor)
            {
                visitor.WriteString(symbol.Name);
                visitor.WriteSymbolKey(symbol.Target);
                visitor.WriteString(symbol.DeclaringSyntaxReferences.FirstOrDefault()?.SyntaxTree.FilePath ?? "");
            }

            public static int GetHashCode(GetHashCodeReader reader)
            {
                return
                    Hash.Combine(reader.ReadString(),
                    Hash.Combine(reader.ReadSymbolKey(),
                                 reader.ReadString()));
            }

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader)
            {
                var name = reader.ReadString();
                var targetResolution = reader.ReadSymbolKey();
                var filePath = reader.ReadString();

                var syntaxTree = reader.Compilation.SyntaxTrees.FirstOrDefault(t => t.FilePath == filePath);
                if (syntaxTree != null)
                {
                    var target = targetResolution.GetAnySymbol();
                    if (target != null)
                    {
                        var semanticModel = reader.Compilation.GetSemanticModel(syntaxTree);
                        var result = Resolve(semanticModel, syntaxTree.GetRoot(reader.CancellationToken), name, target, reader.CancellationToken);
                        if (result.HasValue)
                        {
                            return result.Value;
                        }
                    }
                }

                return default(SymbolKeyResolution);
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
                        var result = Resolve(semanticModel, child.AsNode(), name, target, cancellationToken);
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
}