// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.LanguageServiceIndexFormat;
using Microsoft.CodeAnalysis.Lsif.Generator.LsifGraph;
using Microsoft.CodeAnalysis.Lsif.Generator.Writing;

namespace Microsoft.CodeAnalysis.Lsif.Generator.ResultSetTracking
{
    internal sealed class SymbolHoldingResultSetTracker : IResultSetTracker
    {
        private readonly Dictionary<ISymbol, TrackedResultSet> _symbolToResultSetId = new Dictionary<ISymbol, TrackedResultSet>();
        private readonly ILsifJsonWriter _lsifJsonWriter;

        /// <summary>
        /// The compilation which we are analyzing. When we make ResultSets, we attach monikers to them, and those
        /// monikers express an import/export concept for symbols being consumed from another project. We must distinguish
        /// from source symbols that come from the project being analyzed versus symbols from referenced compilations, so
        /// we can't just use <see cref="ISymbol.Locations" /> to make the determination.
        /// </summary>
        private readonly Compilation _sourceCompilation;

        public SymbolHoldingResultSetTracker(ILsifJsonWriter lsifJsonWriter, Compilation sourceCompilation)
        {
            _lsifJsonWriter = lsifJsonWriter;
            _sourceCompilation = sourceCompilation;
        }

        private TrackedResultSet GetTrackedResultSet(ISymbol symbol)
        {
            if (!_symbolToResultSetId.TryGetValue(symbol, out var trackedResultSet))
            {
                var resultSet = new ResultSet();
                _lsifJsonWriter.Write(resultSet);
                trackedResultSet = new TrackedResultSet(resultSet.GetId());
                _symbolToResultSetId.Add(symbol, trackedResultSet);

                // Since we're creating a ResultSet for a symbol for the first time, let's also attach the moniker. We only generate
                // monikers for original definitions as we don't have a moniker system for those, but also because the place where
                // monikers are needed -- cross-solution find references and go to definition -- only operates on original definitions
                // anyways.
                if (symbol.OriginalDefinition.Equals(symbol))
                {
                    var monikerVertex = TryCreateMonikerVertexForSymbol(symbol);

                    if (monikerVertex != null)
                    {
                        _lsifJsonWriter.Write(monikerVertex);
                        _lsifJsonWriter.Write(Edge.Create("moniker", trackedResultSet.Id, monikerVertex.GetId()));
                    }
                }
            }

            return trackedResultSet;
        }

        private Moniker? TryCreateMonikerVertexForSymbol(ISymbol symbol)
        {
            var moniker = SymbolMoniker.TryCreate(symbol);

            if (moniker == null)
            {
                return null;
            }

            string? kind;

            if (symbol.Kind == SymbolKind.Namespace)
            {
                kind = null;
            }
            else if (symbol.ContainingAssembly.Equals(_sourceCompilation.Assembly))
            {
                kind = "export";
            }
            else
            {
                kind = "import";
            }

            return new Moniker(moniker.Scheme, moniker.Identifier, kind);
        }

        public Id<ResultSet> GetResultSetIdForSymbol(ISymbol symbol)
        {
            return GetTrackedResultSet(symbol).Id;
        }

        public Id<T> GetResultIdForSymbol<T>(ISymbol symbol, string edgeKind, Func<T> vertexCreator) where T : Vertex
        {
            return GetTrackedResultSet(symbol).GetResultId(edgeKind, vertexCreator, _lsifJsonWriter);
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
            /// added via <see cref="ResultSetNeedsInformationalEdgeAdded"/>.
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

            public Id<T> GetResultId<T>(string edgeKind, Func<T> vertexCreator, ILsifJsonWriter lsifJsonWriter) where T : Vertex
            {
                if (_edgeKindToVertexId.TryGetValue(edgeKind, out var existingId))
                {
                    if (!existingId.HasValue)
                    {
                        throw new Exception($"This ResultSet already has an edge of {edgeKind} as {nameof(ResultSetNeedsInformationalEdgeAdded)} was called with this edge kind.");
                    }

                    // TODO: this is a violation of the type system here, really: we're assuming that all calls to this function with the same edge kind
                    // will have the same type parameter. If that's violated, the Id returned here isn't really the right type.
                    return new Id<T>(existingId.Value.NumericId);
                }

                T vertex = vertexCreator();
                _edgeKindToVertexId.Add(edgeKind, vertex.GetId().As<T, Vertex>());

                lsifJsonWriter.Write(vertex);
                lsifJsonWriter.Write(Edge.Create(edgeKind, Id, vertex.GetId()));

                return vertex.GetId();
            }

            public bool ResultSetNeedsInformationalEdgeAdded(string edgeKind)
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
