// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.LanguageServer.Protocol;
using Newtonsoft.Json;

namespace Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Graph
{
    /// <summary>
    /// Represents a hoverResult vertex for serialization. See https://github.com/Microsoft/language-server-protocol/blob/main/indexFormat/specification.md#more-about-request-textdocumenthover for further details.
    /// </summary>
    internal sealed class HoverResult : Vertex
    {
        [JsonProperty("result")]
        public Hover Result { get; }

        public HoverResult(Hover result, IdFactory idFactory)
            : base(label: "hoverResult", idFactory)
        {
            Result = result;
        }
    }
}
