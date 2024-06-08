﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class which represents a file change event.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#fileEvent">Language Server Protocol specification</see> for additional information.
    /// </summary>
    internal class FileEvent
    {
        /// <summary>
        /// Gets or sets the URI of the file.
        /// </summary>
        [JsonPropertyName("uri")]
        [JsonConverter(typeof(DocumentUriConverter))]
        public Uri Uri
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the file change type.
        /// </summary>
        [JsonPropertyName("type")]
        public FileChangeType FileChangeType
        {
            get;
            set;
        }
    }
}
