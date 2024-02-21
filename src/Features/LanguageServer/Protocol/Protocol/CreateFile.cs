// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class representing a create file operation.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#createFile">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    [Kind("create")]
    internal class CreateFile
    {
        /// <summary>
        /// Gets the kind value.
        /// </summary>
        [DataMember(Name = "kind")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Member can't be static since it's part of the protocol")]
        public string Kind => "create";

        /// <summary>
        /// Gets or sets the resource to create.
        /// </summary>
        [DataMember(Name = "uri", IsRequired = true)]
        [JsonConverter(typeof(DocumentUriConverter))]
        public Uri Uri
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the additional options.
        /// </summary>
        [DataMember(Name = "options")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public CreateFileOptions? Options
        {
            get;
            set;
        }
    }
}