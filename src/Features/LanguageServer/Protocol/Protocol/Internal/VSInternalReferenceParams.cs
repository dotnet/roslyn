// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Runtime.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Class which represents extensions of <see cref="ReferenceParams"/> passed as parameter of find reference requests.
    /// </summary>
    internal class VSInternalReferenceParams : ReferenceParams
    {
        /// <summary>
        /// Gets or sets a value indicating the scope of returned items.
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("_vs_scope")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public VSInternalItemOrigin? Scope
        {
            get;
            set;
        }
    }
}
