// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

internal partial class VSProjectContext : IEquatable<VSProjectContext>
{
    [JsonPropertyName("_vs_is_miscellaneous")]
    public bool IsMiscellaneous { get; set; }

    public static bool operator ==(VSProjectContext? left, VSProjectContext? right)
        => left is null ? right is null : left.Equals(right);

    public static bool operator !=(VSProjectContext? left, VSProjectContext? right)
        => !(left == right);

    public bool Equals(VSProjectContext? other)
        => other is not null &&
           string.Equals(Label, other.Label, StringComparison.Ordinal) &&
           string.Equals(Id, other.Id, StringComparison.Ordinal) &&
           Kind == other.Kind && VsKind == other.VsKind;

    public override bool Equals(object? obj)
        => obj is VSProjectContext other && Equals(other);

    public override int GetHashCode()
        => (Label is null ? 53 : Label.GetHashCode() * 13) ^
           (Id is null ? 61 : Id.GetHashCode() * 17) ^
           ((int)Kind * 19) ^
           (VsKind is null ? 13 : VsKind.GetHashCode() * 79);
}
