// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class representing the options for a create file operation.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#renameFileOptions">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class RenameFileOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether the rename should overwrite the target if it already exists. (Overwrite wins over ignoreIfExists).
        /// </summary>
        [DataMember(Name = "overwrite")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool Overwrite
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the action should be ignored if the file already exists.
        /// </summary>
        [DataMember(Name = "ignoreIfExists")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool IgnoreIfExists
        {
            get;
            set;
        }
    }
}
