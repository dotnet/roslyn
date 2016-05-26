// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal partial class SymbolKey
    {
        [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
        private class NonDeclarationSymbolKey : AbstractSymbolKey<NonDeclarationSymbolKey>
        {
            [JsonProperty] private readonly SymbolKey _containingKey;
            [JsonProperty] private readonly string _localName;
            [JsonProperty] private readonly int _ordinal;
            [JsonProperty] private readonly SymbolKind _kind;

            [JsonConstructor]
            internal NonDeclarationSymbolKey(
                SymbolKey _containingKey, string _localName, int _ordinal, SymbolKind _kind)
            {
                this._containingKey = _containingKey;
                this._localName = _localName;
                this._ordinal = _ordinal;
                this._kind = _kind;
            }

            internal NonDeclarationSymbolKey(ISymbol symbol, Visitor visitor)
            {
                _kind = symbol.Kind;
                var containingSymbol = symbol.ContainingSymbol;

                while (!containingSymbol.DeclaringSyntaxReferences.Any())
                {
                    containingSymbol = containingSymbol.ContainingSymbol;
                }

                _containingKey = GetOrCreate(containingSymbol, visitor);
                _localName = symbol.Name;

                Contract.ThrowIfNull(
                    visitor.Compilation,
                    message: $"visitor cannot be created with a null compilation and visit a {nameof(NonDeclarationSymbolKey)}.");
                foreach (var possibleSymbol in EnumerateSymbols(visitor.Compilation, containingSymbol, visitor.CancellationToken))
                {
                    if (possibleSymbol.Item1.Equals(symbol))
                    {
                        _ordinal = possibleSymbol.Item2;
                        break;
                    }
                }
            }

            public override SymbolKeyResolution Resolve(Compilation compilation, bool ignoreAssemblyKey = false, CancellationToken cancellationToken = default(CancellationToken))
            {
                var containingSymbol = _containingKey.Resolve(compilation, ignoreAssemblyKey, cancellationToken).Symbol;

                if (containingSymbol == null)
                {
                    return new SymbolKeyResolution();
                }

                foreach (var symbol in EnumerateSymbols(compilation, containingSymbol, cancellationToken))
                {
                    if (symbol.Item2 == _ordinal)
                    {
                        return new SymbolKeyResolution(symbol.Item1);
                    }
                }

                return new SymbolKeyResolution();
            }

            private IEnumerable<ValueTuple<ISymbol, int>> EnumerateSymbols(
                Compilation compilation, ISymbol containingSymbol,
                CancellationToken cancellationToken)
            {
                int ordinal = 0;

                foreach (var declaringLocation in containingSymbol.DeclaringSyntaxReferences)
                {
                    // This operation can potentially fail. If containingSymbol came from 
                    // a SpeculativeSemanticModel, containingSymbol.ContainingAssembly.Compilation
                    // may not have been rebuilt to reflect the trees used by the 
                    // SpeculativeSemanticModel to produce containingSymbol. In that case,
                    // asking the ContainingAssembly's complation for a SemanticModel based
                    // on trees for containingSymbol with throw an ArgumentException.
                    // Unfortunately, the best way to avoid this (currently) is to see if
                    // we're asking for a model for a tree that's part of the compilation.
                    // (There's no way to get back to a SemanticModel from a symbol).

                    // TODO (rchande): It might be better to call compilation.GetSemanticModel
                    // and catch the ArgumentException. The compilation internally has a 
                    // Dictionary<SyntaxTree, ...> that it uses to check if the SyntaxTree
                    // is applicable wheras the public interface requires us to enumerate
                    // the entire IEnumerable of trees in the Compilation.
                    if (!compilation.SyntaxTrees.Contains(declaringLocation.SyntaxTree))
                    {
                        continue;
                    }

                    var node = declaringLocation.GetSyntax(cancellationToken);
                    if (node.Language == LanguageNames.VisualBasic)
                    {
                        node = node.Parent;
                    }

                    var semanticModel = compilation.GetSemanticModel(node.SyntaxTree);

                    foreach (var token in node.DescendantNodes())
                    {
                        var symbol = semanticModel.GetDeclaredSymbol(token, cancellationToken);

                        if (symbol != null &&
                            symbol.Kind == _kind &&
                            Equals(compilation.IsCaseSensitive, symbol.Name, _localName))
                        {
                            yield return ValueTuple.Create(symbol, ordinal++);
                        }
                    }
                }
            }

            internal override bool Equals(NonDeclarationSymbolKey other, ComparisonOptions options)
            {
                throw new NotImplementedException();
            }

            internal override int GetHashCode(ComparisonOptions options)
            {
                throw new NotImplementedException();
            }
        }
    }
}