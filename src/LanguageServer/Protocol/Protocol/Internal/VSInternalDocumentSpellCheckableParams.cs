// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Parameter for textDocument/_vs_spellCheckableRanges.
    /// </summary>
    internal sealed class VSInternalDocumentSpellCheckableParams : VSInternalStreamingParams, IPartialResultParams<VSInternalSpellCheckableRangeReport[]>
    {
        /// <inheritdoc/>
        [JsonPropertyName(Methods.PartialResultTokenName)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IProgress<VSInternalSpellCheckableRangeReport[]>? PartialResultToken
        {
            get;
            set;
        }
    }
}
