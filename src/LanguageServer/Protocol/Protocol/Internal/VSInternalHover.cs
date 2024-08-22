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
        public object? RawContent { get; set; }

        /// <summary>
        /// The hover's content
        /// </summary>
        /// <remarks>
        /// This may only be null when <see cref="RawContent"/> is specified instead of <see cref="Contents"/>.
        /// </remarks>
        [JsonPropertyName("contents")]
        [JsonRequired]
#pragma warning disable CS0618 // MarkedString is obsolete but this property is not
        public new SumType<string, MarkedString, SumType<string, MarkedString>[], MarkupContent>? Contents
        {
            get => contentsIsNull ? (SumType<string, MarkedString, SumType<string, MarkedString>[], MarkupContent>?)null : base.Contents;
            set
            {
                contentsIsNull = value is null;
                if (value is not null)
                {
                    base.Contents = value.Value;
                }
            }
        }

        bool contentsIsNull = false;
#pragma warning restore CS0618
    }
}
