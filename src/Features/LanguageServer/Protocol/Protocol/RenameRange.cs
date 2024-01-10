// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class which represents a possible result value of the 'textDocument/prepareRename' request.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#textDocument_prepareRename">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class RenameRange
    {
        /// <summary>
        /// Gets or sets the range of the string to rename.
        /// </summary>
        [DataMember(Name = "range")]
        [JsonProperty(Required = Required.Always)]
        public Range Range
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the placeholder text of the string content to be renamed.
        /// </summary>
        [DataMember(Name = "placeholder")]
        [JsonProperty(Required = Required.Always)]
        public string Placeholder
        {
            get;
            set;
        }
    }
}
