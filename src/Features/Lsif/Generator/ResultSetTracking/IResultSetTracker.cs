﻿// Licensed to the .NET Foundation under one or more agreements.
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
        Id<ResultSet> GetResultSetIdForSymbol(ISymbol symbol);
        Id<T> GetResultIdForSymbol<T>(ISymbol symbol, string edgeKind, Func<T> vertexCreator) where T : Vertex;
        bool ResultSetNeedsInformationalEdgeAdded(ISymbol symbol, string edgeKind);
    }
}
