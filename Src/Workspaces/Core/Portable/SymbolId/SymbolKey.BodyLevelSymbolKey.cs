// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal abstract partial class SymbolKey
    {
        private class NonDeclarationSymbolKey<TSymbol> : AbstractSymbolKey<NonDeclarationSymbolKey<TSymbol>> where TSymbol : class, ISymbol
        {
            private readonly SymbolKey containingKey;
            private readonly string localName;
            private readonly int ordinal;

            internal NonDeclarationSymbolKey(TSymbol symbol, Visitor visitor)
            {
                var containingSymbol = symbol.ContainingSymbol;

                while (!containingSymbol.DeclaringSyntaxReferences.Any())
                {
                    containingSymbol = containingSymbol.ContainingSymbol;
                }

                this.containingKey = GetOrCreate(containingSymbol, visitor);
                this.localName = symbol.Name;

                foreach (var possibleSymbol in EnumerateSymbols(visitor.Compilation, containingSymbol, localName, visitor.CancellationToken))
                {
                    if (possibleSymbol.Item1.Equals(symbol))
                    {
                        this.ordinal = possibleSymbol.Item2;
                        break;
                    }
                }
            }

            public override SymbolKeyResolution Resolve(Compilation compilation, bool ignoreAssemblyKey = false, CancellationToken cancellationToken = default(CancellationToken))
            {
                var containingSymbol = containingKey.Resolve(compilation, ignoreAssemblyKey, cancellationToken).Symbol;

                if (containingSymbol == null)
                {
                    return new SymbolKeyResolution();
                }

                foreach (var symbol in EnumerateSymbols(compilation, containingSymbol, localName, cancellationToken))
                {
                    if (symbol.Item2 == ordinal)
                    {
                        return new SymbolKeyResolution(symbol.Item1);
                    }
                }

                return new SymbolKeyResolution();
            }

            private static IEnumerable<ValueTuple<TSymbol, int>> EnumerateSymbols(Compilation compilation, ISymbol containingSymbol, string name, CancellationToken cancellationToken)
            {
                int ordinal = 0;

                foreach (var declaringLocation in containingSymbol.DeclaringSyntaxReferences)
                {
                    var node = declaringLocation.GetSyntax(cancellationToken);
                    if (node.Language == LanguageNames.VisualBasic)
                    {
                        node = node.Parent;
                    }

                    var semanticModel = compilation.GetSemanticModel(node.SyntaxTree);

                    foreach (var token in node.DescendantNodes())
                    {
                        var symbol = semanticModel.GetDeclaredSymbol(token) as TSymbol;

                        if (symbol != null && Equals(compilation.IsCaseSensitive, symbol.Name, name))
                        {
                            yield return ValueTuple.Create(symbol, ordinal++);
                        }
                    }
                }
            }

            internal override bool Equals(NonDeclarationSymbolKey<TSymbol> other, ComparisonOptions options)
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
