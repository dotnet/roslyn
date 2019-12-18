// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Lsif.Generator.LsifGraph
{
    /// <summary>
    /// Represents a single ResultSet in the LSIF file. See https://github.com/Microsoft/language-server-protocol/blob/master/indexFormat/specification.md#result-set for further details.
    /// </summary>
    internal sealed class ResultSet : Vertex
    {
        public ResultSet()
            : base(label: "resultSet")
        {
        }
    }
}
