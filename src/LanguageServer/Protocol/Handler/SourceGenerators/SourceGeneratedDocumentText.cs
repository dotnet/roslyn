// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

/// <summary>
/// Source generated file text result.  The client uses the resultId to inform what the text value is.
/// 
/// An unchanged result has a non-null resultId (same as client request resultId) + null text.
/// 
/// A changed result has a new non-null resultId + possibly null text (if the sg document no longer exists).
/// 
/// In rare circumstances it is possible to get a null resultId + null text - this happens when
/// the source generated document is not open AND the source generated document no longer exists
/// </summary>
internal sealed record SourceGeneratedDocumentText(
    [property: JsonPropertyName("resultId")] string? ResultId,
    [property: JsonPropertyName("text")] string? Text);
