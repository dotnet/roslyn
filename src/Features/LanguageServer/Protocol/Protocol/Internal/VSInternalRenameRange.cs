// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class which represents a possible result value of the 'textDocument/prepareRename' request,
    /// together with extra VS-specific options.
    /// </summary>
    [DataContract]
    internal class VSInternalRenameRange : RenameRange
    {
        /// <summary>
        /// Gets or sets the supported options for the rename request.
        /// </summary>
        [DataMember(Name = "_vs_supportedOptions")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public VSInternalRenameOptionSupport[]? SupportedOptions
        {
            get;
            set;
        }
    }
}
