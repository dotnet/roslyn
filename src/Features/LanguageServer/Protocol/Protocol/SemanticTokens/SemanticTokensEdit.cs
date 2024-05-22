// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class representing an individual edit incrementally applied to a previous
    /// semantic tokens response from the Document provider.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#semanticTokensEdit">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1036:Override methods on comparable types", Justification = "Pending implementation of IComparable")]
    internal class SemanticTokensEdit : IComparable<SemanticTokensEdit>
    {
        /// <summary>
        /// Gets or sets the position in the previous response's <see cref="SemanticTokens.Data"/>
        /// to begin the edit.
        /// </summary>
        [JsonPropertyName("start")]
        public int Start { get; set; }

        /// <summary>
        /// Gets or sets the number of numbers to delete in the <see cref="SemanticTokens.Data"/>
        /// from the previous response.
        /// </summary>
        [JsonPropertyName("deleteCount")]
        public int DeleteCount { get; set; }

        /// <summary>
        /// Gets or sets an array containing the encoded semantic tokens information to insert
        /// into a previous response.
        /// </summary>
        [JsonPropertyName("data")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int[]? Data { get; set; }

        /// <summary>
        /// Compares two <see cref="SemanticTokensEdit"/>s based on their order.
        /// </summary>
        /// <param name="other">The other edit.</param>
        /// <returns>-1 if this item comes first and 1 if it comes second.</returns>
        public int CompareTo(SemanticTokensEdit? other)
            => other is null ? -1 : this.Start.CompareTo(other.Start);
    }
}
