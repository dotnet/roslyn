// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.SemanticSearch;

[DataContract]
internal readonly struct QueryExecutionOptions()
{
    /// <summary>
    /// Execution is cancelled once Find returns this many results.
    /// </summary>
    [property: DataMember]
    public int? ResultCountLimit { get; init; }

    /// <summary>
    /// Keep compiled query available until <see cref="ISemanticSearchQueryService.DiscardQuery(CompiledQueryId)"/> is called.
    /// </summary>
    [property: DataMember]
    public bool KeepCompiledQuery { get; init; }
}
