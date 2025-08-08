// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System;
using System.Text.Json.Serialization;
using Roslyn.Text.Adornments;

/// <summary>
/// Class which represents references information.
/// </summary>
internal sealed class VSInternalReferenceItem
{
    private object? definitionTextValue = null;
    private object? textValue = null;

    /// <summary>
    /// Gets or sets the reference id.
    /// </summary>
    [JsonPropertyName("_vs_id")]
    [JsonRequired]
    public int Id
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the reference location.
    /// </summary>
    [JsonPropertyName("_vs_location")]
    public Location? Location
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the definition Id.
    /// </summary>
    [JsonPropertyName("_vs_definitionId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
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
    [JsonPropertyName("_vs_definitionText")]
    [JsonConverter(typeof(ObjectContentConverter))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? DefinitionText
    {
        get
        {
            return this.definitionTextValue;
        }

        set
        {
            // Null is accepted since for non-definition references
            if (value is null or ImageElement or ContainerElement or ClassifiedTextElement or string)
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
    [JsonPropertyName("_vs_resolutionStatus")]
    public VSInternalResolutionStatusKind ResolutionStatus
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the reference kind.
    /// </summary>
    [JsonPropertyName("_vs_kind")]
    public VSInternalReferenceKind[] Kind
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the document name to be displayed to user when needed.This can be used in cases where URI doesn't have a user friendly file name or it is a remote URI.
    /// </summary>
    [JsonPropertyName("_vs_documentName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DocumentName { get; set; }

    /// <summary>
    /// Gets or sets the project name.
    /// </summary>
    [JsonPropertyName("_vs_projectName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ProjectName { get; set; }

    /// <summary>
    /// Gets or sets the containing type.
    /// </summary>
    [JsonPropertyName("_vs_containingType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ContainingType { get; set; }

    /// <summary>
    /// Gets or sets the containing member.
    /// </summary>
    [JsonPropertyName("_vs_containingMember")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
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
            if (value is ImageElement or ContainerElement or ClassifiedTextElement or string)
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
    [JsonPropertyName("_vs_displayPath")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DisplayPath { get; set; }

    /// <summary>
    /// Gets or sets the origin of the item.The origin is used to filter remote results.
    /// </summary>
    [JsonPropertyName("_vs_origin")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public VSInternalItemOrigin? Origin { get; set; }

    /// <summary>
    /// Gets or sets the icon to show for the definition header.
    /// </summary>
    [JsonPropertyName("_vs_definitionIcon")]
    [JsonConverter(typeof(ImageElementConverter))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ImageElement? DefinitionIcon { get; set; }
}
