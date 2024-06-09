// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol
{
    /// <summary>
    /// Interface for request/notification params that apply to a a position inside a document
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#textDocumentPositionParams">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    internal interface ITextDocumentPositionParams : ITextDocumentParams
    {
        /// <summary>
        /// The position within the document.
        /// </summary>
        [JsonPropertyName("position")]
        [JsonRequired]
        public Position Position { get; set; }
    }
}
