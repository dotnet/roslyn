// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using System.Xml.Linq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using static System.Net.Mime.MediaTypeNames;

    /// <summary>
    /// Class which represents renaming client capabilities.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#renameClientCapabilities">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class RenameClientCapabilities : DynamicRegistrationSetting
    {
        /// <summary>
        /// Gets or sets a value indicating whether the client supports testing for validity of rename operations before execution.
        /// </summary>
        [DataMember(Name = "prepareSupport")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool PrepareSupport
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the value indicating the default behavior used by the client when the (`{ defaultBehavior: boolean }`)
        /// result is used in the 'textDocument/prepareRename' request.
        /// </summary>
        [DataMember(Name = "prepareSupportDefaultBehavior")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public PrepareSupportDefaultBehavior? PrepareSupportDefaultBehavior
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the client honors the change annotations in text edits and resource
        /// operations returned via the rename request's workspace edit, by for example presenting the workspace edit in
        /// the user interface and asking for confirmation.
        /// </summary>
        [DataMember(Name = "honorsChangeAnnotations")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool HonorsChangeAnnotations
        {
            get;
            set;
        }
    }
}
