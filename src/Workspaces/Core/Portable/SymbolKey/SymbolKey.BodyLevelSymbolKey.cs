// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal partial struct SymbolKey
    {
        private static class BodyLevelSymbolKey
        {
            public static bool IsBodyLevelSymbol(ISymbol symbol)
                => symbol switch
                {
                    ILabelSymbol _ => true,
                    IRangeVariableSymbol _ => true,
                    ILocalSymbol _ => true,
                    IMethodSymbol { MethodKind: MethodKind.LocalFunction } _ => true,
                    _ => false,
                };

            public static ImmutableArray<Location> GetBodyLevelSourceLocations(ISymbol symbol, CancellationToken cancellationToken)
            {
                Contract.ThrowIfFalse(IsBodyLevelSymbol(symbol));
                Contract.ThrowIfTrue(symbol.DeclaringSyntaxReferences.IsEmpty && symbol.Locations.IsEmpty);

                using var _ = ArrayBuilder<Location>.GetInstance(out var result);

                foreach (var location in symbol.Locations)
                {
                    if (location.IsInSource)
                        result.Add(location);
                }

                foreach (var syntaxRef in symbol.DeclaringSyntaxReferences)
                    result.Add(syntaxRef.GetSyntax(cancellationToken).GetLocation());

                return result.ToImmutable();
            }

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

                var locations = GetBodyLevelSourceLocations(symbol, visitor.CancellationToken);

                Contract.ThrowIfFalse(locations.All(loc => loc.IsInSource));
                visitor.WriteLocationArray(locations.Distinct());

                // and the ordinal for resilience
                visitor.WriteInteger(GetOrdinal());

                return;

                int GetOrdinal()
                {
                    var syntaxTree = locations[0].SourceTree;
                    var compilation = ((ISourceAssemblySymbol)symbol.ContainingAssembly).Compilation;

                    // Ensure that the tree we're looking at is actually in this compilation.  It may not be in the
                    // compilation in the case of work done with a speculative model.
                    if (TryGetSemanticModel(compilation, syntaxTree, out var semanticModel))
                    {
                        foreach (var possibleSymbol in EnumerateSymbols(semanticModel, kind, localName, visitor.CancellationToken))
                        {
                            if (possibleSymbol.symbol.Equals(symbol))
                                return possibleSymbol.ordinal;
                        }
                    }

                    return int.MaxValue;
                }
            }

            private static bool TryGetSemanticModel(
                Compilation compilation, SyntaxTree? syntaxTree,
                [NotNullWhen(true)] out SemanticModel? semanticModel)
            {
                // Ensure that the tree we're looking at is actually in this compilation.  It may not be in the
                // compilation in the case of work done with a speculative model.
                if (syntaxTree != null && Contains(compilation.SyntaxTrees, syntaxTree))
                {
                    semanticModel = compilation.GetSemanticModel(syntaxTree);
                    return true;
                }

                semanticModel = null;
                return false;
            }

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader, out string? failureReason)
            {
                var cancellationToken = reader.CancellationToken;

                var name = reader.ReadString();
                var kind = (SymbolKind)reader.ReadInteger();
                var locations = reader.ReadLocationArray(out var locationsFailureReason);
                var ordinal = reader.ReadInteger();

                if (locationsFailureReason != null)
                {
                    failureReason = $"({nameof(BodyLevelSymbolKey)} {nameof(locations)} failed -> {locationsFailureReason})";
                    return default;
                }

                // First check if we can recover the symbol just through the original location.

                string? totalFailureReason = null;
                for (var i = 0; i < locations.Count; i++)
                {
                    var loc = locations[i];

                    if (!TryResolveLocation(loc, i, out var resolution, out var reason))
                    {
                        totalFailureReason = totalFailureReason == null
                            ? $"({reason})"
                            : $"({totalFailureReason} -> {reason})";
                        continue;
                    }

                    failureReason = null;
                    return resolution;
                }

                // Couldn't recover.  See if we can still find a match across the textual drift.
                if (ordinal != int.MaxValue &&
                    TryGetSemanticModel(reader.Compilation, locations[0].SourceTree, out var semanticModel))
                {
                    foreach (var symbol in EnumerateSymbols(semanticModel, kind, name, cancellationToken))
                    {
                        if (symbol.ordinal == ordinal)
                        {
                            failureReason = null;
                            return new SymbolKeyResolution(symbol.symbol);
                        }
                    }
                }

                failureReason = $"({nameof(BodyLevelSymbolKey)} '{name}' not found -> {totalFailureReason})";
                return default;

                bool TryResolveLocation(Location loc, int index, out SymbolKeyResolution resolution, out string? reason)
                {
                    var resolutionOpt = reader.ResolveLocation(loc);
                    if (resolutionOpt == null)
                    {
                        reason = $"location {index} failed to resolve";
                        resolution = default;
                        return false;
                    }

                    resolution = resolutionOpt.Value;
                    var symbol = resolution.GetAnySymbol();
                    if (symbol == null)
                    {
                        reason = $"location {index} did not produce any symbol";
                        return false;
                    }

                    if (symbol.Kind != kind)
                    {
                        reason = $"location {index} did not match kind: {symbol.Kind} != {kind}";
                        return false;
                    }

                    if (!SymbolKey.Equals(reader.Compilation, name, symbol.Name))
                    {
                        reason = $"location {index} did not match name: {symbol.Name} != {name}";
                        return false;
                    }

                    reason = null;
                    return true;
                }
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
