// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal partial struct SymbolKey
    {
        private static class BodyLevelSymbolKey
        {
            public static void Create(ISymbol symbol, SymbolKeyWriter visitor)
            {
                // Store the body level symbol in two forms.  The first, a highly precise form that should find explicit
                // symbols for the case of resolving a symbol key back in the *same* solution snapshot it was created
                // from. The second, in a more query-oriented form that can allow the symbol to be found in some cases
                // even if the solution changed (which is a supported use case for SymbolKey).
                //
                // The first way just stores the location of the symbol, which we can then validate during resolution
                // maps back to the same symbol kind/name.  
                //
                // The second determines the sequence of symbols of the same kind and same name in the file and keeps
                // track of our index in that sequence.  That way, if trivial edits happen, or symbols with different
                // names/types are added/removed, we can still find what is likely to be this symbol after the edit.

                var kind = symbol.Kind;
                var localName = symbol.Name;

                Contract.ThrowIfTrue(symbol.DeclaringSyntaxReferences.IsEmpty);
                var syntaxRef = symbol.DeclaringSyntaxReferences[0].GetSyntax(visitor.CancellationToken);

                visitor.WriteString(localName);
                visitor.WriteInteger((int)kind);

                // write out the location for precision
                visitor.WriteLocation(syntaxRef.GetLocation());

                // and the ordinal for resilience
                visitor.WriteInteger(GetOrdinal());

                return;

                int GetOrdinal()
                {
                    var syntaxTree = syntaxRef.SyntaxTree;
                    var compilation = ((ISourceAssemblySymbol)symbol.ContainingAssembly).Compilation;

                    // Ensure that the tree we're looking at is actually in this compilation.  It may not be in the
                    // compilation in the case of work done with a speculative model.
                    if (Contains(compilation.SyntaxTrees, syntaxTree))
                    {
                        var semanticModel = compilation.GetSemanticModel(syntaxTree);
                        foreach (var possibleSymbol in EnumerateSymbols(semanticModel, kind, localName, visitor.CancellationToken))
                        {
                            if (possibleSymbol.symbol.Equals(symbol))
                                return possibleSymbol.ordinal;
                        }
                    }

                    return int.MaxValue;
                }
            }

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader)
            {
                var cancellationToken = reader.CancellationToken;

                var localName = reader.ReadString();
                var kind = (SymbolKind)reader.ReadInteger();
                var location = reader.ReadLocation();
                var ordinal = reader.ReadInteger();

                // First check if we can recover the symbol just through the original location.
                var semanticModel = reader.Compilation.GetSemanticModel(location.SourceTree);
                var declaredSymbol = semanticModel.GetDeclaredSymbol(location.FindNode(reader.CancellationToken));

                if (declaredSymbol?.Name == localName && declaredSymbol?.Kind == kind)
                    return new SymbolKeyResolution(declaredSymbol);

                // Couldn't recover.  See if we can still find a match across the textual drift.
                if (ordinal != int.MaxValue)
                {
                    foreach (var symbol in EnumerateSymbols(semanticModel, kind, localName, cancellationToken))
                    {
                        if (symbol.ordinal == ordinal)
                            return new SymbolKeyResolution(symbol.symbol);
                    }
                }

                return new SymbolKeyResolution();
            }

            private static IEnumerable<(ISymbol symbol, int ordinal)> EnumerateSymbols(
                SemanticModel semanticModel, SymbolKind kind, string localName, CancellationToken cancellationToken)
            {
                var ordinal = 0;
                var root = semanticModel.SyntaxTree.GetRoot(cancellationToken);

                foreach (var node in root.DescendantNodes())
                {
                    var symbol = semanticModel.GetDeclaredSymbol(node, cancellationToken);

                    if (symbol?.Kind == kind &&
                        SymbolKey.Equals(semanticModel.Compilation, symbol.Name, localName))
                    {
                        yield return (symbol, ordinal++);
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
