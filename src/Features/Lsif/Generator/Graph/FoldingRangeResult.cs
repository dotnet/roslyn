// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.LanguageServer.Protocol;
using Newtonsoft.Json;

namespace Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Graph
{
    /// <summary>
    /// Represents a foldingRangeResult vertex for serialization. See https://github.com/Microsoft/language-server-protocol/blob/master/indexFormat/specification.md#request-textdocumentfoldingrange for further details.
    /// </summary>
    internal sealed class FoldingRangeResult : Vertex
    {
        [JsonProperty("result")]
        public FoldingRange[] Result { get; }

        public FoldingRangeResult(FoldingRange[] result, IdFactory idFactory)
            : base(label: "foldingRangeResult", idFactory)
        {
            Result = result;
        }
    }
}
