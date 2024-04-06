// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class representing a folding range in a document.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#foldingRange">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class FoldingRange
    {
        /// <summary>
        /// Gets or sets the start line value.
        /// </summary>
        [DataMember(Name = "startLine")]
        public int StartLine
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the start character value.
        /// </summary>
        [DataMember(Name = "startCharacter")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? StartCharacter
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the end line value.
        /// </summary>
        [DataMember(Name = "endLine")]
        public int EndLine
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the end character value.
        /// </summary>
        [DataMember(Name = "endCharacter")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? EndCharacter
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the folding range kind.
        /// </summary>
        [DataMember(Name = "kind")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public FoldingRangeKind? Kind
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the collapsedText.
        /// </summary>
        [DataMember(Name = "collapsedText")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? CollapsedText
        {
            get;
            set;
        }
    }
}
