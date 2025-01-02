// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class representing the options for a create file operation.
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#renameFileOptions">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    /// <remarks>Since LSP 3.13</remarks>
    internal class RenameFileOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether the rename should overwrite the target if it already exists. (Overwrite wins over ignoreIfExists).
        /// </summary>
        /// <remarks>
        /// <see cref="Overwrite"/> wins over <see cref="IgnoreIfExists"/>.
        /// </remarks>
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
        /// <remarks>
        /// <see cref="Overwrite"/> wins over <see cref="IgnoreIfExists"/>.
        /// </remarks>
        [JsonPropertyName("ignoreIfExists")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool IgnoreIfExists
        {
            get;
            set;
        }
    }
}
