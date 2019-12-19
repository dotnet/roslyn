// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Lsif.Generator.LsifGraph;

namespace Microsoft.CodeAnalysis.Lsif.Generator.ResultSetTracking
{
    internal sealed class DelegatingResultSetTracker : IResultSetTracker
    {
        private readonly Func<ISymbol, IResultSetTracker> _chooseTrackerForSymbol;

        public DelegatingResultSetTracker(Func<ISymbol, IResultSetTracker> chooseTrackerForSymbol)
        {
            _chooseTrackerForSymbol = chooseTrackerForSymbol;
        }

        public Id<T> GetResultIdForSymbol<T>(ISymbol symbol, string edgeKind, Func<T> vertexCreator) where T : Vertex
        {
            return _chooseTrackerForSymbol(symbol).GetResultIdForSymbol(symbol, edgeKind, vertexCreator);
        }

        public Id<ResultSet> GetResultSetIdForSymbol(ISymbol symbol)
        {
            return _chooseTrackerForSymbol(symbol).GetResultSetIdForSymbol(symbol);
        }

        public bool ResultSetNeedsInformationalEdgeAdded(ISymbol symbol, string edgeKind)
        {
            return _chooseTrackerForSymbol(symbol).ResultSetNeedsInformationalEdgeAdded(symbol, edgeKind);
        }
    }
}
