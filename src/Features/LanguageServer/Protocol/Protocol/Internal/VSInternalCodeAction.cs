// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class used to extend <see cref="CodeAction" /> to add the data field for codeAction/_ms_resolve support.
    /// </summary>
    [DataContract]
    internal class VSInternalCodeAction : CodeAction
    {
        /// <summary>
        /// Gets or sets the group this CodeAction belongs to.
        /// </summary>
        [DataMember(Name = "_vs_group")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? Group
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the priority level of the code action.
        /// </summary>
        [DataMember(Name = "_vs_priority")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public VSInternalPriorityLevel? Priority
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the range of the span this action is applicable to.
        /// </summary>
        [DataMember(Name = "_vs_applicableRange")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Range? ApplicableRange
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the children of this action.
        /// </summary>
        [DataMember(Name = "_vs_children")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public VSInternalCodeAction[]? Children
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the telemetry id of this action.
        /// </summary>
        [DataMember(Name = "_vs_telemetryId")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Guid? TelemetryId
        {
            get;
            set;
        }
    }
}
