// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// A change operation with a change annotation identifier
/// </summary>
interface IAnnotatedChange
{
    /// <summary>
    /// Optional annotation identifier describing the operation
    /// </summary>
    /// <remarks>Since 3.16</remarks>
    [JsonPropertyName("annotationId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ChangeAnnotationIdentifier? AnnotationId { get; init; }
}
