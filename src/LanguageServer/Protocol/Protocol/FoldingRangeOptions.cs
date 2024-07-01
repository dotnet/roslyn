// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class representing the folding range provider options for initialization.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#foldingRangeOptions">Language Server Protocol specification</see> for additional information.
    /// </summary>
    internal class FoldingRangeOptions : IWorkDoneProgressOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether work done progress is supported.
        /// </summary>
        [JsonPropertyName("workDoneProgress")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool WorkDoneProgress { get; init; }
    }
}
