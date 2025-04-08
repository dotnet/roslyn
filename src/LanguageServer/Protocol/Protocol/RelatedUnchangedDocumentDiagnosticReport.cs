// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

/// <summary>
/// Class representing an unchanged diagnostic report with a set of related documents.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#relatedUnchangedDocumentDiagnosticReport">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.17</remarks>
[Kind(DocumentDiagnosticReportKind.Unchanged)]
internal sealed class RelatedUnchangedDocumentDiagnosticReport : UnchangedDocumentDiagnosticReport
{
    /// <summary>
    /// Diagnostics of related documents.
    /// <para>
    /// </para>
    /// This information is useful in programming languages where code in a
    /// file A can generate diagnostics in a file B which A depends on. An
    /// example of such a language is C/C++ where macro definitions in a file
    /// a.cpp can result in errors in a header file b.hpp.
    /// </summary>
    [JsonPropertyName("relatedDocuments")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<Uri, SumType<FullDocumentDiagnosticReport, UnchangedDocumentDiagnosticReport>>? RelatedDocuments
    {
        get;
        set;
    }
}
