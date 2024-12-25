// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Contracts.EditAndContinue;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EditAndContinue;

/// <summary>
/// Represents a span of an active statement tracked by the client editor.
/// </summary>
/// <param name="Id">The corresponding <see cref="ActiveStatement.Id"/>.</param>
/// <param name="LineSpan">Line span in the mapped document.</param>
/// <param name="Flags">Flags.</param>
/// <param name="UnmappedDocumentId">
/// The id of the unmapped document where the source of the active statement is and from where the statement might be mapped to <see cref="LineSpan"/> via <c>#line</c> directive.
/// Null if unknown (not determined yet).
/// </param>
[DataContract]
internal readonly record struct ActiveStatementSpan(
    [property: DataMember(Order = 0)] ActiveStatementId Id,
    [property: DataMember(Order = 1)] LinePositionSpan LineSpan,
    [property: DataMember(Order = 2)] ActiveStatementFlags Flags,
    [property: DataMember(Order = 3)] DocumentId? UnmappedDocumentId = null);
