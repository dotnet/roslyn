// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
    internal sealed class CodeLensFindReferencesProgress : IFindReferencesProgress
    {
        private readonly object _gate = new object();

        /// <summary>
        /// this token is linked to an aggregate token that is passed to the Find References
        /// operation, this is used solely to trigger the cancellation of an in progress
        /// find references operation, when the references count hits a given cap.
        /// </summary>
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly SyntaxNode _queriedNode;
        private readonly ISymbol _queriedSymbol;
        private readonly ConcurrentSet<ISymbol> _foundDefinitions = new ConcurrentSet<ISymbol>();

        /// <remarks>
        /// _referencesCount is read and written from multiple threads.
        /// so all read and write should be through a lock around _gate.
        /// </remarks>
        private int _referencesCount;

        /// <remarks>
        /// If the cap is 0, then there is no cap.
        /// </remarks>
        public int SearchCap { get; }

        public CodeLensFindReferencesProgress(
            ISymbol queriedDefinition,
            SyntaxNode queriedNode,
            CancellationTokenSource cancellationTokenSource,
            int searchCap)
        {
            _queriedSymbol = queriedDefinition;
            _queriedNode = queriedNode;
            _cancellationTokenSource = cancellationTokenSource;

            SearchCap = searchCap;
        }

        public bool SearchCapReached
        {
            get
            {
                if (SearchCap == 0)
                {
                    return false;
                }

                lock (_gate)
                {
                    return _referencesCount > SearchCap;
                }
            }
        }

        public int ReferencesCount
        {
            get
            {
                lock (_gate)
                {
                    return _referencesCount;
                }
            }
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
            // Returns nodes from source not equal to actual location
            IEnumerable<SyntaxNode> syntaxNodes = from syntaxReference in symbol.DeclaringSyntaxReferences
                                                  let candidateSyntaxNode = syntaxReference.GetSyntax(cancellationToken)
                                                  where !(syntaxNode.Span == candidateSyntaxNode.Span &&
                                                          syntaxNode.SyntaxTree.FilePath.Equals(candidateSyntaxNode.SyntaxTree.FilePath, StringComparison.OrdinalIgnoreCase))
                                                  select candidateSyntaxNode;

            // This matches the definition locations to syntax references, ignores metadata locations
            IEnumerable<Location> partialLocations = from currentSyntaxNode in syntaxNodes
                                                     from sourceLocation in symbol.Locations
                                                     where !sourceLocation.IsInMetadata
                                                           && currentSyntaxNode.SyntaxTree.Equals(sourceLocation.SourceTree)
                                                           && currentSyntaxNode.Span.Contains(sourceLocation.SourceSpan)
                                                     select sourceLocation;

            return partialLocations;
        }

        /// <remarks>
        /// This method will not be called concurrently with any other progress method.
        /// Hence, locks are not necessary.
        /// </remarks>
        public void OnCompleted()
        {
            // If we didn't hit the cap, we are in the most common case of *not widely cascaded* items.
            // Essentially, the search count we display should be accurate, so,
            // we have to add the count of definitions we've encountered plus account for cases like partial definitions.
            if (SearchCapReached || !_foundDefinitions.Any())
            {
                return;
            }

            // Add all definitions, except the one that we queried on.
            // Note: there can be more than 1 queried definition in case of linked files.
            var queriedDefinitions = _foundDefinitions.Where(IsQueriedDefinition);
            var definitions = _foundDefinitions.Except(queriedDefinitions);
            var definitionLocations = definitions.Select(def => def.Locations);
            _referencesCount += definitionLocations.Count();

            // Add all partial declaration locations.
            foreach (var queriedDefinition in queriedDefinitions)
            {
                var additionalSyntaxLocations = GetPartialLocations(queriedDefinition, _queriedNode, _cancellationTokenSource.Token);
                _referencesCount += additionalSyntaxLocations.Count();
            }
        }

        private bool IsQueriedDefinition(ISymbol symbol)
        {
            return symbol.Locations.Intersect(
                   _queriedSymbol.Locations, LocationComparer.Instance).Any();
        }

        public void OnDefinitionFound(ISymbol symbol)
        {
            if (FilteringHelpers.FilterDeclaration(symbol))
            {
                _foundDefinitions.Add(symbol);
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
            if (!FilteringHelpers.FilterReference(_queriedSymbol, symbol, location))
            {
                return;
            }

            lock (_gate)
            {
                _referencesCount++;

                if (SearchCapReached)
                {
                    _cancellationTokenSource.Cancel();
                }
            }
        }

        public void OnStarted()
        {
        }

        public void ReportProgress(int current, int maximum)
        {
        }
    }
}
