// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class which represents formatting options.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#formattingOptions">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class FormattingOptions
    {
        /// <summary>
        /// Gets or sets the number of spaces to be inserted per tab.
        /// </summary>
        [DataMember(Name = "tabSize")]
        public int TabSize
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether tabs should be spaces.
        /// </summary>
        [DataMember(Name = "insertSpaces")]
        public bool InsertSpaces
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the other potential formatting options.
        /// </summary>
        [JsonExtensionData]
        public Dictionary<string, object>? OtherOptions
        {
            get;
            set;
        }
    }
}
