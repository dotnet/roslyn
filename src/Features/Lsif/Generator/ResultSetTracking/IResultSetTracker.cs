// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Graph;

namespace Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.ResultSetTracking
{
    /// <summary>
    /// An object that tracks a mapping from symbols to the result sets that have information about those symbols.
    /// </summary>
    internal interface IResultSetTracker
    {
        /// <summary>
        /// Returns the ID of the <see cref="ResultSet"/> that represents a symbol.
        /// </summary>
        Id<ResultSet> GetResultSetIdForSymbol(ISymbol symbol);

        /// <summary>
        /// Returns an ID of a vertex that is linked from a result set. For example, a <see cref="ResultSet"/> has an edge that points to a <see cref="ReferenceResult"/>, and
        /// item edges from that <see cref="ReferenceResult"/> are the references for the range. This gives you the ID of the <see cref="ReferenceResult"/> in this case.
        /// </summary>
        Id<T> GetResultIdForSymbol<T>(ISymbol symbol, string edgeKind, Func<IdFactory, T> vertexCreator) where T : Vertex;

        /// <summary>
        /// Similar to <see cref="GetResultIdForSymbol{T}"/>, but instead of creating the vertex (if needed) and adding an edge, this
        /// simply tracks that this method has been called, and it's up to the caller that got a true return value to create and add the vertex themselves. This is handy
        /// when the actual identity of the node isn't needed by any other consumers, or the vertex creation is expensive and we don't want it running under the lock that
        /// <see cref="GetResultIdForSymbol{T}"/> would have to take.
        /// </summary>
        bool ResultSetNeedsInformationalEdgeAdded(ISymbol symbol, string edgeKind);
    }
}
