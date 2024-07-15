// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    [DataContract]
    internal class VSInternalMapCodeMapping
    {
        /// <summary>
        /// Gets or sets identifier for the document the contents are supposed to be mapped into.
        /// </summary>
        [DataMember(Name = "_vs_textDocument")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public TextDocumentIdentifier? TextDocument
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets strings of code/text to map into TextDocument.
        /// </summary>
        [DataMember(Name = "_vs_contents")]
        public string[] Contents
        {
            get;
            set;
        }

        /// <summary>
        /// Prioritized Locations to be used when applying heuristics. For example, cursor location,
        /// related classes (in other documents), viewport, etc. Earlier items should be considered
        /// higher priority.
        /// </summary>
        [DataMember(Name = "_vs_focusLocations")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Location[][]? FocusLocations
        {
            get;
            set;
        }
    }
}
