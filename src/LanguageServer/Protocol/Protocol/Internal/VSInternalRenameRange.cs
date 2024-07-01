// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class which represents a possible result value of the 'textDocument/prepareRename' request,
    /// together with extra VS-specific options.
    /// </summary>
    internal class VSInternalRenameRange : RenameRange
    {
        /// <summary>
        /// Gets or sets the supported options for the rename request.
        /// </summary>
        [JsonPropertyName("_vs_supportedOptions")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public VSInternalRenameOptionSupport[]? SupportedOptions
        {
            get;
            set;
        }
    }
}
