// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class representing diagnostic information about the context of a code action
    ///
    /// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#codeActionContext">Language Server Protocol specification</see> for additional information.
    /// </summary>
    [DataContract]
    internal class CodeActionContext
    {
        /// <summary>
        /// Gets or sets an array of diagnostics relevant to a code action.
        /// </summary>
        [DataMember(Name = "diagnostics")]
        public Diagnostic[] Diagnostics
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets an array of code action kinds to filter for.
        /// </summary>
        [DataMember(Name = "only")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public CodeActionKind[]? Only
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the <see cref="CodeActionTriggerKind"/> indicating how the code action was triggered..
        /// </summary>
        [DataMember(Name = "triggerKind")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public CodeActionTriggerKind? TriggerKind
        {
            get;
            set;
        }
    }
}
