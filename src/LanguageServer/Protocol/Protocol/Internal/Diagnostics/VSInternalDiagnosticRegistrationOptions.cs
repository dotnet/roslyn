// <copyright file="VSInternalDiagnosticRegistrationOptions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Diagnostic registration options.
    /// </summary>
    internal record class VSInternalDiagnosticRegistrationOptions : VSInternalDiagnosticOptions, ITextDocumentRegistrationOptions, IStaticRegistrationOptions
    {
        /// <summary>
        /// Gets or sets the document filters for this registration option.
        /// </summary>
        [JsonPropertyName("documentSelector")]
        public DocumentFilter[]? DocumentSelector { get; set; }

        /// <summary>
        /// Gets or sets the id used to register the request.  The id can be used to deregister the request again.
        /// </summary>
        [JsonPropertyName("id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Id { get; set; }
    }
}
