// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Class representing the options for on auto insert.
    /// </summary>
    internal class VSInternalDocumentOnAutoInsertOptions
    {
        /// <summary>
        /// Gets or sets trigger characters for on auto insert.
        /// </summary>
        [JsonPropertyName("_vs_triggerCharacters")]
        public string[] TriggerCharacters
        {
            get;
            set;
        }
    }
}