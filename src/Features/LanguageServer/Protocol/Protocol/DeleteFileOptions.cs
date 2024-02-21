// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class representing the options for a create file operation.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#deleteFileOptions">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class DeleteFileOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether the delete operation should be applied recursively if a folder is denoted. (Overwrite wins over ignoreIfNotExists).
        /// </summary>
        [DataMember(Name = "recursive")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool Recursive
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the action should be ignored if the file doesn't exists.
        /// </summary>
        [DataMember(Name = "ignoreIfNotExists")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool IgnoreIfNotExists
        {
            get;
            set;
        }
    }
}
