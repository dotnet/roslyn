// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class representing a single signature of a callable item.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#signatureInformation">Language Server Protocol specification</see> for additional information.
    /// </summary>
    internal class SignatureInformation
    {
        /// <summary>
        /// Gets or sets the label of this signature.
        /// </summary>
        [JsonPropertyName("label")]
        public string Label
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the human-readable documentation of this signature.
        /// </summary>
        [JsonPropertyName("documentation")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SumType<string, MarkupContent>? Documentation
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the parameters of this signature.
        /// </summary>
        [JsonPropertyName("parameters")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ParameterInformation[]? Parameters
        {
            get;
            set;
        }
    }
}
