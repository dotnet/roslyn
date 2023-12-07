// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// <see cref="VSDiagnosticProjectInformation"/> represents the project and context in which the <see cref="VSDiagnostic"/> is generated.
    /// </summary>
    [DataContract]
    internal class VSDiagnosticProjectInformation
    {
        /// <summary>
        /// Gets or sets a human-readable identifier for the project in which the diagnostic was generated.
        /// </summary>
        [DataMember(Name = "_vs_projectName")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? ProjectName { get; set; }

        /// <summary>
        /// Gets or sets a human-readable identifier for the build context (e.g. Win32 or MacOS)
        /// in which the diagnostic was generated.
        /// </summary>
        [DataMember(Name = "_vs_context")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? Context { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier for the project in which the diagnostic was generated.
        /// </summary>
        [DataMember(Name = "_vs_projectIdentifier")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? ProjectIdentifier { get; set; }
    }
}
