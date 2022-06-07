// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Graph
{
    /// <summary>
    /// Represents a referenceResult vertex for serialization. See https://github.com/Microsoft/language-server-protocol/blob/master/indexFormat/specification.md#request-textdocumentreferences for further details.
    /// </summary>
    internal sealed class ReferenceResult : Vertex
    {
        public ReferenceResult(IdFactory idFactory)
            : base(label: "referenceResult", idFactory)
        {
        }
    }
}
