// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Client capabilities specific to the <c>textDocument/foldingRange</c> request.
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#foldingRangeClientCapabilities">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    internal class FoldingRangeSetting : DynamicRegistrationSetting
    {
        /// <summary>
        /// The maximum number of folding ranges that the client prefers to receive
        /// per document. The value serves as a hint, servers are free to follow the
        /// limit.
        /// </summary>
        [JsonPropertyName("rangeLimit")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? RangeLimit
        {
            get;
            set;
        }

        /// <summary>
        /// If set, the client signals that it only supports folding complete lines,
        /// and will ignore <see cref="FoldingRange.StartCharacter"/> and
        /// <see cref="FoldingRange.EndCharacter"/> properties.
        /// </summary>
        [JsonPropertyName("lineFoldingOnly")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool LineFoldingOnly
        {
            get;
            set;
        }

        /// <summary>
        /// Client capabilities specific to <see cref="FoldingRangeKind"/>
        /// </summary>
        /// <remarks>Since 3.17</remarks>
        [JsonPropertyName("foldingRangeKind")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public FoldingRangeKindSet? FoldingRangeKind { get; init; }

        /// <summary>
        /// Gets or sets a value indicating the specific options for the folding range.
        /// </summary>
        /// <remarks>Since 3.17</remarks>
        [JsonPropertyName("foldingRange")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public FoldingRangeSettingOptions? FoldingRange { get; set; }
    }
}
