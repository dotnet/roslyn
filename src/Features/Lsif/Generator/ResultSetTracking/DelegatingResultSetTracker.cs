// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Graph;

namespace Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.ResultSetTracking
{
    internal sealed class DelegatingResultSetTracker : IResultSetTracker
    {
        private readonly Func<ISymbol, IResultSetTracker> _chooseTrackerForSymbol;

        public DelegatingResultSetTracker(Func<ISymbol, IResultSetTracker> chooseTrackerForSymbol)
        {
            _chooseTrackerForSymbol = chooseTrackerForSymbol;
        }

        public Id<T> GetResultIdForSymbol<T>(ISymbol symbol, string edgeKind, Func<IdFactory, T> vertexCreator) where T : Vertex
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
