// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

                visitor.WriteString(localName);
                visitor.WriteInteger((int)kind);

                // write out the locations for precision
                Contract.ThrowIfTrue(symbol.DeclaringSyntaxReferences.IsEmpty && symbol.Locations.IsEmpty);

                var locations = symbol.Locations.Concat(
                    symbol.DeclaringSyntaxReferences.SelectAsArray(r => r.GetSyntax(visitor.CancellationToken).GetLocation()));

                visitor.WriteLocationArray(locations);

                // and the ordinal for resilience
                visitor.WriteInteger(GetOrdinal());

                return;

                int GetOrdinal()
                {
                    var syntaxTree = locations[0].SourceTree;
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

                var name = reader.ReadString();
                var kind = (SymbolKind)reader.ReadInteger();
                var locations = reader.ReadLocationArray();
                var ordinal = reader.ReadInteger();

                // First check if we can recover the symbol just through the original location.
                foreach (var loc in locations)
                {
                    var resolutionOpt = reader.ResolveLocation(loc);
                    if (resolutionOpt.HasValue)
                    {
                        var resolution = resolutionOpt.Value;
                        var symbol = resolution.GetAnySymbol();
                        if (symbol?.Kind == kind &&
                            SymbolKey.Equals(reader.Compilation, name, symbol.Name))
                        {
                            return resolution;
                        }
                    }
                }

                // Couldn't recover.  See if we can still find a match across the textual drift.
                if (ordinal != int.MaxValue)
                {
                    var semanticModel = reader.Compilation.GetSemanticModel(locations[0].SourceTree);
                    foreach (var symbol in EnumerateSymbols(semanticModel, kind, name, cancellationToken))
                    {
                        if (symbol.ordinal == ordinal)
                            return new SymbolKeyResolution(symbol.symbol);
                    }
                }

                return default;
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
