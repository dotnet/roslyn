// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Lists the features that support sending off feature requests for all available project contexts instead of the default active one.
    /// </summary>
    [DataContract]
    internal class VSInternalMultipleContextFeatures
    {
        /// <summary>
        /// Gets or sets a value indicating whether the server provides support for sending diagnostics requests for all project contexts.
        /// </summary>
        [DataMember(Name = "_vs_SupportsMultipleContextDiagnostics")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool SupportsMultipleContextsDiagnostics
        {
            get;
            set;
        }
    }
}
