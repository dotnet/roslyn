// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using System.Runtime.Serialization;
    using Roslyn.Text.Adornments;
    using Newtonsoft.Json;

    /// <summary>
    /// Class which represents references information.
    /// </summary>
    [DataContract]
    internal class VSInternalReferenceItem
    {
        private object? definitionTextValue = null;
        private object? textValue = null;

        /// <summary>
        /// Gets or sets the reference id.
        /// </summary>
        [DataMember(Name = "_vs_id", IsRequired = true)]
        public int Id
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the reference location.
        /// </summary>
        [DataMember(Name = "_vs_location")]
        public Location Location
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the definition Id.
        /// </summary>
        [DataMember(Name = "_vs_definitionId")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? DefinitionId
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the definition text displayed as a header when references are grouped by Definition.
        /// Must be of type <see cref="string"/>, <see cref="ClassifiedTextElement"/>, <see cref="ContainerElement"/> and <see cref="ImageElement"/>.
        /// </summary>
        /// <remarks>
        /// This element should colorize syntax, but should not contain highlighting, e.g. <see cref="ClassifiedTextRun"/>
        /// embedded within <see cref="ClassifiedTextElement"/> should not define <see cref="ClassifiedTextRun.MarkerTagType"/>.
        /// </remarks>
        [DataMember(Name = "_vs_definitionText")]
        [JsonConverter(typeof(ObjectContentConverter))]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public object? DefinitionText
        {
            get
            {
                return this.definitionTextValue;
            }

            set
            {
                if (value == null || // Null is accepted since for non-definition references
                    (value is ImageElement || value is ContainerElement || value is ClassifiedTextElement || value is string))
                {
                    this.definitionTextValue = value;
                }
                else
                {
                    throw new InvalidOperationException($"{value.GetType()} is an invalid type.");
                }
            }
        }

        /// <summary>
        /// Gets or sets the resolution status.
        /// </summary>
        [DataMember(Name = "_vs_resolutionStatus")]
        public VSInternalResolutionStatusKind ResolutionStatus
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the reference kind.
        /// </summary>
        [DataMember(Name = "_vs_kind")]
        public VSInternalReferenceKind[] Kind
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the document name to be displayed to user when needed.This can be used in cases where URI doesn't have a user friendly file name or it is a remote URI.
        /// </summary>
        [DataMember(Name = "_vs_documentName")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? DocumentName { get; set; }

        /// <summary>
        /// Gets or sets the project name.
        /// </summary>
        [DataMember(Name = "_vs_projectName")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? ProjectName { get; set; }

        /// <summary>
        /// Gets or sets the containing type.
        /// </summary>
        [DataMember(Name = "_vs_containingType")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? ContainingType { get; set; }

        /// <summary>
        /// Gets or sets the containing member.
        /// </summary>
        [DataMember(Name = "_vs_containingMember")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? ContainingMember { get; set; }

        /// <summary>
        /// Gets or sets the text value for a location reference.
        /// Must be of type <see cref="ImageElement"/> or <see cref="ContainerElement"/> or <see cref="ClassifiedTextElement"/> or <see cref="string"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This element should colorize syntax and highlight the range containing the reference.
        /// Highlighting can be achieved by setting <see cref="ClassifiedTextRun.MarkerTagType"/>
        /// on <see cref="ClassifiedTextRun"/> embedded within <see cref="ClassifiedTextElement"/>.
        /// </para>
        /// <para>
        /// Encouraged values for <see cref="ClassifiedTextRun.MarkerTagType"/> are:
        /// <c>"MarkerFormatDefinition/HighlightedReference"</c> for read references,
        /// <c>"MarkerFormatDefinition/HighlightedWrittenReference"</c> for write references,
        /// <c>"MarkerFormatDefinition/HighlightedDefinition"</c> for definitions.
        /// </para>
        /// </remarks>
        [DataMember(Name = "_vs_text")]
        [JsonConverter(typeof(ObjectContentConverter))]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
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

        /// <summary>
        /// Gets or sets the text value for display path.This would be a friendly display name for scenarios where the actual path on disk may be confusing for users.
        /// This doesn't have to correspond to a real file path, but does need to be parsable by the various Path.GetFileName() methods.
        /// </summary>
        [DataMember(Name = "_vs_displayPath")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? DisplayPath { get; set; }

        /// <summary>
        /// Gets or sets the origin of the item.The origin is used to filter remote results.
        /// </summary>
        [DataMember(Name = "_vs_origin")]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public VSInternalItemOrigin? Origin { get; set; }

        /// <summary>
        /// Gets or sets the icon to show for the definition header.
        /// </summary>
        [DataMember(Name = "_vs_definitionIcon")]
        [JsonConverter(typeof(ImageElementConverter))]
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public ImageElement? DefinitionIcon { get; set; }
    }
}
