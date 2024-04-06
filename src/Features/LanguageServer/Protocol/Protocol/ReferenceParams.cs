// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class representing find reference parameter for find reference request.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#referenceParams">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class ReferenceParams : TextDocumentPositionParams, IPartialResultParams<object>
    {
        // Using IPartialResultParams<object> instead of IPartialResultParams<Location[]> to
        // allow the VS protocol extension to allow returning VSReferenceItem[]

        /// <summary>
        /// Gets or sets the reference context.
        /// </summary>
        [DataMember(Name = "context")]
        public ReferenceContext Context
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the value of the PartialResultToken instance.
        /// </summary>
        [DataMember(Name = Methods.PartialResultTokenName, IsRequired = false)]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public IProgress<object>? PartialResultToken
        {
            get;
            set;
        }
    }
}
