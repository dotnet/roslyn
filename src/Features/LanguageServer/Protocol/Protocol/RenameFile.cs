// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class representing a rename file operation.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#renameFile">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [Kind("rename")]
    internal class RenameFile
    {
        /// <summary>
        /// Gets the kind value.
        /// </summary>
        [JsonPropertyName("kind")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Member can't be static since it's part of the protocol")]
        public string Kind => "rename";

        /// <summary>
        /// Gets or sets the old (existing) location.
        /// </summary>
        [JsonPropertyName("oldUri")]
        [JsonRequired]
        [JsonConverter(typeof(DocumentUriConverter))]
        public Uri OldUri
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the new location.
        /// </summary>
        [JsonPropertyName("newUri")]
        [JsonRequired]
        [JsonConverter(typeof(DocumentUriConverter))]
        public Uri NewUri
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the rename options.
        /// </summary>
        [JsonPropertyName("options")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public RenameFileOptions? Options
        {
            get;
            set;
        }
    }
}