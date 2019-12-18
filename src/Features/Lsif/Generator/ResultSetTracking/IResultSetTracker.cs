// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Lsif.Generator.LsifGraph;

namespace Microsoft.CodeAnalysis.Lsif.Generator.ResultSetTracking
{
    /// <summary>
    /// An object that tracks a mapping from symbols to the result sets that have information about those symbols.
    /// </summary>
    internal interface IResultSetTracker
    {
        Id<ResultSet> GetResultSetIdForSymbol(ISymbol symbol);
    }
}
