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
        [System.Text.Json.Serialization.JsonPropertyName("_vs_group")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public string? Group
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the priority level of the code action.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("_vs_priority")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public VSInternalPriorityLevel? Priority
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the range of the span this action is applicable to.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("_vs_applicableRange")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public Range? ApplicableRange
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the children of this action.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("_vs_children")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public VSInternalCodeAction[]? Children
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the telemetry id of this action.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("_vs_telemetryId")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public Guid? TelemetryId
        {
            get;
            set;
        }
    }
}
