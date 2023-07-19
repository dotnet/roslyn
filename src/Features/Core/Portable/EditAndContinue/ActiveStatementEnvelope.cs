// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EditAndContinue;

/// <summary>
/// Represents a <see cref="Span"/> that covers all active statements of <see cref="MemberBody"/> with a possible <see cref="Hole"/> excluded.
/// </summary>
internal readonly record struct ActiveStatementEnvelope(TextSpan Span, TextSpan Hole = default)
{
    public static implicit operator ActiveStatementEnvelope(TextSpan span)
        => new(span, default);
}
