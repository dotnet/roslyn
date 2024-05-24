// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class representing the options for a create file operation.
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#createFileOptions">Language Server Protocol specification</see> for additional information.
    /// </summary>
    internal class CreateFileOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether the creation should overwrite the file if it already exists. (Overwrite wins over ignoreIfExists).
        /// </summary>
        [JsonPropertyName("overwrite")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool Overwrite
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the action should be ignored if the file already exists.
        /// </summary>
        [JsonPropertyName("ignoreIfExists")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool IgnoreIfExists
        {
            get;
            set;
        }
    }
}
