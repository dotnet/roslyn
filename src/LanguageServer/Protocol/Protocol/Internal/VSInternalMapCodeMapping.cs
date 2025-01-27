// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    internal class VSInternalMapCodeMapping
    {
        /// <summary>
        /// Gets or sets identifier for the document the contents are supposed to be mapped into.
        /// </summary>
        [JsonPropertyName("_vs_textDocument")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public TextDocumentIdentifier? TextDocument
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets strings of code/text to map into TextDocument.
        /// </summary>
        [JsonPropertyName("_vs_contents")]
        public string[] Contents
        {
            get;
            set;
        }

        /// <summary>
        /// Prioritized Locations to be used when applying heuristics. For example, cursor location,
        /// related classes (in other documents), viewport, etc. Earlier items should be considered
        /// higher priority.
        /// </summary>
        [JsonPropertyName("_vs_focusLocations")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Location[][]? FocusLocations
        {
            get;
            set;
        }
    }
}
