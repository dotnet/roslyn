// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Lsif.Generator.LsifGraph;
using Microsoft.CodeAnalysis.Lsif.Generator.Writing;

namespace Microsoft.CodeAnalysis.Lsif.Generator.ResultSetTracking
{
    internal sealed class DeferredFlushResultSetTracker : IResultSetTracker
    {
        private readonly Dictionary<ISymbol, TrackedResultSet> _symbolToResultSetId = new Dictionary<ISymbol, TrackedResultSet>();
        private readonly List<Element> _elementsNeedingFlush = new List<Element>();

        public Id<ResultSet> GetResultSetIdForSymbol(ISymbol symbol)
        {
            if (_symbolToResultSetId.TryGetValue(symbol, out var trackedResultSet))
            {
                return trackedResultSet.Id;
            }

            var resultSet = new ResultSet();
            _elementsNeedingFlush.Add(resultSet);
            _symbolToResultSetId.Add(symbol, new TrackedResultSet(resultSet.GetId()));

            return resultSet.GetId();
        }

        public void Flush(ILsifJsonWriter lsifJsonWriter)
        {
            foreach (var elementNeedingFlush in _elementsNeedingFlush)
            {
                lsifJsonWriter.Write(elementNeedingFlush);
            }

            _elementsNeedingFlush.Clear();
        }

        private class TrackedResultSet
        {
            public Id<ResultSet> Id { get; }

            public TrackedResultSet(Id<ResultSet> id)
            {
                Id = id;
            }
        }
    }
}
