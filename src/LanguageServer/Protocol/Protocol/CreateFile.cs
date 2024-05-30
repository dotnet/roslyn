// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class representing a create file operation.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#createFile">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [Kind("create")]
    internal class CreateFile
    {
        /// <summary>
        /// Gets the kind value.
        /// </summary>
        [JsonPropertyName("kind")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Member can't be static since it's part of the protocol")]
        public string Kind => "create";

        /// <summary>
        /// Gets or sets the resource to create.
        /// </summary>
        [JsonPropertyName("uri")]
        [JsonRequired]
        [JsonConverter(typeof(DocumentUriConverter))]
        public Uri Uri
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the additional options.
        /// </summary>
        [JsonPropertyName("options")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public CreateFileOptions? Options
        {
            get;
            set;
        }
    }
}