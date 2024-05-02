// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EditAndContinue;

/// <summary>
/// Represents location of an active statement tracked by the client editor.
/// </summary>
/// <param name="Id">The corresponding <see cref="ActiveStatement.Id"/>.</param>
/// <param name="LineSpan">Line span in the mapped document.</param>
[DataContract]
internal readonly record struct ActiveStatementLineSpan(
    [property: DataMember(Order = 0)] ActiveStatementId Id,
    [property: DataMember(Order = 1)] LinePositionSpan LineSpan);
