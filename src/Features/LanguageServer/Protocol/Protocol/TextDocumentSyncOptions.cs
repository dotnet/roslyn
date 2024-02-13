// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class which represents configuration values indicating how text documents should be synced.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#textDocumentSyncOptions">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class TextDocumentSyncOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether open and close notifications are sent to the server.
        /// </summary>
        [DataMember(Name = "openClose")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool OpenClose
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the value indicating how text documents are synced with the server.
        /// </summary>
        [DataMember(Name = "change")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(TextDocumentSyncKind.None)]
        public TextDocumentSyncKind? Change
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether 'will save' notifications are sent to the server.
        /// </summary>
        [DataMember(Name = "willSave")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool WillSave
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether 'will save until' notifications are sent to the server.
        /// </summary>
        [DataMember(Name = "willSaveWaitUntil")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool WillSaveWaitUntil
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether save notifications are sent to the server.
        /// </summary>
        [DataMember(Name = "save")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public SumType<bool, SaveOptions>? Save
        {
            get;
            set;
        }
    }
}
