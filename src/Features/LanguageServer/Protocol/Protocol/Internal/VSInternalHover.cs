// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Extension to Hover which adds additional data for colorization.
    /// </summary>
    internal class VSInternalHover : Hover
    {
        /// <summary>
        /// Gets or sets the value which represents the hover content as a tree
        /// of objects from the Microsoft.VisualStudio.Text.Adornments namespace,
        /// such as ContainerElements, ClassifiedTextElements and ClassifiedTextRuns.
        /// </summary>
        [JsonPropertyName("_vs_rawContent")]
        [JsonConverter(typeof(ObjectContentConverter))]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? RawContent
        {
            get;
            set;
        }
    }
}
