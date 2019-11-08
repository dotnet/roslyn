// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Shared.Extensions;
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
            _aggregateCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _locations = new ConcurrentSet<Location>(LocationComparer.Instance);

            SearchCap = searchCap;
        }

        public void OnStarted()
        {
        }

        public void OnCompleted()
        {
        }

        public void OnFindInDocumentStarted(Document document)
        {
        }

        public void OnFindInDocumentCompleted(Document document)
        {
        }

        private static bool FilterDefinition(ISymbol definition)
        {
            return definition.IsImplicitlyDeclared ||
                   (definition as IMethodSymbol)?.AssociatedSymbol != null;
        }

        // Returns partial symbol locations whose node does not match the queried syntaxNode
        private IEnumerable<Location> GetPartialLocations(ISymbol symbol, CancellationToken cancellationToken)
        {
            // Returns nodes from source not equal to actual location
            return from syntaxReference in symbol.DeclaringSyntaxReferences
                   let candidateSyntaxNode = syntaxReference.GetSyntax(cancellationToken)
                   where !(_queriedNode.Span == candidateSyntaxNode.Span &&
                           _queriedNode.SyntaxTree.FilePath.Equals(candidateSyntaxNode.SyntaxTree.FilePath,
                               StringComparison.OrdinalIgnoreCase))
                   select candidateSyntaxNode.GetLocation();
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
            var locations = symbol.Locations.Intersect(_queriedSymbol.Locations, LocationComparer.Instance).Any()
                ? GetPartialLocations(symbol, _aggregateCancellationTokenSource.Token)
                : symbol.Locations;

            _locations.AddRange(locations.Where(location => location.IsInSource));

            if (SearchCapReached)
            {
                _aggregateCancellationTokenSource.Cancel();
            }
        }

        /// <summary>
        /// Exclude the following kind of symbols:
        ///  1. Implicitly declared symbols (such as implicit fields backing properties)
        ///  2. Symbols that can't be referenced by name (such as property getters and setters).
        ///  3. Metadata only symbols, i.e. symbols with no location in source.
        /// </summary>
        private bool FilterReference(ISymbol definition, ReferenceLocation reference)
        {
            var isImplicitlyDeclared = definition.IsImplicitlyDeclared || definition.IsAccessor();
            // FindRefs treats a constructor invocation as a reference to the constructor symbol and to the named type symbol that defines it.
            // While we need to count the cascaded symbol definition from the named type to its constructor, we should not double count the
            // reference location for the invocation while computing references count for the named type symbol. 
            var isImplicitReference = _queriedSymbol is { Kind: SymbolKind.NamedType } && definition is IMethodSymbol { MethodKind: MethodKind.Constructor };
            return isImplicitlyDeclared ||
                   isImplicitReference ||
                   !reference.Location.IsInSource ||
                   !definition.Locations.Any(loc => loc.IsInSource);
        }

        public void OnReferenceFound(ISymbol symbol, ReferenceLocation location)
        {
            if (FilterReference(symbol, location))
            {
                return;
            }

            _locations.Add(location.Location);

            if (SearchCapReached)
            {
                _aggregateCancellationTokenSource.Cancel();
            }
        }

        public void ReportProgress(int current, int maximum)
        {
        }

        public void Dispose()
        {
            _aggregateCancellationTokenSource.Dispose();
        }
    }
}
