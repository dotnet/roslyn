// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal partial struct SymbolKey
    {
        private static class BodyLevelSymbolKey
        {
            public static void Create(ISymbol symbol, SymbolKeyWriter visitor)
            {
                var containingSymbol = symbol.ContainingSymbol;

                while (containingSymbol.DeclaringSyntaxReferences.IsDefaultOrEmpty)
                {
                    containingSymbol = containingSymbol.ContainingSymbol;
                }

                var compilation = ((ISourceAssemblySymbol)symbol.ContainingAssembly).Compilation;
                var kind = symbol.Kind;
                var localName = symbol.Name;
                var ordinal = 0;
                foreach (var possibleSymbol in EnumerateSymbols(compilation, containingSymbol, kind, localName, visitor.CancellationToken))
                {
                    if (possibleSymbol.symbol.Equals(symbol))
                    {
                        ordinal = possibleSymbol.ordinal;
                        break;
                    }
                }

                visitor.WriteString(localName);
                visitor.WriteSymbolKey(containingSymbol);
                visitor.WriteInteger(ordinal);
                visitor.WriteInteger((int)kind);
            }

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader)
            {
                var localName = reader.ReadString();
                var containingSymbolResolution = reader.ReadSymbolKey();
                var ordinal = reader.ReadInteger();
                var kind = (SymbolKind)reader.ReadInteger();

                var containingSymbol = containingSymbolResolution.Symbol;
                if (containingSymbol != null)
                {
                    foreach (var symbol in EnumerateSymbols(
                        reader.Compilation, containingSymbol, kind, localName, reader.CancellationToken))
                    {
                        if (symbol.ordinal == ordinal)
                        {
                            return new SymbolKeyResolution(symbol.symbol);
                        }
                    }
                }

                return new SymbolKeyResolution();
            }

            private static IEnumerable<(ISymbol symbol, int ordinal)> EnumerateSymbols(
                Compilation compilation, ISymbol containingSymbol,
                SymbolKind kind, string localName,
                CancellationToken cancellationToken)
            {
                var ordinal = 0;

                foreach (var declaringLocation in containingSymbol.DeclaringSyntaxReferences)
                {
                    // This operation can potentially fail. If containingSymbol came from 
                    // a SpeculativeSemanticModel, containingSymbol.ContainingAssembly.Compilation
                    // may not have been rebuilt to reflect the trees used by the 
                    // SpeculativeSemanticModel to produce containingSymbol. In that case,
                    // asking the ContainingAssembly's compilation for a SemanticModel based
                    // on trees for containingSymbol with throw an ArgumentException.
                    // Unfortunately, the best way to avoid this (currently) is to see if
                    // we're asking for a model for a tree that's part of the compilation.
                    // (There's no way to get back to a SemanticModel from a symbol).

                    // TODO (rchande): It might be better to call compilation.GetSemanticModel
                    // and catch the ArgumentException. The compilation internally has a 
                    // Dictionary<SyntaxTree, ...> that it uses to check if the SyntaxTree
                    // is applicable wheras the public interface requires us to enumerate
                    // the entire IEnumerable of trees in the Compilation.
                    if (!Contains(compilation.SyntaxTrees, declaringLocation.SyntaxTree))
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

                        if (symbol is { Kind: kind } && SymbolKey.Equals(compilation, symbol.Name, localName))
                        {
                            yield return (symbol, ordinal++);
                        }
                    }
                }
            }

            private static bool Contains(IEnumerable<SyntaxTree> trees, SyntaxTree tree)
            {
                foreach (var current in trees)
                {
                    if (current == tree)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
