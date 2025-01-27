// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class representing the document link options for server capabilities.
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#documentLinkOptions">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    internal class DocumentLinkOptions : IWorkDoneProgressOptions
    {
        /// <summary>
        /// Whether or not the server has a resolve provider for document links.
        /// </summary>
        [JsonPropertyName("resolveProvider")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool ResolveProvider { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether work done progress is supported.
        /// </summary>
        [JsonPropertyName("workDoneProgress")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool WorkDoneProgress { get; init; }
    }
}
