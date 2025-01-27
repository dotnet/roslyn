// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using Roslyn.Text.Adornments;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Extension class for <see cref="Protocol.Location"/>.  Used to relay reference text information with colorization.
    /// </summary>
    internal class VSInternalLocation : VSLocation
    {
        private object? textValue = null;

        /// <summary>
        /// Gets or sets the text value for a location reference. Must be of type <see cref="ImageElement"/> or <see cref="ContainerElement"/> or <see cref="ClassifiedTextElement"/> or <see cref="string"/>.
        /// </summary>
        [JsonPropertyName("_vs_text")]
        [JsonConverter(typeof(ObjectContentConverter))]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? Text
        {
            get
            {
                return this.textValue;
            }

            set
            {
                if (value is ImageElement || value is ContainerElement || value is ClassifiedTextElement || value is string)
                {
                    this.textValue = value;
                }
                else
                {
                    throw new InvalidOperationException($"{value?.GetType()} is an invalid type.");
                }
            }
        }
    }
}