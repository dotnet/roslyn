// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class which represents renaming client capabilities.
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#renameClientCapabilities">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    internal class RenameClientCapabilities : DynamicRegistrationSetting
    {
        /// <summary>
        /// Gets or sets a value indicating whether the client supports testing for validity of rename operations before execution.
        /// </summary>
        /// <remarks>Since LSP 3.12</remarks>
        [JsonPropertyName("prepareSupport")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool PrepareSupport
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the value indicating the default behavior used by the client when the
        /// <see cref="DefaultBehaviorPrepareRename.DefaultBehavior"/> result is used in the 'textDocument/prepareRename' request.
        /// </summary>
        /// <remarks>Since LSP 3.16</remarks>
        [JsonPropertyName("prepareSupportDefaultBehavior")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public PrepareSupportDefaultBehavior? PrepareSupportDefaultBehavior
        {
            get;
            set;
        }

        /// <summary>
        /// Whether the client honors the change annotations in text edits and resource
        /// operations returned via the rename request's workspace edit, by for example presenting the workspace edit in
        /// the user interface and asking for confirmation.
        /// </summary>
        /// <remarks>Since LSP 3.16</remarks>
        [JsonPropertyName("honorsChangeAnnotations")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool HonorsChangeAnnotations
        {
            get;
            set;
        }
    }
}
