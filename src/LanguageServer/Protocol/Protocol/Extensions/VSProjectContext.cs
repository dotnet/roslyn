// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System;
using System.Text.Json.Serialization;

/// <summary>
/// <see cref="VSProjectContext"/> represents a project context.
/// </summary>
internal class VSProjectContext : IEquatable<VSProjectContext>
{
    /// <summary>
    /// Gets or sets the label for the project context.
    /// </summary>
    [JsonPropertyName("_vs_label")]
    [JsonRequired]
    public string Label
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the unique identifier of the project context.
    /// </summary>
    [JsonPropertyName("_vs_id")]
    [JsonRequired]
    public string Id
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the context kind of the project context which is used to determine its associated icon.
    /// </summary>
    [JsonPropertyName("_vs_kind")]
    public VSProjectKind Kind
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets this project context represents miscellaneous files.
    /// </summary>
    [JsonPropertyName("_vs_is_miscellaneous")]
    public bool IsMiscellaneous
    {
        get;
        set;
    }

    public static bool operator ==(VSProjectContext? value1, VSProjectContext? value2)
    {
        if (ReferenceEquals(value1, value2))
        {
            return true;
        }

        // Is null?
        if (value2 is null)
        {
            return false;
        }

        return value1?.Equals(value2) ?? false;
    }

    public static bool operator !=(VSProjectContext? value1, VSProjectContext? value2)
    {
        return !(value1 == value2);
    }

    /// <inheritdoc/>
    public virtual bool Equals(VSProjectContext other)
    {
        return string.Equals(this.Label, other.Label, StringComparison.Ordinal)
            && string.Equals(this.Id, other.Id, StringComparison.Ordinal)
            && this.Kind == other.Kind;
    }

    /// <inheritdoc/>
    public override bool Equals(object obj)
    {
        if (obj is VSProjectContext other)
        {
            return this.Equals(other);
        }
        else
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return (this.Label == null ? 53 : this.Label.GetHashCode() * 13)
            ^ (this.Id == null ? 61 : this.Id.GetHashCode() * 17)
            ^ ((int)this.Kind * 19);
    }
}
