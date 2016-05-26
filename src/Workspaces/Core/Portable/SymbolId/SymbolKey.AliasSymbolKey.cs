using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Newtonsoft.Json;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal abstract partial class SymbolKey
    {
        [JsonObject]
        private class AliasSymbolKey : AbstractSymbolKey<AliasSymbolKey>
        {
            [JsonProperty] private readonly string _name;
            [JsonProperty] private readonly SymbolKey _target;
            [JsonProperty] private readonly string _filePath;

            [JsonConstructor]
            internal AliasSymbolKey(string _name, SymbolKey _target, string _filePath)
            {
                this._name = _name;
                this._target = _target;
                this._filePath = _filePath;
            }

            public AliasSymbolKey(IAliasSymbol aliasSymbol, Visitor visitor)
            {
                _name = aliasSymbol.Name;
                _target = GetOrCreate(aliasSymbol.Target, visitor);
                _filePath = aliasSymbol.DeclaringSyntaxReferences.FirstOrDefault().SyntaxTree.FilePath;
            }

            public override SymbolKeyResolution Resolve(
                Compilation compilation, bool ignoreAssemblyKey = false, CancellationToken cancellationToken = default(CancellationToken))
            {

                var syntaxTree = compilation.SyntaxTrees.FirstOrDefault(t => t.FilePath == _filePath);
                if (syntaxTree != null)
                {
                    var target = _target.Resolve(compilation, ignoreAssemblyKey, cancellationToken).GetAnySymbol();
                    if (target != null)
                    {
                        var semanticModel = compilation.GetSemanticModel(syntaxTree);
                        var result = Resolve(semanticModel, syntaxTree.GetRoot(cancellationToken), target, cancellationToken);
                        if (result.HasValue)
                        {
                            return result.Value;
                        }
                    }
                }

                return default(SymbolKeyResolution);
            }

            private SymbolKeyResolution? Resolve(
                SemanticModel semanticModel, SyntaxNode syntaxNode, ISymbol target, 
                CancellationToken cancellationToken)
            {
                var symbol = semanticModel.GetDeclaredSymbol(syntaxNode, cancellationToken);
                if (symbol != null)
                {
                    if (symbol.Kind == SymbolKind.Alias)
                    {
                        var aliasSymbol = (IAliasSymbol)symbol;
                        if (aliasSymbol.Name == _name &&
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
                        var result = Resolve(semanticModel, child.AsNode(), target, cancellationToken);
                        if (result.HasValue)
                        {
                            return result;
                        }
                    }
                }

                return null;
            }

            internal override bool Equals(AliasSymbolKey other, ComparisonOptions options)
            {
                return _name == other._name &&
                    _target.Equals(other._target, options);
            }

            internal override int GetHashCode(ComparisonOptions options)
            {
                return Hash.Combine(
                    _target.GetHashCode(options), 
                    _name.GetHashCode());
            }
        }
    }
}
