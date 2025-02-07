// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class which represents the parameter that's sent with the 'workspace/symbol' request.
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#workspaceSymbolParams">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    internal class WorkspaceSymbolParams
#pragma warning disable CS0618 // SymbolInformation is obsolete but this class is not
        : IPartialResultParams<SumType<SymbolInformation[], WorkspaceSymbol[]>>, IWorkDoneProgressParams
#pragma warning restore CS0618
    {
        /// <summary>
        /// Gets or sets the query (a non-empty string).
        /// </summary>
        [JsonPropertyName("query")]
        [JsonRequired]
        public string Query { get; set; }

        /// <inheritdoc/>
        [JsonPropertyName(Methods.PartialResultTokenName)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
#pragma warning disable CS0618 // SymbolInformation is obsolete but this property is not
        public IProgress<SumType<SymbolInformation[], WorkspaceSymbol[]>>? PartialResultToken { get; set; }
#pragma warning restore CS0618

        /// <inheritdoc/>
        [JsonPropertyName(Methods.WorkDoneTokenName)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IProgress<WorkDoneProgress>? WorkDoneToken { get; set; }
    }
}
