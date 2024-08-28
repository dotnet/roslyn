// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class representing the parameters sent for a textDocument/colorPresentation request.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#colorPresentation">Language Server Protocol specification</see> for additional information.
    /// </summary>
    internal class ColorPresentation
    {
        /// <summary>
        /// Gets or sets the label value, i.e. display text to users.
        /// </summary>
        [JsonPropertyName("label")]
        [JsonRequired]
        public string Label
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the text edit.
        /// </summary>
        [JsonPropertyName("textEdit")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SumType<TextEdit, InsertReplaceEdit>? TextEdit
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets any additional text edits.
        /// </summary>
        /// <remarks>
        /// Additional text edits must not interfere with the main text edit.
        /// </remarks>
        [JsonPropertyName("additionalTextEdits")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public TextEdit[]? AdditionalTextEdits
        {
            get;
            set;
        }
    }
}
