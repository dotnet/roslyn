// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.FindSymbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeLens
{
    /// <summary>
    /// Tracks incremental progress of a find references search, we use this to
    /// count the number of references up until a certain cap is reached and cancel the search
    /// or until the search completes, if such a cap is not reached.
    /// </summary>
    /// <remarks>
    /// All public methods of this type could be called from multiple threads.
    /// </remarks>
    internal sealed class CodeLensFindReferencesProgress : IFindReferencesProgress, IDisposable
    {
        /// <summary>
        /// this token is linked to an aggregate token that is passed to the Find References
        /// operation, this is used solely to trigger the cancellation of an in progress
        /// find references operation, when the references count hits a given cap.
        /// </summary>
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly CancellationTokenSource _aggregateCancellationTokenSource;
        private readonly SyntaxNode _queriedNode;
        private readonly ISymbol _queriedSymbol;
        private readonly ConcurrentSet<Location> _locations;

        /// <remarks>
        /// If the cap is 0, then there is no cap.
        /// </remarks>
        public int SearchCap { get; }

        /// <summary>
        /// The cancellation token that aggregates the original cancellation token + this progress
        /// </summary>
        public CancellationToken CancellationToken => _aggregateCancellationTokenSource.Token;

        public bool SearchCapReached => SearchCap != 0 && ReferencesCount > SearchCap;

        public int ReferencesCount => _locations.Count;

        public ImmutableArray<Location> Locations => _locations.ToImmutableArray();

        public CodeLensFindReferencesProgress(
            ISymbol queriedDefinition,
            SyntaxNode queriedNode,
            int searchCap,
            CancellationToken cancellationToken)
        {
            _queriedSymbol = queriedDefinition;
            _queriedNode = queriedNode;
            _cancellationTokenSource = new CancellationTokenSource();
            _aggregateCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                _cancellationTokenSource.Token, cancellationToken);
            _locations = new ConcurrentSet<Location>();

            SearchCap = searchCap;
        }

        public void OnCompleted()
        {
        }

        /// <summary>
        /// Returns partial symbol locations whose node does not match the given syntaxNode
        /// </summary>
        /// <param name="symbol">Symbol whose locations are queried</param>
        /// <param name="syntaxNode">Syntax node to compare against to exclude location - actual location being queried</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Partial locations</returns>
        private static IEnumerable<Location> GetPartialLocations(ISymbol symbol, SyntaxNode syntaxNode, CancellationToken cancellationToken)
        {
            // Returns nodes from source not equal to actual location and matches the definition locations to syntax references, ignores metadata locations
            return from syntaxReference in symbol.DeclaringSyntaxReferences
                   let candidateSyntaxNode = syntaxReference.GetSyntax(cancellationToken)
                   where !(syntaxNode.Span == candidateSyntaxNode.Span &&
                           syntaxNode.SyntaxTree.FilePath.Equals(candidateSyntaxNode.SyntaxTree.FilePath, StringComparison.OrdinalIgnoreCase))
                   from sourceLocation in symbol.Locations
                   where !sourceLocation.IsInMetadata
                         && candidateSyntaxNode.SyntaxTree.Equals(sourceLocation.SourceTree)
                         && candidateSyntaxNode.Span.Contains(sourceLocation.SourceSpan)
                   select sourceLocation;
        }

        /// <summary>
        /// Exclude the following kind of symbols:
        ///  1. Implicitly declared symbols (such as implicit fields backing properties)
        ///  2. Symbols that can't be referenced by name (such as property getters and setters).
        ///  3. Metadata only symbols, i.e. symbols with no location in source.
        /// </summary>
        private static bool FilterReference(ISymbol queriedSymbol, ISymbol definition, ReferenceLocation reference)
        {
            return definition.IsImplicitlyDeclared ||
                   (definition as IMethodSymbol)?.AssociatedSymbol != null ||
                   // FindRefs treats a constructor invocation as a reference to the constructor symbol and to the named type symbol that defines it.
                   // While we need to count the cascaded symbol definition from the named type to its constructor, we should not double count the
                   // reference location for the invocation while computing references count for the named type symbol. 
                   (queriedSymbol.Kind == SymbolKind.NamedType && (definition as IMethodSymbol)?.MethodKind == MethodKind.Constructor) ||
                   (!definition.Locations.Any(loc => loc.IsInSource) && !reference.Location.IsInSource);
        }

        private static bool FilterDefinition(ISymbol definition)
        {
            return definition.IsImplicitlyDeclared ||
                   (definition as IMethodSymbol)?.AssociatedSymbol != null ||
                   !definition.Locations.Any(loc => loc.IsInSource);
        }

        public void OnDefinitionFound(ISymbol symbol)
        {
            if (FilterDefinition(symbol))
            {
                return;
            }

            // Partial types can have more than one declaring syntax references.
            // Add remote locations for all the syntax references except the queried syntax node.
            // To query for the partial locations, filter definition locations that occur in source whose span is part of
            // span of any syntax node from Definition.DeclaringSyntaxReferences except for the queried syntax node.
            _locations.AddRange(symbol.Locations.Intersect(_queriedSymbol.Locations, LocationComparer.Instance).Any()
                ? GetPartialLocations(symbol, _queriedNode, _cancellationTokenSource.Token)
                : symbol.Locations);

            if (SearchCapReached)
            {
                _cancellationTokenSource.Cancel();
            }
        }

        public void OnFindInDocumentCompleted(Document document)
        {
        }

        public void OnFindInDocumentStarted(Document document)
        {
        }

        public void OnReferenceFound(ISymbol symbol, ReferenceLocation location)
        {
            if (FilterReference(_queriedSymbol, symbol, location))
            {
                return;
            }

            _locations.Add(location.Location);

            if (SearchCapReached)
            {
                _cancellationTokenSource.Cancel();
            }
        }

        public void OnStarted()
        {
        }

        public void ReportProgress(int current, int maximum)
        {
        }

        public void Dispose()
        {
            _aggregateCancellationTokenSource.Dispose();
            _cancellationTokenSource.Dispose();
        }
    }
}
