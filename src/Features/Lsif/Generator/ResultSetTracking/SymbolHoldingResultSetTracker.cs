// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Graph;
using Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Writing;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.ResultSetTracking
{
    internal sealed class SymbolHoldingResultSetTracker : IResultSetTracker
    {
        private readonly Dictionary<ISymbol, TrackedResultSet> _symbolToResultSetId = new Dictionary<ISymbol, TrackedResultSet>();
        private readonly ReaderWriterLockSlim _readerWriterLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        private readonly ILsifJsonWriter _lsifJsonWriter;
        private readonly IdFactory _idFactory;

        /// <summary>
        /// The compilation which we are analyzing. When we make ResultSets, we attach monikers to them, and those
        /// monikers express an import/export concept for symbols being consumed from another project. We must distinguish
        /// from source symbols that come from the project being analyzed versus symbols from referenced compilations, so
        /// we can't just use <see cref="ISymbol.Locations" /> to make the determination.
        /// </summary>
        private readonly Compilation _sourceCompilation;

        public SymbolHoldingResultSetTracker(ILsifJsonWriter lsifJsonWriter, Compilation sourceCompilation, IdFactory idFactory)
        {
            _lsifJsonWriter = lsifJsonWriter;
            _sourceCompilation = sourceCompilation;
            _idFactory = idFactory;
        }

        private TrackedResultSet GetTrackedResultSet(ISymbol symbol)
        {
            TrackedResultSet? trackedResultSet;

            // First acquire a simple read lock to see if we already have a result set; we do this with
            // just a read lock to ensure we aren't contending a lot if the symbol already exists which
            // is the most common case.
            using (_readerWriterLock.DisposableRead())
            {
                if (_symbolToResultSetId.TryGetValue(symbol, out trackedResultSet))
                {
                    return trackedResultSet;
                }
            }

            using (var upgradeableReadLock = _readerWriterLock.DisposableUpgradeableRead())
            {
                // Check a second for the result set since a request could have gotten between us
                if (_symbolToResultSetId.TryGetValue(symbol, out trackedResultSet))
                {
                    return trackedResultSet;
                }

                // We still don't have it, so we have to start writing now
                upgradeableReadLock.EnterWrite();

                var resultSet = new ResultSet(_idFactory);
                _lsifJsonWriter.Write(resultSet);
                trackedResultSet = new TrackedResultSet(resultSet.GetId());
                _symbolToResultSetId.Add(symbol, trackedResultSet);
            }

            // Since we're creating a ResultSet for a symbol for the first time, let's also attach the moniker. We only generate
            // monikers for original definitions as we don't have a moniker system for those, but also because the place where
            // monikers are needed -- cross-solution find references and go to definition -- only operates on original definitions
            // anyways.
            //
            // This we do outside the lock -- whichever thread was the one to create this was the one that
            // gets to write out the moniker, but others can use the ResultSet Id at this point.
            if (SymbolMoniker.HasMoniker(symbol))
            {
                _ = this.GetMoniker(symbol, _sourceCompilation);
            }

            return trackedResultSet;
        }

        public Id<ResultSet> GetResultSetIdForSymbol(ISymbol symbol)
        {
            return GetTrackedResultSet(symbol).Id;
        }

        public Id<T> GetResultIdForSymbol<T>(ISymbol symbol, string edgeKind, Func<IdFactory, T> vertexCreator) where T : Vertex
        {
            return GetTrackedResultSet(symbol).GetResultId(edgeKind, vertexCreator, _lsifJsonWriter, _idFactory);
        }

        public bool ResultSetNeedsInformationalEdgeAdded(ISymbol symbol, string edgeKind)
        {
            return GetTrackedResultSet(symbol).ResultSetNeedsInformationalEdgeAdded(edgeKind);
        }

        private class TrackedResultSet
        {
            public Id<ResultSet> Id { get; }

            /// <summary>
            /// A map which holds the per-symbol results that are linked from the resultSet. The value will be null if the entry was
            /// added via <see cref="ResultSetNeedsInformationalEdgeAdded"/>. Concurrent acecss is guarded with a monitor lock
            /// on this field itself, with the belief that concurrent access for a single symbol is relatively rare.
            /// </summary>
            /// <remarks>
            /// This class assumes that we more or less have two kinds of edges in the LSIF world:
            /// 
            /// 1. the resultSet might point to a node that doesn't really have any data, but simply points to other data like referenceResults.
            ///    In this case, it's important for clients to get to that Id.
            /// 2. the resultSet points to a node that itself has data, but nobody needs to know the ID, like a hover result. In this case, those results
            ///    are often expensive to compute, but we do want to record that somebody is adding them somewhere.
            /// 
            /// We record the first kind of this in this dictionary with a non-null Id, and the second kind with a null ID. We could conceptually store
            /// two dictionaries for this, but that will add memory pressure and also limit the catching of mistakes if people cross these two APIs.
            /// </remarks>
            private readonly Dictionary<string, Id<Vertex>?> _edgeKindToVertexId = new Dictionary<string, Id<Vertex>?>();

            public TrackedResultSet(Id<ResultSet> id)
            {
                Id = id;
            }

            public Id<T> GetResultId<T>(string edgeLabel, Func<IdFactory, T> vertexCreator, ILsifJsonWriter lsifJsonWriter, IdFactory idFactory) where T : Vertex
            {
                lock (_edgeKindToVertexId)
                {
                    if (_edgeKindToVertexId.TryGetValue(edgeLabel, out var existingId))
                    {
                        if (!existingId.HasValue)
                        {
                            throw new Exception($"This ResultSet already has an edge of {edgeLabel} as {nameof(ResultSetNeedsInformationalEdgeAdded)} was called with this edge label.");
                        }

                        // TODO: this is a violation of the type system here, really: we're assuming that all calls to this function with the same edge kind
                        // will have the same type parameter. If that's violated, the Id returned here isn't really the right type.
                        return new Id<T>(existingId.Value.NumericId);
                    }

                    var vertex = vertexCreator(idFactory);
                    _edgeKindToVertexId.Add(edgeLabel, vertex.GetId().As<T, Vertex>());

                    lsifJsonWriter.Write(vertex);
                    lsifJsonWriter.Write(Edge.Create(edgeLabel, Id, vertex.GetId(), idFactory));

                    return vertex.GetId();
                }
            }

            public bool ResultSetNeedsInformationalEdgeAdded(string edgeKind)
            {
                lock (_edgeKindToVertexId)
                {
                    if (_edgeKindToVertexId.TryGetValue(edgeKind, out var existingId))
                    {
                        if (existingId.HasValue)
                        {
                            throw new InvalidOperationException($"This edge kind was already called with a call to {nameof(GetResultId)} which would imply we are mixing edge types incorrectly.");
                        }

                        return false;
                    }

                    _edgeKindToVertexId.Add(edgeKind, null);
                    return true;
                }
            }
        }
    }
}
