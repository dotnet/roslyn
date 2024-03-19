// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class representing the response of a textDocument/documentLink request.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#documentLink">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class DocumentLink
    {
        /// <summary>
        /// Gets or sets the range the link applies to.
        /// </summary>
        [DataMember(Name = "range")]
        public Range Range
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the uri that the link points to.
        /// </summary>
        [DataMember(Name = "target")]
        [JsonConverter(typeof(DocumentUriConverter))]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Uri? Target
        {
            get;
            set;
        }
    }
}
