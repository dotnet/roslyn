// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Server provided options for pull diagnostic requests.
    /// </summary>
    [DataContract]
    internal record class VSInternalDiagnosticOptions : IWorkDoneProgressOptions
    {
        /// <summary>
        /// Gets or sets a list of id's used to identify diagnostics that may be coming
        /// from build systems instead of a language server.
        ///
        /// VS client will then use the information to do any merging logic in the Error List.
        /// Maps to <see cref="VSDiagnostic.Identifier"/>.
        /// </summary>
        [DataMember(Name = "_vs_buildOnlyDiagnosticIds")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string[]? BuildOnlyDiagnosticIds { get; init; }

        /// <summary>
        /// Gets or sets a list of diagnostic kinds used to query diagnostics in each context.
        /// </summary>
        [DataMember(Name = "_vs_diagnosticKinds")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public VSInternalDiagnosticKind[]? DiagnosticKinds { get; init; }

        /// <summary>
        /// Gets or sets a value indicating whether the server provides support for sending diagnostics requests for all project contexts.
        /// </summary>
        [DataMember(Name = "_vs_supportsMultipleContextDiagnostics")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool SupportsMultipleContextsDiagnostics { get; init; }

        /// <summary>
        /// Gets or sets a value indicating whether work done progress is supported.
        /// </summary>
        [DataMember(Name = "_vs_workDoneProgress")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool WorkDoneProgress { get; init; }
    }
}
