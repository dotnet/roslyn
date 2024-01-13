﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class which represents a text document.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#textDocumentItem">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class TextDocumentItem
    {
        /// <summary>
        /// Gets or sets the document URI.
        /// </summary>
        [DataMember(Name = "uri")]
        [JsonConverter(typeof(DocumentUriConverter))]
        public Uri Uri
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the document language identifier.
        /// </summary>
        [DataMember(Name = "languageId")]
        public string LanguageId
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the document version.
        /// </summary>
        [DataMember(Name = "version")]
        public int Version
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the content of the opened text document.
        /// </summary>
        [DataMember(Name = "text")]
        public string Text
        {
            get;
            set;
        }
    }
}
