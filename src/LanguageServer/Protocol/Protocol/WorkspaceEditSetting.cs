// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class which represents initialization settings for workspace edit.
    /// <para>
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#workspaceEditClientCapabilities">Language Server Protocol specification</see> for additional information.
    /// </para>
    /// </summary>
    internal class WorkspaceEditSetting
    {
        /// <summary>
        /// Whether the client supports versioned document changes in <see cref="WorkspaceEdit"/>
        /// </summary>
        [JsonPropertyName("documentChanges")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool DocumentChanges
        {
            get;
            set;
        }

        /// <summary>
        /// The resource operations the client supports.
        /// </summary>
        /// <remarks>Since LSP 3.13</remarks>
        [JsonPropertyName("resourceOperations")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ResourceOperationKind[]? ResourceOperations
        {
            get;
            set;
        }

        /// <summary>
        /// The client's failure handling strategy when applying workspace changes.
        /// </summary>
        /// <remarks>Since LSP 3.13</remarks>
        [JsonPropertyName("failureHandling")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public FailureHandlingKind? FailureHandling { get; init; }

        /// <summary>
        /// Whether the client normalizes line endings to the client specific setting
        /// when applying workspace changes.
        /// </summary>
        /// <remarks>Since LSP 3.16</remarks>
        [JsonPropertyName("normalizesLineEndings")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? NormalizesLineEndings { get; init; }

        /// <summary>
        /// Whether the client supports change annotations on workspace edits, and
        /// information about how it handles them.
        /// </summary>
        /// <remarks>Since LSP 3.16</remarks>
        [JsonPropertyName("changeAnnotationSupport")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ChangeAnnotationSupport? ChangeAnnotationSupport { get; init; }
    }
}
