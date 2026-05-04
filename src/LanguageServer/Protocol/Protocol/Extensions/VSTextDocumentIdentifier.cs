// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System;
using System.Text.Json.Serialization;

/// <summary>
/// <see cref="VSTextDocumentIdentifier"/> extends <see cref="TextDocumentIdentifier"/> providing additional properties used by Visual Studio.
/// </summary>
internal sealed class VSTextDocumentIdentifier : TextDocumentIdentifier, IEquatable<VSTextDocumentIdentifier>
{
    /// <summary>
    /// Gets or sets the project context of the text document.
    /// </summary>
    [JsonPropertyName("_vs_projectContext")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public VSProjectContext? ProjectContext
    {
        get;
        set;
    }

    public static bool operator ==(VSTextDocumentIdentifier? value1, VSTextDocumentIdentifier? value2)
    {
        if (ReferenceEquals(value1, value2))
        {
            return true;
        }

        // Is null?
        if (ReferenceEquals(null, value2))
        {
            return false;
        }

        return value1?.Equals(value2) ?? false;
    }

    public static bool operator !=(VSTextDocumentIdentifier? value1, VSTextDocumentIdentifier? value2)
    {
        return !(value1 == value2);
    }

    /// <inheritdoc/>
    public bool Equals(VSTextDocumentIdentifier other)
    {
        return this.ProjectContext == other.ProjectContext
            && base.Equals(other);
    }

    /// <inheritdoc/>
    public override bool Equals(object obj)
    {
        if (obj is VSTextDocumentIdentifier other)
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
        return this.ProjectContext == null ? 89 : this.ProjectContext.GetHashCode()
            ^ (base.GetHashCode() * 79);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        var result = base.ToString();
        if (this.ProjectContext != null)
        {
            result += "|" + this.ProjectContext.Label + "|" + this.ProjectContext.Id;
        }

        return result;
    }
}
